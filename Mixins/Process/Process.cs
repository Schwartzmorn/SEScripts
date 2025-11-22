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

namespace IngameScript
{
  partial class Program
  {
    public enum ProcessResult { OK, KO, KILLED };

    /// <summary> Smallest unit that can be scheduled in the <see cref="Scheduler"/>.
    /// <para>If the method <see cref="Done"/> or <see cref="Kill"/> are called, its action won't be executed anymore and it will be removed from the scheduler.</para>
    /// <para>When <see cref="Done"/> is called, the process will wait for all its children to end before calling its provided <see cref="_onDone"/> callback with <see cref="ProcessResult.KO"/>.</para>
    /// <para>When killed with <see cref="Kill"/>, a process kills all its children and executes the provided <see cref="_onDone"/> method with <see cref="ProcessResult.KILLED"/>.</para>
    /// </summary>
    public partial class Process : IProcessSpawner
    {
      /// <summary>Creates a new Process Manager (you should only need one). For it to have any effect, the<see cref="IProcessManager.Tick"/> method must be called once every cycle.</summary>
      /// <param name="logger">Used to log generic errors that happens when handling processes</param>
      /// <returns>A new process manager</returns>
      public static IProcessManager CreateManager(Action<string> logger = null, MyGridProgram program = null) => new ProcessManager(logger, program);
      /// <summary>
      /// Whether the process is still active. An inactive process is no longer ticked by the scheduler and therefore no longer executes its <see cref="_action"/> callback.
      /// <para>An inactive process may still have some active children, in which case it is <see cref="Alive"/>.</para>
      /// </summary>
      public bool Active
      {
        get { return Counter >= 0; }
        private set { if (!value) { Counter = -1; } }
      }
      /// <summary>Even if no longer active, a process is alive if it still has children.</summary>
      public bool Alive => Active || (_children != null && _children.Count > 0);
      /// <summary>Number of ticks since the last time it has been run.</summary>
      public int Counter { get; private set; } = 0;
      /// <summary>Unique id of the process.</summary>
      public readonly int ID;
      /// <summary>Name of the process.</summary>
      public readonly string Name;
      /// <summary>Number of ticks between each run.</summary>
      public int Period;
      public ProcessResult Result { get; private set; } = ProcessResult.OK;
      /// <summary>Whether the process will execute its action only once or not.</summary>
      public bool UseOnce { get; private set; }
      public readonly Process Parent;

      static int PCOUNTER = 0;
      readonly Action<Process> _action;
      List<Process> _children;
      readonly Action<string> _logger;
      readonly Action<Process> _onDone;
      readonly ISchedulerManager _scheduler;

      /// <summary>Ends the process so that the <see cref="_action"/> will no longer be executed.</summary>
      /// <remarks>It will remain alive until all its children are either done or killed</remarks>
      /// <remarks>The <see cref="_onDone"/> callback will only be executed with <see cref="ProcessResult.OK"/> once all its children have been terminated, unless <see cref="Fail"/> or <see cref="Kill"/> is called in the meantime.</remarks>
      public void Done()
      {
        if (Active)
        {
          Active = false;
          if ((_children?.Count ?? 0) == 0)
          {
            _invokeDone();
          }
        }
      }
      /// <summary>Ends the process so that the <see cref="_action"/> will no longer be executed.</summary>
      /// <remarks>It will remain alive until all its children are either done or killed</remarks>
      /// <remarks>The <see cref="_onDone"/> callback will only be executed with <see cref="ProcessResult.KO"/> once all its children have been terminated, unlees <see cref="Kill"/> is called in the meantime.</remarks>
      public void Fail()
      {
        if (Alive && Result == ProcessResult.OK)
        {
          Result = ProcessResult.KO;
          Done();
        }
      }
      /// <summary>
      /// Kills the process and all its children recursively.
      /// If present, the onInterrupt callback wil be executed
      /// </summary>
      public void Kill()
      {
        if (Alive)
        {
          _killNoNotify();
          Parent?._notifyDone(this);
        }
      }

      /// <summary>Kill all the process' children</summary>
      public void KillChildren()
      {
        if (_children != null)
        {
          bool hadChildren = _children.Where(p => p.Alive).Count() > 0;
          foreach (Process p in _children)
          {
            p._killNoNotify();
          }
          _children.Clear();
          if (hadChildren && !Active)
          {
            _invokeDone();
          }
        }
      }

      /// <summary> Sets the tick counter to the given value, which will impact when the action will be next run.</summary>
      /// <remarks>This has no effect on a process that is no longer <see cref="Alive"/></remarks>
      /// <param name="newCounter">New tick counter</param>
      public void ResetCounter(int newCounter = 0)
      {
        if (Active)
        {
          Counter = Math.Min(Math.Max(0, newCounter), Period - 1);
        }
      }

      /// <summary>
      /// Spawns a new process with itself as parent that will be automatically scheduled in the <see cref="Scheduler"/> instance <see cref="_scheduler"/>.
      /// </summary>
      /// <param name="action">Scheduled action to execute. Can be null.</param>
      /// <param name="name">To identify the process</param>
      /// <param name="onDone">Callback to execute when the process ends</param>
      /// <param name="period">Period at which to execute the action.</param>
      /// <param name="useOnce">Whether the action should only be executed once.</param>
      /// <returns>the spawned process</returns>
      public Process Spawn(Action<Process> action, string name = null, Action<Process> onDone = null, int period = 1, bool useOnce = false)
      {
        if (!Alive)
        {
          throw new InvalidOperationException($"Process {Name} is dead.");
        }
        _children = _children ?? new List<Process>();
        _children.Add(new Process(action, _logger, name ?? (Name + "-child"), onDone, this, period, _scheduler, useOnce));
        return _children.Last();
      }

      public override string ToString()
      {
        var sb = new StringBuilder();
        ToString(0, s => sb.Append(s));
        return sb.ToString();
      }

      public void ToString(int indent, Action<string> log)
      {
        log(new string(' ', 2 * indent) + $"{ID}: {Name}");
        foreach (Process child in _children ?? Enumerable.Empty<Process>())
        {
          child.ToString(indent + 1, log);
        }
      }

      void _tick()
      {
        if (Active && (++Counter >= Period))
        {
          try
          {
            _action?.Invoke(this);
          }
          catch (Exception e)
          {
            _logger?.Invoke($"Failed while running {Name}: {e.Message}");
          }
          Counter = Math.Min(0, Counter);
          if (UseOnce)
          {
            Done();
          }
        }
      }

      /// <summary>Protected constructor for unit tests only</summary>
      protected Process() { }

      Process(Action<Process> action, Action<string> logger, string name, Action<Process> onDone, Process parent, int period, ISchedulerManager scheduler, bool useOnce)
      {
        ID = ++PCOUNTER;
        Name = name ?? "<anonymous>";
        Period = Math.Max(1, period);
        UseOnce = useOnce;
        _action = action;
        _logger = logger;
        _onDone = onDone;
        Parent = parent;
        _scheduler = scheduler;
        scheduler.Schedule(this);
      }

      // called by a child when it is done
      void _notifyDone(Process child)
      {
        _children.Remove(child);
        if (!Active && _children.Count == 0)
        {
          _invokeDone();
        }
      }

      // Unschedule itself, kill all children
      void _killNoNotify()
      {
        bool wasAlive = Alive;
        Active = false;
        if (wasAlive)
        {
          Result = ProcessResult.KILLED;
        }
        foreach (Process child in _children ?? Enumerable.Empty<Process>())
        {
          child._killNoNotify();
        }
        if (wasAlive)
        {
          _children?.Clear();
          _invokeDone(false);
        }
      }

      void _invokeDone(bool notify = true)
      {
        try
        {
          _onDone?.Invoke(this);
          if (notify)
          {
            Parent?._notifyDone(this);
          }
        }
        catch (Exception e)
        {
          _logger?.Invoke($"Failed while terminating {Name}: {e.Message}");
        }
      }
    }
  }
}
