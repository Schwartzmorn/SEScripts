using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace IngameScript
{
  partial class Program
  {

    /// <summary>Wraps a <see cref="IMyPistonBase"/></summary>
    public class Piston
    {
      static readonly float OFFSET = 0.15985f; // average offset introduced by a piston

      public float CurrentPosition => _isNegative ? 10 - (OFFSET * 2) - _piston.CurrentPosition : _piston.CurrentPosition;
      public float Velocity => _isNegative ? -_piston.Velocity : _piston.Velocity;

      readonly bool _isNegative;
      readonly IMyPistonBase _piston;

      public Piston(IMyPistonBase piston, bool isNegative)
      {
        piston.Enabled = true;
        piston.Velocity = 0;
        // We want to preserve manually set limits
        piston.MinLimit = Math.Max(0, piston.MinLimit);
        piston.MaxLimit = Math.Min(piston.MaxLimit, 10 - (OFFSET * 2));
        _piston = piston;
        _isNegative = isNegative;
      }

      public bool HasReachedEnd() => (_piston.Velocity > 0 && _piston.CurrentPosition > _piston.MaxLimit - 0.05) ||
        (_piston.Velocity < 0 && _piston.CurrentPosition < _piston.MinLimit + 0.05);

      public void Move(float speed, float averagePos)
      {
        // We want to make the pistons that lag behind behind and vice versa
        float deltaPos = averagePos - CurrentPosition;
        float speedMult = 1 + (MathHelper.Clamp(deltaPos, -0.5f, 0.5f) * Math.Sign(speed));
        float actualSpeed = speed * (_isNegative ? -1 : 1);
        _piston.Velocity = actualSpeed * speedMult;
      }

      public void Stop() => _piston.Velocity = 0;
    }

    public class Actuator
    {
      static readonly double DELTA_CUTOFF = 0.02;

      readonly List<Piston> _pistons = new List<Piston>();
      readonly float _velocityFactor;
      float _maxAverageSpeed;
      int PistonCount => _pistons.Count;

      /// <summary>Creates a new Actuator which is a collection of piston working in parallel on a single axis</summary>
      /// <param name="velocityFactor">Speed will be multiplied by this factor</param>
      public Actuator(float velocityFactor)
      {
        _velocityFactor = Math.Abs(velocityFactor);
      }

      /// <summary>Returns true if there is at least one piston</summary>
      public bool IsValid => PistonCount > 0;

      public void AddPiston(IMyPistonBase piston, bool isNegative)
      {
        _pistons.Add(new Piston(piston, isNegative));
        _maxAverageSpeed = _velocityFactor / PistonCount;
      }

      // returns whether it has reached end of course or position
      public bool Move(double delta, bool needPrecision = false)
      {
        double deltaCutoff = needPrecision ? DELTA_CUTOFF : DELTA_CUTOFF * 4;
        if (Math.Abs(delta) < deltaCutoff)
        {
          Stop();
          return true;
        }
        else
        {
          float speed = _getNextSpeed(delta, needPrecision);
          float averagePos = _pistons.Average(p => p.CurrentPosition);
          foreach (Piston piston in _pistons)
          {
            piston.Move(speed, averagePos);
          }
          return _pistons.All(p => p.HasReachedEnd());
        }
      }

      public void Stop()
      {
        foreach (Piston piston in _pistons)
        {
          piston.Stop();
        }
      }
      public override string ToString() => $"Actuator at speed {_velocityFactor}: {PistonCount} pistons";

      float _getNextSpeed(double delta, bool needPrecision)
      {
        // TODO take into account the rotor speed
        return MathHelper.Clamp((float)delta, -1f, 1f) * _maxAverageSpeed * (needPrecision ? 1 : 2);
        //float prev = this.pistons.Average(p => p.Velocity);
        //return MathHelper.Clamp(targetSpeed, prev - 0.1f, prev + 0.1f);
      }
    }
  }
}
