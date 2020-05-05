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
  partial class Program {

    public enum WelderStatus { Retracted, Deploying, Retracting, Deployed, Welding }
    public class Welder {
      readonly IEnumerable<IMyLightingBlock> lights;
      readonly IEnumerable<IMyPistonBase> pistons;
      readonly IMySensorBlock sensor;
      readonly IEnumerable<IMyShipWelder> welders;
      readonly Process mainProcess;
      readonly int positionMultiplier;

      public float CurrentPosition => this.pistons.Average(p => p.CurrentPosition) * this.positionMultiplier;

      public Welder(IEnumerable<IMyPistonBase> pistons, IMySensorBlock sensor, IEnumerable<IMyShipWelder> welders, IEnumerable<IMyLightingBlock> lights, int positionMultiplier, IProcessSpawner spawner) {
        this.lights = lights;
        this.pistons = pistons;
        this.positionMultiplier = positionMultiplier;
        this.sensor = sensor;
        this.welders = welders;
        this.mainProcess = spawner.Spawn(null, "welder-process");
        this.sensor.Enabled = false;
        if (this.pistons.First().Velocity > 0) {
          this.Deploy();
        } else if (this.welders.First().Enabled) {
          this.move(0);
        } else if (this.pistons.First().Velocity < 0) {
          this.Retract();
        }
      }

      public void Deploy() {
        this.mainProcess.KillChildren();
        this.sensor.Enabled = true;
        this.switchWelder(false);
        this.mainProcess.Spawn(this.extendPistons, "extend-pistons", p => this.sensor.Enabled = false, period: 5);
      }

      public void Retract() {
        this.mainProcess.KillChildren();
        this.switchWelder(false);
        this.mainProcess.Spawn(this.retractPistons, "retract-pistons", period: 30);
      }

      public void Step() {
        this.mainProcess.KillChildren();
        this.switchWelder(true);
        float startPosition = this.CurrentPosition; // need to do it this way
        this.mainProcess.Spawn(p => this.step(p, startPosition), "step");
      }

      public WelderStatus GetStatus() {
        if (this.CurrentPosition < 0.05f) {
          return WelderStatus.Retracted;
        } else if (this.welders.First().Enabled) {
          return this.pistons.First().Velocity < 0
            ? WelderStatus.Welding
            : WelderStatus.Deployed;
        } else if (this.sensor.IsActive || this.pistons.All(p => p.MaxLimit - p.CurrentPosition < 0.05f)) {
          return WelderStatus.Deployed;
        } else {
          return this.pistons.First().Velocity < 0
              ? WelderStatus.Retracting
              : WelderStatus.Deploying;
        }
      }

      void extendPistons(Process p) {
        if (this.sensor.IsActive || this.pistons.All(piston => piston.MaxLimit - piston.CurrentPosition < 0.05)) {
          p.Done();
          this.move(0);
        } else {
          this.move(2);
        }
      }

      void retractPistons(Process p) {
        if (this.CurrentPosition < 0.05) {
          p.Done();
          this.move(0);
        } else {
          this.move(-2);
        }
      }

      void move(float velocity) {
        this.switchLights(velocity != 0);
        foreach (IMyPistonBase piston in this.pistons) {
          piston.Enabled = velocity != 0;
          piston.Velocity = velocity / this.positionMultiplier;
        }
      }

      void switchWelder(bool enabled) {
        foreach (IMyShipWelder welder in this.welders) {
          welder.Enabled = enabled;
        }
      }
      void switchLights(bool enabled) {
        foreach (IMyLightingBlock light in this.lights) {
          light.Enabled = enabled;
        }
      }

      void step(Process p, float startPosition) {
        if (this.CurrentPosition < 0.05 || startPosition - this.CurrentPosition > 0.5f) {
          p.Done();
          this.move(0);
          this.switchLights(true);
        } else {
          this.move(-0.5f);
        }
      }
    }
  }
}
