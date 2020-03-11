using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript {
  partial class Program {
    public partial class Process {
      private interface ISchedulerManager : IProcessManager {
        /// <summary>Not to be called directly as processes are automatically scheduled</summary>
        /// <param name="p">Process to schedule</param>
        void Schedule(Process p);
      }

      private class ProcessManager: ISchedulerManager {
        /// <summary>Returns an enumerable on all the processes present at the time of the call</summary>
        IEnumerable<Process> AllProcesses {
          get {
            // we don't want to process any process added during the foreach
            int count = this.toAdd.Count;
            foreach (Process p in this.processes) {
              yield return p;
            }
            // can't use a foreach on a list that might change anyway
            for (int i = 0; i < count; ++i) {
              yield return this.toAdd[i];
            }
          }
        }
        readonly Action<string> logger;
        readonly List<Process> processes = new List<Process>();
        readonly List<Action<MyIni>> onSave = new List<Action<MyIni>>();
        readonly List<Process> toAdd = new List<Process>();
        bool smartSchedule = true;

        public ProcessManager(Action<string> logger = null) {
          this.logger = logger;
        }

        public void AddOnSave(Action<MyIni> a) => this.onSave.Add(a);

        public void Kill(int pid) => this.AllProcesses.FirstOrDefault(p => p.ID == pid)?.Kill();

        public void KillAll() {
          foreach (Process p in this.AllProcesses) {
            p.Kill();
          }
        }

        public void KillAll(string n) {
          foreach(Process p in this.AllProcesses.Where(p => p.Name == n)) {
            p.Kill();
          }
        }

        public void Save(Action<string> onSave) {
          var ini = new MyIni();
          this.onSave.ForEach(a => a(ini));
          onSave(ini.ToString());
        }

        public void Schedule(Process p) => this.toAdd.Add(p);

        public void SetSmart(bool smart) => this.smartSchedule = smart;

        public Process Spawn(Action<Process> action, string name = null, Action<Process> onDone = null, int period = 1, bool useOnce = false) {
          return new Process(action, this.logger, name, onDone, null, period, this, useOnce);
        }

        public void Tick() {
          if (this.smartSchedule) {
            foreach (var p in this.toAdd.Where(t => t.Alive)) {
              if (p.Period > 1 && !p.UseOnce) {
                var cs = new HashSet<int>(this.processes.Where(b => b.Period == p.Period).Select(b => b.Counter));
                p.ResetCounter(Enumerable.Range(0, p.Period).FirstOrDefault(t => !cs.Contains(t)));
              }
              this.processes.Add(p);
            }
          } else {
            this.processes.AddRange(this.toAdd);
          }
          this.toAdd.Clear();
          foreach (var p in this.processes) {
            try {
              p.tick();
            } catch (Exception e) {
              this.logger?.Invoke($"Failed on {p.Name}: {e.Message}");
            }
          }
          this.processes.RemoveAll(p => !p.Alive);
        }

        public void Log(Action<string> log) {
          foreach (var p in this.AllProcesses.Where(p => p.Alive && p.parent == null)) {
            p.ToString(0, log);
          }
        }
      }
    }
  }
}
