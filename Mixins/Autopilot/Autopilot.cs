using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

namespace IngameScript
{
  partial class Program
  {
    /// <summary>Entry point for the Autopilot</summary>
    public class Autopilot : IPADeactivator
    {
      static readonly Vector3D PLANE = new Vector3D(1, 0, 1);
      static readonly Vector3D FORWARD = new Vector3D(0, 0, -1);
      static readonly Vector3D RIGHT = new Vector3D(1, 0, 0);

      public readonly WPNetwork Network;

      bool _activated;
      readonly Action<string> _logger;
      readonly IMyRemoteControl _remote;
      readonly APSettings _settings = new APSettings();
      readonly CoordinatesTransformer _transformer;
      readonly WheelsController _wheels;
      readonly List<APWaypoint> _currentPath = new List<APWaypoint>();
      /// <summary>Creates a new Autopilot</summary>
      /// <param name="ini"></param>
      /// <param name="wheels"></param>
      /// <param name="cmd"></param>
      /// <param name="remote"></param>
      /// <param name="logger"></param>
      /// <param name="manager"></param>
      public Autopilot(MyIni ini, WheelsController wheels, CommandLine cmd, IMyRemoteControl remote, Action<string> logger, ISaveManager manager)
      {
        _activated = ini.Get("auto-pilot", "activated").ToBoolean();
        _logger = logger;

        Process p = manager.Spawn(_handle, "ap-handle");

        Network = new WPNetwork(remote, logger, p);
        _remote = remote;
        _transformer = new CoordinatesTransformer(remote, p);
        _wheels = wheels;

        cmd.RegisterCommand(new Command("ap-move", Command.Wrap(_move), "Move forward", minArgs: 1, maxArgs: 2));
        cmd.RegisterCommand(new Command("ap-goto", Command.Wrap(GoTo), "Go to the waypoint", nArgs: 1));
        cmd.RegisterCommand(new Command("ap-switch", Command.Wrap(Switch), "Switches the autopilot on/off", nArgs: 1));
        cmd.RegisterCommand(new Command("ap-save", Command.Wrap(Save), "Save the current position", nArgs: 1));
        manager.AddOnSave(_save);
      }

      public bool ShouldDeactivate() => _activated;

      public void Switch(string s)
      {
        _activated = s == "switch" ? !_activated : s == "on";
        _logger?.Invoke($"Autopilot switch {(_activated ? "on" : "off")}");
        if (!_activated)
        {
          _currentPath.Clear();
        }
        _wheels.SetPower(0);
        _wheels.SetSteer(0);
      }
      /// <summary>Requests the Autopilot to go to a waypoint</summary>
      /// <param name="wpName">Name of the waypoint to reach</param>
      public void GoTo(string wpName)
      {
        if (_activated)
        {
          APWaypoint end = Network.GetWaypoint(wpName);
          if (end == null)
          {
            _log($"Could not find waypoint {wpName}");
          }
          else
          {
            Network.GetPath(_remote.GetPosition(), end, _currentPath);
            _log($"Path: {_currentPath.Count}");
          }
        }
      }
      /// <summary>Requests the autopilot to move</summary>
      /// <param name="amtForward">Amount of forward movement</param>
      /// <param name="amtRight">Amount of movement to the right</param>
      public void Move(double amtForward, double amtRight)
      {
        if (_activated)
        {
          _currentPath.Clear();
          Vector3D tgt = _remote.GetPosition() + (amtForward * _remote.WorldMatrix.Forward) + (amtRight * _remote.WorldMatrix.Right);
          _currentPath.Add(new APWaypoint(new MyWaypointInfo("$TMPMOVE", tgt), Terrain.Dangerous, WPType.Maneuvering));
        }
      }
      /// <summary>Saves the current position of the remote as a new waypoint (or updates an existing one)</summary>
      /// <param name="name">Name of the waypoint</param>
      public void Save(string name) => Network.Add(new MyWaypointInfo(name, _remote.GetPosition()));

      void _move(List<string> args)
      {
        double fw, rt = 0;
        double.TryParse(args[0], out fw);
        if (args.Count > 1)
        {
          double.TryParse(args[0], out rt);
        }
        Move(fw, rt);
      }

      void _handle(Process p)
      {
        if (!_activated)
        {
          return;
        }
        if (_currentPath.Count > 0)
        {
          APWaypoint nextWP = _currentPath.Count > 1 ? _currentPath[_currentPath.Count - 2] : null;
          if (_goToWaypoint(_currentPath.Last(), nextWP))
          {
            _currentPath.Pop();
          }
        }
        if (_currentPath.Count == 0)
        {
          _stop();
        }
      }

      bool _goToWaypoint(APWaypoint wp, APWaypoint nextWP)
      {
        Vector3D route = _transformer.Pos(wp.WP.Coords) * PLANE;
        double routeLength = route.Length();
        if (_settings.IsWaypointReached(wp, routeLength))
        {
          return true;
        }
        _remote.HandBrake = false;
        double angle = Math.Acos(Vector3D.Normalize(route).Dot(FORWARD));
        bool reverseDir = angle > Math.PI / 2;
        if (reverseDir)
        {
          angle = -(angle - Math.PI);
        }
        angle *= (route.Dot(RIGHT) > 0) ? 1 : -1;

        double curSpeed = _remote.GetShipSpeed() * (Reversing ? -1 : 1);

        double targetSpeed = _settings.GetTargetSpeed(_settings.GetTargetSpeed(wp), _settings.GetTargetSpeed(nextWP), routeLength) * (reverseDir ? -1 : 1);

        _wheels.SetPower(_getPower(targetSpeed, curSpeed));

        _wheels.SetSteer(_settings.GetSteer(angle, curSpeed));

        return false;
      }

      float _getPower(double targetSpeed, double currentSpeed) => MathHelper.Clamp((float)(targetSpeed - currentSpeed) * _settings.PowerMult, -1, 1);

      void _stop()
      {
        _wheels.SetSteer(0);
        double curSpeed = _remote.GetShipSpeed();
        if (curSpeed > _settings.HandbrakeSpeed)
        {
          _wheels.SetPower(Reversing ? _settings.BrakePower : -_settings.BrakePower);
        }
        else
        {
          _remote.HandBrake = true;
        }
      }

      bool Reversing => _transformer.Dir(_remote.GetShipVelocities().LinearVelocity).Dot(FORWARD) < 0;

      void _save(MyIni ini) => ini.Set("auto-pilot", "activated", _activated);

      void _log(string s) => _logger?.Invoke($"AP: {s}");
    }
  }
}
