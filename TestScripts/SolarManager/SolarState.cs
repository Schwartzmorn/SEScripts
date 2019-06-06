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
    public abstract class SolarState {
      public static SolarState Transition(SolarState oldState, SolarState newState, SolarManager manager) {
        oldState.Stop(manager);
        newState.Start(manager);
        return newState;
      }
      public SolarState Handle(SolarManager manager) {
        SolarState next = Next(manager);
        if (next != null) {
          Transition(this, next, manager);
        } else {
          Act(manager);
          next = this;
        }
        return next;
      }
      public abstract SolarState Next(SolarManager manager);
      public virtual void Start(SolarManager manager) { }
      public virtual void Act(SolarManager manager) { }
      public virtual void Stop(SolarManager manager) { }
      public abstract string GetName();
      public abstract String Serialize();
      public static SolarState Deserialize(String name) {
        if (name == SolarStateIdlingDawn.Key) {
          return new SolarStateIdlingDawn();
        } else if (name == SolarStateChasing.Key) {
          return new SolarStateChasing();
        } else if (name == SolarStateIdlingDay.Key) {
          return new SolarStateIdlingDay();
        } else if (name == SolarStateIdlingDusk.Key) {
          return new SolarStateIdlingDusk();
        } else if (name == SolarStateReseting.Key) {
          return new SolarStateReseting();
        } else {
          return new SolarStateReseting();
        }
      }
    }
    public abstract class DelayedSolarState : SolarState {
      public DelayedSolarState(int maxDelay) {
        MaxDelay = maxDelay;
      }
      public override SolarState Next(SolarManager manager) {
        if (Delay >= MaxDelay) {
          return DelayedNext(manager);
        } else {
          if (manager.WasLastTickRelevant()) {
            ++Delay;
          }
          return null;
        }
      }
      protected abstract SolarState DelayedNext(SolarManager manager);
      private int Delay = 0;
      private int MaxDelay;
    }
    public class SolarStateIdlingDawn : DelayedSolarState {
      public SolarStateIdlingDawn(): base(4) {}
      protected override SolarState DelayedNext(SolarManager manager) {
        if (manager.HasOutput(DawnToDayCutoff) && manager.HasOutputDiminished()) {
          return new SolarStateChasing();
        }
        return null;
      }
      public override string GetName() => "idling at dawn";
      public override String Serialize() => Key;
      public static String Key = "IdlingDawn";
      private static float DawnToDayCutoff = 0.95f;
    }
    public class SolarStateChasing : DelayedSolarState {
      public SolarStateChasing() : base(3) { }
      protected override SolarState DelayedNext(SolarManager manager) {
        if (manager.HasReachedEnd(true)) {
          return new SolarStateIdlingDusk();
        } else if (manager.HasOutputDiminished() || iteration++ > 1) {
          return new SolarStateIdlingDay();
        }
        return null;
      }
      public override void Start(SolarManager manager) => manager.ChaseHorizontally();
      public override void Stop(SolarManager manager) => manager.StopChasing();
      public override string GetName() => "chasing the sun";
      public override String Serialize() => Key;
      public static String Key = "Chasing";
      private int iteration = 0;
    }
    public class SolarStateIdlingDay : DelayedSolarState {
      public SolarStateIdlingDay() : base(4) { }
      protected override SolarState DelayedNext(SolarManager manager) {
        if (manager.HasOutputDiminished()) {
          return new SolarStateChasing();
        }
        return null;
      }
      public override void Act(SolarManager manager) => manager.FixAlignment();
      public override void Stop(SolarManager manager) => manager.StopChasing();
      public override string GetName() => "idling at day";
      public override String Serialize() => Key;
      public static String Key = "Chasing";
    }
    public class SolarStateIdlingDusk : SolarState {
      public override SolarState Next(SolarManager manager) {
        if (!manager.HasOutput(DuskToNightCutoff)) {
          return new SolarStateReseting();
        }
        return null;
      }
      public override string GetName() => "idling at dusk";
      public override String Serialize() => Key;
      public static String Key = "IdlingDusk";
      private static float DuskToNightCutoff = 0.01f;
    }
    public class SolarStateReseting : SolarState {
      public override SolarState Next(SolarManager manager) {
        if (manager.HasReachedEnd(false)) {
          return new SolarStateIdlingNight();
        }
        return null;
      }
      public override void Start(SolarManager manager) => manager.Reset();
      public override void Act(SolarManager manager) => manager.Reset();
      public override void Stop(SolarManager manager) => manager.StopChasing();
      public override string GetName() => "reseting";
      public override String Serialize() => Key;
      public static String Key = "Reseting";
    }
    public class SolarStateIdlingNight : SolarState {
      public override SolarState Next(SolarManager manager) {
        if (manager.HasOutput(NightToDawnCutoff)) {
          return new SolarStateIdlingDawn();
        }
        return null;
      }
      public override string GetName() => "idling at night";
      public override String Serialize() => Key;
      public static String Key = "IdlingNight";
      private static float NightToDawnCutoff = 0.1f;
    }
  }
}
