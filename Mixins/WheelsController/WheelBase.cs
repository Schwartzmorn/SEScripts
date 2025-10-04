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

namespace IngameScript
{
  partial class Program
  {
    /// <summary>Class that contains the characteristics of a wheel base, mostly to enable Ackermann steering</summary>
    public class WheelBase
    {
      static Vector3D LEFT = new Vector3D(-1, 0, 0);
      static Vector3D RIGHT = new Vector3D(1, 0, 0);
      /// <summary>Minimum Z coordinate of the base (ie most forward)</summary>
      public double MinZ => _min.Z;
      /// <summary>Maximum Z coordinate of the base (ie rearmost)</summary>
      public double MaxZ => _max.Z;
      /// <summary>Z coordinate of the center of turn</summary>
      public double CenterOfTurnZ { get; private set; }
      public double CenterOfTurnZOffset
      {
        get { return _centerOfTurnZOffset; }
        set
        {
          _centerOfTurnZOffset = value;
          _update();
        }
      }
      public double TurnRadius { get; private set; }
      public double TurnRadiusOverride
      {
        get { return _turnRadiusOverride; }
        set
        {
          _turnRadiusOverride = Math.Abs(value);
          _update();
        }
      }
      public Vector3D LeftCenterOfTurn { get; private set; }
      public Vector3D RightCenterOfTurn { get; private set; }

      double _centerOfTurnZOffset = 0;
      double _turnRadiusOverride = 0;

      Vector3D _min = new Vector3D(double.MaxValue, 0, double.MaxValue);
      Vector3D _max = new Vector3D(double.MinValue, 0, double.MinValue);

      /// <summary>Updates the characteristics of the wheel base</summary>
      /// <param name="wheel">Wheel to add</param>
      public void AddWheel(PowerWheel wheel)
      {
        Vector3D p = wheel.Position;
        _min.X = Math.Min(p.X, _min.X);
        _min.Z = Math.Min(p.Z, _min.Z);
        _max.X = Math.Max(p.X, _max.X);
        _max.Z = Math.Max(p.Z, _max.Z);
        _update();
      }
      /// <summary>Based on the characteristics of the wheel base, returns the amount the wheel should turn</summary>
      /// <param name="wheel">Wheel whose angle we want</param>
      /// <param name="turnLeft">Whether the wheel is turning left or right</param>
      /// <returns>The angle, in degrees</returns>
      public float GetAngle(PowerWheel wheel, bool turnLeft)
      {
        Vector3D delta = (turnLeft ? LeftCenterOfTurn : RightCenterOfTurn) - wheel.Position;
        delta.Y = 0;
        delta = Vector3D.Normalize(delta);
        return MathHelper.ToDegrees((float)Math.Acos(delta.Dot(turnLeft ? LEFT : RIGHT)));
      }

      void _update()
      {
        CenterOfTurnZ = (_min.Z + _max.Z) / 2;
        TurnRadius = TurnRadiusOverride == 0 ? _max.Z - _min.Z + ((_max.X - _min.X) / 2) : TurnRadiusOverride;
        LeftCenterOfTurn = ((_min + _max) / 2) + new Vector3D(-TurnRadius, 0, 0) + (CenterOfTurnZOffset * Vector3D.Forward);
        RightCenterOfTurn = ((_min + _max) / 2) + new Vector3D(TurnRadius, 0, 0) + (CenterOfTurnZOffset * Vector3D.Forward);
      }
    }
  }
}
