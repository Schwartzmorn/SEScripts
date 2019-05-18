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
    public class GridManager {
      public GridManager(IMyProgrammableBlock block, IMyGridTerminalSystem gts) {
        _block = block;
        Scan(gts);
      }
      public void Scan(IMyGridTerminalSystem gts) {
        _superGrids.Clear();
        _grids.Clear();
        List<IMyMechanicalConnectionBlock> mechs = new List<IMyMechanicalConnectionBlock>();
        List<IMyShipConnector> invs = new List<IMyShipConnector>();
        HashSet<IMyCubeGrid> connectedGrids;
        Dictionary<long, HashSet<long>> sgs = new Dictionary<long, HashSet<long>>();
        gts.GetBlocksOfType(mechs, mech => mech.TopGrid != null);
        gts.GetBlocksOfType(invs);

        connectedGrids = new HashSet<IMyCubeGrid>(invs.ConvertAll(inv => inv.CubeGrid));
        connectedGrids.UnionWith(mechs.ConvertAll(mech => mech.CubeGrid));
        connectedGrids.UnionWith(mechs.ConvertAll(mech => mech.TopGrid));

        foreach (IMyCubeGrid grid in connectedGrids) {
          sgs[grid.EntityId] = new HashSet<long> { grid.EntityId };
          _grids[grid.EntityId] = grid;
        }
        foreach (var mech in mechs) {
          HashSet<long> bot = sgs[mech.CubeGrid.EntityId];
          HashSet<long> top = sgs[mech.TopGrid.EntityId];
          if (top != bot) {
            bot.UnionWith(top);
            foreach (var grid in bot) {
              sgs[grid] = bot;
            }
          }
        }
        foreach (HashSet<long> sg in sgs.Values.Distinct()) {
          _superGrids.Add(sg);
        }
      }
      public bool Manages(IMyCubeGrid grid) => _superGrids.Any(sg => sg.Contains(grid.EntityId));
      IMyProgrammableBlock _block;
      List<HashSet<long>> _superGrids = new List<HashSet<long>>();
      Dictionary<long, IMyCubeGrid> _grids = new Dictionary<long, IMyCubeGrid>();
    }
  }
}
