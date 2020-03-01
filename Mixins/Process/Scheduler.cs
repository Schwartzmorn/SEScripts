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
      public static readonly IScheduler SCHEDULER = new Scheduler();
      
      private class Scheduler: IImplScheduler {
        readonly List<Process> processes = new List<Process>();
        readonly List<Action<MyIni>> onSave = new List<Action<MyIni>>();
        readonly List<Process> toAdd = new List<Process>();
        bool smartSchedule = true;

        public Scheduler() { }

        public void AddOnSave(Action<MyIni> a) => this.onSave.Add(a);

        public void Kill(int pid) => this.processes.Concat(this.toAdd).FirstOrDefault(p => p.ID == pid)?.Kill();

        public void KillAll() {
          foreach (Process p in this.processes.Concat(this.toAdd)) {
            p.Kill();
          }
        }

        public void KillAll(string n) {
          foreach(Process p in this.processes.Concat(this.toAdd).Where(p => p.Name == n)) {
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

        public Process Spawn(Action<Process> action, int period = 1, bool useOnce = false, string name = null, Action onDone = null, Action onInterrupt = null) {
          return new Process(action, null, this, period, useOnce, name, onDone, onInterrupt);
        }

        public void Tick() {
          if (this.smartSchedule) {
            foreach (var p in this.toAdd.Where(t => t.Active)) {
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
              Log($"Failed on {p.Name}: {e.Message}");
            }
          }
          this.processes.RemoveAll(p => !p.Active);
        }
      }
    }
  }
}
