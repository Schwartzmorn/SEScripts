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
    public class ScheduledAction {
      private static readonly Random RAND = new Random();

      public int Period;
      public readonly Action Action;
      public int Counter { get; private set; } = 0;
      public readonly string Name;
      public bool ToBeRemoved { get; private set; }  = false;

      private readonly bool _useOnce;

      public ScheduledAction(Action action, int period = 1, bool useOnce = false, string name = "anonymous action") {
        Period = period;
        _useOnce = useOnce;
        Action = action;
        // to avoid having all the periodic actions scheduled at startup to be on the same cycles
        if(!useOnce && period > 1) {
          Counter = RAND.Next(period);
        }
        Name = name;
      }

      public void Tick() {
        if(!ToBeRemoved && (++Counter >= Period)) {
          Action();
          Counter = 0;
          ToBeRemoved |= _useOnce;
        }
      }

      public void ResetCounter() => Counter = 0;

      public void Dispose() => ToBeRemoved = true;
    }
  }
}
