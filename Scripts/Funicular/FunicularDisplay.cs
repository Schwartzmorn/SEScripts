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
    class FunicularDisplay
    {
      readonly bool _reversed;
      readonly Display _display;

      public FunicularDisplay(IMyTerminalBlock referenceBlock, IMyTextPanel panel)
      {
        _reversed = panel.WorldMatrix.Forward.Dot(referenceBlock.WorldMatrix.Left) > 0;
        _display = new Display(panel);
      }

      public void UpdateStatus(FunicularCommand command, FunicularState state, float distance, Vector3D bottom, Vector3D top)
      {
        using (var f = _display.DrawFrame())
        {
          f.DrawText($"{command} {state} {distance}", _display.SurfaceSize / 2, alignment: TextAlignment.CENTER);
          f.DrawText($"{top}", _display.SurfaceSize / 2 + new Vector2(0, 30), alignment: TextAlignment.CENTER);
          f.DrawText($"{bottom}", _display.SurfaceSize / 2 + new Vector2(0, 60), alignment: TextAlignment.CENTER);
        }
      }
    }
  }
}
