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
    public class GridManager
    {
      static readonly string INI_SECTION = "grid-manager";

      readonly IMyProgrammableBlock _block;
      readonly List<MetaGrid> _metaGrids = new List<MetaGrid>();
      readonly HashSet<string> _excludedGrids;

      readonly List<IMyMechanicalConnectionBlock> _tmpMechs = new List<IMyMechanicalConnectionBlock>();
      readonly List<IMyShipConnector> _tmpCons = new List<IMyShipConnector>();

      readonly Action<string> _logger;

      public GridManager(MyGridProgram program, MyIni ini, IProcessSpawner spawner, Action<string> logger)
      {
        _logger = logger;
        _block = program.Me;
        Scan(program.GridTerminalSystem);
        spawner.Spawn(p => Scan(program.GridTerminalSystem), "grid-scanner", period: 100);
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
        gts.GetBlocksOfType(_tmpMechs, mech => mech.TopGrid != null);
        gts.GetBlocksOfType(_tmpCons);

        var connectedGrids = new HashSet<IMyCubeGrid>
        {
          _block.CubeGrid
        };
        connectedGrids.UnionWith(
          _tmpCons.Select(c => c.CubeGrid)
            .Concat(_tmpMechs.Select(m => m.CubeGrid))
            .Concat(_tmpMechs.Select(m => m.TopGrid).Where(g => g != null)));

        var grids = new Dictionary<int, IMyCubeGrid>();
        foreach (IMyCubeGrid grid in connectedGrids)
        {
          bool found = false;
          foreach (MetaGrid mg in _metaGrids)
          {
            if (mg.IsSameMetaGrid(grid))
            {
              mg.AddGrid(grid);
              found = true;
              break;
            }
          }
          if (!found)
          {
            _metaGrids.Add(new MetaGrid(grid));
          }
        }
        if (previousCount != _metaGrids.Count)
        {
          _log($"We found {_metaGrids.Count} grids");
        }
      }

      public bool Manages(IMyCubeGrid grid)
      {
        var metaGrid = _metaGrids.FirstOrDefault(sg => sg.IsSameMetaGrid(grid));
        bool res = metaGrid == null || !_excludedGrids.Contains(metaGrid.Name);
        return res;
      }

      public override string ToString() => string.Join("\n", _metaGrids.Select(mg => mg.ToString()));

      void _log(string s) => _logger?.Invoke("GM: " + s);
    }
  }
}
