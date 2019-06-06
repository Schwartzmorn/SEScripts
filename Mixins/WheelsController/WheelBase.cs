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
  public class WheelBase {
    static Vector3D L = new Vector3D(-1, 0, 0);
    static Vector3D R = new Vector3D(1, 0, 0);

    public double MinZ => _min.Z;
    public double MaxZ => _max.Z;
    public double CenterOfTurnZ { get; private set; }
    public double TurnRadius { get; private set; }
    public Vector3D LeftCenterOfTurn { get; private set; }
    public Vector3D RightCenterOfTurn { get; private set; }

    private Vector3D _min = new Vector3D(double.MaxValue, 0, double.MaxValue);
    private Vector3D _max = new Vector3D(double.MinValue, 0, double.MinValue);

    public void AddWheel(PowerWheel w) {
      var p = w.Position;
      _min.X = Math.Min(p.X, _min.X);
      _min.Z = Math.Min(p.Z, _min.Z);
      _max.X = Math.Max(p.X, _max.X);
      _max.Z = Math.Max(p.Z, _max.Z);
      CenterOfTurnZ = (_min.Z + _max.Z) / 2;
      TurnRadius = _max.Z - _min.Z + ((_max.X - _min.X) / 2);
      LeftCenterOfTurn = ((_min + _max) / 2) + new Vector3D(-TurnRadius, 0, 0);
      RightCenterOfTurn = ((_min + _max) / 2) + new Vector3D(TurnRadius, 0, 0);
    }

    public float GetAngle(PowerWheel wheel, bool turnLeft) {
      var delta = (turnLeft ? LeftCenterOfTurn : RightCenterOfTurn) - wheel.Position;
      delta.Y = 0;
      delta = Vector3D.Normalize(delta);
      return MathHelper.ToDegrees((float)Math.Acos(delta.Dot(turnLeft ? L : R)));
    }
  }
}
}
