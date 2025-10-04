using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript {
  partial class Program: MyGridProgram {
    readonly CommandLine cmd;
    readonly IProcessManager manager;
    ColorScheme scheme = new ColorScheme();

    public Program() {
      this.Runtime.UpdateFrequency = UpdateFrequency.Update1;
      IMyTextSurface topLeft, topRight, keyboard;
      IMyCockpit cockpit;
      this.manager = Process.CreateManager(this.Echo);
      this.initCockpit(out cockpit, out topLeft, out topRight, out keyboard);
      var ct = new CoordinatesTransformer(cockpit, this.manager);
      var logger = new Logger(this.manager, keyboard, this.scheme.Light, this.scheme.Dark, this.Echo);
      logger.Log("Booting up...");
      this.cmd = new CommandLine("Boring machine", logger.Log, this.manager);
      var genStatus = new GeneralStatus(this, this.cmd);
      var wb = new WheelBase();
      var wheels = new List<IMyMotorSuspension>();
      this.GridTerminalSystem.GetBlocksOfType(wheels, w => w.CubeGrid == this.Me.CubeGrid && w.CustomName.Contains("Power"));
      wheels.ForEach(w => wb.AddWheel(new PowerWheel(w, wb, ct)));
      var ic = new InventoriesController(ct, this.GridTerminalSystem, cockpit, wb.CenterOfTurnZ + 2, this.manager);
      new ScreensController(genStatus, ic, topLeft, topRight, this.scheme, cockpit.CustomData, this.manager);
    }

    public void Save() { }

    public void Main(string arg, UpdateType us) {
      this.cmd.StartCmd(arg, CommandTrigger.User);
      if((us & UpdateType.Update1) > 0) {
        this.manager.Tick();
      }
    }

    void initCockpit(out IMyCockpit cockpit, out IMyTextSurface topLeft, out IMyTextSurface topRight, out IMyTextSurface keyboard) {
      var cockpits = new List<IMyCockpit>();
      this.GridTerminalSystem.GetBlocksOfType(cockpits, c => c.CubeGrid == this.Me.CubeGrid);
      if(cockpits.Count == 0) {
        throw new ArgumentException("No cockpit found");
      }

      cockpit = cockpits[0];
      topLeft = topRight = keyboard = null;
      for(int i = 0; i < cockpit.SurfaceCount; ++i) {
        IMyTextSurface surface = cockpit.GetSurface(i);
        if(surface.DisplayName == "Top Left Screen") {
          topLeft = surface;
        } else if(surface.DisplayName == "Top Right Screen") {
          topRight = surface;
        } else if(surface.DisplayName == "Keyboard") {
          keyboard = surface;
        }
      }
    }
  }
}
