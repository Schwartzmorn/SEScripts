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
    /// <summary>Class that ensures a filter is only run once per tick.</summary>
    public abstract class LazyFilter {
      private int lastFilter = -1;

      protected void FilterLazily() {
        if (GLOBAL_COUNTER != this.lastFilter) {
          this.lastFilter = GLOBAL_COUNTER;
          this.Filter();
        }
      }

      abstract protected void Filter();
    }
  }
}
