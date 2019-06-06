using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
partial class Program {
  public class APSettings {
    public readonly double HandbrakeSpeed = 2;
    public readonly double SpeedPrecision = 2;
    public readonly float BrakePower = 1;
    public readonly float PowerMult = 0.2f;
    public readonly double SpeedSmothingFactor = 40;

    public double GetTargetSpeed(APWaypoint wp) {
      if(wp == null) return 0;
      switch(wp.Terrain) {
        case Terrain.Dangerous:
          return 1;
        case Terrain.Bad:
          return 5;
        case Terrain.Normal:
          return 10;
        case Terrain.Good:
          return 20;
        case Terrain.Open:
        default:
          return 30;
      }
    }

    public double GetTargetPrecision(APWaypoint wp) {
      switch(wp.Type) {
        case WPType.PrecisePath:
          return 0.5;
        case WPType.Maneuvering:
          return 0.1;
        case WPType.Path:
        default:
          return 3;
      }
    }

    public bool IsWaypointReached(APWaypoint wp, double distance) => distance < GetTargetPrecision(wp);

    public double GetTargetSpeed(double curTargetSpeed, double nextTargetSpeed, double distToWP) {
      double smoothingDistance = (Math.Pow(curTargetSpeed, 2) - Math.Pow(nextTargetSpeed, 2)) / SpeedSmothingFactor;
      return distToWP < smoothingDistance
        ? MathHelper.Lerp(curTargetSpeed, nextTargetSpeed, 1 - (distToWP / smoothingDistance))
        : curTargetSpeed;
    }

    public float GetSteer(double angle, double speed) => (float)MathHelper.Clamp(Math.Sqrt(Math.Abs(angle)) * Math.Sign(angle), -1, 1);
  }
}
}
