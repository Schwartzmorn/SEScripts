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

namespace IngameScript {
  partial class Program {
    public class DoorManager {
      private static readonly System.Text.RegularExpressions.Regex SAS_DOOR_RE = new System.Text.RegularExpressions.Regex("^(.*) - (Inner|Outer)\\)$");
      private static readonly int GRACE_PERIOD = 80;
      private static readonly int TIME_TO_CLOSE = 120;

      private readonly List<IMyDoor> _doors = new List<IMyDoor>();
      private readonly List<Sas> _sases = new List<Sas>();
      private readonly HashSet<long> _handledDoors = new HashSet<long>();
      private readonly HashSet<string> _handledSases = new HashSet<string>();

      private readonly List<IMyDoor> _tmpDoorList = new List<IMyDoor>();
      private readonly Dictionary<string, IMyDoor> _tmpSases = new Dictionary<string, IMyDoor>();

      public DoorManager(MyGridProgram program) {
        Scan(program);
        Schedule(new ScheduledAction(() => Scan(program), period: 431));
        Schedule(_handleDoors);
      }

      public void Scan(MyGridProgram program) {
        _doors.Clear();
        _sases.Clear();

        program.GridTerminalSystem.GetBlocksOfType(_tmpDoorList, d => d.CubeGrid == program.Me.CubeGrid && d.IsFunctional);
        foreach (var door in _tmpDoorList) {
          var match = SAS_DOOR_RE.Match(door.DisplayNameText);
          if (match != null) {
            var otherDoor = _tmpSases.GetValueOrDefault(match.Groups[1].Value);
            if (otherDoor != null) {
              _sases.Add(new Sas($"{match.Groups[1].Value})", door, otherDoor));
              _tmpSases.Remove(match.Groups[1].Value);
            } else {
              _tmpSases.Add(match.Groups[1].Value, door);
            }
          } else {
            _doors.Add(door);
          }
        }
        _doors.AddRange(_tmpSases.Values);
        Log($"Found {_doors.Count} doors and {_sases.Count} sases");

        _tmpSases.Clear();
      }

      private void _handleDoors() {
        foreach(var door in _doors) {
          if (door.OpenRatio > 0) {
            if (!_handledDoors.Contains(door.EntityId)) {
              Log($"Will close door {door.DisplayNameText}");
              Schedule(new ScheduledAction(() => _closeDoor(door), period: TIME_TO_CLOSE, useOnce: true));
              _handledDoors.Add(door.EntityId);
            }
          }
        }
        foreach(var sas in _sases) {
          if (sas.IsOpen()) {
            if (!_handledSases.Contains(sas.Name)) {
              Log($"Will close sas {sas.Name}");
              sas.Lock();
              _handledSases.Add(sas.Name);
              Schedule(new ScheduledAction(() => _closeSas(sas), period: TIME_TO_CLOSE, useOnce: true));
            }
          }
        }
      }

      private void _closeDoor(IMyDoor door) {
        door.CloseDoor();
        Schedule(new ScheduledAction(() => _handledDoors.Remove(door.EntityId), period: GRACE_PERIOD, useOnce: true));
      }

      private void _closeSas(Sas sas) {
        sas.Close();
        Schedule(new ScheduledAction(() => _unlockSas(sas), period: GRACE_PERIOD, useOnce: true));
      }

      private void _unlockSas(Sas sas) {
        sas.Unlock();
        _handledSases.Remove(sas.Name);
      }
    }
  }
}
