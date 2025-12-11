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
using VRageRender;

namespace IngameScript
{
  partial class Program
  {
    public class GridManager
    {
      static readonly string INI_SECTION = "grid-manager";

      readonly IMyProgrammableBlock _block;
      readonly List<MetaGrid> _metaGrids = new List<MetaGrid>();
      readonly Dictionary<IMyCubeGrid, MetaGrid> _grids = new Dictionary<IMyCubeGrid, MetaGrid>();
      readonly HashSet<string> _excludedGrids;

      readonly List<IMyMechanicalConnectionBlock> _tmpMechs = new List<IMyMechanicalConnectionBlock>();
      readonly List<IMyShipConnector> _tmpCons = new List<IMyShipConnector>();
      readonly HashSet<IMyCubeGrid> _tmpGrids = new HashSet<IMyCubeGrid>();

      readonly Action<string> _logger;

      public GridManager(MyGridProgram program, MyIni ini, IProcessSpawner spawner, Action<string> logger)
      {
        _logger = logger;
        _block = program.Me;
        Scan(program.GridTerminalSystem);
        spawner.Spawn(p => Scan(program.GridTerminalSystem), "grid-scanner", period: 50);
        _excludedGrids = new HashSet<string>(ini.Get(INI_SECTION, "excluded-grids").ToString().Split(SPLIT_VALUES_CHAR, StringSplitOptions.RemoveEmptyEntries));
        if (_excludedGrids.Count > 0)
        {
          _log($"We exclude {_excludedGrids.Count} grid(s)");
        }
      }

      public void Scan(IMyGridTerminalSystem gts)
      {
        var previousCount = _metaGrids.Count;
        _metaGrids.Clear();
        _tmpGrids.Clear();
        _grids.Clear();
        gts.GetBlocksOfType(_tmpMechs, mech => mech.TopGrid != null);
        gts.GetBlocksOfType(_tmpCons);


        _tmpGrids.Add(_block.CubeGrid);
        _tmpGrids.UnionWith(
          _tmpCons.Select(c => c.CubeGrid)
            .Concat(_tmpMechs.Select(m => m.CubeGrid))
            .Concat(_tmpMechs.Select(m => m.TopGrid).Where(g => g != null)));

        foreach (IMyCubeGrid grid in _tmpGrids)
        {
          var mg = _metaGrids.Find(m => m.IsSameMetaGrid(grid));
          if (mg == null)
          {
            mg = new MetaGrid();
            _metaGrids.Add(mg);
          }
          mg.AddGrid(grid);
          _grids.Add(grid, mg);
        }
        if (previousCount != _metaGrids.Count)
        {
          _log($"We found {_metaGrids.Count} grids");
        }
      }

      public bool Manages(IMyCubeGrid grid)
      {
        MetaGrid mg;
        _grids.TryGetValue(grid, out mg);
        return mg != null && !_excludedGrids.Contains(mg.Name);
      }

      public override string ToString() => string.Join("\n", _metaGrids.Select(mg => mg.ToString()));

      void _log(string s) => _logger?.Invoke("GM: " + s);
    }
  }
}
