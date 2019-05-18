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
  partial class Program {
    public class GeneralStatus {
      private readonly List<IMyReflectorLight> _armLights = new List<IMyReflectorLight>();
      private readonly List<IMyReflectorLight> _frontLights = new List<IMyReflectorLight>();

      public GeneralStatus(MyGridProgram program) {
        var gts = program.GridTerminalSystem;
        var grid = program.Me.CubeGrid;
        gts.GetBlocksOfType(_armLights, l => grid != l.CubeGrid && l.CubeGrid.IsSameConstructAs(grid));
        gts.GetBlocksOfType(_frontLights, l => l.CubeGrid == grid && l.DisplayNameText.Contains("Front"));
      }

      public bool AreArmLightsOn => _armLights.Any(l => l.Enabled);

      public bool AreFrontLightsOn => _frontLights.Any(l => l.Enabled);
    }
  }
}
