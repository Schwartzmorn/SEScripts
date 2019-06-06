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
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
  partial class Program {
    public void ScanRefineries(IMyGridTerminalSystem GTS, GridManager gridManager, bool full) {
      if (full) {
        GTS.GetBlocksOfType(_refineries, r => r.GetInventory(1) != null && gridManager.Manages(r.CubeGrid));
      } else {
        _refineries = _refineries.FindAll(r => r.GetInventory(1) != null);
      }
    }

    List<IMyRefinery> _refineries = new List<IMyRefinery>();
  }
}
