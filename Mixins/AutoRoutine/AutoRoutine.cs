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
    /// <summary>The auto routine is just a <see cref="MultipleInstruction"/> with a name</summary>
    public class AutoRoutine : MultipleInstruction
    {
      public readonly string Name;
      public AutoRoutine(string name, List<Instruction> instructions) : base(instructions)
      {
        Name = name;
      }

      public override void Execute(Process parent, Action<Process> onDone, ArgumentsWrapper args)
      {
        // we cannot use the commmand line process as the parent directly, as it is active for only one tick
        var process = parent.Spawn(null, $"ar-execute {Name}", onDone);
        base.Execute(process, _ => process.Done(), args);
      }
    }
    /// <summary>Entry point for <see cref="AutoRoutine"/></summary>
    public class AutoRoutineHandler
    {
      readonly Dictionary<string, AutoRoutine> _routines = new Dictionary<string, AutoRoutine>();
      public AutoRoutineHandler(CommandLine commandLine)
      {
        commandLine.RegisterCommand(new ParentCommand("ar", "Interacts with the autoroutines")
          .AddSubCommand(new Command("execute", _execute, "Execute a routine", minArgs: 1))
          .AddSubCommand(new Command("list", Command.Wrap(_list), "Lists all the routines", nArgs: 0)));
      }

      public void AddRoutines(List<AutoRoutine> routines)
      {
        foreach (AutoRoutine routine in routines)
        {
          _routines.Add(routine.Name, routine);
        }
      }

      void _list(ArgumentsWrapper _args, Action<string> logger)
      {
        logger("Available routines:");
        foreach (KeyValuePair<string, AutoRoutine> kv in _routines)
        {
          int argc = kv.Value.ArgsCount();
          logger($"  '{kv.Key}': takes {(argc == 0 ? "no" : argc.ToString())} argument{(argc > 1 ? "s" : "")}");
        }
      }

      Action<Process> _execute(ArgumentsWrapper args, Action<string> logger)
      {
        return p =>
        {
          AutoRoutine routine;
          if (_routines.TryGetValue(args[0], out routine))
          {
            int argc = routine.ArgsCount();
            if (args.RemaingCount > argc)
            {
              args.Next();
              routine.Execute(p, null, args);
            }
            else
            {
              logger($"Routine '{args[0]}' needs {argc} argument{(argc > 1 ? "s" : "")}");
            }
          }
          else
          {
            logger($"Could not find routine {args[0]}");
          }
        };
      }
    }
  }
}
