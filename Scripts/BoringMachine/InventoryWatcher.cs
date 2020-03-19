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
  public class InventoryWatcher: JobProvider {
    readonly IMyCockpit _cpit;
    readonly List<IMyCargoContainer> _invs = new List<IMyCargoContainer>();
    ScheduledAction _wait;

    public float LoadFactor => (float)this._invs.Sum(i => i.GetInventory().CurrentVolume.ToIntSafe()) / this._invs.Sum(i => i.GetInventory().MaxVolume.ToIntSafe());

    public InventoryWatcher(CmdLine cmd, IMyGridTerminalSystem gts, IMyCockpit cpit) {
        this._cpit = cpit;
      gts.GetBlocksOfType(this._invs, c => c.CubeGrid == cpit.CubeGrid);
      cmd.AddCmd(new Cmd("inv-while", "Wait for cargo inventory to reach a certain point", (s, c) => StartJob(_startWait, s, c), nArgs: 2));
    }

    void _startWait(List<string> args) {
      bool over = args[0] == "over";
      float loadFactor = float.Parse(args[1]);
        this._wait = new ScheduledAction(() => this._checkInv(over, loadFactor), 10, name: "inv-check");
      Schedule(this._wait);
    }

    void _checkInv(bool over, float loadFactor) {
      if(over && this.LoadFactor < loadFactor)
          this.StopCallback($"Load factor under {loadFactor}");
      else if(!over && this.LoadFactor > loadFactor)
          this.StopCallback($"Load factor over {loadFactor}");
    }

    protected override void StopCallback(string s) {
      if(this._wait != null) {
          this._wait.Dispose();
          this._wait = null;
      }
      base.StopCallback(s);
    }
  }
}
}
