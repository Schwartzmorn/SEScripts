﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IngameScript.MDK {
  class ProcessTest {

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
      public bool Called => this.CallCount > 0;
      public Program.Process GetCall(int i) => this.calls[i];
    };

    private void tick() => this.manager.Tick();

    private Program.IProcessManager manager;

    public void BeforeEach() {
      this.manager = Program.Process.CreateManager(null);
      this.manager.SetSmart(false);
    }

    public void ProcessBasicDone() {
      var mock = new MockActionProcess();
      var mockDone = new Program.MockAction();
      var p = this.manager.Spawn(mock.Action, onDone: mockDone.Action);

      this.tick();
      p.Done();

      Assert.IsFalse(p.Active, "Once done, a process is no longer active");
      Assert.AreEqual(1, mockDone.CallCount);

      p.ResetCounter(0);

      Assert.IsFalse(p.Active, "An inactive process cannot be reactivated");

      p.Done();
      Assert.AreEqual(1, mockDone.CallCount);
      Assert.AreEqual(Program.ProcessResult.OK, mockDone.GetCallResult(0));
    }

    public void ProcessBasicKill() {
      var mock = new MockActionProcess();
      var mockKill = new Program.MockAction();
      var p = this.manager.Spawn(mock.Action, onDone: mockKill.Action);

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
      Assert.AreEqual(Program.ProcessResult.KILLED, mockKill.GetCallResult(0));

      this.tick();

      Assert.AreEqual(2, mock.CallCount, "Once not active, ticking the process has no effect");

      p.Kill();
      Assert.AreEqual(1, mockKill.CallCount);
    }

    public void Period() {
      var mock = new MockActionProcess();
      var p = this.manager.Spawn(mock.Action, period: 3);

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
      var mockDone = new Program.MockAction();
      var p = this.manager.Spawn(mock.Action, period: 3, useOnce: true, onDone: mockDone.Action);

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
      var mockDone = new Program.MockAction();
      var p = this.manager.Spawn(null, useOnce: true, onDone: mockDone.Action);
      var mockChild = new MockActionProcess();
      var mockDoneChild = new Program.MockAction();
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
      Assert.AreEqual(Program.ProcessResult.OK, mockDone.GetCallResult(0));
      Assert.IsTrue(mockDoneChild.Called);
    }

    public void DoneBeforeChildren() {
      var mockDone = new Program.MockAction();
      var p = this.manager.Spawn(null, onDone: mockDone.Action);
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
      Assert.AreEqual(Program.ProcessResult.OK, mockDone.GetCallResult(0));
      Assert.AreEqual(3, mockChild.CallCount);
      Assert.IsFalse(child1.Active);
    }

    public void DoneAfterChildren() {
      var mockDone = new Program.MockAction();
      var p = this.manager.Spawn(null, onDone: mockDone.Action);
      var mockDoneChild = new Program.MockAction();
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
      var mockDone = new Program.MockAction();
      var p = this.manager.Spawn(null, onDone: mockDone.Action);
      var child1 = p.Spawn(null, onDone: mockDone.Action);
      var child2 = p.Spawn(null, onDone: mockDone.Action);
      var grandChild = child1.Spawn(null, onDone: mockDone.Action);

      p.Kill();

      Assert.IsFalse(p.Active);
      Assert.IsFalse(child1.Active);
      Assert.IsFalse(child2.Active);
      Assert.IsFalse(grandChild.Active);
      Assert.AreEqual(4, mockDone.CallCount);
      foreach(var i in Enumerable.Range(0, 4)) {
        Assert.AreEqual(Program.ProcessResult.KILLED, mockDone.GetCallResult(i));
      }
    }

    public void DoneAfterChildrenKilled() {
      var mockDone = new Program.MockAction();
      var p = this.manager.Spawn(null, onDone: mockDone.Action);
      var child = p.Spawn(null, onDone: mockDone.Action);

      child.Kill();

      Assert.IsTrue(p.Active);
      Assert.IsFalse(child.Alive);
      Assert.AreEqual(1, mockDone.CallCount);
      Assert.AreEqual(Program.ProcessResult.KILLED, mockDone.GetCallResult(0));

      p.Done();

      Assert.IsFalse(p.Alive);
      Assert.IsFalse(child.Alive);
      Assert.AreEqual(2, mockDone.CallCount);
      Assert.AreEqual(Program.ProcessResult.OK, mockDone.GetCallResult(1));
    }

    public void DoneBeforeChildrenKilled() {
      var mockDone = new Program.MockAction();
      var p = this.manager.Spawn(null, onDone: mockDone.Action);
      var child = p.Spawn(null, onDone: mockDone.Action);

      p.Done();

      Assert.IsFalse(p.Active);
      Assert.IsTrue(p.Alive);
      Assert.IsTrue(child.Active);
      Assert.IsFalse(mockDone.Called);

      child.Kill();

      Assert.IsFalse(p.Alive);
      Assert.IsFalse(child.Alive);
      Assert.AreEqual(2, mockDone.CallCount);
      Assert.AreEqual(Program.ProcessResult.KILLED, mockDone.GetCallResult(0));
      Assert.AreEqual(Program.ProcessResult.OK, mockDone.GetCallResult(1));
    }

    public void ResetCounter() {
      var p = this.manager.Spawn(null, period: 10);

      p.ResetCounter(5);

      Assert.AreEqual(5, p.Counter, "Tick Counter is set at the given value");

      p.ResetCounter(20);

      Assert.AreEqual(9, p.Counter, "Tick Counter cannot go above the period");

      p.ResetCounter(-1);

      Assert.AreEqual(0, p.Counter, "Tick Counter cannot go below 0");
    }

    public void KillWithChildInError() {
      var mockKill = new Program.MockAction((i) => { throw new NotImplementedException(); } );
      var p = this.manager.Spawn(null, onDone: mockKill.Action);
      var child1 = p.Spawn(null, onDone: mockKill.Action);
      var child2 = p.Spawn(null);
      var grandChild = child1.Spawn(null);

      p.Kill();

      Assert.AreEqual(2, mockKill.CallCount);
      Assert.IsFalse(p.Alive);
      Assert.IsFalse(child1.Alive);
      Assert.IsFalse(child2.Alive);
      Assert.IsFalse(grandChild.Alive);
    }

    public void KillGrandChild() {
      var mock = new Program.MockAction();
      var p = this.manager.Spawn(null, onDone: mock.Action);
      var child = p.Spawn(null, onDone: mock.Action);
      var grandChild = child.Spawn(null, onDone: mock.Action);

      p.Done();

      Assert.IsTrue(p.Alive);

      child.Fail();

      Assert.IsTrue(p.Alive);
      Assert.IsTrue(child.Alive);
      Assert.IsFalse(mock.Called);

      grandChild.Kill();

      Assert.IsFalse(p.Alive);
      Assert.IsFalse(child.Alive);
      Assert.IsFalse(grandChild.Alive);
      Assert.AreEqual(3, mock.CallCount);
      Assert.AreEqual(Program.ProcessResult.KILLED, mock.GetCallResult(0));
      Assert.AreEqual(Program.ProcessResult.KO, mock.GetCallResult(1));
      Assert.AreEqual(Program.ProcessResult.OK, mock.GetCallResult(2));
    }

    public void SelfKillingInCallbackProcess() {
      var mock = new Program.MockAction(p => p.Kill());
      Program.Process process = this.manager.Spawn(null, onDone: mock.Action);

      process.Kill();

      Assert.IsFalse(process.Alive);
    }

    public void SelfKillingProcess() {
      Program.Process process = this.manager.Spawn(p => p.Kill());

      this.tick();

      Assert.IsFalse(process.Alive);
    }

    public void SelfTerminatingProcess() {
      Program.Process process = this.manager.Spawn(p => p.Done());

      this.tick();

      Assert.IsFalse(process.Alive);
    }
  }
}
