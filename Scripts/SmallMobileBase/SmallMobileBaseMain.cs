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
      this.manager = Process.CreateManager(this.Echo);
      var screen = this.GridTerminalSystem.GetBlockWithName("SMB LCD (Rear Seat)") as IMyTextPanel;
      var logger = new Logger(this.manager, screen);
      this.cmd = new CommandLine("Small Mobile Base", logger.Log, this.manager);

      var ini = new IniWatcher(this.Me, this.manager);
      var controller = this.GridTerminalSystem.GetBlockWithName("SMB Remote Control (Forward)") as IMyShipController;
      var transformer = new CoordinatesTransformer(controller, this.manager);
      var wheelsController = new WheelsController(this.cmd, controller, this.GridTerminalSystem, ini, this.manager, transformer);
      new ConnectionClient(ini, this.GridTerminalSystem, this.IGC, this.cmd, this.manager, logger.Log);

      new CameraTurret(this.GridTerminalSystem, this.manager);

      new PilotAssist(this.GridTerminalSystem, ini, logger.Log, this.manager, wheelsController);
    }

    public void Save() => this.manager.Save(s => this.Me.CustomData = s);

    public void Main(string argument, UpdateType updateSource) {
      this.cmd.StartCmd(argument, CommandTrigger.User);
      if ((updateSource & UpdateType.Update1) > 0) {
        this.manager.Tick();
      }
    }
  }
}
