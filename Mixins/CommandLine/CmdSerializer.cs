using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
partial class Program {
public class CmdSerializer {
readonly StringBuilder _b;
public CmdSerializer(string command) { _b = new StringBuilder($"-{command}"); }
public CmdSerializer AddArg(object i) { _b.Append($" \"{i.ToString()}\""); return this; }
public override string ToString() => _b.ToString();
}
}
}
