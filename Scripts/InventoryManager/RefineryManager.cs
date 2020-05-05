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
      private readonly List<IMyRefinery> refineries = new List<IMyRefinery>();

      public string Name => "Refineries manager";

      public RefineryManager(IMyGridTerminalSystem gts, GridManager gridManager, IProcessSpawner spawner) {
        this.Scan(gts, gridManager);
        spawner.Spawn(p => this.Scan(gts, gridManager), "refinery-scanner", period: 100);
      }

      public void Scan(IMyGridTerminalSystem gts, GridManager gridManager) {
        this.refineries.Clear();
        gts.GetBlocksOfType(this.refineries, a => a.GetInventory(1) != null && gridManager.Manages(a.CubeGrid));
      }

      public IEnumerable<IMyInventory> GetOutputInventories() {
        this.FilterLazily();
        return this.refineries.Select(a => a.GetInventory(1));
      }

      public IEnumerable<IMyInventory> GetInputInventories() {
        this.FilterLazily();
        return this.refineries.Select(a => a.GetInventory(0));
      }

      protected override void Filter() => this.refineries.RemoveAll(c => c.GetInventory(1) == null);
    }
  }
}
