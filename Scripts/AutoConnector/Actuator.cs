using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace IngameScript {
  partial class Program {

    /// <summary>Wraps a <see cref="IMyPistonBase"/></summary>
    public class Piston {
      static readonly float OFFSET = 0.157f; // average offset introduced by a piston

      public float CurrentPosition => this.isNegative ? 10 - (OFFSET * 2) - this.piston.CurrentPosition : this.piston.CurrentPosition;
      public float Velocity => this.isNegative ? -this.piston.Velocity : this.piston.Velocity;

      readonly bool isNegative;
      readonly IMyPistonBase piston;

      public Piston(IMyPistonBase piston, bool isNegative) {
        piston.Enabled = true;
        piston.Velocity = 0;
        piston.MinLimit = 0;
        piston.MaxLimit = 10 - (OFFSET * 2);
        this.piston = piston;
        this.isNegative = isNegative;
      }

      public bool HasReachedEnd() => (this.piston.Velocity > 0 && this.piston.CurrentPosition > this.piston.MaxLimit - 0.05) ||
        (this.piston.Velocity < 0 && this.piston.CurrentPosition < this.piston.MinLimit + 0.05);

      public void Move(float speed, float averagePos) {
        // We want to make the pistons that lag behind behind and vice versa
        float deltaPos = averagePos - this.CurrentPosition;
        float speedMult = 1 + (MathHelper.Clamp(deltaPos, -0.5f, 0.5f) * Math.Sign(speed));
        float actualSpeed = speed * (this.isNegative ? -1 : 1);
        this.piston.Velocity = actualSpeed * speedMult;
      }

      public void Stop() => this.piston.Velocity = 0;
    }

    public class Actuator {
      static readonly double DELTA_CUTOFF = 0.02;

      readonly List<Piston> pistons = new List<Piston>();
      readonly float velocityFactor;
      float maxAverageSpeed;
      int PistonCount => this.pistons.Count;

      /// <summary>Creates a new Actuator which is a collection of piston working in parallel on a single axis</summary>
      /// <param name="velocityFactor">Speed will be multiplied by this factor</param>
      public Actuator(float velocityFactor) {
        this.velocityFactor = Math.Abs(velocityFactor);
      }

      /// <summary>Returns true if there is at least one piston</summary>
      public bool IsValid => this.PistonCount > 0;

      public void AddPiston(IMyPistonBase piston, bool isNegative) {
        this.pistons.Add(new Piston(piston, isNegative));
        this.maxAverageSpeed = this.velocityFactor / this.PistonCount;
      }

      // returns whether it has reached end of course or position
      public bool Move(double delta, bool needPrecision = false) {
        double deltaCutoff = needPrecision ? DELTA_CUTOFF : DELTA_CUTOFF * 4;
        if (Math.Abs(delta) < deltaCutoff) {
          this.Stop();
          return true;
        } else {
          float speed = this.getNextSpeed(delta, needPrecision);
          float averagePos = this.pistons.Average(p => p.CurrentPosition);
          foreach (Piston piston in this.pistons) {
            piston.Move(speed, averagePos);
          }
          return this.pistons.All(p => p.HasReachedEnd());
        }
      }

      public void Stop() {
        foreach (Piston piston in this.pistons) {
          piston.Stop();
        }
      }
      public override string ToString() => $"Actuator at speed {this.velocityFactor}: {this.PistonCount} pistons";

      float getNextSpeed(double delta, bool needPrecision) {
        // TODO take into account the rotor speed
        return MathHelper.Clamp((float)delta, -1f, 1f) * this.maxAverageSpeed * (needPrecision ? 1 : 2);
        //float prev = this.pistons.Average(p => p.Velocity);
        //return MathHelper.Clamp(targetSpeed, prev - 0.1f, prev + 0.1f);
      }
    }
  }
}
