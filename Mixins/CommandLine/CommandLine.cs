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
    /// <summary>Enum to categorize what triggered a given command, for some limited access control.</summary>
    public enum CommandTrigger { Antenna = 0, User = 1, Cmd = 2 };

    /// <summary>
    /// Class that parses and dispatches textual commands.
    /// <para>To summon a <see cref="Command"/>, the method <see cref="StartCmd(string, CommandTrigger, Action{ProcessResult})"/> must be called with the string $"-{<see cref="Command.Name"/>}" followed by its space separated arguments</para>
    /// </summary>
    public class CommandLine {
      readonly Dictionary<string, Command> _commands = new Dictionary<string, Command>();
      readonly Action<string> _logger;
      readonly string _name;
      readonly MyCommandLine _parser = new MyCommandLine();
      readonly IProcessSpawner _spawner;
      /// <summary>Create a <see cref="CommandLine"/> able to parse and dispatch commands to its registered <see cref="Command"/>.</summary>
      /// <param name="name">Name of the command line.</param>
      /// <param name="logger">Optional logger to log some events.</param>
      /// <param name="spawner">Default spawner used for the processes.</param>
      public CommandLine(string name, Action<string> logger, IProcessSpawner spawner) {
        _name = name;
        _logger = logger;
        _spawner = spawner;

        RegisterCommand(new Command("help", Command.Wrap(_help), "Displays this help or help on a command", detailedHelp: @"If no argument, gives the list of available command
Else, gives the detailed help on the command", maxArgs: 1));
        _log($"'{_name}' initialized. Run '-help' for more info");
        if ((spawner as IProcessManager) != null) {
          RegisterCommand(new Command("kill", Command.Wrap(_kill), "Kills processes by pid or by name", nArgs: 1, detailedHelp: @"If a number is provided, it will kill the process with the given process id.
Otherwise, it kills all the process with the given name."));
          RegisterCommand(new Command("ps", Command.Wrap(_ps), "Lists alive processes", nArgs: 0));
        }
      }
      /// <summary>Registers a <see cref="Command"/> so the <see cref="CommandLine"/> recognizes it.</summary>
      /// <param name="command"><see cref="Command"/> to register</param>
      public void RegisterCommand(Command command) => _commands.Add(command.Name, command);
      /// <summary> Parses the command and spawns the corresponding process.</summary>
      /// <param name="cmd">Command with the command name and its arguments</param>
      /// <param name="trigger">What triggered the command, for some limited access control</param>
      /// <param name="onDone">Callback the process will call on termination</param>
      /// <param name="spawner">The spawner (typically a <see cref="Process"/>) used to spawn the process. If null, <see cref="CommandLine._spawner"/> will be used.</param>
      /// <returns>The spawned <see cref="Process"/>. Can be null if it failed or was executed immediately.</returns>
      public Process StartCmd(string cmd, CommandTrigger trigger, Action<Process> onDone = null, IProcessSpawner spawner = null) {
        MyTuple<string, List<string>> parsed = ParseCommand(cmd);
        if (parsed.Item1 != "") {
          Command command;
          if (_commands.TryGetValue(parsed.Item1, out command)) {
            return command.Spawn(parsed.Item2, _log, onDone, spawner ?? _spawner, trigger);
          } else {
            _log($"Unknown command {parsed.Item1}");
          }
        }
        return null;
      }
      /// <summary>Parses the command</summary>
      /// <param name="cmd">Command to parse</param>
      /// <returns>Command name and arguments as a <see cref="List{string}"/></returns>
      public MyTuple<string, List<string>> ParseCommand(string cmd) {
        string cmdName = "";
        var args = new List<string>();
        if (string.IsNullOrWhiteSpace(cmd)) {
        } else if (!_parser.TryParse(cmd) || _parser.Switches.Count != 1) {
          _log($"Failed to parse {cmd}");
        } else {
          cmdName = _parser.Switches.ElementAt(0);
          int i = -1;
          string a;
          while ((a = _parser.Switch(cmdName, ++i)) != null) {
            args.Add(a);
          }
        }
        return MyTuple.Create(cmdName, args);
      }

      void _help(List<string> args, Action<string> log) {
        if (args.Count > 0) {
          Command command;
          if (_commands.TryGetValue(args[0], out command)) {
            command.DetailedHelp(log);
          } else {
            log($"Unknown command: {args[0]}");
          }
        } else {
          log($"Commands available on {_name}:");
          foreach (KeyValuePair<string, Command> kv in _commands.Where(kv => kv.Value.RequiredTrigger != CommandTrigger.Cmd)) {
            log($"-{kv.Key}: {kv.Value.BriefHelp}");
          }
        }
      }

      void _kill(List<string> args, Action<string> log) {
        int pid;
        if (int.TryParse(args[0], out pid) && pid > 0) {
          (_spawner as IProcessManager).Kill(pid);
        } else {
          (_spawner as IProcessManager).KillAll(args[0]);
        }
      }

      void _ps(List<string> args, Action<string> log) => (_spawner as IProcessManager).Log(log);

      void _log(string s) => _logger?.Invoke(s);
    }
  }
}
