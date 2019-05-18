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
  partial class Program {
    public class RefineryManager: LazyFilter, IOutputInventoryCollection, IInputInventoryCollection {
      private readonly List<IMyRefinery> _refineries = new List<IMyRefinery>();

      public RefineryManager(IMyGridTerminalSystem gts, GridManager gridManager) {
        Scan(gts, gridManager);
        Scheduler.Inst.AddAction(new ScheduledAction(() => Scan(gts, gridManager), period: 100));
      }

      public void Scan(IMyGridTerminalSystem gts, GridManager gridManager) {
        _refineries.Clear();
        gts.GetBlocksOfType(_refineries, a => a.GetInventory(1) != null && gridManager.Manages(a.CubeGrid));
      }

      public IEnumerable<IMyInventory> GetOutputInventories() {
        FilterLazily();
        return _refineries.Select(a => a.GetInventory(1));
      }

      public IEnumerable<IMyInventory> GetInputInventories() {
        FilterLazily();
        return _refineries.Select(a => a.GetInventory(0));
      }

      protected override void Filter() => _refineries.RemoveAll(c => c.GetInventory(1) == null);
    }
  }
}
