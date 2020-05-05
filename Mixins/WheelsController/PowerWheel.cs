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
    /// <summary>Wraps a <see cref="IMyMotorSuspension"/> to give it some additional features and info</summary>
    public class PowerWheel {
      // Determined for small 5*5 wheels:
      // F[wheel] = Strength squared * compressionRatio * STRENGTH_MULT
      const float STRENGTH_MULT = 4326;
      static readonly Vector3D UP = new Vector3D(0, 1, 0);
      static readonly Vector3D RIGHT = new Vector3D(1, 0, 0);
      const string PO = "Propulsion override";
      const string SO = "Steer override";

      public float Angle => this.wheel.SteerAngle;
      public float MaxAngle => this.wheel.MaxSteerAngle;
      public readonly MyCubeSize CubeSize;
      public readonly float Mass;
      public readonly Vector3D Position;
      public float Power {
        get { return this.IsRight ? -this.wheel.GetValueFloat(PO) : this.wheel.GetValueFloat(PO); }
        set { this.wheel.SetValue(PO, this.IsRight ? -value : value); }
      }
      public float Steer {
        get { return (this.wheelBase.CenterOfTurnZ < this.Position.Z) ? -this.wheel.GetValueFloat(SO) : this.wheel.GetValueFloat(SO); }
        set { this.wheel.SetValue(SO, (this.wheelBase.CenterOfTurnZ < this.Position.Z) ? -value : value); }
      }
      public float Strength => this.wheel.Strength;
      public readonly int WheelSize;
      public bool IsRight { get; private set; }

      readonly float minY, maxY;
      float previousRoll;
      readonly bool reverseY;
      readonly CoordinatesTransformer transformer;
      readonly IMyMotorSuspension wheel;
      readonly WheelBase wheelBase;
      /// <summary>Creates a new power wheel</summary>
      /// <param name="wheel">The actual wheel it wraps</param>
      /// <param name="wb">The wheel base, to which the power wheel will be added</param>
      /// <param name="tform">Coordinates transformer, should be one that makes the Z axis parallel the wheel direction and Y axis the up axis</param>
      public PowerWheel(IMyMotorSuspension wheel, WheelBase wb, CoordinatesTransformer tform) {
        this.wheelBase = wb;
        this.CubeSize = wheel.CubeGrid.GridSizeEnum;
        this.Position = tform.Pos(wheel.GetPosition());
        this.IsRight = RIGHT.Dot(tform.Dir(wheel.WorldMatrix.Up)) > 0;
        this.reverseY = UP.Dot(tform.Dir(wheel.WorldMatrix.Backward)) > 0;
        this.transformer = tform;
        this.wheel = wheel;

        wheel.Height = 10;
        this.maxY = wheel.Height;
        wheel.Height = -10;
        this.minY = wheel.Height;
        if (this.reverseY) {
          float tmp = this.minY;
          this.minY = -this.maxY;
          this.maxY = -tmp;
        }
        this.WheelSize = (wheel.Top.Max - wheel.Top.Min).X + 1;
        this.Mass = this.CubeSize == MyCubeSize.Small
          ? this.WheelSize == 1 ? 105 : (this.WheelSize == 3 ? 205 : 310)
          : this.WheelSize == 1 ? 420 : (this.WheelSize == 3 ? 590 : 760);
        // real center of rotation of the wheel
        this.Position += tform.Dir(wheel.WorldMatrix.Up) * wheel.CubeGrid.GridSize;
        this.wheelBase.AddWheel(this);
      }
      /// <summary>Makes it so that the wheel strafes (ie all the wheels remain parallel)</summary>
      /// <param name="comZPos">Z coordinates of the center of mass of the grid of the wheel base, in the same referential than the wheels</param>
      public void Strafe(double comZPos) {
        this.wheel.SetValue("MaxSteerAngle", 30f);
        this.wheel.InvertSteer = comZPos < this.Position.Z;
      }
      /// <summary>Makes it so that the wheel turns. Automatically adjusts the direction with respect to the center of mass and center of turn</summary>
      /// <param name="comZPos">Z coordinates of the center of mass of the grid of the wheel base, in the same referential than the wheels</param>
      public void Turn(double comZPos) {
        bool turnLeft = (this.wheelBase.CenterOfTurnZ < this.Position.Z) ^ (this.wheel.SteerAngle > 0);
        float angle = Math.Abs(this.wheelBase.GetAngle(this, turnLeft));
        if (Math.Abs(this.wheel.SteerAngle) > 0.03 && Math.Abs(this.wheel.MaxSteerAngle - MathHelper.ToRadians(angle)) > 0.01) {
          // We set the angle and invert steer lazily as setting it can cause problems with the steering
          this.wheel.SetValue("MaxSteerAngle", angle);
          bool invertSteer = (comZPos < this.Position.Z) ^ (this.wheelBase.CenterOfTurnZ < this.Position.Z);
          if (this.wheel.InvertSteer != invertSteer) {
            this.wheel.InvertSteer = invertSteer;
          }
        }
      }
      /// <summary>Adjusts the suspension so that the wheel base "rolls" (has one side higher than the other)</summary>
      /// <param name="roll">Amount of roll needed, between -0.25 (roll left) and 0.25 (roll right)</param>
      public void Roll(float roll) {
        if (roll != this.previousRoll) {
          this.previousRoll = roll;
          this.wheel.Height = (roll > 0 && this.IsRight) || (roll < 0 && !this.IsRight)
            ? (this.reverseY ? -this.maxY : this.minY) + Math.Abs(roll)
            : this.reverseY ? -this.maxY : this.minY;
        }
      }
      /// <summary>Gets how much the suspension is compressed</summary>
      /// <returns>The compression ratio, normal values are between 0 (not compressed) and 1 (fully compressed)</returns>
      public float GetCompressionRatio() {
        double pos = this.transformer.Pos(this.wheel.Top.GetPosition()).Y - this.transformer.Pos(this.wheel.GetPosition()).Y;
        return ((float)(this.reverseY ? -pos : pos) - this.minY) / (this.maxY - this.minY);
      }
      /// <summary>Estimates the coordinates of the point of contact of the wheel with the ground</summary>
      /// <returns>The world coordinates</returns>
      public Vector3D GetPointOfContactW() => this.wheel.Top.GetPosition() +
          (this.radius() * (this.reverseY ? this.wheel.WorldMatrix.Forward : this.wheel.WorldMatrix.Backward));
      /// <summary>Update the strength of the suspension to reach a given compression ratio</summary>
      /// <param name="force">Force applied on the suspesion</param>
      /// <param name="targetRatio">Compression ratio to target between 0 and 1</param>
      public void SetStrength(float force, float targetRatio) {
        float newStrength = (float)Math.Sqrt(force / (STRENGTH_MULT * Math.Max(targetRatio, 0.001f)));
        if (Math.Abs(newStrength - this.wheel.Strength) > 0.1) {
          this.wheel.Strength = newStrength;
        }
      }
      /// <summary>Returns the current force applied by the suspension</summary>
      /// <returns>the force</returns>
      public float GetForce() => this.GetCompressionRatio() * STRENGTH_MULT * this.wheel.Strength * this.wheel.Strength;

      double radius() => this.WheelSize * (this.CubeSize == MyCubeSize.Large ? 1.25 : 0.25);
    }
  }
}
