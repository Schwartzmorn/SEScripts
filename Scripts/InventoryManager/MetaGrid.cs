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
    public class MetaGrid {
      public string Name { get; private set; }

      private IMyCubeGrid _grid;
      private readonly HashSet<long> _grids = new HashSet<long>();
      private int _volume = 0;

      public MetaGrid(IMyCubeGrid grid) {
        AddGrid(grid);
      }

      public void AddGrid(IMyCubeGrid grid) {
        _grids.Add(grid.EntityId);
        _tryUseAsRef(grid);
      }

      public bool IsSameMetaGrid(IMyCubeGrid grid) => _grids.Contains(grid.EntityId) || grid.IsSameConstructAs(_grid);

      public override string ToString() => $"{Name}: {_grids.Count} grid{(_grids.Count > 1 ? "s" : "")}";

      private void _tryUseAsRef(IMyCubeGrid grid) {
        int volume = (grid.Max - grid.Min).Volume();
        if (volume > _volume) {
          _grid = grid;
          Name = grid.CustomName;
          _volume = volume;
        }
      }
    }
  }
}
