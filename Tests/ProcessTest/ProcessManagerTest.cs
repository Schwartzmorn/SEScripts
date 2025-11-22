namespace ProcessTest;

using System;
using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using IngameScript;
using NUnit.Framework;

[TestFixture]
class ProcessManagerTest
{

  private Program.IProcessManager _manager;

  [SetUp]
  public void SetUp() => _manager = Program.Process.CreateManager();

  [Test]
  public void It_Respects_Periodicity()
  {
    var mock = new ProcessTest.MockActionProcess();
    var p = _manager.Spawn(mock.Action, period: 5);

    for (int i = 0; i < 4; ++i)
    {
      _manager.Tick();
    }

    Assert.That(p.Counter, Is.EqualTo(4));
    Assert.That(mock.Called, Is.False);

    _manager.Tick();

    Assert.That(p.Counter, Is.EqualTo(0));
    Assert.That(mock.Called);
  }

  [Test]
  public void It_Tries_To_Schedule_Smartly()
  {
    var processes = Enumerable.Range(0, 5).Select(i => _manager.Spawn(null, name: "Repeat", period: 10)).ToList();
    var pOnce = _manager.Spawn(null, name: "Once", period: 10, useOnce: true);
    _manager.Tick();
    var counters = new HashSet<int>(processes.Select(p => p.Counter));
    Assert.That(counters.Count, Is.EqualTo(5), "Starting counters are spread out if possible");
    Assert.That(pOnce.Counter, Is.EqualTo(1), "When UseOnce, the starting counter is always 0 (but it already ticked once)");
  }

  [Test]
  public void It_Allows_Killing_Processes()
  {
    var capturedProcesses = A.Captured<Program.Process>();
    var mock = A.Fake<Action<Program.Process>>();
    A.CallTo(() => mock(capturedProcesses.Ignored)).DoesNothing();
    var killedByName = Enumerable.Range(0, 2).Select(i => _manager.Spawn(null, name: "KillMeByName", onDone: mock)).ToList();
    var killedById = _manager.Spawn(null, onDone: mock);
    var killedInTheEnd = Enumerable.Range(0, 2).Select(i => _manager.Spawn(null, onDone: mock)).ToList();
    _manager.Tick();

    _manager.KillAll("KillMeByName");


    A.CallTo(() => mock(A<Program.Process>.Ignored)).MustHaveHappenedTwiceExactly();
    foreach (var p in killedByName)
    {
      Assert.That(p.Alive, Is.False);
    }

    _manager.Kill(killedById.ID);

    A.CallTo(() => mock(A<Program.Process>.Ignored)).MustHaveHappened(3, Times.Exactly);
    Assert.That(killedById.Alive, Is.False);

    _manager.KillAll();

    A.CallTo(() => mock(A<Program.Process>.Ignored)).MustHaveHappened(5, Times.Exactly);
    foreach (var p in killedInTheEnd)
    {
      Assert.That(p.Alive, Is.False);
    }
    foreach (int i in Enumerable.Range(0, 5))
    {
      Assert.That(capturedProcesses.Values[i].Result, Is.EqualTo(Program.ProcessResult.KILLED));
    }
  }

  // Same test than Kill, but when the scheduler has not yet ticked
  [Test]
  public void Killing_Processing_Works_Before_Ticking()
  {
    var capturedProcesses = A.Captured<Program.Process>();
    var mock = A.Fake<Action<Program.Process>>();
    A.CallTo(() => mock(capturedProcesses.Ignored)).DoesNothing();
    var killedByName = Enumerable.Range(0, 2).Select(i => _manager.Spawn(null, name: "KillMeByName", onDone: mock)).ToList();
    var killedById = _manager.Spawn(null, onDone: mock);
    var killedInTheEnd = Enumerable.Range(0, 2).Select(i => _manager.Spawn(null, onDone: mock)).ToList();

    _manager.KillAll("KillMeByName");

    A.CallTo(() => mock(A<Program.Process>.Ignored)).MustHaveHappenedTwiceExactly();
    foreach (var p in killedByName)
    {
      Assert.That(p.Alive, Is.False);
    }

    _manager.Kill(killedById.ID);

    A.CallTo(() => mock(A<Program.Process>.Ignored)).MustHaveHappened(3, Times.Exactly);
    Assert.That(killedById.Alive, Is.False);

    _manager.KillAll();

    A.CallTo(() => mock(A<Program.Process>.Ignored)).MustHaveHappened(5, Times.Exactly);
    foreach (var p in killedInTheEnd)
    {
      Assert.That(p.Alive, Is.False);
    }
    foreach (int i in Enumerable.Range(0, 5))
    {
      Assert.That(capturedProcesses.Values[i].Result, Is.EqualTo(Program.ProcessResult.KILLED));
    }
  }

  [Test]
  public void It_Handles_Process_Failures()
  {
    var mockFail = new ProcessTest.MockActionProcess(() => { throw new NotImplementedException(); });
    var mock = new ProcessTest.MockActionProcess();
    _manager.Spawn(mockFail.Action);
    _manager.Spawn(mock.Action);

    _manager.Tick();

    Assert.That(mockFail.Called);
    Assert.That(mock.Called);
  }

  [Test]
  public void Killing_Leaves_Spawning_Children_Alive()
  {
    var mock = A.Fake<Action<Program.Process>>();
    A.CallTo(() => mock(A<Program.Process>.Ignored)).Invokes(() => _manager.Spawn(null, "killme"));

    // we check that processes can spawn new processes when being killed, and that we ignore those processes
    Program.Process process1 = _manager.Spawn(null, "killme", mock);
    Program.Process process2 = _manager.Spawn(null, "killme", mock);

    _manager.KillAll("killme");

    Assert.That(process2.Alive, Is.False);
    Assert.That(process1.Alive, Is.False);
    A.CallTo(() => mock(A<Program.Process>.Ignored)).MustHaveHappenedTwiceExactly();

    var processes = new List<string>();
    _manager.Log(processes.Add);

    Assert.That(processes.Count(s => s.Contains("killme")), Is.EqualTo(2));

    // Although ignored at first because spawned during the KillAll call, a subsequent call to KillAll will kill them
    _manager.KillAll("killme");
    processes.Clear();
    _manager.Log(s => processes.Add(s));

    Assert.That(processes.Count(s => s.Contains("killme")), Is.EqualTo(0));
  }
}
