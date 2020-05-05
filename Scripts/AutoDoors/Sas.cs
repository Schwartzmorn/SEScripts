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
    public class Sas {
      public readonly string Name;

      private readonly List<IMyDoor> doors = new List<IMyDoor>(2);

      public Sas(string name, IMyDoor doorA, IMyDoor doorB) {
        this.Name = name;
        this.doors.Add(doorA);
        this.doors.Add(doorB);
      }

      public bool IsOpen() => this.doors.Any(d => d.OpenRatio > 0);

      public void Lock() => this.doors.ForEach(d => d.Enabled = d.OpenRatio > 0);

      public void Close() => this.doors.ForEach(d => d.CloseDoor());

      public void Unlock() => this.doors.ForEach(d => d.Enabled = true);
    }
  }
}
