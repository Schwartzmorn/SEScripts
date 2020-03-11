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
  partial class Program: MyGridProgram {

    public Program() {
      var cockpit = this.GridTerminalSystem.GetBlockWithName("Cockpit");
      var t = new CoordinatesTransformer(cockpit);
      var wheels = new List<IMyMotorSuspension>();
      GridTerminalSystem.GetBlocksOfType(wheels);
      var screen = Me.GetSurface(0).WriteText(string.Join("\n", wheels.Select(w => $"{w.CustomName}: Pos: {t.Pos(w.GetPosition())} Left: {t.Dir(w.WorldMatrix.Left)} Up: {t.Dir(w.WorldMatrix.Up)} Forward: {t.Dir(w.WorldMatrix.Forward)}"))
       + $"\n Left: {t.Dir(cockpit.WorldMatrix.Left)} Up: {t.Dir(cockpit.WorldMatrix.Up)} Forward: {t.Dir(cockpit.WorldMatrix.Forward)}");
    }

    public void Save() {
    }

    public void Main(string argument, UpdateType updateSource) {
    }
  }
}