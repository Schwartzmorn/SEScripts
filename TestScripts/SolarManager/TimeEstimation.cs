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
    public class TimeEstimation {
      public bool Reliable = false;
      public double Length;
    }
    public partial class TimeStruct {
      // TODO format time, time until next state
      public class HH_MM {
        public int Hour;
        public int Minute;
        public override String ToString() => Hour.ToString("D2") + ":" + Minute.ToString("D2");
      }
      public HH_MM GetTime() => GetHHMMFromTime(Time);
      public HH_MM GetDayLength() => GetHHMMFromMs(DayLength);
      public TimeEstimation Time;
      public TimeEstimation DayLength;
      public TimeEstimation DuskLength;
      public TimeEstimation NightLength;
      public TimeEstimation DawnLength;

      static HH_MM GetHHMMFromTime(TimeEstimation time) {
        int hour = (int)Math.Floor(time.Length);
        int minute = (int)Math.Floor((time.Length - hour) * 60);
        return new HH_MM {
          Hour = hour,
          Minute = minute
        };
      }
      static HH_MM GetHHMMFromMs(TimeEstimation time) {
        int hour = (int)Math.Floor(time.Length / MsInHour);
        int minute = (int)Math.Floor((time.Length - (hour * MsInHour)) / MsInMinute);
        return new HH_MM {
          Hour = hour,
          Minute = minute
        };
      }
      static int MsInHour = 3600000;
      static int MsInMinute = 60000;
    }
  }
}
