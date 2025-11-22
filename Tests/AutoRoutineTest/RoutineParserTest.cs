using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IngameScript;
using NUnit.Framework;

namespace AutoRoutineTest;

[TestFixture]
public class RoutineParserTest
{
  private Program.CommandLine _commandLine;
  private Program.IProcessManager _manager;
  private Program.RoutineParser _parser;

  private void _checkError(string msg, string faultyString)
  {
    var error = Assert.Throws<InvalidOperationException>(() => _parser.Parse(faultyString));
    Assert.That(error.Message, Is.EqualTo(msg));
  }

  private object _get<T>(T i, string s)
  {
    FieldInfo f = typeof(T).GetField(s, BindingFlags.NonPublic | BindingFlags.Instance);
    return f.GetValue(i);
  }

  private string _getTime(Program.WaitInstruction i) => _get(i, "time") as string;

  private string _getCmd(Program.CommandInstruction i) => _get(i, "command") as string;

  private List<string> _getArgs(Program.CommandInstruction i) => _get(i, "args") as List<string>;

  private List<Program.Instruction> _getInstructions(Program.MultipleInstruction i) => _get(i, "instructions") as List<Program.Instruction>;

  private Program.SingleInstruction _getCondition(Program.WhileInstruction i) => _get(i, "condition") as Program.SingleInstruction;

  [SetUp]
  public void SetUp()
  {
    _manager = Program.Process.CreateManager();
    _commandLine = new Program.CommandLine("test", null, _manager);
    _parser = new Program.RoutineParser(_commandLine);
  }

  public void It_Can_Parse_A_Valid_Routine()
  {
    List<Program.AutoRoutine> routines = _parser.Parse(@"
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
    Assert.That(routines.Count, Is.EqualTo(2));
    Program.AutoRoutine routine1 = routines[0];
    Program.AutoRoutine routine2 = routines[1];

    Assert.That(routine1.Name, Is.EqualTo("Test routine"));
    List<Program.Instruction> instructions = _getInstructions(routine1);
    Assert.That(instructions.Count, Is.EqualTo(3));
    Assert.That(_getTime(instructions[0] as Program.WaitInstruction), Is.EqualTo("10"));
    Assert.That(_getCmd(instructions[1] as Program.CommandInstruction), Is.EqualTo("cmd"));
    Assert.That(new List<string> { "arg" }.SequenceEqual(_getArgs(instructions[1] as Program.CommandInstruction)));
    Assert.That(_getCondition(instructions[2] as Program.WhileInstruction), Is.InstanceOf<Program.ForeverInstruction>());
    instructions = _getInstructions(instructions[2] as Program.MultipleInstruction);
    Assert.That(instructions.Count, Is.EqualTo(3));
    Assert.That(instructions[0], Is.InstanceOf<Program.CommandInstruction>());
    Assert.That(_getCmd(instructions[1] as Program.CommandInstruction), Is.EqualTo("cmd"));
    Assert.That(new List<string> { "arg3", "test test" }.SequenceEqual(_getArgs(instructions[1] as Program.CommandInstruction)));
    Assert.That(_getTime(instructions[2] as Program.WaitInstruction), Is.EqualTo("100"));
    Assert.That(routine1.ArgsCount(), Is.EqualTo(0));

    Assert.That(routine2.Name, Is.EqualTo("Test routine 2"));
    Assert.That(routine2.ArgsCount(), Is.EqualTo(0));
  }

  [Test]
  public void It_Returns_Meaningful_Errors()
  {
    _checkError("Unexpected start of new auto routine at line 3", "=test\nwhile forever\n=test2");
    _checkError("Unexpected instruction '-cmd test' at line 1 outside of a routine", "-cmd test");
    _checkError("Could not parse instruction 'something' at line 2", "=test\nwhile something");
  }

  [Test]
  public void It_Can_Parse_Placeholders()
  {
    List<Program.AutoRoutine> routines = _parser.Parse(@"
; line to be ignored
= Test routine
wait $2
-cmd $1
while wait $3
  -cmd $1 $4
end
");
    Assert.That(routines.Count, Is.EqualTo(1));
    Program.AutoRoutine routine = routines[0];

    Assert.That(routine.ArgsCount(), Is.EqualTo(4));

  }

  [Test]
  public void It_Can_Parse_The_Mining_Routine()
  {
    List<Program.AutoRoutine> routines = _parser.Parse(@"
= Mine
while -inv-while under 1
  -arm-drill $auto-low
  -arm-drill $top
end
-arm-recall $auto-low
");
    Assert.That(routines.Count, Is.EqualTo(2));
    Program.AutoRoutine routine = routines[0];

    Assert.That(routine.ArgsCount(), Is.EqualTo(0));
  }
}
