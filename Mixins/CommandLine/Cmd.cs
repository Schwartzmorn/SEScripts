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
    public class Cmd {
      public readonly string Name;
      public readonly string BriefHelp;
      public readonly List<string> DetailedHelp;
      public readonly Action<List<string>, Action<string>> Action;
      public readonly int MinArgs;
      public readonly int MaxArgs;
      public readonly bool RequirePermission;

      public Cmd(string name, string briefHelp, Action<List<string>> callback, string detailedHelp = "", int minArgs = 0, int maxArgs = int.MaxValue, bool requirePermission = true) :
        this(name, briefHelp, (a, c) => { callback(a); c(null); }, detailedHelp, minArgs, maxArgs, requirePermission) { }

      public Cmd(string name, string briefHelp, Action<List<string>, Action<string>> callback, string detailedHelp = "", int minArgs = 0, int maxArgs = int.MaxValue, bool requirePermission = true) {
        Name = name;
        BriefHelp = briefHelp;
        DetailedHelp = detailedHelp.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        Callback = callback;
        MinArgs = minArgs;
        MaxArgs = maxArgs;
        RequirePermission = requirePermission;
      }
    }
  }
}
