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
      static readonly Vector2 CON_TEXT_POS = new Vector2(12, 27);
      static readonly Vector2 DRILL_ARM_CENTER = new Vector2(163, 92);
      static readonly Vector2 INV_TEXT_POS = new Vector2(12, 7);
      static readonly Vector2 LIGHTS_OFFSET = new Vector2(132, 112);
      static readonly Vector2 STATUS_RECT_POS = new Vector2(8, 155);
      static readonly Vector2 VEHICLE_OFFSET = new Vector2(2, -13);
      static readonly float STATUS_RECT_PADDING = 10;
      static readonly float DETECTOR_BAR_WIDTH = 10;
      static readonly float DETECTOR_OFFSET = 2;
      static readonly float SIDE_DETECTOR_BRACKET_LENGTH = 40;
      // static readonly Log LOG = Log.GetLog("SC");

      readonly Display _drillDisplay;
      readonly Display _sensorDisplay;
      readonly GeneralStatus _status;
      readonly SensorManager _sensorManager;
      readonly List<SensorDetection> _tempDetection = new List<SensorDetection>();
      readonly ColorScheme _scheme;
      bool _previouslyForward;

      public ScreensController(GeneralStatus status, InventoryWatcher invWatcher, IMyTextSurface drillStatusSurface, IMyTextSurface sensorDisplay,
          string sprites, IProcessSpawner spawner, SensorManager sensorManager)
      {
        _scheme = new ColorScheme(drillStatusSurface.ScriptForegroundColor, drillStatusSurface.ScriptBackgroundColor);
        var sprts = new ShapeCollections(_scheme);
        sprts.Parse(sprites);
        _drillDisplay = new Display(drillStatusSurface, scheme: _scheme, sprites: sprts);
        _sensorDisplay = new Display(sensorDisplay, scheme: _scheme);
        _status = status;
        _sensorManager = sensorManager;
        spawner.Spawn(p => _updateScreens(status, invWatcher), "screens-update", period: 10);
      }

      void _updateScreens(GeneralStatus status, InventoryWatcher invWatcher)
      {
        _drawStatus(_drillDisplay, status, invWatcher.LoadFactor, -status.ArmAngle, -status.ArmTarget);
        _drawSensorStatus();
      }

      void _drawStatus(Display screen, GeneralStatus status, float loadFactor, float angle, float targetAngle)
      {
        using (Display.Frame f = screen.DrawFrame())
        {
          if (status.AreFrontLightsOn)
          {
            f.DrawCollection("drillLights", LIGHTS_OFFSET);
          }

          if (status.AreArmLightsOn)
          {
            f.DrawCollection("drillLights", DRILL_ARM_CENTER, angle);
          }

          f.DrawCollection("drillsShapesBackground", VEHICLE_OFFSET);
          f.Draw(new Shape("SquareSimple", _scheme.MedDark, new Vector2(34, 80), new Vector2(loadFactor * 108, 45)));
          f.DrawCollection("drillsShapesForeground", VEHICLE_OFFSET);
          if (!float.IsNaN(angle))
          {
            if (!float.IsNaN(targetAngle))
            {
              f.DrawCollection("drillShapesArm", DRILL_ARM_CENTER, targetAngle, _scheme.MedDark);
            }

            f.DrawCollection("drillShapesArm", DRILL_ARM_CENTER, angle);
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
        f.Draw(new Shape("SquareSimple", active ? _scheme.Light : _scheme.MedDark, position, new Vector2(width, 20)));
        f.DrawText(text, position + new Vector2(5, 2), _scheme.Dark, 0.5f, TextAlignment.LEFT);
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

      void _drawSensorStatus()
      {
        using (var f = _sensorDisplay.DrawFrame())
        {
          _sensorManager.GetDetections(SensorDirection.Left, _tempDetection);
          _drawSideSensorStatus(f, _tempDetection, true, true);
          _drawSideSensorStatus(f, _tempDetection, true, false);
          _sensorManager.GetDetections(SensorDirection.Right, _tempDetection);
          _drawSideSensorStatus(f, _tempDetection, false, true);
          _drawSideSensorStatus(f, _tempDetection, false, false);

          _sensorManager.GetDetections(SensorDirection.Forward, _tempDetection);
          _drawFrontSensorStatus(f, _tempDetection, true, true);
          _drawFrontSensorStatus(f, _tempDetection, true, false);
          _sensorManager.GetDetections(SensorDirection.Forward, _tempDetection);
          _drawFrontSensorStatus(f, _tempDetection, false, true);
          _drawFrontSensorStatus(f, _tempDetection, false, false);

          _drawDepthField(f);
        }
      }

      void _drawSideSensorStatus(Display.Frame f, List<SensorDetection> detections, bool isLeft, bool isLong)
      {
        var neededDetection = (int)(isLong ? SensorDetection.Long : SensorDetection.Short);
        var offset = isLong ? 0 : DETECTOR_BAR_WIDTH + DETECTOR_OFFSET;
        var barHeight = (f._d.SurfaceSize.Y - (DETECTOR_OFFSET * (detections.Count - 1)) - 2 * offset) / detections.Count;

        var barSize = new Vector2(DETECTOR_BAR_WIDTH, barHeight);
        var bracketLength = isLong ? SIDE_DETECTOR_BRACKET_LENGTH : (SIDE_DETECTOR_BRACKET_LENGTH - offset);

        var currentPosition = new Vector2(isLeft ? offset : f._d.SurfaceSize.X - offset - DETECTOR_BAR_WIDTH, offset);
        for (var i = 0; i < detections.Count; ++i)
        {
          f.Draw(new Shape("SquareSimple", (int)detections[i] >= neededDetection ? _scheme.Light : _scheme.MedDark, currentPosition, barSize));
          currentPosition.Y += barHeight + DETECTOR_OFFSET;
        }

        barSize = new Vector2(SIDE_DETECTOR_BRACKET_LENGTH - offset, DETECTOR_BAR_WIDTH);
        currentPosition = isLeft ? new Vector2(offset, offset) : new Vector2(f._d.SurfaceSize.X - SIDE_DETECTOR_BRACKET_LENGTH, offset);
        f.Draw(new Shape("SquareSimple", (int)detections[0] >= neededDetection ? _scheme.Light : _scheme.MedDark, currentPosition, barSize));
        currentPosition.Y = f._d.SurfaceSize.Y - DETECTOR_BAR_WIDTH - offset;
        f.Draw(new Shape("SquareSimple", (int)detections[detections.Count - 1] >= neededDetection ? _scheme.Light : _scheme.MedDark, currentPosition, barSize));
      }

      void _drawFrontSensorStatus(Display.Frame f, List<SensorDetection> detections, bool isFront, bool isLong)
      {
        var neededDetection = (int)(isLong ? SensorDetection.Long : SensorDetection.Short);
        var offset = isLong ? 0 : DETECTOR_BAR_WIDTH + DETECTOR_OFFSET;
        var barLength = (f._d.SurfaceSize.X - (SIDE_DETECTOR_BRACKET_LENGTH * 2) - DETECTOR_OFFSET * (detections.Count + 1)) / detections.Count;
        var currentPosition = new Vector2(SIDE_DETECTOR_BRACKET_LENGTH + DETECTOR_OFFSET, isFront ? offset : f._d.SurfaceSize.Y - offset - DETECTOR_BAR_WIDTH);

        var barSize = new Vector2(barLength, DETECTOR_BAR_WIDTH);

        for (var i = 0; i < detections.Count; ++i)
        {
          f.Draw(new Shape("SquareSimple", (int)detections[i] >= neededDetection ? _scheme.Light : _scheme.MedDark, currentPosition, barSize));
          currentPosition.X += barLength + DETECTOR_OFFSET;
        }
      }

      void _drawDepthField(Display.Frame f)
      {
        // Done this way to make the decision "sticky"
        if (_sensorManager.ReferenceController.MoveIndicator.Z > 0)
        {
          _previouslyForward = false;
        }
        else if (_sensorManager.ReferenceController.MoveIndicator.Z < 0)
        {
          _previouslyForward = true;
        }
        var camera = _previouslyForward ? _sensorManager.CameraHandler.ForwardCamera : _sensorManager.CameraHandler.BackwardCamera;

        var squareSide = f._d.SurfaceSize.Y - 4 * (DETECTOR_BAR_WIDTH + DETECTOR_OFFSET);
        var offset = f._d.SurfaceSize / 2;
        offset.X -= squareSide / 2;
        offset.Y -= squareSide / 2;
        var aspectRatio = camera.ConeLimit.X / camera.ConeLimit.Y;
        var imageSize = new Vector2(squareSide, squareSide);
        if (aspectRatio < 1)
        {
          offset.X += squareSide * (1 - aspectRatio) / 2;
          imageSize.X *= aspectRatio;
        }
        else if (aspectRatio > 1)
        {
          offset.Y += squareSide * (1 - 1 / aspectRatio) / 2;
          imageSize.Y /= aspectRatio;
        }
        var pixelSize = imageSize / camera.Resolution;

        for (var x = 0; x < camera.Resolution.X; ++x)
        {
          for (var y = 0; y < camera.Resolution.Y; ++y)
          {
            var col = (float)Math.Max(0, 1 - (camera.DepthField[x + y * camera.Resolution.X] / camera.RaycastLength));
            var color = new Color(
              (byte)MathHelper.Lerp(_scheme.Dark.R, _scheme.Light.R, col),
              (byte)MathHelper.Lerp(_scheme.Dark.G, _scheme.Light.G, col),
              (byte)MathHelper.Lerp(_scheme.Dark.B, _scheme.Light.B, col)
            );

            f.Draw(new Shape("SquareSimple", color, new Vector2(pixelSize.X * x, pixelSize.Y * y) + offset, pixelSize));
          }
        }
      }
    }
  }
}
