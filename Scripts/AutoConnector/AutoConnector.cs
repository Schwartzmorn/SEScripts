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

namespace IngameScript
{
  partial class Program
  {
    /// <summary>This class is in charge of actuating the pistons, rotors and connectors</summary>
    public class AutoConnector
    {
      public static readonly string INI_CONNECTOR_PREFIX = "connector-";
      static readonly string INI_STAGE = "initialization-stage";
      static readonly Vector3D LEFT = new Vector3D(1, 0, 0);
      static readonly Vector3D UP = new Vector3D(0, 1, 0);
      static readonly Vector3D FORWARD = new Vector3D(0, 0, 1);
      static readonly double CONNECTION_OFFSET_SMALL = 0.5;
      static readonly double CONNECTION_OFFSET_LARGE = 1.25;
      static readonly double CONNECTION_SAFETY_OFFSET = 2;
      static readonly double DELTA_TARGET = 0.05;
      // moving parts
      readonly IMyShipConnector _downConnector;
      readonly IMyShipConnector _frontConnector;
      readonly Actuator _x, _y, _z;
      readonly IMyMotorStator _stator;
      readonly List<IMyLightingBlock> _lights = new List<IMyLightingBlock>();
      // state
      int _initializationStage = 0;
      Vector3D _max;
      Vector3D _min;
      Vector3D _restPosition;
      public readonly string Name;
      readonly CircularBuffer<Waypoint> _waypoints = new CircularBuffer<Waypoint>(10);
      // in case of wild disconnection, this should ensure we position the correct block and don't flail
      ConnectionType _lastConnectionType = ConnectionType.None;
      // position
      Vector3D _currentPosition;
      // helpers
      readonly Action<string> _logger;
      readonly CoordinatesTransformer _transformer;
      /// <summary>
      /// Re creates a serialized auto connector
      /// </summary>
      /// <param name="stationName"></param>
      /// <param name="sectionName"></param>
      /// <param name="program"></param>
      /// <param name="posTransformer"></param>
      /// <param name="orientationTransformer"></param>
      /// <param name="ini"></param>
      public AutoConnector(
          string stationName,
          string sectionName,
          MyGridProgram program,
          Action<string> logger,
          CoordinatesTransformer transformer,
          MyIni ini)
          : this(stationName, sectionName.Substring(INI_CONNECTOR_PREFIX.Length), program, logger, transformer)
      {
        // Parse ini
        _log($"parsing configuration");
        _initializationStage = ini.GetThrow(sectionName, INI_STAGE).ToInt32();
        _log($"at stage {_initializationStage} ({(IsInitialized() ? "" : "not ")}initialized)");
        if (_initializationStage > 1)
        {
          _max = ini.GetVector(sectionName, "max");
        }
        if (_initializationStage > 3)
        {
          _min = ini.GetVector(sectionName, "min");
          _initRestPosition();
        }
        _deserializeWaypoints(sectionName, ini);
      }
      /// <summary>
      /// Creates a new Auto connector
      /// </summary>
      /// <param name="stationName"></param>
      /// <param name="name"></param>
      /// <param name="program"></param>
      /// <param name="posTransformer"></param>
      /// <param name="orientationTransformer"></param>
      public AutoConnector(
          string stationName,
          string name,
          MyGridProgram program,
          Action<string> logger,
          CoordinatesTransformer transformer)
      {
        _logger = logger;
        IMyGridTerminalSystem gts = program.GridTerminalSystem;
        Name = name;
        _log($"initialization");
        // initialize moving parts
        _initMandatoryField(gts, $"{stationName} / {Name} / Connector Down", out _downConnector);
        _initMandatoryField(gts, $"{stationName} / {Name} / Connector Front", out _frontConnector);
        _initMandatoryField(gts, $"{stationName} / {Name} / Rotor", out _stator);
        if (_downConnector == null && _frontConnector == null)
        {
          throw new InvalidOperationException("Need at least one connector");
        }
        _transformer = transformer;
        string pistonPrefix = $"{stationName} / {Name} / Piston";
        var pistons = new List<IMyPistonBase>();
        gts.GetBlocksOfType(pistons, p => p.CustomName.StartsWith(pistonPrefix));
        _x = new Actuator(4);
        _y = new Actuator(1);
        _z = new Actuator(2);
        foreach (IMyPistonBase piston in pistons)
        {
          Vector3D up = _transformer.Dir(piston.WorldMatrix.Up);
          double dot = FORWARD.Dot(up);
          if (Math.Abs(dot) > 0.95)
          {
            bool isNegative = dot < 0;
            piston.CustomName = pistonPrefix + " " + (isNegative ? "-" : "") + "Z";
            _z.AddPiston(piston, isNegative);
            continue;
          }
          dot = LEFT.Dot(up);
          if (Math.Abs(dot) > 0.95)
          {
            bool isNegative = dot < 0;
            piston.CustomName = pistonPrefix + " " + (isNegative ? "-" : "") + "X";
            _x.AddPiston(piston, isNegative);
            continue;
          }
          dot = UP.Dot(up);
          if (Math.Abs(dot) > 0.95)
          {
            bool isNegative = dot < 0;
            piston.CustomName = pistonPrefix + " " + (isNegative ? "-" : "") + "Y";
            _y.AddPiston(piston, isNegative);
            continue;
          }
          _log($"Could not place piston '{piston.CustomName}'");
        }
        if (!_x.IsValid)
        {
          throw new InvalidOperationException($"Connector '{Name}': no piston on axis X");
        }
        if (!_y.IsValid)
        {
          throw new InvalidOperationException($"Connector '{Name}': no piston on axis Y");
        }
        if (!_z.IsValid)
        {
          throw new InvalidOperationException($"Connector '{Name}': no piston on axis Z");
        }
        if (_frontConnector?.Status == MyShipConnectorStatus.Connected)
        {
          _lastConnectionType = ConnectionType.Front;
        }
        else if (_downConnector?.Status == MyShipConnectorStatus.Connected)
        {
          _lastConnectionType = ConnectionType.Down;
        }
        gts.GetBlocksOfType(_lights, l => l.CubeGrid == (_frontConnector ?? _downConnector).CubeGrid);
      }
      /// <summary>Returns true if the connector is ready to take requests</summary>
      /// <returns>True if initialized</returns>
      public bool IsInitialized() => _initializationStage >= 4;
      /// <summary>Returns true if the connector is moving</summary>
      /// <returns></returns>
      public bool IsMoving() => _waypoints.Count != 0;

      /// <summary>Returns true if the connector is already connected</summary>
      /// <returns></returns>
      public bool IsConnected() => _getCurrentConnected() != null;

      void _initialize()
      {
        bool hasReachedNextStep = true;
        if (_initializationStage == 0)
        {
          hasReachedNextStep &= _x.Move(10);
          hasReachedNextStep &= _y.Move(10);
          hasReachedNextStep &= _z.Move(10);
          if (hasReachedNextStep)
          {
            _max = _transformer.Pos(_getPositionBlock().GetPosition());
          }
        }
        else if (_initializationStage == 1)
        {
          hasReachedNextStep &= _x.Move(-10);
          hasReachedNextStep &= _z.Move(-10);
        }
        else if (_initializationStage == 2)
        {
          hasReachedNextStep &= _y.Move(-10);
          if (hasReachedNextStep)
          {
            _min = _transformer.Pos(_getPositionBlock().GetPosition());
            _initRestPosition();
          }
        }
        else if (_initializationStage == 3)
        {
          hasReachedNextStep &= _y.Move(10);
        }
        if (hasReachedNextStep)
        {
          ++_initializationStage;
          _log($"is now at initialization step {_initializationStage}");
        }
      }

      public void Save(MyIni ini)
      {
        string sectionName = INI_CONNECTOR_PREFIX + Name;
        ini.Set(sectionName, INI_STAGE, _initializationStage);
        if (_max != null)
        {
          ini.SetVector(sectionName, "max", _max);
        }
        if (_min != null)
        {
          ini.SetVector(sectionName, "min", _min);
        }
        int i = 1;
        foreach (Waypoint waypoint in _waypoints)
        {
          string prefix = $"waypoint-{i}";
          ini.SetVector(sectionName, prefix, waypoint.Position);
          if (waypoint.Angle != 0)
          {
            ini.Set(sectionName, $"{prefix}-angle", waypoint.Angle);
          }
          if (waypoint.Connection != ConnectionType.None)
          {
            ini.Set(sectionName, $"{prefix}-connect", waypoint.Connection.ToString());
          }
          if (waypoint.NeedPrecision)
          {
            ini.Set(sectionName, $"{prefix}-precise", waypoint.NeedPrecision);
          }
          ++i;
        }
      }
      /// <summary>Returns the distance of a position from the volume covered by the auto connector</summary>
      /// <param name="position">Position from which to compute the distance</param>
      /// <returns>Distance</returns>
      public double GetDistance(Vector3D position) => IsInitialized()
          ? _getDistance(_min.X, _max.X, position.X)
              + _getDistance(_min.Y, _max.Y, position.Y)
              + _getDistance(_min.Z, _max.Z, position.Z)
          : double.MaxValue;

      /// <summary>Requests the autoconnector to connect</summary>
      /// <param name="size">Size of the requesting connector</param>
      /// <param name="position">Position of the requesting connector</param>
      /// <param name="orientation">Orientation of the requesting connector</param>
      public void Connect(MyCubeSize size, Vector3D position, Vector3D orientation)
      {
        double offset = _getOffset(size) + _getOffset((_frontConnector ?? _downConnector).CubeGrid.GridSizeEnum);

        Vector3D finalTarget = position + (offset * orientation);
        _queueConnectionRoute(finalTarget, orientation);
      }
      /// <summary>Requests the autoconnector to disconnect</summary>
      public void Disconnect()
      {
        _waypoints.Clear();
        IMyShipConnector connector = _getCurrentConnected()
            ?? _getConnector(_lastConnectionType)
            ?? _frontConnector
            ?? _downConnector;
        Vector3D curPos = _transformer.Pos(connector.GetPosition());
        Vector3D curOrientation = _transformer.Dir(connector.WorldMatrix.Backward);
        connector.Disconnect();
        Vector3D safetyTarget = curPos + (curOrientation * CONNECTION_SAFETY_OFFSET);
        _waypoints.Enqueue(new Waypoint(
            new Vector3D(safetyTarget.X, safetyTarget.Y, safetyTarget.Z),
            _stator?.Angle ?? 0,
            needPrecision: true
          )).Enqueue(new Waypoint(
            new Vector3D(safetyTarget.X, _max.Y, safetyTarget.Z)
          )).Enqueue(new Waypoint(
            _restPosition
          ));
      }

      /// <summary>Main loop that handles movements and initialization</summary>
      /// <returns>True if still moving</returns>
      public bool Update()
      {
        if (IsInitialized())
        {
          if (_waypoints.Count > 0)
          {

            IMyTerminalBlock block = _getPositionBlock();
            _currentPosition = _transformer.Pos(block.GetPosition());
            bool isReached = _goToWaypoint(_waypoints.Peek());
            if (isReached)
            {
              Waypoint waypoint = _waypoints.Dequeue();
              _log($"reached a waypoint, {_waypoints.Count} to go");
              Stop();

              // We only try to connect on the last point
              if (_waypoints.Count == 0)
              {
                _getConnector(waypoint.Connection)?.Connect();
              }
              _lastConnectionType = waypoint.Connection;
            }
          }
          bool res = _waypoints.Count != 0;
          foreach (IMyLightingBlock l in _lights)
          {
            l.Enabled = res;
          }
          return res;
        }
        else
        {
          _initialize();
        }
        return false;
      }

      public double GetRemainingLength()
      {
        double length = 0;
        Vector3D curPos = _currentPosition;
        foreach (Waypoint waypoint in _waypoints)
        {
          Vector3D delta = curPos - waypoint.Position;
          // because of the way pistons move, we take the max of the distance
          double dist = Math.Max(Math.Max(Math.Abs(delta.X), Math.Abs(delta.Y)), Math.Abs(delta.Z));
          if (waypoint.NeedPrecision)
          {
            dist *= 2;
          }
          length += dist;
        }
        return length;
      }

      public void Stop()
      {
        _x.Stop();
        _y.Stop();
        _z.Stop();
        if (_stator != null)
        {
          _stator.TargetVelocityRad = 0;
        }
      }

      double _getOffset(MyCubeSize size) => size == MyCubeSize.Large ? CONNECTION_OFFSET_LARGE : CONNECTION_OFFSET_SMALL;

      void _queueConnectionRoute(Vector3D target, Vector3D orientation)
      {
        _waypoints.Clear();
        ConnectionType connectionType = Math.Abs(orientation.Dot(UP)) > 0.8 ? ConnectionType.Down : ConnectionType.Front;
        float angle = connectionType == ConnectionType.Front ? _getTargetAngle(orientation) : 0;
        IMyShipConnector con = _getCurrentConnected();
        Vector3D curPos = _transformer.Pos((con ?? _stator ?? _frontConnector ?? _downConnector as IMyTerminalBlock).GetPosition());
        ConnectionType firstType = ConnectionType.None;
        if (con != null)
        {
          con.Disconnect();
          Vector3D firstPos = curPos + (_transformer.Dir(con.WorldMatrix.Backward) * CONNECTION_SAFETY_OFFSET);
          // we give a connection type to have the correct connector positioned
          _waypoints.Enqueue(new Waypoint(
            firstPos,
            angle: _stator?.Angle ?? 0,
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

      bool _goToWaypoint(Waypoint waypoint)
      {
        if (waypoint.Connection == ConnectionType.None)
        {
          _downConnector?.Disconnect();
          _frontConnector?.Disconnect();
        }
        bool isReached = true;
        //this._log($"{waypoint.Position.X - this.currentPosition.X:0.00} {waypoint.Position.Y - this.currentPosition.Y:0.00} {waypoint.Position.Z - this.currentPosition.Z:0.00}");
        isReached &= _x.Move(waypoint.Position.X - _currentPosition.X, waypoint.NeedPrecision);
        isReached &= _y.Move(waypoint.Position.Y - _currentPosition.Y, waypoint.NeedPrecision);
        isReached &= _z.Move(waypoint.Position.Z - _currentPosition.Z, waypoint.NeedPrecision);
        if (_stator != null)
        {
          isReached &= _moveRotor(_stator.AngleProxy(waypoint.Angle), waypoint.NeedPrecision);
        }
        return isReached;
      }

      void _deserializeWaypoints(string sectionName, MyIni ini)
      {
        int waypointNumber = 1;
        while (true)
        {
          string prefix = $"waypoint-{waypointNumber++}";
          if (ini.ContainsKey(sectionName, $"{prefix}-x"))
          {
            _waypoints.Enqueue(new Waypoint(
                ini.GetVector(sectionName, prefix),
                ini.Get(sectionName, $"{prefix}-angle").ToSingle(0),
                (ConnectionType)Enum.Parse(
                    typeof(ConnectionType),
                    ini.Get(sectionName, $"{prefix}-connect").ToString("None")),
                ini.Get(sectionName, $"{prefix}-precise").ToBoolean(false)
              ));
          }
          else
          {
            break;
          }
        }
        _log($"has {_waypoints.Count} pending waypoints");
      }

      void _initMandatoryField<T>(IMyGridTerminalSystem gts, string name, out T field) where T : class, IMyTerminalBlock
      {
        field = gts.GetBlockWithName(name) as T;
        if (field == null)
        {
          _log($"could not find {typeof(T)} '{name}'");
        }
      }

      IMyTerminalBlock _getPositionBlock()
      {
        ConnectionType type = _waypoints
            .Select(w => w.Connection)
            .FirstOrDefault(c => c != ConnectionType.None);
        return _getConnector(type) ?? (_getConnector(_lastConnectionType) ?? _stator ?? _frontConnector ?? _downConnector as IMyTerminalBlock);
      }

      float _getTargetAngle(Vector3D targetOrientation)
      {
        if (_stator == null)
        {
          return 0;
        }
        else
        {
          return RotorHelper.Mod(_getAngle(targetOrientation) - _getAngle(_transformer.Dir(_frontConnector?.WorldMatrix.Backward ?? FORWARD))
              + _stator.Angle, MathHelper.Pi * 2);
        }
      }

      float _getAngle(Vector3D orientation)
      {
        // project orientation on normal plane of Up
        var proj = Vector3.Normalize((orientation.Dot(LEFT) * LEFT) + (orientation.Dot(FORWARD) * FORWARD));
        float angle = (float)Math.Acos(proj.Dot(FORWARD)) + MathHelper.Pi;
        bool invert = proj.Cross(FORWARD).Dot(UP) < 0;
        return invert ? angle : -angle;
      }

      bool _moveRotor(float delta, bool needPrecision)
      {
        _stator.TargetVelocityRad = MathHelper.Clamp(delta * (needPrecision ? 1 : 4), -1, +1);
        return Math.Abs(delta) < DELTA_TARGET;
      }

      void _initRestPosition() => _restPosition = new Vector3D(
          (_min.X + _max.X) / 2,
          _max.Y,
          (_min.Z + _max.Z) / 2);

      void _log(string log) => _logger?.Invoke($"Connector {Name}: {log}");

      IMyShipConnector _getCurrentConnected()
      {
        return _frontConnector?.Status == MyShipConnectorStatus.Connected
          ? _frontConnector
          : _downConnector?.Status == MyShipConnectorStatus.Connected
            ? _downConnector : null;
      }

      IMyShipConnector _getConnector(ConnectionType type)
      {
        if (type == ConnectionType.Down)
        {
          return _downConnector;
        }
        else if (type == ConnectionType.Front)
        {
          return _frontConnector;
        }
        return null;
      }

      static double _getDistance(double min, double max, double pos)
      {
        return pos < min
            ? min - pos
            : pos < max
                ? 0
                : pos - max;
      }
    }
  }
}
