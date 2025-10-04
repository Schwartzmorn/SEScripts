namespace ProcessTest;

using FakeItEasy;
using IngameScript;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

[TestFixture]
class ProcessTest
{

  public class MockActionProcess(Action action = null)
  {
    public int CallCount => _calls.Count;
    readonly Action _action = action;
    readonly List<Program.Process> _calls = [];

    public void Action(Program.Process p)
    {
      _calls.Add(p);
      _action?.Invoke();
    }
    public bool Called => CallCount > 0;
    public Program.Process GetCall(int i) => _calls[i];
  };

  public class MockAction
  {
    public readonly Action<Program.Process> Action;

    public readonly Captured<Program.Process> CapturedCalls;

    public bool Called { get => CallCount > 0; }

    public int CallCount { get => CapturedCalls.Values.Count; }

    public Program.ProcessResult GetCallResult(int i) => CapturedCalls.Values[i].Result;

    public MockAction()
    {
      CapturedCalls = A.Captured<Program.Process>();
      Action = A.Fake<Action<Program.Process>>();
      A.CallTo(() => Action(CapturedCalls.Ignored)).DoesNothing();
    }
  }

  private void _tick() => _manager.Tick();

  private Program.IProcessManager _manager;

  [SetUp]
  public void SetUp()
  {
    _manager = Program.Process.CreateManager(null);
    _manager.SetSmart(false);
  }

  [Test]
  public void It_Can_Finish_And_Calls_OnDone()
  {
    var mock = new MockActionProcess();
    var mockDone = new MockAction();
    var p = _manager.Spawn(mock.Action, onDone: mockDone.Action);

    _tick();
    p.Done();

    Assert.That(p.Active, Is.False, "Once done, a process is no longer active");
    Assert.That(mockDone.CallCount, Is.EqualTo(1));

    p.ResetCounter(0);

    Assert.That(p.Active, Is.False, "An inactive process cannot be reactivated");

    p.Done();
    Assert.That(mockDone.CallCount, Is.EqualTo(1));
    Assert.That(mockDone.GetCallResult(0), Is.EqualTo(Program.ProcessResult.OK));
  }

  [Test]
  public void Processes_Can_Be_Killed()
  {
    var mock = new MockActionProcess();
    var mockKill = new MockAction();
    var p = _manager.Spawn(mock.Action, onDone: mockKill.Action);

    Assert.That(p.Active, "A newly created process is active");
    Assert.That(p.Counter, Is.EqualTo(0));

    _tick();

    Assert.That(p.Active, "useOnce is false by default, so a process remains active");
    Assert.That(mock.CallCount, Is.EqualTo(1), "Period is one, so the action is called at each tick");
    Assert.That(mock.GetCall(0), Is.EqualTo(p), "the action is called with itself as argument");

    _tick();

    Assert.That(mock.CallCount, Is.EqualTo(2), "Period is one, so the action is called at each tick");
    Assert.That(mock.GetCall(1), Is.EqualTo(p));

    p.Kill();

    Assert.That(p.Active, Is.False, "Once killed, a process is no longer active");
    Assert.That(mockKill.CallCount, Is.EqualTo(1));
    Assert.That(mockKill.GetCallResult(0), Is.EqualTo(Program.ProcessResult.KILLED));

    _tick();

    Assert.That(mock.CallCount, Is.EqualTo(2), "Once not active, ticking the process has no effect");

    p.Kill();
    Assert.That(mockKill.CallCount, Is.EqualTo(1));
  }

  [Test]
  public void Periodicity_Is_Respected()
  {
    var mock = new MockActionProcess();
    var p = _manager.Spawn(mock.Action, period: 3);

    _tick();
    _tick();

    Assert.That(mock.Called, Is.False, "Not enough ticks, the action has not been called");

    _tick();

    Assert.That(mock.CallCount, Is.EqualTo(1), "The action is called exactly on the tick number 'period'");
    Assert.That(p.Counter, Is.EqualTo(0), "The tick counter is reset after having been called");
    Assert.That(p.Active, "The process is still active");
  }

  [Test]
  public void UseOnce_Processes_Run_Only_Once()
  {
    var mock = new MockActionProcess();
    var mockDone = new MockAction();
    var p = _manager.Spawn(mock.Action, period: 3, useOnce: true, onDone: mockDone.Action);

    _tick();
    _tick();

    Assert.That(mock.Called, Is.False, "Not enough ticks, the action has not been called");
    Assert.That(mockDone.Called, Is.False, "the onDone callback has not yes been called.");

    _tick();

    Assert.That(mock.CallCount, Is.EqualTo(1), "The action is called exactly on the tick number 'period'");
    Assert.That(p.Active, Is.False, "The process is no longer active.");
    Assert.That(mockDone.CallCount, Is.EqualTo(1), "the onDone callback has been called.");
  }

  [Test]
  public void A_UseOnce_Process_Can_Be_Outlived_By_Its_Children()
  {
    var mockDone = new MockAction();
    var p = _manager.Spawn(null, useOnce: true, onDone: mockDone.Action);
    var mockChild = new MockActionProcess();
    var mockDoneChild = new MockAction();
    var child = p.Spawn(mockChild.Action, period: 3, useOnce: true, onDone: mockDoneChild.Action);

    Assert.That(p.Active);
    Assert.That(child.Active);

    _tick();

    Assert.That(p.Active, Is.False);
    Assert.That(child.Active);

    _tick();

    Assert.That(child.Active);
    Assert.That(mockDone.Called, Is.False);
    Assert.That(mockChild.Called, Is.False);

    _tick();

    Assert.That(mockChild.Called);
    Assert.That(mockChild.GetCall(0), Is.EqualTo(child), "The action is called with the child process as argument");
    Assert.That(child.Active, Is.False);
    Assert.That(mockDone.Called);
    Assert.That(mockDone.GetCallResult(0), Is.EqualTo(Program.ProcessResult.OK));
    Assert.That(mockDoneChild.Called);
  }

  [Test]
  public void A_Process_Can_Be_Outlived_By_Its_Children()
  {
    var mockDone = new MockAction();
    var p = _manager.Spawn(null, onDone: mockDone.Action);
    var mockChild = new MockActionProcess();
    var child1 = p.Spawn(mockChild.Action);
    var child2 = p.Spawn(null);

    _tick();

    p.Done();

    Assert.That(mockDone.Called, Is.False);
    Assert.That(p.Active, Is.False);
    Assert.That(child1.Active);
    Assert.That(child2.Active);

    _tick();
    child2.Done();

    Assert.That(mockDone.Called, Is.False);
    Assert.That(mockChild.CallCount, Is.EqualTo(2));
    Assert.That(child1.Active);
    Assert.That(child2.Active, Is.False);

    _tick();
    child1.Done();

    Assert.That(mockDone.Called);
    Assert.That(mockDone.GetCallResult(0), Is.EqualTo(Program.ProcessResult.OK));
    Assert.That(mockChild.CallCount, Is.EqualTo(3));
    Assert.That(child1.Active, Is.False);
  }

  [Test]
  public void A_Process_Can_Outlive_Its_Children()
  {
    var mockDone = new MockAction();
    var p = _manager.Spawn(null, onDone: mockDone.Action);
    var mockDoneChild = new MockAction();
    var child = p.Spawn(null, onDone: mockDoneChild.Action);

    _tick();

    Assert.That(p.Active);
    Assert.That(child.Active);

    _tick();
    child.Done();

    Assert.That(p.Active);
    Assert.That(child.Active, Is.False);
    Assert.That(mockDone.Called, Is.False);
    Assert.That(mockDoneChild.Called);

    _tick();
    p.Done();
    Assert.That(p.Active, Is.False);
    Assert.That(mockDone.Called);
  }

  [Test]
  public void Killing_A_Process_Kills_Its_Children_And_Grandchildren()
  {
    var mockDone = new MockAction();
    var p = _manager.Spawn(null, onDone: mockDone.Action);
    var child1 = p.Spawn(null, onDone: mockDone.Action);
    var child2 = p.Spawn(null, onDone: mockDone.Action);
    var grandChild = child1.Spawn(null, onDone: mockDone.Action);

    p.Kill();

    Assert.That(p.Active, Is.False);
    Assert.That(child1.Active, Is.False);
    Assert.That(child2.Active, Is.False);
    Assert.That(grandChild.Active, Is.False);
    Assert.That(mockDone.CallCount, Is.EqualTo(4));
    foreach (var i in Enumerable.Range(0, 4))
    {
      Assert.That(mockDone.GetCallResult(i), Is.EqualTo(Program.ProcessResult.KILLED));
    }
  }

  [Test]
  public void A_Process_Has_Result_OK_When_Done_Even_If_Its_Children_Were_Killed_While_Alive()
  {
    var mockDone = new MockAction();
    var p = _manager.Spawn(null, onDone: mockDone.Action);
    var child = p.Spawn(null, onDone: mockDone.Action);

    child.Kill();

    Assert.That(p.Active);
    Assert.That(child.Alive, Is.False);
    Assert.That(mockDone.CallCount, Is.EqualTo(1));
    Assert.That(mockDone.GetCallResult(0), Is.EqualTo(Program.ProcessResult.KILLED));

    p.Done();

    Assert.That(p.Alive, Is.False);
    Assert.That(child.Alive, Is.False);
    Assert.That(mockDone.CallCount, Is.EqualTo(2));
    Assert.That(mockDone.GetCallResult(1), Is.EqualTo(Program.ProcessResult.OK));
  }

  [Test]
  public void A_Process_Has_Result_OK_Even_If_Its_Children_Were_Killed_After_It_Was_Done()
  {
    var mockDone = new MockAction();
    var p = _manager.Spawn(null, onDone: mockDone.Action);
    var child = p.Spawn(null, onDone: mockDone.Action);

    p.Done();

    Assert.That(p.Active, Is.False);
    Assert.That(p.Alive);
    Assert.That(child.Active);
    Assert.That(mockDone.Called, Is.False);

    child.Kill();

    Assert.That(p.Alive, Is.False);
    Assert.That(child.Alive, Is.False);
    Assert.That(mockDone.CallCount, Is.EqualTo(2));
    Assert.That(mockDone.GetCallResult(0), Is.EqualTo(Program.ProcessResult.KILLED));
    Assert.That(mockDone.GetCallResult(1), Is.EqualTo(Program.ProcessResult.OK));
  }

  [Test]
  public void ResetCounter_Allows_Delaying_A_Process()
  {
    var p = _manager.Spawn(null, period: 10);

    p.ResetCounter(5);

    Assert.That(p.Counter, Is.EqualTo(5), "Tick Counter is set at the given value");

    p.ResetCounter(20);

    Assert.That(p.Counter, Is.EqualTo(9), "Tick Counter cannot go above the period");

    p.ResetCounter(-1);

    Assert.That(p.Counter, Is.EqualTo(0), "Tick Counter cannot go below 0");
  }

  [Test]
  public void Exceptions_Are_Handled_When_Processing()
  {
    var mock = new MockAction();
    A.CallTo(() => mock.Action(A<Program.Process>.Ignored)).Throws<NotImplementedException>();
    var p = _manager.Spawn(mock.Action);

    _manager.Tick();
    _manager.Tick();

    A.CallTo(() => mock.Action(A<Program.Process>.Ignored)).MustHaveHappenedTwiceExactly();
  }

  [Test]
  public void Exceptions_Are_Handled_When_Killing()
  {
    var mockKill = new MockAction();
    A.CallTo(() => mockKill.Action(A<Program.Process>.Ignored)).Throws<NotImplementedException>();
    var p = _manager.Spawn(null, onDone: mockKill.Action);
    var child1 = p.Spawn(null, onDone: mockKill.Action);
    var child2 = p.Spawn(null);
    var grandChild = child1.Spawn(null);

    p.Kill();

    A.CallTo(() => mockKill.Action(A<Program.Process>.Ignored)).MustHaveHappenedTwiceExactly();
    Assert.That(p.Alive, Is.False);
    Assert.That(child1.Alive, Is.False);
    Assert.That(child2.Alive, Is.False);
    Assert.That(grandChild.Alive, Is.False);
  }

  [Test]
  public void GrandChildren_Can_Be_Killed_Too()
  {
    var mock = new MockAction();
    var p = _manager.Spawn(null, onDone: mock.Action);
    var child = p.Spawn(null, onDone: mock.Action);
    var grandChild = child.Spawn(null, onDone: mock.Action);

    p.Done();

    Assert.That(p.Alive);

    child.Fail();

    Assert.That(p.Alive);
    Assert.That(child.Alive);
    Assert.That(mock.Called, Is.False);

    grandChild.Kill();

    Assert.That(p.Alive, Is.False);
    Assert.That(child.Alive, Is.False);
    Assert.That(grandChild.Alive, Is.False);
    Assert.That(mock.CallCount, Is.EqualTo(3));
    Assert.That(mock.GetCallResult(0), Is.EqualTo(Program.ProcessResult.KILLED));
    Assert.That(mock.GetCallResult(1), Is.EqualTo(Program.ProcessResult.KO));
    Assert.That(mock.GetCallResult(2), Is.EqualTo(Program.ProcessResult.OK));
  }

  [Test]
  public void Self_Killing_In_OnDone_Does_Not_Trigger_Stack_Overflow()
  {
    var mockKill = new MockAction();
    A.CallTo(() => mockKill.Action(A<Program.Process>.Ignored)).Invokes((Program.Process p) => p.Kill());
    Program.Process process = _manager.Spawn(null, onDone: mockKill.Action);

    process.Kill();

    Assert.That(process.Alive, Is.False);
  }

  [Test]
  public void A_Process_Can_Kill_Itself()
  {
    Program.Process process = _manager.Spawn(p => p.Kill());

    _tick();

    Assert.That(process.Alive, Is.False);
  }

  [Test]
  public void A_Process_Can_Finish_Itself()
  {
    Program.Process process = _manager.Spawn(p => p.Done());

    _tick();

    Assert.That(process.Alive, Is.False);
  }
}
