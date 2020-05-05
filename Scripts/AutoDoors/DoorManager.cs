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
      static readonly System.Text.RegularExpressions.Regex SAS_DOOR_RE = new System.Text.RegularExpressions.Regex("^(.*) - (Inner|Outer)\\)$");
      static readonly int GRACE_PERIOD = 80;
      static readonly int TIME_TO_CLOSE = 120;

      readonly List<IMyDoor> doors = new List<IMyDoor>();
      readonly HashSet<long> handledDoors = new HashSet<long>();
      readonly HashSet<string> handledSases = new HashSet<string>();
      readonly Action<string> logger;
      readonly List<Sas> sases = new List<Sas>();
      readonly Process mainProcess;

      readonly List<IMyDoor> tmpDoorList = new List<IMyDoor>();
      readonly Dictionary<string, IMyDoor> tmpSases = new Dictionary<string, IMyDoor>();

      public DoorManager(MyGridProgram program, IProcessSpawner spawner, Action<string> logger) {
        this.logger = logger;
        this.mainProcess = spawner.Spawn(this._handleDoors, "autodoors-main");
        this.Scan(program);
        this.mainProcess.Spawn(p => this.Scan(program), "scan", period: 431);
      }

      public void Scan(MyGridProgram program) {
        this.doors.Clear();
        this.sases.Clear();

        program.GridTerminalSystem.GetBlocksOfType(this.tmpDoorList, d => d.CubeGrid == program.Me.CubeGrid && d.IsFunctional);
        foreach (IMyDoor door in this.tmpDoorList) {
          var match = SAS_DOOR_RE.Match(door.DisplayNameText);
          if (match != null) {
            IMyDoor otherDoor = this.tmpSases.GetValueOrDefault(match.Groups[1].Value);
            if (otherDoor != null) {
              this.sases.Add(new Sas($"{match.Groups[1].Value})", door, otherDoor));
              this.tmpSases.Remove(match.Groups[1].Value);
            } else {
              this.tmpSases.Add(match.Groups[1].Value, door);
            }
          } else {
            this.doors.Add(door);
          }
        }
        this.doors.AddRange(this.tmpSases.Values);
        this.logger?.Invoke($"Found {this.doors.Count} doors and {this.sases.Count} sases");

        this.tmpSases.Clear();
      }

      void _handleDoors(Process p) {
        foreach(IMyDoor door in this.doors) {
          if (door.OpenRatio > 0) {
            if (!this.handledDoors.Contains(door.EntityId)) {
              this.logger?.Invoke($"Will close door {door.DisplayNameText}");
              this.mainProcess.Spawn(pc => this.closeDoor(door), $"close-door {door.DisplayNameText}", period: TIME_TO_CLOSE, useOnce: true);
              this.handledDoors.Add(door.EntityId);
            }
          }
        }
        foreach(Sas sas in this.sases) {
          if (sas.IsOpen()) {
            if (!this.handledSases.Contains(sas.Name)) {
              this.logger?.Invoke($"Will close sas {sas.Name}");
              sas.Lock();
              this.handledSases.Add(sas.Name);
              this.mainProcess.Spawn(pc => this.closeSas(sas), $"close-sas {sas.Name}", period: TIME_TO_CLOSE, useOnce: true);
            }
          }
        }
      }

      void closeDoor(IMyDoor door) {
        door.CloseDoor();
        this.mainProcess.Spawn(pc => this.handledDoors.Remove(door.EntityId), $"cleanup-door {door.DisplayNameText}", period: GRACE_PERIOD, useOnce: true);
      }

      void closeSas(Sas sas) {
        sas.Close();
        this.mainProcess.Spawn(pc => this.unlockSas(sas), $"unlock-sas {sas.Name}", period: GRACE_PERIOD, useOnce: true);
      }

      void unlockSas(Sas sas) {
        sas.Unlock();
        this.handledSases.Remove(sas.Name);
      }
    }
  }
}
