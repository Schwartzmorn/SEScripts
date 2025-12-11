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
  partial class Program : MyGridProgram
  {
    readonly CommandLine _commandLine;
    readonly IProcessManager _manager;

    public Program()
    {
      Runtime.UpdateFrequency = UpdateFrequency.Update1;
      _manager = Process.CreateManager(Echo);
      var logger = new Logger(_manager, Me.GetSurface(0), echo: Echo);
      _manager.SetLogger(logger.Log);
      _commandLine = new CommandLine("Funicular", logger.Log, _manager);
      var watcher = new IniWatcher(Me, _manager);
      var funicular = new Funicular(GridTerminalSystem, Me, _manager, _commandLine, watcher);
      _manager.AddOnSave(funicular.OnSave);
    }

    public void Save() => _manager.Save(s => Me.CustomData = s);

    public void Main(string argument, UpdateType updateSource)
    {
      _commandLine.StartCmd(argument, CommandTrigger.User);
      _manager.Tick();
    }
  }
}
