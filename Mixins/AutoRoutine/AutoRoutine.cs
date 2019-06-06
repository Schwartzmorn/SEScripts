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
  public class JobProvider {
    protected Action<string> Callback;
    public void StartJob(Action<List<string>> job, List<string> args, Action<string> callback) {
      CancelCallback();
      Callback = callback;
      job(args);
    }
    public void StartJob(Action<string> job, string arg, Action<string> callback) {
      CancelCallback();
      Callback = callback;
      job(arg);
    }
    public void StartJob(Action job, Action<string> callback) {
      CancelCallback();
      Callback = callback;
      job();
    }
    protected virtual void StopCallback(string s) {
      Callback?.Invoke(s);
      Callback = null;
    }
    protected void CancelCallback() => StopCallback(ARHandler.ABORT);
  }
  public abstract class Job {
    public abstract Process Spawn(CmdLine cmd, Action<string> cb, List<string> subst);
  }
  public class CmdJob: Job {
    class CmdProcess: Process {
      public CmdProcess(string name): base(name) { }
      public override void Kill() {

      }
    }
    static readonly System.Text.RegularExpressions.Regex RX = new System.Text.RegularExpressions.Regex("\\${(\\d+)}");
    readonly string _cmd;
    public CmdJob(string cmd) {
      _cmd = cmd;
    }
    public override void pExec(CmdLine cmd, Action<string> cb, List<string> subst) {
      string c = RX.Replace(_cmd, m => {
        int i = int.Parse(m.Groups[1].Value);
        if (i >= subst.Count)
          throw new InvalidOperationException($"Auto routine: need at least {i} arguments");
        return subst[i];
      });
      Schedule(new ScheduledAction(() => cmd.StartCmd(c, cb, CmdTrigger.Cmd), 1, true, "job"));
      return new Process()
    }
  }
  public class AlwaysJob: Job {
    protected override void pExec(CmdLine cmd, Action<string> cb, List<string> subst) { }
  }
  public class WaitJob: Job {
    ScheduledAction _callback;
    readonly int _wait;
    public WaitJob(int w) { _wait = w; }
    protected override void pExec(CmdLine cmd, Action<string> cb, List<string> subst) {
      Log($"Waiting {_wait} cycles");
      _callback = new ScheduledAction(() => cb("Wait done"), _wait, true, "wait");
      Schedule(_callback);
    }
    protected override void pCancel() {
      _callback?.Dispose();
      _callback = null;
    }
  }
  public class RepeatJob: Job {
    readonly Job _c;
    readonly Routine _loop;
    bool _running = false;
    public RepeatJob(Job job, Routine loop) {
      _c = job;
      _loop = loop;
    }
    protected override void pExec(CmdLine cmd, Action<string> cb, List<string> subst) {
      Log("Start looping");
      _running = true;
      _c.Exec(cmd, s => _break(s, cb), subst);
      _continue(cmd, "start", subst);
    }
    void _continue(CmdLine cmd, string s, List<string> subst) {
      if (_running)
        _loop.Exec(cmd, r => _continue(cmd, r, subst), subst);
    }
    void _break(string s, Action<string> cb) {
      cb(s);
      Cancel();
    }
    protected override void pCancel() {
      _running = false;
      _loop.Cancel();
    }
  }
  public class Routine: Job {
    int _index;
    readonly List<Job> _jobs = new List<Job>();
    public void AddJob(Job job) => _jobs.Add(job);
    protected override void pExec(CmdLine cmd, Action<string> cb, List<string> subst) {
      _index = -1;
      _jobDone(cmd, "start", cb, subst);
    }
    protected override void pCancel() {
      if (_index >= 0 && _index < _jobs.Count)
        _jobs[_index].Cancel();
      _index = -1;
    }
    void _jobDone(CmdLine cmd, string s, Action<string> cb, List<string> subst) {
      if (_index >= 0) Log($"Job {_index} complete with message '{s}'");
      ++_index;
      if(_index < _jobs.Count)
        _jobs[_index].Exec(cmd, r => _jobDone(cmd, r, cb, subst), subst);
      else {
        cb(s);
        _index = -1;
      }
    }
  }
  public class AutoRoutine: Routine {
    public readonly string Name;
    public AutoRoutine(string nm) {
      Name = nm;
    }
  }
}
}
