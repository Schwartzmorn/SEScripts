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

namespace IngameScript
{
  partial class Program
  {
    public class AssemblerManager : LazyFilter, IOutputInventoryCollection, IInputInventoryCollection
    {
      readonly List<IMyAssembler> _assemblers = new List<IMyAssembler>();

      public string Name => "Assemblers manager";

      public AssemblerManager(IMyGridTerminalSystem gts, GridManager gridManager, IProcessSpawner spawner)
      {
        Scan(gts, gridManager);
        spawner.Spawn(p => Scan(gts, gridManager), "assembler-scanner", period: 100);
      }

      public void Scan(IMyGridTerminalSystem gts, GridManager gridManager)
      {
        _assemblers.Clear();
        gts.GetBlocksOfType(_assemblers, a => a.GetInventory(1) != null && gridManager.Manages(a.CubeGrid));
      }

      public IEnumerable<IMyInventory> GetOutputInventories()
      {
        FilterLazily();
        return _assemblers.Where(a => a.Mode == MyAssemblerMode.Assembly).Select(a => a.GetInventory(1));
      }

      public IEnumerable<IMyInventory> GetInputInventories()
      {
        FilterLazily();
        return _assemblers.Select(a => a.GetInventory(0));
      }

      protected override void Filter() => _assemblers.RemoveAll(c => c.GetInventory(1) == null);
    }
  }
}
