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
  partial class Program : MyGridProgram {
    public class ConsumerMock: IIniConsumer {
      public bool Called => this.calls.Count > 0;
      public MyIni GetCall(int i) => this.calls[i];

      private readonly List<MyIni> calls = new List<MyIni>();

      public void Read(MyIni ini) => this.calls.Add(ini);
    }
    public void Main(string argument, UpdateType updateSource) { }
  }
}
