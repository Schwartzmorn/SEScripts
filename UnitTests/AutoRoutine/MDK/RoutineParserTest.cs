using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IngameScript.MDK {
  class RoutineParserTest {
    Program.CommandLine commandLine;
    Program.IProcessManager manager;
    Program.RoutineParser parser;
    private void checkError(string msg, string faultyString) {
      try {
        this.parser.Parse(faultyString);
        Assert.Fail("Expected the parsing to have thrown");
      } catch (InvalidOperationException e) {
        Assert.AreEqual(msg, e.Message);
      }
    }
    object get<T>(T i, string s) {
      FieldInfo f = typeof(T).GetField(s, BindingFlags.NonPublic | BindingFlags.Instance);
      return f.GetValue(i);
    }
    string getTime(Program.WaitInstruction i) => this.get(i, "time") as string;
    string getCmd(Program.CommandInstruction i) => this.get(i, "command") as string;
    List<string> getArgs(Program.CommandInstruction i) => this.get(i, "args") as List<string>;
    List<Program.Instruction> getInstructions(Program.MultipleInstruction i) => this.get(i, "instructions") as List<Program.Instruction>;
    Program.SingleInstruction getCondition(Program.WhileInstruction i) => this.get(i, "condition") as Program.SingleInstruction;
    public void BeforeEach() {
      this.manager = Program.Process.CreateManager(null);
      this.commandLine = new Program.CommandLine("test", null, this.manager);
      this.parser = new Program.RoutineParser(this.commandLine);
    }

    public void Parse() {
      List<Program.AutoRoutine> routines = this.parser.Parse(@"
; line to be ignored
= Test routine
wait 10
-cmd arg
while forever
  -cmd arg2
  -cmd arg3  ""test test""  
  wait    100
end
=Test routine 2  
while -cmd arg4
  -cmd arg5
  while -cmd arg6
    -cmd arg7
  end
end
");
      Assert.AreEqual(2, routines.Count);
      Program.AutoRoutine routine1 = routines[0];
      Program.AutoRoutine routine2 = routines[1];

      Assert.AreEqual("Test routine", routine1.Name);
      List<Program.Instruction> instructions = this.getInstructions(routine1);
      Assert.AreEqual(3, instructions.Count);
      Assert.AreEqual("10", this.getTime(instructions[0] as Program.WaitInstruction));
      Assert.AreEqual("cmd", this.getCmd(instructions[1] as Program.CommandInstruction));
      Assert.IsTrue(new List<string>{"arg"}.SequenceEqual(this.getArgs(instructions[1] as Program.CommandInstruction)));
      Assert.IsInstanceOfType(this.getCondition(instructions[2] as Program.WhileInstruction), typeof(Program.ForeverInstruction));
      instructions = this.getInstructions(instructions[2] as Program.MultipleInstruction);
      Assert.AreEqual(3, instructions.Count);
      Assert.IsInstanceOfType(instructions[0], typeof(Program.CommandInstruction));
      Assert.AreEqual("cmd", this.getCmd(instructions[1] as Program.CommandInstruction));
      Assert.IsTrue(new List<string> { "arg3", "test test" }.SequenceEqual(this.getArgs(instructions[1] as Program.CommandInstruction)));
      Assert.AreEqual("100", this.getTime(instructions[2] as Program.WaitInstruction));
      Assert.AreEqual(0, routine1.ArgsCount());

      Assert.AreEqual("Test routine 2", routine2.Name);
      Assert.AreEqual(0, routine2.ArgsCount());
    }

    public void ParseErrors() {
      this.checkError("Unexpected start of new auto routine at line 3", "=test\nwhile forever\n=test2");
      this.checkError("Unexpected instruction '-cmd test' at line 1 outside of a routine", "-cmd test");
      this.checkError("Could not parse instruction 'something' at line 2", "=test\nwhile something");
    }

    public void ParsePlaceholders() {
      List<Program.AutoRoutine> routines = this.parser.Parse(@"
; line to be ignored
= Test routine
wait $2
-cmd $1
while wait $3
  -cmd $1 $4
end
");
      Assert.AreEqual(1, routines.Count);
      Program.AutoRoutine routine = routines[0];

      Assert.AreEqual(4, routine.ArgsCount());
    }
  }
}
