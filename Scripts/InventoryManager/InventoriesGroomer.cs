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
      int _outputCounter = 0;
      readonly List<IOutputInventoryCollection> _outputInventories;
      readonly Action<string> logger;

      public InventoriesGroomer(GridManager gridManager, ContainerManager contManager, AssemblerManager assemblerManager,
          MiscInventoryManager miscInventoryManager, RefineryManager refineryManager, IProcessSpawner spawner, Action<string> logger) {
        this.logger = logger;
        this._outputInventories = new List<IOutputInventoryCollection> { assemblerManager, miscInventoryManager, refineryManager };
        spawner.Spawn(p => this._groomContainers(contManager), "container-groomer", period: 100);
        spawner.Spawn(p => this.groomOutputInventories(gridManager, contManager), "producer-groomer", period: 47);
      }

      void groomOutputInventories(GridManager gridManager, ContainerManager contManager) {
        var collection = this._outputInventories[this._outputCounter];
        this.log($"Grooming containers {this._outputCounter + 1}/{this._outputInventories.Count}: {collection.Name}");

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

        ++this._outputCounter;
        if(this._outputCounter >= this._outputInventories.Count) {
          this._outputCounter = 0;
        }
        this.log("Done");
      }

      void _groomContainers(ContainerManager contManager) {
        this.log("Grooming containers...");
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
        this.log("Done");
      }
       void log(string s) => this.logger?.Invoke("IG: " + s);
    }
  }
}
