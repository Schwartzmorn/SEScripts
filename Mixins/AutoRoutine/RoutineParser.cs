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
    /// <summary>Class that parses the routines</summary>
    public class RoutineParser
    {
      readonly CommandLine _commandLine;
      /// <summary>Creates a new parser</summary>
      /// <param name="commandLine">Command line that will be used to execute commands</param>
      public RoutineParser(CommandLine commandLine)
      {
        _commandLine = commandLine;
      }
      /// <summary>
      /// Parses the string and creates the auto routines. The syntax is as follows:
      /// <list type="bullet">
      /// <item>Empty lines and lines that start with a ';' are ignored</item>
      /// <item>A line that starts with a '=' denotes a new <see cref="AutoRoutine"/>, followed by its name</item>
      /// <item>A line that starts with the keyword 'while' starts a new <see cref="WhileInstruction"/>. The keyword must then be followed by a <see cref="SingleInstruction"/></item>
      /// <item>A while instruction must then be ended with the keyword 'end'. Can also be optionally used to terminate an <see cref="AutoRoutine"/></item>
      /// <item>A line that starts with a '-' will create a <see cref="CommandInstruction"/> that will execute the command as written on the line, modulo placeholders (see below)</item>
      /// <item>'wait' will create a <see cref="WaitInstruction"/> that just wait for the specified amount of ticks</item>
      /// <item>'forever' creates a <see cref="ForeverInstruction"/> that never ends</item>
      /// </list>
      /// </summary>
      /// In lieu of arguments, a routine can use placeholder denoted by '$1', '$2', etc. that will then be replaced by the arguments given at the start of the autoroutine
      /// <param name="routineString"></param>
      /// <returns></returns>
      public List<AutoRoutine> Parse(string routineString)
      {
        var routines = new List<AutoRoutine>();
        var current = new Stack<List<Instruction>>();
        int count = 0;
        foreach (string l in routineString.Split(new char[] { '\n' }))
        {
          ++count;
          string line = l.Trim();
          if (line == "" || line.StartsWith(";"))
          {
            continue;
          }
          if (line.StartsWith("="))
          {
            if (current.Count > 1)
            {
              throw new InvalidOperationException($"Unexpected start of new auto routine at line {count}");
            }
            else if (current.Count == 1)
            {
              current.Pop();
            }
            current.Push(new List<Instruction>());
            routines.Add(new AutoRoutine(line.Substring(1).Trim(), current.Peek()));
          }
          else if (current.Count == 0)
          {
            throw new InvalidOperationException($"Unexpected instruction '{line}' at line {count} outside of a routine");
          }
          else if (line.StartsWith("while"))
          {
            var instructions = new List<Instruction>();
            current.Peek().Add(new WhileInstruction(_parse(line.Substring(5).Trim(), count), instructions));
            current.Push(instructions);
          }
          else if (line == "end")
          {
            current.Pop();
          }
          else
          {
            current.Peek().Add(_parse(line, count));
          }
        }
        return routines;
      }

      SingleInstruction _parse(string s, int count)
      {
        try
        {
          if (s.StartsWith("wait"))
          {
            return new WaitInstruction(s.Substring(4));
          }
          else if (s == "forever")
          {
            return new ForeverInstruction();
          }
          else
          {
            MyTuple<string, ArgumentsWrapper> parsed = _commandLine.ParseCommand(s);
            return new CommandInstruction(parsed.Item1, parsed.Item2, _commandLine);
          }
        }
        catch (Exception) { }
        throw new InvalidOperationException($"Could not parse instruction '{s}' at line {count}");
      }
    }
  }
}
