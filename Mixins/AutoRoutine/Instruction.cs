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
    /// <summary>Base class for the instructions</summary>
    public abstract class Instruction
    {
      /// <summary>Executes the instruction (or sequence of instructions)</summary>
      /// <param name="parent">parent process to spawn the processes</param>
      /// <param name="onDone">Callback to execute when the process is terminated</param>
      /// <param name="args">Arguments provided to the auto routine</param>
      public abstract void Execute(Process parent, Action<Process> onDone, ArgumentsWrapper args);
      /// <summary>Returns the minimum number of arguments needed by the instruction</summary>
      /// <returns>the number of arguments</returns>
      public abstract int ArgsCount();
    }
    /// <summary>Superclass of all the simple instructions</summary>
    public abstract class SingleInstruction : Instruction
    {
      protected int _getArgCount(string s)
      {
        if (s.StartsWith("$"))
        {
          int res;
          if (int.TryParse(s.Substring(1), out res))
          {
            return res;
          }
        }
        return 0;
      }
    }
    /// <summary>Instruction that executes a list of instruction one after the other</summary>
    public class MultipleInstruction : Instruction
    {
      readonly List<Instruction> _instructions;
      /// <summary>Creates a sequence of instructions that will be executed one after the other until the last one</summary>
      /// <param name="instructions">List of instructions to execute</param>
      public MultipleInstruction(List<Instruction> instructions)
      {
        _instructions = instructions;
      }
      /// <summary>Execute all the <see cref="_instructions"/> in sequence</summary>
      /// <param name="parent">Process used to spawn the instructions</param>
      /// <param name="onDone">Callback to use once the last instruction terminates</param>
      /// <param name="args">Arguments provided to the auto routine</param>
      public override void Execute(Process parent, Action<Process> onDone, ArgumentsWrapper args)
      {
        _executeNext(_instructions.GetEnumerator(), parent, onDone, args);
      }
      public override int ArgsCount() => (_instructions != null && _instructions.Count > 0) ? _instructions.Max(i => i.ArgsCount()) : 0;
      void _executeNext(List<Instruction>.Enumerator instruction, Process parent, Action<Process> onDone, ArgumentsWrapper args)
      {
        if (parent.Active)
        {
          if (instruction.MoveNext())
          {
            instruction.Current.Execute(parent, p => _executeNext(instruction, parent, onDone, args), args);
          }
          else
          {
            onDone?.Invoke(parent);
          }
        }
      }
    }
    /// <summary>Executes the instructions of the loop until the condition instruction terminates, at which point it kills itself</summary>
    public class WhileInstruction : MultipleInstruction
    {
      readonly SingleInstruction _condition;
      /// <summary>Creates a interupting "while" loop. It differs from a real while loop as the condition is a process that runs in parallel to the loop body and interrupts the body whenever it terminates</summary>
      /// <param name="condition">Instruction that makes the loop stop when it terminates</param>
      /// <param name="instructions">Instruction to execute in a loop</param>
      public WhileInstruction(SingleInstruction condition, List<Instruction> instructions) : base(instructions)
      {
        _condition = condition;
      }
      public override void Execute(Process parent, Action<Process> onDone, ArgumentsWrapper args)
      {
        Process whileProcess = parent.Spawn(null, "ar-while", onDone, period: 1, useOnce: false);
        base.Execute(whileProcess, p => _onLoopDone(whileProcess, args), args);
        _condition.Execute(whileProcess, p => _onConditionDone(whileProcess, onDone), args);
      }
      public override int ArgsCount() => Math.Max(_condition.ArgsCount(), base.ArgsCount());
      void _onConditionDone(Process whileProcess, Action<Process> onDone)
      {
        whileProcess.Kill();
        onDone?.Invoke(whileProcess);
      }
      void _onLoopDone(Process whileProcess, ArgumentsWrapper args)
      {
        if (whileProcess.Active)
        {
          base.Execute(whileProcess, p => _onLoopDone(whileProcess, args), args);
        }
      }
    }
    /// <summary>Simple instruction that execute a command from a <see cref="CommandLine"/></summary>
    public class CommandInstruction : SingleInstruction
    {
      readonly ArgumentsWrapper _args;
      readonly int _argsCount = 0;
      readonly string _command;
      readonly CommandLine _commandLine;
      public CommandInstruction(string command, ArgumentsWrapper args, CommandLine commandLine)
      {
        _command = command;
        _args = args;
        _commandLine = commandLine;
        if (args.RemaingCount > 0)
        {
          _argsCount = _args.Max(s => _getArgCount(s));
        }
      }
      public override void Execute(Process parent, Action<Process> onDone, ArgumentsWrapper args)
      {
        var actualArgs = _args.ToList();
        for (int i = 0; i < actualArgs.Count; ++i)
        {
          int n = _getArgCount(actualArgs[i]);
          if (n > 0)
          {
            actualArgs[i] = args[n - 1];
          }
        }
        var serializer = new CommandSerializer(_command);
        foreach (string s in actualArgs)
        {
          serializer.AddArg(s);
        }
        _commandLine.StartCmd(serializer.ToString(), CommandTrigger.Cmd, onDone, parent);
      }
      public override int ArgsCount() => _argsCount;
    }
    /// <summary>Simple instruction that terminates after a determined number of ticks</summary>
    public class WaitInstruction : SingleInstruction
    {
      readonly string _time;
      /// <summary>Creates a wait instruction</summary>
      /// <param name="time">Number of ticks to wait, can be an arg reference</param>
      public WaitInstruction(string time)
      {
        _time = time.Trim();
      }
      public override void Execute(Process parent, Action<Process> onDone, ArgumentsWrapper args)
      {
        int time = _time.StartsWith("$")
          ? int.Parse(args[int.Parse(_time.Substring(1)) - 1])
          : int.Parse(_time);
        parent.Spawn(null, "ar-wait", onDone, time, true);
      }
      public override int ArgsCount() => _getArgCount(_time);
    }
    /// <summary>Simple instruction that never terminates</summary>
    public class ForeverInstruction : SingleInstruction
    {
      public override void Execute(Process parent, Action<Process> onDone, ArgumentsWrapper args)
      {
        // period is actually irrelevant since it does not do anything
        parent.Spawn(null, "ar-forever", onDone, period: 100);
      }
      public override int ArgsCount() => 0;
    }


  }
}
