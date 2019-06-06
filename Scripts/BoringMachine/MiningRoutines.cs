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
  public class MiningRoutines: JobProvider {
    readonly Dictionary<string, int> _miningRoutes = new Dictionary<string, int>();
    readonly Autopilot _ap;
    public MiningRoutines(Ini ini, CmdLine cmd, Autopilot ap) {
      _ap = ap;
      var keys = new List<MyIniKey>();
      ini.GetKeys("mining-routine", keys);
      keys.ForEach(k => _miningRoutes[k.Name] = ini.Get(k).ToInt32());
      cmd.AddCmd(new Cmd("mine-recall", "Goes to the given mining position", (s, c) => _recall(s[0], c), nArgs: 1));
      cmd.AddCmd(new Cmd("mine-save", "Saves the current mining position", s => _savePos(s[0]), nArgs: 1));
      ScheduleOnSave(_save);
    }

    void _savePos(string wpName) {
      int i;
      _miningRoutes.TryGetValue(wpName, out  i);
      string prev = _name(wpName, i++);
      _ap.Network.AddLinkedWP(_name(wpName, i), prev);
      _miningRoutes[wpName] = i;
    }

    void _recall(string wpName, Action<string> s) {
      int i;
      _miningRoutes.TryGetValue(wpName, out i);
      _ap.StartJob(_ap.GoTo, _name(wpName, i), s);
    }

    string _name(string wpName, int i) => i == 0 ? wpName : $"$mine-{wpName}-{i}";

    void _save(MyIni ini) {
      foreach(var kv in _miningRoutes)
        ini.Set("mining-routine", kv.Key, kv.Value);
    }
  }
}
}
