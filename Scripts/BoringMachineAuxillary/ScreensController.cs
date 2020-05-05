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
          IMyTextSurface wheelStatusSurface, ColorScheme scheme, string sprites, IProcessSpawner spawner) {
        this._scheme = scheme;
        if(drillStatusSurface != null) {
          var offset = new Vector2(2, 25);
          var sprts = new ShapeCollections(this._scheme);
          sprts.Parse(sprites);
          this._drillSurface = new Display(drillStatusSurface, offset, scheme: this._scheme, sprites: sprts);
        }
        if(wheelStatusSurface != null) {
          this._wheelSurface = new Display(wheelStatusSurface, scheme: this._scheme);
        }
        this._status = status;
        spawner.Spawn(p => this._updateScreens(status, inventoryController), "screens-update", period: 20);
      }

      void _updateScreens(GeneralStatus status, InventoriesController inventoryController) {
        if(this._drillSurface != null) {
          this._drawDrillStatus(this._drillSurface, status, inventoryController.LoadFactor, -status.ArmAngle, -status.ArmTarget);
        }
      }

      void _drawDrillStatus(Display screen, GeneralStatus status, float loadFactor, float angle, float targetAngle) {
        using(Display.Frame f = screen.DrawFrame()) {
          if(status.AreFrontLightsOn) {
            f.DrawCollection("drillLights");
          }

          if (!float.IsNaN(angle) && !float.IsNaN(targetAngle)) {
            f.DrawCollection("drillShapesArm", translation: DRILL_ARM_CENTER, rotation: targetAngle, color: this._scheme.MedDark);
          }

          if (status.AreArmLightsOn) {
            f.DrawCollection("drillLights", translation: DRILL_LIGHTS_OFFSET, rotation: angle);
          }

          f.DrawCollection("drillsShapesBackground");
          f.Draw(new Shape("SquareSimple", this._scheme.MedDark, position: new Vector2(32, 93), size: new Vector2(loadFactor * 108, 45)));
          f.DrawCollection("drillsShapesForeground");
          if(!float.IsNaN(angle)) {
            if(!float.IsNaN(targetAngle)) {
              f.DrawCollection("drillShapesArm", rotation: targetAngle, color: this._scheme.MedDark);
            }

            f.DrawCollection("drillShapesArm", rotation: angle);
          }
          f.DrawText($"Full at {loadFactor * 100:000}%", INV_TEXT_POS, scale: 0.5f, alignment: TextAlignment.LEFT);
          f.DrawText(this._conStatus(), CON_TEXT_POS, scale: 0.5f, alignment: TextAlignment.LEFT);
        }
      }

      string _conStatus() {
        switch(this._status.ConnectionState) {
          case ConnectionState.Connected: return "Connected";
          case ConnectionState.Ready:
            switch(this._status.FailReason) {
              case FailReason.Cancellation: return "Request cancelled";
              case FailReason.Failure: return "Request failed";
              case FailReason.User: return "Disconnected";
              case FailReason.None: return "Connector ready";
              case FailReason.Timeout: default: return "No answer";
            }
          case ConnectionState.Standby: return "Connecting:\n  waitlisted";
          case ConnectionState.WaitingCon: return $"Connecting:\n  {this._status.Progress * 100:000}%";
          case ConnectionState.WaitingDisc: return $"Disconnecting:\n  {this._status.Progress * 100:000}%";
        }
        return "";
      }
    }
  }
}
