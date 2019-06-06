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
      static readonly Vector2 CON_TEXT_POS = new Vector2(10, 40);
      static readonly Vector2 DRILL_ARM_CENTER = new Vector2(161, 105);
      static readonly Vector2 DRILL_LIGHTS_OFFSET = new Vector2(35, -20);
      static readonly Vector2 INV_TEXT_POS = new Vector2(10, 20);

      readonly Display _drillSurface;
      readonly ColorScheme _scheme;
      readonly GeneralStatus _status;
      readonly Display _wheelSurface;

      public ScreensController(GeneralStatus status, InventoriesController inventoryController, IMyTextSurface drillStatusSurface,
          IMyTextSurface wheelStatusSurface, ColorScheme scheme, string sprites) {
        _scheme = scheme;
        if(drillStatusSurface != null) {
          var offset = new Vector2(2, 25);
          var sprts = new ShapeCollection(offset, _scheme);
          sprts.Parse(sprites);
          _drillSurface = new Display(drillStatusSurface, offset, _scheme, sprts);
        }
        if(wheelStatusSurface != null) {
          _wheelSurface = new Display(wheelStatusSurface, scheme: _scheme);
        }
        _status = status;
        Schedule(new ScheduledAction(() => _updateScreens(status, inventoryController), 20, name: "screens-update"));
      }

      void _updateScreens(GeneralStatus status, InventoriesController inventoryController) {
        if(_drillSurface != null)
          _drawDrillStatus(_drillSurface, status, inventoryController.LoadFactor, -status.ArmAngle, -status.ArmTarget);
      }

      void _drawDrillStatus(Display screen, GeneralStatus status, float loadFactor, float angle, float targetAngle) {
        using(var f = screen.DrawFrame()) {
          if(status.AreFrontLightsOn)
            f.DrawCollection("drillLights");
          if(!float.IsNaN(angle) && !float.IsNaN(targetAngle))
            f.DrawCollectionTform("drillShapesArm", centerOfRotation: DRILL_ARM_CENTER, rot: targetAngle, col: _scheme.MedDark);
          if(status.AreArmLightsOn)
            f.DrawCollectionTform("drillLights", translation: DRILL_LIGHTS_OFFSET, centerOfRotation: DRILL_ARM_CENTER, rot: angle);
          f.DrawCollection("drillsShapesBackground");
          f.Draw(new Shape("SquareSimple", _scheme.MedDark, pos: new Vector2(32, 93), size: new Vector2(loadFactor * 108, 45)));
          f.DrawCollection("drillsShapesForeground");
          if(!float.IsNaN(angle)) {
            if(!float.IsNaN(targetAngle))
              f.DrawCollectionTform("drillShapesArm", centerOfRotation: DRILL_ARM_CENTER, rot: targetAngle, col: _scheme.MedDark);
            f.DrawCollectionTform("drillShapesArm", centerOfRotation: DRILL_ARM_CENTER, rot: angle);
          }
          f.DrawTxt($"Full at {loadFactor * 100:000}%", INV_TEXT_POS, scale: 0.5f, al: TextAlignment.LEFT);
          f.DrawTxt(_conStatus(), CON_TEXT_POS, scale: 0.5f, al: TextAlignment.LEFT);
        }
      }

      string _conStatus() {
        switch(_status.ConnectionState) {
          case ConnectionState.Connected: return "Connected";
          case ConnectionState.Ready:
            switch(_status.FailReason) {
              case FailReason.Cancellation: return "Request cancelled";
              case FailReason.Failure: return "Request failed";
              case FailReason.User: return "Disconnected";
              case FailReason.None: return "Connector ready";
              case FailReason.Timeout: default: return "No answer";
            }
          case ConnectionState.Standby: return "Connecting:\n  waitlisted";
          case ConnectionState.WaitingCon: return $"Connecting:\n  {_status.Progress * 100:000}%";
          case ConnectionState.WaitingDisc: return $"Disconnecting:\n  {_status.Progress * 100:000}%";
        }
        return "";
      }
    }
  }
}
