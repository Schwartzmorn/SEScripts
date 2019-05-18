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
    public class ContainerManager {
      public ContainerManager(IMyGridTerminalSystem GTS, GridManager gridManager) {
        List<IMyCargoContainer> containers = new List<IMyCargoContainer>();
        GTS.GetBlocksOfType(containers, cont => cont.GetInventory() != null && gridManager.Manages(cont.CubeGrid));
        _containers = containers.ConvertAll(container => new Container(container));
      }

      public List<Container> GetSortedContainers(IMyInventoryItem item) {
        _containers.Sort((a, b) => Container.CompareTo(a, b, item));
        return _containers;
      }

      List<Container> _containers;
    }

  }
}
