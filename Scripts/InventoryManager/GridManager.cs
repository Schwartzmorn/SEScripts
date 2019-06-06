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
    public class GridManager {
      static readonly string INI_SECTION = "grid-manager";

      readonly IMyProgrammableBlock _block;
      readonly List<MetaGrid> _metaGrids = new List<MetaGrid>();
      readonly HashSet<string> _managedGrids;

      readonly List<IMyMechanicalConnectionBlock> _tmpMechs = new List<IMyMechanicalConnectionBlock>();
      readonly List<IMyShipConnector> _tmpCons = new List<IMyShipConnector>();

      public GridManager(MyGridProgram program, MyIni ini) {
        _block = program.Me;
        Scan(program.GridTerminalSystem);
        Schedule(new ScheduledAction(() => Scan(program.GridTerminalSystem), period: 100));
        _managedGrids = new HashSet<string>(ini.Get(INI_SECTION, "managed-grids").ToString().Split(SPLIT_VALUES_CHAR, StringSplitOptions.RemoveEmptyEntries));
      }

      public void Scan(IMyGridTerminalSystem gts) {
        _metaGrids.Clear();
        gts.GetBlocksOfType(_tmpMechs, mech => mech.TopGrid != null);
        gts.GetBlocksOfType(_tmpCons);

        var connectedGrids = new HashSet<IMyCubeGrid>();
        connectedGrids.UnionWith(
          _tmpCons.Select(c => c.CubeGrid)
            .Concat(_tmpMechs.Select(m => m.CubeGrid))
            .Concat(_tmpMechs.Select(m => m.TopGrid)));

        var grids = new Dictionary<int, IMyCubeGrid>();
        foreach(IMyCubeGrid grid in connectedGrids) {
          bool found = false;
          foreach(var mg in _metaGrids) {
            if(mg.IsSameMetaGrid(grid)) {
              mg.AddGrid(grid);
              found = true;
              break;
            }
          }
          if(!found) {
            _metaGrids.Add(new MetaGrid(grid));
          }
        }
      }

      public bool Manages(IMyCubeGrid grid) {
        var metaGrid = _metaGrids.FirstOrDefault(sg => sg.IsSameMetaGrid(grid));
        return metaGrid != null && _managedGrids.Contains(metaGrid.Name);
      }

      public override string ToString() => string.Join("\n",_metaGrids.Select(mg => mg.ToString()));
    }
  }
}
