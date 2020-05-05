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
      readonly Action<string> logger;

      public string Name => "Misc. inventories manager";

      public MiscInventoryManager(IMyGridTerminalSystem gts, GridManager gridManager, IProcessSpawner spawner, Action<string> logger) {
        this.Scan(gts, gridManager);
        spawner.Spawn(p => this.Scan(gts, gridManager), "misc-inventory-groomer", period: 300);
        this.logger = logger;
      }

      public void Scan(IMyGridTerminalSystem gts, GridManager gridManager) {
        this.log("Scanning...");
        gts.GetBlocksOfType(this._inventoryOwners, i => _filter(i, gridManager));
        this.log($"Found {this._inventoryOwners.Count} inventories");
      }

      public IEnumerable<IMyInventory> GetOutputInventories() {
        this.FilterLazily();
        return this._inventoryOwners.Select(i => i.GetInventory());
      }

      protected override void Filter() {
        this._inventoryOwners.RemoveAll(i => i.GetInventory() == null);
      }

      void log(string s) => this.logger?.Invoke("MIM: " + s);

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
