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

    const string SECTION = "wheels-controller";
    // Empirically determined to have a compression ratio of 0.35
    const float STRENGTH_MULT = 0.00086f;
    const float TARGET_RATIO = 0.35f;
    const float TARGET_RATIO_HIGH = 0.05f;

    readonly WheelBase _base = new WheelBase();
    Calibration? _cal = null;
    readonly IMyShipController _cont;
    bool _high = false;
    float _power = 0;
    float _roll = 0;
    float _steer = 0;
    bool _strafing = false;
    readonly List<PowerWheel> _wheels;
    readonly CoordsTransformer _tform;

    public WheelsController(MyIni ini, MyGridProgram program, CmdLine cmd, CoordsTransformer transformer, IMyShipController controller) {
      _tform = transformer;
      _cont = controller;
      var wheels = new List<IMyMotorSuspension>();
      program.GridTerminalSystem.GetBlocksOfType(wheels, w => w.CubeGrid == program.Me.CubeGrid && w.DisplayNameText.Contains("Power"));
      _wheels = wheels.Select(w => new PowerWheel(w, _base, transformer)).ToList();
      _wheels.Sort((w1, w2) => Math.Sign(w1.Position.Z - w2.Position.Z)); // needed for calibration

      cmd.AddCmd(new Cmd("wc-calibrate", "Calibrates the suspensions", _ => Calibrate(), nArgs: 0));
      cmd.AddCmd(new Cmd("wc-position", "Changes between normal and high position", s => SetPosition(s[0]),
          nArgs: 1, detailedHelp: "Possible values:\n  high\n  normal\n  switch"));
      cmd.AddCmd(new Cmd("wc-power", "Set power override", s => SetPower(s[0]), nArgs: 1));
      cmd.AddCmd(new Cmd("wc-reset", "Delete all suspension calibrations", _ => Reset(), nArgs: 0));
      cmd.AddCmd(new Cmd("wc-roll", "Set roll override", s => SetRoll(float.Parse(s[0])), nArgs: 1));
      cmd.AddCmd(new Cmd("wc-steer", "Set steer override", s => SetSteer(s[0]), nArgs: 1));
      cmd.AddCmd(new Cmd("wc-strafe", "Activates or deactivates straffing", s => SetStrafing(s[0]),
          nArgs: 1, detailedHelp: "Possible values:\n  on\n  off\n  switch"));
      Schedule(new ScheduledAction(() => _updateWheels(), 10, name: "wc-handle"));
      ScheduleOnSave(Save);

      if(ini.ContainsKey(SECTION, "cal-weight"))
        _cal = new Calibration() {
          Weight = ini.Get(SECTION, "cal-weight").ToSingle(),
          MinZ = ini.Get(SECTION, "cal-min-z").ToDouble(),
          MaxZ = ini.Get(SECTION, "cal-max-z").ToDouble(),
          MinMult = ini.Get(SECTION, "cal-min-mult").ToSingle(),
          MaxMult = ini.Get(SECTION, "cal-max-mult").ToSingle()
        };
      _high = ini.Get(SECTION, "is-high").ToBoolean();
    }

    public void SetStrafing(string arg) {
      _strafing = arg == "on" ? true : arg == "off" ? false : !_strafing;
      _updateWheels();
    }

    public void SetPosition(string arg) {
      _high = arg == "high" ? true : arg == "normal" ? false : !_high;
      _updateWheels();
    }

    public void SetPower(float power) => _power = power;
    public void SetPower(string power) => float.TryParse(power, out _power);

    public void SetSteer(float steer) => _steer = steer;
    public void SetSteer(string steer) => float.TryParse(steer, out _steer);

    public void SetRoll(float roll) => _roll = roll;

    public void Reset() => _cal = null;

    public void Calibrate() {
      PowerWheel minW = _wheels[0], maxW = _wheels.Last();
      double minZ = minW.Position.Z, maxZ = maxW.Position.Z;
      float target = _high ? TARGET_RATIO_HIGH : TARGET_RATIO;
      _cal = new Calibration() {
        Weight = _getShipWeight(),
        MaxZ = maxZ,
        MinZ = minZ,
        MaxMult = (float)Math.Sqrt(Math.Max(maxW.GetCompressionRatio(), 0) / target) * _getCalibratedMult(maxZ),
        MinMult = (float)Math.Sqrt(Math.Max(minW.GetCompressionRatio(), 0) / target) * _getCalibratedMult(minZ),
      };
    }

    public void Save(MyIni ini) {
      ini.Set(SECTION, "is-high", _high);
      if (_cal != null) {
        var c = _cal.Value;
        ini.Set(SECTION, "cal-weight", c.Weight);
        ini.Set(SECTION, "cal-min-z", c.MinZ);
        ini.Set(SECTION, "cal-max-z", c.MaxZ);
        ini.Set(SECTION, "cal-min-mult", c.MinMult);
        ini.Set(SECTION, "cal-max-mult", c.MaxMult);
      }
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
      double resO = _tform.Pos(res).Z - _base.MinZ;
      return res + resO*frontDir;
    }

    // returns normal to the plane of wheel contact with the ground
    public Vector3D GetContactPlaneW() {
      var center = new Vector3D(0, 0, 0);
      // todo? take into account compression ratio not to look at wheel not making contact
      _wheels.ForEach(w => center += w.GetPointOfContactW());
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
      double detX = yy*zz - yz*yz, detY = xx * zz - xz * xz, detZ = xx * yy - xy * xy;
      double detMax = Math.Max(detX, Math.Max(detY, detZ));

      Vector3D res;
      if (detMax == detX)
        res = new Vector3D(detX, xz*yz - xy*zz, xy*yz - xz*yy);
      else if (detMax == detY)
        res = new Vector3D(xz*yz - xy*zz, detY, xy*xz - yz*xx);
      else
        res = new Vector3D(xy*yz - xz*yy, xy*xz - yz*xx, detZ);

      return Vector3D.Normalize(res);
    }

    void _updateWheels() {
      double comPos = _getCenterOfMass();
      if(_strafing)
        foreach(PowerWheel wheel in _wheels)
          wheel.Strafe(comPos);
      else
        foreach(PowerWheel wheel in _wheels)
          wheel.Turn(comPos);
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
        if (Math.Abs(wheel.Strength - strength) > 0.01)
          wheel.Strength = strength;
      }
    }

    float _getCalibratedMult(double z) {
      float ratioMult = _high ? TARGET_RATIO_HIGH / TARGET_RATIO : 1;
      return _cal == null ? ratioMult : _cal.Value.GetMult(z) * ratioMult;
    }

    float _getDefaultStrength() => (float)Math.Sqrt(_getShipWeight() * STRENGTH_MULT / _wheels.Count);

    double _getCenterOfMass() => _tform.Pos(_cont.CenterOfMass).Z;

    float _getShipWeight() => (float)_cont.GetNaturalGravity().Length() * (_cont.CalculateShipMass().PhysicalMass - _wheels.Sum(w => w.Mass));
  }
}
}