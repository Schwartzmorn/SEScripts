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

    public static T AssertNonNull<T>(T obj, string msg)
    {
      if (obj == null)
      {
        throw new InvalidOperationException(msg);
      }
      return obj;
    }
    public static IEnumerable<T> AssertNonEmpty<T>(IEnumerable<T> obj, string msg)
    {
      if (obj.Count() == 0)
      {
        throw new InvalidOperationException(msg);
      }
      return obj;
    }

    public Program()
    {
      Runtime.UpdateFrequency = UpdateFrequency.Update1;
      _manager = Process.CreateManager(Echo);
      var logger = new ScreenLogger(_manager, Me.GetSurface(0), echo: Echo);
      _manager.SetLogger(logger.Log);
      _commandLine = new CommandLine("Fabricator", logger.Log, _manager);
      var ini = new MyIni();
      ini.Parse(Me.CustomData);
      new Fabricator(ini, GridTerminalSystem, _manager, _commandLine, logger.Log);
    }
    //public void Save() => this.manager.Save(s => this.Me.CustomData = s);

    public void Main(string argument, UpdateType updateSource)
    {
      _commandLine.StartCmd(argument, CommandTrigger.User);
      _manager.Tick();
    }
  }
}
