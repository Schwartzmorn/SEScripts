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
    public class AutoRoutine: MultipleInstruction {
      public readonly string Name;
      public AutoRoutine(string name, List<Instruction> instructions): base(instructions) {
        this.Name = name;
      }

      public override void Execute(Process parent, Action<Process> onDone, List<string> args) {
        int argCount = this.ArgsCount();
        if (argCount > 0 && (args == null || args.Count < argCount)) {
          throw new InvalidOperationException($"AutoRoutine '{this.Name}' expects at least {argCount} arguments.");
        }
        base.Execute(parent, onDone, args);
      }
    }

    public class AutoRoutineHandler {
      readonly Dictionary<string, AutoRoutine> routines = new Dictionary<string, AutoRoutine>();
      public AutoRoutineHandler(CommandLine commandLine) {
        commandLine.RegisterCommand(new Command("ar-execute", this.execute, "Execute a routine", minArgs: 1));
        commandLine.RegisterCommand(new Command("ar-list", this.list, "Lists all the routines", nArgs: 0));
      }

      public void AddRoutines(List<AutoRoutine> routines) {
        foreach (AutoRoutine routine in routines) {
          this.routines.Add(routine.Name, routine);
        }
      }

      private MyTuple<int, bool, Action<Process>> list(List<string> args, Action<string> logger) {
        return MyTuple.Create<int, bool, Action<Process>>(1, true, p => {
          logger("Available routines:");
          foreach (var kv in this.routines) {
            logger($"  '{kv.Key}': 0 argument");
          }
        });
      }

      private MyTuple<int, bool, Action<Process>> execute(List<string> args, Action<string> logger) {
        return MyTuple.Create<int, bool, Action<Process>>(1, true, p => {
          AutoRoutine routine;
          if (this.routines.TryGetValue(args[0], out routine)) {
            // TODO check arguments
            routine.Execute(p, null, args);
          } else {
            logger($"Could not find routine {args[0]}");
          }
        });
      }
    }
  }
}
