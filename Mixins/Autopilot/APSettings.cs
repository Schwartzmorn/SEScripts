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
        if (wp.Terrain == Terrain.Bad) {
          return 5;
        } else if(wp.Terrain == Terrain.Normal) {
          return 10;
        } else if(wp.Terrain == Terrain.Good) {
          return 20;
        } else {
          return 30;
        }
      }

      public double GetTargetPrecision(APWaypoint wp) {
        return wp.Type == WPType.Path ? 3 : 0.5;
      }

      public bool IsWaypointReached(APWaypoint wp, double distance) {
        return distance < GetTargetPrecision(wp);
      }

      public double GetTargetSpeed(double curTargetSpeed, double nextTargetSpeed, double distToWP, double angle) {
        double smoothingDistance = (Math.Pow(curTargetSpeed, 2) - Math.Pow(nextTargetSpeed, 2)) / SpeedSmothingFactor;
        if(distToWP < smoothingDistance) {
          return MathHelper.Lerp(curTargetSpeed, nextTargetSpeed, 1 - (distToWP / smoothingDistance));
        }
        return curTargetSpeed;
        // TODO take angle into account
      }

      public float GetSteer(double angle, double speed) {
        return (float)MathHelper.Clamp(Math.Sqrt(Math.Abs(angle)) * Math.Sign(angle), -1, 1);
      }
    }
  }
}
