using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing.Text;
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
    public enum SensorDirection
    {
      Forward = 0,
      Left = 1,
      Backward = 2,
      Right = 3,
    }

    public enum SensorDetection
    {
      None = 0,
      Long = 1,
      Short = 2
    }

    public class SensorManager
    {

      static readonly Log LOG = Log.GetLog("SM");

      private class VirtualSensor
      {
        readonly List<IMySensorBlock> _longSensors = new List<IMySensorBlock>();
        readonly List<IMySensorBlock> _shortSensors = new List<IMySensorBlock>();

        readonly List<MyDetectedEntityInfo> _tmpDetection = new List<MyDetectedEntityInfo>();

        public VirtualSensor(IMyTerminalBlock reference, IMyGridTerminalSystem gts, SensorDirection sensorDirection)
        {
          _getSensors(reference, gts, "Long", _longSensors, sensorDirection);
          _getSensors(reference, gts, "Short", _shortSensors, sensorDirection);
          if (_longSensors.Count != _shortSensors.Count)
          {
            LOG.Error($"Virtual sensor {sensorDirection} has inconsistent sensors.");
          }
        }

        static void _getSensors(IMyTerminalBlock reference, IMyGridTerminalSystem gts, string nameDiscriminator, List<IMySensorBlock> sensors, SensorDirection direction)
        {
          var refMatrix = reference.WorldMatrix;
          var dir = refMatrix.Forward;
          switch (direction)
          {
            case SensorDirection.Right: dir = refMatrix.Right; break;
            case SensorDirection.Backward: dir = refMatrix.Backward; break;
            case SensorDirection.Left: dir = refMatrix.Left; break;
          }

          gts.GetBlocksOfType(sensors, s => s.CubeGrid == reference.CubeGrid && s.WorldMatrix.Forward.Dot(dir) > 0.95 && s.CustomName.Contains(nameDiscriminator));
          if (sensors.Count == 0)
          {
            LOG.Error($"Virtual sensor {direction} - {nameDiscriminator} is incomplete");
          }

          // we want the sensors ordered left to right and front to back
          var orderer = direction == SensorDirection.Forward || direction == SensorDirection.Backward ? refMatrix.Right : refMatrix.Backward;
          sensors.Sort((a, b) => a.WorldMatrix.Translation.Dot(orderer).CompareTo(b.WorldMatrix.Translation.Dot(orderer)));
        }

        public void GetDetections(List<SensorDetection> triggeredSensors)
        {
          triggeredSensors.Clear();

          for (var i = 0; i < Math.Min(_longSensors.Count, _shortSensors.Count); ++i)
          {
            _tmpDetection.Clear();
            _shortSensors[i].DetectedEntities(_tmpDetection);
            if (_tmpDetection.Count > 0)
            {
              triggeredSensors.Add(SensorDetection.Short);
            }
            else
            {
              _tmpDetection.Clear();
              _longSensors[i].DetectedEntities(_tmpDetection);
              triggeredSensors.Add(_tmpDetection.Count > 0 ? SensorDetection.Long : SensorDetection.None);
            }
          }
        }
      }

      readonly VirtualSensor[] _sensors;
      public readonly CameraHandler CameraHandler;
      public readonly IMyShipController ReferenceController;
      readonly List<SensorDetection> _tmpDetections = new List<SensorDetection>();

      public SensorManager(IMyShipController reference, IMyGridTerminalSystem gts, IProcessManager manager)
      {
        ReferenceController = reference;
        _sensors = new VirtualSensor[]{
          new VirtualSensor(reference, gts, SensorDirection.Forward),
          new VirtualSensor(reference, gts, SensorDirection.Left),
          new VirtualSensor(reference, gts, SensorDirection.Backward),
          new VirtualSensor(reference, gts, SensorDirection.Right)
        };
        CameraHandler = new CameraHandler(reference, gts, manager);
      }

      public void GetDetections(SensorDirection direction, List<SensorDetection> detections)
      {
        _sensors[(int)direction].GetDetections(detections);
      }

      public SensorDirection GetSafeDirection(bool forward)
      {
        var dir = forward ? SensorDirection.Forward : SensorDirection.Backward;
        GetDetections(dir, _tmpDetections);
        if (_tmpDetections.All(d => d == SensorDetection.None))
        {
          return dir;
        }
        var averageIndex = _averageIndex(SensorDetection.Short);
        if (averageIndex != -1)
        {
          return averageIndex >= _tmpDetections.Count / 2 ? SensorDirection.Right : SensorDirection.Left;
        }
        averageIndex = _averageIndex(SensorDetection.Long);
        return averageIndex >= _tmpDetections.Count / 2 ? SensorDirection.Right : SensorDirection.Left;
      }

      int _averageIndex(SensorDetection detection)
      {
        var res = 0;
        var found = 0;

        for (var i = 0; i < _tmpDetections.Count; ++i)
        {
          if (_tmpDetections[i] == detection)
          {
            res += i;
            ++found;
          }
        }

        return found == 0 ? -1 : res / found;
      }
    }
  }
}
