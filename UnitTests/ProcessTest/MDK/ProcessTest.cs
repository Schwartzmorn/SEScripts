using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IngameScript.MDK {
  class ProcessTest {

    public class MockAction {
      public int CallCount { get; private set; } = 0;
      readonly Action action;
      public MockAction(Action action = null) {
        this.action = action;
      }
      public void Action() {
        ++this.CallCount;
        this.action?.Invoke();
      }
      public bool Called => this.CallCount > 0;
    };

    public class MockActionProcess {
      public int CallCount => this.calls.Count;
      readonly Action action;
      readonly List<Program.Process> calls = new List<Program.Process>();
      public MockActionProcess(Action action = null) {
        this.action = action;
      }
      public void Action(Program.Process p) {
        this.calls.Add(p);
        this.action?.Invoke();
      }
      public Program.Process GetCall(int i) => this.calls[i];
      public bool Called => this.CallCount > 0;
    };

    private void tick() => Program.SCHEDULER.Tick();

    public void BeforeEach() {
      Program.SCHEDULER.KillAll();
      Program.SCHEDULER.SetSmart(false);
      Program.SCHEDULER.Tick();
    }

    public void ProcessBasicDone() {
      var mock = new MockActionProcess();
      var mockDone = new MockAction();
      var p = Program.SCHEDULER.Spawn(mock.Action, onDone: mockDone.Action);

      this.tick();
      p.Done();

      Assert.IsFalse(p.Active, "Once done, a process is no longer active");
      Assert.AreEqual(1, mockDone.CallCount);

      p.ResetCounter(0);

      Assert.IsFalse(p.Active, "An inactive process cannot be reactivated");

      p.Done();
      Assert.AreEqual(1, mockDone.CallCount);
    }

    public void ProcessBasicKill() {
      var mock = new MockActionProcess();
      var mockKill = new MockAction();
      var p = Program.SCHEDULER.Spawn(mock.Action, onInterrupt: mockKill.Action);

      Assert.IsTrue(p.Active, "A newly created process is active");
      Assert.AreEqual(0, p.Counter);

      this.tick();

      Assert.IsTrue(p.Active, "useOnce is false by default, so a process remains active");
      Assert.AreEqual(1, mock.CallCount, "Period is one, so the action is called at each tick");
      Assert.AreEqual(p, mock.GetCall(0), "the action is called with itself as argument");

      this.tick();

      Assert.AreEqual(2, mock.CallCount, "Period is one, so the action is called at each tick");
      Assert.AreEqual(p, mock.GetCall(1));

      p.Kill();

      Assert.IsFalse(p.Active, "Once killed, a process is no longer active");
      Assert.AreEqual(1, mockKill.CallCount);

      this.tick();

      Assert.AreEqual(2, mock.CallCount, "Once not active, ticking the process has no effect");

      p.Kill();
      Assert.AreEqual(1, mockKill.CallCount);
    }

    public void Period() {
      var mock = new MockActionProcess();
      var p = Program.SCHEDULER.Spawn(mock.Action, period: 3);

      this.tick();
      this.tick();

      Assert.IsFalse(mock.Called, "Not enough ticks, the action has not been called");

      this.tick();

      Assert.AreEqual(1, mock.CallCount, "The action is called exactly on the tick number 'period'");
      Assert.AreEqual(0, p.Counter, "The tick counter is reset after having been called");
      Assert.IsTrue(p.Active, "The process is still active");
    }

    public void UseOnce() {
      var mock = new MockActionProcess();
      var mockDone = new MockAction();
      var p = Program.SCHEDULER.Spawn(mock.Action, period: 3, useOnce: true, onDone: mockDone.Action);

      this.tick();
      this.tick();

      Assert.IsFalse(mock.Called, "Not enough ticks, the action has not been called");
      Assert.IsFalse(mockDone.Called, "the onDone callback has not yes been called.");

      this.tick();

      Assert.AreEqual(1, mock.CallCount, "The action is called exactly on the tick number 'period'");
      Assert.IsFalse(p.Active, "The process is no longer active.");
      Assert.AreEqual(1, mockDone.CallCount, "the onDone callback has been called.");
    }

    public void DoneBeforeChildrenUseOnce() {
      var mockDone = new MockAction();
      var p = Program.SCHEDULER.Spawn(null, useOnce: true, onDone: mockDone.Action);
      var mockChild = new MockActionProcess();
      var mockDoneChild = new MockAction();
      var child = p.Spawn(mockChild.Action, period: 3, useOnce: true, onDone: mockDoneChild.Action);

      Assert.IsTrue(p.Active);
      Assert.IsTrue(child.Active);

      this.tick();

      Assert.IsFalse(p.Active);
      Assert.IsTrue(child.Active);

      this.tick();

      Assert.IsTrue(child.Active);
      Assert.IsFalse(mockDone.Called);
      Assert.IsFalse(mockChild.Called);

      this.tick();

      Assert.IsTrue(mockChild.Called);
      Assert.AreEqual(child, mockChild.GetCall(0), "The action is called with the child process as argument");
      Assert.IsFalse(child.Active);
      Assert.IsTrue(mockDone.Called);
      Assert.IsTrue(mockDoneChild.Called);
    }

    public void DoneBeforeChildren() {
      var mockDone = new MockAction();
      var p = Program.SCHEDULER.Spawn(null, onDone: mockDone.Action);
      var mockChild = new MockActionProcess();
      var child1 = p.Spawn(mockChild.Action);
      var child2 = p.Spawn(null);

      this.tick();

      p.Done();

      Assert.IsFalse(mockDone.Called);
      Assert.IsFalse(p.Active);
      Assert.IsTrue(child1.Active);
      Assert.IsTrue(child2.Active);

      this.tick();
      child2.Done();

      Assert.IsFalse(mockDone.Called);
      Assert.AreEqual(2, mockChild.CallCount);
      Assert.IsTrue(child1.Active);
      Assert.IsFalse(child2.Active);

      this.tick();
      child1.Done();

      Assert.IsTrue(mockDone.Called);
      Assert.AreEqual(3, mockChild.CallCount);
      Assert.IsFalse(child1.Active);
    }

    public void DoneAfterChildren() {
      var mockDone = new MockAction();
      var p = Program.SCHEDULER.Spawn(null, onDone: mockDone.Action);
      var mockDoneChild = new MockAction();
      var child = p.Spawn(null, onDone: mockDoneChild.Action);

      this.tick();

      Assert.IsTrue(p.Active);
      Assert.IsTrue(child.Active);

      this.tick();
      child.Done();

      Assert.IsTrue(p.Active);
      Assert.IsFalse(child.Active);
      Assert.IsFalse(mockDone.Called);
      Assert.IsTrue(mockDoneChild.Called);

      this.tick();
      p.Done();
      Assert.IsFalse(p.Active);
      Assert.IsTrue(mockDone.Called);
    }

    public void KillChildren() {
      var mockKill = new MockAction();
      var mockDone = new MockAction();
      var p = Program.SCHEDULER.Spawn(null, onDone: mockDone.Action, onInterrupt: mockKill.Action);
      var child1 = p.Spawn(null, onDone: mockDone.Action, onInterrupt: mockKill.Action);
      var child2 = p.Spawn(null, onDone: mockDone.Action, onInterrupt: mockKill.Action);
      var grandChild = child1.Spawn(null, onDone: mockDone.Action, onInterrupt: mockKill.Action);

      p.Kill();

      Assert.IsFalse(p.Active);
      Assert.IsFalse(child1.Active);
      Assert.IsFalse(child2.Active);
      Assert.IsFalse(grandChild.Active);
      Assert.AreEqual(4, mockKill.CallCount);
      Assert.IsFalse(mockDone.Called);
    }

    public void DoneAfterChildrenKilled() {
      var mockKill = new MockAction();
      var mockDone = new MockAction();
      var p = Program.SCHEDULER.Spawn(null, onDone: mockDone.Action, onInterrupt: mockKill.Action);
      var child = p.Spawn(null, onDone: mockDone.Action, onInterrupt: mockKill.Action);

      child.Kill();

      Assert.IsTrue(p.Active);
      Assert.IsFalse(child.Alive);
      Assert.AreEqual(1, mockKill.CallCount);
      Assert.IsFalse(mockDone.Called);

      p.Done();

      Assert.IsFalse(p.Alive);
      Assert.IsFalse(child.Alive);
      Assert.AreEqual(1, mockKill.CallCount);
      Assert.AreEqual(1, mockDone.CallCount);
    }

    public void DoneBeforeChildrenKilled() {
      var mockKill = new MockAction();
      var mockDone = new MockAction();
      var p = Program.SCHEDULER.Spawn(null, onDone: mockDone.Action, onInterrupt: mockKill.Action);
      var child = p.Spawn(null, onDone: mockDone.Action, onInterrupt: mockKill.Action);

      p.Done();

      Assert.IsFalse(p.Active);
      Assert.IsTrue(p.Alive);
      Assert.IsTrue(child.Active);
      Assert.IsFalse(mockDone.Called);
      Assert.IsFalse(mockKill.Called);

      child.Kill();

      Assert.IsFalse(p.Alive);
      Assert.IsFalse(child.Alive);
      Assert.AreEqual(1, mockKill.CallCount);
      Assert.AreEqual(1, mockDone.CallCount);
    }

    public void ResetCounter() {
      var p = Program.SCHEDULER.Spawn(null, period: 10);

      p.ResetCounter(5);

      Assert.AreEqual(5, p.Counter, "Tick Counter is set at the given value");

      p.ResetCounter(20);

      Assert.AreEqual(9, p.Counter, "Tick Counter cannot go above the period");

      p.ResetCounter(-1);

      Assert.AreEqual(0, p.Counter, "Tick Counter cannot go below 0");
    }

    public void KillWithChildInError() {
      var mockKill = new MockAction(() => { throw new NotImplementedException(); } );
      var p = Program.SCHEDULER.Spawn(null, onInterrupt: mockKill.Action);
      var child1 = p.Spawn(null, onInterrupt: mockKill.Action);
      var child2 = p.Spawn(null);
      var grandChild = child1.Spawn(null);

      p.Kill();

      Assert.AreEqual(2, mockKill.CallCount);
      Assert.IsFalse(p.Alive);
      Assert.IsFalse(child1.Alive);
      Assert.IsFalse(child2.Alive);
      Assert.IsFalse(grandChild.Alive);
    }
  }
}
