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
    ColorScheme _scheme = new ColorScheme();

    public Program()
    {
      Runtime.UpdateFrequency = UpdateFrequency.Update1;
      var topLefts = new List<IMyTextSurface>();
      IMyTextSurface keyboard;
      IMyCockpit cockpit;
      _manager = Process.CreateManager(Echo);
      _initCockpit(out cockpit, topLefts, out keyboard);
      var logger = new ScreenLogger(_manager, keyboard, new Color(0, 39, 15), new Color(27, 228, 33), Echo, 1.0f);
      LOG_SETTINGS.SetLogger(logger.Log);
      var ct = new CoordinatesTransformer(cockpit, _manager);
      _cmd = new CommandLine("Boring machine", logger.Log, _manager);
      var ini = new IniWatcher(Me, _manager);
      var wc = new WheelsController(_cmd, cockpit, GridTerminalSystem, ini, _manager, ct);
      var ac = new ArmController(ini, this, _cmd, cockpit, wc, _manager);
      var iw = new InventoryWatcher(_cmd, GridTerminalSystem, cockpit);
      var cc = new ConnectionClient(Me, ini, GridTerminalSystem, IGC, _cmd, _manager, logger.Log);
      var rcs = new List<IMyRemoteControl>();
      GridTerminalSystem.GetBlocksOfType(rcs, r => r.CubeGrid == Me.CubeGrid);
      IMyRemoteControl frc = rcs.First(r => r.CubeGrid == Me.CubeGrid && r.CustomName.Contains("Forward"));
      IMyRemoteControl brc = rcs.First(r => r.CubeGrid == Me.CubeGrid && r.CustomName.Contains("Backward"));
      var ap = new Autopilot(ini, wc, _cmd, frc, logger.Log, _manager);
      var ah = new PilotAssist(Me, GridTerminalSystem, ini, logger.Log, _manager, wc, _cmd);
      ah.AddBraker(cc);
      ah.AddDeactivator(ap);
      var ar = new AutoRoutineHandler(_cmd);
      var arParser = new RoutineParser(_cmd);
      ar.AddRoutines(arParser.Parse(brc.CustomData));

      // new MiningRoutines(ini, _cmd, ap, _manager);
      var progs = new List<IMyProgrammableBlock>();
      GridTerminalSystem.GetBlocksOfType(progs, pr => pr.CubeGrid == Me.CubeGrid);
      var genStatus = new GeneralStatus(this, ac, cc, wc, ap);
      new ScreensController(genStatus, iw, topLefts, _scheme, cockpit.CustomData, _manager);
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

    void _initCockpit(out IMyCockpit cockpit, List<IMyTextSurface> topLefts, out IMyTextSurface loggerScreen)
    {
      var cockpits = new List<IMyCockpit>();
      GridTerminalSystem.GetBlocksOfType(cockpits, c => c.CubeGrid == Me.CubeGrid);
      if (cockpits.Count == 0)
      {
        throw new ArgumentException("No cockpit found");
      }

      cockpit = cockpits[0];
      loggerScreen = null;
      foreach (IMyCockpit cpit in cockpits)
      {
        for (int i = 0; i < cpit.SurfaceCount; ++i)
        {
          IMyTextSurface surface = cpit.GetSurface(i);
          if (surface.DisplayName == "Top Left Screen")
          {
            topLefts.Add(surface);
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
