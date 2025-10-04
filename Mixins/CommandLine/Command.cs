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
    public delegate MyTuple<int, bool, Action<Process>> ActionProvider(List<string> arguments, Action<string> logger);
    /// <summary>
    /// <para>The most important in this class is the <see cref="ActionProvider"/> <see cref="_provider"/>:</para>
    /// <para>It is a function that takes the arguments <see cref="List{string}"/> from a parsed command, an optional logger <see cref="Action{string}"/> and returns a <see cref="MyTuple"/> that contains:
    /// <list type="bullet">
    ///   <item>The <see cref="Process.Period"/></item>
    ///   <item>Whether the process is <see cref="Process.UseOnce"/></item>
    ///   <item>The <see cref="Process._action"/></item>
    /// </list>
    /// </para>
    /// </summary>
    public class Command {

      public readonly string BriefHelp;
      public readonly int MaxArgs = int.MaxValue;
      public readonly int MinArgs = 0;
      public readonly string Name;
      public readonly CommandTrigger RequiredTrigger;

      readonly List<string> _help;
      readonly ActionProvider _provider;
      /// <summary>Method to wrap an <see cref="Action"/> as a command</summary>
      public static ActionProvider Wrap(Action action, int period = 1, bool useOnce = true) => (args, logger) => MyTuple.Create<int, bool, Action<Process>>(period, useOnce, _ => action());
      /// <summary>Method to wrap an <see cref="Action{Process}"/> as a command</summary>
      public static ActionProvider Wrap(Action<Process> action, int period = 1, bool useOnce = true) => (args, logger) => MyTuple.Create<int, bool, Action<Process>>(period, useOnce, p => action(p));
      /// <summary>Method to wrap an <see cref="Action{string}"/> as a command</summary>
      public static ActionProvider Wrap(Action<string> action, int period = 1, bool useOnce = true) => (args, logger) => MyTuple.Create<int, bool, Action<Process>>(period, useOnce, _ => action(args[0]));
      /// <summary>Method to wrap an <see cref="Action{Process, string}"/> as a command</summary>
      public static ActionProvider Wrap(Action<Process, string> action, int period = 1, bool useOnce = true) => (args, logger) => MyTuple.Create<int, bool, Action<Process>>(period, useOnce, p => action(p, args[0]));
      /// <summary>Method to wrap an <see cref="Action{List{string}}"/> as a command</summary>
      public static ActionProvider Wrap(Action<List<string>> action, int period = 1, bool useOnce = true) => (args, logger) => MyTuple.Create<int, bool, Action<Process>>(period, useOnce, _ => action(args));
      /// <summary>Method to wrap an <see cref="Action{Process, List{string}}"/> as a command</summary>
      public static ActionProvider Wrap(Action<Process, List<string>> action, int period = 1, bool useOnce = true) => (args, logger) => MyTuple.Create<int, bool, Action<Process>>(period, useOnce, p => action(p, args));
      /// <summary>Method to wrap an <see cref="Action{List{string}, Action{string}}"/> as a command</summary>
      public static ActionProvider Wrap(Action<List<string>, Action<string>> action, int period = 1, bool useOnce = true) => (args, logger) => MyTuple.Create<int, bool, Action<Process>>(period, useOnce, _ => action(args, logger));
      /// <summary>Creates a new command ready to be registered in a <see cref="CommandLine"/>.</summary>
      /// <param name="name">Unique name of the command, used summon the command.</param>
      /// <param name="actionProvider">Function that returns the </param>
      /// <param name="briefHelp"></param>
      /// <param name="detailedHelp"></param>
      /// <param name="maxArgs">Ignored if <paramref name="nArgs"/> is given</param>
      /// <param name="minArgs">Ignored if <paramref name="nArgs"/> is given</param>
      /// <param name="nArgs">Number of argguments required by the command</param>
      /// <param name="requiredTrigger"></param>
      public Command(string name, ActionProvider actionProvider, string briefHelp, string detailedHelp = null, int maxArgs = int.MaxValue, int minArgs = 0, int nArgs = -1, CommandTrigger requiredTrigger = CommandTrigger.User) {
        Name = name;
        _provider = actionProvider;
        BriefHelp = briefHelp;
        if (detailedHelp != null) {
          _help = detailedHelp.Split(new char[] { '\n' }).ToList();
        }
        if (nArgs >= 0) {
          MaxArgs = MinArgs = nArgs;
        } else {
          MaxArgs = maxArgs;
          MinArgs = minArgs;
        }
        RequiredTrigger = requiredTrigger;
      }
      /// <summary>Spawns a new <see cref="Process"/>, taking into account the arguments. Can fail and return null if the arguments are incorrect.</summary>
      /// <param name="args">The arguments given to the command line.</param>
      /// <param name="logger">The logger the process can use to log.</param>
      /// <param name="onDone">The callback to call on process termination.</param>
      /// <param name="spawner">Used to spawn the process</param>
      /// <param name="trigger">What triggered the command</param>
      /// <returns>The spawned process, if successful.</returns>
      public Process Spawn(List<string> args, Action<string> logger, Action<Process> onDone, IProcessSpawner spawner, CommandTrigger trigger) {
        if (trigger < RequiredTrigger) {
          logger?.Invoke($"Permission denied for '{Name}'");
        } else if (args.Count <= MaxArgs && args.Count >= MinArgs) {
          MyTuple<int, bool, Action<Process>> parameters = _provider(args, logger);
          return spawner.Spawn(parameters.Item3, Name, onDone, parameters.Item1, parameters.Item2);
        } else {
          logger?.Invoke($"Wrong number of arguments for '{Name}'");
          logger?.Invoke($"Run -help '{Name}' for more info");
        }
        return null;
      }
      /// <summary>Returns some help on the command</summary>
      /// <param name="logger">Logger to use to display the help</param>
      public void DetailedHelp(Action<string> logger) {
        string s = $"-{Name}: ";
        if (MaxArgs == 0) {
          s += "(no argument)";
        } else {
          s += "takes ";
          if (MinArgs == MaxArgs) {
            s += $"{MinArgs}";
          } else if (MinArgs == 0) {
            s += $"{(MaxArgs < int.MaxValue ? $"up to {MaxArgs}" : "any number of")}";
          } else if (MaxArgs < int.MaxValue) {
            s += $"{MinArgs}-{MaxArgs}";
          } else {
            s += $"at least {MinArgs}";
          }

          s += $" argument{(MaxArgs > 1 ? "s" : "")}";
        }
        logger(s);
        if (_help != null) {
          foreach (string h in _help) {
            logger("  " + h);
          }
        } else {
          logger("  " + BriefHelp);
        }
      }
    }
  }
}
