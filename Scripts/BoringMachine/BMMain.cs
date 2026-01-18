using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
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

namespace IngameScript
{
  partial class Program : MyGridProgram
  {
    readonly CommandLine _cmd;
    readonly IProcessManager _manager;

    public Program()
    {
      Runtime.UpdateFrequency = UpdateFrequency.Update1;
      IMyTextSurface status;
      IMyTextSurface sensor;
      IMyTextSurface keyboard;
      IMyCockpit cockpit;
      _manager = Process.CreateManager(Echo);
      _initCockpit(out cockpit, out status, out sensor, out keyboard);
      var logger = new ScreenLogger(_manager, keyboard, null, null, Echo, 0.5f);
      var ini = new IniWatcher(Me, _manager);
      _cmd = new CommandLine("Boring machine", logger.Log, _manager);
      LOG_SETTINGS.Init(logger.Log, ini, _cmd, _manager);
      var ct = new CoordinatesTransformer(cockpit, _manager);
      var wc = new WheelsController(_cmd, cockpit, GridTerminalSystem, ini, _manager, ct);
      var ac = new ArmController(ini, this, _cmd, cockpit, wc, _manager);
      var iw = new InventoryWatcher(_cmd, GridTerminalSystem, cockpit);
      var cc = new ConnectionClient(Me, ini, GridTerminalSystem, IGC, _cmd, _manager, logger.Log);
      var rcs = new List<IMyRemoteControl>();
      GridTerminalSystem.GetBlocksOfType(rcs, r => r.CubeGrid == Me.CubeGrid);
      IMyRemoteControl frc = rcs.First(r => r.CubeGrid == Me.CubeGrid && r.CustomName.Contains("Forward"));
      IMyRemoteControl brc = rcs.First(r => r.CubeGrid == Me.CubeGrid && r.CustomName.Contains("Backward"));
      var sensorManager = new SensorManager(cockpit, GridTerminalSystem, _manager);
      var ap = new Autopilot(ini, wc, _cmd, frc, _manager, sensorManager);
      var ah = new PilotAssist(Me, GridTerminalSystem, ini, _manager, wc, _cmd);
      ah.AddBraker(cc);
      ah.AddDeactivator(ap);
      var ar = new AutoRoutineHandler(_cmd);
      var arParser = new RoutineParser(_cmd);
      ar.AddRoutines(arParser.Parse(brc.CustomData));

      // new MiningRoutines(ini, _cmd, ap, _manager);
      var progs = new List<IMyProgrammableBlock>();
      GridTerminalSystem.GetBlocksOfType(progs, pr => pr.CubeGrid == Me.CubeGrid);
      var genStatus = new GeneralStatus(this, ac, cc, wc, ap);
      new ScreensController(genStatus, iw, status, sensor, cockpit.CustomData, _manager, sensorManager);
    }

    public void Save() => _manager.Save(s => Me.CustomData = s);

    public void Main(string arg, UpdateType us)
    {
      _cmd.StartCmd(arg, CommandTrigger.User);
      if ((us & UpdateType.Update1) > 0)
      {
        _manager.Tick();
      }
    }

    void _initCockpit(out IMyCockpit cockpit, out IMyTextSurface topLeft, out IMyTextSurface topCenter, out IMyTextSurface loggerScreen)
    {
      var cockpits = new List<IMyCockpit>();
      topLeft = null;
      topCenter = null;
      loggerScreen = null;
      GridTerminalSystem.GetBlocksOfType(cockpits, c => c.CubeGrid == Me.CubeGrid);
      if (cockpits.Count == 0)
      {
        throw new ArgumentException("No cockpit found");
      }

      cockpit = cockpits[0];
      foreach (IMyCockpit cpit in cockpits)
      {
        for (int i = 0; i < cpit.SurfaceCount; ++i)
        {
          var surface = cpit.GetSurface(i);
          if (surface.DisplayName == "Top Left Screen")
          {
            topLeft = surface;
          }
          else if (surface.DisplayName == "Top Center Screen")
          {
            topCenter = surface;
          }
          else if (surface.DisplayName == "Top Right Screen")
          {
            if (loggerScreen == null)
            {
              // we want the logger on the keyboard screen if possible
              loggerScreen = surface;
            }
          }
          else if (surface.DisplayName == "Keyboard")
          {
            loggerScreen = surface;
          }
        }
      }
    }
  }
}
