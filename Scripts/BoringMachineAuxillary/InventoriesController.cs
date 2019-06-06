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
      readonly ScheduledAction _invAction;

      public float LoadFactor => (float)_invs.Sum(i => i.Inv.CurrentVolume.ToIntSafe()) / _invs.Sum(i => i.Inv.MaxVolume.ToIntSafe());

      public InventoriesController(CmdLine cmd, CoordsTransformer tformer,
          IMyGridTerminalSystem gts, IMyCockpit cockpit, double idealCenterOfMass) {
        _cpit = cockpit;
        _idealCoM = idealCenterOfMass;
        var containers = new List<IMyCargoContainer>();
        gts.GetBlocksOfType(containers, c => c.CubeGrid == cockpit.CubeGrid);
        _invs = containers
          .Select(c => new Inventory(c, tformer.Pos(c.GetPosition()).Z))
          .OrderBy(inv => inv.Z)
          .ToList();
        gts.GetBlocksOfType(_lights, light => light.DisplayNameText.StartsWith("BM Spotlight")
            && !light.DisplayNameText.Contains("Rear"));
        Schedule(_updateDrills);
        _invAction = new ScheduledAction(() => _updateInventories(tformer.Pos(cockpit.CenterOfMass).Z), 100, name: "inv-handle");
        Schedule(_invAction);
      }

      void _updateInventories(double centerOfMass) {
        bool updating = false;
        double delta = _idealCoM - centerOfMass;
        if(Math.Abs(delta) > 0.2)
          updating = _moveCargo(delta > 0);
        _invAction.Period = updating ? 1 : 100;
      }

      void _updateDrills() {
        if(LoadFactor > 0.95)
          _lights.ForEach(l => l.Color = Color.Red);
        else
          _lights.ForEach(l => l.Color = Color.White);
      }

      bool _moveCargo(bool frontToRear) {
        MyFixedPoint remaining = 200;
        int fromIdx = 0, toIdx = _invs.Count - 1;
        while(fromIdx < toIdx) {
          IMyInventory from = _invs[frontToRear ? fromIdx : _invs.Count - 1 - fromIdx].Inv;
          IMyInventory to = _invs[frontToRear ? toIdx : _invs.Count - 1 - toIdx].Inv;
          bool isToFull = false;
          var items = new List<MyInventoryItem>();
          from.GetItems(items);

          isToFull = true;
          foreach(var item in items) {
            var previousAmount = item.Amount;
            from.TransferItemTo(to, item, remaining);
            if(from.GetItemAt(fromIdx).HasValue) {
              var amountTransfered = previousAmount - from.GetItemAt(fromIdx).Value.Amount;
              remaining -= amountTransfered;
              if(remaining > 1) {
                isToFull = true;
                break;
              }
            } else
              remaining -= previousAmount;
            if(remaining < 1)
              break;
          }
          if(isToFull)
            --toIdx;
          else if(remaining > 1)
            ++fromIdx;
          else
            break;
        }
        return remaining < 1;
      }
    }
  }
}
