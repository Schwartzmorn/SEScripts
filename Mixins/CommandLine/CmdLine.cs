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
    public class CmdLine {
      readonly string _name;
      readonly Dictionary<string, Cmd> _cmds = new Dictionary<string, Cmd>();
      readonly Action<string> _echo;
      readonly MyCommandLine _parser = new MyCommandLine();

      public CmdLine(string name, Action<string> echo = null) {
        _echo = echo;
        _name = name;
        AddCmd(new Cmd(
          "help", "displays this help or help on a command", v => _hlp(_echo, v), maxArgs: 1,
          detailedHelp: @"If no argument, gives the list of available command
Else, gives the detailed help on the command"));
        _log($"initialized. Run '-help' for more info");
      }

      public void AddCmd(Cmd command) => _cmds.Add(command.Name, command);


      public void StartCmd(string ln, bool hasPermission, Action<string> callback) {
        bool success = true;
        if(!string.IsNullOrWhiteSpace(ln)) {
          if(_parser.TryParse(ln)) {
            foreach(var s in _parser.Switches) {
              Cmd cmd;
              _cmds.TryGetValue(s, out cmd);
              if(cmd != null) {
                _log($"'{s}' command received");
                if(!hasPermission && cmd.RequirePermission) {
                  _log($"permission denied for '{s}'");
                  return;
                }
                var args = new List<string>();
                int i = 0;
                while(_parser.Switch(s, i) != null) {
                  args.Add(_parser.Switch(s, i));
                  ++i;
                }
                if(args.Count <= cmd.MaxArgs && args.Count >= cmd.MinArgs) {
                  _cmds[s].Action(args, callback);
                } else {
                  success = false;
                  _log($"wrong number of arguments for '{s}'");
                }
              } else {
                success = false;
                _log($"unknown command '{s}'");
              }
            }
          } else {
            success = false;
            _log($"could not parse '{ln}'");
          }
        }
        if(!success) {
          _log($"run '-help' for more info");
        }
      }


      public void HandleCmd(string ln, bool hasPermission) => StartCmd(ln, hasPermission, _ => { });

      private void _hlp(Action<string> echo, List<string> args) {
        if (args.Count == 0) {
          echo(_name);
          foreach (var kv in _cmds) {
            echo($"-{kv.Key}: {kv.Value.BriefHelp}");
          }
        } else {
          if (_cmds.ContainsKey(args[0])) {
            var cmd = _cmds[args[0]];
            echo(_getHlp(cmd));
            if (cmd.DetailedHelp.Count > 0) {
              foreach (var s in cmd.DetailedHelp) {
                echo("  " + s);
              }
            } else {
              echo("  " + cmd.BriefHelp);
            }
          } else {
            _log($"unknown command '{args[0]}'");
          }
        }
      }

      private void _log(string log) => _echo?.Invoke($"Command: {log}");

      private static string _getHlp(Cmd cmd) {
        string line = $"-{cmd.Name}";
        int maxArgs = cmd.MaxArgs;
        int minArgs = cmd.MinArgs;
        if (maxArgs == 0) {
          line += " (no argument)";
        } else {
          line += " takes ";
          if (minArgs == maxArgs) {
            line += $"{minArgs}";
          } else if (minArgs == 0) {
            if (maxArgs < int.MaxValue) {
              line += $"up to {maxArgs}";
            } else {
              line += "any number of";
            }
          } else if (maxArgs < int.MaxValue) {
            line += $"{minArgs}-{maxArgs}";
          } else {
            line += $"at least {minArgs}";
          }
          line += " argument";
          if (maxArgs > 1) {
            line += 's';
          }
        }
        return line;
      }
    }
  }
}
