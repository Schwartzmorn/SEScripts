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

namespace IngameScript {
  partial class Program {
    public class ProcessMock : Process {
      public readonly Action<Process> action;

      public ProcessMock(Action<Process> action) {
        this.action = action;
      }
    };
    // Mock
    public class ProcessSpawnerMock : ISaveManager {
      public VRage.MyTuple<Action<Process>, string, Action<Process>, int, bool>? LastCall { get; private set; }
      public bool HasOnSave => this.onSave != null;
      public bool Saved { get; private set; } = false;
      private Process lastProcess;
      private Action<MyIni> onSave;
      public Process Spawn(Action<Process> action, string name = null, Action<Process> onDone = null, int period = 1, bool useOnce = false) {
        this.LastCall = VRage.MyTuple.Create(action, name, onDone, period, useOnce);
        this.lastProcess = new ProcessMock(action);
        return this.lastProcess;
      }
      public void AddOnSave(Action<MyIni> onSave) => this.onSave = onSave;
      public void Save(Action<string> action) => this.Saved = true;
      public void MockProcessTick() => this.LastCall.Value.Item1(this.lastProcess);
      public string GetSavedString() {
        var ini = new MyIni();
        this.onSave(ini);
        return ini.ToString();
      }
    };
  }
}
