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

        public float GetMult(double z) => MathHelper.Lerp(this.MinMult, this.MaxMult, (float)((z - this.MinZ) / (this.MaxZ - this.MinZ)));
      }

      const string SECTION = "wheels-controller";

      public readonly WheelBase WheelBase = new WheelBase();
      Calibration? calibration = null;
      readonly IMyShipController controller;
      float targetRatio;
      float power = 0;
      float roll = 0;
      float steer = 0;
      bool strafing = false;
      readonly CoordinatesTransformer transformer;
      readonly List<PowerWheel> wheels;

      /// <summary>Class that allow some high level control over the wheels. To be controlled, the wheels must be on the same grid than <paramref name="controller"/> and contain the keyword "Power"</summary>
      /// <param name="command">Command line where the commands will be registered</param>
      /// <param name="controller">Controller on the same grid than the wheels to get some physics information</param>
      /// <param name="gts">To retrieve the wheels</param>
      /// <param name="ini">Contains some serialized information for persistence</param>
      /// <param name="manager">To spawn the updater process</param>
      /// <param name="transformer">Transformer that transform world coordinates into the vehicules coordinate: Z must be parallel to the vehicle's forward direction an Y must be parallel to the vehicle's up direction</param>
      public WheelsController(CommandLine command, IMyShipController controller, IMyGridTerminalSystem gts, MyIni ini, ISaveManager manager, CoordinatesTransformer transformer) {
        this.transformer = transformer;
        this.controller = controller;
        var wheels = new List<IMyMotorSuspension>();
        gts.GetBlocksOfType(wheels, w => w.CubeGrid == controller.CubeGrid && w.DisplayNameText.Contains("Power"));
        this.wheels = wheels.Select(w => new PowerWheel(w, this.WheelBase, transformer)).ToList();
        this.wheels.Sort((w1, w2) => Math.Sign(w1.Position.Z - w2.Position.Z)); // needed for calibration

        this.registerCommands(command);
        manager.Spawn(this.updateWheels, "wheels-controller", period: 10);
        manager.AddOnSave(this.save);

        if (ini.ContainsKey(SECTION, "cal-weight")) {
          this.calibration = new Calibration() {
            Weight = ini.Get(SECTION, "cal-weight").ToSingle(),
            MinZ = ini.Get(SECTION, "cal-min-z").ToDouble(),
            MaxZ = ini.Get(SECTION, "cal-max-z").ToDouble(),
            MinMult = ini.Get(SECTION, "cal-min-mult").ToSingle(),
            MaxMult = ini.Get(SECTION, "cal-max-mult").ToSingle()
          };
        }

        this.targetRatio = ini.Get(SECTION, "target-ratio").ToSingle(0.35f);
        this.WheelBase.CenterOfTurnZOffset = ini.Get(SECTION, "turn-center-offset").ToDouble();
        this.WheelBase.TurnRadiusOverride = ini.Get(SECTION, "turn-radius").ToDouble();
      }

      /// <summary>Adjusts the suspensions to have the vehicle more level with the ground. Should be activated on level ground</summary>
      public void Calibrate() {
        PowerWheel minW = this.wheels[0], maxW = this.wheels.Last();
        double minZ = minW.Position.Z, maxZ = maxW.Position.Z;
        float weightPerWheel = this.getShipWeight() / this.wheels.Count;
        this.calibration = new Calibration() {
          Weight = this.getShipWeight(), // not yet used
          MaxZ = maxZ,
          MinZ = minZ,
          MaxMult = maxW.GetForce() / weightPerWheel,
          MinMult = minW.GetForce() / weightPerWheel,
        };
      }

      /// <summary>Computes the plane of contact of the wheel with the ground</summary>
      /// <returns>Normal to the plane, in world coordinates</returns>
      public Vector3D GetContactPlaneW() {
        Vector3D center = Vector3D.Zero;
        // todo? take into account compression ratio not to look at wheel not making contact
        this.wheels.ForEach(w => center += w.GetPointOfContactW());
        center /= this.wheels.Count;
        double xx = 0, xy = 0, xz = 0, yy = 0, yz = 0, zz = 0;
        foreach (PowerWheel w in this.wheels) {
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
      public Vector3D GetPointOfContactW(Vector3D frontDir) {
        Vector3D frontPoc = Vector3D.Zero;
        int count = 0;
        foreach (PowerWheel w in this.wheels) {
          if (w.Position.Z < this.WheelBase.CenterOfTurnZ + 0.2) {
            frontPoc += w.GetPointOfContactW();
            ++count;
          }
        }
        Vector3D res = frontPoc / count;
        double resO = this.transformer.Pos(res).Z - this.WheelBase.MinZ;
        return res + (resO * frontDir);
      }

      /// <summary>Resets all the calibrations done to the vehicle's suspensions</summary>
      public void Reset() => this.calibration = null;

      /// <summary>Changes the stance of the vehicle(high or normal)</summary>
      /// <param name="arg">the compression ratio</param>
      public void SetPosition(string arg) {
        this.targetRatio = MathHelper.Clamp(float.Parse(arg), 0, 1);
        this.updateWheels(null);
      }

      /// <summary>Overrides the power the wheels provide.</summary>
      /// <param name="power">Amount of power: positive values to go forward, negative to go backward, 0 to deactivate override</param>
      public void SetPower(float power) => this.power = power;

      /// <summary>Makes the vehicle roll</summary>
      /// <param name="roll">Amount of roll, positive values to roll right, negative to roll left, or "left", "right", "reset"</param>
      public void SetRoll(string roll) {
        if (roll == "left") {
          this.roll -= 0.1f;
        } else if (roll == "right") {
          this.roll += 0.1f;
        } else {
          this.roll = roll == "reset" ? 0 : float.Parse(roll);
        }
        this.roll = MathHelper.Clamp(this.roll, -0.4f, 0.4f);
      }

      /// <summary>Overrides the steering</summary>
      /// <param name="steer">Amount of steering, positive values to turn right, negative to turn left,  0 to deactivate override</param>
      public void SetSteer(float steer) => this.steer = steer;

      /// <summary>Activates / deactivates strafing (all wheels remaining parallel)</summary>
      /// <param name="arg">"on", "off" or "switch"</param>
      public void SetStrafing(string arg) {
        this.strafing = arg == "on" ? true : arg == "off" ? false : !this.strafing;
        this.updateWheels(null);
      }

      public float GetAverageCompressionRatio() => this.wheels.Average(w => w.GetCompressionRatio());

      float getCalibratedWeight(float defaultWeight, double positionZ) {
        if (this.calibration == null) {
          return defaultWeight;
        } else {
          Calibration calibration = this.calibration.Value;
          double relPos = (positionZ - calibration.MinZ) / (calibration.MaxZ - calibration.MinZ);
          return defaultWeight * MathHelper.Lerp(calibration.MinMult, calibration.MaxMult, (float)relPos);
        }
      }

      double getCenterOfMass() => this.transformer.Pos(this.controller.CenterOfMass).Z;

      float getShipWeight() => (float)this.controller.GetNaturalGravity().Length() * (this.controller.CalculateShipMass().PhysicalMass - this.wheels.Sum(w => w.Mass));

      void registerCommands(CommandLine command) {
        command.RegisterCommand(new Command("wc-calibrate", Command.Wrap(this.Calibrate), "Calibrates the suspensions", nArgs: 0));
        command.RegisterCommand(new Command("wc-position", Command.Wrap(this.SetPosition), "Changes the position on the wheels",
                                detailedHelp: "From 0 (no compression) to 1 (completely compressed)", nArgs: 1));
        command.RegisterCommand(new Command("wc-power", Command.Wrap(args => this.SetPower(float.Parse(args[0]))), "Set power override", nArgs: 1));
        command.RegisterCommand(new Command("wc-reset", Command.Wrap(this.Reset), "Delete all suspension calibrations", nArgs: 0));
        command.RegisterCommand(new Command("wc-roll", Command.Wrap(this.SetRoll), "Set roll override", nArgs: 1));
        command.RegisterCommand(new Command("wc-steer", Command.Wrap(args => this.SetSteer(float.Parse(args[0]))), "Set power override", nArgs: 1));
        command.RegisterCommand(new Command("wc-strafe", Command.Wrap(this.SetStrafing), "Set power override",
                                detailedHelp: "Possible values:\n  on\n  off\n  switch", nArgs: 1));
        command.RegisterCommand(new Command("wc-turn-radius", Command.Wrap(this.turnRadius), "Overrides the turn radius", nArgs: 1));
        command.RegisterCommand(new Command("wc-turn-center", Command.Wrap(this.turnCenter), "Offsets the turn center", nArgs: 1));
      }

      void turnCenter(string arg) => this.WheelBase.CenterOfTurnZOffset = double.Parse(arg);

      void turnRadius(string arg) => this.WheelBase.TurnRadiusOverride = double.Parse(arg);

      void save(MyIni ini) {
        ini.Set(SECTION, "target-ratio", this.targetRatio);
        ini.Set(SECTION, "turn-center-offset", this.WheelBase.CenterOfTurnZOffset);
        ini.Set(SECTION, "turn-radius", this.WheelBase.TurnRadiusOverride);
        if (this.calibration != null) {
          Calibration c = this.calibration.Value;
          ini.Set(SECTION, "cal-weight", c.Weight);
          ini.Set(SECTION, "cal-min-z", c.MinZ);
          ini.Set(SECTION, "cal-max-z", c.MaxZ);
          ini.Set(SECTION, "cal-min-mult", c.MinMult);
          ini.Set(SECTION, "cal-max-mult", c.MaxMult);
        }
      }

      void updateStrength() {
        float defaultWeight = this.getShipWeight() / this.wheels.Count;
        foreach (PowerWheel wheel in this.wheels) {
          wheel.SetStrength(getCalibratedWeight(defaultWeight, wheel.Position.Z), this.targetRatio);
        }
      }

      void updateWheels(Process p) {
        double comPos = this.getCenterOfMass();
        if (this.strafing) {
          foreach (PowerWheel wheel in this.wheels) {
            wheel.Strafe(comPos);
          }
        } else {
          foreach (PowerWheel wheel in this.wheels) {
            wheel.Turn(comPos);
          }
        }
        if (this.controller.GetShipSpeed() > 4) {
          this.roll = 0;
        }
        foreach (PowerWheel wheel in this.wheels) {
          wheel.Power = this.power;
          wheel.Steer = this.steer;
          wheel.Roll(this.roll);
        }
        this.updateStrength();
      }
    }
  }
}