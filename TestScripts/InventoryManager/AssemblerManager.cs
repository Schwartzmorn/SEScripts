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
    public class AssemblerManager {
      public AssemblerManager(IMyGridTerminalSystem GTS, GridManager gridManager) {
        Scan(GTS, gridManager);
      }
      public void Scan(IMyGridTerminalSystem GTS, GridManager gridManager) {
        _assemblers.Clear();
        GTS.GetBlocksOfType(_assemblers, a => a.GetInventory(1) != null && gridManager.Manages(a.CubeGrid));
      }
      List<IMyAssembler> _assemblers = new List<IMyAssembler>();
    }

  }
}

