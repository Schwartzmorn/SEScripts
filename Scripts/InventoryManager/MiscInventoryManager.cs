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
    public class MiscInventoryManager: LazyFilter, IOutputInventoryCollection {
      readonly List<IMyTerminalBlock> _inventoryOwners = new List<IMyTerminalBlock>();

      public MiscInventoryManager(IMyGridTerminalSystem gts, GridManager gridManager) {
        Scan(gts, gridManager);
        Scheduler.Inst.AddAction(new ScheduledAction(
          () => Scan(gts, gridManager), period: 300));
      }

      public void Scan(IMyGridTerminalSystem gts, GridManager gridManager) {
        gts.GetBlocksOfType(_inventoryOwners, i => _filter(i, gridManager));
      }

      public IEnumerable<IMyInventory> GetOutputInventories() {
        FilterLazily();
        return _inventoryOwners.Select(i => i.GetInventory());
      }

      protected override void Filter() {
        _inventoryOwners.RemoveAll(i => i.GetInventory() == null);
      }

      static bool _filter(IMyTerminalBlock block, GridManager gridManager) => block.GetInventory() != null &&
          gridManager.Manages(block.CubeGrid) &&
          !(block is IMyUserControllableGun) &&
          !(block is IMyCargoContainer) &&
          !(block is IMyProductionBlock) &&
          !(block is IMyGasGenerator) &&
          !(block is IMyGasTank) &&
          !(block is IMyReactor);
    }
  }
}
