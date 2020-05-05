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
    readonly IProcessManager manager;
    readonly SolarManager solarManager;
    readonly CommandLine command;

    public Program() {
      this.Runtime.UpdateFrequency = UpdateFrequency.Update1;
      this.manager = Process.CreateManager(this.Echo);
      var logger = new Logger(this.manager, this.Me.GetSurface(0), echo: this.Echo);
      this.command = new CommandLine("Solar Manager", logger.Log, this.manager);
      this.solarManager = new SolarManager(this, this.command, this.manager, logger.Log);
    }

    public void Save() {
      var ini = new MyIni();
      ini.TryParse(this.Me.CustomData);
      this.manager.Save(s => this.Me.CustomData = s, ini);
    }

    public void Main(string args, UpdateType updateSource) {
      this.command.StartCmd(args, CommandTrigger.Cmd);
      if ((updateSource & UpdateType.Update1) > 0) {
        this.manager.Tick();
      }
    }
  }
}
