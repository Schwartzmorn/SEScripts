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
      static readonly Vector2 INV_TEXT_POS = new Vector2(10, 20);
      static readonly Vector2 LIGHTS_OFFSET = new Vector2(130, 125);

      readonly List<Display> drillDisplays;
      readonly ColorScheme scheme;
      readonly GeneralStatus status;
      //readonly List<Display> wheelDisplays;

      public ScreensController(GeneralStatus status, InventoryWatcher invWatcher, IEnumerable<IMyTextSurface> drillStatusSurfaces,
          IEnumerable<IMyTextSurface> wheelStatusSurfaces, ColorScheme scheme, string sprites, IProcessSpawner spawner) {
        this.scheme = scheme;
        var sprts = new ShapeCollections(this.scheme);
        sprts.Parse(sprites);
        this.drillDisplays = drillStatusSurfaces.Select(s => new Display(s, new Vector2(2, 25), scheme: this.scheme, sprites: sprts)).ToList();
        //this.wheelDisplays = wheelStatusSurfaces.Select(s => new Display(s, new Vector2(2, 25), scheme: this.scheme)).ToList();
        this.status = status;
        spawner.Spawn(p => this.updateScreens(status, invWatcher), "screens-update", period: 20);
      }

      void updateScreens(GeneralStatus status, InventoryWatcher invWatcher) {
        foreach(Display display in this.drillDisplays) {
          this.drawDrillStatus(display, status, invWatcher.LoadFactor, -status.ArmAngle, -status.ArmTarget);
        }
      }

      void drawDrillStatus(Display screen, GeneralStatus status, float loadFactor, float angle, float targetAngle) {
        using(Display.Frame f = screen.DrawFrame()) {
          if(status.AreFrontLightsOn) {
            f.DrawCollection("drillLights", translation: LIGHTS_OFFSET);
          }

          if (status.AreArmLightsOn) {
            f.DrawCollection("drillLights", translation: DRILL_ARM_CENTER, rotation: angle);
          }

          f.DrawCollection("drillsShapesBackground");
          f.Draw(new Shape("SquareSimple", this.scheme.MedDark, position: new Vector2(32, 93), size: new Vector2(loadFactor * 108, 45)));
          f.DrawCollection("drillsShapesForeground");
          if(!float.IsNaN(angle)) {
            if(!float.IsNaN(targetAngle)) {
              f.DrawCollection("drillShapesArm", translation: DRILL_ARM_CENTER, rotation: targetAngle, color: this.scheme.MedDark);
            }

            f.DrawCollection("drillShapesArm", translation: DRILL_ARM_CENTER, rotation: angle);
          }
          f.DrawText($"Full at {loadFactor * 100:000}%", INV_TEXT_POS, scale: 0.5f, alignment: TextAlignment.LEFT);
          f.DrawText(this.conStatus(), CON_TEXT_POS, scale: 0.5f, alignment: TextAlignment.LEFT);
        }
      }

      string conStatus() {
        switch(this.status.ConnectionState) {
          case ConnectionState.Connected: return "Connected";
          case ConnectionState.Ready:
            switch(this.status.FailReason) {
              case FailReason.Cancellation: return "Request cancelled";
              case FailReason.Failure: return "Request failed";
              case FailReason.User: return "Disconnected";
              case FailReason.None: return "Connector ready";
              case FailReason.Timeout: default: return "No answer";
            }
          case ConnectionState.Standby: return "Connecting:\n  waitlisted";
          case ConnectionState.WaitingCon: return $"Connecting:\n  {this.status.Progress * 100:000}%";
          case ConnectionState.WaitingDisc: return $"Disconnecting:\n  {this.status.Progress * 100:000}%";
        }
        return "";
      }
    }
  }
}
