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

    public class WheelsController
    {
      public struct Calibration
      {
        public float Weight;
        public double MinZ;
        public double MaxZ;
        public float MinMult;
        public float MaxMult;

        public float GetMult(double z) => MathHelper.Lerp(MinMult, MaxMult, (float)((z - MinZ) / (MaxZ - MinZ)));
      }

      const string SECTION = "wheels-controller";

      public readonly WheelBase WheelBase = new WheelBase();
      Calibration? _calibration = null;
      readonly IMyShipController _controller;
      float _targetRatio;
      float _power = 0;
      float _roll = 0;
      float _steer = 0;
      bool _strafing = false;
      readonly CoordinatesTransformer _transformer;
      readonly List<PowerWheel> _wheels;

      /// <summary>Class that allow some high level control over the wheels. To be controlled, the wheels must be on the same grid than <paramref name="controller"/> and contain the keyword "Power"</summary>
      /// <param name="command">Command line where the commands will be registered</param>
      /// <param name="controller">Controller on the same grid than the wheels to get some physics information</param>
      /// <param name="gts">To retrieve the wheels</param>
      /// <param name="ini">Contains some serialized information for persistence</param>
      /// <param name="manager">To spawn the updater process</param>
      /// <param name="transformer">Transformer that transform world coordinates into the vehicules coordinate: Z must be parallel to the vehicle's forward direction an Y must be parallel to the vehicle's up direction</param>
      public WheelsController(CommandLine command, IMyShipController controller, IMyGridTerminalSystem gts, MyIni ini, ISaveManager manager, CoordinatesTransformer transformer)
      {
        _transformer = transformer;
        _controller = controller;
        var wheels = new List<IMyMotorSuspension>();
        gts.GetBlocksOfType(wheels, w => w.CubeGrid == controller.CubeGrid && w.CustomName.Contains("Power"));
        _wheels = wheels.Select(w => new PowerWheel(w, WheelBase, transformer)).ToList();
        _wheels.Sort((w1, w2) => Math.Sign(w1.Position.Z - w2.Position.Z)); // needed for calibration

        _registerCommands(command);
        manager.Spawn(_updateWheels, "wheels-controller", period: 10);
        manager.AddOnSave(_save);

        if (ini.ContainsKey(SECTION, "cal-weight"))
        {
          _calibration = new Calibration()
          {
            Weight = ini.Get(SECTION, "cal-weight").ToSingle(),
            MinZ = ini.Get(SECTION, "cal-min-z").ToDouble(),
            MaxZ = ini.Get(SECTION, "cal-max-z").ToDouble(),
            MinMult = ini.Get(SECTION, "cal-min-mult").ToSingle(),
            MaxMult = ini.Get(SECTION, "cal-max-mult").ToSingle()
          };
        }

        _targetRatio = ini.Get(SECTION, "target-ratio").ToSingle(0.35f);
        WheelBase.CenterOfTurnZOffset = ini.Get(SECTION, "turn-center-offset").ToDouble();
        WheelBase.TurnRadiusOverride = ini.Get(SECTION, "turn-radius").ToDouble();
      }

      /// <summary>Adjusts the suspensions to have the vehicle more level with the ground. Should be activated on level ground</summary>
      public void Calibrate()
      {
        PowerWheel minW = _wheels[0], maxW = _wheels.Last();
        double minZ = minW.Position.Z, maxZ = maxW.Position.Z;
        float weightPerWheel = _getShipWeight() / _wheels.Count;
        _calibration = new Calibration()
        {
          Weight = _getShipWeight(), // not yet used
          MaxZ = maxZ,
          MinZ = minZ,
          MaxMult = maxW.GetForce() / weightPerWheel,
          MinMult = minW.GetForce() / weightPerWheel,
        };
      }

      /// <summary>Computes the plane of contact of the wheel with the ground</summary>
      /// <returns>Normal to the plane, in world coordinates</returns>
      public Vector3D GetContactPlaneW()
      {
        Vector3D center = Vector3D.Zero;
        // todo? take into account compression ratio not to look at wheel not making contact
        _wheels.ForEach(w => center += w.GetPointOfContactW());
        center /= _wheels.Count;
        double xx = 0, xy = 0, xz = 0, yy = 0, yz = 0, zz = 0;
        foreach (PowerWheel w in _wheels)
        {
          Vector3D r = w.GetPointOfContactW() - center;
          xx += r.X * r.X;
          xy += r.X * r.Y;
          xz += r.X * r.Z;
          yy += r.Y * r.Y;
          yz += r.Y * r.Z;
          zz += r.Z * r.Z;
        }
        double detX = (yy * zz) - (yz * yz);
        double detY = (xx * zz) - (xz * xz);
        double detZ = (xx * yy) - (xy * xy);
        double detMax = Math.Max(detX, Math.Max(detY, detZ));

        Vector3D res = detMax == detX
          ? new Vector3D(detX, (xz * yz) - (xy * zz), (xy * yz) - (xz * yy))
          : detMax == detY
            ? new Vector3D((xz * yz) - (xy * zz), detY, (xy * xz) - (yz * xx))
            : new Vector3D((xy * yz) - (xz * yy), (xy * xz) - (yz * xx), detZ);
        return Vector3D.Normalize(res);
      }

      /// <summary>Computes the point of contact of the frontmost wheels with the ground</summary>
      /// <returns>The point of contact, in world coordinates</returns>
      public Vector3D GetPointOfContactW(Vector3D frontDir)
      {
        Vector3D frontPoc = Vector3D.Zero;
        int count = 0;
        foreach (PowerWheel w in _wheels)
        {
          if (w.Position.Z < WheelBase.CenterOfTurnZ + 0.2)
          {
            frontPoc += w.GetPointOfContactW();
            ++count;
          }
        }
        Vector3D res = frontPoc / count;
        double resO = _transformer.Pos(res).Z - WheelBase.MinZ;
        return res + (resO * frontDir);
      }

      /// <summary>Resets all the calibrations done to the vehicle's suspensions</summary>
      public void Reset() => _calibration = null;

      /// <summary>Changes the stance of the vehicle(high or normal)</summary>
      /// <param name="arg">the compression ratio</param>
      public void SetPosition(string arg)
      {
        _targetRatio = MathHelper.Clamp(float.Parse(arg), 0, 1);
        _updateWheels(null);
      }

      /// <summary>Overrides the power the wheels provide.</summary>
      /// <param name="power">Amount of power: positive values to go forward, negative to go backward, 0 to deactivate override</param>
      public void SetPower(float power) => _power = power;

      /// <summary>Makes the vehicle roll</summary>
      /// <param name="roll">Amount of roll, positive values to roll right, negative to roll left, or "left", "right", "reset"</param>
      public void SetRoll(string roll)
      {
        if (roll == "left")
        {
          _roll -= 0.1f;
        }
        else if (roll == "right")
        {
          _roll += 0.1f;
        }
        else
        {
          _roll = roll == "reset" ? 0 : float.Parse(roll);
        }
        _roll = MathHelper.Clamp(_roll, -0.4f, 0.4f);
      }

      /// <summary>Overrides the steering</summary>
      /// <param name="steer">Amount of steering, positive values to turn right, negative to turn left,  0 to deactivate override</param>
      public void SetSteer(float steer) => _steer = steer;

      /// <summary>Activates / deactivates strafing (all wheels remaining parallel)</summary>
      /// <param name="arg">"on", "off" or "switch"</param>
      public void SetStrafing(string arg)
      {
        _strafing = arg == "on" ? true : arg == "off" ? false : !_strafing;
        _updateWheels(null);
      }

      public bool IsStrafing => _strafing;

      public float GetAverageCompressionRatio() => _wheels.Average(w => w.GetCompressionRatio());

      float _getCalibratedWeight(float defaultWeight, double positionZ)
      {
        if (_calibration == null)
        {
          return defaultWeight;
        }
        else
        {
          Calibration calibration = _calibration.Value;
          double relPos = (positionZ - calibration.MinZ) / (calibration.MaxZ - calibration.MinZ);
          return defaultWeight * MathHelper.Lerp(calibration.MinMult, calibration.MaxMult, (float)relPos);
        }
      }

      double _getCenterOfMass() => _transformer.Pos(_controller.CenterOfMass).Z;

      float _getShipWeight() => (float)_controller.GetNaturalGravity().Length() * (_controller.CalculateShipMass().PhysicalMass - _wheels.Sum(w => w.Mass));

      void _registerCommands(CommandLine command)
      {
        command.RegisterCommand(new ParentCommand("wc", "Interacts with the wheel controller")
          .AddSubCommand(new Command("calibrate", Command.Wrap(Calibrate), "Calibrates the suspensions", nArgs: 0))
          .AddSubCommand(new Command("position", Command.Wrap(SetPosition), "Changes the position on the wheels\nFrom 0 (no compression) to 1 (completely compressed)", nArgs: 1))
          .AddSubCommand(new Command("power", Command.Wrap(args => SetPower(float.Parse(args[0]))), "Set power override", nArgs: 1))
          .AddSubCommand(new Command("reset", Command.Wrap(Reset), "Delete all suspension calibrations", nArgs: 0))
          .AddSubCommand(new Command("roll", Command.Wrap(SetRoll), "Set roll override", nArgs: 1))
          .AddSubCommand(new Command("steer", Command.Wrap(args => SetSteer(float.Parse(args[0]))), "Set power override", nArgs: 1))
          .AddSubCommand(new Command("strafe", Command.Wrap(SetStrafing), "Set power override\nPossible values:\n  on\n  off\n  switch", nArgs: 1))
          .AddSubCommand(new Command("turn-radius", Command.Wrap(_turnRadius), "Overrides the turn radius", nArgs: 1))
          .AddSubCommand(new Command("turn-center", Command.Wrap(_turnCenter), "Offsets the turn center", nArgs: 1)));
      }

      void _turnCenter(string arg) => WheelBase.CenterOfTurnZOffset = double.Parse(arg);

      void _turnRadius(string arg) => WheelBase.TurnRadiusOverride = double.Parse(arg);

      void _save(MyIni ini)
      {
        ini.Set(SECTION, "target-ratio", _targetRatio);
        ini.Set(SECTION, "turn-center-offset", WheelBase.CenterOfTurnZOffset);
        ini.Set(SECTION, "turn-radius", WheelBase.TurnRadiusOverride);
        if (_calibration != null)
        {
          Calibration c = _calibration.Value;
          ini.Set(SECTION, "cal-weight", c.Weight);
          ini.Set(SECTION, "cal-min-z", c.MinZ);
          ini.Set(SECTION, "cal-max-z", c.MaxZ);
          ini.Set(SECTION, "cal-min-mult", c.MinMult);
          ini.Set(SECTION, "cal-max-mult", c.MaxMult);
        }
      }

      void _updateStrength()
      {
        float defaultWeight = _getShipWeight() / _wheels.Count;
        foreach (PowerWheel wheel in _wheels)
        {
          wheel.SetStrength(_getCalibratedWeight(defaultWeight, wheel.Position.Z), _targetRatio);
        }
      }

      void _updateWheels(Process p)
      {
        double comPos = _getCenterOfMass();
        if (_strafing)
        {
          foreach (PowerWheel wheel in _wheels)
          {
            wheel.Strafe(comPos);
          }
        }
        else
        {
          foreach (PowerWheel wheel in _wheels)
          {
            wheel.Turn(comPos);
          }
        }
        if (_controller.GetShipSpeed() > 4)
        {
          _roll = 0;
        }
        foreach (PowerWheel wheel in _wheels)
        {
          wheel.Power = _power;
          wheel.Steer = _steer;
          wheel.Roll(_roll);
        }
        _updateStrength();
      }
    }
  }
}
