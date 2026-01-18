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
    /// <summary>Wraps a <see cref="IMyMotorSuspension"/> to give it some additional features and info</summary>
    public class PowerWheel
    {
      static readonly Log LOG = Log.GetLog("PW");
      // Determined for small 5*5 wheels:
      // F[wheel] = Strength squared * compressionRatio * STRENGTH_MULT
      const float STRENGTH_MULT = 4326;
      static readonly Vector3D UP = new Vector3D(0, 1, 0);
      static readonly Vector3D RIGHT = new Vector3D(1, 0, 0);
      static readonly float MAX_STEERING_ANGLE = MathHelper.ToRadians(45.83f);

      public float Angle => _wheel.SteerAngle;
      public float MaxAngle => _wheel.MaxSteerAngle;
      public readonly MyCubeSize CubeSize;
      public readonly float Mass;
      public readonly Vector3D Position;
      public float Power
      {
        get { return IsRight ? -_wheel.PropulsionOverride : _wheel.PropulsionOverride; }
        set { _wheel.PropulsionOverride = IsRight ? -value : value; }
      }
      public float Steer
      {
        get { return (_wheelBase.CenterOfTurnZ < Position.Z) ? -_wheel.SteeringOverride : _wheel.SteeringOverride; }
        set { _wheel.SteeringOverride = (_wheelBase.CenterOfTurnZ < Position.Z) ? -value : value; }
      }
      public float Strength => _wheel.Strength;
      public readonly int WheelSize;
      public bool IsRight { get; private set; }

      readonly float _minY, _maxY;
      float _previousRoll;
      readonly bool _reverseY;
      readonly CoordinatesTransformer _transformer;
      readonly IMyMotorSuspension _wheel;
      readonly WheelBase _wheelBase;
      /// <summary>Creates a new power wheel</summary>
      /// <param name="wheel">The actual wheel it wraps</param>
      /// <param name="wb">The wheel base, to which the power wheel will be added</param>
      /// <param name="tform">Coordinates transformer, should be one that makes the Z axis parallel the wheel direction and Y axis the up axis</param>
      public PowerWheel(IMyMotorSuspension wheel, WheelBase wb, CoordinatesTransformer tform)
      {
        _wheelBase = wb;
        CubeSize = wheel.CubeGrid.GridSizeEnum;
        Position = tform.Pos(wheel.GetPosition());
        IsRight = RIGHT.Dot(tform.Dir(wheel.WorldMatrix.Up)) > 0;
        _reverseY = UP.Dot(tform.Dir(wheel.WorldMatrix.Backward)) > 0;
        _transformer = tform;
        _wheel = wheel;

        wheel.Height = 10;
        _maxY = wheel.Height;
        wheel.Height = -10;
        _minY = wheel.Height;
        if (_reverseY)
        {
          float tmp = _minY;
          _minY = -_maxY;
          _maxY = -tmp;
        }
        WheelSize = (wheel.Top.Max - wheel.Top.Min).X + 1;
        Mass = CubeSize == MyCubeSize.Small
          ? WheelSize == 1 ? 105 : (WheelSize == 3 ? 205 : 310)
          : WheelSize == 1 ? 420 : (WheelSize == 3 ? 590 : 760);
        // real center of rotation of the wheel
        Position += tform.Dir(wheel.WorldMatrix.Up) * wheel.CubeGrid.GridSize;
        _wheelBase.AddWheel(this);
      }
      /// <summary>Makes it so that the wheel strafes (ie all the wheels remain parallel)</summary>
      /// <param name="comZPos">Z coordinates of the center of mass of the grid of the wheel base, in the same referential than the wheels</param>
      public void Strafe(double comZPos)
      {
        _wheel.MaxSteerAngle = MAX_STEERING_ANGLE;
        _wheel.InvertSteer = comZPos < Position.Z;
      }
      /// <summary>Makes it so that the wheel turns. Automatically adjusts the direction with respect to the center of mass and center of turn</summary>
      /// <param name="comZPos">Z coordinates of the center of mass of the grid of the wheel base, in the same referential than the wheels</param>
      public void Turn(double comZPos)
      {
        bool turnLeft = (_wheelBase.CenterOfTurnZ < Position.Z) ^ (_wheel.SteerAngle > 0);
        float angle = Math.Abs(_wheelBase.GetAngle(this, turnLeft));
        // We only change the settings if the wheel turns enough to matter
        if (Math.Abs(_wheel.SteerAngle) > 0.03 || _wheel.MaxSteerAngle < 0.01)
        {
          // We set the angle and invert steer lazily as setting it can cause problems with the steering
          bool invertSteer = (comZPos < Position.Z) ^ (_wheelBase.CenterOfTurnZ < Position.Z);
          if (Math.Abs(_wheel.MaxSteerAngle - angle) > 0.01)
          {
            LOG.Debug($"{_wheel.CustomName} => {angle:F2} {invertSteer}");
            _wheel.MaxSteerAngle = Math.Min(angle, MAX_STEERING_ANGLE);
          }
          if (_wheel.InvertSteer != invertSteer)
          {
            _wheel.InvertSteer = invertSteer;
          }
        }
      }
      /// <summary>Adjusts the suspension so that the wheel base "rolls" (has one side higher than the other)</summary>
      /// <param name="roll">Amount of roll needed, between -0.25 (roll left) and 0.25 (roll right)</param>
      public void Roll(float roll)
      {
        if (roll != _previousRoll)
        {
          _previousRoll = roll;
          _wheel.Height = (roll > 0 && IsRight) || (roll < 0 && !IsRight)
            ? (_reverseY ? -_maxY : _minY) + Math.Abs(roll)
            : _reverseY ? -_maxY : _minY;
        }
      }
      /// <summary>Gets how much the suspension is compressed</summary>
      /// <returns>The compression ratio, normal values are between 0 (not compressed) and 1 (fully compressed)</returns>
      public float GetCompressionRatio()
      {
        double pos = _transformer.Pos(_wheel.Top.GetPosition()).Y - _transformer.Pos(_wheel.GetPosition()).Y;
        return ((float)(_reverseY ? -pos : pos) - _minY) / (_maxY - _minY);
      }
      /// <summary>Estimates the coordinates of the point of contact of the wheel with the ground</summary>
      /// <returns>The world coordinates</returns>
      public Vector3D GetPointOfContactW() => _wheel.Top.GetPosition() +
          (_radius() * (_reverseY ? _wheel.WorldMatrix.Forward : _wheel.WorldMatrix.Backward));
      /// <summary>Update the strength of the suspension to reach a given compression ratio</summary>
      /// <param name="force">Force applied on the suspesion</param>
      /// <param name="targetRatio">Compression ratio to target between 0 and 1</param>
      public void SetStrength(float force, float targetRatio)
      {
        float newStrength = (float)Math.Sqrt(force / (STRENGTH_MULT * Math.Max(targetRatio, 0.001f)));
        if (Math.Abs(newStrength - _wheel.Strength) > 0.1)
        {
          _wheel.Strength = newStrength;
        }
      }
      /// <summary>Returns the current force applied by the suspension</summary>
      /// <returns>the force</returns>
      public float GetForce() => GetCompressionRatio() * STRENGTH_MULT * _wheel.Strength * _wheel.Strength;

      double _radius() => WheelSize * (CubeSize == MyCubeSize.Large ? 1.25 : 0.25);
    }
  }
}
