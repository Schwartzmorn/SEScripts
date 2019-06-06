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
  readonly CmdLine _cmd;

  public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update1;
    IMyTextSurface keyboard;
    IMyCockpit cockpit;
    _initCockpit(out cockpit, out keyboard);
    var ct = new CoordsTransformer(cockpit, true);
    Logger.SetupGlobalInstance(new Logger(keyboard, 1.0f, new Color(27, 228, 33), new Color(0, 39, 15)), Echo);
    _cmd = new CmdLine("Boring machine", Log);
    var ini = new Ini(Me);
    var wc = new WheelsController(ini, this, _cmd, ct, cockpit);
    var ac = new ArmController(ini, this, _cmd, cockpit, wc);
    var ic = new InventoryWatcher(_cmd, GridTerminalSystem, cockpit);
    var client = new ConnectionClient(ini, this, _cmd);
    var rcs = new List<IMyRemoteControl>();
    GridTerminalSystem.GetBlocksOfType(rcs, r => r.CubeGrid == Me.CubeGrid);
    var frc = rcs.First(r => r.DisplayNameText.Contains("Forward"));
    var brc = rcs.First(r => r.DisplayNameText.Contains("Backward"));
    var ap = new Autopilot(ini, wc, _cmd, frc);
    var ah = new PilotAssist(ini, wc, GridTerminalSystem);
    ah.AddBraker(client);
    ah.AddDeactivator(ap);
    var ar = new ARHandler(ini, _cmd, brc);
    new MiningRoutines(ini, _cmd, ap);
    var progs = new List<IMyProgrammableBlock>();
    GridTerminalSystem.GetBlocksOfType(progs, pr => pr.CubeGrid == Me.CubeGrid);
    var p = progs.First(pr => pr.DisplayNameText.Contains("Auxillary"));
    Schedule(new ScheduledAction(() => p.TryRun(new CmdSerializer("gs-arm").AddArg(ac.Angle).AddArg(ac.TargetAngle).ToString()), 10));
    Schedule(new ScheduledAction(() => p.TryRun(new CmdSerializer("gs-con").AddArg(client.State).AddArg(client.FailReason).AddArg(client.Progress).ToString()), 10));
  }

  public void Save() => Scheduler.Inst.Save(s => Me.CustomData = s);

  public void Main(string arg, UpdateType us) {
    _cmd.HandleCmd(arg, CmdTrigger.User);
    if ((us & UpdateType.Update1) > 0)
      Scheduler.Inst.Tick();
  }

  void _initCockpit(out IMyCockpit cockpit, out IMyTextSurface keyboard) {
    var cockpits = new List<IMyCockpit>();
    GridTerminalSystem.GetBlocksOfType(cockpits, c => c.CubeGrid == Me.CubeGrid);
    if(cockpits.Count == 0)
      throw new ArgumentException("No cockpit found");
    cockpit = cockpits[0];
    keyboard = null;
    for(int i = 0; i < cockpit.SurfaceCount; ++i) {
      var surface = cockpit.GetSurface(i);
      if(surface.DisplayName == "Keyboard")
        keyboard = surface;
    }
  }
}
}