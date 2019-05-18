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
    public partial class DayTracker {
      public class TestDayTracker : TestSuite.Test {
        public override void DoTests() {
          TestSerial1();
          TestSerial2();
          TestEstimateDayLength();
          TestEstimateTime();
          TestTracking();
        }
        private void TestSerial1() {
          Deserializer deserializer = new Deserializer("");
          DayTracker dayTracker = new DayTracker();
          dayTracker.Deserialize(deserializer);

          AssertEqual(dayTracker._timestamp, 0, "Wrong default timestamp");
          AssertEqual(dayTracker._lastRelevantTransition, null, "Wrong default last transition");
          AssertEqual(dayTracker._historicalData.WholeDayLengthEstimations.Count(), 0, "Wrong default whole day historical data length");
          AssertEqual(dayTracker._historicalData.DayLengthEstimations.Count(), 0, "Wrong default whole daytime historical data length");
          AssertEqual(dayTracker._historicalData.DuskLengthEstimations.Count(), 0, "Wrong default dusk historical data length");
          AssertEqual(dayTracker._historicalData.NightLengthEstimations.Count(), 0, "Wrong default night historical data length");
          AssertEqual(dayTracker._historicalData.DawnLengthEstimations.Count(), 0, "Wrong default dawn historical data length");
        }
        private void TestSerial2() {
          StringBuilder serialized = new StringBuilder();
          serialized.Append("dayTrackerTimestamp=5050000\n");
          serialized.Append("dayTrackerLastTransitionType=IdlingToChasing\n");
          serialized.Append("dayTrackerLastTransitionTimestamp=4000000\n");
          serialized.Append("dayTrackerLastTransitionEstimatedTime=12\n");
          serialized.Append("dayTrackerWholeDays=0;\n");
          serialized.Append("dayTrackerDays=1;3600000;\n");
          serialized.Append("dayTrackerDawns=2;100000;100000;\n");
          serialized.Append("dayTrackerNights=3;100000;100000;100000;\n");
          serialized.Append("dayTrackerDusks=4;100000;100000;100000;100000;\n");
          Deserializer deserializer = new Deserializer(serialized.ToString());
          DayTracker dayTracker = new DayTracker();
          dayTracker.Deserialize(deserializer);

          AssertEqual(dayTracker._timestamp, 5050000, "Wrong timestamp");
          AssertEqual(dayTracker._lastRelevantTransition.Type, TransitionTypeEnum.IdlingToChasing, "Wrong last transition type");
          AssertEqual(dayTracker._lastRelevantTransition.Timestamp, 4000000, "Wrong last transition timestamp");
          AssertEqual(dayTracker._lastRelevantTransition.EstimatedTime, 12, "Wrong last transition estimated time");
          AssertEqual(dayTracker._historicalData.WholeDayLengthEstimations.Count(), 0, "Wrong whole day historical data length");
          AssertEqual(dayTracker._historicalData.DayLengthEstimations.Count(), 1, "Wrong whole daytime historical data length");
          AssertEqual(dayTracker._historicalData.DayLengthEstimations[0], 3600000, "Wrong whole daytime historical data");
          AssertEqual(dayTracker._historicalData.DuskLengthEstimations.Count(), 4, "Wrong dusk historical data length");
          AssertEqual(dayTracker._historicalData.NightLengthEstimations.Count(), 3, "Wrong night historical data length");
          AssertEqual(dayTracker._historicalData.DawnLengthEstimations.Count(), 2, "Wrong dawn historical data length");

          Serializer serializer = new Serializer();
          dayTracker.Serialize(serializer);
          AssertEqual(serializer.GetSerializedString(), serialized.ToString(), "Wrong serialization of day tracker");
        }
        private void TestEstimateDayLength() {
          DayTracker dayTracker = new DayTracker();
          AssertEqual(dayTracker.EstimateDayLength().Length, 1000000, "Wrong default day length");
          dayTracker._historicalData.Daytime = new PeriodHolder {
            FirstTransition = new Transition {
              Type = TransitionTypeEnum.IdlingToChasing,
              Timestamp = 900,
              EstimatedTime = 4
            },
            LastTransition = new Transition {
              Type = TransitionTypeEnum.IdlingToChasing,
              Timestamp = 1000,
              EstimatedTime = 6
            }
          };
          AssertEqual(dayTracker.EstimateDayLength().Length, 1200, "Wrong day length estimation from daytime");
          dayTracker._historicalData.DayLengthEstimations = new List<double> { 100, 200, 300 };
          AssertEqual(dayTracker.EstimateDayLength().Length, 200, "Wrong day length estimation from historical daytimes");
          dayTracker._historicalData.WholeDay = new PeriodHolder {
            FirstTransition = new Transition {
              Type = TransitionTypeEnum.IdlingToChasing,
              Timestamp = 1000,
              EstimatedTime = 9
            },
            LastTransition = new Transition {
              Type = TransitionTypeEnum.IdlingToChasing,
              Timestamp = 4600,
              EstimatedTime = 21
            }
          };
          AssertEqual(dayTracker.EstimateDayLengthFromWholeDay().Length, 2400, "Wrong day length estimation from whole day");
          AssertEqual(dayTracker.EstimateDayLength().Length, 2400, "Wrong day length estimation from last whole day");
          dayTracker._historicalData.WholeDayLengthEstimations = new List<double> { 150, 250, 350 };
          AssertEqual(dayTracker.EstimateDayLength().Length, 250, "Wrong day length estimation from historical whole days");
        }
        private void TestEstimateTime() {
          DayTracker dayTracker = new DayTracker();
          AssertEqual(dayTracker.EstimateTimeOfDay(new TimeEstimation()).Length, 12, "Wrong default time estimation");
          dayTracker._lastRelevantTransition = new Transition {
            Type = TransitionTypeEnum.IdlingToChasing,
            Timestamp = 15000,
            EstimatedTime = 15
          };
          dayTracker._lastState = new SolarStateChasing();
          AssertEqual(dayTracker.EstimateTimeOfDay(new TimeEstimation()).Length, 15, "Wrong time esitmation during the day");
          dayTracker._timestamp = 18000;
          TimeEstimation time = new TimeEstimation {
            Length = 24000,
            Reliable = true
          };
          AssertEqual(dayTracker.EstimateTimeOfDay(time).Length, 18, "Wrong historical estimation");
          dayTracker._timestamp = 25000;
          AssertEqual(dayTracker.EstimateTimeOfDay(time).Length, 1, "Wrong historical estimation after midnight");
        }
        private void TestTracking() {
          DayTracker dayTracker = new DayTracker();
          // Night
          dayTracker.Tick(new SolarStateIdlingNight(), 0, 1000);
          AssertEqual(dayTracker._timestamp, 0, "DT Night 0: wrong timestamp");
          Assert(dayTracker._lastState is SolarStateIdlingNight, "DT Night 0: wrong last state");
          dayTracker.Tick(new SolarStateIdlingNight(), 0, 1000);
          AssertEqual(dayTracker._timestamp, 1000, "DT Night 1: wrong timestamp");
          AssertEqual(dayTracker._lastRelevantTransition, null, "DT Night 1: wrong last relevant transition");
          Assert(dayTracker._lastState is SolarStateIdlingNight, "DT Night 1: wrong last state");
          AssertEqual(dayTracker.GetTime().Time.Length, 0, "DT Night 1: wrong time");
          // Dawn
          dayTracker.Tick(new SolarStateIdlingDawn(), 0, 1000);
          AssertEqual(dayTracker._timestamp, 2000, "DT Dawn 1: wrong timestamp");
          AssertEqual(dayTracker._lastRelevantTransition, null, "DT Dawn 1: wrong last relevant transition");
          Assert(dayTracker._lastState is SolarStateIdlingDawn, "DT Dawn 1: wrong last state");
          AssertEqual(dayTracker.GetTime().Time.Length, 4, "DT Dawn 1: wrong time");
          // Day to dawn  1
          dayTracker.Tick(new SolarStateChasing(), 0, 1000);
          AssertEqual(dayTracker._timestamp, 3000, "DT Day to dawn 1: wrong timestamp");
          AssertEqual(dayTracker._lastRelevantTransition, null, "DT Day to dawn 1: wrong last relevant transition");
          Assert(dayTracker._lastState is SolarStateChasing, "DT Day to dawn 1: wrong last state");
          AssertEqual(dayTracker.GetTime().Time.Length, 12, "DT Day to dawn 1: wrong time");
          // Day - Part 1
          dayTracker.Tick(new SolarStateIdlingDay(), (float) Math.PI / 12, 1000);
          AssertEqual(dayTracker._timestamp, 4000, "DT Day 1: wrong timestamp");
          AssertEqual(dayTracker._lastRelevantTransition, null, "DT Day 1: wrong last relevant transition");
          Assert(dayTracker._lastState is SolarStateIdlingDay, "DT Day to dawn 1: wrong last state");
          AssertEqual(dayTracker.GetTime().Time.Length, 12, "DT Day 1: wrong time");
          // Day - Part 2
          dayTracker.Tick(new SolarStateChasing(), (float)Math.PI / 12, 1000);
          AssertEqual(dayTracker._timestamp, 5000, "DT Day 2: wrong timestamp");
          double time = dayTracker.GetTime().Time.Length;
          Assert(Math.Abs(time - 7) < 0.001f, "DT Day 2: wrong time");
          Transition expectedFirstTransition = new Transition {
            Type = TransitionTypeEnum.IdlingToChasing,
            Timestamp = 5000,
            EstimatedTime = time
          };
          AssertEqual(dayTracker._lastRelevantTransition, expectedFirstTransition, "DT Day 2: wrong first transition");
          Assert(dayTracker._lastState is SolarStateChasing, "DT Day 2: wrong last state");
          AssertEqual(dayTracker._historicalData.Daytime.FirstTransition, expectedFirstTransition, "DT Day 2: wrong daytime historical first transition");
          AssertEqual(dayTracker._historicalData.Daytime.LastTransition, null, "DT Day 2: wrong daytime historical last transition");
          AssertEqual(dayTracker._historicalData.WholeDay.FirstTransition, null, "DT Day 2: wrong whole day historical first transition");
          AssertEqual(dayTracker._historicalData.WholeDay.LastTransition, expectedFirstTransition, "DT Day 2: wrong whole day historical last transition");
          // Day - Part 3
          dayTracker.Tick(new SolarStateIdlingDay(), (float)Math.PI / 6, 1000);
          AssertEqual(dayTracker._timestamp, 6000, "DT Day 3: wrong timestamp");
          time = dayTracker.GetTime().Time.Length;
          Assert(Math.Abs(time - 7) < 0.001f, "DT Day 3: wrong time");
          AssertEqual(dayTracker._lastRelevantTransition, expectedFirstTransition, "DT Day 3: wrong last relevant transition");
          Assert(dayTracker._lastState is SolarStateIdlingDay, "DT Day 3: wrong last state");
          AssertEqual(dayTracker._historicalData.Daytime.FirstTransition, expectedFirstTransition, "DT Day 3: wrong daytime historical first transition");
          AssertEqual(dayTracker._historicalData.Daytime.LastTransition, null, "DT Day 3: wrong daytime historical last transition");
          AssertEqual(dayTracker._historicalData.WholeDay.FirstTransition, null, "DT Day 3: wrong whole day historical first transition");
          AssertEqual(dayTracker._historicalData.WholeDay.LastTransition, expectedFirstTransition, "DT Day 3: wrong whole day historical last transition");
          // Day - Part 4
          dayTracker.Tick(new SolarStateChasing(), (float)Math.PI / 6, 1000);
          AssertEqual(dayTracker._timestamp, 7000, "DT Day 4: wrong timestamp");
          time = dayTracker.GetTime().Time.Length;
          Assert(Math.Abs(time - 8) < 0.001f, "DT Day 4: wrong time");
          Transition expectedSecondTransition = new Transition {
            Type = TransitionTypeEnum.IdlingToChasing,
            Timestamp = 7000,
            EstimatedTime = time
          };
          AssertEqual(dayTracker._lastRelevantTransition, expectedSecondTransition, "DT Day 4: wrong last relevant transition");
          AssertEqual(dayTracker._historicalData.Daytime.FirstTransition, expectedFirstTransition, "DT Day 4: wrong daytime historical first transition");
          AssertEqual(dayTracker._historicalData.Daytime.LastTransition, expectedSecondTransition, "DT Day 4: wrong daytime historical last transition");
          AssertEqual(dayTracker._historicalData.WholeDay.FirstTransition, null, "DT Day 4: wrong whole day historical first transition");
          AssertEqual(dayTracker._historicalData.WholeDay.LastTransition, expectedSecondTransition, "DT Day 4: wrong whole day historical last transition");
          Assert(Math.Abs(dayTracker.GetTime().DayLength.Length - 48000) < 1, "DT Day 4: wrong day length: " + dayTracker.GetTime().DayLength.Length.ToString());
          // Day - Part 5
          dayTracker.Tick(new SolarStateIdlingDay(), (float)Math.PI / 2, 1000);
          AssertEqual(dayTracker._lastRelevantTransition, expectedSecondTransition, "DT Day 5: wrong last relevant transition 1");
          dayTracker.Tick(new SolarStateChasing(), (float)Math.PI / 2, 1000);
          AssertEqual(dayTracker._timestamp, 9000, "DT Day 5: wrong timestamp");
          Transition expectedThirdTransition = new Transition {
            Type = TransitionTypeEnum.IdlingToChasing,
            Timestamp = 9000,
            EstimatedTime = 12
          };
          AssertEqual(dayTracker._lastRelevantTransition, expectedThirdTransition, "DT Day 5: wrong last relevant transition 2");
          AssertEqual(dayTracker._historicalData.Daytime.FirstTransition, expectedFirstTransition, "DT Day 5: wrong daytime historical first transition");
          AssertEqual(dayTracker._historicalData.Daytime.LastTransition, expectedThirdTransition, "DT Day 5: wrong daytime historical last transition");
          AssertEqual(dayTracker._historicalData.WholeDay.FirstTransition, null, "DT Day 5: wrong whole day historical first transition");
          AssertEqual(dayTracker._historicalData.WholeDay.LastTransition, expectedThirdTransition, "DT Day 5: wrong whole day historical last transition");
          // Day - Part 6
          dayTracker.Tick(new SolarStateIdlingDay(), (float)Math.PI, 3500);
          dayTracker.Tick(new SolarStateChasing(), (float)Math.PI, 3500);
          Transition expectedFourthTransition = new Transition {
            Type = TransitionTypeEnum.IdlingToChasing,
            Timestamp = 16000,
            EstimatedTime = 18
          };
          AssertEqual(dayTracker._lastRelevantTransition, expectedFourthTransition, "DT Day 6: wrong last relevant transition 2");
          AssertEqual(dayTracker._historicalData.DayLengthEstimations.Count(), 0, "DT Day 6: wrong daytime historical");
          AssertEqual(dayTracker._historicalData.Daytime.FirstTransition, expectedFirstTransition, "DT Day 6: wrong daytime historical first transition");
          AssertEqual(dayTracker._historicalData.Daytime.LastTransition, expectedFourthTransition, "DT Day 6: wrong daytime historical last transition");
          AssertEqual(dayTracker._historicalData.WholeDay.FirstTransition, null, "DT Day 6: wrong whole day historical first transition");
          AssertEqual(dayTracker._historicalData.WholeDay.LastTransition, expectedThirdTransition, "DT Day 6: wrong whole day historical last transition");
          // Dusk - Part 1
          dayTracker.Tick(new SolarStateIdlingDusk(), (float)Math.PI, 1000);
          AssertEqual(dayTracker._timestamp, 17000, "DT Dusk 1: wrong timestamp");
          Transition expectedToDuskTransition = new Transition {
            Type = TransitionTypeEnum.DayToDusk,
            Timestamp = 17000,
            EstimatedTime = 18
          };
          AssertEqual(dayTracker._lastRelevantTransition, expectedFourthTransition, "DT Dusk 1: wrong last relevant transition 2");
          AssertEqual(dayTracker._historicalData.DayLengthEstimations.Count(), 1, "DT Dusk 1: wrong daytime historical number");
          AssertEqual(dayTracker._historicalData.DayLengthEstimations[0], 24000, "DT Dusk 1: wrong daytime historical");
          AssertEqual(dayTracker._historicalData.Daytime.FirstTransition, null, "DT Dusk 1: wrong daytime historical first transition");
          AssertEqual(dayTracker._historicalData.Daytime.LastTransition, null, "DT Dusk 1: wrong daytime historical last transition");
          AssertEqual(dayTracker._historicalData.WholeDay.FirstTransition, expectedThirdTransition, "DT Dusk 1: wrong whole day historical first transition");
          AssertEqual(dayTracker._historicalData.WholeDay.LastTransition, null, "DT Dusk 1: wrong whole day historical last transition");
          AssertEqual(dayTracker._historicalData.WholeDayLengthEstimations.Count(), 0, "DT Dusk 1: wrong whole day historical number");
          AssertEqual(dayTracker._historicalData.DuskStart, expectedToDuskTransition, "DT Dusk 1: wrong transition to dusk");
          // Night - Part 1
          dayTracker.Tick(new SolarStateReseting(), (float)Math.PI, 3000);
          time = dayTracker.GetTime().Time.Length;
          Assert(Math.Abs(time - 22) < 0.001f, "DT Night 1: wrong time: " + time.ToString());
          Transition expectedToNightTransition = new Transition {
            Type = TransitionTypeEnum.DuskToNight,
            Timestamp = 20000,
            EstimatedTime = 18
          };
          AssertEqual(dayTracker._historicalData.DuskStart, null, "DT Night 1: wrong transition to dusk");
          AssertEqual(dayTracker._historicalData.NightStart, expectedToNightTransition, "DT Night 1: wrong transition to night");
          AssertEqual(dayTracker._historicalData.DuskLengthEstimations.Count(), 1, "DT Night 1: wrong dusk historical number");
          AssertEqual(dayTracker._historicalData.DuskLengthEstimations[0], 3000, "DT Night 1: wrong dusk historical");
          // Night - Part 2
          dayTracker.Tick(new SolarStateIdlingNight(), 0, 3000);
          time = dayTracker.GetTime().Time.Length;
          Assert(Math.Abs(time - 1) < 0.001f, "DT Night 2: wrong time: " + time.ToString());
          // Dawn - Part 2
          dayTracker.Tick(new SolarStateIdlingDawn(), 0, 3000);
          time = dayTracker.GetTime().Time.Length;
          Assert(Math.Abs(time - 4) < 0.001f, "DT Dawn 2: wrong time: " + time.ToString());
          Transition expectedToDawnTransition = new Transition {
            Type = TransitionTypeEnum.NightToDawn,
            Timestamp = 26000,
            EstimatedTime = 6
          };
          AssertEqual(dayTracker._historicalData.NightStart, null, "DT Dawn 2: wrong transition to night");
          AssertEqual(dayTracker._historicalData.DawnStart, expectedToDawnTransition, "DT Dawn 2: wrong transition to dawn");
          AssertEqual(dayTracker._historicalData.NightLengthEstimations.Count(), 1, "DT Night 1: wrong night historical number");
          AssertEqual(dayTracker._historicalData.NightLengthEstimations[0], 6000, "DT Night 1: wrong night historical");
          // Day 2 - Part 1
          dayTracker.Tick(new SolarStateChasing(), 0, 3000);
          AssertEqual(dayTracker._historicalData.DawnStart, null, "DT Day 2 - 1: wrong transition to dawn");
          AssertEqual(dayTracker._historicalData.DawnLengthEstimations.Count(), 2, "DT Day 2 - 1: wrong dawn historical number");
          AssertEqual(dayTracker._historicalData.DawnLengthEstimations[0], 1000, "DT Day 2 - 1: wrong dawn historical 1");
          AssertEqual(dayTracker._historicalData.DawnLengthEstimations[1], 3000, "DT Day 2 - 1: wrong dawn historical 2");
          AssertEqual(dayTracker._historicalData.Daytime.FirstTransition, null, "DT Day 2 - 1: wrong daytime historical");
          // Day 2 - Part 2
          dayTracker.Tick(new SolarStateIdlingDay(), (float)Math.PI / 2, 2000);
          dayTracker.Tick(new SolarStateChasing(), (float)Math.PI / 2, 2000);
          Transition fourthTransition = new Transition {
            Type = TransitionTypeEnum.IdlingToChasing,
            Timestamp = 33000,
            EstimatedTime = 12
          };
          AssertEqual(dayTracker._historicalData.Daytime.FirstTransition, fourthTransition, "DT Day 2 - 1: wrong daytime historical first");
          AssertEqual(dayTracker._historicalData.Daytime.LastTransition, null, "DT Day 2 - 1: wrong daytime historical last");
          AssertEqual(dayTracker._historicalData.WholeDay.FirstTransition, expectedThirdTransition, "DT Day 2 - 1: wrong whole day historical first");
          AssertEqual(dayTracker._historicalData.WholeDay.LastTransition, fourthTransition, "DT Day 2 - 1: wrong whole day historical last");
          // Day 2 - Part 3
          dayTracker.Tick(new SolarStateIdlingDay(), (float)Math.PI, 1000);
          dayTracker.Tick(new SolarStateChasing(), (float)Math.PI, 1000);
          Transition fifthTransition = new Transition {
            Type = TransitionTypeEnum.IdlingToChasing,
            Timestamp = 35000,
            EstimatedTime = 18
          };
          AssertEqual(dayTracker._historicalData.Daytime.FirstTransition, fourthTransition, "DT Day 2 - 1: wrong daytime historical first");
          AssertEqual(dayTracker._historicalData.Daytime.LastTransition, fifthTransition, "DT Day 2 - 1: wrong daytime historical last");
          AssertEqual(dayTracker._historicalData.WholeDay.FirstTransition, expectedThirdTransition, "DT Day 2 - 1: wrong whole day historical first");
          AssertEqual(dayTracker._historicalData.WholeDay.LastTransition, fourthTransition, "DT Day 2 - 1: wrong whole day historical last");
          // Dusk 2
          dayTracker.Tick(new SolarStateIdlingDusk(), (float)Math.PI, 1000);
          AssertEqual(dayTracker._historicalData.DayLengthEstimations.Count(), 2, "DT Dusk 2: wrong daytime historical");
          AssertEqual(dayTracker._historicalData.DayLengthEstimations[0], 24000, "DT Dusk 2: wrong daytime historical 1");
          AssertEqual(dayTracker._historicalData.DayLengthEstimations[1], 8000, "DT Dusk 2: wrong daytime historical 2");
          AssertEqual(dayTracker._historicalData.WholeDayLengthEstimations.Count(), 1, "DT Dusk 2: wrong whole day historical number");
          AssertEqual(dayTracker._historicalData.WholeDayLengthEstimations[0], 24000, "DT Dusk 2: wrong whole day historical");
          AssertEqual(dayTracker.GetTime().DayLength.Length, 24000, "DT Dusk 2: wrong day length estimation");
          AssertEqual(dayTracker._historicalData.Daytime.FirstTransition, null, "DT DT Dusk 2: wrong daytime historical first");
          AssertEqual(dayTracker._historicalData.Daytime.LastTransition, null, "DT DT Dusk 2: wrong daytime historical last");
          AssertEqual(dayTracker._historicalData.WholeDay.FirstTransition, fourthTransition, "DT DT Dusk 2: wrong whole day historical first");
          AssertEqual(dayTracker._historicalData.WholeDay.LastTransition, null, "DT DT Dusk 2: wrong whole day historical last");
        }
      }
    }
  }
}
