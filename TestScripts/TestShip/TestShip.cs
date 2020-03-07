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
    CoordinatesTransformer _ct;
    IMyProgrammableBlock _b;
    int counter = 0;

    public Program() {
      Runtime.UpdateFrequency = UpdateFrequency.Update1;
      Logger.SetupGlobalInstance(new Logger(Me.GetSurface(0), size: 0.5f), Echo);
      Schedule(Logger.Flush);
      var bs = new List<IMyProgrammableBlock>();
      GridTerminalSystem.GetBlocksOfType(bs);
      _b = bs.First(b => b != Me);
    }

    public void Save() {
    }

    public void Main(string argument, UpdateType updateSource) {
      Scheduler.Inst.Tick();
      if ((updateSource & UpdateType.Update1) > 0) {
        if(Me.DisplayNameText == "Programmable block A") {
          _b.TryRun("A");
          _b.TryRun("B");
        }
        Log($"{counter++}: {argument}");
      }
      if (argument != "") {
        Log(argument);
      }
    }
  }
}