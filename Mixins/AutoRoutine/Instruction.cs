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
      public abstract void Execute(Process parent, Action<Process> onDone);
    }
    /// <summary>Superclass of all the simple instructions</summary>
    public abstract class SingleInstruction: Instruction { }
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
      public override void Execute(Process parent, Action<Process> onDone) {
        this.executeNext(this.instructions.GetEnumerator(), parent, onDone);
      }
      void executeNext(List<Instruction>.Enumerator instruction, Process parent, Action<Process> onDone) {
        if (parent.Active) {
          if (instruction.MoveNext()) {
            instruction.Current.Execute(parent, p => this.executeNext(instruction, parent, onDone));
          } else {
            onDone?.Invoke(parent);
          }
        }
      }
    }
    /// <summary>Executes the instructions of the loop until the condition instruction terminates, at which point it kills itself</summary>
    public class WhileInstruction : MultipleInstruction {
      readonly SingleInstruction condition;
      /// <summary>Creates a interupting "while" loop. It differs from a real while loop as the condition is a process that runs in parallel to the loop body and interupts the body whenever it terminates</summary>
      /// <param name="condition">Instruction that makes the loop stop when it terminates</param>
      /// <param name="instructions">Instruction to execute in a loop</param>
      public WhileInstruction(SingleInstruction condition, List<Instruction> instructions) : base(instructions) {
        this.condition = condition;
      }
      public override void Execute(Process parent, Action<Process> onDone) {
        Process whileProcess = parent.Spawn(null, "ar-while", onDone, period: 1, useOnce: false);
        base.Execute(whileProcess, p => this.onLoopDone(whileProcess));
        this.condition.Execute(whileProcess, p => this.onConditionDone(whileProcess, onDone));
      }
      void onConditionDone(Process whileProcess, Action<Process> onDone) {
        whileProcess.Kill();
        onDone?.Invoke(whileProcess);
      }
      void onLoopDone(Process whileProcess) {
        if (whileProcess.Active) {
          base.Execute(whileProcess, p => this.onLoopDone(whileProcess));
        }
      }
    }
    /// <summary>Simple instruction that execute a command from a <see cref="CommandLine"/></summary>
    public class CommandInstruction: SingleInstruction {
      readonly string command;
      readonly CommandLine commandLine;
      public CommandInstruction(string command, CommandLine commandLine) {
        this.command = command;
        this.commandLine = commandLine;
      }
      public override void Execute(Process parent, Action<Process> onDone) {
        this.commandLine.StartCmd(this.command, CommandTrigger.Cmd, onDone, parent);
      }
    }
    /// <summary>Simple instruction that terminates after a determined number of ticks</summary>
    public class WaitInstruction : SingleInstruction {
      readonly int time;
      /// <summary>Creates a wait instruction</summary>
      /// <param name="time">Number of ticks to wait</param>
      public WaitInstruction(int time) {
        this.time = time;
      }
      public override void Execute(Process parent, Action<Process> onDone) {
        parent.Spawn(null, "ar-wait", onDone, this.time, true);
      }
    }
    /// <summary>Simple instruction that never terminates</summary>
    public class ForeverInstruction : SingleInstruction {
      public override void Execute(Process parent, Action<Process> onDone) {
        // period is actually irrelevant since it does not do anything
        parent.Spawn(null, "ar-forever", onDone, period: 100);
      }
    }


  }
}
