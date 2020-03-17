using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IngameScript.MDK {
  class AutoRoutineHandlerTest {
    Program.AutoRoutineHandler arHandler;
    Program.CommandLine commandLine;
    List<string> logs;
    Program.IProcessManager manager;
    public void BeforeEach() {
      this.logs = new List<string>();
      this.manager = Program.Process.CreateManager(null);
      this.commandLine = new Program.CommandLine("test", s => this.logs.Add(s), this.manager);
      this.arHandler = new Program.AutoRoutineHandler(this.commandLine);
    }
    public void StartCommand() {
      string result = "";
      this.commandLine.RegisterCommand(new Program.Command("cmd", (args, logger) => 
        VRage.MyTuple.Create<int, bool, Action<Program.Process>>(1, true, p => {
          result = string.Join(",", args);
        }), ""
      ));

      this.arHandler.AddRoutines(new List<Program.AutoRoutine>{
        new Program.AutoRoutine("test", new List<Program.Instruction> {
          new Program.CommandInstruction("cmd", new List<string>{"arg1", "$1"}, this.commandLine)
        })
      });

      this.commandLine.StartCmd("-ar-execute test placeholder-arg", Program.CommandTrigger.User);

      // On tick to start the autoroutine, on tick to execute the command
      this.manager.Tick();
      this.manager.Tick();

      Assert.AreEqual("arg1,placeholder-arg", result);
    }

    public void ListRoutines() {

      this.arHandler.AddRoutines(new List<Program.AutoRoutine>{
        new Program.AutoRoutine("test-none", new List<Program.Instruction> {
          new Program.WaitInstruction("10")
        }),
        new Program.AutoRoutine("test-one", new List<Program.Instruction> {
          new Program.WaitInstruction("$1")
        }),
        new Program.AutoRoutine("test-several", new List<Program.Instruction> {
          new Program.WaitInstruction("$3")
        })
      });

      this.commandLine.StartCmd("-ar-list", Program.CommandTrigger.User);
      this.manager.Tick();

      Assert.IsTrue(this.logs.Any(s => s.Contains("'test-none': takes no argument")));
      Assert.IsTrue(this.logs.Any(s => s.Contains("'test-one': takes 1 argument")));
      Assert.IsTrue(this.logs.Any(s => s.Contains("'test-several': takes 3 arguments")));
    }
  }
}
