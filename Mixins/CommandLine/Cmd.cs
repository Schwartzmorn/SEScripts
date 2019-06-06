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
  public class Process {
    static int COUNTER = 1;
    public readonly int Id;
    public void Kill() {}
    public readonly string Name;
    bool _dead = false;
    public Process(string name) {
      Id = COUNTER++;
      Name = name;
    }
  }
  public class Cmd {
    public readonly string Name;
    public readonly string BriefHelp;
    public readonly List<string> DetailedHelp;
    public readonly Action<List<string>, Action<string>, CmdTrigger> Action;
    public readonly int MinArgs;
    public readonly int MaxArgs;
    public readonly CmdTrigger RequiredTrigger;

    public Cmd(string name, string briefHelp, Action<List<string>> callback, string detailedHelp = "", int minArgs = 0, int maxArgs = int.MaxValue, int nArgs = -1, CmdTrigger reqTrigger = CmdTrigger.User) :
      this(name, briefHelp, (a, c, t) => { callback(a); c(null); }, detailedHelp, minArgs, maxArgs, nArgs, reqTrigger) { }

    public Cmd(string name, string briefHelp, Action<List<string>, Action<string>> callback, string detailedHelp = "", int minArgs = 0, int maxArgs = int.MaxValue, int nArgs = -1, CmdTrigger reqTrigger = CmdTrigger.User) :
      this(name, briefHelp, (a, c, t) => { callback(a, c); }, detailedHelp, minArgs, maxArgs, nArgs, reqTrigger) { }

    public Cmd(string name, string briefHelp, Action<List<string>, Action<string>, CmdTrigger> callback, string detailedHelp = "", int minArgs = 0, int maxArgs = int.MaxValue, int nArgs = -1, CmdTrigger reqTrigger = CmdTrigger.User) {
      Name = name;
      BriefHelp = briefHelp;
      DetailedHelp = detailedHelp.Split(new char[] { '\n' }).ToList();
      Action = callback;
      if (nArgs >= 0)
        MinArgs = MaxArgs = nArgs;
      else {
        MinArgs = minArgs;
        MaxArgs = maxArgs;
      }
      RequiredTrigger = reqTrigger;
    }

    public Process Spawn(List<string> args, Action<string> callback, CmdTrigger trig) {

      return new Process(Name);
    }

    public string Hlp() {
      string s = $"-{Name}";
      if(MaxArgs == 0)
        s += " (no argument)";
      else {
        s += " takes ";
        if(MinArgs == MaxArgs)
          s += $"{MinArgs}";
        else if(MinArgs == 0)
          s += $"{(MaxArgs < int.MaxValue ? $"up to {MaxArgs}" : "any number of")}";
        else if(MaxArgs < int.MaxValue)
          s += $"{MinArgs}-{MaxArgs}";
        else
          s += $"at least {MinArgs}";
        s += $" argument{(MaxArgs > 1 ? "s" : "")}";
      }
      return s;
    }
  }
}
}
