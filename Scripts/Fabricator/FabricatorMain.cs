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
    readonly CommandLine commandLine;
    readonly IProcessManager manager;

    public static T AssertNonNull<T>(T obj, string msg) {
      if (obj == null) {
        throw new InvalidOperationException(msg);
      }
      return obj;
    }
    public static IEnumerable<T> AssertNonEmpty<T>(IEnumerable<T> obj, string msg) {
      if (obj.Count() == 0) {
        throw new InvalidOperationException(msg);
      }
      return obj;
    }

    public Program() {
      this.Runtime.UpdateFrequency = UpdateFrequency.Update1;
      this.manager = Process.CreateManager(this.Echo);
      var logger = new Logger(this.manager, this.Me.GetSurface(0), echo: this.Echo);
      this.manager.SetLogger(logger.Log);
      this.commandLine = new CommandLine("Fabricator", logger.Log, this.manager);
      var ini = new MyIni();
      ini.Parse(this.Me.CustomData);
      new Fabricator(ini, this.GridTerminalSystem, this.manager, this.commandLine, logger.Log);
    }
    //public void Save() => this.manager.Save(s => this.Me.CustomData = s);

    public void Main(string argument, UpdateType updateSource) {
      this.commandLine.StartCmd(argument, CommandTrigger.User);
      this.manager.Tick();
    }
  }
}
