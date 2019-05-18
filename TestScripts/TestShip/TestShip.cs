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
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
  partial class Program : MyGridProgram {

    readonly IMyCockpit _cockpit;
    readonly CoordinatesTransformer _transformer;
    readonly CmdLine _command;

    public Program() {
      Runtime.UpdateFrequency = UpdateFrequency.Update1;
      var cs = new List<IMyCockpit>();
      GridTerminalSystem.GetBlocksOfType(cs);
      _cockpit = cs[0];
      Logger.SetupGlobalInstance(new Logger(_cockpit.GetSurface(0), fontSize: 1), Echo);
      _transformer = new CoordinatesTransformer(_cockpit, true);
      _command = new CmdLine("Test", Logger.Inst.Log);
      var wc = new WheelsController(this, new MyIni(), _command, _transformer, _cockpit);
      //new Autopilot(wc, _command, GridTerminalSystem.GetBlockWithName("BM Remote Control (Forward)") as IMyRemoteControl);
      Scheduler.Inst.AddAction(Logger.Flush);
    }

    public void Save() {
    }

    public void Main(string argument, UpdateType updateSource) {
      _command.HandleCmd(argument, true);
      Scheduler.Inst.Tick();
    }
  }
}