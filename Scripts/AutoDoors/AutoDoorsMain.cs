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
    readonly DoorManager doorManager;
    readonly IProcessManager manager;

    public Program() {
      this.Runtime.UpdateFrequency = UpdateFrequency.Update1;
      this.manager = Process.CreateManager(Echo);
      var logger = new Logger(this.manager, this.Me.GetSurface(0), echo: this.Echo);

      this.doorManager = new DoorManager(this, this.manager, logger.Log);
    }

    public void Save() {
    }

    public void Main(string argument, UpdateType updateSource) => this.manager.Tick();
  }
}