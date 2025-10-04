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
    public struct SolarOutput
    {
      public readonly float Output;
      public readonly float MaxOutput;
      public readonly float Position;
      public readonly bool Direction;
      public SolarOutput(float output, float maxOutput, float position, bool direction)
      {
        Output = output;
        MaxOutput = maxOutput;
        Position = position;
        Direction = direction;
      }
      public SolarOutput(string s)
      {
        string[] vals = s.Split(IniHelper.SEP);
        float.TryParse(vals[0], out Output);
        float.TryParse(vals[1], out MaxOutput);
        float.TryParse(vals[2], out Position);
        bool.TryParse(vals[3], out Direction);
      }
      public override string ToString() => $"{Output},{MaxOutput},{Position},{Direction}";
    }
    public class SolarRotor
    {
      struct SolarId
      {
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

      readonly IMyMotorStator _mainRotor;
      readonly Action<string> _logger;
      readonly SolarPanel _panel;
      readonly List<SolarRotor> _rotors;

      float _adjustTarget;
      int _counter;
      bool _firstLockOfTheDay;
      SolarId _id;
      float _maxObservedOutput;
      bool _nightMode;
      SolarOutput _previousLock;
      float _previousOutput;
      float _resetAngle;
      RotorState _state;

      public float CurrentOutput => _panel == null ? _rotors.Sum(r => r.CurrentOutput) : _panel.CurrentOutput;
      public float MaxOutput => _panel == null ? _rotors.Sum(r => r.MaxOutput) : _panel.MaxOutput;
      public float MaxPossibleOutput => _panel == null ? _rotors.Sum(r => r.MaxPossibleOutput) : _panel.MaxPossibleOutput;
      public int PanelCount => _panel == null ? _rotors.Count : 1;
      public bool IsTwoAxis => _panel == null;
      public string Name => _mainRotor.CustomName;
      public float Ratio => _maxObservedOutput == 0 ? 0 : MaxOutput / _maxObservedOutput;
      public int IDNumber
      {
        get { return _id.Number; }
        set
        {
          _id.Number = value;
          _updateNames();
        }
      }
      public RotorState State
      {
        get { return _state; }
        set { _enterState(value); }
      }

      public SolarRotor(string prefix, List<IMyMotorStator> rotors, SolarPanel panel, Action<string> logger)
      {
        _logger = logger;
        _id.Prefix = prefix;
        _panel = panel;
        _setupRotors(rotors, out _mainRotor);
        _updateNames();
      }

      public SolarRotor(string prefix, List<IMyMotorStator> rotors, List<SolarRotor> solarRotors, Action<string> logger)
      {
        _logger = logger;
        _id.Prefix = prefix;
        _rotors = solarRotors;
        _setupRotors(rotors, out _mainRotor);
        _updateNames();
      }

      /// <summary>
      /// Main loop
      /// </summary>
      /// <param name="nightMode"></param>
      public void Update(bool nightMode) => _update(nightMode, false);
      /// <summary>
      /// Adjusts the position of the rotor (or one of the auxillary rotot)
      /// </summary>
      /// <param name="offset">offset in degree</param>
      /// <param name="auxId">if not 0, adjusts the position of an auxillary rotor instead</param>
      public void Adjust(float offset, int auxId = 0)
      {
        if (auxId == 0)
        {
          _adjustTarget = _mainRotor.Angle + MathHelper.ToRadians(offset);
          State = RotorState.Adjust;
        }
        else if (_rotors != null)
        {
          foreach (SolarRotor rotor in _rotors)
          {
            if (State != RotorState.Adjust)
            {
              State = RotorState.AdjustAux;
            }
            if (rotor._id.AuxNumber == auxId)
            {
              rotor.Adjust(offset);
            }
            else
            {
              rotor.State = _ifNotNightMode();
            }
          }
        }
      }

      public void Track()
      {
        if (State == RotorState.Idle)
        {
          State = RotorState.TrackSunPrevDir;
        }
      }

      void _update(bool nightMode, bool isAux)
      {
        _nightMode = nightMode;
        State = _getNextDayState(isAux);
        _previousOutput = MaxOutput;
        if (_previousOutput > _maxObservedOutput)
        {
          _maxObservedOutput = _previousOutput;
        }
      }

      RotorState _getNextDayState(bool isAux)
      {
        float currentOutput = MaxOutput;
        bool outputIncrease = _previousOutput < currentOutput;
        bool outputDecrease = _previousOutput > currentOutput;
        if (_nightMode && !NIGHT_STATES.Contains(State))
        {
          return RotorState.NightIdle;
        }
        switch (State)
        {
          case RotorState.Idle:
            return _ifNotNightMode(isAux || outputDecrease ? RotorState.TrackSunPrevDir : State);
          case RotorState.TrackSunPrevDir:
            return outputDecrease
                ? _delayState(RotorState.TrackSunRevDir)
                : (outputIncrease ? RotorState.TrackSunPrevDirConfirmed : State);
          case RotorState.TrackSunRevDir:
            return outputDecrease
                ? _delayState(RotorState.ResetPrevious)
                : (outputIncrease ? RotorState.TrackSunRevDirConfirmed : State);
          case RotorState.TrackSunPrevDirConfirmed:
          case RotorState.TrackSunRevDirConfirmed:
            return outputDecrease || _mainRotor.HasReachedEnd()
                ? _delayState((_rotors != null) ? RotorState.TrackSunAux : RotorState.GoIdle)
                : State;
          case RotorState.TrackSunAux:
            return _rotors?.All(r => r.State == RotorState.Idle) ?? true ? RotorState.GoIdle : State;
          case RotorState.ResetPrevious:
            return _mainRotor.HasReached(_previousLock.Position)
                ? (_rotors != null)
                    ? RotorState.TrackSunAux
                    : RotorState.Idle
                : State;
          case RotorState.GoIdle:
            return RotorState.Idle;
          case RotorState.Adjust:
            return _mainRotor.HasReached(_adjustTarget)
                ? _rotors?.All(r => r.State == RotorState.Idle || r.State == RotorState.NightIdle) ?? true
                    ? _ifNotNightMode()
                    : RotorState.AdjustAux
                : State;
          case RotorState.AdjustAux:
            return _rotors?.All(r => r.State == RotorState.Idle) ?? true
                ? _ifNotNightMode()
                : State;
          case RotorState.NightIdle:
            return _nightMode ? State : RotorState.TrackSunPrevDir;
        }
        return State;
      }

      void _enterState(RotorState state)
      {
        _log($"{_state} {state}");
        _counter = State != state ? 0 : _counter + 1;
        switch (state)
        {
          case RotorState.Idle:
            _stop();
            foreach (SolarRotor r in _rotors ?? Enumerable.Empty<SolarRotor>())
            {
              r.State = RotorState.Idle;
            }
            break;
          case RotorState.TrackSunPrevDir:
          case RotorState.TrackSunPrevDirConfirmed:
          case RotorState.TrackSunRevDir:
          case RotorState.TrackSunRevDirConfirmed:
            _rotate(_previousLock.Direction ^ (State == RotorState.TrackSunRevDir || State == RotorState.TrackSunRevDirConfirmed));
            break;
          case RotorState.TrackSunAux:
            _stop();
            if (State != RotorState.TrackSunAux)
            {
              _updateAux();
            }
            else
            {
              _updateAux(r => r.State != RotorState.Idle);
            }
            break;
          case RotorState.ResetPrevious:
            _goTo(_previousLock.Position);
            break;
          case RotorState.GoIdle:
            _stop();
            _previousLock = new SolarOutput(CurrentOutput, MaxOutput, _mainRotor.Angle, _mainRotor.AngleProxy(_previousLock.Position) < 0);
            if (_firstLockOfTheDay)
            {
              _log("First lock of the day: saving position");
              _firstLockOfTheDay = false;
              _resetAngle = _previousLock.Position;
            }
            break;
          case RotorState.Adjust:
            if (State == RotorState.NightIdle)
            {
              _resetAngle = _adjustTarget;
            }
            else if (State != RotorState.Adjust)
            {
              _previousLock = new SolarOutput(_previousLock.Output, _previousLock.MaxOutput, _adjustTarget, _previousLock.Direction);
            }
            _goTo(_adjustTarget);
            break;
          case RotorState.AdjustAux:
            _stop();
            _updateAux(r => r.State == RotorState.Adjust);
            foreach (SolarRotor rotor in _rotors.Where(r => r.State != RotorState.Adjust))
            {
              rotor.State = _ifNotNightMode();
            }
            break;
          case RotorState.NightIdle:
            _firstLockOfTheDay = true;
            _goTo(_resetAngle);
            _updateAux();
            break;
        }
        _state = state;
      }

      RotorState _delayState(RotorState state, int delay = 1) => _counter > delay ? state : State;

      RotorState _ifNotNightMode(RotorState state = RotorState.Idle) => _nightMode ? RotorState.NightIdle : state;

      /// <summary>Ensure the rotor is at the given position</summary>
      /// <param name="angle">angle at which we want the rotor</param>
      void _goTo(float angle)
      {
        if (_mainRotor.HasReached(angle))
        {
          _stop();
        }
        else
        {
          _mainRotor.Enabled = true;
          _mainRotor.TargetVelocityRad = Math.Max(-RESET_SPEED, Math.Min(_mainRotor.AngleProxy(angle) * 0.3f, RESET_SPEED));
        }
      }

      public void Save()
      {
        var ini = new MyIni();
        ini.Set(SECTION, "id-number", _id.Number);
        ini.Set(SECTION, "id-aux-number", _id.AuxNumber);
        ini.Set(SECTION, "reset-angle", _resetAngle);
        ini.Set(SECTION, "state", State.ToString());
        ini.Set(SECTION, "previous-output", _previousLock.ToString());
        if (State == RotorState.Adjust)
        {
          ini.Set(SECTION, "adjust-target", _adjustTarget);
        }
        ini.Set(SECTION, "first-lock", _firstLockOfTheDay);
        _mainRotor.CustomData = ini.ToString();
        if (_rotors != null)
        {
          foreach (SolarRotor rotor in _rotors)
          {
            rotor.Save();
          }
        }
      }

      void _stop()
      {
        _mainRotor.Enabled = false;
        _mainRotor.TargetVelocityRad = 0;
      }

      void _rotate(bool dir)
      {
        _mainRotor.Enabled = true;
        _mainRotor.TargetVelocityRad = dir ? TRACKING_SPEED : -TRACKING_SPEED;
      }

      void _load(string s)
      {
        var ini = new MyIni();
        if (ini.TryParse(s))
        {
          _id.Number = ini.Get(SECTION, "id-number").ToInt32();
          _id.AuxNumber = ini.Get(SECTION, "id-aux-number").ToInt32();
          _resetAngle = ini.Get(SECTION, "reset-angle").ToSingle(_mainRotor.Angle);
          Enum.TryParse(ini.Get(SECTION, "state").ToString("Idle"), out _state);
          _previousLock = new SolarOutput(ini.Get(SECTION, "previous-output").ToString($"0,1,{_mainRotor.Angle},true"));
          _maxObservedOutput = ini.Get(SECTION, "max-observed-output").ToSingle(MaxOutput);
          if (State == RotorState.Adjust)
          {
            _adjustTarget = ini.Get(SECTION, "adjust-target").ToSingle();
          }
          _firstLockOfTheDay = ini.Get(SECTION, "first-lock").ToBoolean();
          _updateNames();
        }
      }

      void _updateNames()
      {
        if (IDNumber != 0)
        {
          if (_rotors != null)
          {
            _id.AuxNumber = 0;
            _mainRotor.CustomName = $"{_id.Prefix} {IDNumber}-Base";
            foreach (SolarRotor rotor in _rotors)
            {
              rotor._id.Prefix = _id.Prefix;
              rotor._id.Number = _id.Number;
            }
            var usedIds = new HashSet<int>(_rotors.Select(r => r._id.AuxNumber).Where(i => i != 0));
            int id = 1;
            foreach (SolarRotor rotor in _rotors)
            {
              if (rotor._id.AuxNumber == 0)
              {
                while (usedIds.Contains(id))
                {
                  ++id;
                }
                rotor._id.AuxNumber = id;
                usedIds.Add(id);
              }
              rotor._updateNames();
            }
          }
          else
          {
            _mainRotor.CustomName = _id.AuxNumber == 0
                ? $"{_id.Prefix} {IDNumber}"
                : $"{_id.Prefix} {IDNumber}-{_id.AuxNumber}";
          }
        }
      }

      void _setupRotors(List<IMyMotorStator> rotors, out IMyMotorStator rotor)
      {
        rotor = rotors.First();
        rotor = rotors.FirstOrDefault(r => r.CustomData.StartsWith($"[{SECTION}]")) ?? rotors.First();
        _load(rotor.CustomData);
        rotor.BrakingTorque = TORQUE;
        rotor.Torque = TORQUE;
        rotor.Enabled = true;
        rotor.TargetVelocityRad = 0;
        foreach (IMyMotorStator r in rotors.Where(r => r != _mainRotor))
        {
          r.BrakingTorque = 0;
          r.Enabled = false;
          r.RotorLock = false;
          r.TargetVelocityRad = 0;
          r.LowerLimitDeg = float.MinValue;
          r.UpperLimitDeg = float.MaxValue;
        }
      }

      void _updateAux(Func<SolarRotor, bool> filter = null)
      {
        if (_rotors != null)
        {
          foreach (SolarRotor rotor in filter == null ? _rotors : _rotors.Where(filter))
          {
            rotor._update(_nightMode, true);
          }
        }
      }

      void _log(string s) => _logger?.Invoke($"{Name}: {s}");
    }
  }
}
