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
    public interface IProcessSpawner {
      /// <summary>
      /// Creates a new <see cref="Process"/> without a parent that will be automatically scheduled in the <see cref="IProcessManager"/>.
      /// <para>See also <seealso cref="Process.Spawn(Action{Process}, int, bool, string, Action, Action)"/></para>
      /// </summary>
      /// <param name="action">Scheduled action to execute</param>
      /// <param name="name">To identify the process</param>
      /// <param name="onDone">Callback to execute when the process is finished</param>
      /// <param name="period">Period at which to execute the action.</param>
      /// <param name="useOnce">Whether the action should only be executed once.</param>
      /// <returns>the spawned process</returns>
      Process Spawn(Action<Process> action, string name = null, Action<Process> onDone = null, int period = 1, bool useOnce = false);
    }
    /// <summary>No good reason for to expand IProcessSpawner, it's just more convenient than having two objects</summary>
    public interface ISaveManager: IProcessSpawner  {
      /// <summary>Adds an action to execute when saving.</summary>
      /// <param name="action">Action to execute</param>
      void AddOnSave(Action<MyIni> action);
      /// <summary>Executes all the actions added with <see cref="AddOnSave(Action{MyIni})"/> and execute the <paramref name="action"/> on the result of <see cref="MyIni.ToString"/>.</summary>
      /// <param name="action">Action to execute with the serialized <see cref="MyIni"/> string.</param>
      void Save(Action<string> action);
      /// <summary>Executes all the actions added with <see cref="AddOnSave(Action{MyIni})"/> on the provided <paramref name="ini"/> and execute the <paramref name="action"/> on the result of <see cref="MyIni.ToString"/>.</summary>
      /// <param name="action">Action to execute with the serialized <see cref="MyIni"/> string.</param>
      /// <param name="ini">Ini object to start with <see cref="MyIni"/> string.</param>
      void Save(Action<string> action, MyIni ini);
    }
    /// <summary>
    /// The class allows to conveniently schedule actions, repeating or not. It does so by using <see cref="Process"/>.
    /// </summary>
    public interface IProcessManager: ISaveManager {
      /// <summary>Kill the <see cref="Process"/> with the given id <paramref name="pid"/>.</summary>
      /// <param name="pid">id of the process to kill</param>
      void Kill(int pid);
      /// <summary>Kills all the processes.</summary>
      void KillAll();
      /// <summary>Kills all the processes with the given <paramref name="name"/>.</summary>
      /// <param name="name">Name of the process(es) to kill</param>
      void KillAll(string name);
      /// <summary>Makes all the scheduled <see cref="Process"/>es tick, cleans up the dead processes.</summary>
      void Tick();
      /// <summary>Dumps all the alive processes</summary>
      /// <param name="log">Action to run for each process</param>
      void Log(Action<string> log);
      void SetLogger(Action<string> logger);
    }
  }
}
