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
      private readonly IMyCockpit _cpit;
      private readonly double _idealCenterOfMass;
      private readonly List<Inventory> _invs = new List<Inventory>();
      private readonly List<IMyReflectorLight> _lights = new List<IMyReflectorLight>();
      private readonly ScheduledAction _invAction;

      public float LoadFactor {
        get {
          int current = _invs.Select(inv => inv.Inv.CurrentVolume.ToIntSafe()).Sum();
          int total = _invs.Select(inv => inv.Inv.MaxVolume.ToIntSafe()).Sum();
          return (float)current / total;
        }
      }

      public InventoriesController(CoordsTransformer tformer,
          IMyGridTerminalSystem gts, IMyCockpit cockpit, double idealCenterOfMass) {
        _cpit = cockpit;
        _idealCenterOfMass = idealCenterOfMass;
        var containers = new List<IMyCargoContainer>();
        gts.GetBlocksOfType(containers, c => c.CubeGrid == cockpit.CubeGrid);
        _invs = containers
          .Select(c => new Inventory(c, tformer.Pos(c.GetPosition()).Z))
          .OrderBy(inv => inv.Z)
          .ToList();
        int nWorking = containers.Where(c => c.IsWorking).Count();
        if(nWorking == _invs.Count) {
          Logger.Inst.Log($"Cargo... {nWorking}/{nWorking} OK");
        } else {
          Logger.Inst.Log($"Cargo... {_invs.Count - nWorking}/{_invs.Count} KO");
        }
        gts.GetBlocksOfType(_lights, light => light.DisplayNameText.StartsWith("BM Spotlight")
            && !light.DisplayNameText.Contains("Rear"));

        Scheduler.Inst.AddAction(_updateDrills);
        _invAction = new ScheduledAction(() => _updateInventories(tformer.Pos(cockpit.CenterOfMass).Z), 100);
        Scheduler.Inst.AddAction(_invAction);
      }

      private void _updateInventories(double centerOfMass) {
        bool updating = false;
        double delta = _idealCenterOfMass - centerOfMass;
        if(Math.Abs(delta) > 0.2) {
          updating = _moveCargo(delta > 0);
        }
        _invAction.Period = updating ? 1 : 100;
      }

      private void _updateDrills() {
        if(_isAlmostFull()) {
          foreach(var light in _lights) {
            light.Color = Color.Red;
          }
        } else {
          foreach(var light in _lights) {
            light.Color = Color.White;
          }
        }
      }

      private bool _isAlmostFull() => LoadFactor > 0.95;

      private bool _moveCargo(bool frontToRear) {
        VRage.MyFixedPoint remaining = 200;
        int fromIdx = 0, toIdx = _invs.Count - 1;
        while(fromIdx < toIdx) {
          IMyInventory from = _invs[frontToRear ? fromIdx : _invs.Count - 1 - fromIdx].Inv;
          IMyInventory to = _invs[frontToRear ? toIdx : _invs.Count - 1 - toIdx].Inv;
          bool isToFull = false;
          var items = new List<MyInventoryItem>();
          from.GetItems(items);

          isToFull = true;
          foreach(MyInventoryItem item in items) {
            VRage.MyFixedPoint previousAmount = item.Amount;
            from.TransferItemTo(to, item, remaining);
            if(from.GetItemAt(fromIdx).HasValue) {
              var amountTransfered = previousAmount - from.GetItemAt(fromIdx).Value.Amount;
              remaining -= amountTransfered;
              if(remaining > 1) {
                isToFull = true;
                break;
              }
            } else {
              remaining -= previousAmount;
            }
            if(remaining < 1) {
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
