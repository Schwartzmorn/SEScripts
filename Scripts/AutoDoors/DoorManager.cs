using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
  partial class Program
  {
    public class DoorManager
    {
      static readonly System.Text.RegularExpressions.Regex SAS_DOOR_RE = new System.Text.RegularExpressions.Regex("^(.*) - (Inner|Outer)$");
      static readonly int GRACE_PERIOD = 80;
      static readonly int TIME_TO_CLOSE = 120;

      readonly List<IMyDoor> _doors = new List<IMyDoor>();
      readonly HashSet<long> _handledDoors = new HashSet<long>();
      readonly HashSet<string> _handledSases = new HashSet<string>();
      readonly Action<string> _logger;
      readonly List<Sas> _sases = new List<Sas>();
      readonly Process _mainProcess;

      readonly List<IMyDoor> _tmpDoorList = new List<IMyDoor>();
      readonly Dictionary<string, IMyDoor> _tmpSases = new Dictionary<string, IMyDoor>();

      public DoorManager(MyGridProgram program, IProcessSpawner spawner, Action<string> logger)
      {
        _logger = logger;
        _mainProcess = spawner.Spawn(_handleDoors, "autodoors-main");
        Scan(program);
        _mainProcess.Spawn(p => Scan(program), "scan", period: 431);
      }

      public void Scan(MyGridProgram program)
      {
        _doors.Clear();
        _sases.Clear();

        program.GridTerminalSystem.GetBlocksOfType(_tmpDoorList, d => d.CubeGrid == program.Me.CubeGrid && d.IsFunctional);
        foreach (IMyDoor door in _tmpDoorList)
        {
          var match = SAS_DOOR_RE.Match(door.CustomName);
          if (match != null)
          {
            IMyDoor otherDoor = _tmpSases.GetValueOrDefault(match.Groups[1].Value);
            if (otherDoor != null)
            {
              _sases.Add(new Sas($"{match.Groups[1].Value}", door, otherDoor));
              _tmpSases.Remove(match.Groups[1].Value);
            }
            else
            {
              _tmpSases.Add(match.Groups[1].Value, door);
            }
          }
          else
          {
            _doors.Add(door);
          }
        }
        _doors.AddRange(_tmpSases.Values);
        _logger?.Invoke($"Found {_doors.Count} doors and {_sases.Count} sases");

        _tmpSases.Clear();
      }

      void _handleDoors(Process p)
      {
        foreach (IMyDoor door in _doors)
        {
          if (door.OpenRatio > 0)
          {
            if (!_handledDoors.Contains(door.EntityId))
            {
              _logger?.Invoke($"Will close door {door.CustomName}");
              _mainProcess.Spawn(pc => _closeDoor(door), $"close-door {door.CustomName}", period: TIME_TO_CLOSE, useOnce: true);
              _handledDoors.Add(door.EntityId);
            }
          }
        }
        foreach (Sas sas in _sases)
        {
          if (sas.IsOpen())
          {
            if (!_handledSases.Contains(sas.Name))
            {
              _logger?.Invoke($"Will close sas {sas.Name}");
              sas.Lock();
              _handledSases.Add(sas.Name);
              _mainProcess.Spawn(pc => _closeSas(sas), $"close-sas {sas.Name}", period: TIME_TO_CLOSE, useOnce: true);
            }
          }
        }
      }

      void _closeDoor(IMyDoor door)
      {
        door.CloseDoor();
        _mainProcess.Spawn(pc => _handledDoors.Remove(door.EntityId), $"cleanup-door {door.CustomName}", period: GRACE_PERIOD, useOnce: true);
      }

      void _closeSas(Sas sas)
      {
        sas.Close();
        _mainProcess.Spawn(pc => _unlockSas(sas), $"unlock-sas {sas.Name}", period: GRACE_PERIOD, useOnce: true);
      }

      void _unlockSas(Sas sas)
      {
        sas.Unlock();
        _handledSases.Remove(sas.Name);
      }
    }
  }
}
