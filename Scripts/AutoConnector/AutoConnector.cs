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
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
  partial class Program {
    public class AutoConnector {
      public static readonly string IniConnectorPrefix = "connector-";
      private static readonly string INI_STAGE = "initialization-stage";
      private static readonly Vector3D LEFT = new Vector3D(1, 0, 0);
      private static readonly Vector3D UP = new Vector3D(0, 1, 0);
      private static readonly Vector3D FORWARD = new Vector3D(0, 0, 1);
      private static readonly double CONNECTION_OFFSET_SMALL = 1;
      private static readonly double CONNECTION_OFFSET_LARGE = 1.75;
      private static readonly double CONNECTION_SAFETY_OFFSET = 2;
      private static readonly double DELTA_TARGET = 0.05;
      // moving parts
      private readonly IMyShipConnector _downConnector;
      private readonly IMyShipConnector _frontConnector;
      private readonly Actuator _x, _y, _z;
      private readonly IMyMotorStator _stator;
      // state
      private int _initializationStage = 0;
      private Vector3D _max;
      private Vector3D _min;
      private Vector3D _restPosition;
      public readonly string Name;
      private readonly CircularBuffer<Waypoint> _waypoints = new CircularBuffer<Waypoint>(10);
      // in case of wild disconnection, this should ensure we position the correct block and don't flail
      private ConnectionType _lastConnectionType = ConnectionType.None;
      // position
      private Vector3D _currentPosition;
      private Vector3D _currentOrientation;
      // helpers
      private readonly VectorTransformer _posTransformer;
      private readonly VectorTransformer _orientationTransformer;

      public AutoConnector(
          string stationName,
          string sectionName,
          MyGridProgram program,
          VectorTransformer posTransformer,
          VectorTransformer orientationTransformer,
          MyIni ini)
          : this(stationName, sectionName.Substring(IniConnectorPrefix.Length), program, posTransformer, orientationTransformer) {
        // Parse ini
        _log($"parsing configuration");
        _initializationStage = ini.GetThrow(sectionName, INI_STAGE).ToInt32();
        _log($"at stage {_initializationStage} ({(IsInitialized() ? "" : "not ")}initialized)");
        if(_initializationStage > 1) {
          _max = ini.GetVector(sectionName, "max");
        }
        if(_initializationStage > 3) {
          _min = ini.GetVector(sectionName, "min");
          _initRestPosition();
        }
        _deserializeWaypoints(sectionName, ini);
      }

      public AutoConnector(
          string stationName,
          string name,
          MyGridProgram program,
          VectorTransformer posTransformer,
          VectorTransformer orientationTransformer) {
        var gts = program.GridTerminalSystem;
        Name = name;
        _log($"initialization");
        // initialize moving parts
        _initMandatoryField(gts, $"{stationName} Connector {Name} Down", out _downConnector);
        _initMandatoryField(gts, $"{stationName} Connector {Name} Front", out _frontConnector);
        _initMandatoryField(gts, $"{stationName} Rotor {Name}", out _stator);
        _posTransformer = posTransformer;
        _orientationTransformer = orientationTransformer;
        string pistonPrefix = $"{stationName} Piston {Name}";
        var pistons = new List<IMyPistonBase>();
        gts.GetBlocksOfType(pistons, p => p.DisplayNameText.StartsWith(pistonPrefix));
        _x = new Actuator(4);
        _y = new Actuator(1);
        _z = new Actuator(2);
        foreach(var piston in pistons) {
          var up = _orientationTransformer(piston.WorldMatrix.Up);
          var dot = FORWARD.Dot(up);
          if(Math.Abs(dot) > 0.95) {
            bool isNegative = dot < 0;
            piston.CustomName = pistonPrefix + " " + (isNegative ? "-" : "") + "Z";
            _z.AddPiston(piston, isNegative);
            continue;
          }
          dot = LEFT.Dot(up);
          if(Math.Abs(dot) > 0.95) {
            bool isNegative = dot < 0;
            piston.CustomName = pistonPrefix + " " + (isNegative ? "-" : "") + "X";
            _x.AddPiston(piston, isNegative);
            continue;
          }
          dot = UP.Dot(up);
          if(Math.Abs(dot) > 0.95) {
            bool isNegative = dot < 0;
            piston.CustomName = pistonPrefix + " " + (isNegative ? "-" : "") + "Y";
            _y.AddPiston(piston, isNegative);
            continue;
          }
          _log($"Could not place piston '{piston.DisplayNameText}'");
        }
        if(!_x.IsValid) {
          throw new InvalidOperationException($"Connector '{Name}': no piston on axis X");
        }
        if(!_y.IsValid) {
          throw new InvalidOperationException($"Connector '{Name}': no piston on axis Y");
        }
        if(!_z.IsValid) {
          throw new InvalidOperationException($"Connector '{Name}': no piston on axis Z");
        }
        if(_frontConnector.Status == MyShipConnectorStatus.Connected) {
          _lastConnectionType = ConnectionType.Front;
        } else if(_downConnector.Status == MyShipConnectorStatus.Connected) {
          _lastConnectionType = ConnectionType.Down;
        }
      }

      public bool IsInitialized() => _initializationStage >= 4;

      public bool IsMoving() => _waypoints.Count != 0;

      public bool IsConnected() => _getCurrentConnected() != null;

      private void _initialize() {
        bool hasReachedNextStep = true;
        if(_initializationStage == 0) {
          hasReachedNextStep &= _x.Move(10);
          hasReachedNextStep &= _y.Move(10);
          hasReachedNextStep &= _z.Move(10);
          if(hasReachedNextStep) {
            _max = _posTransformer(_stator.GetPosition());
          }
        } else if(_initializationStage == 1) {
          hasReachedNextStep &= _x.Move(-10);
          hasReachedNextStep &= _z.Move(-10);
        } else if(_initializationStage == 2) {
          hasReachedNextStep &= _y.Move(-10);
          if(hasReachedNextStep) {
            _min = _posTransformer(_stator.GetPosition());
            _initRestPosition();
          }
        } else if(_initializationStage == 3) {
          hasReachedNextStep &= _y.Move(10);
        }
        if(hasReachedNextStep) {
          ++_initializationStage;
          _log($"is now at initialization step {_initializationStage}");
        }
      }

      public void Save(MyIni ini) {
        var sectionName = IniConnectorPrefix + Name;
        ini.Set(sectionName, INI_STAGE, _initializationStage);
        if(_max != null) {
          ini.SetVector(sectionName, "max", _max);
        }
        if(_min != null) {
          ini.SetVector(sectionName, "min", _min);
        }
        int i = 1;
        foreach(var waypoint in _waypoints) {
          var prefix = $"waypoint-{i}";
          ini.SetVector(sectionName, prefix, waypoint.Position);
          if(waypoint.Angle != 0) {
            ini.Set(sectionName, $"{prefix}-angle", waypoint.Angle);
          }
          if(waypoint.Connection != ConnectionType.None) {
            ini.Set(sectionName, $"{prefix}-connect", waypoint.Connection.ToString());
          }
          if(waypoint.NeedPrecision) {
            ini.Set(sectionName, $"{prefix}-precise", waypoint.NeedPrecision);
          }
          ++i;
        }
      }

      public double GetDistance(Vector3D position) => IsInitialized()
          ? GetDistance(_min.X, _max.X, position.X)
              + GetDistance(_min.Y, _max.Y, position.Y)
              + GetDistance(_min.Z, _max.Z, position.Z)
          : double.MaxValue;

      public void Connect(MyCubeSize size, Vector3D position, Vector3D orientation) {
        double offset = (size == MyCubeSize.Small) ? CONNECTION_OFFSET_SMALL : CONNECTION_OFFSET_LARGE;
        Vector3D finalTarget = position + (offset * orientation);
        _queueConnectionRoute(finalTarget, orientation);
      }

      public void Disconnect() {
        _waypoints.Clear();
        var connector = _getCurrentConnected()
            ?? _getConnector(_lastConnectionType)
            ?? _frontConnector;
        Vector3D curPos = _posTransformer(connector.GetPosition());
        Vector3D curOrientation = _orientationTransformer(connector.WorldMatrix.Backward);
        connector.Disconnect();
        Vector3D safetyTarget = curPos + (curOrientation * CONNECTION_SAFETY_OFFSET);
        _waypoints.Enqueue(new Waypoint(
            new Vector3D(safetyTarget.X, safetyTarget.Y, safetyTarget.Z),
            _stator.Angle,
            needPrecision: true
          )).Enqueue(new Waypoint(
            new Vector3D(safetyTarget.X, _max.Y, safetyTarget.Z)
          )).Enqueue(new Waypoint(
            _restPosition
          ));
      }

      // returns true if it is updating
      public bool Update() {
        if(IsInitialized()) {
          if(_waypoints.Count > 0) {
            var block = _getPositionBlock();
            _currentPosition = _posTransformer(block.GetPosition());
            _currentOrientation = _frontConnector.WorldMatrix.Forward;
            var isReached = _goToWaypoint(_waypoints.Peek());
            if(isReached) {
              var waypoint = _waypoints.Dequeue();
              _log($"reached a waypoint, {_waypoints.Count} to go");
              _x.Stop();
              _y.Stop();
              _z.Stop();
              _stator.TargetVelocityRad = 0;

              // We only try to connect on the last point
              if(_waypoints.Count == 0) {
                _getConnector(waypoint.Connection)?.Connect();
              }
              _lastConnectionType = waypoint.Connection;
            }
          }
          return _waypoints.Count != 0;
        } else {
          _initialize();
        }
        return false;
      }

      public double GetRemainingLength() {
        double length = 0;
        Vector3D curPos = _currentPosition;
        foreach(var waypoint in _waypoints) {
          Vector3D delta = curPos - waypoint.Position;
          // because of the way pistons move, we take the max of the distance
          double dist = Math.Max(Math.Max(Math.Abs(delta.X), Math.Abs(delta.Y)), Math.Abs(delta.Z));
          if(waypoint.NeedPrecision) {
            dist *= 2;
          }
          length += dist;
        }
        return length;
      }

      private void _queueConnectionRoute(Vector3D target, Vector3D orientation) {
        _waypoints.Clear();
        var connectionType = Math.Abs(orientation.Dot(UP)) > 0.8 ? ConnectionType.Down : ConnectionType.Front;
        float angle = connectionType == ConnectionType.Front ? _getTargetAngle(orientation) : 0;
        var con = _getCurrentConnected();
        var curPos = _posTransformer((con ?? _stator as IMyTerminalBlock).GetPosition());
        ConnectionType firstType = ConnectionType.None;
        if(con != null) {
          con.Disconnect();
          var firstPos = curPos + (_orientationTransformer(con.WorldMatrix.Backward) * CONNECTION_SAFETY_OFFSET);
          // we give a connection type to have the correct connector positioned
          _waypoints.Enqueue(new Waypoint(
            firstPos,
            angle: _stator.Angle,
            needPrecision: true
          ));
          firstType = con == _frontConnector ? ConnectionType.Front : ConnectionType.Down;
          curPos = firstPos;
        }
        Vector3D safetyTarget = target + (orientation * CONNECTION_SAFETY_OFFSET);
        // we give a connection type to have the correct connector positioned for the first points
        _waypoints.Enqueue(new Waypoint(
            new Vector3D(curPos.X, _max.Y, curPos.Z),
            connection: firstType
          )).Enqueue(new Waypoint(
            new Vector3D(safetyTarget.X, _max.Y, safetyTarget.Z)
          )).Enqueue(new Waypoint(
            new Vector3D(safetyTarget.X, _max.Y - 3, safetyTarget.Z)
          )).Enqueue(new Waypoint(
            new Vector3D(safetyTarget.X, safetyTarget.Y, safetyTarget.Z),
            angle
          )).Enqueue(new Waypoint(
            new Vector3D(target.X, target.Y, target.Z),
            angle,
            connectionType,
            needPrecision: true
          ));
      }

      private bool _goToWaypoint(Waypoint waypoint) {
        if(waypoint.Connection == ConnectionType.None) {
          _downConnector.Disconnect();
          _frontConnector.Disconnect();
        }
        bool isReached = true;
        isReached &= _x.Move(waypoint.Position.X - _currentPosition.X, waypoint.NeedPrecision);
        isReached &= _y.Move(waypoint.Position.Y - _currentPosition.Y, waypoint.NeedPrecision);
        isReached &= _z.Move(waypoint.Position.Z - _currentPosition.Z, waypoint.NeedPrecision);
        isReached &= _moveRotor(AngleProxy(_stator.Angle, waypoint.Angle), waypoint.NeedPrecision);
        return isReached;
      }

      private void _deserializeWaypoints(string sectionName, MyIni ini) {
        int waypointNumber = 1;
        while(true) {
          string prefix = $"waypoint-{waypointNumber++}";
          if(ini.ContainsKey(sectionName, $"{prefix}-x")) {
            _waypoints.Enqueue(new Waypoint(
                ini.GetVector(sectionName, prefix),
                ini.Get(sectionName, $"{prefix}-angle").ToSingle(0),
                (ConnectionType)Enum.Parse(
                    typeof(ConnectionType),
                    ini.Get(sectionName, $"{prefix}-connect").ToString("None")),
                ini.Get(sectionName, $"{prefix}-precise").ToBoolean(false)
              ));
          } else {
            break;
          }
        }
        _log($"has {_waypoints.Count} pending waypoints");
      }

      private void _initMandatoryField<T>(IMyGridTerminalSystem gts, string name, out T field) where T : class, IMyTerminalBlock {
        field = gts.GetBlockWithName(name) as T;
        if(field == null) {
          throw new InvalidOperationException($"Connector '{Name}': could not find {typeof(T)} '{name}'");
        }
      }

      private IMyTerminalBlock _getPositionBlock() {
        ConnectionType type = _waypoints
            .Select(w => w.Connection)
            .FirstOrDefault(c => c != ConnectionType.None);
        return _getConnector(type) ?? (_getConnector(_lastConnectionType) ?? _stator as IMyTerminalBlock);
      }

      private float _getTargetAngle(Vector3D targetOrientation) {
        return (float)Mod(_getAngle(targetOrientation) - _getAngle(_orientationTransformer(_frontConnector.WorldMatrix.Backward))
            + _stator.Angle, Math.PI * 2);
      }

      private float _getAngle(Vector3D orientation) {
        // project orientation on normal plane of Up
        var proj = Vector3.Normalize((orientation.Dot(LEFT) * LEFT) + (orientation.Dot(FORWARD) * FORWARD));
        float angle = (float)Math.Acos(proj.Dot(FORWARD)) + MathHelper.Pi;
        bool invert = proj.Cross(FORWARD).Dot(UP) < 0;
        return invert ? angle : -angle;
      }

      private bool _moveRotor(float delta, bool needPrecision) {
        _stator.TargetVelocityRad = MathHelper.Clamp(delta * (needPrecision ? 1 : 4), -1, +1);
        return Math.Abs(delta) < DELTA_TARGET;
      }

      private void _initRestPosition() => _restPosition = new Vector3D(
          (_min.X + _max.X) / 2,
          _max.Y,
          (_min.Z + _max.Z) / 2);

      private void _log(string log) => Logger.Inst.Log($"Connector {Name}: {log}");

      private IMyShipConnector _getCurrentConnected() {
        return _frontConnector.Status == MyShipConnectorStatus.Connected
          ? _frontConnector
          : _downConnector.Status == MyShipConnectorStatus.Connected
            ? _downConnector : null;
      }

      private IMyShipConnector _getConnector(ConnectionType type) {
        if(type == ConnectionType.Down) {
          return _downConnector;
        } else if(type == ConnectionType.Front) {
          return _frontConnector;
        }
        return null;
      }

      private static double GetDistance(double min, double max, double pos) {
        if(pos < min) {
          return min - pos;
        } else if(pos < max) {
          return 0;
        } else {
          return pos - max;
        }
      }

      private static float AngleProxy(float A1, float A2) {
        A1 = A2 - A1;
        A1 = (float)Mod((double)A1 + Math.PI, 2 * Math.PI) - (float)Math.PI;
        return A1;
      }

      private static double Mod(double A, double N) => A - (Math.Floor(A / N) * N);
    }
  }
}
