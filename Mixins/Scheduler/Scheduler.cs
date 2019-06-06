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
public static void Schedule(ScheduledAction a) => Scheduler.Inst.AddAction(a);
public static void Schedule(Action a) => Scheduler.Inst.AddAction(a);
public static void ScheduleOnSave(Action<MyIni> a) => Scheduler.Inst.AddOnSave(a);
public class Scheduler {
public static readonly Scheduler Inst = new Scheduler();
private readonly List<ScheduledAction> _actions = new List<ScheduledAction>(10);
private readonly List<ScheduledAction> _toAdd = new List<ScheduledAction>();
private readonly List<Action<MyIni>> _onSave = new List<Action<MyIni>>();
public void Tick() {
foreach(var a in _actions) {
  try {
    a.Tick();
  } catch(Exception e) {
    Log($"Failed on {a.Name}: {e.Message}");
  }
}
_actions.RemoveAll(a => a.ToBeRemoved);
foreach(var a in _toAdd) {
  if (a.Period > 1 && !a.Once) {
    var cs = new HashSet<int>(_actions.Where(b => b.Period == a.Period).Select(b => b.Counter));
    a.ResetCounter(Enumerable.Range(0, a.Period).FirstOrDefault(p => !cs.Contains(p)));
  }
}
_actions.AddRange(_toAdd);
_toAdd.Clear();
}
public void Save(Action<string> onSave) {
var ini = new MyIni();
_onSave.ForEach(a => a(ini));
onSave(ini.ToString());
}
public void AddAction(ScheduledAction a) => _toAdd.Add(a);
public void AddAction(Action a) => _toAdd.Add(new ScheduledAction(a));
public void AddOnSave(Action<MyIni> a) => _onSave.Add(a);
public void Remove(string name) {
if (name != null)
  foreach(var action in _actions.Where(a => a.Name == name))
    action.Dispose();
}
}
}
}
