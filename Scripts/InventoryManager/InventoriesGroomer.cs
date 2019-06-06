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
    public class InventoriesGroomer {
      private int _outputCounter = 0;
      private readonly List<IOutputInventoryCollection> _outputInventories;

      public InventoriesGroomer(GridManager gridManager, ContainerManager contManager, AssemblerManager assemblerManager,
          MiscInventoryManager miscInventoryManager, RefineryManager refineryManager) {
        _outputInventories = new List<IOutputInventoryCollection> { assemblerManager, miscInventoryManager, refineryManager };
        Schedule(new ScheduledAction(
            () => _groomContainers(contManager), period: 100));
        Schedule(new ScheduledAction(
            () => _groomOutputInventories(gridManager, contManager), period: 47));
      }

      private void _groomOutputInventories(GridManager gridManager, ContainerManager contManager) {
        var collection = _outputInventories[_outputCounter];

        foreach(var fromInv in collection.GetOutputInventories()) {
          int iFrom = fromInv.ItemCount - 1;
          while (iFrom >= 0) {
            MyInventoryItem item = fromInv.GetItemAt(iFrom).Value;
            int prevCount = fromInv.ItemCount;
            int prevFrom = iFrom;
            foreach(var toCont in contManager.GetSortedContainers(item)) {
              toCont.GetInventory().TransferItemFrom(fromInv, iFrom);
              if(prevCount != fromInv.ItemCount) {
                --iFrom;
                break;
              }
            }
            if (prevFrom == iFrom) {
              --iFrom;
            }
          }
        }

        ++_outputCounter;
        if(_outputCounter >= _outputInventories.Count) {
          _outputCounter = 0;
        }
      }

      private void _groomContainers(ContainerManager contManager) {
        foreach(var fromCont in contManager.GetContainers()) {
          int iFrom = fromCont.GetInventory().ItemCount - 1;
          while (iFrom >= 0) {
            MyInventoryItem item = fromCont.GetInventory().GetItemAt(iFrom).Value;
            int fromAff = fromCont.GetAffinity(item);
            int prevCount = fromCont.GetInventory().ItemCount;
            int prevFrom = iFrom;
            foreach(var toCont in contManager.GetSortedContainers(item)) {
              if (toCont.GetAffinity(item) > fromAff) {
                fromCont.GetInventory().TransferItemTo(toCont.GetInventory(), item);
                if (prevCount != fromCont.GetInventory().ItemCount) {
                  --iFrom;
                  break;
                }
              } else {
                --iFrom;
                break;
              }
            }
            if(prevFrom == iFrom) {
              --iFrom;
            }
          }
        }
      }
    }
  }
}
