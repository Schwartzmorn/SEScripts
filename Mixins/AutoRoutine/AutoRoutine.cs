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
      public void StartJob(Action<List<string>> job,
                           List<string> args,
                           Action<string> callback) {
        this.CancelCallback();
        this.Callback = callback;
        job(args);
      }
      public void StartJob(Action<string> job, string arg, Action<string> callback) {
        this.CancelCallback();
        this.Callback = callback;
        job(arg);
      }
      public void StartJob(Action job, Action<string> callback) {
        this.CancelCallback();
        this.Callback = callback;
        job();
      }
      protected virtual void StopCallback(string s) {
        this.Callback?.Invoke(s);
        this.Callback = null;
      }
      protected void CancelCallback() => this.StopCallback(ARHandler.ABORT);
    }
    public abstract class Job {
      public abstract Process Spawn(CmdLine cmd, Action<string> cb, List<string> subst);
    }
    public class CmdJob : Job {
      class CmdProcess : Process {
        public CmdProcess(string name) : base(name) { }
        public override void Kill() {

        }
      }
      static readonly System.Text.RegularExpressions.Regex RX = new System.Text.RegularExpressions.Regex("\\${(\\d+)}");
      readonly string cmd;
      public CmdJob(string cmd) {
        this.cmd = cmd;
      }
      public override void pExec(CmdLine cmd, Action<string> cb, List<string> subst) {
        string c = RX.Replace(this.cmd, m => {
          int i = int.Parse(m.Groups[1].Value);
          if (i >= subst.Count)
            throw new InvalidOperationException($"Auto routine: need at least {i} arguments");
          return subst[i];
        });
        Schedule(new ScheduledAction(() => cmd.StartCmd(c, cb, CmdTrigger.Cmd), 1, true, "job"));
        return new Process();
      }
    }
    public class AlwaysJob : Job {
      protected override void pExec(CmdLine cmd, Action<string> cb, List<string> subst) { }
    }
    public class WaitJob : Job {
      ScheduledAction callback;
      readonly int wait;
      public WaitJob(int w) { this.wait = w; }
      protected override void pExec(CmdLine cmd, Action<string> cb, List<string> subst) {
        Log($"Waiting {this.wait} cycles");
        this.callback = new ScheduledAction(() => cb("Wait done"), this.wait, true, "wait");
        Schedule(this.callback);
      }
      protected override void pCancel() {
        this.callback?.Dispose();
        this.callback = null;
      }
    }
    public class RepeatJob : Job {
      readonly Job c;
      readonly Routine loop;
      bool running = false;
      public RepeatJob(Job job, Routine loop) {
        this.c = job;
        this.loop = loop;
      }
      protected override void pExec(CmdLine cmd, Action<string> cb, List<string> subst) {
        Log("Start looping");
        this.running = true;
        this.c.Exec(cmd, s => _break(s, cb), subst);
        this.continueJob(cmd, "start", subst);
      }
      void continueJob(CmdLine cmd, string s, List<string> subst) {
        if (this.running)
          this.loop.Exec(cmd, r => _continue(cmd, r, subst), subst);
      }
      void _break(string s, Action<string> cb) {
        cb(s);
        Cancel();
      }
      protected override void pCancel() {
        this.running = false;
        this.loop.Cancel();
      }
    }
    public class Routine : Job {
      int index;
      readonly List<Job> jobs = new List<Job>();
      public void AddJob(Job job) => this.jobs.Add(job);
      protected override void pExec(CmdLine cmd, Action<string> cb, List<string> subst) {
        this.index = -1;
        this.jobDone(cmd, "start", cb, subst);
      }
      protected override void pCancel() {
        if (this.index >= 0 && this.index < this.jobs.Count)
          this.jobs[this.index].Cancel();
        this.index = -1;
      }
      void jobDone(CmdLine cmd, string s, Action<string> cb, List<string> subst) {
        if (this.index >= 0)
          Log($"Job {this.index} complete with message '{s}'");
        ++this.index;
        if (this.index < this.jobs.Count)
          this.jobs[this.index].Exec(cmd, r => _jobDone(cmd, r, cb, subst), subst);
        else {
          cb(s);
          this.index = -1;
        }
      }
    }
    public class AutoRoutine : Routine {
      public readonly string Name;
      public AutoRoutine(string nm) {
        this.Name = nm;
      }
    }
  }
}
