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
      enum TransitionTypeEnum { IdlingToChasing, ChasingToIdling, DayToDusk, DuskToNight, NightToDawn, DawnToDay, Irrelevant, None }
      public void Tick(SolarState state, float solarRotorPosition, double elapsedTime) {
        if (elapsedTime < 4000 && _lastState != null) {
          _timestamp += elapsedTime;
          Transition transition = GetTransition(state, solarRotorPosition);
          UpdateHistory(transition);
        }
        _lastState = state;
      }
      public TimeStruct GetTime() {
        TimeEstimation dayLength = EstimateDayLength();
        return new TimeStruct {
          Time = EstimateTimeOfDay(dayLength),
          DayLength = dayLength,
          DuskLength = EstimateLength(_historicalData.DuskLengthEstimations, dayLength.Length / 8),
          NightLength = EstimateLength(_historicalData.NightLengthEstimations, dayLength.Length / 4),
          DawnLength = EstimateLength(_historicalData.DawnLengthEstimations, dayLength.Length / 8)
        };
      }
      public void Serialize(Serializer serializer) {
        serializer.Serialize("dayTrackerTimestamp", _timestamp);
        if (_lastRelevantTransition != null) {
          _lastRelevantTransition.Serialize(serializer);
        }
        _historicalData.Serialize(serializer);
      }
      public void Deserialize(Deserializer deserializer) {
        _timestamp = deserializer.GetAsDouble("dayTrackerTimestamp");
        Transition deserializedTransition = new Transition();
        deserializedTransition.Deserialize(deserializer);
        if (deserializedTransition.Type != TransitionTypeEnum.Irrelevant) {
          _lastRelevantTransition = deserializedTransition;
        }
        _historicalData.Deserialize(deserializer);
      }
      private TimeEstimation EstimateDayLength() {
        TimeEstimation historicalWholeDay, wholeDay, historicalDayTime, dayTime;
        if (_historicalData.WholeDayLengthEstimations.Count() > 0) {
          historicalWholeDay = new TimeEstimation {
            Length = _historicalData.WholeDayLengthEstimations.Average(),
            Reliable = true
          };
        } else {
          historicalWholeDay = new TimeEstimation();
        }
        wholeDay = EstimateDayLengthFromWholeDay();
        if (_historicalData.DayLengthEstimations.Count() > 0) {
          historicalDayTime = new TimeEstimation {
            Length = _historicalData.DayLengthEstimations.Average(),
            Reliable = true
          };
        } else {
          historicalDayTime = new TimeEstimation();
        }
        dayTime = EstimateDayLengthFromDaytime();
        if (historicalWholeDay.Length > 0) {
          return historicalWholeDay;
        } else if (wholeDay.Length > 0) {
          return wholeDay;
        } else if (historicalDayTime.Length > 0) {
          return historicalDayTime;
        } else if (dayTime.Length > 0) {
          return dayTime;
        }
        return new TimeEstimation() {
          Length = 1000000
        };
      }
      private TimeEstimation EstimateLength(List<double> historicalLength, double defaultLength) {
        if (historicalLength.Count() > 0) {
          return new TimeEstimation {
            Length = historicalLength.Average(),
            Reliable = true
          };
        } else {
          return new TimeEstimation {
            Length = defaultLength,
            Reliable = true
          };
        }
      }
      private Transition GetTransition(SolarState newState, float solarRotorPosition) => new Transition {
        Type = GetTransitionType(newState),
        Timestamp = _timestamp,
        EstimatedTime = EstimateTimeFromPosition(solarRotorPosition)
      };
      private TransitionTypeEnum GetTransitionType(SolarState newState) {
        if (_lastState == null) {
          return TransitionTypeEnum.Irrelevant;
        } else if (_lastState.GetName() == newState.GetName()) {
          // apparently can't check types directly
          return TransitionTypeEnum.None;
        } else if (_lastState is SolarStateIdlingDay && newState is SolarStateChasing) {
          return TransitionTypeEnum.IdlingToChasing;
        } else if (_lastState is SolarStateChasing && newState is SolarStateIdlingDay) {
          return TransitionTypeEnum.ChasingToIdling;
        } else if (_lastState is SolarStateChasing && newState is SolarStateIdlingDusk) {
          return TransitionTypeEnum.DayToDusk;
        } else if (_lastState is SolarStateIdlingDusk && newState is SolarStateReseting) {
          return TransitionTypeEnum.DuskToNight;
        } else if (_lastState is SolarStateIdlingNight && newState is SolarStateIdlingDawn) {
          return TransitionTypeEnum.NightToDawn;
        } else if (_lastState is SolarStateIdlingDawn && newState is SolarStateChasing) {
          return TransitionTypeEnum.DawnToDay;
        } else {
          return TransitionTypeEnum.Irrelevant;
        }
      }
      private void UpdateHistory(Transition transition) {
        switch (transition.Type) {
          case TransitionTypeEnum.None:
          case TransitionTypeEnum.Irrelevant:
            break;
          case TransitionTypeEnum.IdlingToChasing:
            Compile(_historicalData.DawnStart, transition, _historicalData.DawnLengthEstimations);
            _historicalData.DawnStart = null;
            if (_historicalData.Daytime.FirstTransition != null) {
              if (_historicalData.Daytime.FirstTransition.Type == TransitionTypeEnum.ChasingToIdling) {
                _historicalData.Daytime.FirstTransition = transition;
                _historicalData.Daytime.LastTransition = null;
              } else {
                _historicalData.Daytime.LastTransition = transition;
              }
            }
            HandleWholeDayPeriod(transition);
            break;
          case TransitionTypeEnum.ChasingToIdling:
            Compile(_historicalData.DawnStart, transition, _historicalData.DawnLengthEstimations);
            _historicalData.DawnStart = null;
            if (_historicalData.Daytime.FirstTransition == null) {
              _historicalData.Daytime.FirstTransition = transition;
              _historicalData.Daytime.LastTransition = null;
            }
            break;
          case TransitionTypeEnum.DayToDusk:
            CompileDaytime(_historicalData.Daytime, _historicalData.DayLengthEstimations);
            CompileWholeDay(_historicalData.WholeDay, _historicalData.WholeDayLengthEstimations);
            _historicalData.DuskStart = transition;
            break;
          case TransitionTypeEnum.DuskToNight:
            Compile(_historicalData.DuskStart, transition, _historicalData.DuskLengthEstimations);
            _historicalData.DuskStart = null;
            _historicalData.NightStart = transition;
            break;
          case TransitionTypeEnum.NightToDawn:
            Compile(_historicalData.NightStart, transition, _historicalData.NightLengthEstimations);
            _historicalData.NightStart = null;
            _historicalData.DawnStart = transition;
            break;
          case TransitionTypeEnum.DawnToDay:
            Compile(_historicalData.DawnStart, transition, _historicalData.DawnLengthEstimations);
            _historicalData.DawnStart = null;
            break;
        }
      }
      private void Compile(Transition firstTransition, Transition lastTransition, List<double> estimations) {
        if (firstTransition != null && lastTransition != null) {
          double length = lastTransition.Timestamp - firstTransition.Timestamp;
          if (length > 0) {
            estimations.Add(length);
          }
        }
      }
      private void CompileDaytime(PeriodHolder holder, List<double> estimations) {
        TimeEstimation estimation = EstimateDayLengthFromDaytime();
        if (estimation.Reliable) {
          estimations.Add(estimation.Length);
        }
        holder.Reset();
      }
      private TimeEstimation EstimateDayLengthFromDaytime() {
        PeriodHolder holder = _historicalData.Daytime;
        if (holder.FirstTransition != null && holder.LastTransition != null) {
          double length = holder.LastTransition.Timestamp - holder.FirstTransition.Timestamp;
          double estimatedTime = holder.LastTransition.EstimatedTime - holder.FirstTransition.EstimatedTime;
          if (length > 0 && estimatedTime > 0.1) {
            // estimation of the day length
            return new TimeEstimation {
              Length = 24 * length / estimatedTime,
              Reliable = true
            };
          }
        }
        return new TimeEstimation();
      }
      private void HandleWholeDayPeriod(Transition transition) {
        _lastRelevantTransition = transition;
        PeriodHolder holder = _historicalData.WholeDay;
        if (holder.LastTransition == null) {
          holder.LastTransition = transition;
        } else {
          if (Math.Abs(transition.EstimatedTime - 12) < Math.Abs(holder.LastTransition.EstimatedTime - 12)) {
            holder.LastTransition = transition;
          }
        }
      }
      private void CompileWholeDay(PeriodHolder holder, List<double> estimations) {
        TimeEstimation estimation = EstimateDayLengthFromWholeDay();
        if (estimation.Reliable) {
          estimations.Add(estimation.Length);
        }
        holder.Reset(holder.LastTransition);
      }
      private TimeEstimation EstimateDayLengthFromWholeDay() {
        PeriodHolder holder = _historicalData.WholeDay;
        if (holder.FirstTransition != null && holder.FirstTransition.Type == TransitionTypeEnum.IdlingToChasing &&
            holder.LastTransition != null && holder.LastTransition.Type == TransitionTypeEnum.IdlingToChasing) {
          double estimatedTime = (24 - holder.FirstTransition.EstimatedTime) + holder.LastTransition.EstimatedTime;
          double length = holder.LastTransition.Timestamp - holder.FirstTransition.Timestamp;
          return new TimeEstimation {
            Reliable = true,
            Length = 24 * length / estimatedTime
          };
        }
        return new TimeEstimation();
      }
      private float EstimateTimeFromPosition(float solarRotorPosition) =>
        // Position is between 0 and PI radians, which in solar time correspond to 6:00 and 18:00
        6 + (12 * solarRotorPosition / (float)Math.PI);
      private TimeEstimation EstimateTimeOfDay(TimeEstimation dayLength) {
        if (_lastRelevantTransition !=  null && dayLength.Reliable) {
          double currentTime = _lastRelevantTransition.EstimatedTime +
            (24 * (_timestamp - _lastRelevantTransition.Timestamp) / dayLength.Length);
          return new TimeEstimation {
            Length = Mod(currentTime, 24),
            Reliable = true
          };
        } else if (_lastRelevantTransition != null &&
            (_lastState is SolarStateChasing || _lastState is SolarStateIdlingDay)) {
          return new TimeEstimation {
            Length = _lastRelevantTransition.EstimatedTime,
            Reliable = false
          };
        } else if (_lastState is SolarStateIdlingDusk) {
          return new TimeEstimation {
            Length = 20,
            Reliable = false
          };
        } else if (_lastState is SolarStateReseting) {
          return new TimeEstimation {
            Length = 22,
            Reliable = false
          };
        } else if (_lastState is SolarStateIdlingNight) {
          return new TimeEstimation {
            Length = 0,
            Reliable = false
          };
        } else if (_lastState is SolarStateIdlingDawn) {
          return new TimeEstimation {
            Length = 4,
            Reliable = false
          };
        } else {
          return new TimeEstimation {
            Length = 12,
            Reliable = false
          };
        }
      }
      class Transition {
        public override string ToString() => Type.ToString() + ": " + Timestamp.ToString("F0") + " @ " + EstimatedTime.ToString("F2");
        public override bool Equals(object obj) {
          if (obj == null) {
            return false;
          } else {
            return (obj is Transition) && ToString() == obj.ToString();
          }
        }
        public override int GetHashCode() => ToString().GetHashCode();
        public Transition() {
          Type = TransitionTypeEnum.Irrelevant;
        }
        public TransitionTypeEnum Type;
        public double Timestamp;
        public double EstimatedTime;
        public void Serialize(Serializer serializer) {
          serializer.Serialize("dayTrackerLastTransitionType", Type);
          serializer.Serialize("dayTrackerLastTransitionTimestamp", Timestamp);
          serializer.Serialize("dayTrackerLastTransitionEstimatedTime", EstimatedTime);
        }
        public void Deserialize(Deserializer deserializer) {
          Type = deserializer.GetAsEnum("dayTrackerLastTransitionType", TransitionTypeEnum.Irrelevant);
          Timestamp = deserializer.GetAsDouble("dayTrackerLastTransitionTimestamp");
          EstimatedTime = deserializer.GetAsDouble("dayTrackerLastTransitionEstimatedTime");
        }
      }
      class PeriodHolder {
        public Transition FirstTransition;
        public Transition LastTransition;
        public void Reset(Transition firstTransition = null) {
          FirstTransition = firstTransition;
          LastTransition = null;
        }
      }
      class HistoricalData {
        // Tries to hold longest uninterrupted day
        public PeriodHolder Daytime = new PeriodHolder();
        // Tries to hold two consecutive midday
        public PeriodHolder WholeDay = new PeriodHolder();
        // Tries to hold transitions of the beginning and end of day part 
        public Transition DawnStart;
        public Transition DuskStart;
        public Transition NightStart;
        // Holds the previous recorded informations
        public List<double> WholeDayLengthEstimations = new List<double>();
        public List<double> DayLengthEstimations = new List<double>();
        public List<double> DawnLengthEstimations = new List<double>();
        public List<double> NightLengthEstimations = new List<double>();
        public List<double> DuskLengthEstimations = new List<double>();
        public void Serialize(Serializer serializer) {
          SerializeList("dayTrackerWholeDays", FilterForSerialization(WholeDayLengthEstimations), serializer);
          SerializeList("dayTrackerDays", FilterForSerialization(DayLengthEstimations), serializer);
          SerializeList("dayTrackerDawns", FilterForSerialization(DawnLengthEstimations), serializer);
          SerializeList("dayTrackerNights", FilterForSerialization(NightLengthEstimations), serializer);
          SerializeList("dayTrackerDusks", FilterForSerialization(DuskLengthEstimations), serializer);
        }
        public void Deserialize(Deserializer deserializer) {
          WholeDayLengthEstimations = DeserializeList("dayTrackerWholeDays", deserializer);
          DayLengthEstimations = DeserializeList("dayTrackerDays", deserializer);
          DawnLengthEstimations = DeserializeList("dayTrackerDawns", deserializer);
          NightLengthEstimations = DeserializeList("dayTrackerNights", deserializer);
          DuskLengthEstimations = DeserializeList("dayTrackerDusks", deserializer);
        }
        public static void SerializeList(String key, List<double> list, Serializer serializer) {
          Marshaller marshaller = new Marshaller();
          marshaller.MarshallList(list);
          serializer.Serialize(key, marshaller.GetStorage());
        }
        public static List<double> DeserializeList(String key, Deserializer deserializer) {
          Unmarshaller unmarshaller = new Unmarshaller(deserializer.Get(key));
          return new List<double>(unmarshaller.UnmarshallArrayOfDouble());
        }
        static private List<double> FilterForSerialization(List<double> list) {
          if (list.Count <= 10) {
            return list;
          }
          double average = list.Average();
          double stdDev = Math.Pow(list.Average(val => Math.Pow(val - average, 2)), 0.5);
          double filter = Math.Max(2 * stdDev, 0.001 * Math.Abs(average));
          List<double> goodValues = list.FindAll(val => Math.Abs(val - average) < filter);
          return new List<double>(goodValues.Skip(Math.Max(goodValues.Count - 10, 0)));
        }
      }
      private Transition _lastRelevantTransition;
      private HistoricalData _historicalData = new HistoricalData();
      private double _timestamp;
      private SolarState _lastState = null;
    }
  }
}
