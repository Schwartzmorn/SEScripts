using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
partial class Program {
  public enum CmdTrigger {Antenna=0, User=1, Cmd=2};
  public class CmdLine {
    readonly string _name;
    readonly Dictionary<string, Cmd> _cmds = new Dictionary<string, Cmd>();
    readonly Action<string> _echo;
    readonly MyCommandLine _parser = new MyCommandLine();

    public CmdLine(string name, Action<string> echo = null) {
      _echo = echo;
      _name = name;
      AddCmd(new Cmd(
        "help", "displays this help or help on a command", v => _hlp(v), maxArgs: 1,
        detailedHelp: @"If no argument, gives the list of available command
Else, gives the detailed help on the command"));
      _log($"{_name} initialized. Run '-help' for more info");
    }

    public void AddCmd(Cmd cmd) => _cmds.Add(cmd.Name, cmd);

    public void StartCmd(string ln, Action<string> callback, CmdTrigger trig) {
      if(!string.IsNullOrWhiteSpace(ln)) {
        bool f = true;
        if(_parser.TryParse(ln)) {
          if(_parser.Switches.Count > 0) {
            string s = _parser.Switches.ElementAt(0);
            Cmd cmd;
            if(_cmds.TryGetValue(s, out cmd)) {
              _log($"'{s}' command received");
              if(trig < cmd.RequiredTrigger) {
                _log($"permission denied for '{s}'");
                return;
              }
              var args = new List<string>();
              int i = -1;
              string a;
              while((a = _parser.Switch(s, ++i)) != null)
                args.Add(a);
              if(i <= cmd.MaxArgs && i >= cmd.MinArgs) {
                f = false;
                cmd.Action(args, callback, trig);
              } else
                _log($"wrong number of arguments for '{s}'");
            } else
              _log($"unknown command '{s}'");
          }
        } else
          _log($"could not parse '{ln}'");
        if(f)
          _log($"run '-help' for more info");
      }
    }

    public void HandleCmd(string ln, CmdTrigger trig) {
      try {
        StartCmd(ln, s => { if(s != null) _echo($"Done: {s}"); }, trig);
      } catch(Exception e) {
        Log($"Failed cmd: {e.Message}");
      }
    }

    void _hlp(List<string> args) {
      if (args.Count == 0) {
        _echo(_name);
        foreach (var kv in _cmds)
          _echo($"-{kv.Key}: {kv.Value.BriefHelp}");
      } else {
        if (_cmds.ContainsKey(args[0])) {
          var cmd = _cmds[args[0]];
          _echo(cmd.Hlp());
          if(cmd.DetailedHelp.Count > 0)
            cmd.DetailedHelp.ForEach(s => _echo("  " + s));
          else
            _echo("  " + cmd.BriefHelp);
        } else
          _log($"unknown command '{args[0]}'");
      }
    }

    void _log(string log) => _echo?.Invoke($"{_name}: {log}");
  }
}
}
