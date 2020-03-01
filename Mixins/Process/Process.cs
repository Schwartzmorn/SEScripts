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

    public class Scheduler {
    }

    /// <summary>
    /// Smallest unit that can be scheduled in the <see cref="IngameScript.Program.Scheduler"/>
    /// </summary>
    /// <remarks>
    /// <para>If the method <c>Done</c> or <c>Kill</c> are called, its action won't be executed anymore and it will be removed from the scheduler.</para>
    /// <para>When <c>Done</c> is called, the process will wait for all its children to end before calling its provided <c>onDone</c> callback.</para>
    /// <para>When killed with <c>Kill</c>, a process kills all its children and executes the provided <c>onInterrupt</c> method.</para>
    /// </remarks>
    public class Process {
      /// <summary>Number of ticks since the last time it has been run</summary>
      public int Counter { get; private set; } = 0;
      /// <summary>Name of the process</summary>
      public readonly string Name;
      /// <summary>Number of ticks between each run</summary>
      public readonly int Period;
      /// <summary>Whether the process is still active. An inactive process may still have some active children.</summary>
      public bool Active {
        get { return this.Counter < 0; }
        private set { if (!value) this.Counter = -1; }
      }

      private readonly Action<Process> action;
      private List<Process> children;
      private readonly bool useOnce;
      private readonly Action onDone;
      private readonly Action onInterrupt;
      private readonly Process parent;

      /// <summary>
      /// Creates a new process that will be automatically scheduled in the <see cref="IngameScript.Program.Scheduler"/>
      /// </summary>
      /// <param name="action">Scheduled action to execute</param>
      /// <param name="period">Period at which to execute the action.</param>
      /// <param name="useOnce">Whether the action should only be executed once.</param>
      /// <param name="name">To identify the process</param>
      /// <param name="onDone">Callback to execute when the process is done</param>
      /// <param name="onInterrupt">Callback to execute when the process has been killed</param>
      public Process(Action<Process> action, int period = 1, bool useOnce = false, string name = null, Action onDone = null, Action onInterrupt = null):
          this(action, null, period, useOnce, name, onDone, onInterrupt) { }

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
        this.Active = false;
        this.parent?.notifyDone(this);
      }

      /// <summary>
      /// Sets the tick counter to the given value, which will impact when the action will be next run.
      /// </summary>
      /// <param name="newCounter">New tick counter</param>
      public void ResetCounter(int newCounter = 0) {
        this.Counter = Math.Min(Math.Max(0, newCounter), this.Period - 1);
      }

      /// <summary>
      /// Spawns a new process with itself as parent.
      /// For more info about the parameters, look at <see cref="IngameScript.Program.Process.Process(Action{Process}, int, bool, string, Action, Action)"/>
      /// </summary>
      /// <returns>the spawned process</returns>
      public Process Spawn(Action<Process> action, int period = 1, bool useOnce = false, string name = null, Action onDone = null, Action onInterrupt = null) {
        this.children = this.children ?? new List<Process>();
        this.children.Add(new Process(action, this, period, useOnce, name, onDone, onInterrupt));
        return this.children.Last();
      }

      /// <summary>
      /// Should not be called directly
      /// </summary>
      public void Tick() {
        if (this.Active && (++this.Counter >= this.Period)) {
          this.Active = this.useOnce;
          this.action?.Invoke(this);
          this.Counter = 0;
        }
      }

      private Process(Action<Process> action, Process parent, int period = 1, bool useOnce = false, string name = null, Action onDone = null, Action onInterrupt = null) {
        this.action = action;
        this.Period = period;
        this.useOnce = useOnce;
        this.Name = name;
        this.parent = parent;
        this.onDone = onDone;
        this.onInterrupt = onInterrupt;
        // TODO schedule itself
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
        this.Active = false;
        foreach (Process child in this.children ?? Enumerable.Empty<Process>()) {
          child.killNoNotify();
        }
        this.onInterrupt?.Invoke();
      }
    }
  }
}
