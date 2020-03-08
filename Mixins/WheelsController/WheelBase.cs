using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript {
partial class Program {
  /// <summary>Class that contains the characteristics of a wheel base, mostly to enable Ackermann steering</summary>
  public class WheelBase {
    static Vector3D LEFT = new Vector3D(-1, 0, 0);
    static Vector3D RIGHT = new Vector3D(1, 0, 0);
    /// <summary>Minimum Z coordinate of the base (ie most forward)</summary>
    public double MinZ => this.min.Z;
    /// <summary>Maximum Z coordinate of the base (ie rearmost)</summary>
    public double MaxZ => this.max.Z;
    /// <summary>Z coordinate of the center of turn</summary>
    public double CenterOfTurnZ { get; private set; }
    public double TurnRadius { get; private set; }
    public Vector3D LeftCenterOfTurn { get; private set; }
    public Vector3D RightCenterOfTurn { get; private set; }

    Vector3D min = new Vector3D(double.MaxValue, 0, double.MaxValue);
    Vector3D max = new Vector3D(double.MinValue, 0, double.MinValue);

    /// <summary>Updates the characteristics of the wheel base</summary>
    /// <param name="wheel">Wheel to add</param>
    public void AddWheel(PowerWheel wheel) {
      Vector3D p = wheel.Position;
      this.min.X = Math.Min(p.X, this.min.X);
      this.min.Z = Math.Min(p.Z, this.min.Z);
      this.max.X = Math.Max(p.X, this.max.X);
      this.max.Z = Math.Max(p.Z, this.max.Z);
      this.CenterOfTurnZ = (this.min.Z + this.max.Z) / 2;
      this.TurnRadius = this.max.Z - this.min.Z + ((this.max.X - this.min.X) / 2);
      this.LeftCenterOfTurn = ((this.min + this.max) / 2) + new Vector3D(-this.TurnRadius, 0, 0);
      this.RightCenterOfTurn = ((this.min + this.max) / 2) + new Vector3D(this.TurnRadius, 0, 0);
    }
    /// <summary>Based on the characteristics of the wheel base, returns the amount the wheel should turn</summary>
    /// <param name="wheel">Wheel whose angle we want</param>
    /// <param name="turnLeft">Whether the wheel is turning left or right</param>
    /// <returns>The angle, in degrees</returns>
    public float GetAngle(PowerWheel wheel, bool turnLeft) {
      Vector3D delta = (turnLeft ? this.LeftCenterOfTurn : this.RightCenterOfTurn) - wheel.Position;
      delta.Y = 0;
      delta = Vector3D.Normalize(delta);
      return MathHelper.ToDegrees((float)Math.Acos(delta.Dot(turnLeft ? LEFT : RIGHT)));
    }
  }
}
}
