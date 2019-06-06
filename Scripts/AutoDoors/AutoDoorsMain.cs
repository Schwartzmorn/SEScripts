using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
  partial class Program: MyGridProgram {
    private readonly DoorManager _doorManager;

    public Program() {
      Runtime.UpdateFrequency = UpdateFrequency.Update1;
      Logger.SetupGlobalInstance(new Logger(Me.GetSurface(0)), Echo);
      _doorManager = new DoorManager(this);
      Schedule(Logger.Flush);
    }

    public void Save() {
    }

    public void Main(string argument, UpdateType updateSource) => Scheduler.Inst.Tick();
  }
}