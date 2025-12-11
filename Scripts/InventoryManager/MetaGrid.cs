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
    /// <summary>Class that encapsulate the grids that are mechanically connected with pistons or rotors (not connectors). Keeps track of the largest grid, but otherwise only keeps the ids.</summary>
    public class MetaGrid
    {
      /// <summary>Name of the MetaGrid. Returns the name of the largest grid.</summary>
      public string Name => _grid?.CustomName;

      private IMyCubeGrid _grid;
      private readonly HashSet<long> _grids = new HashSet<long>();
      private int _volume = -1;
      /// <summary>Adds a grid to the metagrid</summary>
      /// <param name="grid">grid to add</param>
      public void AddGrid(IMyCubeGrid grid)
      {
        _grids.Add(grid.EntityId);
        _tryUseAsRef(grid);
      }
      /// <summary>Returns whether the grid belongs to the meta grid</summary>
      /// <param name="grid">grid to test</param>
      /// <returns>Whether <paramref name="grid"/> belongs to the MetaGrid</returns>
      public bool IsSameMetaGrid(IMyCubeGrid grid) => _grids.Contains(grid.EntityId) || grid.IsSameConstructAs(_grid);

      public override string ToString() => $"{Name}: {_grids.Count} grid{(_grids.Count > 1 ? "s" : "")}";

      private void _tryUseAsRef(IMyCubeGrid grid)
      {
        int volume = (grid.Max - grid.Min).Volume();
        if (volume > _volume)
        {
          _grid = grid;
          _volume = volume;
        }
      }
    }
  }
}
