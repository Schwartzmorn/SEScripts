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
public class Ini : MyIni {
public static readonly char[] SEP = new char[] { ',' };
readonly IMyTerminalBlock _b;
readonly List<IIniConsumer> _c = new List<IIniConsumer>();
string _prev;
public Ini(IMyTerminalBlock b) {
_b = b;
this.Parse(b.CustomData);
_prev = _b.CustomData;
}

public void Add(IIniConsumer c) {
if (_c.Count == 0)
  Schedule(new ScheduledAction(_upd, 100, false, "ini-update"));
_c.Add(c);
}

private void _upd() {
if (!ReferenceEquals(_b.CustomData, _prev)) {
  _prev = _b.CustomData;
  this.Parse(_prev);
  _c.ForEach(c => c.Read(this));
}
}
}
}
}
