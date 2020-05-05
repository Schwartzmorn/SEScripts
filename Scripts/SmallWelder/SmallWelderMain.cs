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
    readonly CommandLine cmd;
    readonly IProcessManager manager;
    public Program() {
      this.Runtime.UpdateFrequency = UpdateFrequency.Update1;
      var cockpits = new List<IMyCockpit>();
      this.GridTerminalSystem.GetBlocksOfType(cockpits, c => c.CubeGrid == this.Me.CubeGrid);
      IMyCockpit cockpit = cockpits.First();
      this.manager = Process.CreateManager(this.Echo);
      var ct = new CoordinatesTransformer(cockpit, this.manager);
      var logger = new Logger(this.manager, cockpit.GetSurface(0), new Color(0, 39, 15), new Color(27, 228, 33), this.Echo, 1.0f);
      this.cmd = new CommandLine("Small welder", logger.Log, this.manager);
      var ini = new IniWatcher(this.Me, this.manager);
      var wc = new WheelsController(this.cmd, cockpit, this.GridTerminalSystem, ini, this.manager, ct);
      var ac = new ArmController(ini, this, this.cmd, cockpit, wc, this.manager);
      var client = new ConnectionClient(ini, this.GridTerminalSystem, this.IGC, this.cmd, this.manager, logger.Log);
      var ah = new PilotAssist(this.GridTerminalSystem, ini, logger.Log, this.manager, wc);
      ah.AddBraker(client);
    }

    public void Save() => this.manager.Save(s => this.Me.CustomData = s);

    public void Main(string arg, UpdateType us) {
      this.cmd.StartCmd(arg, CommandTrigger.User);
      if ((us & UpdateType.Update1) > 0) {
        this.manager.Tick();
      }
    }
  }
}
