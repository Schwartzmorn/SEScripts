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
      public readonly string Name;
      readonly List<Process> children = new List<Process>();
      bool dead = false;
      public Process(string name) {
        this.Id = COUNTER++;
        this.Name = name;
      }
      public void Kill() {
        if (this.dead) {
          return;
        }
        foreach (var p in this.children) {
          p.Kill();
        }
        this.PKill();
        this.dead = true;
      }
      public readonly int Id;
      protected virtual void PKill() { }
    }

    public class Cmd {
      public readonly string Name;
      public readonly string BriefHelp;
      public readonly List<string> DetailedHelp;
      public readonly Action<List<string>, Action<string>, CmdTrigger> Action;
      public readonly int MinArgs;
      public readonly int MaxArgs;
      public readonly CmdTrigger RequiredTrigger;

      public Cmd(string name,
                 string briefHelp,
                 Action<List<string>> callback,
                 string detailedHelp = "",
                 int minArgs = 0,
                 int maxArgs = int.MaxValue,
                 int nArgs = -1,
                 CmdTrigger reqTrigger = CmdTrigger.User) :
        this(name, briefHelp, (a, c, t) => { callback(a); c(null); }, detailedHelp, minArgs, maxArgs, nArgs, reqTrigger) { }

      public Cmd(string name,
                 string briefHelp,
                 Action<List<string>, Action<string>> callback,
                 string detailedHelp = "",
                 int minArgs = 0,
                 int maxArgs = int.MaxValue,
                 int nArgs = -1,
                 CmdTrigger reqTrigger = CmdTrigger.User) :
        this(name, briefHelp, (a, c, t) => { callback(a, c); }, detailedHelp, minArgs, maxArgs, nArgs, reqTrigger) { }

      public Cmd(string name,
                 string briefHelp,
                 Action<List<string>, Action<string>, CmdTrigger> callback,
                 string detailedHelp = "",
                 int minArgs = 0,
                 int maxArgs = int.MaxValue,
                 int nArgs = -1,
                 CmdTrigger reqTrigger = CmdTrigger.User) {
        this.Name = name;
        this.BriefHelp = briefHelp;
        this.DetailedHelp = detailedHelp.Split(new char[] { '\n' }).ToList();
        this.Action = callback;
        if (nArgs >= 0)
          this.MinArgs = this.MaxArgs = nArgs;
        else {
          this.MinArgs = minArgs;
          this.MaxArgs = maxArgs;
        }
        this.RequiredTrigger = reqTrigger;
      }

      public Process Spawn(List<string> args,
                           Action<string> callback,
                           CmdTrigger trig) => new Process(this.Name);

      public string Hlp() {
        string s = $"-{this.Name}";
        if (this.MaxArgs == 0)
          s += " (no argument)";
        else {
          s += " takes ";
          if (this.MinArgs == this.MaxArgs)
            s += $"{this.MinArgs}";
          else if (this.MinArgs == 0)
            s += $"{(this.MaxArgs < int.MaxValue ? $"up to {this.MaxArgs}" : "any number of")}";
          else if (this.MaxArgs < int.MaxValue)
            s += $"{this.MinArgs}-{this.MaxArgs}";
          else
            s += $"at least {this.MinArgs}";
          s += $" argument{(this.MaxArgs > 1 ? "s" : "")}";
        }
        return s;
      }
    }
  }
}
