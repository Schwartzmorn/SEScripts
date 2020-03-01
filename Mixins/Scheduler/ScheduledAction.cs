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
      public int Period;
      public readonly Action Action;
      public int Counter { get; private set; } = 0;
      public readonly string Name;
      public bool ToBeRemoved { get; private set; } = false;
      public bool Once { get; private set; }
      public ScheduledAction(Action action, int period = 1, bool useOnce = false, string name = "anonymous action") {
        Period = period;
        Once = useOnce;
        Action = action;
        Name = name;
      }
      public void Tick() {
        if (!ToBeRemoved && (++Counter >= Period)) {
          ToBeRemoved = Once;
          Action();
          Counter = 0;
        }
      }
      public void ResetCounter(int c = 0) => Counter = c;
      public void Dispose() => ToBeRemoved = true;
    }
  }
}
