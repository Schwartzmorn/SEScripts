using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IngameScript.MDK {
  class InstructionsTest {
    List<string> commandCalls;
    Program.CommandLine commandLine;
    Program.IProcessManager manager;
    Program.MockAction mock;
    Program.Process process;
    public void BeforeEach() {
      this.commandCalls = new List<string>();
      this.manager = Program.Process.CreateManager(s => System.Diagnostics.Debug.WriteLine(s));
      this.commandLine = new Program.CommandLine("test", null, this.manager);
      this.commandLine.RegisterCommand(new Program.Command("cmd", this.command, "", requiredTrigger: Program.CommandTrigger.Cmd));
      this.mock = new Program.MockAction();
      this.process = this.manager.Spawn(null, "test");
    }

    void tick() => this.manager.Tick();

    List<string> getProcesses() {
      var processes = new List<string>();
      this.manager.Log(s => processes.Add(s));
      return processes;
    }

    VRage.MyTuple<int, bool, Action<Program.Process>> command(List<string> args, Action<string> logger) {
      return VRage.MyTuple.Create<int, bool, Action<Program.Process>>(1, true, p => { this.commandCalls.Add(string.Join(",", args)); });
    }

    public void CommandInstruction() {
      var cmd = new Program.CommandInstruction("cmd", new List<string>{"arg1", "arg2"}, this.commandLine);

      cmd.Execute(this.process, this.mock.Action, new List<string>());
      List<string> processes = this.getProcesses();

      Assert.AreEqual(2, processes.Count);
      Assert.IsTrue(processes.Any(s => s.Contains("cmd")));
      Assert.IsFalse(this.mock.Called);

      this.tick();
      processes = this.getProcesses();

      Assert.AreEqual(1, processes.Count);
      Assert.AreEqual(1, this.commandCalls.Count);
      Assert.AreEqual("arg1,arg2", this.commandCalls[0]);
      Assert.IsTrue(this.mock.Called);
      Assert.IsFalse(processes.Any(s => s.Contains("cmd")));
    }
    public void KillCommandInstruction() {
      // We check that the callback is executed correctly when we kill the process
      var cmd = new Program.CommandInstruction("cmd", new List<string>(), this.commandLine);

      cmd.Execute(this.process, this.mock.Action, new List<string>());

      Assert.IsFalse(this.mock.Called);

      this.manager.KillAll("cmd");

      Assert.IsTrue(this.mock.Called);

      this.tick();

      Assert.AreEqual(1, this.mock.CallCount);
    }

    public void WaitInstruction() {
      var wait = new Program.WaitInstruction("4");

      wait.Execute(this.process, this.mock.Action, new List<string>());
      List<string> processes = this.getProcesses();

      Assert.AreEqual(2, processes.Count);
      Assert.IsTrue(processes.Any(s => s.Contains("ar-wait")));

      foreach(int i in Enumerable.Range(0, 3)) {
        this.tick();
      }
      processes = this.getProcesses();

      Assert.AreEqual(2, processes.Count);
      Assert.IsFalse(this.mock.Called);
      Assert.IsTrue(processes.Any(s => s.Contains("ar-wait")));

      this.tick();
      processes = this.getProcesses();

      Assert.AreEqual(1, processes.Count);
      Assert.IsTrue(this.mock.Called);
      Assert.IsFalse(processes.Any(s => s.Contains("ar-wait")));
    }
    public void KillWaitInstruction() {
      var wait = new Program.WaitInstruction("1");

      wait.Execute(this.process, this.mock.Action, new List<string>());

      Assert.IsFalse(this.mock.Called);

      this.manager.KillAll("ar-wait");

      Assert.IsTrue(this.mock.Called);

      this.tick();

      Assert.AreEqual(1, this.mock.CallCount);
    }

    public void ForeverInstruction() {
      var always = new Program.ForeverInstruction();

      always.Execute(this.process, this.mock.Action, new List<string>());
      List<string> processes = this.getProcesses();

      Assert.AreEqual(2, processes.Count);
      Assert.IsTrue(processes.Any(s => s.Contains("ar-forever")));

      foreach (int i in Enumerable.Range(0, 200)) {
        this.tick();
      }
      processes = this.getProcesses();

      Assert.AreEqual(2, processes.Count);
      Assert.IsTrue(processes.Any(s => s.Contains("ar-forever")));
      Assert.IsFalse(this.mock.Called);

      this.manager.KillAll("ar-forever");
      Assert.IsTrue(this.mock.Called);
    }

    public void MultipleInstruction() {
      var multiple = new Program.MultipleInstruction(new List<Program.Instruction>{
        new Program.CommandInstruction("cmd", new List<string>{"1"}, this.commandLine),
        new Program.WaitInstruction("2"),
        new Program.CommandInstruction("cmd", new List<string>{"2"}, this.commandLine)
      });

      multiple.Execute(this.process, this.mock.Action, new List<string>());

      this.tick();

      Assert.AreEqual(2, this.getProcesses().Count);
      Assert.IsTrue(this.getProcesses().Any(s => s.Contains("ar-wait")));
      Assert.AreEqual(1, this.commandCalls.Count);
      Assert.AreEqual("1", this.commandCalls[0]);

      this.tick();
      this.tick();

      Assert.IsFalse(this.mock.Called);

      this.tick();

      Assert.AreEqual(1, this.getProcesses().Count);
      Assert.AreEqual(2, this.commandCalls.Count);
      Assert.AreEqual("2", this.commandCalls[1]);
      Assert.IsTrue(this.mock.Called);
    }

    public void KillMultipleInstruction() {
      var multiple = new Program.MultipleInstruction(new List<Program.Instruction>{
        new Program.CommandInstruction("cmd", new List<string>{"1"}, this.commandLine),
        new Program.WaitInstruction("2"),
        new Program.CommandInstruction("cmd", new List<string>{"2"}, this.commandLine)
      });

      multiple.Execute(this.process, this.mock.Action, new List<string>());

      this.tick();
      Assert.IsTrue(this.getProcesses().Any(s => s.Contains("ar-wait")));

      this.manager.KillAll("ar-wait");
      this.tick();

      Assert.AreEqual(1, this.getProcesses().Count);
      Assert.AreEqual(2, this.commandCalls.Count);
      Assert.AreEqual("1", this.commandCalls[0]);
      Assert.AreEqual("2", this.commandCalls[1]);
      Assert.IsTrue(this.mock.Called);
    }

    public void WhileInstruction() {
      var whileInstruction = new Program.WhileInstruction(
        new Program.WaitInstruction("5"),
        new List<Program.Instruction> {
           new Program.CommandInstruction("cmd", new List<string>{"1"}, this.commandLine),
           new Program.CommandInstruction("cmd", new List<string>{"2"}, this.commandLine),
           new Program.CommandInstruction("cmd", new List<string>{"3"}, this.commandLine),
        }
      );
      whileInstruction.Execute(this.process, this.mock.Action, new List<string>());

      foreach(int i in Enumerable.Range(0, 4)) {
        this.tick();
      }

      Assert.IsFalse(this.mock.Called);
      Assert.AreEqual(4, this.commandCalls.Count);

      this.tick();

      Assert.IsTrue(this.mock.Called);
      Assert.AreEqual(4, this.commandCalls.Count);
      Assert.AreEqual("1,2,3,1", string.Join(",", this.commandCalls));

      this.tick();

      Assert.AreEqual(4, this.commandCalls.Count);
    }

    public void KillWhileInstruction() {
      var whileInstruction = new Program.WhileInstruction(
        new Program.WaitInstruction("5"),
        new List<Program.Instruction> {
           new Program.CommandInstruction("cmd", new List<string>{"1"}, this.commandLine),
           new Program.CommandInstruction("cmd", new List<string>{"2"}, this.commandLine),
           new Program.CommandInstruction("cmd", new List<string>{"3"}, this.commandLine),
        }
      );
      whileInstruction.Execute(this.process, this.mock.Action, new List<string>());

      this.tick();

      Assert.AreEqual(1, this.commandCalls.Count);

      this.manager.KillAll("ar-wait");

      Assert.IsTrue(this.mock.Called);
      Assert.AreEqual(1, this.commandCalls.Count);
      Assert.AreEqual("1", string.Join(",", this.commandCalls));

      this.tick();

      Assert.AreEqual(1, this.commandCalls.Count);
    }

    public void Placeholders() {
      var multipleInstruction = new Program.MultipleInstruction(
        new List<Program.Instruction> {
          new Program.CommandInstruction("cmd", new List<string>{"$1"}, this.commandLine),
          new Program.CommandInstruction("cmd", new List<string>{"$1", "$4"}, this.commandLine),
          new Program.WaitInstruction("$3"),
        }
      );

      multipleInstruction.Execute(this.process, this.mock.Action, new List<string>{ "arg1", "arg2", "4", "arg4" });

      this.tick();

      Assert.IsFalse(this.mock.Called);
      Assert.AreEqual(1, this.commandCalls.Count);
      Assert.AreEqual("arg1", this.commandCalls[0]);

      this.tick();

      Assert.IsFalse(this.mock.Called);
      Assert.AreEqual(2, this.commandCalls.Count);
      Assert.AreEqual("arg1,arg4", this.commandCalls[1]);

      foreach (int i in Enumerable.Range(0, 3)) {
        this.tick();
      }

      Assert.IsFalse(this.mock.Called);
      Assert.AreEqual(2, this.commandCalls.Count);

      this.tick();

      Assert.IsTrue(this.mock.Called);
    }
  }
}
