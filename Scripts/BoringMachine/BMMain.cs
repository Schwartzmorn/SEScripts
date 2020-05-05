using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript {
  partial class Program : MyGridProgram {
    readonly CommandLine cmd;
    readonly IProcessManager manager;
    ColorScheme scheme = new ColorScheme();

    public Program() {
      this.Runtime.UpdateFrequency = UpdateFrequency.Update1;
      var topLefts = new List<IMyTextSurface>();
      var topRights = new List<IMyTextSurface>();
      IMyTextSurface keyboard;
      IMyCockpit cockpit;
      this.manager = Process.CreateManager(this.Echo);
      this.initCockpit(out cockpit, topLefts, topRights, out keyboard);
      var ct = new CoordinatesTransformer(cockpit, this.manager);
      var logger = new Logger(this.manager, keyboard, new Color(0, 39, 15), new Color(27, 228, 33), this.Echo, 1.0f);
      this.cmd = new CommandLine("Boring machine", logger.Log, this.manager);
      var ini = new IniWatcher(this.Me, this.manager);
      var wc = new WheelsController(this.cmd, cockpit, this.GridTerminalSystem, ini, this.manager, ct);
      var ac = new ArmController(ini, this, this.cmd, cockpit, wc, this.manager);
      var iw = new InventoryWatcher(this.cmd, this.GridTerminalSystem, cockpit);
      var cc = new ConnectionClient(ini, this.GridTerminalSystem, this.IGC, this.cmd, this.manager, logger.Log);
      var rcs = new List<IMyRemoteControl>();
      this.GridTerminalSystem.GetBlocksOfType(rcs, r => r.CubeGrid == this.Me.CubeGrid);
      IMyRemoteControl frc = rcs.First(r => r.DisplayNameText.Contains("Forward"));
      IMyRemoteControl brc = rcs.First(r => r.DisplayNameText.Contains("Backward"));
      var ap = new Autopilot(ini, wc, this.cmd, frc, logger.Log, this.manager);
      var ah = new PilotAssist(this.GridTerminalSystem, ini, logger.Log, this.manager, wc);
      ah.AddBraker(cc);
      ah.AddDeactivator(ap);
      var ar = new AutoRoutineHandler(this.cmd);
      // TODO parse routines
      new MiningRoutines(ini, this.cmd, ap, this.manager);
      var progs = new List<IMyProgrammableBlock>();
      this.GridTerminalSystem.GetBlocksOfType(progs, pr => pr.CubeGrid == this.Me.CubeGrid);
      var genStatus = new GeneralStatus(this, ac, cc);
      new ScreensController(genStatus, iw, topLefts, topRights, this.scheme, cockpit.CustomData, this.manager);
    }

    public void Save() => this.manager.Save(s => this.Me.CustomData = s);

    public void Main(string arg, UpdateType us) {
      this.cmd.StartCmd(arg, CommandTrigger.User);
      if ((us & UpdateType.Update1) > 0) {
        this.manager.Tick();
      }
    }

    void initCockpit(out IMyCockpit cockpit, List<IMyTextSurface> topLefts, List<IMyTextSurface> topRights, out IMyTextSurface keyboard) {
      var cockpits = new List<IMyCockpit>();
      this.GridTerminalSystem.GetBlocksOfType(cockpits, c => c.CubeGrid == this.Me.CubeGrid);
      if (cockpits.Count == 0) {
        throw new ArgumentException("No cockpit found");
      }

      cockpit = cockpits[0];
      keyboard = null;
      foreach (IMyCockpit cpit in cockpits) {
        for (int i = 0; i < cpit.SurfaceCount; ++i) {
          IMyTextSurface surface = cpit.GetSurface(i);
          if (surface.DisplayName == "Top Left Screen") {
            topLefts.Add(surface);
          } else if (surface.DisplayName == "Top Right Screen") {
            topRights.Add(surface);
          } else if (surface.DisplayName == "Keyboard") {
            keyboard = surface;
          }
        }
      }
    }
  }
}