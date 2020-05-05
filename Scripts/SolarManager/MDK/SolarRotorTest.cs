using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IngameScript.Mockups;
using IngameScript.Mockups.Blocks;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IngameScript.MDK {
  class SolarRotorTest {
    MockSolarPanel getPanel(float currentOutput, float maximumOutput) {
      return new MockSolarPanel() {
        CurrentOutput = currentOutput,
        MaxOutput = maximumOutput
      };
    }

    Program.SolarPanel getPanel(List<MockSolarPanel> panels) {
      var grid = new MockCubeGrid();
      var grids = new List<IMyCubeGrid> { grid };
      foreach (MockSolarPanel panel in panels) {
        panel.CubeGrid = grid;
      }
      var ps = panels.Select(p => p as IMySolarPanel).ToList();
      return new Program.SolarPanel(grids, ps);
    }

    MockMotorStator getStator(string customData = "") {
      var attachable = new MockMotorRotor();
      var res = new MockMotorStator() {
        CustomData = customData
      };
      res.MockPendingAttachment = attachable;
      res.Attach();
      return res;
    }

    public void Output() {
      var panelA = this.getPanel(new List<MockSolarPanel> { this.getPanel(100, 200) });
      var panelB = this.getPanel(new List<MockSolarPanel> { this.getPanel(50, 200), this.getPanel(150, 200) });

      var rotorA = new Program.SolarRotor(
        "",
        new List<IMyMotorStator>{ this.getStator() },
        panelA,
        null);

      var rotorB = new Program.SolarRotor(
        "",
        new List<IMyMotorStator> { this.getStator() },
        panelB,
        null);

      var rotorC = new Program.SolarRotor(
        "",
        new List<IMyMotorStator> { this.getStator() },
        new List<Program.SolarRotor> { rotorA, rotorB },
        null
      );

      /*Assert.AreEqual(0.5f, panelA.MaxRatio);
      Assert.AreEqual(0.5f, rotorA.Ratio);

      Assert.AreEqual(0.75f, panelB.MaxRatio);
      Assert.AreEqual(0.75f, rotorB.Ratio);

      Assert.AreEqual(0.75f, rotorC.Ratio);
      Assert.AreEqual(300, rotorC.CurrentOutput);
      Assert.AreEqual(600, rotorC.MaxOutput);*/
    }
  }
}
