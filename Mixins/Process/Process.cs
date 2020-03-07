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
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
  partial class Program {
    public enum ProcessResult { OK, KO, KILLED };

    /// <summary> Smallest unit that can be scheduled in the <see cref="Scheduler"/>.
    /// <para>If the method <see cref="Done"/> or <see cref="Kill"/> are called, its action won't be executed anymore and it will be removed from the scheduler.</para>
    /// <para>When <see cref="Done"/> is called, the process will wait for all its children to end before calling its provided <see cref="onDone"/> callback with <see cref="ProcessResult.KO"/>.</para>
    /// <para>When killed with <see cref="Kill"/>, a process kills all its children and executes the provided <see cref="onDone"/> method with <see cref="ProcessResult.KILLED"/>.</para>
    /// </summary>
    public partial class Process : IProcessSpawner {
      /// <summary>Creates a new Process Manager (you should only need one). For it to have any effect, the<see cref="IProcessManager.Tick"/> method must be called once every cycle.</summary>
      /// <param name="logger">Used to log generic errors that happens when handling processes</param>
      /// <returns>A new process manager</returns>
      public static IProcessManager CreateManager(Action<string> logger) => new ProcessManager(logger);
      /// <summary>
      /// Whether the process is still active. An inactive process is no longer ticked by the scheduler and therefore no longer executes its <see cref="action"/> callback.
      /// <para>An inactive process may still have some active children, in which case it is <see cref="Alive"/>.</para>
      /// </summary>
      public bool Active {
        get { return this.Counter >= 0; }
        private set { if (!value) this.Counter = -1; }
      }
      /// <summary>Even if no longer active, a process is alive if it still has children.</summary>
      public bool Alive => this.Active || (this.children != null && this.children.Count > 0);
      /// <summary>Number of ticks since the last time it has been run.</summary>
      public int Counter { get; private set; } = 0;
      /// <summary>Unique id of the process.</summary>
      public readonly int ID;
      /// <summary>Name of the process.</summary>
      public readonly string Name;
      /// <summary>Number of ticks between each run.</summary>
      public readonly int Period;
      public ProcessResult Result { get; private set; } = ProcessResult.OK;
      /// <summary>Whether the process will execute its action only once or not.</summary>
      public bool UseOnce { get; private set; }

      private static int PCOUNTER = 0;
      private readonly Action<Process> action;
      private List<Process> children;
      private readonly Action<string> logger;
      private readonly Action<Process> onDone;
      private readonly Process parent;
      private readonly ISchedulerManager scheduler;

      /// <summary>Ends the process so that the <see cref="action"/> will no longer be executed.</summary>
      /// <remarks>It will remain alive until all its children are either done or killed</remarks>
      /// <remarks>The <see cref="onDone"/> callback will only be executed with <see cref="ProcessResult.OK"/> once all its children have been terminated, unless <see cref="Fail"/> or <see cref="Kill"/> is called in the meantime.</remarks>
      public void Done() {
        if (this.Active) {
          if ((this.children?.Count ?? 0) == 0) {
            this.invokeDone();
            this.parent?.notifyDone(this);
          }
          this.Active = false;
        }
      }
      /// <summary>Ends the process so that the <see cref="action"/> will no longer be executed.</summary>
      /// <remarks>It will remain alive until all its children are either done or killed</remarks>
      /// <remarks>The <see cref="onDone"/> callback will only be executed with <see cref="ProcessResult.KO"/> once all its children have been terminated, unlees <see cref="Kill"/> is called in the meantime.</remarks>
      public void Fail() {
        if (this.Alive && this.Result == ProcessResult.OK) {
          this.Result = ProcessResult.KO;
          this.Done();
        }
      }
      /// <summary>
      /// Kills the process and all its children recursively.
      /// If present, the onInterrupt callback wil be executed
      /// </summary>
      public void Kill() {
        if (this.Alive) {
          this.killNoNotify();
          this.parent?.notifyDone(this);
        }
      }

      /// <summary> Sets the tick counter to the given value, which will impact when the action will be next run.</summary>
      /// <remarks>This has no effect on a process that is no longer <see cref="Alive"/></remarks>
      /// <param name="newCounter">New tick counter</param>
      public void ResetCounter(int newCounter = 0) {
        if (this.Active) {
          this.Counter = Math.Min(Math.Max(0, newCounter), this.Period - 1);
        }
      }

      /// <summary>
      /// Spawns a new process with itself as parent that will be automatically scheduled in the <see cref="Scheduler"/> instance <see cref="scheduler"/>.
      /// </summary>
      /// <param name="action">Scheduled action to execute. Can be null.</param>
      /// <param name="name">To identify the process</param>
      /// <param name="onDone">Callback to execute when the process ends</param>
      /// <param name="period">Period at which to execute the action.</param>
      /// <param name="useOnce">Whether the action should only be executed once.</param>
      /// <returns>the spawned process</returns>
      public Process Spawn(Action<Process> action, string name = null, Action<Process> onDone = null, int period = 1, bool useOnce = false) {
        if (!this.Alive) {
          throw new InvalidOperationException($"Process {this.Name} is dead.");
        }
        this.children = this.children ?? new List<Process>();
        this.children.Add(new Process(action, this.logger, name ?? (this.Name + "-child"), onDone, this, period, this.scheduler, useOnce));
        return this.children.Last();
      }

      public override string ToString() {
        var sb = new StringBuilder();
        this.ToString(0, s => sb.Append(s).Append('\n'));
        return sb.ToString();
      }

      public void ToString(int indent, Action<string> log) {
        log(new string(' ', 2 * indent) + $"{this.ID}: {this.Name}");
        foreach (Process child in this.children ?? Enumerable.Empty<Process>()) {
          child.ToString(indent + 1, log);
        }
      }

      private void tick() {
        if (this.Active && (++this.Counter >= this.Period)) {
          this.action?.Invoke(this);
          this.Counter = 0;
          if (this.UseOnce) {
            this.Done();
          }
        }
      }

      /// <summary>Protected constructor for unit tests only</summary>
      protected Process() { }

      private Process(Action<Process> action, Action<string> logger, string name, Action<Process> onDone, Process parent, int period, ISchedulerManager scheduler, bool useOnce) {
        this.ID = ++PCOUNTER;
        this.Name = name ?? "<anonymous>";
        this.Period = Math.Max(1, period);
        this.UseOnce = useOnce;
        this.action = action;
        this.logger = logger;
        this.onDone = onDone;
        this.parent = parent;
        this.scheduler = scheduler;
        scheduler.Schedule(this);
      }

      // called by a child when it is done
      private void notifyDone(Process child) {
        this.children.Remove(child);
        if (!this.Active && this.children.Count == 0) {
          this.invokeDone();
          this.parent?.notifyDone(this);
        }
      }

      // Unschedule itself, kill all children
      private void killNoNotify() {
        foreach (Process child in this.children ?? Enumerable.Empty<Process>()) {
          child.killNoNotify();
        }
        if (this.Alive) {
          this.Result = ProcessResult.KILLED;
          this.invokeDone();
        }
        this.children?.Clear();
        this.Active = false;
      }

      private void invokeDone() {
        try {
          this.onDone?.Invoke(this);
        } catch (Exception e) {
          this.logger?.Invoke($"Failed while terminating {this.Name}: {e.Message}");
        }
      }
    }
  }
}
