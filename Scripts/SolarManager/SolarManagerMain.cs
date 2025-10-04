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

namespace IngameScript
{
  public partial class Program : MyGridProgram
  {
    readonly IProcessManager _manager;
    readonly SolarManager _solarManager;
    readonly CommandLine _command;

    public Program()
    {
      Runtime.UpdateFrequency = UpdateFrequency.Update1;
      _manager = Process.CreateManager(Echo);
      var logger = new Logger(_manager, Me.GetSurface(0), echo: Echo);
      _command = new CommandLine("Solar Manager", logger.Log, _manager);
      _solarManager = new SolarManager(this, _command, _manager, logger.Log);
    }

    public void Save()
    {
      var ini = new MyIni();
      ini.TryParse(Me.CustomData);
      _manager.Save(s => Me.CustomData = s, ini);
    }

    public void Main(string args, UpdateType updateSource)
    {
      _command.StartCmd(args, CommandTrigger.Cmd);
      if ((updateSource & UpdateType.Update1) > 0)
      {
        _manager.Tick();
      }
    }
  }
}
