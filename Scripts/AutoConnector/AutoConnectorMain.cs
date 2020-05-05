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
    readonly CommandLine commandLine;
    readonly IProcessManager manager;

    public Program() {
      this.Runtime.UpdateFrequency = UpdateFrequency.Update1;
      this.manager = Process.CreateManager(Echo);
      var logger = new Logger(this.manager, this.Me.GetSurface(0), echo: this.Echo);
      this.manager.SetLogger(logger.Log);
      this.commandLine = new CommandLine("Auto connector station", logger.Log, this.manager);
      var ini = new MyIni();
      ini.Parse(this.Me.CustomData);
      new AutoConnectionDispatcher(this, this.commandLine, ini, logger.Log, this.manager);
    }

    public void Save() => this.manager.Save(s => this.Me.CustomData = s);

    public void Main(string argument, UpdateType updateSource) {
      this.commandLine.StartCmd(argument, CommandTrigger.User);
      this.manager.Tick();
    }
  }
}