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
      static readonly Vector3D UP = new Vector3D(0, 1, 0);
      static readonly Vector3D RIGHT = new Vector3D(1, 0, 0);
      static readonly string PO = "Propulsion override";
      static readonly string SO = "Steer override";

      public float Angle => this.wheel.SteerAngle;
      public readonly MyCubeSize CubeSize;
      public readonly float Mass;
      public readonly Vector3D Position;
      public float Power {
        get { return this.reversePow ? -this.wheel.GetValueFloat(PO) : this.wheel.GetValueFloat(PO); }
        set { this.wheel.SetValue(PO, this.reversePow ? -value : value); }
      }
      public float Steer {
        get { return (this.wheelBase.CenterOfTurnZ < this.Position.Z) ? -this.wheel.GetValueFloat(SO) : this.wheel.GetValueFloat(SO); }
        set { this.wheel.SetValue(SO, (this.wheelBase.CenterOfTurnZ < this.Position.Z) ? -value : value); }
      }
      public float Strength {
        get { return this.wheel.Strength; }
        set { this.wheel.Strength = value; }
      }
      public readonly int WheelSize;

      readonly float minY, maxY;
      int previousRoll;
      readonly bool reversePow;
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
        this.reversePow = RIGHT.Dot(tform.Dir(wheel.WorldMatrix.Up)) > 0;
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
        bool turnLeft = this.wheel.SteerAngle < 0;
        float angle = this.wheelBase.GetAngle(this, turnLeft);
        // We set the angle and invert steer lazily as setting it can cause problems with the steering
        if (Math.Abs(this.wheel.MaxSteerAngle - angle) > 0.01) {
          this.wheel.SetValue("MaxSteerAngle", angle);
        }
        bool invertSteer = (comZPos < this.Position.Z) ^ (this.wheelBase.CenterOfTurnZ < this.Position.Z);
        if (this.wheel.InvertSteer != invertSteer) {
          this.wheel.InvertSteer = invertSteer;
        }
      }
      /// <summary>Adjusts the suspension so that the wheel base "rolls" (has one side higher than the other)</summary>
      /// <param name="roll">Amount of roll needed, between -0.25 (roll left) and 0.25 (roll right)</param>
      public void Roll(float roll) {
        int iRoll = (int)(5 * MathHelper.Clamp(roll * 4, -1, 1));
        if (iRoll != this.previousRoll) {
          this.previousRoll = iRoll;
          this.wheel.Height = (iRoll > 0 && this.reversePow) || (iRoll < 0 && !this.reversePow)
            ? (this.reverseY ? -this.maxY : this.minY) + ((Math.Abs(iRoll) + 2) * 0.04f)
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

      double radius() => this.WheelSize * (this.CubeSize == MyCubeSize.Large ? 1.25 : 0.25);
    }
  }
}
