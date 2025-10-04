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
  partial class Program
  {
    public class SolarPanel
    {
      readonly List<IMySolarPanel> _panels;

      public float CurrentOutput => _panels.Sum(p => p.CurrentOutput);
      public float MaxOutput => _panels.Sum(p => p.MaxOutput);

      public float MaxPossibleOutput => _panels.Sum(p => p.CubeGrid.GridSize == 2.5f ? 160000 : 40000);

      public SolarPanel(List<IMyCubeGrid> grids, List<IMySolarPanel> panels)
      {
        _panels = panels.Where(p => grids.Contains(p.CubeGrid)).ToList();
      }
    }
  }
}
