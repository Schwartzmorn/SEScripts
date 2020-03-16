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
    public class RoutineParser {
      readonly CommandLine commandLine;
      public RoutineParser(CommandLine commandLine) {
        this.commandLine = commandLine;
      }
      public List<AutoRoutine> Parse(string routineString) {
        var routines = new List<AutoRoutine>();
        var current = new Stack<List<Instruction>>();
        int count = 0;
        foreach (string l in routineString.Split(new char[]{ '\n' })) {
          ++count;
          string line = l.Trim();
          if (line == "" || line.StartsWith(";")) {
            continue;
          }
          if (line.StartsWith("=")) {
            if (current.Count > 1) {
              throw new InvalidOperationException($"Unexpected start of new auto routine at line {count}");
            } else if (current.Count == 1) {
              current.Pop();
            }
            current.Push(new List<Instruction>());
            routines.Add(new AutoRoutine(line.Substring(1).Trim(), current.Peek()));
          } else if (current.Count == 0) {
            throw new InvalidOperationException($"Unexpected instruction '{line}' at line {count} outside of a routine");
          } else if (line.StartsWith("while")) {
            var instructions = new List<Instruction>();
            current.Peek().Add(new WhileInstruction(this.parse(line.Substring(5).Trim(), count), instructions));
            current.Push(instructions);
          } else if (line == "end") {
            current.Pop();
          } else {
            current.Peek().Add(this.parse(line, count));
          }
        }
        return routines;
      }

      private SingleInstruction parse(string s, int count) {
        try {
          if (s.StartsWith("wait")) {
            return new WaitInstruction(s.Substring(4));
          } else if (s == "forever") {
            return new ForeverInstruction();
          } else if (s.StartsWith("-")) {
            MyTuple<string, List<string>> parsed = this.commandLine.ParseCommand(s);
            return new CommandInstruction(parsed.Item1, parsed.Item2, this.commandLine);
          }
        } catch (Exception) { }
        throw new InvalidOperationException($"Could not parse instruction '{s}' at line {count}");
      }
    }
  }
}
