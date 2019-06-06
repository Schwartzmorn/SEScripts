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
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
  partial class Program {
    public class BatteryManager {
      public BatteryManager(Func<IMyBatteryBlock, bool> filter) {
        _filter = filter;
        UpdateFromGrid();
      }
      public void UpdateFromGrid() => GTS.GetBlocksOfType<IMyBatteryBlock>(_batteries, _filter);
      public float MaxInput {
        get {
          return _batteries.Sum(battery => battery == null ? 0 : battery.MaxInput);
        }
      }
      public float CurentInput {
        get {
          return _batteries.Sum(battery => battery == null ? 0 : battery.CurrentInput);
        }
      }
      public float MaxOutput {
        get {
          return _batteries.Sum(battery => battery == null ? 0 : battery.MaxOutput);
        }
      }
      public float CurentOutput {
        get {
          return _batteries.Sum(battery => battery == null ? 0 : battery.CurrentOutput);
        }
      }
      public float CurrentCharge {
        get {
          return _batteries.Sum(battery => battery == null ? 0 : battery.CurrentStoredPower);
        }
      }
      public float MaxCharge {
        get {
          return _batteries.Sum(battery => battery == null ? 0 : battery.MaxStoredPower);
        }
      }
      Func<IMyBatteryBlock, bool> _filter;
      List<IMyBatteryBlock> _batteries = new List<IMyBatteryBlock>();
    }
  }
}
