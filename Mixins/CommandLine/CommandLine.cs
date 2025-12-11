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
    /// <summary>Enum to categorize what triggered a given command, for some limited access control.</summary>
    public enum CommandTrigger { Antenna = 0, User = 1, Cmd = 2 };

    /// <summary>
    /// Class that parses and dispatches textual commands.
    /// <para>To summon a <see cref="Command"/>, the method <see cref="StartCmd(string, CommandTrigger, Action{ProcessResult})"/> must be called with the string $"-{<see cref="Command.Name"/>}" followed by its space separated arguments</para>
    /// </summary>
    public class CommandLine
    {
      readonly Dictionary<string, AbstractCommand> _commands = new Dictionary<string, AbstractCommand>();
      readonly Action<string> _logger;
      readonly string _name;
      readonly MyCommandLine _parser = new MyCommandLine();
      readonly IProcessSpawner _spawner;
      /// <summary>Create a <see cref="CommandLine"/> able to parse and dispatch commands to its registered <see cref="Command"/>.</summary>
      /// <param name="name">Name of the command line.</param>
      /// <param name="logger">Optional logger to log some events.</param>
      /// <param name="spawner">Default spawner used for the processes.</param>
      public CommandLine(string name, Action<string> logger, IProcessSpawner spawner)
      {
        _name = name;
        _logger = logger;
        _spawner = spawner;

        RegisterCommand(new Command("help", Command.Wrap(_help), @"Displays this help or help on a command
If no argument, gives the list of available command
Else, gives the detailed help on the command", nArgs: 0));
        _log($"'{_name}' initialized. Run 'help' for more info");
        if ((spawner as IProcessManager) != null)
        {
          RegisterCommand(new Command("kill", Command.Wrap(_kill), @"Kills processes by pid or by name.
If a number is provided, it will kill the process with the given process id.
Otherwise, it kills all the process with the given name.", nArgs: 1));
          RegisterCommand(new Command("ps", Command.Wrap(_ps), "Lists alive processes", nArgs: 0));
        }
      }
      /// <summary>Registers a <see cref="AbstractCommand"/> so the <see cref="CommandLine"/> recognizes it.</summary>
      /// <param name="command"><see cref="AbstractCommand"/> to register</param>
      public void RegisterCommand(AbstractCommand command) => _commands.Add(command.Name, command);
      /// <summary> Parses the command and spawns the corresponding process.</summary>
      /// <param name="cmd">Command with the command name and its arguments</param>
      /// <param name="trigger">What triggered the command, for some limited access control</param>
      /// <param name="onDone">Callback the process will call on termination</param>
      /// <param name="spawner">The spawner (typically a <see cref="Process"/>) used to spawn the process. If null, <see cref="CommandLine._spawner"/> will be used.</param>
      /// <returns>The spawned <see cref="Process"/>. Can be null if it failed or was executed immediately.</returns>
      public Process StartCmd(string cmd, CommandTrigger trigger, Action<Process> onDone = null, IProcessSpawner spawner = null)
      {
        var parsed = ParseCommand(cmd);
        if (parsed.Item1 == null)
        {
          return null;
        }
        AbstractCommand command;
        if (_commands.TryGetValue(parsed.Item1, out command))
        {
          return command.Spawn(parsed.Item2, _log, onDone, spawner ?? _spawner, trigger);
        }
        else
        {
          _log($"Unknown command {parsed.Item1}");
          return null;
        }
      }

      public MyTuple<string, ArgumentsWrapper> ParseCommand(string cmd)
      {
        if (cmd == null || cmd.Trim() == "")
        {
          return MyTuple.Create<string, ArgumentsWrapper>(null, null);
        }
        if (!_parser.TryParse(cmd))
        {
          _log($"Failed to parse {cmd}");
          return MyTuple.Create<string, ArgumentsWrapper>(null, null);
        }
        var args = new ArgumentsWrapper(_parser);
        var cmdName = args.Next();
        return MyTuple.Create(cmdName, args);
      }

      void _help(ArgumentsWrapper _wrapper, Action<string> log)
      {
        log($"Available commands on {_name}:");
        foreach (KeyValuePair<string, AbstractCommand> kv in _commands.Where(kv => kv.Value.RequiredTrigger != CommandTrigger.Cmd))
        {
          log($"{kv.Key}: {kv.Value.BriefHelp}");
        }
      }

      void _kill(ArgumentsWrapper args, Action<string> log)
      {
        int pid;
        if (int.TryParse(args.Peek(), out pid) && pid > 0)
        {
          (_spawner as IProcessManager).Kill(pid);
        }
        else
        {
          (_spawner as IProcessManager).KillAll(args.Peek());
        }
      }

      void _ps(ArgumentsWrapper _wrapper, Action<string> log) => (_spawner as IProcessManager).Log(log);

      void _log(string s) => _logger?.Invoke(s);
    }
  }
}
