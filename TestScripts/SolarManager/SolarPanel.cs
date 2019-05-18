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
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
  partial class Program {
    public class SolarPanel {
      public SolarPanel(List<SolarRotor> rotors) {
        _rotors = rotors;
        UpdateFromGrid();
      }
      public float MaxOutput {
        get {
          return _panels.Sum(panel => panel == null ? 0 : panel.MaxOutput);
        }
      }
      public void UpdateFromGrid() => GTS.GetBlocksOfType<IMySolarPanel>(_panels, solarPanel => _rotors.Count(rotor => rotor.GetRotor() != null && solarPanel.CubeGrid == rotor.GetRotor().TopGrid) > 0);
      private List<IMySolarPanel> _panels = new List<IMySolarPanel>();
      private List<SolarRotor> _rotors;
    }
  }
}
