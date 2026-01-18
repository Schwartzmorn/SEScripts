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
    IMyCargoContainer _cargo;
    IMyInventory _inventory;
    static Action<string> ECHO;

    Program()
    {
      _cargo = GridTerminalSystem.GetBlockWithName("Cargo Container") as IMyCargoContainer;
      _inventory = _cargo?.GetInventory(0);
      ECHO = Echo;
    }

    public void Main(string argument, UpdateType updateSource)
    {
      var surface = Me.GetSurface(0);
      surface.ContentType = ContentType.TEXT_AND_IMAGE;
      // surface.WriteText($"Cargo is null: {_cargo == null}\n");
      // surface.WriteText($"Cargo inventory is null: {_inventory == null}\n", true);
      // surface.WriteText($"Cargo inventory has {_inventory?.ItemCount} item(s)", true);

      var wheel = GridTerminalSystem.GetBlockWithName("Wheel Suspension") as IMyMotorSuspension;
      var camera = GridTerminalSystem.GetBlockWithName("Camera") as IMyCameraBlock;
      surface.WriteText($"{camera.RaycastConeLimit}");

      var list = new List<ITerminalProperty>();
      wheel.GetProperties(list);
      surface.WriteText($"{wheel.MaxSteerAngle}\n");
      foreach (var prop in list)
      {
        surface.WriteText(prop.Id + "\n", true);
      }
    }
  }
}
