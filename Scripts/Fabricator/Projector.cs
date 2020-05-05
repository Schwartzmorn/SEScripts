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

    public enum ProjectorStatus { Retracted, Deploying, Retracting, Deployed, Projecting };
    public class Projector {
      readonly IMyDoor door;
      readonly IMyPistonBase piston;
      readonly IMyProjector projector;
      Action<string, string> logger;
      readonly Process mainProcess;
      readonly List<IMyMechanicalConnectionBlock> mechs = new List<IMyMechanicalConnectionBlock>();

      public Projector(IMyDoor door, IMyPistonBase piston, IMyProjector projector, IProcessSpawner spawner, Action<string, string> logger) {
        this.door = door;
        this.piston = piston;
        this.projector = projector;
        this.mainProcess = spawner.Spawn(null, "projector-process");
        this.logger = logger;

        if (this.door.Status == DoorStatus.Closing || this.piston.Velocity < 0) {
          this.Retract();
        } else if (this.door.Status == DoorStatus.Opening || this.piston.Velocity > 0) {
          this.Deploy();
        }
      }

      public void Deploy() {
        this.mainProcess.KillChildren();
        Process p = this.mainProcess.Spawn(null, "deploy", period: 30);
        p.Spawn(this.openDoor, "open-door", period: 5);
      }

      public void Retract() {
        this.mainProcess.KillChildren();
        if (!this.HasBlocksAttached()) {
          Process p = this.mainProcess.Spawn(null, "retract", period: 30);
          p.Spawn(this.lowerPiston, "lower-piston", period: 5);
        } else {
          this.log("Cannot retract: blocks attached");
        }
      }

      public bool HasBlocksAttached() {
        Vector3I bounds = this.projector.CubeGrid.Max - this.projector.CubeGrid.Min;
        return bounds.X > 2 || bounds.Y > 3 || bounds.Z > 2;
      }

      public float GetCompletion() {
        return (float)(this.projector.TotalBlocks - this.projector.RemainingBlocks) / this.projector.TotalBlocks;
      }

      public void AttachEverything(IMyGridTerminalSystem gts) {
        this.mechs.Clear();
        gts.GetBlocksOfType(this.mechs, w => w.CubeGrid == this.projector.CubeGrid);
        foreach (IMyMotorSuspension wheel in this.mechs.Where(m => !m.IsAttached)) {
          wheel.ApplyAction("Add Top Part");
        }
      }

      public ProjectorStatus GetStatus() {
        if (this.piston.CurrentPosition < 0.05f) {
          return ProjectorStatus.Retracted;
        } else if (this.piston.MaxLimit - this.piston.CurrentPosition < 0.05f) {
          return this.projector.IsProjecting
              ? ProjectorStatus.Projecting
              : ProjectorStatus.Deployed;
        } else {
          return this.piston.Velocity < 0
              ? ProjectorStatus.Retracting
              : ProjectorStatus.Deploying;
        }
      }

      void openDoor(Process p) {
        this.door.Enabled = true;
        this.door.OpenDoor();
        if (this.door.OpenRatio == 1) {
          p.Done();
          this.door.Enabled = false;
          p.Parent.Spawn(this.raisePiston, "raise-piston");
        }
      }

      void raisePiston(Process p) {
        p.Parent.Done();
        this.move(1);
        if (this.piston.MaxLimit - this.piston.CurrentPosition < 0.05) {
          p.Done();
          this.projector.Enabled = true;
          this.move(0);
        }
      }

      void lowerPiston(Process p) {
        this.piston.Enabled = true;
        this.projector.Enabled = false;
        this.move(-1);
        if (this.piston.CurrentPosition < 0.05) {
          p.Done();
          this.move(0);
          p.Parent.Spawn(this.closeDoor, "close-piston");
        }
      }

      void move(float velocity) {
        this.piston.Enabled = velocity != 0;
        this.piston.Velocity = velocity;
      }

      void closeDoor(Process p) {
        p.Parent.Done();
        this.door.Enabled = true;
        this.door.CloseDoor();
        if (this.door.OpenRatio == 0) {
          p.Done();
          this.door.Enabled = false;
        }
      }

      void log(string s) => this.logger(s, "proj");
    }
  }
}
