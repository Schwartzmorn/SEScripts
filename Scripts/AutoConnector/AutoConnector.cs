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
    /// <summary>This class is in charge of actuating the pistons, rotors and connectors</summary>
    public class AutoConnector {
      public static readonly string IniConnectorPrefix = "connector-";
      static readonly string INI_STAGE = "initialization-stage";
      static readonly Vector3D LEFT = new Vector3D(1, 0, 0);
      static readonly Vector3D UP = new Vector3D(0, 1, 0);
      static readonly Vector3D FORWARD = new Vector3D(0, 0, 1);
      static readonly double CONNECTION_OFFSET_SMALL = 0.5;
      static readonly double CONNECTION_OFFSET_LARGE = 1.25;
      static readonly double CONNECTION_SAFETY_OFFSET = 2;
      static readonly double DELTA_TARGET = 0.05;
      // moving parts
      readonly IMyShipConnector downConnector;
      readonly IMyShipConnector frontConnector;
      readonly Actuator x, y, z;
      readonly IMyMotorStator stator;
      readonly List<IMyLightingBlock> lights = new List<IMyLightingBlock>();
      // state
      int initializationStage = 0;
      Vector3D max;
      Vector3D min;
      Vector3D restPosition;
      public readonly string Name;
      readonly CircularBuffer<Waypoint> waypoints = new CircularBuffer<Waypoint>(10);
      // in case of wild disconnection, this should ensure we position the correct block and don't flail
      ConnectionType lastConnectionType = ConnectionType.None;
      // position
      Vector3D currentPosition;
      Vector3D currentOrientation;
      // helpers
      readonly Action<string> logger;
      readonly CoordinatesTransformer transformer;
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
          : this(stationName, sectionName.Substring(IniConnectorPrefix.Length), program, logger, transformer) {
        // Parse ini
        this.log($"parsing configuration");
        this.initializationStage = ini.GetThrow(sectionName, INI_STAGE).ToInt32();
        this.log($"at stage {this.initializationStage} ({(this.IsInitialized() ? "" : "not ")}initialized)");
        if(this.initializationStage > 1) {
          this.max = ini.GetVector(sectionName, "max");
        }
        if(this.initializationStage > 3) {
          this.min = ini.GetVector(sectionName, "min");
          this.initRestPosition();
        }
        this.deserializeWaypoints(sectionName, ini);
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
          CoordinatesTransformer transformer) {
        this.logger = logger;
        IMyGridTerminalSystem gts = program.GridTerminalSystem;
        this.Name = name;
        this.log($"initialization");
        // initialize moving parts
        this.initMandatoryField(gts, $"{stationName} Connector {this.Name} Down", out this.downConnector);
        this.initMandatoryField(gts, $"{stationName} Connector {this.Name} Front", out this.frontConnector);
        this.initMandatoryField(gts, $"{stationName} Rotor {this.Name}", out this.stator);
        if (this.downConnector == null && this.frontConnector == null) {
          throw new InvalidOperationException("Need at least one connector");
        }
        this.transformer = transformer;
        string pistonPrefix = $"{stationName} Piston {this.Name}";
        var pistons = new List<IMyPistonBase>();
        gts.GetBlocksOfType(pistons, p => p.CustomName.StartsWith(pistonPrefix));
        this.x = new Actuator(4);
        this.y = new Actuator(1);
        this.z = new Actuator(2);
        foreach(IMyPistonBase piston in pistons) {
          Vector3D up = this.transformer.Dir(piston.WorldMatrix.Up);
          double dot = FORWARD.Dot(up);
          if(Math.Abs(dot) > 0.95) {
            bool isNegative = dot < 0;
            piston.CustomName = pistonPrefix + " " + (isNegative ? "-" : "") + "Z";
            this.z.AddPiston(piston, isNegative);
            continue;
          }
          dot = LEFT.Dot(up);
          if(Math.Abs(dot) > 0.95) {
            bool isNegative = dot < 0;
            piston.CustomName = pistonPrefix + " " + (isNegative ? "-" : "") + "X";
            this.x.AddPiston(piston, isNegative);
            continue;
          }
          dot = UP.Dot(up);
          if(Math.Abs(dot) > 0.95) {
            bool isNegative = dot < 0;
            piston.CustomName = pistonPrefix + " " + (isNegative ? "-" : "") + "Y";
            this.y.AddPiston(piston, isNegative);
            continue;
          }
          this.log($"Could not place piston '{piston.CustomName}'");
        }
        if(!this.x.IsValid) {
          throw new InvalidOperationException($"Connector '{this.Name}': no piston on axis X");
        }
        if(!this.y.IsValid) {
          throw new InvalidOperationException($"Connector '{this.Name}': no piston on axis Y");
        }
        if(!this.z.IsValid) {
          throw new InvalidOperationException($"Connector '{this.Name}': no piston on axis Z");
        }
        if(this.frontConnector?.Status == MyShipConnectorStatus.Connected) {
          this.lastConnectionType = ConnectionType.Front;
        } else if(this.downConnector?.Status == MyShipConnectorStatus.Connected) {
          this.lastConnectionType = ConnectionType.Down;
        }
        gts.GetBlocksOfType(this.lights, l => l.CubeGrid == (this.frontConnector ?? this.downConnector).CubeGrid);
      }
      /// <summary>Returns true if the connector is ready to take requests</summary>
      /// <returns>True if initialized</returns>
      public bool IsInitialized() => this.initializationStage >= 4;
      /// <summary>Returns true if the connector is moving</summary>
      /// <returns></returns>
      public bool IsMoving() => this.waypoints.Count != 0;

      /// <summary>Returns true if the connector is already connected</summary>
      /// <returns></returns>
      public bool IsConnected() => this.getCurrentConnected() != null;

      void initialize() {
        bool hasReachedNextStep = true;
        if(this.initializationStage == 0) {
          hasReachedNextStep &= this.x.Move(10);
          hasReachedNextStep &= this.y.Move(10);
          hasReachedNextStep &= this.z.Move(10);
          if(hasReachedNextStep) {
            this.max = this.transformer.Pos(this.getPositionBlock().GetPosition());
          }
        } else if(this.initializationStage == 1) {
          hasReachedNextStep &= this.x.Move(-10);
          hasReachedNextStep &= this.z.Move(-10);
        } else if(this.initializationStage == 2) {
          hasReachedNextStep &= this.y.Move(-10);
          if(hasReachedNextStep) {
            this.min = this.transformer.Pos(this.getPositionBlock().GetPosition());
            this.initRestPosition();
          }
        } else if(this.initializationStage == 3) {
          hasReachedNextStep &= this.y.Move(10);
        }
        if(hasReachedNextStep) {
          ++this.initializationStage;
          this.log($"is now at initialization step {this.initializationStage}");
        }
      }

      public void Save(MyIni ini) {
        string sectionName = IniConnectorPrefix + this.Name;
        ini.Set(sectionName, INI_STAGE, this.initializationStage);
        if(this.max != null) {
          ini.SetVector(sectionName, "max", this.max);
        }
        if(this.min != null) {
          ini.SetVector(sectionName, "min", this.min);
        }
        int i = 1;
        foreach(Waypoint waypoint in this.waypoints) {
          string prefix = $"waypoint-{i}";
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
      /// <summary>Returns the distance of a position from the volume covered by the auto connector</summary>
      /// <param name="position">Position from which to compute the distance</param>
      /// <returns>Distance</returns>
      public double GetDistance(Vector3D position) => this.IsInitialized()
          ? GetDistance(this.min.X, this.max.X, position.X)
              + GetDistance(this.min.Y, this.max.Y, position.Y)
              + GetDistance(this.min.Z, this.max.Z, position.Z)
          : double.MaxValue;

      /// <summary>Requests the autoconnector to connect</summary>
      /// <param name="size">Size of the requesting connector</param>
      /// <param name="position">Position of the requesting connector</param>
      /// <param name="orientation">Orientation of the requesting connector</param>
      public void Connect(MyCubeSize size, Vector3D position, Vector3D orientation) {
        double offset = this.getOffset(size) + this.getOffset((this.frontConnector ?? this.downConnector).CubeGrid.GridSizeEnum);

        Vector3D finalTarget = position + (offset * orientation);
        this.queueConnectionRoute(finalTarget, orientation);
      }
      /// <summary>Requests the autoconnector to disconnect</summary>
      public void Disconnect() {
        this.waypoints.Clear();
        IMyShipConnector connector = this.getCurrentConnected()
            ?? this.getConnector(this.lastConnectionType)
            ?? this.frontConnector
            ?? this.downConnector;
        Vector3D curPos = this.transformer.Pos(connector.GetPosition());
        Vector3D curOrientation = this.transformer.Dir(connector.WorldMatrix.Backward);
        connector.Disconnect();
        Vector3D safetyTarget = curPos + (curOrientation * CONNECTION_SAFETY_OFFSET);
        this.waypoints.Enqueue(new Waypoint(
            new Vector3D(safetyTarget.X, safetyTarget.Y, safetyTarget.Z),
            this.stator?.Angle ?? 0,
            needPrecision: true
          )).Enqueue(new Waypoint(
            new Vector3D(safetyTarget.X, this.max.Y, safetyTarget.Z)
          )).Enqueue(new Waypoint(
            this.restPosition
          ));
      }

      /// <summary>Main loop that handles movements and initialization</summary>
      /// <returns>True if still moving</returns>
      public bool Update() {
        if(this.IsInitialized()) {
          if(this.waypoints.Count > 0) {
            
            IMyTerminalBlock block = this.getPositionBlock();
            this.currentPosition = this.transformer.Pos(block.GetPosition());
            this.currentOrientation = this.frontConnector?.WorldMatrix.Forward ?? FORWARD;
            bool isReached = this.goToWaypoint(this.waypoints.Peek());
            if(isReached) {
              Waypoint waypoint = this.waypoints.Dequeue();
              this.log($"reached a waypoint, {this.waypoints.Count} to go");
              this.Stop();

              // We only try to connect on the last point
              if(this.waypoints.Count == 0) {
                getConnector(waypoint.Connection)?.Connect();
              }
              this.lastConnectionType = waypoint.Connection;
            }
          }
          bool res = this.waypoints.Count != 0;
          foreach (IMyLightingBlock l in this.lights) {
            l.Enabled = res;
          }
          return res;
        } else {
          this.initialize();
        }
        return false;
      }

      public double GetRemainingLength() {
        double length = 0;
        Vector3D curPos = this.currentPosition;
        foreach(Waypoint waypoint in this.waypoints) {
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

      public void Stop() {
        this.x.Stop();
        this.y.Stop();
        this.z.Stop();
        if (this.stator != null) {
          this.stator.TargetVelocityRad = 0;
        }
      }

      double getOffset(MyCubeSize size) => size == MyCubeSize.Large ? CONNECTION_OFFSET_LARGE : CONNECTION_OFFSET_SMALL;

      void queueConnectionRoute(Vector3D target, Vector3D orientation) {
        this.waypoints.Clear();
        ConnectionType connectionType = Math.Abs(orientation.Dot(UP)) > 0.8 ? ConnectionType.Down : ConnectionType.Front;
        float angle = connectionType == ConnectionType.Front ? this.getTargetAngle(orientation) : 0;
        IMyShipConnector con = this.getCurrentConnected();
        Vector3D curPos = this.transformer.Pos((con ?? this.stator ?? this.frontConnector ?? this.downConnector as IMyTerminalBlock).GetPosition());
        ConnectionType firstType = ConnectionType.None;
        if(con != null) {
          con.Disconnect();
          Vector3D firstPos = curPos + (this.transformer.Dir(con.WorldMatrix.Backward) * CONNECTION_SAFETY_OFFSET);
          // we give a connection type to have the correct connector positioned
          this.waypoints.Enqueue(new Waypoint(
            firstPos,
            angle: this.stator?.Angle ?? 0,
            needPrecision: true
          ));
          firstType = con == this.frontConnector ? ConnectionType.Front : ConnectionType.Down;
          curPos = firstPos;
        }
        Vector3D safetyTarget = target + (orientation * CONNECTION_SAFETY_OFFSET);
        // we give a connection type to have the correct connector positioned for the first points
        this.waypoints.Enqueue(new Waypoint(
            new Vector3D(curPos.X, this.max.Y, curPos.Z),
            connection: firstType
          )).Enqueue(new Waypoint(
            new Vector3D(safetyTarget.X, this.max.Y, safetyTarget.Z)
          )).Enqueue(new Waypoint(
            new Vector3D(safetyTarget.X, this.max.Y - 3, safetyTarget.Z)
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

      bool goToWaypoint(Waypoint waypoint) {
        if(waypoint.Connection == ConnectionType.None) {
          this.downConnector?.Disconnect();
          this.frontConnector?.Disconnect();
        }
        bool isReached = true;
        //this._log($"{waypoint.Position.X - this.currentPosition.X:0.00} {waypoint.Position.Y - this.currentPosition.Y:0.00} {waypoint.Position.Z - this.currentPosition.Z:0.00}");
        isReached &= this.x.Move(waypoint.Position.X - this.currentPosition.X, waypoint.NeedPrecision);
        isReached &= this.y.Move(waypoint.Position.Y - this.currentPosition.Y, waypoint.NeedPrecision);
        isReached &= this.z.Move(waypoint.Position.Z - this.currentPosition.Z, waypoint.NeedPrecision);
        if (this.stator != null) {
          isReached &= this.moveRotor(this.stator.AngleProxy(waypoint.Angle), waypoint.NeedPrecision);
        }
        return isReached;
      }

      void deserializeWaypoints(string sectionName, MyIni ini) {
        int waypointNumber = 1;
        while(true) {
          string prefix = $"waypoint-{waypointNumber++}";
          if(ini.ContainsKey(sectionName, $"{prefix}-x")) {
            this.waypoints.Enqueue(new Waypoint(
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
        this.log($"has {this.waypoints.Count} pending waypoints");
      }

      void initMandatoryField<T>(IMyGridTerminalSystem gts, string name, out T field) where T : class, IMyTerminalBlock {
        field = gts.GetBlockWithName(name) as T;
        if(field == null) {
          this.log($"Connector '{this.Name}': could not find {typeof(T)} '{name}'");
        }
      }

      IMyTerminalBlock getPositionBlock() {
        ConnectionType type = this.waypoints
            .Select(w => w.Connection)
            .FirstOrDefault(c => c != ConnectionType.None);
        return this.getConnector(type) ?? (this.getConnector(this.lastConnectionType) ?? this.stator ?? this.frontConnector ?? this.downConnector as IMyTerminalBlock);
      }

      float getTargetAngle(Vector3D targetOrientation) {
        if (this.stator == null) {
          return 0;
        } else {
          return RotorHelper.Mod(this.getAngle(targetOrientation) - this.getAngle(this.transformer.Dir(this.frontConnector?.WorldMatrix.Backward ?? FORWARD))
              + this.stator.Angle, MathHelper.Pi * 2);
        }
      }

      float getAngle(Vector3D orientation) {
        // project orientation on normal plane of Up
        var proj = Vector3.Normalize((orientation.Dot(LEFT) * LEFT) + (orientation.Dot(FORWARD) * FORWARD));
        float angle = (float)Math.Acos(proj.Dot(FORWARD)) + MathHelper.Pi;
        bool invert = proj.Cross(FORWARD).Dot(UP) < 0;
        return invert ? angle : -angle;
      }

      bool moveRotor(float delta, bool needPrecision) {
        this.stator.TargetVelocityRad = MathHelper.Clamp(delta * (needPrecision ? 1 : 4), -1, +1);
        return Math.Abs(delta) < DELTA_TARGET;
      }

      void initRestPosition() => this.restPosition = new Vector3D(
          (this.min.X + this.max.X) / 2,
          this.max.Y,
          (this.min.Z + this.max.Z) / 2);

      void log(string log) => this.logger?.Invoke($"Connector {this.Name}: {log}");

      IMyShipConnector getCurrentConnected() {
        return this.frontConnector?.Status == MyShipConnectorStatus.Connected
          ? this.frontConnector
          : this.downConnector?.Status == MyShipConnectorStatus.Connected
            ? this.downConnector : null;
      }

      IMyShipConnector getConnector(ConnectionType type) {
        if(type == ConnectionType.Down) {
          return this.downConnector;
        } else if(type == ConnectionType.Front) {
          return this.frontConnector;
        }
        return null;
      }

      static double GetDistance(double min, double max, double pos) {
        return pos < min
            ? min - pos
            : pos < max 
                ? 0
                : pos - max;
      }
    }
  }
}
