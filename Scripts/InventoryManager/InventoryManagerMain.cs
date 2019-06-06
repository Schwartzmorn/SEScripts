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
    static IMyGridProgramRuntimeInfo RUNTIME;
    static DateTime START_TIME;
    static char[] SPLIT_VALUES_CHAR = new char[] { ',' };
    static int GLOBAL_COUNTER = 0;

    private AssemblerManager _assemblerManager;
    private ContainerManager _containerManager;
    private GridManager _gridManager;
    private InventoriesGroomer _inventoriesGroomer;
    private RefineryManager _refineryManager;
    private MiscInventoryManager _miscInventoryManager;

    public Program() {
      RUNTIME = Runtime;
      Runtime.UpdateFrequency = UpdateFrequency.Update1;
      Logger.SetupGlobalInstance(new Logger(Me.GetSurface(0)), Echo);
      Schedule(() => START_TIME = DateTime.Now);
      Schedule(Logger.Flush);

      int counter = 0;
      var ini = new MyIni();
      ini.TryParse(Me.CustomData);

      Schedule(new ScheduledAction(
        () => _gridManager = new GridManager(this, ini),
        period: ++counter, useOnce: true));
      Schedule(new ScheduledAction(
        () => _containerManager = new ContainerManager(GridTerminalSystem, _gridManager),
        period: ++counter, useOnce: true));
      Schedule(new ScheduledAction(
        () => _assemblerManager = new AssemblerManager(GridTerminalSystem, _gridManager),
        period: ++counter, useOnce: true));
      Schedule(new ScheduledAction(
        () => _refineryManager = new RefineryManager(GridTerminalSystem, _gridManager),
        period: ++counter, useOnce: true));
      Schedule(new ScheduledAction(
        () => _miscInventoryManager = new MiscInventoryManager(GridTerminalSystem, _gridManager),
        period: ++counter, useOnce: true));
      Schedule(new ScheduledAction(
        () => _inventoriesGroomer = new InventoriesGroomer(_gridManager, _containerManager,
          _assemblerManager, _miscInventoryManager, _refineryManager),
        period: ++counter, useOnce: true));
    }

    public void Save() {
    }

    static void EchoStat(string message) =>
        Log((DateTime.Now - START_TIME).TotalMilliseconds.ToString("#,000.00") + " - "
          + RUNTIME.CurrentInstructionCount + '\n' + message + '\n');

    public void Main(string argument, UpdateType updateSource) {
      Scheduler.Inst.Tick();
      GLOBAL_COUNTER++;
    }
  }
}