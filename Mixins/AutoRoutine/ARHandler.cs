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
      readonly CmdLine _cmd;
      readonly Dictionary<string, AutoRoutine> _routines = new Dictionary<string, AutoRoutine>();
      AutoRoutine _cur = null;
      int _index;
      bool _waiting = false;
      public ARHandler(CmdLine cmd, string data) {
        _cmd = cmd;
        //parse data
        int i = 0;
        AutoRoutine ar = null;
        foreach(var l in data.Split(SEP).Select(l => l.Trim())) {
          ++i;
          if(l == "" || l.StartsWith(";")) continue;
          if(l.StartsWith("=")) {
            ar = new AutoRoutine(l.Substring(1));
            _routines[l] = ar;
          } else if (ar == null) {
            throw new InvalidOperationException($"Error at line {i}: no routine");
          } else {
            ar.AddJob(l);
          }
        }
        _cmd.AddCmd(new Cmd("ar-start", "Starts an auto routine", StartRoutine, minArgs: 1, maxArgs: 1));
        Scheduler.Inst.AddAction(new ScheduledAction(_handle, name: "ar-handler"));
        // TODO Scheduler.Inst.AddActionOnSave();
      }
      public void StartRoutine(List<string> args) {
        _index = 0;
        _routines.TryGetValue(args[0], out _cur);
        if (_cur == null) {
          Logger.Inst.Log($"No routine named {args[0]}");
        }
      }
      private void _handle() {
        if(_cur != null && !_waiting) {
          if (_index < _cur.Cmds.Count) {
            _waiting = true;
            _cmd.StartCmd(_cur.Cmds[_index], true, _endCmd);
          } else {
            _cur = null;
            _index = 0;
          }
        }
      }
      private void _endCmd(string arg) {
        ++_index;
        _waiting = false;
      }
    }
  }
}
