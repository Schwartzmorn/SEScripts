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
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
  public partial class Program : MyGridProgram
  {
    readonly DoorManager _doorManager;
    readonly IProcessManager _manager;

    public Program()
    {
      Runtime.UpdateFrequency = UpdateFrequency.Update1;
      _manager = Process.CreateManager(Echo);
      var logger = new Logger(_manager, Me.GetSurface(0), echo: Echo);

      _doorManager = new DoorManager(this, _manager, logger.Log);
    }

    public void Save()
    {
    }

    public void Main(string argument, UpdateType updateSource) => _manager.Tick();
  }
}
