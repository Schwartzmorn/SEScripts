namespace AutoRoutineTest;

using System;
using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using IngameScript;
using NUnit.Framework;

[TestFixture]
public class InstructionsTest
{
  private Action<Program.Process> _spyCallback;
  private List<string> _commandCalls;
  private Program.CommandLine _commandLine;
  private Program.IProcessManager _manager;
  private Program.Process _process;

  private void _tick() => _manager.Tick();

  private List<string> _getProcesses()
  {
    var processes = new List<string>();
    _manager.Log(processes.Add);
    return processes;
  }

  private Action<Program.Process> _command(Program.ArgumentsWrapper args, Action<string> logger)
  {
    return p => { _commandCalls.Add(string.Join(",", args)); };
  }

  [SetUp]
  public void SetUp()
  {
    _spyCallback = A.Fake<Action<Program.Process>>();
    _commandCalls = [];
    _manager = Program.Process.CreateManager(s => System.Diagnostics.Debug.WriteLine(s));
    _commandLine = new Program.CommandLine("test", null, _manager);
    _commandLine.RegisterCommand(new Program.Command("cmd", _command, "", requiredTrigger: Program.CommandTrigger.Cmd));
    _process = _manager.Spawn(null, "test");
  }

  [Test]
  public void CommandInstruction_Calls_The_Command()
  {
    var cmd = new Program.CommandInstruction("cmd", new Program.ArgumentsWrapper(["arg1", "arg2"]), _commandLine);

    cmd.Execute(_process, _spyCallback, new Program.ArgumentsWrapper([]));
    List<string> processes = _getProcesses();

    Assert.That(processes.Count, Is.EqualTo(2));
    Assert.That(processes.Any(s => s.Contains("cmd")));
    A.CallTo(() => _spyCallback(A<Program.Process>.Ignored)).MustNotHaveHappened();

    _tick();
    processes = _getProcesses();

    Assert.That(processes.Count, Is.EqualTo(1));
    Assert.That(_commandCalls.Count, Is.EqualTo(1));
    Assert.That(_commandCalls[0], Is.EqualTo("arg1,arg2"));
    A.CallTo(() => _spyCallback(A<Program.Process>.Ignored)).MustHaveHappened();
    Assert.That(processes.Any(s => s.Contains("cmd")), Is.False);
  }

  [Test]
  public void CommandInstruction_Can_Be_Killed()
  {
    // We check that the callback is executed correctly when we kill the process
    var cmd = new Program.CommandInstruction("cmd", new Program.ArgumentsWrapper([]), _commandLine);

    cmd.Execute(_process, _spyCallback, new Program.ArgumentsWrapper([]));

    A.CallTo(() => _spyCallback(A<Program.Process>.Ignored)).MustNotHaveHappened();

    _manager.KillAll("cmd");

    A.CallTo(() => _spyCallback(A<Program.Process>.Ignored)).MustHaveHappened();

    _tick();

    A.CallTo(() => _spyCallback(A<Program.Process>.Ignored)).MustHaveHappenedOnceExactly();
  }

  [Test]
  public void WaitInstruction_Runs_For_The_Given_Number_Of_Cycles()
  {
    var wait = new Program.WaitInstruction("4");

    wait.Execute(_process, _spyCallback, new Program.ArgumentsWrapper([]));
    List<string> processes = _getProcesses();

    Assert.That(processes.Count, Is.EqualTo(2));
    Assert.That(processes.Any(s => s.Contains("ar-wait")));

    foreach (int i in Enumerable.Range(0, 3))
    {
      _tick();
    }
    processes = _getProcesses();

    Assert.That(processes.Count, Is.EqualTo(2));
    A.CallTo(() => _spyCallback(A<Program.Process>.Ignored)).MustNotHaveHappened();
    Assert.That(processes.Any(s => s.Contains("ar-wait")));

    _tick();
    processes = _getProcesses();

    Assert.That(processes.Count, Is.EqualTo(1));
    A.CallTo(() => _spyCallback(A<Program.Process>.Ignored)).MustHaveHappened();
    Assert.That(processes.Any(s => s.Contains("ar-wait")), Is.False);
  }

  [Test]
  public void WaitInstruction_Can_Be_Killed()
  {
    var wait = new Program.WaitInstruction("1");

    wait.Execute(_process, _spyCallback, new Program.ArgumentsWrapper([]));

    A.CallTo(() => _spyCallback(A<Program.Process>.Ignored)).MustNotHaveHappened();

    _manager.KillAll("ar-wait");

    A.CallTo(() => _spyCallback(A<Program.Process>.Ignored)).MustHaveHappened();

    _tick();

    A.CallTo(() => _spyCallback(A<Program.Process>.Ignored)).MustHaveHappenedOnceExactly();
  }

  [Test]
  public void ForeverInstructions_Runs_Until_Killed()
  {
    var always = new Program.ForeverInstruction();

    always.Execute(_process, _spyCallback, new Program.ArgumentsWrapper([]));
    List<string> processes = _getProcesses();

    Assert.That(processes.Count, Is.EqualTo(2));
    Assert.That(processes.Any(s => s.Contains("ar-forever")));

    foreach (int i in Enumerable.Range(0, 200))
    {
      _tick();
    }
    processes = _getProcesses();

    Assert.That(processes.Count, Is.EqualTo(2));
    Assert.That(processes.Any(s => s.Contains("ar-forever")));
    A.CallTo(() => _spyCallback(A<Program.Process>.Ignored)).MustNotHaveHappened();

    _manager.KillAll("ar-forever");
    A.CallTo(() => _spyCallback(A<Program.Process>.Ignored)).MustHaveHappenedOnceExactly();
  }

  [Test]
  public void MultipleInstruction_Runs_Instructions_Sequentially()
  {
    var multiple = new Program.AutoRoutine("Test", [
      new Program.CommandInstruction("cmd",  new Program.ArgumentsWrapper(["1"]), _commandLine),
        new Program.WaitInstruction("2"),
        new Program.CommandInstruction("cmd",  new Program.ArgumentsWrapper(["2"]), _commandLine)
    ]);

    multiple.Execute(_process, _spyCallback, new Program.ArgumentsWrapper([]));

    _tick();

    Assert.That(_getProcesses().Count, Is.EqualTo(3));
    Assert.That(_getProcesses().Any(s => s.Contains("ar-wait")));
    Assert.That(_commandCalls.Count, Is.EqualTo(1));
    Assert.That(_commandCalls[0], Is.EqualTo("1"));

    _tick();
    _tick();

    A.CallTo(() => _spyCallback(A<Program.Process>.Ignored)).MustNotHaveHappened();

    _tick();

    Assert.That(_getProcesses().Count, Is.EqualTo(1));
    Assert.That(_commandCalls.Count, Is.EqualTo(2));
    Assert.That(_commandCalls[1], Is.EqualTo("2"));
    A.CallTo(() => _spyCallback(A<Program.Process>.Ignored)).MustHaveHappenedOnceExactly();
  }

  [Test]
  public void MultipleInstruction_Continues_If_One_Instruction_Is_Killed()
  {
    var multiple = new Program.AutoRoutine("test", [
      new Program.CommandInstruction("cmd", new Program.ArgumentsWrapper(["1"]), _commandLine),
      new Program.WaitInstruction("2"),
      new Program.CommandInstruction("cmd", new Program.ArgumentsWrapper(["2"]), _commandLine)
    ]);

    multiple.Execute(_process, _spyCallback, new Program.ArgumentsWrapper([]));

    _tick();

    Assert.That(_getProcesses().Any(s => s.Contains("ar-wait")));

    _manager.KillAll("ar-wait");
    _tick();

    Assert.That(_getProcesses().Count, Is.EqualTo(1));
    Assert.That(_commandCalls.Count, Is.EqualTo(2));
    Assert.That(_commandCalls[0], Is.EqualTo("1"));
    Assert.That(_commandCalls[1], Is.EqualTo("2"));
    A.CallTo(() => _spyCallback(A<Program.Process>.Ignored)).MustHaveHappenedOnceExactly();
  }

  [Test]
  public void WhileInstruction_Loops_Until_Done()
  {
    var whileInstruction = new Program.WhileInstruction(
      new Program.WaitInstruction("5"),
      [
        new Program.CommandInstruction("cmd",  new Program.ArgumentsWrapper(["1"]), _commandLine),
        new Program.CommandInstruction("cmd",  new Program.ArgumentsWrapper(["2"]), _commandLine),
        new Program.CommandInstruction("cmd",  new Program.ArgumentsWrapper(["3"]), _commandLine),
      ]
    );
    whileInstruction.Execute(_process, _spyCallback, new Program.ArgumentsWrapper([]));

    foreach (int i in Enumerable.Range(0, 4))
    {
      _tick();
    }

    A.CallTo(() => _spyCallback(A<Program.Process>.Ignored)).MustNotHaveHappened();
    Assert.That(_commandCalls.Count, Is.EqualTo(4));

    _tick();

    A.CallTo(() => _spyCallback(A<Program.Process>.Ignored)).MustHaveHappened();
    Assert.That(_commandCalls.Count, Is.EqualTo(4));
    Assert.That(string.Join(",", _commandCalls), Is.EqualTo("1,2,3,1"));

    _tick();

    Assert.That(_commandCalls.Count, Is.EqualTo(4));
  }

  [Test]
  public void WhileInstructions_Can_Be_Killed()
  {
    var whileInstruction = new Program.WhileInstruction(
      new Program.WaitInstruction("5"),
      [
          new Program.CommandInstruction("cmd",  new Program.ArgumentsWrapper(["1"]), _commandLine),
          new Program.CommandInstruction("cmd",  new Program.ArgumentsWrapper(["2"]), _commandLine),
          new Program.CommandInstruction("cmd",  new Program.ArgumentsWrapper(["3"]), _commandLine),
      ]
    );
    whileInstruction.Execute(_process, _spyCallback, new Program.ArgumentsWrapper([]));

    _tick();

    Assert.That(_commandCalls.Count, Is.EqualTo(1));

    _manager.KillAll("ar-wait");

    A.CallTo(() => _spyCallback(A<Program.Process>.Ignored)).MustHaveHappened();
    Assert.That(_commandCalls.Count, Is.EqualTo(1));
    Assert.That(string.Join(",", _commandCalls), Is.EqualTo("1"));

    _tick();

    Assert.That(_commandCalls.Count, Is.EqualTo(1));
  }

  [Test]
  public void Placholders_Can_Be_Used_In_Instructions()
  {
    var multipleInstruction = new Program.AutoRoutine("test",
      [
        new Program.CommandInstruction("cmd",  new Program.ArgumentsWrapper(["$1"]), _commandLine),
        new Program.CommandInstruction("cmd",  new Program.ArgumentsWrapper(["$1", "$4"]), _commandLine),
        new Program.WaitInstruction("$3"),
      ]
    );
    multipleInstruction.Execute(_process, _spyCallback, new Program.ArgumentsWrapper(["arg1", "arg2", "4", "arg4"]));

    _tick();

    A.CallTo(() => _spyCallback(A<Program.Process>.Ignored)).MustNotHaveHappened();
    Assert.That(_commandCalls.Count, Is.EqualTo(1));
    Assert.That(_commandCalls[0], Is.EqualTo("arg1"));

    _tick();

    A.CallTo(() => _spyCallback(A<Program.Process>.Ignored)).MustNotHaveHappened();
    Assert.That(_commandCalls.Count, Is.EqualTo(2));
    Assert.That(_commandCalls[1], Is.EqualTo("arg1,arg4"));

    foreach (int i in Enumerable.Range(0, 3))
    {
      _tick();
    }

    A.CallTo(() => _spyCallback(A<Program.Process>.Ignored)).MustNotHaveHappened();
    Assert.That(_commandCalls.Count, Is.EqualTo(2));

    _tick();

    A.CallTo(() => _spyCallback(A<Program.Process>.Ignored)).MustHaveHappened();
  }

  [Test]
  public void WhileInstruction_Kills_Its_Children_But_Not_Its_successor()
  {
    // This terrible test helped diagnose an issue that caused WhileInstructions to fire multiple times the next instructions
    var routine = new Program.AutoRoutine("test routine",
      [
        new Program.WhileInstruction(
          new Program.WaitInstruction("5"),
          [
              new Program.CommandInstruction("cmd",  new Program.ArgumentsWrapper(["1"]), _commandLine),
              new Program.ForeverInstruction(),
              new Program.CommandInstruction("cmd",  new Program.ArgumentsWrapper(["2"]), _commandLine),
          ]
        ),
        new Program.WaitInstruction("4"),
        new Program.CommandInstruction("cmd",  new Program.ArgumentsWrapper(["3"]), _commandLine),
      ]
    );
    routine.Execute(_process, _spyCallback, new Program.ArgumentsWrapper([]));

    foreach (int _i in Enumerable.Range(0, 2))
    {
      _tick();
    }

    Assert.That(_commandCalls.Count, Is.EqualTo(1));
    Assert.That(_commandCalls[0], Is.EqualTo("1"));

    foreach (int _i in Enumerable.Range(0, 5))
    {
      _tick();
    }

    Assert.That(_commandCalls.Count, Is.EqualTo(1));

    foreach (int _i in Enumerable.Range(0, 3))
    {
      _tick();
    }

    Assert.That(_commandCalls.Count, Is.EqualTo(2));

    foreach (int _i in Enumerable.Range(0, 10))
    {
      _tick();
    }

    Assert.That(_commandCalls.Count, Is.EqualTo(2));

  }
}
