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
    public partial class TimeStruct {
      public class TestTimeStruct : TestSuite.Test {
        public override void DoTests() {
          TimeStruct time = new TimeStruct {
            Time = new TimeEstimation {
              Length = 5.5
            }
          };
          AssertEqual(time.GetTime().ToString(), "05:30", "Wrong formatting of time");
        }
      }
    }
  }
}
