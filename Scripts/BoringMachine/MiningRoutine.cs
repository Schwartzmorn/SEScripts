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
    public class MiningRoutines
    {
      readonly Dictionary<string, int> _miningRoutes = new Dictionary<string, int>();
      readonly Autopilot _ap;
      public MiningRoutines(MyIni ini, CommandLine cmd, Autopilot ap, ISaveManager manager)
      {
        _ap = ap;
        var keys = new List<MyIniKey>();
        ini.GetKeys("mining-routine", keys);
        keys.ForEach(k => _miningRoutes[k.Name] = ini.Get(k).ToInt32());
        cmd.RegisterCommand(new Command("mine-recall", Command.Wrap(_recall), "Goes to the given mining position", nArgs: 1));
        cmd.RegisterCommand(new Command("mine-save", Command.Wrap(_savePos), "Saves the current mining position", nArgs: 1));
        manager.AddOnSave(_save);
      }

      void _savePos(string wpName)
      {
        int i;
        _miningRoutes.TryGetValue(wpName, out i);
        string prev = _name(wpName, i++);
        _ap.Network.AddLinkedWP(_name(wpName, i), prev);
        _miningRoutes[wpName] = i;
      }

      void _recall(string wpName)
      {
        int i;
        _miningRoutes.TryGetValue(wpName, out i);
        _ap.GoTo(_name(wpName, i));
      }

      string _name(string wpName, int i) => i == 0 ? wpName : $"$mine-{wpName}-{i}";

      void _save(MyIni ini)
      {
        foreach (KeyValuePair<string, int> kv in _miningRoutes)
          ini.Set("mining-routine", kv.Key, kv.Value);
      }
    }
  }
}
