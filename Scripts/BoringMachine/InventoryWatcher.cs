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

namespace IngameScript {
partial class Program {
  public class InventoryWatcher {
    readonly List<IMyCargoContainer> invs = new List<IMyCargoContainer>();

    public float LoadFactor => (float)this.invs.Sum(i => i.GetInventory().CurrentVolume.ToIntSafe()) / this.invs.Sum(i => i.GetInventory().MaxVolume.ToIntSafe());

    public InventoryWatcher(CommandLine cmd, IMyGridTerminalSystem gts, IMyCockpit cpit) {
      gts.GetBlocksOfType(this.invs, c => c.CubeGrid == cpit.CubeGrid);
      cmd.RegisterCommand(new Command("inv-while", Command.Wrap(this.startWait), "Wait for cargo inventory to reach a certain point", nArgs: 2));
    }

    void startWait(Process p, List<string> args) {
      bool over = args[0] == "over";
      float loadFactor = float.Parse(args[1]);
      p.Spawn(pc => this.checkInv(pc, over, loadFactor), "inv-check", period: 10);
    }

    void checkInv(Process pc, bool over, float loadFactor) {
      if(over && this.LoadFactor < loadFactor) {
        pc.Done();
      } else if (!over && this.LoadFactor > loadFactor) {
        pc.Done();
      }
    }
  }
}
}
