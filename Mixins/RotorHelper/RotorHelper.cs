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
  static class RotorHelper {
    const float PRECISION = 0.005f;
    /// <summary>
    /// Returns the smallest angle to travel to reach the given position, taking into account limits.
    /// The result is normally between -Pi and +Pi, unless there are limits that would prevent the rotor from going this direction.
    /// </summary>
    /// <param name="stator">stator whose position we want to change</param>
    /// <param name="target">position to reach. It is assumed to be in the limits of the stator</param>
    /// <returns>the angle to travel</returns>
    public static float AngleProxy(this IMyMotorStator stator, float target) {
      // gets the shortest path
      float ap = Mod(target - stator.Angle + MathHelper.Pi, 2 * MathHelper.Pi) - MathHelper.Pi;
      // check whether we'll bump into a limit. We assume the target is within the limits
      float proxyTarget = target;
      if (ap > 0 && stator.UpperLimitRad < 7) {
        while (proxyTarget < stator.Angle) {
          proxyTarget += 2 * MathHelper.Pi;
        }
        if (proxyTarget > stator.UpperLimitRad + PRECISION) {
          ap -= 2 * MathHelper.Pi;
        }
      } else if (ap < 0 && stator.LowerLimitRad > -7) {
        while (proxyTarget > stator.Angle) {
          proxyTarget -= 2 * MathHelper.Pi;
        }
        if (proxyTarget < stator.LowerLimitRad - PRECISION) {
          ap += 2 * MathHelper.Pi;
        }
      }

      return ap;
    }

    /// <summary>
    /// Returns whether stator has reached its limit given its current velocity
    /// </summary>
    /// <param name="stator"></param>
    /// <returns></returns>
    public static bool HasReachedEnd(this IMyMotorStator stator) => stator.TargetVelocityRad == 0 ? false : stator.HasReachedEnd(stator.TargetVelocityRad > 0);

    /// <summary>
    /// Returns whether stator has reached its limit
    /// </summary>
    /// <param name="stator"></param>
    /// <param name="upperLimit"></param>
    /// <returns></returns>
    public static bool HasReachedEnd(this IMyMotorStator stator, bool upperLimit) {
      if (upperLimit && stator.Angle > stator.UpperLimitRad - PRECISION) {
        return true;
      } else if (!upperLimit && stator.Angle < stator.LowerLimitRad + PRECISION) {
        return true;
      }
      return false;
    }

    public static bool HasReached(this IMyMotorStator stator, float angle) {
      return Math.Abs(stator.AngleProxy(angle)) < PRECISION || stator.HasReachedEnd();
    }

    public static float Mod(float A, float N) => A - (MathHelper.Floor(A / N) * N);
  }
}
