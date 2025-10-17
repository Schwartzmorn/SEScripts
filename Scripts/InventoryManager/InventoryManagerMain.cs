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
  public partial class Program : MyGridProgram
  {
    static char[] SPLIT_VALUES_CHAR = new char[] { ',' };
    static int GLOBAL_COUNTER = 0;

    AssemblerManager _assemblerManager;
    CommandLine _command;
    ContainerManager _containerManager;
    GridManager _gridManager;
    RefineryManager _refineryManager;
    MiscInventoryManager _miscInventoryManager;
    readonly IProcessManager _manager;

    public Program()
    {
      Runtime.UpdateFrequency = UpdateFrequency.Update1;
      _manager = Process.CreateManager(Echo);
      var logger = new Logger(_manager, Me.GetSurface(0), echo: Echo);
      _command = new CommandLine("Inventory Manager", logger.Log, _manager);

      int counter = 0;
      var ini = new MyIni();
      ini.TryParse(Me.CustomData);

      _manager.Spawn(p => _gridManager = new GridManager(this, ini, _manager, logger.Log), "gm-init", period: ++counter, useOnce: true);
      _manager.Spawn(p => _containerManager = new ContainerManager(GridTerminalSystem, _gridManager, _manager, logger.Log), "cm-init", period: ++counter, useOnce: true);
      _manager.Spawn(p => _assemblerManager = new AssemblerManager(GridTerminalSystem, _gridManager, _manager), "am-init", period: ++counter, useOnce: true);
      _manager.Spawn(p => _refineryManager = new RefineryManager(GridTerminalSystem, _gridManager, _manager), "rm-init", period: ++counter, useOnce: true);
      _manager.Spawn(p => _miscInventoryManager = new MiscInventoryManager(GridTerminalSystem, _gridManager, _manager, logger.Log), "mim-init", period: ++counter, useOnce: true);
      _manager.Spawn(p => new InventoriesGroomer(_containerManager, _assemblerManager,
          _miscInventoryManager, _refineryManager, _manager, logger.Log), "ig-init", period: ++counter, useOnce: true);
    }

    public void Save()
    {
    }

    public void Main(string argument, UpdateType updateSource)
    {
      _command.StartCmd(argument, CommandTrigger.Cmd);
      if ((updateSource | UpdateType.Update1) > 0)
      {
        _manager.Tick();
        GLOBAL_COUNTER++;
      }
    }
  }
}
