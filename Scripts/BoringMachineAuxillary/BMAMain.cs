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
    readonly CmdLine _cmd;
    ColorScheme _scheme = new ColorScheme();

    public Program() {
      Runtime.UpdateFrequency = UpdateFrequency.Update1;
      IMyTextSurface topLeft, topRight, keyboard;
      IMyCockpit cockpit;
      _initCockpit(out cockpit, out topLeft, out topRight, out keyboard);
      var ct = new CoordsTransformer(cockpit, false);
      _initLogger(keyboard);
      _cmd = new CmdLine("Boring machine", Log);
      var genStatus = new GeneralStatus(this, _cmd);
      var wb = new WheelBase();
      var wheels = new List<IMyMotorSuspension>();
      GridTerminalSystem.GetBlocksOfType(wheels, w => w.CubeGrid == Me.CubeGrid && w.DisplayNameText.Contains("Power"));
      wheels.ForEach(w => wb.AddWheel(new PowerWheel(w, wb, ct)));
      var ic = new InventoriesController(_cmd, ct, GridTerminalSystem, cockpit, wb.CenterOfTurnZ + 2);
      new ScreensController(genStatus, ic, topLeft, topRight, _scheme, cockpit.CustomData);
    }

    public void Save() { }

    public void Main(string arg, UpdateType us) {
      _cmd.HandleCmd(arg, CmdTrigger.User);
      if((us & UpdateType.Update1) > 0)
        Scheduler.Inst.Tick();
    }

    void _initCockpit(out IMyCockpit cockpit, out IMyTextSurface topLeft, out IMyTextSurface topRight, out IMyTextSurface keyboard) {
      var cockpits = new List<IMyCockpit>();
      GridTerminalSystem.GetBlocksOfType(cockpits, c => c.CubeGrid == Me.CubeGrid);
      if(cockpits.Count == 0)
        throw new ArgumentException("No cockpit found");
      cockpit = cockpits[0];
      topLeft = topRight = keyboard = null;
      for(int i = 0; i < cockpit.SurfaceCount; ++i) {
        var surface = cockpit.GetSurface(i);
        if(surface.DisplayName == "Top Left Screen")
          topLeft = surface;
        else if(surface.DisplayName == "Top Right Screen")
          topRight = surface;
        else if(surface.DisplayName == "Keyboard")
          keyboard = surface;
      }
    }

    void _initLogger(IMyTextSurface keyboard) {
      Logger.SetupGlobalInstance(new Logger(keyboard, 1.0f, _scheme.Light, _scheme.Dark), Echo);
      Log("Booting up...");
    }
  }
}