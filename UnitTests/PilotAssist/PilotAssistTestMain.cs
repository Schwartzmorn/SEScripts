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

    public class MockDeactivator: IPADeactivator {
      public bool Deactivate = false;
      public bool ShouldDeactivate() => this.Deactivate;
    }

    public class MockBraker : IPABraker {
      public bool Handbrake = false;
      public bool ShouldHandbrake() => this.Handbrake;
    }

    // Mock
    public class WheelsController {
      readonly List<float> powers = new List<float>();
      readonly List<float> steers = new List<float>();
      public float Power => this.powers.Last();
      public float Steer => this.steers.Last();
      public void SetPower(float power) => this.powers.Add(power);
      public void SetSteer(float steer) => this.steers.Add(steer);
    }

    public void Main(string argument, UpdateType updateSource) {}
  }
}
