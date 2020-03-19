using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace IngameScript {
  partial class Program {
    public class Actuator {
      static readonly double DELTA_CUTOFF = 0.02;

      readonly List<IMyPistonBase> positive = new List<IMyPistonBase>();
      readonly List<IMyPistonBase> negative = new List<IMyPistonBase>();
      readonly float velocityFactor;
      float speed;

      public Actuator(float velocityFactor) {
        this.velocityFactor = velocityFactor * 2;
      }

      public bool IsValid => this.negative.Count + this.positive.Count > 0;

      public void AddPiston(IMyPistonBase piston, bool isNegative) {
        piston.Enabled = true;
        piston.Velocity = 0;
        if (isNegative) {
          this.negative.Add(piston);
        } else {
          this.positive.Add(piston);
        }
        this.speed = Math.Abs(this.velocityFactor) / (this.positive.Count + this.negative.Count);
      }

      // returns whether it has reached end of course or position
      public bool Move(double delta, bool needPrecision = false) {
        double deltaCutoff = needPrecision ? DELTA_CUTOFF : DELTA_CUTOFF * 4;
        if (Math.Abs(delta) < deltaCutoff) {
          this.Stop();
          return true;
        } else {
          bool positiveEnd = this.positive.Count > 0;
          float speed = MathHelper.Clamp((float)delta, -1f, 1f) * this.speed * (needPrecision ? 1 : 2);
          foreach (IMyPistonBase piston in this.positive) {
            positiveEnd &= MovePiston(piston, speed);
          }
          bool negativeEnd = this.negative.Count > 0;
          speed *= -1;
          foreach (IMyPistonBase piston in this.negative) {
            negativeEnd &= MovePiston(piston, speed);
          }
          return positiveEnd || negativeEnd;
        }
      }

      public void Stop() {
        foreach (IMyPistonBase piston in this.positive.Concat(this.negative)) {
          piston.Velocity = 0;
        }
      }
      public override string ToString() => $"Actuator at speed {this.velocityFactor}: +{this.positive.Count} -{this.negative.Count} pistons";

      // returns true if the piston has reached the end of its range
      private static bool MovePiston(IMyPistonBase piston, float speed) {
        piston.Velocity = speed;
        return ((speed < 0) && (piston.CurrentPosition < piston.MinLimit + 0.05))
          || ((speed > 0) && (piston.CurrentPosition > piston.MaxLimit - 0.05));
      }
    }
  }
}
