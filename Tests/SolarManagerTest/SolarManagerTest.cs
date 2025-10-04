namespace SolarManagerTest;

using IngameScript;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using System.Collections.Generic;
using System.Reflection;
using Utilities;
using Utilities.Mocks.Base;
using Utilities.Mocks.Blocks;

[TestFixture]
public class SolarManagerTest
{
  private TestBed _testBed;
  private ProgramWrapper _wrapper;
  private Program.SolarManager _solarManager;

  [SetUp]
  public void SetUp()
  {
    _testBed = new TestBed();
    _wrapper = _testBed.CreateProgram<Program>();
    _wrapper.CubeGridMock.IsStatic = true;
    _solarManager = null;
  }

  private void _startRun()
  {
    _wrapper.ProgrammableBlockMock.CustomData = "[solar-manager]\nkeyword=Solar";
    _testBed.Tick();
    _solarManager = _wrapper.Program.GetType().GetField("_solarManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(_wrapper.Program) as Program.SolarManager;
  }

  private MyMotorStatorMock _createMotor(MyCubeGridMock baseGrid, MyCubeGridMock topGrid, string name)
  {
    var rotor = new MyMotorStatorMock(baseGrid) { CustomName = name };
    _ = new MyAttachableBlockMock(topGrid, rotor);
    return rotor;
  }

  [Test]
  public void It_Works_On_Basic_Single_Rotor()
  {

    var topGrid = new MyCubeGridMock(_wrapper.GridTerminalSystemMock);
    _ = new MySolarPanelMock(topGrid) { CurrentOutput = 500, CustomName = "Solar A" };
    _ = new MySolarPanelMock(topGrid) { CurrentOutput = 350, CustomName = "Solar B" };
    var stator = _createMotor(_wrapper.CubeGridMock, topGrid, "Rotor Solar 1");

    _startRun();

    Assert.That(_solarManager, Is.Not.Null);
    Assert.That(_solarManager.PanelCount, Is.EqualTo(1));
    Assert.That(_solarManager.CurrentOutput, Is.EqualTo(850));
    Assert.That(stator.CustomName, Is.EqualTo("Solar 1"));
  }

  [Test]
  public void It_Works_On_Basic_T_Rotor()
  {
    var middleGrid = new MyCubeGridMock(_wrapper.GridTerminalSystemMock);
    var leftGrid = new MyCubeGridMock(_wrapper.GridTerminalSystemMock);
    var rightGrid = new MyCubeGridMock(_wrapper.GridTerminalSystemMock);
    var baseRotor = _createMotor(_wrapper.CubeGridMock, middleGrid, "Solar base");
    var leftRotor = _createMotor(middleGrid, leftGrid, "Solar left");
    var rightRotor = _createMotor(middleGrid, rightGrid, "Solar right");
    _ = new MySolarPanelMock(leftGrid) { CurrentOutput = 200, CustomName = "Solar A" };
    _ = new MySolarPanelMock(rightGrid) { CurrentOutput = 250, CustomName = "Solar B" };

    _startRun();

    Assert.That(_solarManager.PanelCount, Is.EqualTo(2));
    Assert.That(_solarManager.RotorCount, Is.EqualTo(1));
    Assert.That(_solarManager.CurrentOutput, Is.EqualTo(450));
    Assert.That(baseRotor.CustomName, Is.EqualTo("Solar 1-Base"));
    var names = new HashSet<string> { leftRotor.CustomName, rightRotor.CustomName };
    Assert.That(names, Contains.Item("Solar 1-1"));
    Assert.That(names, Contains.Item("Solar 1-2"));
  }

  [Test]
  public void It_Ignores_Fluff()
  {
    var endGrid = new MyCubeGridMock(_wrapper.GridTerminalSystemMock);
    var solarGrid = new MyCubeGridMock(_wrapper.GridTerminalSystemMock);
    var endFluff = new MyCubeGridMock(_wrapper.GridTerminalSystemMock);
    var endRotorGrid = new MyCubeGridMock(_wrapper.GridTerminalSystemMock);
    var middleFluff = new MyCubeGridMock(_wrapper.GridTerminalSystemMock);
    var baseRotor = new MyCubeGridMock(_wrapper.GridTerminalSystemMock);
    _ = new MySolarPanelMock(solarGrid) { CurrentOutput = 200 };
    _createMotor(solarGrid, endGrid, "rotor fluff");
    _createMotor(endFluff, solarGrid, "end fluff");
    _createMotor(endRotorGrid, endFluff, "Solar end rotor");
    _createMotor(middleFluff, endRotorGrid, "middle fluff");
    _createMotor(_wrapper.CubeGridMock, baseRotor, "middle fluff");
    var baseStator = _createMotor(baseRotor, middleFluff, "Solar base rotor");

    baseStator.CustomData = "[solar-rotor]\nid-number=2";

    _startRun();

    Assert.That(_solarManager.RotorCount, Is.EqualTo(1));
    Assert.That(_solarManager.CurrentOutput, Is.EqualTo(200));
    Assert.That(baseStator.CustomName, Is.EqualTo("Solar 2-Base"));
  }

  [Test]
  public void It_Fails_On_Rotor_On_Solar_grid()
  {
    var gridA = new MyCubeGridMock(_wrapper.GridTerminalSystemMock);
    var gridB = new MyCubeGridMock(_wrapper.GridTerminalSystemMock);
    var rotorA = _createMotor(gridA, gridB, "Solar A");
    _createMotor(gridB, gridA, "Soplar B");
    _ = new MySolarPanelMock(gridA);

    var exception = Assert.Throws<TargetInvocationException>(_startRun);
    Assert.That(exception.InnerException.Message, Is.EqualTo($"Solar rotor '{rotorA.CustomName}' is on a metagrid with solar panels"));
  }

  [Test]
  public void It_Fails_On_Cycle()
  {
    var gridA = new MyCubeGridMock(_wrapper.GridTerminalSystemMock);
    var gridB = new MyCubeGridMock(_wrapper.GridTerminalSystemMock);
    var gridC = new MyCubeGridMock(_wrapper.GridTerminalSystemMock);
    _createMotor(gridA, gridB, "Solar A");
    _createMotor(gridB, gridA, "Solar B");
    _createMotor(gridB, gridC, "Solar C");
    _ = new MySolarPanelMock(gridC);

    var exception = Assert.Throws<TargetInvocationException>(_startRun);
    Assert.That(exception.InnerException.Message, Contains.Substring("Cycle detected"));
  }
}
