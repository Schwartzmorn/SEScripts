using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript {
  internal partial class Program {
    public class ScreensController {
      static readonly Vector2 CON_TEXT_POSITION = new Vector2(10, 40);
      static readonly Vector2 DRILL_ARM_CENTER = new Vector2(161, 105);
      static readonly Vector2 DRILL_LIGHTS_OFFSET = new Vector2(35, -20);
      static readonly Vector2 INV_TEXT_POSITION = new Vector2(10, 20);

      readonly ConnectionClient _connection;
      readonly Display _drillSurface;
      readonly ColorScheme _scheme;
      readonly Display _wheelSurface;

      public ScreensController(ArmController armController, GeneralStatus status, InventoriesController inventoryController,
          ConnectionClient connection, IMyTextSurface drillStatusSurface, IMyTextSurface wheelStatusSurface, ColorScheme scheme, string sprites) {
        _connection = connection;
        _scheme = scheme;
        int nWorking = 0;
        if(drillStatusSurface != null) {
          var offset = new Vector2(2, 25);
          var sprts = new ShapeCollection(offset, _scheme);
          sprts.Parse(sprites);
          _drillSurface = new Display(drillStatusSurface, offset, _scheme, sprts);
          ++nWorking;
        }
        if(wheelStatusSurface != null) {
          _wheelSurface = new Display(wheelStatusSurface, scheme: _scheme);
          ++nWorking;
        }
        if(nWorking == 2) {
          Logger.Inst.Log("Display... 2/2 OK");
        } else {
          Logger.Inst.Log($"Display... {2 - nWorking}/2 KO");
        }
        Scheduler.Inst.AddAction(new ScheduledAction(() => _updateScreens(armController, status, inventoryController, connection), period: 20));
      }

      void _updateScreens(ArmController armController, GeneralStatus status,
          InventoriesController inventoryController, ConnectionClient connection) {
        if(_drillSurface != null) {
          _drawDrillStatus(_drillSurface, status, inventoryController.LoadFactor, -armController.Angle, -armController.TargetAngle);
        }
        if(_wheelSurface != null) {
          _drawWheelsStatus(_wheelSurface);
        }
      }

      void _drawDrillStatus(Display screen, GeneralStatus status, float loadFactor, float angle, float targetAngle) {
        using(var f = screen.DrawFrame()) {
          if(status.AreFrontLightsOn) {
            f.DrawCollection("drillLights");
          }
          if(!float.IsNaN(angle) && !float.IsNaN(targetAngle)) {
            f.DrawCollectionTform("drillShapesArm", centerOfRotation: DRILL_ARM_CENTER, rot: targetAngle, col: _scheme.MedDark);
          }
          if(status.AreArmLightsOn) {
            f.DrawCollectionTform("drillLights", translation: DRILL_LIGHTS_OFFSET, centerOfRotation: DRILL_ARM_CENTER, rot: angle);
          }
          f.DrawCollection("drillsShapesBackground");
          f.Draw(new Shape(Sprites.Square, _scheme.MedDark, pos: new Vector2(32, 93), size: new Vector2(loadFactor * 108, 45)));
          f.DrawCollection("drillsShapesForeground");
          if(!float.IsNaN(angle)) {
            if(!float.IsNaN(targetAngle)) {
              f.DrawCollectionTform("drillShapesArm", centerOfRotation: DRILL_ARM_CENTER, rot: targetAngle, col: _scheme.MedDark);
            }
            f.DrawCollectionTform("drillShapesArm", centerOfRotation: DRILL_ARM_CENTER, rot: angle);
          }
          f.DrawTxt($"Full at {loadFactor * 100:000}%", INV_TEXT_POSITION, scale: 0.5f, al: TextAlignment.LEFT);
          f.DrawTxt(_getConnectorStatus(), CON_TEXT_POSITION, scale: 0.5f, al: TextAlignment.LEFT);
        }
      }

      string _getConnectorStatus() {
        if(_connection.State == ConnectionState.Connected) {
          return "Connected";
        } else if (_connection.State == ConnectionState.Ready) {
          if (_connection.FailureState == FailReason.Cancellation) {
            return "Request cancelled";
          } else if (_connection.FailureState == FailReason.Failure) {
            return "Request failed";
          } else if(_connection.FailureState == FailReason.User) {
            return "Disconnected";
          } else if(_connection.FailureState == FailReason.None) {
            return "Connector ready";
          } else if(_connection.FailureState == FailReason.Timeout) {
            return "No answer";
          }
        } else if(_connection.State == ConnectionState.Standby) {
          return "Connecting:\n  waitlisted";
        } else if(_connection.State == ConnectionState.WaitingCon) {
          return $"Connecting:\n  {_connection.CurrentProgress * 100:000}%";
        } else if(_connection.State == ConnectionState.WaitingDisc) {
          return $"Disconnecting:\n  {_connection.CurrentProgress * 100:000}%";
        }
        return "";
      }

      void _drawWheelsStatus(Display screen) {
        using(var frame = screen.DrawFrame()) {
        }
      }
    }
  }
}
