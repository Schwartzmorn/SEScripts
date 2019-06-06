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
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
  partial class Program {
    public class Actuator {
      private static readonly double DELTA_CUTOFF = 0.02;

      private readonly List<IMyPistonBase> _positive = new List<IMyPistonBase>();
      private readonly List<IMyPistonBase> _negative = new List<IMyPistonBase>();
      private readonly float _velocityFactor;
      private float _speed;

      public Actuator(float velocityFactor) {
        _velocityFactor = velocityFactor * 2;
      }

      public bool IsValid => _negative.Count + _positive.Count > 0;

      public void AddPiston(IMyPistonBase piston, bool isNegative) {
        piston.Enabled = true;
        piston.Velocity = 0;
        if (isNegative) {
          _negative.Add(piston);
        } else {
          _positive.Add(piston);
        }
        _speed = Math.Abs(_velocityFactor) / (_positive.Count + _negative.Count);
      }

      // returns whether it has reached end of course or position
      public bool Move(double delta, bool needPrecision = false) {
        double deltaCutoff = needPrecision ? DELTA_CUTOFF : DELTA_CUTOFF * 4;
        if (Math.Abs(delta) < deltaCutoff) {
          Stop();
          return true;
        } else {
          bool positiveEnd = _positive.Count > 0;
          float speed = MathHelper.Clamp((float)delta, -1f, 1f) * _speed * (needPrecision ? 1 : 2);
          foreach (IMyPistonBase piston in _positive) {
            positiveEnd &= MovePiston(piston, speed);
          }
          bool negativeEnd = _negative.Count > 0;
          speed *= -1;
          foreach (IMyPistonBase piston in _negative) {
            negativeEnd &= MovePiston(piston, speed);
          }
          return positiveEnd || negativeEnd;
        }
      }

      public void Stop() {
        foreach (IMyPistonBase piston in _positive.Concat(_negative)) {
          piston.Velocity = 0;
        }
      }
      public override string ToString() => $"Actuator at speed {_velocityFactor}: +{_positive.Count} -{_negative.Count} pistons";

      // returns true if the piston has reached the end of its range
      private static bool MovePiston(IMyPistonBase piston, float speed) {
        piston.Velocity = speed;
        return ((speed < 0) && (piston.CurrentPosition < piston.MinLimit + 0.05))
          || ((speed > 0) && (piston.CurrentPosition > piston.MaxLimit - 0.05));
      }
    }
  }
}
