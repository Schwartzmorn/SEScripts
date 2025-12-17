using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
  partial class Program : MyGridProgram
  {
    readonly CommandLine _cmd;
    readonly IProcessManager _manager;
    public Program()
    {
      Runtime.UpdateFrequency = UpdateFrequency.Update1;
      _manager = Process.CreateManager(Echo);
      var screen = GridTerminalSystem.GetBlockWithName("SMB LCD (Rear Seat)") as IMyTextPanel;
      var logger = new ScreenLogger(_manager, screen);
      _cmd = new CommandLine("Small Mobile Base", logger.Log, _manager);

      var ini = new IniWatcher(Me, _manager);
      var controller = GridTerminalSystem.GetBlockWithName("SMB Remote Control (Forward)") as IMyShipController;
      var transformer = new CoordinatesTransformer(controller, _manager);
      var wheelsController = new WheelsController(_cmd, controller, GridTerminalSystem, ini, _manager, transformer);
      new ConnectionClient(Me, ini, GridTerminalSystem, IGC, _cmd, _manager, logger.Log);

      new CameraTurret(GridTerminalSystem, _manager);

      new PilotAssist(Me, GridTerminalSystem, ini, logger.Log, _manager, wheelsController);
    }

    public void Save() => _manager.Save(s => Me.CustomData = s);

    public void Main(string argument, UpdateType updateSource)
    {
      _cmd.StartCmd(argument, CommandTrigger.User);
      if ((updateSource & UpdateType.Update1) > 0)
      {
        _manager.Tick();
      }
    }
  }
}
