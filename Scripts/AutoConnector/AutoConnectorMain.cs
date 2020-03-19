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
  partial class Program : MyGridProgram {
    public delegate Vector3D VectorTransformer(Vector3D pos);
    private readonly AutoConnectionDispatcher _controller;
    private readonly CmdLine _commandLine;

    public Program() {
      this.Runtime.UpdateFrequency = UpdateFrequency.Update1;
      Logger.SetupGlobalInstance(new Logger(this.Me.GetSurface(0)), this.Echo);
      Schedule(Logger.Flush);
      this._commandLine = new CmdLine("Auto connector station", Log);
      var ini = new MyIni();
      ini.Parse(this.Me.CustomData);
      this._controller = new AutoConnectionDispatcher(this, this._commandLine, ini);
    }

    public void Save() => Scheduler.Inst.Save(s => this.Me.CustomData = s);

    public void Main(string argument, UpdateType updateSource) {
      this._commandLine.HandleCmd(argument, true);
      Scheduler.Inst.Tick();
    }
  }
}