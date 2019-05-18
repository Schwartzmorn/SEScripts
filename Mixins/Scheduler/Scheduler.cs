using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript {
  partial class Program {
    public class Scheduler {
      public static readonly Scheduler Inst = new Scheduler();

      private readonly List<ScheduledAction> _actions = new List<ScheduledAction>(10);
      private readonly List<ScheduledAction> _actionsToAdd = new List<ScheduledAction>();
      private readonly List<Action<MyIni>> _actionsOnSave = new List<Action<MyIni>>();

      public void Tick() {
        foreach(var a in _actions) {
          try {
            a.Tick();
          } catch(Exception e) {
            Logger.Inst.Log($"Failed on {a.Name}: {e.Message}");
          }
        }
        _actions.RemoveAll(a => a.ToBeRemoved);
        _actions.AddRange(_actionsToAdd);
        _actionsToAdd.Clear();
      }

      public void Save(Action<string> onSave) {
        var ini = new MyIni();
        _actionsOnSave.ForEach(a => a(ini));
        onSave(ini.ToString());
      }

      public void AddAction(ScheduledAction a) => _actionsToAdd.Add(a);

      public void AddAction(Action a) => _actionsToAdd.Add(new ScheduledAction(a));

      public void AddActionOnSave(Action<MyIni> a) => _actionsOnSave.Add(a);

      public void Remove(string name) {
        if (name != null) {
          foreach(var action in _actions.Where(a => a.Name == name)) {
            action.Dispose();
          }
        }
      }
    }
  }
}
