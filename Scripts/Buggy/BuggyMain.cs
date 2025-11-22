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
      var topRights = new List<IMyTextSurface>();
      IMyTextSurface keyboard;
      IMyCockpit cockpit;
      _manager = Process.CreateManager(Echo);
      _initCockpit(out cockpit, topLefts, topRights, out keyboard);
      var ct = new CoordinatesTransformer(cockpit, _manager);
      _cmd = new CommandLine("Buggy", Echo, _manager);
      var ini = new IniWatcher(Me, _manager);
      var wc = new WheelsController(_cmd, cockpit, GridTerminalSystem, ini, _manager, ct);
      var cc = new ConnectionClient(Me, ini, GridTerminalSystem, IGC, _cmd, _manager, Echo);
      var rcs = new List<IMyRemoteControl>();
      GridTerminalSystem.GetBlocksOfType(rcs, r => r.CubeGrid == Me.CubeGrid);
      var ah = new PilotAssist(Me, GridTerminalSystem, ini, Echo, _manager, wc, _cmd);
      ah.AddBraker(cc);
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

    void _initCockpit(out IMyCockpit cockpit, List<IMyTextSurface> topLefts, List<IMyTextSurface> topRights, out IMyTextSurface keyboard)
    {
      var cockpits = new List<IMyCockpit>();
      GridTerminalSystem.GetBlocksOfType(cockpits, c => c.CubeGrid == Me.CubeGrid);
      if (cockpits.Count == 0)
      {
        throw new ArgumentException("No cockpit found");
      }

      cockpit = cockpits[0];
      keyboard = null;
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
            topRights.Add(surface);
          }
          else if (surface.DisplayName == "Keyboard")
          {
            keyboard = surface;
          }
        }
      }
    }
  }
}
