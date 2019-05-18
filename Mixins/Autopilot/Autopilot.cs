using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
  partial class Program {
    public class Autopilot: IAHDeactivator {
      static readonly Vector3D PLANE = new Vector3D(1, 0, 1);
      static readonly Vector3D FORWARD = new Vector3D(0, 0, -1);
      static readonly Vector3D RIGHT = new Vector3D(1, 0, 0);

      bool _activated = true;
      readonly WPNetwork _network;
      readonly IMyRemoteControl _remote;
      readonly APSettings _settings = new APSettings();
      readonly CoordsTransformer _transformer;
      readonly WheelsController _wheels;

      List<APWaypoint> _curPath = new List<APWaypoint>();

      public Autopilot(WheelsController wheels, CmdLine command, IMyRemoteControl remote) {
        _network = new WPNetwork(remote);
        _remote = remote;
        _transformer = new CoordsTransformer(remote, true);
        _wheels = wheels;

        command.AddCmd(new Cmd("ap-goto", "Go to the waypoint", s => GoTo(s[0]), minArgs: 1, maxArgs: 1));
        command.AddCmd(new Cmd("ap-switch", "Switches the autopilot on/off", _ => Switch(), maxArgs: 0));

        Scheduler.Inst.AddAction(new ScheduledAction(Handle, period: 10));
      }

      public bool ShouldDeactivate() => _activated;

      public void Switch() {
        _activated = !_activated;
        _wheels.SetPower(0);
        _wheels.SetSteer(0);
      }

      public void Handle() {
        if (!_activated) {
          return;
        }
        if (_curPath.Count > 0) {
          APWaypoint nextWP = _curPath.Count > 1 ? _curPath[_curPath.Count - 2] : null;

          if (_goToWaypoint(_curPath.Last(), nextWP)) {
            _curPath.Pop();
          }
        }
        if (_curPath.Count == 0) {
          _stop();
        }
      }

      public void GoTo(string wpName) {
        APWaypoint end = _network.GetWP(wpName);
        if (end == null) {
          _log($"Could not find waypoint {wpName}");
        }
        _network.GetPath(_remote.GetPosition(), end, _curPath);
        _log($"Path: {_curPath.Count}");
      }

      bool _goToWaypoint(APWaypoint wp, APWaypoint nextWP) {
        Vector3D route = _transformer.Pos(wp.WP.Coords) * PLANE;
        double routeLength = route.Length();
        if (_settings.IsWaypointReached(wp, routeLength)) {
          return true;
        }
        _remote.HandBrake = false;
        double angle = Math.Acos(Vector3D.Normalize(route).Dot(FORWARD));
        bool reverseDir = angle > Math.PI / 2;
        if (reverseDir) {
          angle = -(angle - Math.PI);
        }
        angle *= (route.Dot(RIGHT) > 0) ? 1 : -1;
        _log($"going to {wp.WP.Name}");

        double curSpeed = _remote.GetShipSpeed() * (_isReversing() ? -1 : 1);

        double wpSpeed = _settings.GetTargetSpeed(wp);
        double nextWPSpeed = nextWP == null ? 0 : _settings.GetTargetSpeed(nextWP);
        double nextTurn = nextWP == null ? 0 : Math.Acos(Vector3D.Normalize(_transformer.Dir(nextWP.WP.Coords - wp.WP.Coords)).Dot(Vector3D.Normalize(route)));

        double targetSpeed = _settings.GetTargetSpeed(wpSpeed, nextWPSpeed, routeLength, nextTurn) * (reverseDir ? -1 : 1);

        _wheels.SetPower(_getPower(targetSpeed, curSpeed));

        _wheels.SetSteer(_settings.GetSteer(angle, curSpeed));

        return false;
      }

      float _getPower(double targetSpeed, double currentSpeed) {
        return MathHelper.Clamp((float)(targetSpeed - currentSpeed) * _settings.PowerMult, -1, 1);
      }

      void _stop() {
        _wheels.SetSteer(0);
        double curSpeed = _remote.GetShipSpeed();
        if(curSpeed > _settings.HandbrakeSpeed) {
          _wheels.SetPower(_isReversing() ? _settings.BrakePower : -_settings.BrakePower);
          _log($"stopping");
        } else {
          _remote.HandBrake = true;
        }
      }

      private bool _isReversing() => _transformer.Dir(_remote.GetShipVelocities().LinearVelocity).Dot(FORWARD) < 0;

      private void _log(string s) => Logger.Inst.Log($"AP: {s}");
    }
  }
}
