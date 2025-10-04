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
    public class InventoriesController {
      readonly IMyCockpit _cpit;
      readonly double _idealCoM;
      readonly List<Inventory> _invs = new List<Inventory>();
      readonly List<IMyReflectorLight> _lights = new List<IMyReflectorLight>();
      readonly Process _invAction;

      public float LoadFactor => (float)this._invs.Sum(i => i.Inv.CurrentVolume.ToIntSafe()) / this._invs.Sum(i => i.Inv.MaxVolume.ToIntSafe());

      public InventoriesController(CoordinatesTransformer tformer, IMyGridTerminalSystem gts, IMyCockpit cockpit, double idealCenterOfMass, IProcessSpawner spawner) {
        this._cpit = cockpit;
        this._idealCoM = idealCenterOfMass;
        var containers = new List<IMyCargoContainer>();
        gts.GetBlocksOfType(containers, c => c.CubeGrid == cockpit.CubeGrid);
        this._invs = containers
          .Select(c => new Inventory(c, tformer.Pos(c.GetPosition()).Z))
          .OrderBy(inv => inv.Z)
          .ToList();
        gts.GetBlocksOfType(this._lights, light => light.CustomName.StartsWith("BM Spotlight")
            && !light.CustomName.Contains("Rear"));
        spawner.Spawn(p => this.updateDrills(), "drill-updater");

        this._invAction = spawner.Spawn(p => this.updateInventories(tformer.Pos(cockpit.CenterOfMass).Z), "inv-handle", period: 100);
      }

      void updateInventories(double centerOfMass) {
        bool updating = false;
        double delta = this._idealCoM - centerOfMass;
        if(Math.Abs(delta) > 0.2) {
          updating = this.moveCargo(delta > 0);
        }
        this._invAction.Period = updating ? 1 : 100;
      }

      void updateDrills() {
        if(this.LoadFactor > 0.95) {
          this._lights.ForEach(l => l.Color = Color.Red);
        } else {
          this._lights.ForEach(l => l.Color = Color.White);
        }
      }

      bool moveCargo(bool frontToRear) {
        MyFixedPoint remaining = 200;
        int fromIdx = 0, toIdx = this._invs.Count - 1;
        while(fromIdx < toIdx) {
          IMyInventory from = this._invs[frontToRear ? fromIdx : this._invs.Count - 1 - fromIdx].Inv;
          IMyInventory to = this._invs[frontToRear ? toIdx : this._invs.Count - 1 - toIdx].Inv;
          bool isToFull = false;
          var items = new List<MyInventoryItem>();
          from.GetItems(items);

          isToFull = true;
          foreach(MyInventoryItem item in items) {
            MyFixedPoint previousAmount = item.Amount;
            from.TransferItemTo(to, item, remaining);
            if(from.GetItemAt(fromIdx).HasValue) {
              MyFixedPoint amountTransfered = previousAmount - from.GetItemAt(fromIdx).Value.Amount;
              remaining -= amountTransfered;
              if(remaining > 1) {
                isToFull = true;
                break;
              }
            } else {
              remaining -= previousAmount;
            }

            if (remaining < 1) {
              break;
            }
          }
          if(isToFull) {
            --toIdx;
          } else if(remaining > 1) {
            ++fromIdx;
          } else {
            break;
          }
        }
        return remaining < 1;
      }
    }
  }
}
