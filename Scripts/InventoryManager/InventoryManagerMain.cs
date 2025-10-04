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

namespace IngameScript {
  partial class Program: MyGridProgram {
    static char[] SPLIT_VALUES_CHAR = new char[] { ',' };
    static int GLOBAL_COUNTER = 0;

    AssemblerManager assemblerManager;
    CommandLine command;
    ContainerManager containerManager;
    GridManager gridManager;
    RefineryManager refineryManager;
    MiscInventoryManager miscInventoryManager;
    readonly IProcessManager manager ;

    public Program() {
      this.Runtime.UpdateFrequency = UpdateFrequency.Update1;
      this.manager = Process.CreateManager(this.Echo);
      var logger = new Logger(this.manager, this.Me.GetSurface(0), echo: this.Echo);
      this.command = new CommandLine("Inventory Manager", logger.Log, this.manager);

      int counter = 0;
      var ini = new MyIni();
      ini.TryParse(this.Me.CustomData);

      this.manager.Spawn(p => this.gridManager = new GridManager(this, ini, this.manager, logger.Log), "gm-init", period: ++counter, useOnce: true);
      this.manager.Spawn(p => this.containerManager = new ContainerManager(this.GridTerminalSystem, this.gridManager, this.manager, logger.Log), "cm-init", period: ++counter, useOnce: true);
      this.manager.Spawn(p => this.assemblerManager = new AssemblerManager(this.GridTerminalSystem, this.gridManager, this.manager), "am-init", period: ++counter, useOnce: true);
      this.manager.Spawn(p => this.refineryManager = new RefineryManager(this.GridTerminalSystem, this.gridManager, this.manager), "rm-init", period: ++counter, useOnce: true);
      this.manager.Spawn(p => this.miscInventoryManager = new MiscInventoryManager(this.GridTerminalSystem, this.gridManager, this.manager, logger.Log), "mim-init", period: ++counter, useOnce: true);
      this.manager.Spawn(p => new InventoriesGroomer(this.gridManager, this.containerManager, this.assemblerManager,
          this.miscInventoryManager, this.refineryManager, this.manager, logger.Log), "ig-init", period: ++counter, useOnce: true);
    }

    public void Save() {
    }

    public void Main(string argument, UpdateType updateSource) {
      this.command.StartCmd(argument, CommandTrigger.Cmd);
      if ((updateSource | UpdateType.Update1) > 0) {
        this.manager.Tick();
        GLOBAL_COUNTER++;
      }
    }
  }
}
