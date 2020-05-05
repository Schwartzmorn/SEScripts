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
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript {
  partial class Program {
    public class BatteryManager {
      readonly List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
      public float MaxInput => this.batteries.Sum(b => b.MaxInput);
      public float CurrentInput => this.batteries.Sum(b => b.CurrentInput);
      public float MaxOutput => this.batteries.Sum(b => b.MaxOutput);
      public float CurentOutput => this.batteries.Sum(b => b.CurrentOutput);
      public float MaxCharge => this.batteries.Sum(b => b.CurrentStoredPower);
      public float CurrentCharge => this.batteries.Sum(b => b.MaxStoredPower);
      public bool IsCharging => this.CurrentInput < this.CurentOutput;
      public BatteryManager(Program p, IProcessSpawner spawner) {
        this.update(p);
        spawner.Spawn(process => this.update(p), "battery-manager", period: 100);
      }
      void update(Program p) {
        this.batteries.Clear();
        p.GridTerminalSystem.GetBlocksOfType(this.batteries, b => b.CubeGrid.IsSameConstructAs(p.Me.CubeGrid));
      }
    }
  }
}
