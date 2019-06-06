using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

namespace IngameScript {
partial class Program {
  public class Autopilot: JobProvider, IPADeactivator {
    static readonly Vector3D PLANE = new Vector3D(1, 0, 1);
    static readonly Vector3D FORWARD = new Vector3D(0, 0, -1);
    static readonly Vector3D RIGHT = new Vector3D(1, 0, 0);

    public readonly WPNetwork Network;

    bool _activated;
    readonly IMyRemoteControl _remote;
    readonly APSettings _settings = new APSettings();
    readonly CoordsTransformer _tform;
    readonly WheelsController _wheels;

    List<APWaypoint> _curPath = new List<APWaypoint>();

    public Autopilot(Ini ini, WheelsController wheels, CmdLine cmd, IMyRemoteControl remote) {
      _activated = ini.Get("auto-pilot", "activated").ToBoolean();
      Network = new WPNetwork(remote);
      _remote = remote;
      _tform = new CoordsTransformer(remote, true);
      _wheels = wheels;

      cmd.AddCmd(new Cmd("ap-move", "Move forward", (s, c) => StartJob(_move, s, c), minArgs: 1, maxArgs: 2));
      cmd.AddCmd(new Cmd("ap-goto", "Go to the waypoint", (s, c) => StartJob(GoTo, s[0], c), nArgs: 1));
      cmd.AddCmd(new Cmd("ap-switch", "Switches the autopilot on/off", s => Switch(s[0]), nArgs: 1));
      cmd.AddCmd(new Cmd("ap-save", "Save the current position", s => Save(s[0]), nArgs: 1));

      Schedule(new ScheduledAction(_handle, name: "ap-handle"));
      ScheduleOnSave(_save);
    }

    public bool ShouldDeactivate() => _activated;

    public void Switch(string s) {
      _activated = s == "switch" ? !_activated : s == "on";
      Log($"Autopilot switch {(_activated ? "on" : "off")}");
      if(!_activated) {
        CancelCallback();
        _curPath.Clear();
      }
      _wheels.SetPower(0);
      _wheels.SetSteer(0);
    }

    public void GoTo(string wpName) {
      if(_activated) {
        APWaypoint end = Network.GetWP(wpName);
        if(end == null)
          _log($"Could not find waypoint {wpName}");
        Network.GetPath(_remote.GetPosition(), end, _curPath);
        _log($"Path: {_curPath.Count}");
      } else
        CancelCallback();
    }

    public void Move(double amtForward, double amtRight) {
      if(_activated) {
        _curPath.Clear();
        var tgt = _remote.GetPosition() + (amtForward * _remote.WorldMatrix.Forward) + (amtRight * _remote.WorldMatrix.Right);
        _curPath.Add(new APWaypoint(new MyWaypointInfo("$TMPMOVE", tgt), Terrain.Dangerous, WPType.Maneuvering));
      } else
        CancelCallback();
    }

    public void Save(string name) => Network.Add(new MyWaypointInfo(name, _remote.GetPosition()));

    void _move(List<string> args) {
      double fw, rt = 0;
      double.TryParse(args[0], out fw);
      if (args.Count > 1)
        double.TryParse(args[0], out rt);
      Move(fw, rt);
    }

    void _handle() {
      if(!_activated)
        return;
      if(_curPath.Count > 0) {
        APWaypoint nextWP = _curPath.Count > 1 ? _curPath[_curPath.Count - 2] : null;
        if(_goToWaypoint(_curPath.Last(), nextWP))
          _curPath.Pop();
      }
      if(_curPath.Count == 0) {
        StopCallback("Destination reached");
        _stop();
      }
    }

    bool _goToWaypoint(APWaypoint wp, APWaypoint nextWP) {
      Vector3D route = _tform.Pos(wp.WP.Coords) * PLANE;
      double routeLength = route.Length();
      if (_settings.IsWaypointReached(wp, routeLength))
        return true;
      _remote.HandBrake = false;
      double angle = Math.Acos(Vector3D.Normalize(route).Dot(FORWARD));
      bool reverseDir = angle > Math.PI / 2;
      if (reverseDir)
        angle = -(angle - Math.PI);
      angle *= (route.Dot(RIGHT) > 0) ? 1 : -1;

      double curSpeed = _remote.GetShipSpeed() * (_reversing ? -1 : 1);

      double targetSpeed = _settings.GetTargetSpeed(_settings.GetTargetSpeed(wp), _settings.GetTargetSpeed(nextWP), routeLength) * (reverseDir ? -1 : 1);

      _wheels.SetPower(_getPower(targetSpeed, curSpeed));

      _wheels.SetSteer(_settings.GetSteer(angle, curSpeed));

      return false;
    }

    float _getPower(double targetSpeed, double currentSpeed) => MathHelper.Clamp((float)(targetSpeed - currentSpeed) * _settings.PowerMult, -1, 1);

    void _stop() {
      _wheels.SetSteer(0);
      double curSpeed = _remote.GetShipSpeed();
      if(curSpeed > _settings.HandbrakeSpeed)
        _wheels.SetPower(_reversing ? _settings.BrakePower : -_settings.BrakePower);
      else
        _remote.HandBrake = true;
    }

    bool _reversing => _tform.Dir(_remote.GetShipVelocities().LinearVelocity).Dot(FORWARD) < 0;

    void _save(MyIni ini) => ini.Set("auto-pilot", "activated", _activated);

    void _log(string s) => Log($"AP: {s}");
  }
}
}
