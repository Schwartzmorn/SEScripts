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

namespace IngameScript
{
  partial class Program : MyGridProgram
  {
    readonly CommandLine _cmd;
    readonly IProcessManager _manager;
    ColorScheme _scheme = new ColorScheme();

    public Program()
    {
      Runtime.UpdateFrequency = UpdateFrequency.Update1;
      IMyTextSurface topLeft, topRight, keyboard;
      IMyCockpit cockpit;
      _manager = Process.CreateManager(Echo);
      _initCockpit(out cockpit, out topLeft, out topRight, out keyboard);
      var ct = new CoordinatesTransformer(cockpit, _manager);
      var logger = new ScreenLogger(_manager, keyboard, _scheme.Light, _scheme.Dark, Echo);
      logger.Log("Booting up...");
      _cmd = new CommandLine("Boring machine", logger.Log, _manager);
      var genStatus = new GeneralStatus(this, _cmd);
      var wb = new WheelBase();
      var wheels = new List<IMyMotorSuspension>();
      GridTerminalSystem.GetBlocksOfType(wheels, w => w.CubeGrid == Me.CubeGrid && w.CustomName.Contains("Power"));
      wheels.ForEach(w => wb.AddWheel(new PowerWheel(w, wb, ct)));
      var ic = new InventoriesController(ct, GridTerminalSystem, cockpit, wb.DefaultCenterOfTurnZ + 2, _manager);
      new ScreensController(genStatus, ic, topLeft, topRight, _scheme, cockpit.CustomData, _manager);
    }

    public void Save() { }

    public void Main(string arg, UpdateType us)
    {
      _cmd.StartCmd(arg, CommandTrigger.User);
      if ((us & UpdateType.Update1) > 0)
      {
        _manager.Tick();
      }
    }

    void _initCockpit(out IMyCockpit cockpit, out IMyTextSurface topLeft, out IMyTextSurface topRight, out IMyTextSurface keyboard)
    {
      var cockpits = new List<IMyCockpit>();
      GridTerminalSystem.GetBlocksOfType(cockpits, c => c.CubeGrid == Me.CubeGrid);
      if (cockpits.Count == 0)
      {
        throw new ArgumentException("No cockpit found");
      }

      cockpit = cockpits[0];
      topLeft = topRight = keyboard = null;
      for (int i = 0; i < cockpit.SurfaceCount; ++i)
      {
        IMyTextSurface surface = cockpit.GetSurface(i);
        if (surface.DisplayName == "Top Left Screen")
        {
          topLeft = surface;
        }
        else if (surface.DisplayName == "Top Right Screen")
        {
          topRight = surface;
        }
        else if (surface.DisplayName == "Keyboard")
        {
          keyboard = surface;
        }
      }
    }
  }
}
