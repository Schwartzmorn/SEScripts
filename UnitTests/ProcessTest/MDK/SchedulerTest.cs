using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IngameScript.MDK {
  class SchedulerTest {
    public void BeforeEach() {
      Program.SCHEDULER.KillAll();
      Program.SCHEDULER.SetSmart(true);
      Program.SCHEDULER.Tick();
    }

    public void Tick() {
      var mock = new ProcessTest.MockActionProcess();
      var p = Program.SCHEDULER.Spawn(mock.Action, period: 5);

      for (int i = 0; i < 4; ++i) {
        Program.SCHEDULER.Tick();
      }

      Assert.AreEqual(4, p.Counter);
      Assert.IsFalse(mock.Called);

      Program.SCHEDULER.Tick();

      Assert.AreEqual(0, p.Counter);
      Assert.IsTrue(mock.Called);
    }

    public void SmartSchedule() {
      var processes = Enumerable.Range(0, 5).Select(i => Program.SCHEDULER.Spawn(null, name: "Repeat", period: 10)).ToList();
      var pOnce = Program.SCHEDULER.Spawn(null, name: "Once", period: 10, useOnce: true);
      Program.SCHEDULER.Tick();
      var counters = new HashSet<int>(processes.Select(p => p.Counter));
      Assert.AreEqual(5, counters.Count, "Starting counters are spread out if possible");
      Assert.AreEqual(1, pOnce.Counter, "When UseOnce, the starting counter is always 0 (but it already ticked once)");
    }

    public void Kill() {
      var mock = new ProcessTest.MockAction();
      var killedByName = Enumerable.Range(0, 2).Select(i => Program.SCHEDULER.Spawn(null, name: "KillMeByName", onInterrupt: mock.Action)).ToList();
      var killedById = Program.SCHEDULER.Spawn(null, onInterrupt: mock.Action);
      var killedInTheEnd = Enumerable.Range(0, 2).Select(i => Program.SCHEDULER.Spawn(null, onInterrupt: mock.Action)).ToList();
      Program.SCHEDULER.Tick();

      Program.SCHEDULER.KillAll("KillMeByName");

      Assert.AreEqual(2, mock.CallCount);
      foreach (var p in killedByName) {
        Assert.IsFalse(p.Alive);
      }

      Program.SCHEDULER.Kill(killedById.ID);

      Assert.AreEqual(3, mock.CallCount);
      Assert.IsFalse(killedById.Alive);

      Program.SCHEDULER.KillAll();

      Assert.AreEqual(5, mock.CallCount);
      foreach (var p in killedInTheEnd) {
        Assert.IsFalse(p.Alive);
      }
    }

    // Same test than Kill, but when the scheduler has not yet ticked
    public void KillNotTicked() {
      var mock = new ProcessTest.MockAction();
      var killedByName = Enumerable.Range(0, 2).Select(i => Program.SCHEDULER.Spawn(null, name: "KillMeByName", onInterrupt: mock.Action)).ToList();
      var killedById = Program.SCHEDULER.Spawn(null, onInterrupt: mock.Action);
      var killedInTheEnd = Enumerable.Range(0, 2).Select(i => Program.SCHEDULER.Spawn(null, onInterrupt: mock.Action)).ToList();

      Program.SCHEDULER.KillAll("KillMeByName");

      Assert.AreEqual(2, mock.CallCount);
      foreach (var p in killedByName) {
        Assert.IsFalse(p.Alive);
      }

      Program.SCHEDULER.Kill(killedById.ID);

      Assert.AreEqual(3, mock.CallCount);
      Assert.IsFalse(killedById.Alive);

      Program.SCHEDULER.KillAll();

      Assert.AreEqual(5, mock.CallCount);
      foreach (var p in killedInTheEnd) {
        Assert.IsFalse(p.Alive);
      }
    }

    public void TickWithFailure() {
      var mockFail = new ProcessTest.MockActionProcess(() => { throw new NotImplementedException(); });
      var mock = new ProcessTest.MockActionProcess();
      Program.SCHEDULER.Spawn(mockFail.Action);
      Program.SCHEDULER.Spawn(mock.Action);

      Program.SCHEDULER.Tick();

      Assert.IsTrue(mockFail.Called);
      Assert.IsTrue(mock.Called);
    }
  }
}
