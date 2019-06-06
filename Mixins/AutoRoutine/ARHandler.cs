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
  static readonly char[] SEP = new char[] { '\n' };
  public class ARHandler {
    public const string ABORT = "$abort";
    readonly CmdLine _cmd;
    readonly Dictionary<string, AutoRoutine> _routines = new Dictionary<string, AutoRoutine>();
    readonly IMyTerminalBlock _bl;
    readonly List<AutoRoutine> _running = new List<AutoRoutine>();
    string _prev;
    public ARHandler(Ini ini, CmdLine cmd, IMyTerminalBlock block) {
      _bl = block;
      _cmd = cmd;
      _cmd.AddCmd(new Cmd("ar-start", "Starts an auto routine", StartRoutine, minArgs: 1));
      _read();
      Schedule(new ScheduledAction(_read, 100, name: "ar-reader"));
      ScheduleOnSave(_save);
    }
    public void StartRoutine(List<string> args, Action<string> cb, CmdTrigger t) {
      AutoRoutine r;
      if (_routines.TryGetValue(args[0], out r)) {
        r.Exec(_cmd, s => _endRoutine(s, cb, r), args);
      }
    }
    void _read() {
      var stack = new List<Routine>();
      if (!ReferenceEquals(_prev, _bl.CustomData)) {
        _prev = _bl.CustomData;
        _routines.Clear();
        int i = 0;
        AutoRoutine ar = null;
        foreach(string l in _prev.Split(SEP).Select(l => l.Trim())) {
          ++i;
          if(l == "" || l.StartsWith(";")) continue;
          if(l.StartsWith("=")) {
            stack.Clear();
            ar = new AutoRoutine(l.Substring(1).Trim());
            _routines[ar.Name] = ar;
            stack.Add(ar);
          } else if(stack.Count == 0) _throw("expected routine", i);
          else if(l.StartsWith("while")) {
            var routine = new Routine();
            var job = new RepeatJob(_parseJob(l.Substring(6).Trim(), i), routine);
            stack.Last().AddJob(job);
            stack.Add(routine);
          } else if(l.StartsWith("end"))
            stack.Pop();
          else
            stack.Last().AddJob(_parseJob(l, i));
        }
        Log($"Found {_routines.Count} routines");
      }
    }
    Job _parseJob(string s, int i) {
      if (s == "always")
        return new AlwaysJob();
      else if (s.StartsWith("wait")) {
        int w;
        int.TryParse(s.Substring(5), out w);
        if (w < 1) _throw("invalid duration", i);
        return new WaitJob(w);
      } else if (!s.StartsWith("-")) _throw("expected command", i);
      return new CmdJob(s);
    }
    void _throw(string s, int i) {
      throw new InvalidOperationException($"Error at line {i}: {s}");
    }
    void _endRoutine(string s, Action<string> cb, AutoRoutine r) {
      r.Cancel();
      Log($"Routine {r.Name} finished with msg '{s}'");
      cb(s);
    }
    void _save(MyIni ini) {
      // TODO
    }
  }
}
}
