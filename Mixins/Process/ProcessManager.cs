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

namespace IngameScript
{
  partial class Program
  {
    public partial class Process
    {
      private interface ISchedulerManager : IProcessManager
      {
        /// <summary>Not to be called directly as processes are automatically scheduled</summary>
        /// <param name="p">Process to schedule</param>
        void Schedule(Process p);
      }

      private class ProcessManager : ISchedulerManager
      {
        /// <summary>Returns an enumerable on all the processes present at the time of the call</summary>
        IEnumerable<Process> AllProcesses
        {
          get
          {
            // we don't want to process any process added during the foreach
            int count = _toAdd.Count;
            foreach (Process p in _processes)
            {
              yield return p;
            }
            // can't use a foreach on a list that might change anyway
            for (int i = 0; i < count; ++i)
            {
              yield return _toAdd[i];
            }
          }
        }
        Action<string> _logger;
        readonly List<Process> _processes = new List<Process>();
        readonly List<Action<MyIni>> _onSave = new List<Action<MyIni>>();
        readonly List<Process> _toAdd = new List<Process>();
        bool _smartSchedule = true;

        public ProcessManager(Action<string> logger = null)
        {
          _logger = logger;
        }

        public void SetLogger(Action<string> logger) => _logger = logger;

        public void AddOnSave(Action<MyIni> a) => _onSave.Add(a);

        public void Kill(int pid) => AllProcesses.FirstOrDefault(p => p.ID == pid)?.Kill();

        public void KillAll()
        {
          foreach (Process p in AllProcesses)
          {
            p.Kill();
          }
        }

        public void KillAll(string n)
        {
          foreach (Process p in AllProcesses.Where(p => p.Name == n))
          {
            p.Kill();
          }
        }

        public void Save(Action<string> onSave) => Save(onSave, new MyIni());

        public void Save(Action<string> onSave, MyIni ini)
        {
          _onSave.ForEach(a => a(ini));
          onSave(ini.ToString());
        }

        public void Schedule(Process p) => _toAdd.Add(p);

        public void SetSmart(bool smart) => _smartSchedule = smart;

        public Process Spawn(Action<Process> action, string name = null, Action<Process> onDone = null, int period = 1, bool useOnce = false)
        {
          return new Process(action, _logger, name, onDone, null, period, this, useOnce);
        }

        public void Tick()
        {
          if (_smartSchedule)
          {
            foreach (Process p in _toAdd.Where(t => t.Alive))
            {
              if (p.Period > 1 && !p.UseOnce)
              {
                var cs = new HashSet<int>(_processes.Where(b => b.Period == p.Period).Select(b => b.Counter));
                p.ResetCounter(Enumerable.Range(0, p.Period).FirstOrDefault(t => !cs.Contains(t)));
              }
              _processes.Add(p);
            }
          }
          else
          {
            _processes.AddRange(_toAdd);
          }
          _toAdd.Clear();
          foreach (Process p in _processes)
          {
            try
            {
              p._tick();
            }
            catch (Exception e)
            {
              _logger?.Invoke($"Failed on {p.Name}: {e.Message}");
              _logger?.Invoke($"{e.StackTrace}");
            }
          }
          _processes.RemoveAll(p => !p.Alive);
        }

        public void Log(Action<string> log)
        {
          foreach (Process p in AllProcesses.Where(p => p.Alive && p.Parent == null))
          {
            p.ToString(0, log);
          }
        }
      }
    }
  }
}
