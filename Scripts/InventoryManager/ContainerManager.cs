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
      private readonly List<Container> _containers = new List<Container>();

      private readonly List<IMyCargoContainer> _tmpList = new List<IMyCargoContainer>();

      public ContainerManager(IMyGridTerminalSystem gts, GridManager gridManager) {
        Schedule(new ScheduledAction(() => Scan(gts, gridManager), period: 100));
        Scan(gts, gridManager);
      }

      public void Scan(IMyGridTerminalSystem GTS, GridManager gridManager) {
        _containers.Clear();
        GTS.GetBlocksOfType(_tmpList, cont => cont.GetInventory() != null && gridManager.Manages(cont.CubeGrid));
        _containers.AddRange(_tmpList.Select(c => new Container(c)));
      }

      public List<Container> GetSortedContainers(MyInventoryItem item) {
        FilterLazily();
        _containers.Sort((a, b) => Container.CompareTo(a, b, item));
        return _containers;
      }

      public List<Container> GetContainers() {
        FilterLazily();
        return _containers.ToList();
      }

      protected override void Filter() => _containers.RemoveAll(c => c.GetInventory() == null);
    }

  }
}
