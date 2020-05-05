using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IngameScript.Mockups;
using IngameScript.Mockups.Blocks;
using Sandbox.ModAPI.Ingame;
using System.Reflection;
using VRage.Game;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IngameScript.MDK {
  class SolarManagerTest {
    static int id = 0;

    static int GetId() => ++id;

    ConsoleMockedRun run;

    MockCubeGrid baseGrid;
    MockProgrammableBlock programableBlock;
    MockGridTerminalSystem gts;
    Program program;
    Program.SolarManager solarManager;
    MockCubeGrid getMockCubeGrid(string name = null, bool isStatic = false) {
      int id = GetId();
      return new MockCubeGrid {
        CustomName = $"{name ?? "CubeGrid"} {id}",
        EntityId = id,
        GridSizeEnum = MyCubeSize.Large,
        IsStatic = isStatic,
      };
    }

    MockSolarPanel addMockSolarPanel(MockCubeGrid grid) {
      int id = GetId();
      var res = new MockSolarPanel {
        CubeGrid = grid,
        CustomName = $"solar panel {id}",
        EntityId = id,
        CurrentOutput = 100,
        MaxOutput = 200
      };
      this.gts.Add(res);
      return res;
    }

    MockMotorStator addMockStator(MockCubeGrid baseGrid, MockCubeGrid topGrid, string name = null, bool isSolar = true) {
      var attachable = new MockMotorRotor {
        CubeGrid = topGrid
      };
      int id = GetId();
      var res = new MockMotorStator {
        CubeGrid = baseGrid,
        CustomName = $"{(name ?? "rotor")} {(isSolar ? "solar " : "")}{id}",
        EntityId = id
      };
      res.MockPendingAttachment = attachable;
      res.Attach();
      this.gts.Add(res);
      return res;
    }

    void startRun() {
      this.run = new ConsoleMockedRun() {
        GridTerminalSystem = this.gts
      };
      this.run.NextTick();
      this.program = this.programableBlock.Program as Program;
      this.solarManager = this.program.GetType().GetField("solarManager", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this.program) as Program.SolarManager;
    }

    public void BeforeEach() {
      this.baseGrid = this.getMockCubeGrid("base grid", true);
      this.programableBlock = new MockProgrammableBlock {
        CubeGrid = this.baseGrid,
        CustomData = "[solar-manager]\nkeyword=solar",
        CustomName = "main pb",
        EntityId = GetId(),
        ProgramType = typeof(Program)
      };
      this.gts = new MockGridTerminalSystem {
        this.programableBlock
      };
      this.program = null;
      this.run = null;
      this.solarManager = null;
    }

    public void BasicSingleRotorCase() {
      var topGrid = this.getMockCubeGrid();
      this.addMockSolarPanel(topGrid);
      this.addMockSolarPanel(topGrid);
      var baseRotor = this.addMockStator(this.baseGrid, topGrid);

      this.startRun();

      Assert.IsNotNull(this.solarManager);
      Assert.AreEqual(1, this.solarManager.PanelCount);
      Assert.AreEqual(200, this.solarManager.CurrentOutput);
      Assert.AreEqual("solar 1", baseRotor.CustomName);
    }

    public void BasicTRotorCase() {
      var middleGrid = this.getMockCubeGrid("middle");
      var leftGrid = this.getMockCubeGrid("left");
      var rightGrid = this.getMockCubeGrid("right");
      var baseRotor = this.addMockStator(this.baseGrid, middleGrid, "base");
      var leftRotor = this.addMockStator(middleGrid, leftGrid, "left");
      var rightRotor = this.addMockStator(middleGrid, rightGrid, "right");
      this.addMockSolarPanel(leftGrid);
      this.addMockSolarPanel(rightGrid);

      this.startRun();

      Assert.AreEqual(2, this.solarManager.PanelCount);
      Assert.AreEqual(1, this.solarManager.RotorCount);
      Assert.AreEqual(200, this.solarManager.CurrentOutput);
      Assert.AreEqual("solar 1-Base", baseRotor.CustomName);
      var names = new HashSet<string> { leftRotor.CustomName, rightRotor.CustomName };
      Assert.IsTrue(names.Contains("solar 1-1"));
      Assert.IsTrue(names.Contains("solar 1-2"));
    }

    public void BasicRotorWithFluff() {
      var endGrid = this.getMockCubeGrid("end");
      var solarGrid = this.getMockCubeGrid("solar");
      this.addMockStator(solarGrid, endGrid, "end fluff", isSolar: false);
      this.addMockSolarPanel(solarGrid);
      var endFluff = this.getMockCubeGrid("end fluff");
      this.addMockStator(endFluff, solarGrid, "end fluff", isSolar: false);
      var endRotorGrid = this.getMockCubeGrid("end rotor grid");
      this.addMockStator(endRotorGrid, endFluff, "end rotor", isSolar: true);
      var middleFluff = this.getMockCubeGrid("middle fluff");
      this.addMockStator(middleFluff, endRotorGrid, "middle fluff", isSolar: false);
      var baseRotor = this.getMockCubeGrid("base rotor");
      var baseStator = this.addMockStator(baseRotor, middleFluff, "base rotor", isSolar: true);
      this.addMockStator(this.baseGrid, baseRotor, "middle fluff", isSolar: false);

      baseStator.CustomData = "[solar-rotor]\nid-number=2";

      this.startRun();

      Assert.AreEqual(1, this.solarManager.RotorCount);
      Assert.AreEqual(100, this.solarManager.CurrentOutput);
      Assert.AreEqual("solar 2-Base", baseStator.CustomName);
    }

    public void FailOnRotorOnSolarGrid() {
      var gridA = this.getMockCubeGrid("A");
      var gridB = this.getMockCubeGrid("B");
      var rotorA = this.addMockStator(gridA, gridB, "A");
      this.addMockStator(gridB, gridA, "B");
      this.addMockSolarPanel(gridA);

      try {
        this.startRun();
        Assert.Fail("Expected the parsing to have thrown");
      } catch (TargetInvocationException e) {
        Assert.AreEqual($"Solar rotor '{rotorA.CustomName}' is on a metagrid with solar panels", e.InnerException.Message);
      }
    }

    public void FailOnCycle() {
      var gridA = this.getMockCubeGrid("A");
      var gridB = this.getMockCubeGrid("B");
      var gridC = this.getMockCubeGrid("C");
      this.addMockStator(gridA, gridB, "A");
      this.addMockStator(gridB, gridA, "B");
      this.addMockStator(gridB, gridC, "C");
      this.addMockSolarPanel(gridC);

      try {
        this.startRun();
        Assert.Fail("Expected the parsing to have thrown");
      } catch (TargetInvocationException e) {
        Assert.IsTrue(e.InnerException.Message.StartsWith("Cycle detected"));
      }
    }
  }
}
