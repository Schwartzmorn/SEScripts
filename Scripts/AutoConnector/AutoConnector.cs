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
      private readonly CircBuf<Waypoint> _waypoints = new CircBuf<Waypoint>(10);
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
        this._log($"parsing configuration");
        this._initializationStage = ini.GetThrow(sectionName, INI_STAGE).ToInt32();
        this._log($"at stage {this._initializationStage} ({(this.IsInitialized() ? "" : "not ")}initialized)");
        if(this._initializationStage > 1) {
          this._max = ini.GetVector(sectionName, "max");
        }
        if(this._initializationStage > 3) {
          this._min = ini.GetVector(sectionName, "min");
          this._initRestPosition();
        }
        this._deserializeWaypoints(sectionName, ini);
      }

      public AutoConnector(
          string stationName,
          string name,
          MyGridProgram program,
          VectorTransformer posTransformer,
          VectorTransformer orientationTransformer) {
        var gts = program.GridTerminalSystem;
        this.Name = name;
        this._log($"initialization");
        // initialize moving parts
        this._initMandatoryField(gts, $"{stationName} Connector {this.Name} Down", out this._downConnector);
        this._initMandatoryField(gts, $"{stationName} Connector {this.Name} Front", out this._frontConnector);
        this._initMandatoryField(gts, $"{stationName} Rotor {this.Name}", out this._stator);
        this._posTransformer = posTransformer;
        this._orientationTransformer = orientationTransformer;
        string pistonPrefix = $"{stationName} Piston {this.Name}";
        var pistons = new List<IMyPistonBase>();
        gts.GetBlocksOfType(pistons, p => p.DisplayNameText.StartsWith(pistonPrefix));
        this._x = new Actuator(4);
        this._y = new Actuator(1);
        this._z = new Actuator(2);
        foreach(var piston in pistons) {
          var up = this._orientationTransformer(piston.WorldMatrix.Up);
          var dot = FORWARD.Dot(up);
          if(Math.Abs(dot) > 0.95) {
            bool isNegative = dot < 0;
            piston.CustomName = pistonPrefix + " " + (isNegative ? "-" : "") + "Z";
            this._z.AddPiston(piston, isNegative);
            continue;
          }
          dot = LEFT.Dot(up);
          if(Math.Abs(dot) > 0.95) {
            bool isNegative = dot < 0;
            piston.CustomName = pistonPrefix + " " + (isNegative ? "-" : "") + "X";
            this._x.AddPiston(piston, isNegative);
            continue;
          }
          dot = UP.Dot(up);
          if(Math.Abs(dot) > 0.95) {
            bool isNegative = dot < 0;
            piston.CustomName = pistonPrefix + " " + (isNegative ? "-" : "") + "Y";
            this._y.AddPiston(piston, isNegative);
            continue;
          }
          this._log($"Could not place piston '{piston.DisplayNameText}'");
        }
        if(!this._x.IsValid) {
          throw new InvalidOperationException($"Connector '{this.Name}': no piston on axis X");
        }
        if(!this._y.IsValid) {
          throw new InvalidOperationException($"Connector '{this.Name}': no piston on axis Y");
        }
        if(!this._z.IsValid) {
          throw new InvalidOperationException($"Connector '{this.Name}': no piston on axis Z");
        }
        if(this._frontConnector.Status == MyShipConnectorStatus.Connected) {
          this._lastConnectionType = ConnectionType.Front;
        } else if(this._downConnector.Status == MyShipConnectorStatus.Connected) {
          this._lastConnectionType = ConnectionType.Down;
        }
      }

      public bool IsInitialized() => this._initializationStage >= 4;

      public bool IsMoving() => this._waypoints.Count != 0;

      public bool IsConnected() => this._getCurrentConnected() != null;

      private void _initialize() {
        bool hasReachedNextStep = true;
        if(this._initializationStage == 0) {
          hasReachedNextStep &= this._x.Move(10);
          hasReachedNextStep &= this._y.Move(10);
          hasReachedNextStep &= this._z.Move(10);
          if(hasReachedNextStep) {
            this._max = this._posTransformer(this._stator.GetPosition());
          }
        } else if(this._initializationStage == 1) {
          hasReachedNextStep &= this._x.Move(-10);
          hasReachedNextStep &= this._z.Move(-10);
        } else if(this._initializationStage == 2) {
          hasReachedNextStep &= this._y.Move(-10);
          if(hasReachedNextStep) {
            this._min = this._posTransformer(this._stator.GetPosition());
            this._initRestPosition();
          }
        } else if(this._initializationStage == 3) {
          hasReachedNextStep &= this._y.Move(10);
        }
        if(hasReachedNextStep) {
          ++this._initializationStage;
          this._log($"is now at initialization step {this._initializationStage}");
        }
      }

      public void Save(MyIni ini) {
        var sectionName = IniConnectorPrefix + this.Name;
        ini.Set(sectionName, INI_STAGE, this._initializationStage);
        if(this._max != null) {
          ini.SetVector(sectionName, "max", this._max);
        }
        if(this._min != null) {
          ini.SetVector(sectionName, "min", this._min);
        }
        int i = 1;
        foreach(var waypoint in this._waypoints) {
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

      public double GetDistance(Vector3D position) => this.IsInitialized()
          ? GetDistance(this._min.X, this._max.X, position.X)
              + GetDistance(this._min.Y, this._max.Y, position.Y)
              + GetDistance(this._min.Z, this._max.Z, position.Z)
          : double.MaxValue;

      public void Connect(MyCubeSize size, Vector3D position, Vector3D orientation) {
        double offset = (size == MyCubeSize.Small) ? CONNECTION_OFFSET_SMALL : CONNECTION_OFFSET_LARGE;
        Vector3D finalTarget = position + (offset * orientation);
        this._queueConnectionRoute(finalTarget, orientation);
      }

      public void Disconnect() {
        this._waypoints.Clr();
        var connector = this._getCurrentConnected()
            ?? this._getConnector(this._lastConnectionType)
            ?? this._frontConnector;
        Vector3D curPos = this._posTransformer(connector.GetPosition());
        Vector3D curOrientation = this._orientationTransformer(connector.WorldMatrix.Backward);
        connector.Disconnect();
        Vector3D safetyTarget = curPos + (curOrientation * CONNECTION_SAFETY_OFFSET);
        this._waypoints.Enqueue(new Waypoint(
            new Vector3D(safetyTarget.X, safetyTarget.Y, safetyTarget.Z),
            this._stator.Angle,
            needPrecision: true
          )).Enqueue(new Waypoint(
            new Vector3D(safetyTarget.X, this._max.Y, safetyTarget.Z)
          )).Enqueue(new Waypoint(
            this._restPosition
          ));
      }

      // returns true if it is updating
      public bool Update() {
        if(this.IsInitialized()) {
          if(this._waypoints.Count > 0) {
            var block = this._getPositionBlock();
            this._currentPosition = this._posTransformer(block.GetPosition());
            this._currentOrientation = this._frontConnector.WorldMatrix.Forward;
            var isReached = _goToWaypoint(this._waypoints.Peek());
            if(isReached) {
              var waypoint = this._waypoints.Dequeue();
              this._log($"reached a waypoint, {this._waypoints.Count} to go");
              this._x.Stop();
              this._y.Stop();
              this._z.Stop();
              this._stator.TargetVelocityRad = 0;

              // We only try to connect on the last point
              if(this._waypoints.Count == 0) {
                _getConnector(waypoint.Connection)?.Connect();
              }
              this._lastConnectionType = waypoint.Connection;
            }
          }
          return this._waypoints.Count != 0;
        } else {
          this._initialize();
        }
        return false;
      }

      public double GetRemainingLength() {
        double length = 0;
        Vector3D curPos = this._currentPosition;
        foreach(var waypoint in this._waypoints) {
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
        this._waypoints.Clr();
        var connectionType = Math.Abs(orientation.Dot(UP)) > 0.8 ? ConnectionType.Down : ConnectionType.Front;
        float angle = connectionType == ConnectionType.Front ? this._getTargetAngle(orientation) : 0;
        var con = this._getCurrentConnected();
        var curPos = this._posTransformer((con ?? this._stator as IMyTerminalBlock).GetPosition());
        ConnectionType firstType = ConnectionType.None;
        if(con != null) {
          con.Disconnect();
          var firstPos = curPos + (this._orientationTransformer(con.WorldMatrix.Backward) * CONNECTION_SAFETY_OFFSET);
          // we give a connection type to have the correct connector positioned
          this._waypoints.Enqueue(new Waypoint(
            firstPos,
            angle: this._stator.Angle,
            needPrecision: true
          ));
          firstType = con == this._frontConnector ? ConnectionType.Front : ConnectionType.Down;
          curPos = firstPos;
        }
        Vector3D safetyTarget = target + (orientation * CONNECTION_SAFETY_OFFSET);
        // we give a connection type to have the correct connector positioned for the first points
        this._waypoints.Enqueue(new Waypoint(
            new Vector3D(curPos.X, this._max.Y, curPos.Z),
            connection: firstType
          )).Enqueue(new Waypoint(
            new Vector3D(safetyTarget.X, this._max.Y, safetyTarget.Z)
          )).Enqueue(new Waypoint(
            new Vector3D(safetyTarget.X, this._max.Y - 3, safetyTarget.Z)
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
          this._downConnector.Disconnect();
          this._frontConnector.Disconnect();
        }
        bool isReached = true;
        isReached &= this._x.Move(waypoint.Position.X - this._currentPosition.X, waypoint.NeedPrecision);
        isReached &= this._y.Move(waypoint.Position.Y - this._currentPosition.Y, waypoint.NeedPrecision);
        isReached &= this._z.Move(waypoint.Position.Z - this._currentPosition.Z, waypoint.NeedPrecision);
        isReached &= this._moveRotor(AngleProxy(this._stator.Angle, waypoint.Angle), waypoint.NeedPrecision);
        return isReached;
      }

      private void _deserializeWaypoints(string sectionName, MyIni ini) {
        int waypointNumber = 1;
        while(true) {
          string prefix = $"waypoint-{waypointNumber++}";
          if(ini.ContainsKey(sectionName, $"{prefix}-x")) {
            this._waypoints.Enqueue(new Waypoint(
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
        this._log($"has {this._waypoints.Count} pending waypoints");
      }

      private void _initMandatoryField<T>(IMyGridTerminalSystem gts, string name, out T field) where T : class, IMyTerminalBlock {
        field = gts.GetBlockWithName(name) as T;
        if(field == null) {
          throw new InvalidOperationException($"Connector '{this.Name}': could not find {typeof(T)} '{name}'");
        }
      }

      private IMyTerminalBlock _getPositionBlock() {
        ConnectionType type = this._waypoints
            .Select(w => w.Connection)
            .FirstOrDefault(c => c != ConnectionType.None);
        return this._getConnector(type) ?? (this._getConnector(this._lastConnectionType) ?? this._stator as IMyTerminalBlock);
      }

      private float _getTargetAngle(Vector3D targetOrientation) {
        return (float)Mod(this._getAngle(targetOrientation) - this._getAngle(this._orientationTransformer(this._frontConnector.WorldMatrix.Backward))
            + this._stator.Angle, Math.PI * 2);
      }

      private float _getAngle(Vector3D orientation) {
        // project orientation on normal plane of Up
        var proj = Vector3.Normalize((orientation.Dot(LEFT) * LEFT) + (orientation.Dot(FORWARD) * FORWARD));
        float angle = (float)Math.Acos(proj.Dot(FORWARD)) + MathHelper.Pi;
        bool invert = proj.Cross(FORWARD).Dot(UP) < 0;
        return invert ? angle : -angle;
      }

      private bool _moveRotor(float delta, bool needPrecision) {
        this._stator.TargetVelocityRad = MathHelper.Clamp(delta * (needPrecision ? 1 : 4), -1, +1);
        return Math.Abs(delta) < DELTA_TARGET;
      }

      private void _initRestPosition() => this._restPosition = new Vector3D(
          (this._min.X + this._max.X) / 2,
          this._max.Y,
          (this._min.Z + this._max.Z) / 2);

      private void _log(string log) => Log($"Connector {this.Name}: {log}");

      private IMyShipConnector _getCurrentConnected() {
        return this._frontConnector.Status == MyShipConnectorStatus.Connected
          ? this._frontConnector
          : this._downConnector.Status == MyShipConnectorStatus.Connected
            ? this._downConnector : null;
      }

      private IMyShipConnector _getConnector(ConnectionType type) {
        if(type == ConnectionType.Down) {
          return this._downConnector;
        } else if(type == ConnectionType.Front) {
          return this._frontConnector;
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
