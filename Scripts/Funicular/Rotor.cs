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
  partial class Program : MyGridProgram
  {
    public class Rotor
    {
      readonly IMyMotorStator _rotor;
      readonly bool _reversed;
      readonly FunicularSettings _settings;

      public Rotor(IMyMotorStator rotor, bool reversed, FunicularSettings settings)
      {
        _rotor = rotor;
        _reversed = reversed;
        _settings = settings;
      }

      public bool Stop()
      {
        _rotor.RotorLock = _updateVelocity(0);
        if (_rotor.RotorLock)
        {
          _rotor.Enabled = false;
        }
        return _rotor.RotorLock;
      }

      public void Move(bool up, float distance)
      {
        _rotor.Enabled = true;
        _rotor.RotorLock = false;
        float direction = up ^ _reversed ? 1 : -1;
        float targetSpeed = distance > _settings.DeccelerationDistance
          ? _settings.MaxSpeed
          : _settings.MaxSpeed * distance / _settings.DeccelerationDistance;

        targetSpeed *= direction;
        if (distance < 0)
        {
          // case where the position is not yet defined
          targetSpeed = direction * _settings.SafeSpeed;
        }

        _updateVelocity(targetSpeed);
      }

      private bool _updateVelocity(float target)
      {
        var newTarget = MathHelper.Clamp(
          target,
          _rotor.TargetVelocityRPM - _settings.MaxAcceleration,
          _rotor.TargetVelocityRPM + _settings.MaxAcceleration
        );

        _rotor.TargetVelocityRPM = newTarget;
        return newTarget == target;
      }
    }
  }
}
