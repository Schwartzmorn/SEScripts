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
    public delegate Action<Process> ActionProvider(ArgumentsWrapper args, Action<string> logger);

    public class ArgumentsWrapper : IEnumerable<string>
    {
      /// <summary>
      /// The complete string of the command being called
      /// </summary>
      public readonly string Command;
      private readonly List<string> _arguments;
      private readonly HashSet<string> _switches;
      private int _position = 0;

      public ArgumentsWrapper(List<string> arguments, IEnumerable<string> switches = null)
      {
        _arguments = arguments;
        if (switches != null)
        {
          _switches = new HashSet<string>(switches);
        }
      }

      public ArgumentsWrapper(string cmd, MyCommandLine myCommandLine)
      {
        Command = cmd;
        _arguments = new List<string>();
        // we copy data to avoid 
        for (var i = 0; i < myCommandLine.ArgumentCount; ++i)
        {
          _arguments.Add(myCommandLine.Argument(i));
        }
        if (myCommandLine.Switches.Count != 0)
        {
          _switches = new HashSet<string>(myCommandLine.Switches);
        }
      }

      /// <summary>
      /// The number of remaining non consumed arguments
      /// </summary>
      public int RemaingCount
      {
        get
        {
          return _arguments.Count - _position;
        }
      }

      /// <summary>
      /// The argument at the given index, where 0 points to the first non consumed argument.
      /// Therefore -1 points to the last consummed argument (if any)
      /// </summary>
      /// <param name="i">the index</param>
      /// <returns>the argument, or null if out of bounds</returns>
      public string this[int i]
      {
        get
        {
          var index = _position + i;
          if (index < 0 || index >= _arguments.Count)
          {

            return null;
          }
          return _arguments[index];
        }
      }

      /// <summary>
      /// Consumes and returns the next argument.
      /// </summary>
      /// <returns></returns>
      public string Next()
      {
        var result = this[0];
        ++_position;
        return result;
      }

      /// <summary>
      /// returns the the first non consumed argument
      /// </summary>
      /// <returns></returns>
      public string Peek()
      {
        return this[0];
      }

      /// <summary>
      /// Returns true if the argument -<switch name> was present
      /// </summary>
      /// <param name="s">name of the switch</param>
      /// <returns></returns>
      public bool HasSwitch(string s)
      {
        return _switches?.Contains(s) ?? false;
      }

      public IEnumerator<string> GetEnumerator()
      {
        int i = _position;
        while (i < _arguments.Count)
        {
          yield return _arguments[i++];
        }
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
        return GetEnumerator();
      }
    }

    public abstract class AbstractCommand
    {
      public readonly string Name;
      public AbstractCommand Parent;
      public readonly CommandTrigger RequiredTrigger;
      public string BriefHelp => _help[0];

      private string _fullName;
      protected readonly string[] _help;

      public string FullName
      {
        get
        {
          if (_fullName == null)
          {
            var chain = new List<string>();
            var cur = this;
            while (cur != null)
            {
              chain.Add(cur.Name);
              cur = cur.Parent;
            }
            chain.Reverse();
            _fullName = string.Join(" ", chain);
          }
          return _fullName;
        }
      }

      public AbstractCommand(string name, CommandTrigger requiredTrigger, string help)
      {
        Name = name;
        RequiredTrigger = requiredTrigger;
        _help = help.Split('\n');
      }

      /// <summary>Spawns a new <see cref="Process"/>, taking into account the arguments. Can fail and return null if the arguments are incorrect.</summary>
      /// <param name="wrapper">The arguments given to the command line.</param>
      /// <param name="logger">The logger the process can use to log.</param>
      /// <param name="onDone">The callback to call on process termination.</param>
      /// <param name="spawner">Used to spawn the process</param>
      /// <param name="trigger">What triggered the command</param>
      /// <returns>The spawned process, if successful.</returns>
      public Process Spawn(ArgumentsWrapper wrapper, Action<string> logger, Action<Process> onDone, IProcessSpawner spawner, CommandTrigger trigger)
      {
        if (trigger < RequiredTrigger)
        {
          logger?.Invoke($"Permission denied for \"{FullName}\"");
          return null;
        }
        return _spawnImpl(wrapper, logger, onDone, spawner, trigger);
      }

      protected void _printHelpOrError(ArgumentsWrapper wrapper, Action<string> logger, string arg)
      {
        if (wrapper.HasSwitch("h"))
        {
          if (logger != null)
          {
            logger.Invoke($"{FullName}: {BriefHelp}");
            foreach (var h in _help.Skip(1))
            {
              logger.Invoke(h);
            }
            _printHelp(logger);
          }
        }
        else
        {
          logger?.Invoke($"{arg} for \"{FullName}\". Use the -h option for more info.");
        }
      }

      protected abstract Process _spawnImpl(ArgumentsWrapper wrapper, Action<string> logger, Action<Process> onDone, IProcessSpawner spawner, CommandTrigger trigger);

      protected abstract void _printHelp(Action<string> logger);
    }

    /// <summary>A command that delegates to one of its subcommands</summary>
    public class ParentCommand : AbstractCommand
    {

      private readonly Dictionary<string, AbstractCommand> _subCommands = new Dictionary<string, AbstractCommand>();

      public ParentCommand(string name, string help, CommandTrigger requiredTrigger = CommandTrigger.User) : base(name, requiredTrigger, help)
      {
      }

      public ParentCommand AddSubCommand(AbstractCommand subCommand)
      {
        _subCommands.Add(subCommand.Name, subCommand);
        subCommand.Parent = this; // TODO hide this shit
        return this;
      }

      protected override void _printHelp(Action<string> logger)
      {
        logger.Invoke($"  Available subcommands are:");
        logger.Invoke($"  {string.Join(", ", _subCommands.Keys)}");
      }

      protected override Process _spawnImpl(ArgumentsWrapper wrapper, Action<string> logger, Action<Process> onDone, IProcessSpawner spawner, CommandTrigger trigger)
      {
        var arg = wrapper.Next();
        if (arg == null)
        {
          _printHelpOrError(wrapper, logger, "Expected subcommand");
          return null;
        }
        AbstractCommand subCommand;
        if (!_subCommands.TryGetValue(arg, out subCommand))
        {
          _printHelpOrError(wrapper, logger, $"Invalid subcommand \"{arg}\"");
          return null;
        }
        return subCommand.Spawn(wrapper, logger, onDone, spawner, trigger);
      }
    }

    /// <summary>Standard command</summary>
    public class Command : AbstractCommand
    {
      private readonly ActionProvider _provider;
      private readonly int _minArgs;
      private readonly int _maxArgs;

      public Command(string name, ActionProvider actionProvider, string help, int maxArgs = int.MaxValue, int minArgs = 0, int nArgs = -1, CommandTrigger requiredTrigger = CommandTrigger.User) : base(name, requiredTrigger, help)
      {
        _provider = actionProvider;
        if (nArgs != -1)
        {
          _minArgs = nArgs;
          _maxArgs = nArgs;
        }
        else
        {
          _minArgs = minArgs;
          _maxArgs = maxArgs;
        }
      }

      /// <summary>Method to wrap an <see cref="Action"/> as a command</summary>
      public static ActionProvider Wrap(Action action) => (args, logger) => (_) => action();
      /// <summary>Method to wrap an <see cref="Action{Process}"/> as a command</summary>
      public static ActionProvider Wrap(Action<Process> action) => (args, logger) => p => action(p);
      /// <summary>Method to wrap an <see cref="Action{string}"/> as a command</summary>
      public static ActionProvider Wrap(Action<string> action) => (args, logger) => (_) => action(args.Peek());
      /// <summary>Method to wrap an <see cref="Action{Process, string}"/> as a command</summary>
      public static ActionProvider Wrap(Action<Process, string> action) => (args, logger) => p => action(p, args.Peek());
      /// <summary>Method to wrap an <see cref="ArgumentsWrapper"/> as a command</summary>
      public static ActionProvider Wrap(Action<ArgumentsWrapper> action) => (args, logger) => _ => action(args);
      /// <summary>Method to wrap an <see cref="Action{Process, ArgumentsWrapper}"/> as a command</summary>
      public static ActionProvider Wrap(Action<Process, ArgumentsWrapper> action) => (args, logger) => p => action(p, args);
      /// <summary>Method to wrap an <see cref="Action{ArgumentsWrapper, Action{string}}"/> as a command</summary>
      public static ActionProvider Wrap(Action<ArgumentsWrapper, Action<string>> action) => (args, logger) => _ => action(args, logger);

      protected override void _printHelp(Action<string> logger)
      {
        var sb = new StringBuilder($"  Takes ");
        if (_maxArgs == 0)
        {
          sb.Append("no arguments");
        }
        else
        {
          if (_minArgs == _maxArgs)
          {
            sb.Append(_minArgs);
          }
          else if (_minArgs == 0)
          {
            sb.Append($"{(_maxArgs < int.MaxValue ? $"up to {_maxArgs}" : "any number of")}");
          }
          else if (_maxArgs < int.MaxValue)
          {
            sb.Append($"{_minArgs}-{_maxArgs}");
          }
          else
          {
            sb.Append($"at least {_minArgs}");
          }

          sb.Append($" argument{(_maxArgs != 1 ? "s" : "")}");
        }
        logger?.Invoke(sb.ToString());
      }

      protected override Process _spawnImpl(ArgumentsWrapper wrapper, Action<string> logger, Action<Process> onDone, IProcessSpawner spawner, CommandTrigger trigger)
      {
        if (wrapper.HasSwitch("h") || wrapper.RemaingCount > _maxArgs || wrapper.RemaingCount < _minArgs)
        {
          _printHelpOrError(wrapper, logger, $"Wrong number of arguments");
          return null;
        }
        Action<Process> action = _provider(wrapper, logger);
        return spawner.Spawn(action, wrapper.Command, onDone, useOnce: true);
      }
    }
  }
}