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
    readonly CommandLine cmd;
    readonly IProcessManager manager;
    Action<string> logger;
    public Program() {
      this.Runtime.UpdateFrequency = UpdateFrequency.Update1;
      this.manager = Process.CreateManager(this.Echo);
      var screen = this.GridTerminalSystem.GetBlockWithName("LCD (Rear Seat)") as IMyTextPanel;
      var logger = new Logger(this.manager, this.Me.GetSurface(0), size: 0.25f);
      this.logger = logger.Log;
      this.manager.SetLogger(logger.Log);
      this.cmd = new CommandLine("Small Mobile Base", logger.Log, this.manager);
      IMyTerminalBlock wheel = this.GridTerminalSystem.GetBlockWithName("Wheel test");
      ITerminalAction attach = wheel.GetActionWithName("Add Top Part");
      var proj = this.GridTerminalSystem.GetBlockWithName("Projector test") as IMyProjector;
      ITerminalAction spawn = proj.GetActionWithName("SpawnProjection");
      //attach.Apply(wheel);
      //var param = TerminalActionParameter.Get("Blueprints/cloud/Boring Machine Drill/bp.sbc");
      //var param = TerminalActionParameter.Get("Boring Machine Drill");
      //var param = TerminalActionParameter.Get("Blueprints/cloud/Boring Machine Drill");
      var param = TerminalActionParameter.Get("Welder");
      proj.ApplyAction("SpawnProjection", new List<TerminalActionParameter> { param });
      this.logger(spawn.Name.ToString());
      var sb = new StringBuilder("");
      spawn.WriteValue(proj, sb);
      this.logger(sb.ToString());
      var list = new List<IMyShipToolBase>();
      GridTerminalSystem.GetBlocksOfType(list, c => c.CubeGrid == this.Me.CubeGrid);
      this.logger("=====");
      foreach (var t in list) {
        this.logger(t.CustomName);
      }
    }

    public void Save() {
      this.manager.Save(s => this.Me.CustomData = s);
    }

    public void Main(string argument, UpdateType updateSource) {
      this.cmd.StartCmd(argument, CommandTrigger.User);
      if ((updateSource & UpdateType.Update1) > 0) {
        this.manager.Tick();
      }
    }

    void log(string a, string b, string c) => this.logger($"{a,6}:{b,9}{c,9}");
  }
}