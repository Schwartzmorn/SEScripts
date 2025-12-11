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
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
  partial class Program
  {
    public class InventoryWatcher
    {
      readonly List<IMyCargoContainer> _invs = new List<IMyCargoContainer>();

      public float LoadFactor => (float)_invs.Sum(i => i.GetInventory().CurrentVolume.ToIntSafe()) / _invs.Sum(i => i.GetInventory().MaxVolume.ToIntSafe());

      public InventoryWatcher(CommandLine cmd, IMyGridTerminalSystem gts, IMyCockpit cpit)
      {
        gts.GetBlocksOfType(_invs, c => c.CubeGrid == cpit.CubeGrid);
        cmd.RegisterCommand(new Command("inv-while", Command.Wrap(_startWait), "Wait for cargo inventory to reach a certain point", nArgs: 2));
      }

      void _startWait(Process p, ArgumentsWrapper args)
      {
        bool over = args[0] == "over";
        float thresholdLoadFactor = float.Parse(args[1]);
        p.Spawn(pc => _checkInv(pc, over, thresholdLoadFactor), "inv-check", period: 10);
      }

      void _checkInv(Process pc, bool over, float thresholdLoadFactor)
      {
        var currentLoadFactor = LoadFactor;
        if (over && currentLoadFactor < thresholdLoadFactor)
        {
          pc.Done();
        }
        else if (!over && currentLoadFactor >= thresholdLoadFactor)
        {
          pc.Done();
        }
      }
    }
  }
}
