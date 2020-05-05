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
      
      readonly Action<string> logger;

      public GridManager(MyGridProgram program, MyIni ini, IProcessSpawner spawner, Action<string> logger) {
        this.logger = logger;
        this._block = program.Me;
        this.Scan(program.GridTerminalSystem);
        spawner.Spawn(p => this.Scan(program.GridTerminalSystem), "grid-scanner", period: 100);
        this._managedGrids = new HashSet<string>(ini.Get(INI_SECTION, "managed-grids").ToString().Split(SPLIT_VALUES_CHAR, StringSplitOptions.RemoveEmptyEntries));
        this.log($"We manage {this._managedGrids.Count} grids");
      }

      public void Scan(IMyGridTerminalSystem gts) {
        this.log("Scanning...");
        this._metaGrids.Clear();
        gts.GetBlocksOfType(this._tmpMechs, mech => mech.TopGrid != null);
        gts.GetBlocksOfType(this._tmpCons);

        var connectedGrids = new HashSet<IMyCubeGrid>();
        connectedGrids.Add(this._block.CubeGrid);
        connectedGrids.UnionWith(
          this._tmpCons.Select(c => c.CubeGrid)
            .Concat(this._tmpMechs.Select(m => m.CubeGrid))
            .Concat(this._tmpMechs.Select(m => m.TopGrid).Where(g => g != null)));

        var grids = new Dictionary<int, IMyCubeGrid>();
        foreach(IMyCubeGrid grid in connectedGrids) {
          bool found = false;
          foreach(MetaGrid mg in this._metaGrids) {
            if(mg.IsSameMetaGrid(grid)) {
              mg.AddGrid(grid);
              found = true;
              break;
            }
          }
          if(!found) {
            this._metaGrids.Add(new MetaGrid(grid));
          }
        }
        this.log($"We found {this._metaGrids.Count} grids");
      }

      public bool Manages(IMyCubeGrid grid) {
        var metaGrid = this._metaGrids.FirstOrDefault(sg => sg.IsSameMetaGrid(grid));
        bool res = metaGrid != null && this._managedGrids.Contains(metaGrid.Name);
        return res;
      }

      public override string ToString() => string.Join("\n", this._metaGrids.Select(mg => mg.ToString()));

      void log(string s) => this.logger?.Invoke("GM: " + s);
    }
  }
}
