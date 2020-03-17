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
    /// <summary>Base class for the instructions</summary>
    public abstract class Instruction {
      /// <summary>Executes the instruction (or sequence of instructions)</summary>
      /// <param name="parent">parent process to spawn the processes</param>
      /// <param name="onDone">Callback to execute when the process is terminated</param>
      /// <param name="args">Arguments provided to the auto routine</param>
      public abstract void Execute(Process parent, Action<Process> onDone, List<string> args);
      /// <summary>Returns the minimum number of arguments needed by the instruction</summary>
      /// <returns>the number of arguments</returns>
      public abstract int ArgsCount();
    }
    /// <summary>Superclass of all the simple instructions</summary>
    public abstract class SingleInstruction: Instruction {
      protected int GetArgCount(string s) {
        if (s.StartsWith("$")) {
          int res;
          if (int.TryParse(s.Substring(1), out res)) {
            return res;
          }
        }
        return 0;
      }
    }
    /// <summary>Instruction that executes a list of instruction one after the other</summary>
    public class MultipleInstruction : Instruction {
      readonly List<Instruction> instructions;
      /// <summary>Creates a sequence of instructions that will be executed one after the other until the last one</summary>
      /// <param name="instructions">List of instructions to execute</param>
      public MultipleInstruction(List<Instruction> instructions) {
        this.instructions = instructions;
      }
      /// <summary>Execute all the <see cref="instructions"/> in sequence</summary>
      /// <param name="parent">Process used to spawn the instructions</param>
      /// <param name="onDone">Callback to use once the last instruction terminates</param>
      /// <param name="args">Arguments provided to the auto routine</param>
      public override void Execute(Process parent, Action<Process> onDone, List<string> args) {
        this.executeNext(this.instructions.GetEnumerator(), parent, onDone, args);
      }
      public override int ArgsCount() => (this.instructions != null && this.instructions.Count > 0)  ? this.instructions.Max(i => i.ArgsCount()) : 0;
      void executeNext(List<Instruction>.Enumerator instruction, Process parent, Action<Process> onDone, List<string> args) {
        if (parent.Active) {
          if (instruction.MoveNext()) {
            instruction.Current.Execute(parent, p => this.executeNext(instruction, parent, onDone, args), args);
          } else {
            onDone?.Invoke(parent);
          }
        }
      }
    }
    /// <summary>Executes the instructions of the loop until the condition instruction terminates, at which point it kills itself</summary>
    public class WhileInstruction : MultipleInstruction {
      readonly SingleInstruction condition;
      /// <summary>Creates a interupting "while" loop. It differs from a real while loop as the condition is a process that runs in parallel to the loop body and interrupts the body whenever it terminates</summary>
      /// <param name="condition">Instruction that makes the loop stop when it terminates</param>
      /// <param name="instructions">Instruction to execute in a loop</param>
      public WhileInstruction(SingleInstruction condition, List<Instruction> instructions) : base(instructions) {
        this.condition = condition;
      }
      public override void Execute(Process parent, Action<Process> onDone, List<string> args) {
        Process whileProcess = parent.Spawn(null, "ar-while", onDone, period: 1, useOnce: false);
        base.Execute(whileProcess, p => this.onLoopDone(whileProcess, args), args);
        this.condition.Execute(whileProcess, p => this.onConditionDone(whileProcess, onDone), args);
      }
      public override int ArgsCount() => Math.Max(this.condition.ArgsCount(), base.ArgsCount());
      void onConditionDone(Process whileProcess, Action<Process> onDone) {
        whileProcess.Kill();
        onDone?.Invoke(whileProcess);
      }
      void onLoopDone(Process whileProcess, List<string> args) {
        if (whileProcess.Active) {
          base.Execute(whileProcess, p => this.onLoopDone(whileProcess, args), args);
        }
      }
    }
    /// <summary>Simple instruction that execute a command from a <see cref="CommandLine"/></summary>
    public class CommandInstruction: SingleInstruction {
      readonly List<string> args;
      readonly int argsCount = 0;
      readonly string command;
      readonly CommandLine commandLine;
      public CommandInstruction(string command, List<string> args, CommandLine commandLine) {
        this.command = command;
        this.args = args;
        this.commandLine = commandLine;
        if (args.Count > 0) {
          this.argsCount = this.args.Max(s => this.GetArgCount(s));
        }
      }
      public override void Execute(Process parent, Action<Process> onDone, List<string> args) {
        var actualArgs = this.args.ToList();
        for (int i = 0; i < actualArgs.Count; ++i) {
          int n = this.GetArgCount(actualArgs[i]);
          if (n > 0) {
            actualArgs[i] = args[n - 1];
          }
        }
        var serializer = new CommandSerializer(this.command);
        foreach(string s in actualArgs) {
         serializer.AddArg(s);
        }
        this.commandLine.StartCmd(serializer.ToString(), CommandTrigger.Cmd, onDone, parent);
      }
      public override int ArgsCount() => this.argsCount;
    }
    /// <summary>Simple instruction that terminates after a determined number of ticks</summary>
    public class WaitInstruction : SingleInstruction {
      readonly string time;
      /// <summary>Creates a wait instruction</summary>
      /// <param name="time">Number of ticks to wait, can be an arg reference</param>
      public WaitInstruction(string time) {
        this.time = time.Trim();
      }
      public override void Execute(Process parent, Action<Process> onDone, List<string> args) {
        int time = this.time.StartsWith("$")
          ? int.Parse(args[int.Parse(this.time.Substring(1)) - 1])
          : int.Parse(this.time);
        parent.Spawn(null, "ar-wait", onDone, time, true);
      }
      public override int ArgsCount() => this.GetArgCount(this.time);
    }
    /// <summary>Simple instruction that never terminates</summary>
    public class ForeverInstruction : SingleInstruction {
      public override void Execute(Process parent, Action<Process> onDone, List<string> args) {
        // period is actually irrelevant since it does not do anything
        parent.Spawn(null, "ar-forever", onDone, period: 100);
      }
      public override int ArgsCount() => 0;
    }


  }
}
