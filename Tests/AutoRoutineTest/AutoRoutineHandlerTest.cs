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
  List<string> _calls;

  [SetUp]
  public void SetUp()
  {
    _logs = [];
    _calls = [];
    _manager = Program.Process.CreateManager();
    _commandLine = new Program.CommandLine("test", _logs.Add, _manager);
    _arHandler = new Program.AutoRoutineHandler(_commandLine);

    _commandLine.RegisterCommand(new Program.Command("cmd", (args, logger) =>
      p =>
      {
        _calls.Add(string.Join(",", args));
      }, ""
    ));
  }

  [Test]
  public void It_Starts_Commands()
  {
    _arHandler.AddRoutines([
        new Program.AutoRoutine("test", [
          new Program.CommandInstruction("cmd",  new Program.ArgumentsWrapper(["arg1", "$1"]), _commandLine)
        ])
      ]);

    _commandLine.StartCmd("ar execute test placeholder-arg", Program.CommandTrigger.User);

    // On tick to start the autoroutine, on tick to execute the command
    _manager.Tick();
    _manager.Tick();

    Assert.That(_calls.Last(), Is.EqualTo("arg1,placeholder-arg"));
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

    _commandLine.StartCmd("ar list", Program.CommandTrigger.User);
    _manager.Tick();

    Assert.That(_logs.Any(s => s.Contains("'test-none': takes no argument")));
    Assert.That(_logs.Any(s => s.Contains("'test-one': takes 1 argument")));
    Assert.That(_logs.Any(s => s.Contains("'test-several': takes 3 arguments")));
  }

  [Test]
  public void It_Allows_Running_Routines()
  {
    // Basic integration test
    var parser = new Program.RoutineParser(_commandLine);
    _arHandler.AddRoutines(parser.Parse(@"
=Test
cmd 1
cmd 2
cmd 3
"));
    var done = false;
    _commandLine.StartCmd("ar execute Test", Program.CommandTrigger.User, _ => done = true);
    _manager.Tick();
    _manager.Tick();
    _manager.Tick();
    _manager.Tick();

    Assert.That(_calls.Count, Is.EqualTo(3));
    Assert.That(done);
  }

  [Test]
  public void It_Correctly_Handles_Echo_Commands()
  {
    // Basic integration test
    var parser = new Program.RoutineParser(_commandLine);
    _arHandler.AddRoutines(parser.Parse(@"
=TestEcho
echo This is a message
"));
    _commandLine.StartCmd("ar execute TestEcho", Program.CommandTrigger.User);
    _manager.Tick();
    _manager.Tick();

    // We'll have to live with the double quotes for now
    Assert.That(_logs.Last(), Is.EqualTo("\"This is a message\""));
  }
}
