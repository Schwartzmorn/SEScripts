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
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;
using System.Runtime.InteropServices;

namespace IngameScript
{
  partial class Program
  {
    public class VirtualCamera
    {
      public readonly double RaycastLength;
      public double[] DepthField { get; private set; }
      public readonly Vector2I Resolution;
      private readonly double[] _depthFieldA;
      private readonly double[] _depthFieldB;
      private double[] _currentDepthField;

      readonly static Log LOG = Log.GetLog("VC");
      // Bool is true if the camera is upside down
      readonly List<MyTuple<IMyCameraBlock, bool>> _cameras;
      public readonly Vector2 ConeLimit;
      readonly Vector2 _coneStep;

      Vector2I _currentPixel = Vector2I.Zero;

      public VirtualCamera(IMyTerminalBlock reference, List<IMyCameraBlock> cameras, Vector2I resolution, Vector2 fieldOfView, double raycastLength)
      {
        LOG.Debug($"{4} camera(s) with resolution {resolution}");
        RaycastLength = raycastLength;
        cameras.Sort((a, b) =>
        {
          var relativePosition = b.WorldMatrix.Translation - a.WorldMatrix.Translation;
          return (int)Math.Round(relativePosition.Dot(reference.WorldMatrix.Up) * 100 + relativePosition.Dot(reference.WorldMatrix.Left) * 10);
        });
        _cameras = cameras.Select(c => MyTuple.Create(c, c.WorldMatrix.Up.Dot(reference.WorldMatrix.Up) < 0)).ToList();
        LOG.Debug("Cameras are in the following order:");
        foreach (var camera in _cameras)
        {
          camera.Item1.Enabled = true;
          camera.Item1.EnableRaycast = true;
          LOG.Debug($"{(camera.Item2 ? "V" : "^")} {camera.Item1.CustomName}");
        }
        Resolution = resolution;
        _depthFieldA = new double[resolution.X * resolution.Y];
        _depthFieldB = new double[_depthFieldA.Length];
        _currentDepthField = _depthFieldA;
        DepthField = _depthFieldB;
        var limit = _cameras[0].Item1.RaycastConeLimit;
        ConeLimit = new Vector2(MathHelper.Min(fieldOfView.X / 2, limit), MathHelper.Min(fieldOfView.Y / 2, limit));
        _coneStep = ConeLimit * 2 / (Resolution - 1);
      }

      /// <summary>
      /// Updates the depth with one raycast per camera
      /// </summary>
      /// <returns>True iff the depth field has been entirely updated</returns>
      public bool UpdateDepthField()
      {
        foreach (var camera in _cameras)
        {
          int x = _currentPixel.X;
          int y = _currentPixel.Y;
          if (camera.Item2)
          {
            x = Resolution.X - x - 1;
            y = Resolution.Y - y - 1;
          }
          float yaw = -ConeLimit.X + (x * _coneStep.X);
          float pitch = ConeLimit.Y - (y * _coneStep.Y);

          var detection = camera.Item1.Raycast(RaycastLength, pitch, yaw);
          // TODO review that
          if (detection.HitPosition.HasValue)
          {
            // We use always the first camera as the reference
            _currentDepthField[_currentPixel.X + _currentPixel.Y * Resolution.X] = (detection.HitPosition.GetValueOrDefault() - _cameras[0].Item1.WorldMatrix.Translation).Length();
          }
          else
          {
            _currentDepthField[_currentPixel.X + _currentPixel.Y * Resolution.X] = RaycastLength;
          }

          if (!_nextPixel())
          {
            DepthField = _currentDepthField;
            _currentDepthField = _currentDepthField == _depthFieldA ? _depthFieldB : _depthFieldA;

            return true;
          }
        }
        return false;
      }

      /// <summary>
      /// computes the next pixel to raycast, starting from 0, 0 and going diagonal by diagonal
      /// </summary>
      /// <returns>false if there is no pixel left</returns>
      bool _nextPixel()
      {
        _currentPixel.X -= 1;
        _currentPixel.Y += 1;
        if (_currentPixel.X != -1 && _currentPixel.Y < Resolution.Y)
        {
          // we are not done with the diagonal
          return true;
        }
        var total = _currentPixel.X + _currentPixel.Y + 1;
        if (total == Resolution.X + Resolution.Y - 1)
        {
          // we are done iterating on the pixels of the image
          _currentPixel = Vector2I.Zero;
          return false;
        }

        // we are starting a new diagonal
        _currentPixel.X = Math.Min(total, Resolution.X - 1);
        _currentPixel.Y = total - _currentPixel.X;
        return true;
      }
    }

    public class CameraHandler
    {
      readonly static Log LOG = Log.GetLog("CH");
      public VirtualCamera ForwardCamera { get; private set; }
      public VirtualCamera BackwardCamera { get; private set; }

      public CameraHandler(IMyTerminalBlock reference, IMyGridTerminalSystem gts, IProcessManager manager)
      {
        var cameras = new List<IMyCameraBlock>();
        gts.GetBlocksOfType(cameras);
        var resolution = new Vector2I(20, 4);
        var fieldOfView = new Vector2(90, 30);
        ForwardCamera = new VirtualCamera(reference, cameras.FindAll(c => c.WorldMatrix.Forward.Dot(reference.WorldMatrix.Forward) > 0.95), resolution, fieldOfView, 33);
        BackwardCamera = new VirtualCamera(reference, cameras.FindAll(c => c.WorldMatrix.Forward.Dot(reference.WorldMatrix.Forward) < -0.95), resolution, fieldOfView, 33);

        manager.Spawn(p =>
        {
          ForwardCamera.UpdateDepthField();
          BackwardCamera.UpdateDepthField();
        }, "camera-handler");
      }
    }
  }
}