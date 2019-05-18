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
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
  partial class Program {
    public enum SolarRotorType { Vertical, HorizontalLeft, HorizontalRight };
    public class SolarRotor {
      public SolarRotor(String pylonKey, SolarRotorType type) {
        _pylonKey = pylonKey;
        _type = type;
      }
      public bool HasReachedEnd(bool forNight) {
        if (GetRotor() == null) {
          return false;
        }
        if (_type == SolarRotorType.Vertical) {
          return false;
        } else {
          bool checkUpper = forNight ^ (_type == SolarRotorType.HorizontalLeft);
          if (checkUpper) {
            return Math.Abs(GetRotor().Angle - GetRotor().UpperLimitRad) < 0.01f;
          } else {
            return Math.Abs(GetRotor().Angle - GetRotor().LowerLimitRad) < 0.01f;
          }
        }
      }
      public IMyMotorStator GetRotor() {
        if (_motor == null) {
          _motor = GTS.GetBlockWithName(GetRotorName()) as IMyMotorStator;
          if (_motor != null) {
            Init();
          }
        }
        return _motor;
      }
      public void Move(float speed) {
        if (GetRotor() != null) {
          GetRotor().TargetVelocityRad = (_type == SolarRotorType.HorizontalLeft) ? -speed : speed;
          if (speed == 0) {
            GetRotor().ApplyAction("OnOff_Off");
            GetRotor().RotorLock = true;
          } else {
            GetRotor().ApplyAction("OnOff_On");
            GetRotor().RotorLock = false;
          }
        }
      }
      public float GetPosition() {
        float angle = GetRotor() != null ? AngleProxy(GetRotor().Angle) : 0;
        return (_type == SolarRotorType.HorizontalLeft) ? -angle : angle;
      }
      private String GetRotorName() {
        String name = "Main Base Solar Panel Rotor " + _pylonKey + " ";
        switch (_type) {
          case SolarRotorType.Vertical:
            name += "1";
            break;
          case SolarRotorType.HorizontalLeft:
            name += "2 (left)";
            break;
          case SolarRotorType.HorizontalRight:
            name += "2 (right)";
            break;
        }
        return name;
      }
      private void Init() {
        _motor.Torque = 1000000;
        _motor.BrakingTorque = 1000000;
        switch (_type) {
          case SolarRotorType.HorizontalLeft:
            _motor.LowerLimitRad = -(float)Math.PI;
            _motor.UpperLimitRad = 0;
            break;
          case SolarRotorType.HorizontalRight:
            _motor.LowerLimitRad = 0;
            _motor.UpperLimitRad = (float)Math.PI;
            break;
          case SolarRotorType.Vertical:
            _motor.LowerLimitRad = -(float)Math.PI / 4;
            _motor.UpperLimitRad = (float)Math.PI / 4;
            break;
        }
      }
      IMyMotorStator _motor;
      String _pylonKey;
      SolarRotorType _type;
    }
  }
}
