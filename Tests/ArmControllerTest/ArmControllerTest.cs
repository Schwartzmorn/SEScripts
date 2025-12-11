namespace ArmControllerTest;

using System;
using IngameScript;
using NUnit.Framework;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI.Ingame;
using Utilities;
using Utilities.Mocks.Base;
using Utilities.Mocks.Blocks;
using VRageMath;

[TestFixture]
public class ArmControllerTest
{
  TestBed _testBed;
  ProgramWrapper _wrapper;
  Program _program;

  MyMotorSuspensionMock _getSuspension(Vector3D pos, bool left, MyCubeGridMock grid = null)
  {
    var motor = new MyMotorSuspensionMock(_wrapper.CubeGridMock)
    {
      CustomName = "Power",
      MaxSteerAngle = 30f,
      Strength = 1,
      // This should be the world matrix in case the wheels are in the correct orientation
      WorldMatrix = new MatrixD(
              0, 0, left ? 1 : -1, 0,
              left ? -1 : 1, 0, 0, 0,
              0, -1, 0, 0,
              pos.X, pos.Y, pos.Z, 1),
    };

    (motor.Top as MyAttachableBlockMock).WorldPositionMock = pos + new Vector3D(0, -1, 0);
    return motor;
  }

  [SetUp]
  public void SetUp()
  {
    _testBed = new TestBed();

    _wrapper = _testBed.CreateProgram<Program>();
    _program = _wrapper.Program as Program;
    // cockpit
    _ = new MyShipControllerMock(_wrapper.CubeGridMock)
    {
      CustomName = "Cockpit",
      ShipMassMock = new MyShipMass(1820, 1820, 1820),
    };
    // front wheels
    _ = _getSuspension(new Vector3D(-1, 0, -4), true);
    _ = _getSuspension(new Vector3D(1, 0, -4), false);
    // rear wheels
    _ = _getSuspension(new Vector3D(-1, 0, 4), true);
    _ = _getSuspension(new Vector3D(1, 0, 4), false);
    // create rotors
    var rotor = new MyMotorStatorMock(_wrapper.CubeGridMock)
    {
      CustomName = "Arm Rotor"
    };
    var topGrid = new MyCubeGridMock(_wrapper.GridTerminalSystemMock);
    rotor.Top = new MyAttachableBlockMock(topGrid);
    // create drill
    _ = new MyShipDrillMock(topGrid);
  }

  [Test]
  public void It_Allows_Using_Default_Positions()
  {
    _wrapper.ProgrammableBlockMock.CustomData = @"[arm-status]
pos=0,4.5
";
    _testBed.Tick();

    // we loaded the saved target
    Assert.That(_program.Controller.TargetPosition, Is.EqualTo(new Program.ArmPos(4.5f, 0)));

    _wrapper.Run("arm recall $auto-low");
    _testBed.Tick();

    // we have changed the target
    Assert.That(_program.Controller.TargetPosition, Is.EqualTo(new Program.ArmPos(2f, 0)));

    _wrapper.RunOnSave();

    // we have saved our new target
    Assert.That(_wrapper.ProgrammableBlockMock.CustomData, Contains.Substring("[arm-status]\npos=0,2"));
  }
}
