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

    public class WheelsController {
      public struct Calibration {
        public float Weight;
        public double MinZ;
        public double MaxZ;
        public float MinMult;
        public float MaxMult;

        public float GetMult(double z) => MathHelper.Lerp(MinMult, MaxMult, (float)((z - MinZ) / (MaxZ - MinZ)));
      }

      const string INI_SECTION = "wheels-controller";
      // Empirically determined to have a compression ratio of 0.35
      const float STRENGTH_MULT = 0.00086f;
      const float TARGET_RATIO = 0.35f;
      const float TARGET_RATIO_HIGH = 0.5f;

      readonly WheelBase _base = new WheelBase();
      Calibration? _minCalibration = null;
      Calibration? _maxCalibration = null;
      readonly IMyShipController _controller;
      bool _high = false;
      float _power = 0;
      float _roll = 0;
      float _steer = 0;
      bool _strafing = false;
      readonly List<PowerWheel> _wheels = new List<PowerWheel>();
      readonly CoordsTransformer _transformer;

      public WheelsController(MyGridProgram program, MyIni ini, CmdLine cmd, CoordsTransformer transformer, IMyShipController controller) {
        _transformer = transformer;
        _controller = controller;
        var wheels = new List<IMyMotorSuspension>();
        program.GridTerminalSystem.GetBlocksOfType(wheels, w => w.CubeGrid == program.Me.CubeGrid);
        int nWorking = 0;
        foreach(var w in wheels) {
          if(w.DisplayNameText.Contains("Power")) {
            _wheels.Add(new PowerWheel(w, _base, transformer));
            if(w.IsWorking) {
              ++nWorking;
            }
          }
        }
        _wheels.Sort((w1, w2) => Math.Sign(w1.Position.Z - w2.Position.Z));
        if(nWorking == _wheels.Count) {
          Logger.Inst.Log($"Wheels... {nWorking + 2}/{nWorking + 2} OK");
        } else {
          Logger.Inst.Log($"Wheels... {_wheels.Count - nWorking}/{_wheels.Count + 2} KO");
        }

        cmd.AddCmd(new Cmd("wc-calibrate", "Calibrates the suspensions", _ => Calibrate(),
            maxArgs: 0, detailedHelp: "Must be done on even and horizontal floor"));
        cmd.AddCmd(new Cmd("wc-position", "Changes between normal and high position", s => SetPosition(s[0]),
            minArgs: 1, maxArgs: 1, detailedHelp: "Argument can take the following values:\n  high\n  normal\n  switch"));
        cmd.AddCmd(new Cmd("wc-power", "Set power override", s => SetPower(s[0]),
            minArgs: 1, maxArgs: 1));
        cmd.AddCmd(new Cmd("wc-reset", "Delete all suspension calibrations", _ => Reset(),
            maxArgs: 0));
        cmd.AddCmd(new Cmd("wc-roll", "Set roll override", s => SetRoll(float.Parse(s[0])),
            minArgs: 1, maxArgs: 1));
        cmd.AddCmd(new Cmd("wc-steer", "Set steer override", s => SetSteer(s[0]),
            minArgs: 1, maxArgs: 1));
        cmd.AddCmd(new Cmd("wc-strafe", "Activates or deactivates straffing", s => SetStrafing(s[0]),
            minArgs: 1, maxArgs: 1, detailedHelp: "Argument can take the following values:\n  on\n  off\n  switch"));
        Scheduler.Inst.AddAction(new ScheduledAction(() => _updateWheels(), 10));
        Scheduler.Inst.AddActionOnSave(Save);

        if(ini.ContainsKey(INI_SECTION, "min-weight")) {
          _minCalibration = _deserializeCalibration(ini, "min");
          if(ini.ContainsKey(INI_SECTION, "max-weight")) {
            _minCalibration = _deserializeCalibration(ini, "max");
          }
        }
        _high = ini.Get(INI_SECTION, "is-high").ToBoolean();
      }

      public void SetStrafing(string arg) {
        if(arg == "on") {
          _strafing = true;
        } else if(arg == "off") {
          _strafing = false;
        } else if(arg == "switch") {
          _strafing = !_strafing;
        }
        _updateWheels();
      }

      public void SetPosition(string arg) {
        if(arg == "high") {
          _high = true;
        } else if(arg == "normal") {
          _high = false;
        } else if(arg == "switch") {
          _high = !_high;
        }
        _updateWheels();
      }

      public void SetPower(float power) => _power = power;

      public void SetPower(string power) => float.TryParse(power, out _power);

      public void SetSteer(float steer) => _steer = steer;

      public void SetSteer(string steer) => float.TryParse(steer, out _steer);

      public void SetRoll(float roll) => _roll = roll;

      public double GetIdealCenterOfMass() => _base.CenterOfTurnZ;

      public void Reset() {
        _minCalibration = null;
        _maxCalibration = null;
      }

      public void Calibrate() {
        double minZ = double.MaxValue, maxZ = double.MinValue;
        PowerWheel minW = null, maxW = null;
        foreach (var w in _wheels) {
          if (w.Position.Z > maxZ) {
            maxZ = w.Position.Z;
            maxW = w;
          }
          if (w.Position.Z < minZ) {
            minZ = w.Position.Z;
            minW = w;
          }
        }
        float target = _high ? TARGET_RATIO_HIGH : TARGET_RATIO;
        var newC = new Calibration() {
          Weight = _getShipWeight(),
          MaxZ = maxZ,
          MinZ = minZ,
          MaxMult = (float)Math.Sqrt(Math.Max(maxW.GetCompressionRatio(), 0) / target) * _getCalibratedMult(maxZ),
          MinMult = (float)Math.Sqrt(Math.Max(minW.GetCompressionRatio(), 0) / target) * _getCalibratedMult(minZ),
        };
        if (_minCalibration == null) {
          _minCalibration = newC;
        } else if (_maxCalibration == null) {
          var minC = _minCalibration.Value;
          if (Math.Max(minC.Weight, newC.Weight) / Math.Min(minC.Weight, newC.Weight) < 2) {
            _minCalibration = newC;
          } else if (minC.Weight < newC.Weight) {
            _maxCalibration = newC;
          } else {
            _maxCalibration = _minCalibration;
            _minCalibration = newC;
          }
        } else {
          var minC = _minCalibration.Value;
          var maxC = _maxCalibration.Value;
          if (newC.Weight >= maxC.Weight) {
            _maxCalibration = newC;
          } else if (newC.Weight <= minC.Weight) {
            _minCalibration = newC;
          } else {
            float minRatio = Math.Max(minC.Weight, newC.Weight) / Math.Min(minC.Weight, newC.Weight);
            float maxRatio = Math.Max(maxC.Weight, newC.Weight) / Math.Min(maxC.Weight, newC.Weight);
            if(minRatio < maxRatio) {
              _minCalibration = newC;
            } else {
              _maxCalibration = newC;
            }
          }
        }
      }

      public void Save(MyIni ini) {
        ini.Set(INI_SECTION, "is-high", _high);
        if (_minCalibration != null) {
          _saveCalibration(ini, _minCalibration.Value, "min");
          if (_maxCalibration != null) {
            _saveCalibration(ini, _maxCalibration.Value, "max");
          }
        }
      }

      void _saveCalibration(MyIni ini, Calibration c, string prefix) {
        ini.Set(INI_SECTION, prefix + "-weight", c.Weight);
        ini.Set(INI_SECTION, prefix + "-min-z", c.MinZ);
        ini.Set(INI_SECTION, prefix + "-max-z", c.MaxZ);
        ini.Set(INI_SECTION, prefix + "-min-mult", c.MinMult);
        ini.Set(INI_SECTION, prefix + "-max-mult", c.MaxMult);
      }

      Calibration _deserializeCalibration(MyIni ini, string prefix) {
        return new Calibration() {
          Weight = ini.Get(INI_SECTION, prefix + "-weight").ToSingle(),
          MinZ = ini.Get(INI_SECTION, prefix + "-min-z").ToDouble(),
          MaxZ = ini.Get(INI_SECTION, prefix + "-max-z").ToDouble(),
          MinMult = ini.Get(INI_SECTION, prefix + "-min-mult").ToSingle(),
          MaxMult = ini.Get(INI_SECTION, prefix + "-max-mult").ToSingle()
        };
      }

      // point of contact of frontmost wheels, taking into account the contact plane
      public Vector3D GetPointOfContactW(Vector3D frontDir) {
        var frontPoc = new Vector3D(0, 0, 0);
        int count = 0;
        foreach(var w in _wheels) {
          if (w.Position.Z < _base.CenterOfTurnZ + 0.2) {
            frontPoc += w.GetPointOfContactW();
            ++count;
          }
        }
        var res = frontPoc / count;
        double resO = _transformer.Pos(res).Z - _base.MinZ;
        return res + resO * frontDir;
      }

      // returns normal to the plane of wheel contact with the ground
      public Vector3D GetContactPlaneW() {
        var center = new Vector3D(0, 0, 0);
        foreach(var w in _wheels) {
          center += w.GetPointOfContactW();
        }
        center = center / _wheels.Count;
        double xx = 0, xy = 0, xz = 0, yy = 0, yz = 0, zz = 0;
        foreach(var w in _wheels) {
          var r = w.GetPointOfContactW() - center;
          xx += r.X*r.X;
          xy += r.X*r.Y;
          xz += r.X*r.Z;
          yy += r.Y*r.Y;
          yz += r.Y*r.Z;
          zz += r.Z*r.Z;
        }
        double detX = yy*zz - yz*yz;
        double detY = xx*zz - xz*xz;
        double detZ = xx*yy - xy*xy;
        double detMax = Math.Max(detX, Math.Max(detY, detZ));

        Vector3D res;
        if (detMax == detX) {
          res = new Vector3D(detX, xz*yz - xy*zz, xy*yz - xz*yy);
        } else if (detMax == detY) {
          res = new Vector3D(xz*yz - xy*zz, detY, xy*xz - yz*xx);
        } else {
          res = new Vector3D(xy*yz - xz*yy, xy*xz - yz*xx, detZ);
        }
        return Vector3D.Normalize(res);
      }

      void _updateWheels() {
        double comPos = _getCenterOfMass();
        if(_strafing) {
          foreach(PowerWheel wheel in _wheels) {
            wheel.Strafe(comPos);
          }
        } else {
          foreach(PowerWheel wheel in _wheels) {
            wheel.Turn(comPos);
          }
        }
        foreach (var wheel in _wheels) {
          wheel.Power = _power;
          wheel.Steer = _steer;
          wheel.Roll(_roll);
        }
        _updateStrength();
      }

      void _updateStrength() {
        float defaultStrength = _getDefaultStrength();
        foreach(var wheel in _wheels) {
          float strength = defaultStrength * _getCalibratedMult(wheel.Position.Z);
          if (Math.Abs(wheel.Strength - strength) > 0.01) {
            wheel.Strength = strength;
          }
        }
      }

      float _getCalibratedMult(double z) {
        float ratioMult = _high ? TARGET_RATIO_HIGH / TARGET_RATIO : 1;
        if (_minCalibration == null) {
          return ratioMult;
        } else if (_maxCalibration == null) {
          return _minCalibration.Value.GetMult(z) * ratioMult;
        } else {
          var minC = _minCalibration.Value;
          var maxC = _maxCalibration.Value;
          return MathHelper.Lerp(minC.GetMult(z), maxC.GetMult(z), (_getShipWeight() - minC.Weight) / (maxC.Weight - minC.Weight)) * ratioMult;
        }
      }

      float _getDefaultStrength() => (float)Math.Sqrt(_getShipWeight() * STRENGTH_MULT / _wheels.Count);

      double _getCenterOfMass() => _transformer.Pos(_controller.CenterOfMass).Z;

      float _getShipWeight() => (float)_controller.GetNaturalGravity().Length() * (_controller.CalculateShipMass().PhysicalMass - _wheels.Sum(w => w.Mass));
    }
  }
}