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

    public static IScheduler SCHEDULER => Process.SCHEDULER;

    /// <summary>
    /// Smallest unit that can be scheduled in the <see cref="IngameScript.Program.Scheduler"/>
    /// </summary>
    /// <remarks>
    /// <para>If the method <see cref="Done"/> or <see cref="Kill"/> are called, its action won't be executed anymore and it will be removed from the scheduler.</para>
    /// <para>When <see cref="Done"/> is called, the process will wait for all its children to end before calling its provided <c>onDone</c> callback.</para>
    /// <para>When killed with <see cref="Kill"/>, a process kills all its children and executes the provided <c>onInterrupt</c> method.</para>
    /// </remarks>
    public partial class Process {
      /// <summary>Number of ticks since the last time it has been run</summary>
      /// <summary>Whether the process is still active. An inactive process may still have some active children.</summary>
      public bool Active {
        get { return this.Counter >= 0; }
        private set { if (!value) this.Counter = -1; }
      }
      /// <summary>Even if no longer active, a process is alive if it still has children.</summary>
      public bool Alive => this.Active || (this.children != null && this.children.Count > 0);
      /// <summary>Process will only call its action once then be done.</summary>
      public int Counter { get; private set; } = 0;
      /// <summary>Unique id of the process</summary>
      public readonly int ID;
      /// <summary>Name of the process</summary>
      public readonly string Name;
      /// <summary>Number of ticks between each run</summary>
      public readonly int Period;
      /// <summary>Whether the process will execute its action only once or not</summary>
      public bool UseOnce { get; private set; }

      private static int PCOUNTER = 0;
      private readonly Action<Process> action;
      private List<Process> children;
      private readonly Action onDone;
      private readonly Action onInterrupt;
      private readonly Process parent;
      private readonly IImplScheduler scheduler;

      /// <summary>
      /// Kills the process and all its children recursively.
      /// If present, the onInterrupt callback wil be executed
      /// </summary>
      public void Kill() {
        this.parent?.notifyDone(this);
        this.killNoNotify();
      }

      /// <summary>
      /// Notifies that the process is done
      /// </summary>
      public void Done() {
        this.parent?.notifyDone(this);
        if (this.Active && (this.children?.Count ?? 0) == 0) {
          this.onDone?.Invoke();
        }
        this.Active = false;
      }

      /// <summary>
      /// Sets the tick counter to the given value, which will impact when the action will be next run.
      /// </summary>
      /// <param name="newCounter">New tick counter</param>
      public void ResetCounter(int newCounter = 0) {
        if (this.Active) {
          this.Counter = Math.Min(Math.Max(0, newCounter), this.Period - 1);
        }
      }

      /// <summary>
      /// Spawns a new process with itself as parent that will be automatically scheduled in the <see cref="Scheduler"/> instance <see cref="SCHEDULER"/>.
      /// </summary>
      /// <param name="action">Scheduled action to execute. Can be null.</param>
      /// <param name="period">Period at which to execute the action.</param>
      /// <param name="useOnce">Whether the action should only be executed once.</param>
      /// <param name="name">To identify the process</param>
      /// <param name="onDone">Callback to execute when the process is done</param>
      /// <param name="onInterrupt">Callback to execute when the process has been killed</param>
      /// <returns>the spawned process</returns>
      public Process Spawn(Action<Process> action, int period = 1, bool useOnce = false, string name = null, Action onDone = null, Action onInterrupt = null) {
        this.children = this.children ?? new List<Process>();
        this.children.Add(new Process(action, this, this.scheduler, period, useOnce, name, onDone, onInterrupt));
        return this.children.Last();
      }

      public override string ToString() {
        var sb = new StringBuilder();
        this.toString(0, sb);
        return sb.ToString();
      }

      private void toString(int indent, StringBuilder sb) {
        sb.Append(' ', 2 * indent).Append(this.ID).Append(": ").Append(this.Name).Append('\n');
        foreach (Process child in this.children ?? Enumerable.Empty<Process>()) {
          child.toString(indent + 1, sb);
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

      private Process(Action<Process> action, Process parent, IImplScheduler scheduler, int period, bool useOnce, string name, Action onDone, Action onInterrupt) {
        this.ID = ++PCOUNTER;
        this.Name = name;
        this.Period = Math.Max(1, period);
        this.UseOnce = useOnce;
        this.action = action;
        this.onDone = onDone;
        this.onInterrupt = onInterrupt;
        this.parent = parent;
        this.scheduler = scheduler;
        scheduler.Schedule(this);
      }

      // called by a child when it is done
      private void notifyDone(Process child) {
        this.children.Remove(child);
        if (!this.Active && this.children.Count == 0) {
          this.onDone?.Invoke();
        }
      }

      // Unschedule itself, kill all children
      private void killNoNotify() {
        foreach (Process child in this.children ?? Enumerable.Empty<Process>()) {
          child.killNoNotify();
        }
        if (this.Alive) {
          try {
            this.onInterrupt?.Invoke();
          } catch(Exception e) {
            Log($"Failed while killing {this.Name}: {e.Message}");
          }
        }
        this.children?.Clear();
        this.Active = false;
      }
    }
  }
}
