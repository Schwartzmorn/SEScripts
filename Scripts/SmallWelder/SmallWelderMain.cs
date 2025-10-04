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
  partial class Program : MyGridProgram {
    readonly CommandLine _cmd;
    readonly IProcessManager _manager;
    public Program() {
      Runtime.UpdateFrequency = UpdateFrequency.Update1;
      var cockpits = new List<IMyCockpit>();
      GridTerminalSystem.GetBlocksOfType(cockpits, c => c.CubeGrid == Me.CubeGrid);
      IMyCockpit cockpit = cockpits.First();
      _manager = Process.CreateManager(Echo);
      var ct = new CoordinatesTransformer(cockpit, _manager);
      var logger = new Logger(_manager, cockpit.GetSurface(0), new Color(0, 39, 15), new Color(27, 228, 33), Echo, 1.0f);
      _cmd = new CommandLine("Small welder", logger.Log, _manager);
      var ini = new IniWatcher(Me, _manager);
      var wc = new WheelsController(_cmd, cockpit, GridTerminalSystem, ini, _manager, ct);
      var ac = new ArmController(ini, this, _cmd, cockpit, wc, _manager);
      var client = new ConnectionClient(ini, GridTerminalSystem, IGC, _cmd, _manager, logger.Log);
      var ah = new PilotAssist(GridTerminalSystem, ini, logger.Log, _manager, wc);
      ah.AddBraker(client);
    }

    public void Save() => _manager.Save(s => Me.CustomData = s);

    public void Main(string arg, UpdateType us) {
      _cmd.StartCmd(arg, CommandTrigger.User);
      if ((us & UpdateType.Update1) > 0) {
        _manager.Tick();
      }
    }
  }
}
