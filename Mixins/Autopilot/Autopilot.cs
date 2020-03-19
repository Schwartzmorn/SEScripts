using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

namespace IngameScript {
  partial class Program {
    /// <summary>Entry point for the Autopilot</summary>
    public class Autopilot : IPADeactivator {
      static readonly Vector3D PLANE = new Vector3D(1, 0, 1);
      static readonly Vector3D FORWARD = new Vector3D(0, 0, -1);
      static readonly Vector3D RIGHT = new Vector3D(1, 0, 0);

      public readonly WPNetwork Network;

      bool activated;
      readonly Action<string> logger;
      readonly IMyRemoteControl remote;
      readonly APSettings settings = new APSettings();
      readonly CoordinatesTransformer transformer;
      readonly WheelsController wheels;
      readonly List<APWaypoint> currentPath = new List<APWaypoint>();
      /// <summary>Creates a new Autopilot</summary>
      /// <param name="ini"></param>
      /// <param name="wheels"></param>
      /// <param name="cmd"></param>
      /// <param name="remote"></param>
      /// <param name="logger"></param>
      /// <param name="manager"></param>
      public Autopilot(MyIni ini, WheelsController wheels, CommandLine cmd, IMyRemoteControl remote, Action<string> logger, ISaveManager manager) {
        this.activated = ini.Get("auto-pilot", "activated").ToBoolean();
        this.logger = logger;

        Process p = manager.Spawn(this.handle, "ap-handle");

        this.Network = new WPNetwork(remote, logger, p);
        this.remote = remote;
        this.transformer = new CoordinatesTransformer(remote, p);
        this.wheels = wheels;

        cmd.RegisterCommand(new Command("ap-move", Command.Wrap(this.move), "Move forward", minArgs: 1, maxArgs: 2));
        cmd.RegisterCommand(new Command("ap-goto", Command.Wrap(this.GoTo), "Go to the waypoint", nArgs: 1));
        cmd.RegisterCommand(new Command("ap-switch", Command.Wrap(this.Switch), "Switches the autopilot on/off", nArgs: 1));
        cmd.RegisterCommand(new Command("ap-save", Command.Wrap(this.Save), "Save the current position", nArgs: 1));
        manager.AddOnSave(this.save);
      }

      public bool ShouldDeactivate() => this.activated;

      public void Switch(string s) {
        this.activated = s == "switch" ? !this.activated : s == "on";
        this.logger?.Invoke($"Autopilot switch {(this.activated ? "on" : "off")}");
        if (!this.activated) {
          this.currentPath.Clear();
        }
        this.wheels.SetPower(0);
        this.wheels.SetSteer(0);
      }
      /// <summary>Requests the Autopilot to go to a waypoint</summary>
      /// <param name="wpName">Name of the waypoint to reach</param>
      public void GoTo(string wpName) {
        if (this.activated) {
          APWaypoint end = this.Network.GetWaypoint(wpName);
          if (end == null) {
            this.log($"Could not find waypoint {wpName}");
          }  else {
            this.Network.GetPath(this.remote.GetPosition(), end, this.currentPath);
            this.log($"Path: {this.currentPath.Count}");
          }
        }
      }
      /// <summary>Requests the autopilot to move</summary>
      /// <param name="amtForward">Amount of forward movement</param>
      /// <param name="amtRight">Amount of movement to the right</param>
      public void Move(double amtForward, double amtRight) {
        if (this.activated) {
          this.currentPath.Clear();
          Vector3D tgt = this.remote.GetPosition() + (amtForward * this.remote.WorldMatrix.Forward) + (amtRight * this.remote.WorldMatrix.Right);
          this.currentPath.Add(new APWaypoint(new MyWaypointInfo("$TMPMOVE", tgt), Terrain.Dangerous, WPType.Maneuvering));
        }
      }
      /// <summary>Saves the current position of the remote as a new waypoint (or updates an existing one)</summary>
      /// <param name="name">Name of the waypoint</param>
      public void Save(string name) => this.Network.Add(new MyWaypointInfo(name, this.remote.GetPosition()));

      void move(List<string> args) {
        double fw, rt = 0;
        double.TryParse(args[0], out fw);
        if (args.Count > 1) {
          double.TryParse(args[0], out rt);
        }
        this.Move(fw, rt);
      }

      void handle(Process p) {
        if (!this.activated) {
          return;
        }
        if (this.currentPath.Count > 0) {
          APWaypoint nextWP = this.currentPath.Count > 1 ? this.currentPath[this.currentPath.Count - 2] : null;
          if (this.goToWaypoint(this.currentPath.Last(), nextWP)) {
            this.currentPath.Pop();
          }
        }
        if (this.currentPath.Count == 0) {
          this.stop();
        }
      }

      bool goToWaypoint(APWaypoint wp, APWaypoint nextWP) {
        Vector3D route = this.transformer.Pos(wp.WP.Coords) * PLANE;
        double routeLength = route.Length();
        if (this.settings.IsWaypointReached(wp, routeLength)) {
          return true;
        }
        this.remote.HandBrake = false;
        double angle = Math.Acos(Vector3D.Normalize(route).Dot(FORWARD));
        bool reverseDir = angle > Math.PI / 2;
        if (reverseDir) {
          angle = -(angle - Math.PI);
        }
        angle *= (route.Dot(RIGHT) > 0) ? 1 : -1;

        double curSpeed = this.remote.GetShipSpeed() * (this.Reversing ? -1 : 1);

        double targetSpeed = this.settings.GetTargetSpeed(this.settings.GetTargetSpeed(wp), this.settings.GetTargetSpeed(nextWP), routeLength) * (reverseDir ? -1 : 1);

        this.wheels.SetPower(this.getPower(targetSpeed, curSpeed));

        this.wheels.SetSteer(this.settings.GetSteer(angle, curSpeed));

        return false;
      }

      float getPower(double targetSpeed, double currentSpeed) => MathHelper.Clamp((float)(targetSpeed - currentSpeed) * this.settings.PowerMult, -1, 1);

      void stop() {
        this.wheels.SetSteer(0);
        double curSpeed = this.remote.GetShipSpeed();
        if (curSpeed > this.settings.HandbrakeSpeed) {
          this.wheels.SetPower(this.Reversing ? this.settings.BrakePower : -this.settings.BrakePower);
        } else {
          this.remote.HandBrake = true;
        }
      }

      bool Reversing => this.transformer.Dir(this.remote.GetShipVelocities().LinearVelocity).Dot(FORWARD) < 0;

      void save(MyIni ini) => ini.Set("auto-pilot", "activated", this.activated);

      void log(string s) => this.logger?.Invoke($"AP: {s}");
    }
  }
}
