namespace AutoRoutineTest;

using System;
using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using IngameScript;
using NUnit.Framework;

[TestFixture]
public class AutoRoutineHandlerTest
{
  Program.AutoRoutineHandler _arHandler;
  Program.CommandLine _commandLine;
  List<string> _logs;
  Program.IProcessManager _manager;

  [SetUp]
  public void SetUp()
  {
    _logs = [];
    _manager = Program.Process.CreateManager();
    _commandLine = new Program.CommandLine("test", _logs.Add, _manager);
    _arHandler = new Program.AutoRoutineHandler(_commandLine);
  }

  [Test]
  public void It_Starts_Commands()
  {
    string result = "";
    _commandLine.RegisterCommand(new Program.Command("cmd", (args, logger) =>
      VRage.MyTuple.Create<int, bool, Action<Program.Process>>(1, true, p =>
      {
        result = string.Join(",", args);
      }), ""
    ));

    _arHandler.AddRoutines([
        new Program.AutoRoutine("test", [
          new Program.CommandInstruction("cmd", ["arg1", "$1"], _commandLine)
        ])
      ]);

    _commandLine.StartCmd("-ar-execute test placeholder-arg", Program.CommandTrigger.User);

    // On tick to start the autoroutine, on tick to execute the command
    _manager.Tick();
    _manager.Tick();

    Assert.That(result, Is.EqualTo("arg1,placeholder-arg"));
  }

  [Test]
  public void It_Lists_Routines()
  {
    _arHandler.AddRoutines([
      new("test-none", [
          new Program.WaitInstruction("10")
        ]),
        new("test-one", [
          new Program.WaitInstruction("$1")
        ]),
        new("test-several", [
          new Program.WaitInstruction("$3")
        ])
    ]);

    _commandLine.StartCmd("-ar-list", Program.CommandTrigger.User);
    _manager.Tick();

    Assert.That(_logs.Any(s => s.Contains("'test-none': takes no argument")));
    Assert.That(_logs.Any(s => s.Contains("'test-one': takes 1 argument")));
    Assert.That(_logs.Any(s => s.Contains("'test-several': takes 3 arguments")));
  }
}
