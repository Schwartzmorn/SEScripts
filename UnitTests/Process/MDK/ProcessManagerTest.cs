using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IngameScript.MDK {
  class ProcessManagerTest {

    private Program.IProcessManager manager;

    public void BeforeEach() => this.manager = Program.Process.CreateManager(null);

    public void Tick() {
      var mock = new ProcessTest.MockActionProcess();
      var p = this.manager.Spawn(mock.Action, period: 5);

      for (int i = 0; i < 4; ++i) {
        this.manager.Tick();
      }

      Assert.AreEqual(4, p.Counter);
      Assert.IsFalse(mock.Called);

      this.manager.Tick();

      Assert.AreEqual(0, p.Counter);
      Assert.IsTrue(mock.Called);
    }

    public void SmartSchedule() {
      var processes = Enumerable.Range(0, 5).Select(i => this.manager.Spawn(null, name: "Repeat", period: 10)).ToList();
      var pOnce = this.manager.Spawn(null, name: "Once", period: 10, useOnce: true);
      this.manager.Tick();
      var counters = new HashSet<int>(processes.Select(p => p.Counter));
      Assert.AreEqual(5, counters.Count, "Starting counters are spread out if possible");
      Assert.AreEqual(1, pOnce.Counter, "When UseOnce, the starting counter is always 0 (but it already ticked once)");
    }

    public void Kill() {
      var mock = new Program.MockAction();
      var killedByName = Enumerable.Range(0, 2).Select(i => this.manager.Spawn(null, name: "KillMeByName", onDone: mock.Action)).ToList();
      var killedById = this.manager.Spawn(null, onDone: mock.Action);
      var killedInTheEnd = Enumerable.Range(0, 2).Select(i => this.manager.Spawn(null, onDone: mock.Action)).ToList();
      this.manager.Tick();

      this.manager.KillAll("KillMeByName");

      Assert.AreEqual(2, mock.CallCount);
      foreach (var p in killedByName) {
        Assert.IsFalse(p.Alive);
      }

      this.manager.Kill(killedById.ID);

      Assert.AreEqual(3, mock.CallCount);
      Assert.IsFalse(killedById.Alive);

      this.manager.KillAll();

      Assert.AreEqual(5, mock.CallCount);
      foreach (var p in killedInTheEnd) {
        Assert.IsFalse(p.Alive);
      }
      foreach (int i in Enumerable.Range(0, 5)) {
        Assert.AreEqual(Program.ProcessResult.KILLED, (mock.GetCallResult(i)));
      }
    }

    // Same test than Kill, but when the scheduler has not yet ticked
    public void KillNotTicked() {
      var mock = new Program.MockAction();
      var killedByName = Enumerable.Range(0, 2).Select(i => this.manager.Spawn(null, name: "KillMeByName", onDone: mock.Action)).ToList();
      var killedById = this.manager.Spawn(null, onDone: mock.Action);
      var killedInTheEnd = Enumerable.Range(0, 2).Select(i => this.manager.Spawn(null, onDone: mock.Action)).ToList();

      this.manager.KillAll("KillMeByName");

      Assert.AreEqual(2, mock.CallCount);
      foreach (var p in killedByName) {
        Assert.IsFalse(p.Alive);
      }

      this.manager.Kill(killedById.ID);

      Assert.AreEqual(3, mock.CallCount);
      Assert.IsFalse(killedById.Alive);

      this.manager.KillAll();

      Assert.AreEqual(5, mock.CallCount);
      foreach (var p in killedInTheEnd) {
        Assert.IsFalse(p.Alive);
      }
      foreach (int i in Enumerable.Range(0, 5)) {
        Assert.AreEqual(Program.ProcessResult.KILLED, mock.GetCallResult(i));
      }
    }

    public void TickWithFailure() {
      var mockFail = new ProcessTest.MockActionProcess(() => { throw new NotImplementedException(); });
      var mock = new ProcessTest.MockActionProcess();
      this.manager.Spawn(mockFail.Action);
      this.manager.Spawn(mock.Action);

      this.manager.Tick();

      Assert.IsTrue(mockFail.Called);
      Assert.IsTrue(mock.Called);
    }

    public void KillAllSpawning() {
      // we check that processes can spawn new processes when being killed, and that we ignore those processes
      var mock = new Program.MockAction(p => this.manager.Spawn(null, "killme"));
      Program.Process process1 = this.manager.Spawn(null, "killme", mock.Action);
      Program.Process process2 = this.manager.Spawn(null, "killme", mock.Action);

      this.manager.KillAll("killme");

      Assert.IsFalse(process2.Alive);
      Assert.IsFalse(process1.Alive);
      Assert.AreEqual(2, mock.CallCount);

      var processes = new List<string>();
      this.manager.Log(s => processes.Add(s));

      Assert.AreEqual(2, processes.Count(s => s.Contains("killme")));

      // Although ignored at first because spawned during the KillAll call, a subsequent call to KillAll will kill them
      this.manager.KillAll("killme");
      processes.Clear();
      this.manager.Log(s => processes.Add(s));

      Assert.AreEqual(0, processes.Count(s => s.Contains("killme")));
    }
  }
}
