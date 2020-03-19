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
    /// <summary>The auto routine is just a <see cref="MultipleInstruction"/> with a name</summary>
    public class AutoRoutine: MultipleInstruction {
      public readonly string Name;
      public AutoRoutine(string name, List<Instruction> instructions): base(instructions) {
        this.Name = name;
      }
    }
    /// <summary>Entry point for <see cref="AutoRoutine"/></summary>
    public class AutoRoutineHandler {
      readonly Dictionary<string, AutoRoutine> routines = new Dictionary<string, AutoRoutine>();
      public AutoRoutineHandler(CommandLine commandLine) {
        commandLine.RegisterCommand(new Command("ar-execute", this.execute, "Execute a routine", minArgs: 1));
        commandLine.RegisterCommand(new Command("ar-list", Command.Wrap(this.list), "Lists all the routines", nArgs: 0));
      }

      public void AddRoutines(List<AutoRoutine> routines) {
        foreach (AutoRoutine routine in routines) {
          this.routines.Add(routine.Name, routine);
        }
      }

      void list(List<string> args, Action<string> logger) {
        logger("Available routines:");
        foreach (KeyValuePair<string, AutoRoutine> kv in this.routines) {
          int argc = kv.Value.ArgsCount();
          logger($"  '{kv.Key}': takes {(argc == 0 ? "no" : argc.ToString())} argument{(argc > 1 ? "s" : "")}");
        }
      }

      MyTuple<int, bool, Action<Process>> execute(List<string> args, Action<string> logger) {
        return MyTuple.Create<int, bool, Action<Process>>(1, true, p => {
          AutoRoutine routine;
          if (this.routines.TryGetValue(args[0], out routine)) {
            int argc = routine.ArgsCount();
            if (args.Count > argc) {
              routine.Execute(p, null, args.GetRange(1, argc));
            } else {
              logger($"Routine '{args[0]}' needs {argc} argument{(argc > 1 ? "s" : "")}");
            }
          } else {
            logger($"Could not find routine {args[0]}");
          }
        });
      }
    }
  }
}
