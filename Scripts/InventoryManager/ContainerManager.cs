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
    public class ContainerManager : LazyFilter {
      readonly List<Container> _containers = new List<Container>();

      readonly List<IMyCargoContainer> _tmpList = new List<IMyCargoContainer>();

      readonly Action<string> logger;

      public ContainerManager(IMyGridTerminalSystem gts, GridManager gridManager, IProcessSpawner spawner, Action<string> logger) {
        this.logger = logger;
        spawner.Spawn(p => this.Scan(gts, gridManager), "container-scanner", period: 100);
        this.Scan(gts, gridManager);
      }

      public void Scan(IMyGridTerminalSystem GTS, GridManager gridManager) {
        this.log($"Scanning... {gridManager ==  null}");
        this._containers.Clear();
        GTS.GetBlocksOfType(this._tmpList, cont => cont.GetInventory() != null && gridManager.Manages(cont.CubeGrid));
        this._containers.AddRange(this._tmpList.Select(c => new Container(c)));
        this.log($"Found {this._containers.Count} containers");
      }

      public List<Container> GetSortedContainers(MyInventoryItem item) {
        this.FilterLazily();
        this._containers.Sort((a, b) => Container.CompareTo(a, b, item));
        return this._containers;
      }

      public List<Container> GetContainers() {
        this.FilterLazily();
        return this._containers.ToList();
      }

      protected override void Filter() => this._containers.RemoveAll(c => c.GetInventory() == null);

      void log(string s) => this.logger?.Invoke("CM: " + s);
    }

  }
}
