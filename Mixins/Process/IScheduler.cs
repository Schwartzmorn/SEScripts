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
    /// <summary>
    /// The class allows to conveniently schedule actions, repeating or not. It does so by using <see cref="Process"/>.
    /// </summary>
    public interface IScheduler {
      /// <summary>Adds an action to execute when saving.</summary>
      /// <param name="action">Action to execute</param>
      void AddOnSave(Action<MyIni> action);
      /// <summary>Kill the process with the given id.</summary>
      /// <param name="pid">id of the process to kill</param>
      void Kill(int pid);
      /// <summary>Kills all the processes.</summary>
      void KillAll();
      /// <summary>Kills all the processes with the given name.</summary>
      /// <param name="name">Name of the processe(s) to kill</param>
      void KillAll(string name);
      /// <summary>Executes all the OnSave actions.</summary>
      /// <param name="action">Action to execute with the serialized <see cref="MyIni"/> string</param>
      void Save(Action<string> action);
      /// <summary>
      /// Changes the smart scheduling.
      /// If the smart scheduling is active, the scheduler will try to spread the scheduled actions with a period greater than 1 over several cycles to avoid.
      /// By default, it will be true.
      /// </summary>
      /// <param name="smart">Whether the scheduler should try to schedule smartly the processed</param>
      void SetSmart(bool smart);
      /// <summary>
      /// Creates a new process without a parent that will be automatically scheduled.
      /// See <see cref="Process(Action{Process}, Process, int, bool, string, Action, Action)"/> for more information about the parameters
      /// </summary>
      /// <param name="action">Scheduled action to execute</param>
      /// <param name="period">Period at which to execute the action.</param>
      /// <param name="useOnce">Whether the action should only be executed once.</param>
      /// <param name="name">To identify the process</param>
      /// <param name="onDone">Callback to execute when the process is done</param>
      /// <param name="onInterrupt">Callback to execute when the process has been killed</param>
      /// <returns>the spawned process</returns>
      Process Spawn(Action<Process> action, int period = 1, bool useOnce = false, string name = null, Action onDone = null, Action onInterrupt = null);
      /// <summary>Makes all the processes tick, cleans up the dead processes.</summary>
      void Tick();
    }

    public interface IImplScheduler: IScheduler {
      /// <summary>Not to be called directly as processes are automatically scheduled</summary>
      /// <param name="p">Process to schedule</param>
      void Schedule(Process p);
    }
  }
}
