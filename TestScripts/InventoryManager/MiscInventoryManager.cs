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
  partial class Program {
    public class MiscInventoryManager {
    }
    public override string ToString() => "MiscInventoryManager: " + _inventoryOwners.ConvertAll(owner => '"' + owner.DisplayNameText + '"').Aggregate((a, b) => a + ", " + b);

    public void ScanMiscInventories(IMyGridTerminalSystem GTS, GridManager gridManager, bool full) {
      if (full) {
        List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
        GTS.GetBlocks(blocks);
        _inventoryOwners = blocks.FindAll(block => Filter(block, gridManager));
      } else {
        _inventoryOwners = _inventoryOwners.FindAll(block => block.GetInventory() != null);
      }
    }

    bool Filter(IMyTerminalBlock block, GridManager gridManager) => block.GetInventory() != null &&
        gridManager.Manages(block.CubeGrid) &&
        !(block is IMyUserControllableGun) &&
        !(block is IMyCargoContainer) &&
        !(block is IMyProductionBlock) &&
        !(block is IMyGasGenerator) &&
        !(block is IMyGasTank) &&
        !(block is IMyReactor);
    List<IMyTerminalBlock> _inventoryOwners;
  }
}
