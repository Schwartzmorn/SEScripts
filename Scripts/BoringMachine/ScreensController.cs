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

namespace IngameScript
{
  partial class Program
  {
    public class ScreensController
    {
      static readonly Vector2 CON_TEXT_POS = new Vector2(10, 40);
      static readonly Vector2 DRILL_ARM_CENTER = new Vector2(161, 105);
      static readonly Vector2 INV_TEXT_POS = new Vector2(10, 20);
      static readonly Vector2 LIGHTS_OFFSET = new Vector2(130, 125);
      static readonly Vector2 STATUS_RECT_POS = new Vector2(6, 168);
      static readonly float STATUS_RECT_PADDING = 10;

      readonly List<Display> _drillDisplays;
      readonly ColorScheme _scheme;
      readonly GeneralStatus _status;

      public ScreensController(GeneralStatus status, InventoryWatcher invWatcher, IEnumerable<IMyTextSurface> drillStatusSurfaces,
          ColorScheme scheme, string sprites, IProcessSpawner spawner)
      {
        _scheme = scheme;
        var sprts = new ShapeCollections(_scheme);
        sprts.Parse(sprites);
        _drillDisplays = drillStatusSurfaces.Select(s => new Display(s, new Vector2(2, 25), scheme: _scheme, sprites: sprts)).ToList();
        _status = status;
        spawner.Spawn(p => _updateScreens(status, invWatcher), "screens-update", period: 20);
      }

      void _updateScreens(GeneralStatus status, InventoryWatcher invWatcher)
      {
        foreach (Display display in _drillDisplays)
        {
          _drawDrillStatus(display, status, invWatcher.LoadFactor, -status.ArmAngle, -status.ArmTarget);
        }
      }

      void _drawDrillStatus(Display screen, GeneralStatus status, float loadFactor, float angle, float targetAngle)
      {
        using (Display.Frame f = screen.DrawFrame())
        {
          if (status.AreFrontLightsOn)
          {
            f.DrawCollection("drillLights", translation: LIGHTS_OFFSET);
          }

          if (status.AreArmLightsOn)
          {
            f.DrawCollection("drillLights", translation: DRILL_ARM_CENTER, rotation: angle);
          }

          f.DrawCollection("drillsShapesBackground");
          f.Draw(new Shape("SquareSimple", _scheme.MedDark, position: new Vector2(32, 93), size: new Vector2(loadFactor * 108, 45)));
          f.DrawCollection("drillsShapesForeground");
          if (!float.IsNaN(angle))
          {
            if (!float.IsNaN(targetAngle))
            {
              f.DrawCollection("drillShapesArm", translation: DRILL_ARM_CENTER, rotation: targetAngle, color: _scheme.MedDark);
            }

            f.DrawCollection("drillShapesArm", translation: DRILL_ARM_CENTER, rotation: angle);
          }
          f.DrawText($"Full at {loadFactor * 100:000}%", INV_TEXT_POS, scale: 0.5f, alignment: TextAlignment.LEFT);
          f.DrawText(_conStatus(), CON_TEXT_POS, scale: 0.5f, alignment: TextAlignment.LEFT);
          var statusPos = STATUS_RECT_POS;
          statusPos.X += _drawStatusRect(f, "STRAFE", status.IsStrafing, statusPos) + STATUS_RECT_PADDING;
          statusPos.X += _drawStatusRect(f, "AP", status.IsAutopilotEngaged, statusPos) + STATUS_RECT_PADDING;
          statusPos.X += _drawStatusRect(f, "(!)", status.IsParkEngaged, statusPos) + STATUS_RECT_PADDING;
        }
      }

      float _drawStatusRect(Display.Frame f, string text, bool active, Vector2 position)
      {
        var width = MathHelper.RoundToInt(f._d.Surface.MeasureStringInPixels(new StringBuilder(text), "Monospace", 0.5f).X) + 10;
        f.Draw(new Shape("SquareSimple", active ? _scheme.Light : _scheme.MedDark, position: position, size: new Vector2(width, 20)));
        f.DrawText(text, position + new Vector2(5, 2), color: _scheme.Dark, scale: 0.5f, alignment: TextAlignment.LEFT);
        return width;
      }

      string _conStatus()
      {
        switch (_status.ConnectionState)
        {
          case ConnectionState.Connected: return "Connected";
          case ConnectionState.Ready:
            switch (_status.FailReason)
            {
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
