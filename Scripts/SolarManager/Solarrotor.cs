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
    public struct SolarOutput {
      public readonly float Output;
      public readonly float MaxOutput;
      public readonly float Position;
      public readonly bool Direction;
      public SolarOutput(float output, float maxOutput, float position, bool direction) {
        this.Output = output;
        this.MaxOutput = maxOutput;
        this.Position = position;
        this.Direction = direction;
      }
      public SolarOutput(string s) {
        string[] vals = s.Split(IniHelper.SEP);
        float.TryParse(vals[0], out this.Output);
        float.TryParse(vals[1], out this.MaxOutput);
        float.TryParse(vals[2], out this.Position);
        bool.TryParse(vals[3], out this.Direction);
      }
      public override string ToString() => $"{this.Output},{this.MaxOutput},{this.Position},{this.Direction}";
    }
    public class SolarRotor {
      struct SolarId {
        public string Prefix;
        public int Number;
        public int AuxNumber;
      }
      public enum RotorState { Idle, TrackSunPrevDir, TrackSunPrevDirConfirmed, TrackSunRevDir, TrackSunRevDirConfirmed, TrackSunAux, ResetPrevious, GoIdle, AdjustAux, Adjust, NightIdle };


      const string SECTION = "solar-rotor";
      const float TORQUE = 10000000f;
      const float TRACKING_SPEED = 0.01f;
      const float RESET_SPEED = 0.05f;
      readonly static HashSet<RotorState> NIGHT_STATES = new HashSet<RotorState> { RotorState.GoIdle, RotorState.AdjustAux, RotorState.Adjust };

      readonly IMyMotorStator mainRotor;
      readonly Action<string> logger;
      readonly SolarPanel panel;
      readonly List<SolarRotor> rotors;

      float adjustTarget;
      int counter;
      bool firstLockOfTheDay;
      SolarId id;
      float maxObservedOutput;
      bool nightMode;
      SolarOutput previousLock;
      float previousOutput;
      float resetAngle;
      RotorState state;

      public float CurrentOutput => this.panel == null ? this.rotors.Sum(r => r.CurrentOutput) : this.panel.CurrentOutput;
      public float MaxOutput => this.panel == null ? this.rotors.Sum(r => r.MaxOutput) : this.panel.MaxOutput;
      public float MaxPossibleOutput => this.panel == null ? this.rotors.Sum(r => r.MaxPossibleOutput) : this.panel.MaxPossibleOutput;
      public int PanelCount => this.panel == null ? this.rotors.Count : 1;
      public bool IsTwoAxis => this.panel == null;
      public string Name => this.mainRotor.CustomName;
      public float Ratio => this.maxObservedOutput == 0 ? 0 : this.MaxOutput / this.maxObservedOutput;
      public int IDNumber {
        get {  return this.id.Number; }
        set {
          this.id.Number = value;
          this.updateNames();
        }
      }
      public RotorState State {
        get { return this.state; }
        set { this.enterState(value); }
      }

      public SolarRotor(string prefix, List<IMyMotorStator> rotors, SolarPanel panel, Action<string> logger) {
        this.logger = logger;
        this.id.Prefix = prefix;
        this.panel = panel;
        this.setupRotors(rotors, out this.mainRotor);
        this.updateNames();
      }

      public SolarRotor(string prefix, List<IMyMotorStator> rotors, List<SolarRotor> solarRotors, Action<string> logger) {
        this.logger = logger;
        this.id.Prefix = prefix;
        this.rotors = solarRotors;
        this.setupRotors(rotors, out this.mainRotor);
        this.updateNames();
      }

      /// <summary>
      /// Main loop
      /// </summary>
      /// <param name="nightMode"></param>
      public void Update(bool nightMode) => this.update(nightMode, false);
      /// <summary>
      /// Adjusts the position of the rotor (or one of the auxillary rotot)
      /// </summary>
      /// <param name="offset">offset in degree</param>
      /// <param name="auxId">if not 0, adjusts the position of an auxillary rotor instead</param>
      public void Adjust(float offset, int auxId = 0) {
        if (auxId == 0) {
          this.adjustTarget = this.mainRotor.Angle + MathHelper.ToRadians(offset);
          this.State = RotorState.Adjust;
        } else if (this.rotors != null) {
          foreach(SolarRotor rotor in this.rotors) {
            if (this.State != RotorState.Adjust) {
              this.State = RotorState.AdjustAux;
            }
            if (rotor.id.AuxNumber == auxId) {
              rotor.Adjust(offset);
            } else {
              rotor.State = this.ifNotNightMode();
            }
          }
        }
      }

      public void Track() {
        if (this.State == RotorState.Idle) {
          this.State = RotorState.TrackSunPrevDir;
        }
      }

      void update(bool nightMode, bool isAux) {
        this.nightMode = nightMode;
        this.State = this.getNextDayState(isAux);
        this.previousOutput = this.MaxOutput;
        if (this.previousOutput > this.maxObservedOutput) {
          this.maxObservedOutput = this.previousOutput;
        }
      }

      RotorState getNextDayState(bool isAux) {
        float currentOutput = this.MaxOutput;
        bool outputIncrease = this.previousOutput < currentOutput;
        bool outputDecrease = this.previousOutput > currentOutput;
        if (this.nightMode && !NIGHT_STATES.Contains(this.State)) {
          return RotorState.NightIdle;
        }
        switch (this.State) {
          case RotorState.Idle:
            return this.ifNotNightMode(isAux || outputDecrease ? RotorState.TrackSunPrevDir : this.State);
          case RotorState.TrackSunPrevDir:
            return outputDecrease
                ? this.delayState(RotorState.TrackSunRevDir)
                : (outputIncrease ? RotorState.TrackSunPrevDirConfirmed : this.State);
          case RotorState.TrackSunRevDir:
            return outputDecrease
                ? this.delayState(RotorState.ResetPrevious)
                : (outputIncrease ? RotorState.TrackSunRevDirConfirmed : this.State);
          case RotorState.TrackSunPrevDirConfirmed:
          case RotorState.TrackSunRevDirConfirmed:
            return outputDecrease || this.mainRotor.HasReachedEnd()
                ? this.delayState((this.rotors != null) ? RotorState.TrackSunAux : RotorState.GoIdle)
                : this.State;
          case RotorState.TrackSunAux:
            return this.rotors?.All(r => r.State == RotorState.Idle) ?? true ? RotorState.GoIdle : this.State;
          case RotorState.ResetPrevious:
            return this.mainRotor.HasReached(this.previousLock.Position)
                ? (this.rotors != null)
                    ? RotorState.TrackSunAux
                    : RotorState.Idle
                : this.State;
          case RotorState.GoIdle:
            return RotorState.Idle;
          case RotorState.Adjust:
            return this.mainRotor.HasReached(this.adjustTarget)
                ? this.rotors?.All(r => r.State == RotorState.Idle || r.State == RotorState.NightIdle) ?? true
                    ? this.ifNotNightMode()
                    : RotorState.AdjustAux
                : this.State;
          case RotorState.AdjustAux:
            return this.rotors?.All(r => r.State == RotorState.Idle) ?? true
                ? this.ifNotNightMode()
                : this.State;
          case RotorState.NightIdle:
            return this.nightMode ? this.State : RotorState.TrackSunPrevDir;
        }
        return this.State;
      }

      void enterState(RotorState state) {
        this.log($"{this.state} {state}");
        this.counter = this.State != state ? 0 : this.counter + 1;
        switch (state) {
          case RotorState.Idle:
            this.stop();
            foreach(SolarRotor r in this.rotors ?? Enumerable.Empty<SolarRotor>()) {
              r.State = RotorState.Idle;
            }
            break;
          case RotorState.TrackSunPrevDir:
          case RotorState.TrackSunPrevDirConfirmed:
          case RotorState.TrackSunRevDir:
          case RotorState.TrackSunRevDirConfirmed:
            this.rotate(this.previousLock.Direction ^ (this.State == RotorState.TrackSunRevDir || this.State == RotorState.TrackSunRevDirConfirmed));
            break;
          case RotorState.TrackSunAux:
            this.stop();
            if (this.State != RotorState.TrackSunAux) {
              this.updateAux();
            } else {
              this.updateAux(r => r.State != RotorState.Idle);
            }
            break;
          case RotorState.ResetPrevious:
            this.goTo(this.previousLock.Position);
            break;
          case RotorState.GoIdle:
            this.stop();
            this.previousLock = new SolarOutput(this.CurrentOutput, this.MaxOutput, this.mainRotor.Angle, this.mainRotor.AngleProxy(this.previousLock.Position) < 0);
            if (this.firstLockOfTheDay) {
              this.log("First lock of the day: saving position");
              this.firstLockOfTheDay = false;
              this.resetAngle = this.previousLock.Position;
            }
            break;
          case RotorState.Adjust:
            if (this.State == RotorState.NightIdle) {
              this.resetAngle = this.adjustTarget;
            } else if (this.State != RotorState.Adjust) {
              this.previousLock = new SolarOutput(this.previousLock.Output, this.previousLock.MaxOutput, this.adjustTarget, this.previousLock.Direction);
            }
            this.goTo(this.adjustTarget);
            break;
          case RotorState.AdjustAux:
            this.stop();
            this.updateAux(r => r.State == RotorState.Adjust);
            foreach (SolarRotor rotor in this.rotors.Where(r => r.State != RotorState.Adjust)) {
              rotor.State = this.ifNotNightMode();
            }
            break;
          case RotorState.NightIdle:
            this.firstLockOfTheDay = true;
            this.goTo(this.resetAngle);
            this.updateAux();
            break;
        }
        this.state = state;
      }

      RotorState delayState(RotorState state, int delay = 1) => this.counter > delay ? state : this.State;

      RotorState ifNotNightMode(RotorState state = RotorState.Idle) => this.nightMode ? RotorState.NightIdle : state;

      /// <summary>Ensure the rotor is at the given position</summary>
      /// <param name="angle">angle at which we want the rotor</param>
      void goTo(float angle) {
        if (this.mainRotor.HasReached(angle)) {
          this.stop();
        } else {
          this.mainRotor.Enabled = true;
          this.mainRotor.TargetVelocityRad = Math.Max(-RESET_SPEED, Math.Min(this.mainRotor.AngleProxy(angle) * 0.3f, RESET_SPEED));
        }
      }

      public void Save() {
        var ini = new MyIni();
        ini.Set(SECTION, "id-number", this.id.Number);
        ini.Set(SECTION, "id-aux-number", this.id.AuxNumber);
        ini.Set(SECTION, "reset-angle", this.resetAngle);
        ini.Set(SECTION, "state", this.State.ToString());
        ini.Set(SECTION, "previous-output", this.previousLock.ToString());
        if (this.State == RotorState.Adjust) {
          ini.Set(SECTION, "adjust-target", this.adjustTarget);
        }
        ini.Set(SECTION, "first-lock", this.firstLockOfTheDay);
        this.mainRotor.CustomData = ini.ToString();
        if (this.rotors != null) {
          foreach(SolarRotor rotor in this.rotors) {
            rotor.Save();
          }
        }
      }

      void stop() {
        this.mainRotor.Enabled = false;
        this.mainRotor.TargetVelocityRad = 0;
      }

      void rotate(bool dir) {
        this.mainRotor.Enabled = true;
        this.mainRotor.TargetVelocityRad = dir ? TRACKING_SPEED : -TRACKING_SPEED;
      }

      void load(string s) {
        var ini = new MyIni();
        if (ini.TryParse(s)) {
          this.id.Number = ini.Get(SECTION, "id-number").ToInt32();
          this.id.AuxNumber = ini.Get(SECTION, "id-aux-number").ToInt32();
          this.resetAngle = ini.Get(SECTION, "reset-angle").ToSingle(this.mainRotor.Angle);
          Enum.TryParse(ini.Get(SECTION, "state").ToString("Idle"), out this.state);
          this.previousLock = new SolarOutput(ini.Get(SECTION, "previous-output").ToString($"0,1,{this.mainRotor.Angle},true"));
          this.maxObservedOutput = ini.Get(SECTION, "max-observed-output").ToSingle(this.MaxOutput);
          if (this.State == RotorState.Adjust) {
            this.adjustTarget = ini.Get(SECTION, "adjust-target").ToSingle();
          }
          this.firstLockOfTheDay = ini.Get(SECTION, "first-lock").ToBoolean();
          this.updateNames();
        }
      }

      void updateNames() {
        if (this.IDNumber != 0) {
          if (this.rotors != null) {
            this.id.AuxNumber = 0;
            this.mainRotor.CustomName = $"{this.id.Prefix} {this.IDNumber}-Base";
            foreach (SolarRotor rotor in this.rotors) {
              rotor.id.Prefix = this.id.Prefix;
              rotor.id.Number = this.id.Number;
            }
            var usedIds = new HashSet<int>(this.rotors.Select(r => r.id.AuxNumber).Where(i => i != 0));
            int id = 1;
            foreach (SolarRotor rotor in this.rotors) {
              if (rotor.id.AuxNumber == 0) {
                while (usedIds.Contains(id)) {
                  ++id;
                }
                rotor.id.AuxNumber = id;
                usedIds.Add(id);
              }
              rotor.updateNames();
            }
          } else {
            this.mainRotor.CustomName = this.id.AuxNumber == 0
                ? $"{this.id.Prefix} {this.IDNumber}"
                : $"{this.id.Prefix} {this.IDNumber}-{this.id.AuxNumber}";
          }
        }
      }

      void setupRotors(List<IMyMotorStator> rotors, out IMyMotorStator rotor) {
        rotor = rotors.First();
        rotor = rotors.FirstOrDefault(r => r.CustomData.StartsWith($"[{SECTION}]")) ?? rotors.First();
        this.load(rotor.CustomData);
        rotor.BrakingTorque = TORQUE;
        rotor.Torque = TORQUE;
        rotor.Enabled = true;
        rotor.TargetVelocityRad = 0;
        foreach (IMyMotorStator r in rotors.Where(r => r != this.mainRotor)) {
          r.BrakingTorque = 0;
          r.Enabled = false;
          r.RotorLock = false;
          r.TargetVelocityRad = 0;
          r.LowerLimitDeg = float.MinValue;
          r.UpperLimitDeg = float.MaxValue;
        }
      }

      void updateAux(Func<SolarRotor, bool> filter = null) {
        if (this.rotors != null) {
          foreach(SolarRotor rotor in filter == null ? this.rotors : this.rotors.Where(filter)) {
            rotor.update(this.nightMode, true);
          }
        }
      }

      void log(string s) => this.logger?.Invoke($"{this.Name}: {s}");
    }
  }
}
