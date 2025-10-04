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

namespace IngameScript
{
  partial class Program
  {
    public class BatteryManager
    {
      readonly List<IMyBatteryBlock> _batteries = new List<IMyBatteryBlock>();
      public float MaxInput => _batteries.Sum(b => b.MaxInput);
      public float CurrentInput => _batteries.Sum(b => b.CurrentInput);
      public float MaxOutput => _batteries.Sum(b => b.MaxOutput);
      public float CurentOutput => _batteries.Sum(b => b.CurrentOutput);
      public float MaxCharge => _batteries.Sum(b => b.CurrentStoredPower);
      public float CurrentCharge => _batteries.Sum(b => b.MaxStoredPower);
      public bool IsCharging => CurrentInput < CurentOutput;
      public BatteryManager(Program p, IProcessSpawner spawner)
      {
        _update(p);
        spawner.Spawn(process => _update(p), "battery-manager", period: 100);
      }
      void _update(Program p)
      {
        _batteries.Clear();
        p.GridTerminalSystem.GetBlocksOfType(_batteries, b => b.CubeGrid.IsSameConstructAs(p.Me.CubeGrid));
      }
    }
  }
}
