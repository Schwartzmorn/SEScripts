namespace WheelControllerTest;

using System;
using System.Collections.Generic;
using IngameScript;
using NUnit.Framework;
using Sandbox.ModAPI.Ingame;
using Utilities;
using Utilities.Mocks;
using Utilities.Mocks.Base;
using Utilities.Mocks.Blocks;
using VRageMath;

[TestFixture]
class PowerWheelTest
{
  static readonly float TOLERANCE = 0.01f;

  MyShipControllerMock _controller;
  Program.WheelBase _wheelBase;
  TestBed _testBed;
  MyGridTerminalSystemMock _gts;
  MyCubeGridMock _cubeGridMock;

  [SetUp]
  public void SetUp()
  {
    _testBed = new TestBed();
    _gts = new MyGridTerminalSystemMock(_testBed);
    _cubeGridMock = new MyCubeGridMock(_gts)
    {
      GridSizeEnum = VRage.Game.MyCubeSize.Small
    };
    // this controller's coodrinates will be the same as the world coordinates
    _controller = new MyShipControllerMock(_cubeGridMock)
    {
      ShipMassMock = new MyShipMass(1820, 1820, 1820),
      WorldPositionMock = Vector3D.Zero,
      WorldMatrix = MatrixD.Identity
    };
    _wheelBase = new Program.WheelBase();
  }

  public MyMotorSuspensionMock GetSuspension(Vector3D pos, bool left, MyCubeGridMock grid = null)
  {
    var motor = new MyMotorSuspensionMock(grid ?? _cubeGridMock)
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
      WorldPositionMock = pos
    };

    (motor.Top as MyAttachableBlockMock).WorldPositionMock = pos + new Vector3D(0, -1, 0);
    return motor;
  }

  [Test]
  public void Create()
  {
    var transformer = new Program.CoordinatesTransformer(_controller);
    var leftWheel = GetSuspension(new Vector3D(-1, 0, 1), true);
    var rightWheel = GetSuspension(new Vector3D(1, 0, 1), false);
    (rightWheel.Top as MyAttachableBlockMock).WorldPositionMock = new Vector3D(1, 1, 1);
    var leftPowerWheel = new Program.PowerWheel(leftWheel, _wheelBase, transformer);
    var rightPowerWheel = new Program.PowerWheel(rightWheel, _wheelBase, transformer);

    Assert.That(leftPowerWheel.WheelSize, Is.EqualTo(3));
    // We are in the wheel's referential, so it should be -1, 0, 1 but it is offset to take into account the point around which the wheel actually rotates
    Assert.That(leftPowerWheel.Position, Is.EqualTo(new Vector3D(-1.5, 0, 1)));
    Assert.That(leftPowerWheel.Mass, Is.EqualTo(205));
    Assert.That(leftPowerWheel.GetCompressionRatio(), Is.EqualTo(0.25).Within(TOLERANCE));

    Assert.That(rightPowerWheel.Position, Is.EqualTo(new Vector3D(1.5, 0, 1)));
    Assert.That(rightPowerWheel.GetCompressionRatio(), Is.EqualTo(0.75).Within(TOLERANCE));

    // to go forward, we need to invert the power on the right wheels
    leftPowerWheel.Power = 1;
    rightPowerWheel.Power = 1;
    Assert.That(leftWheel.PropulsionOverride, Is.EqualTo(1));
    Assert.That(rightWheel.PropulsionOverride, Is.EqualTo(-1));

    // center of mass is forward of the wheels
    leftPowerWheel.Strafe(-1);
    rightPowerWheel.Strafe(-1);
    Assert.That(leftWheel.InvertSteer);
    Assert.That(rightWheel.InvertSteer);
    leftPowerWheel.Strafe(1);
    rightPowerWheel.Strafe(1);
    Assert.That(leftWheel.InvertSteer, Is.False);
    Assert.That(rightWheel.InvertSteer, Is.False);

    // Test GetPointOfContactW
    Assert.That(leftPowerWheel.GetPointOfContactW(), Is.EqualTo(new Vector3D(-1, -1.75, 1)));
    Assert.That(rightPowerWheel.GetPointOfContactW(), Is.EqualTo(new Vector3D(1, 0.25, 1)));

    // Test roll
    leftPowerWheel.Roll(0.25f);
    rightPowerWheel.Roll(0.25f);
    Assert.That(leftWheel.Height, Is.EqualTo(-2));
    Assert.That(rightWheel.Height, Is.EqualTo(-1.75f));
    leftPowerWheel.Roll(0);
    rightPowerWheel.Roll(0);
    Assert.That(leftWheel.Height, Is.EqualTo(-2));
    Assert.That(rightWheel.Height, Is.EqualTo(-2));
  }

  [Test]
  public void WheelBase()
  {
    var transformer = new Program.CoordinatesTransformer(_controller);
    var frontLeftWheel = GetSuspension(new Vector3D(-1, 0, -4), true);
    var frontLeft = new Program.PowerWheel(frontLeftWheel, _wheelBase, transformer);
    var frontRightWheel = GetSuspension(new Vector3D(1, 0, -4), false);
    var frontRight = new Program.PowerWheel(frontRightWheel, _wheelBase, transformer);

    var middleLeftWheel = GetSuspension(new Vector3D(-1, 0, -1), true);
    var middleLeft = new Program.PowerWheel(middleLeftWheel, _wheelBase, transformer);
    var middleRightWheel = GetSuspension(new Vector3D(1, 0, -1), false);
    var middleRight = new Program.PowerWheel(middleRightWheel, _wheelBase, transformer);

    var rearLeftWheel = GetSuspension(new Vector3D(-1, 0, 0), true);
    var rearLeft = new Program.PowerWheel(rearLeftWheel, _wheelBase, transformer);
    var rearRightWheel = GetSuspension(new Vector3D(1, 0, 0), false);
    var rearRight = new Program.PowerWheel(rearRightWheel, _wheelBase, transformer);

    var powerWheels = new List<Tuple<Program.PowerWheel, MyMotorSuspensionMock>>{
        Tuple.Create(frontLeft, frontLeftWheel),
        Tuple.Create(frontRight, frontRightWheel),
        Tuple.Create(middleLeft, middleLeftWheel),
        Tuple.Create(middleRight, middleRightWheel),
        Tuple.Create(rearLeft, rearLeftWheel),
        Tuple.Create(rearRight, rearRightWheel)
      };

    Assert.That(_wheelBase.MinZ, Is.EqualTo(-4));
    Assert.That(_wheelBase.MaxZ, Is.EqualTo(0));

    Assert.That(_wheelBase.CenterOfTurnZ, Is.EqualTo(-2));

    Assert.That(_wheelBase.TurnRadius, Is.EqualTo(5.5));

    Assert.That(_wheelBase.LeftCenterOfTurn, Is.EqualTo(new Vector3D(-5.5, 0, -2)));
    Assert.That(_wheelBase.RightCenterOfTurn, Is.EqualTo(new Vector3D(5.5, 0, -2)));

    int nInverted = 0;
    foreach (Tuple<Program.PowerWheel, MyMotorSuspensionMock> p in powerWheels)
    {
      Program.PowerWheel powerWheel = p.Item1;
      var wheel = p.Item2;
      powerWheel.Turn(-2);
      Assert.That(wheel.InvertSteer, Is.False, $"Wheel {powerWheel.Position}");
      powerWheel.Turn(-0.5);
      if (powerWheel.Position.Z == -1)
      {
        ++nInverted;
        Assert.That(wheel.InvertSteer, $"Wheel {powerWheel.Position}");
      }
      else
      {
        Assert.That(wheel.InvertSteer, Is.False, $"Wheel {powerWheel.Position}");
      }
    }
    Assert.That(nInverted, Is.EqualTo(2));
  }

  [Test]
  public void WheelBaseTurn()
  {
    var transformer = new Program.CoordinatesTransformer(_controller);
    var frontLeftWheel = GetSuspension(new Vector3D(-1.5, 0, -2), true);
    var frontLeft = new Program.PowerWheel(frontLeftWheel, _wheelBase, transformer);
    var frontRightWheel = GetSuspension(new Vector3D(1.5, 0, -2), false);
    var frontRight = new Program.PowerWheel(frontRightWheel, _wheelBase, transformer);

    var middleLeftWheel = GetSuspension(new Vector3D(-1.5, 0, 0), true);
    var middleLeft = new Program.PowerWheel(middleLeftWheel, _wheelBase, transformer);
    var middleRightWheel = GetSuspension(new Vector3D(1.5, 0, 0), false);
    var middleRight = new Program.PowerWheel(middleRightWheel, _wheelBase, transformer);

    var rearLeftWheel = GetSuspension(new Vector3D(-1.5, 0, 2), true);
    var rearLeft = new Program.PowerWheel(rearLeftWheel, _wheelBase, transformer);
    var rearRightWheel = GetSuspension(new Vector3D(1.5, 0, 2), false);
    var rearRight = new Program.PowerWheel(rearRightWheel, _wheelBase, transformer);

    Assert.That(_wheelBase.TurnRadius, Is.EqualTo(6));

    Assert.That(_wheelBase.LeftCenterOfTurn, Is.EqualTo(new Vector3D(-6, 0, 0)));
    Assert.That(_wheelBase.RightCenterOfTurn, Is.EqualTo(new Vector3D(6, 0, 0)));

    _wheelBase.TurnRadiusOverride = 4;

    Assert.That(_wheelBase.TurnRadius, Is.EqualTo(4));

    Assert.That(_wheelBase.LeftCenterOfTurn, Is.EqualTo(new Vector3D(-4, 0, 0)));
    Assert.That(_wheelBase.RightCenterOfTurn, Is.EqualTo(new Vector3D(4, 0, 0)));

    Assert.That(_wheelBase.GetAngle(frontLeft, false), Is.EqualTo(18.43f).Within(TOLERANCE));
    Assert.That(_wheelBase.GetAngle(middleLeft, false), Is.EqualTo(0));
    Assert.That(_wheelBase.GetAngle(rearLeft, false), Is.EqualTo(18.43f).Within(TOLERANCE));
    Assert.That(_wheelBase.GetAngle(frontRight, false), Is.EqualTo(45));
    Assert.That(_wheelBase.GetAngle(middleRight, false), Is.EqualTo(0));
    Assert.That(_wheelBase.GetAngle(rearRight, false), Is.EqualTo(45));

    Assert.That(_wheelBase.GetAngle(frontLeft, true), Is.EqualTo(45));
    Assert.That(_wheelBase.GetAngle(middleLeft, true), Is.EqualTo(0));
    Assert.That(_wheelBase.GetAngle(rearLeft, true), Is.EqualTo(45));
    Assert.That(_wheelBase.GetAngle(frontRight, true), Is.EqualTo(18.43f).Within(TOLERANCE));
    Assert.That(_wheelBase.GetAngle(middleRight, true), Is.EqualTo(0));
    Assert.That(_wheelBase.GetAngle(rearRight, true), Is.EqualTo(18.43f).Within(TOLERANCE));
  }

  [Test]
  public void Everything()
  {
    var transformer = new Program.CoordinatesTransformer(_controller);

    var ini = new VRage.Game.ModAPI.Ingame.Utilities.MyIni();
    ini.TryParse(@"");

    var saveManager = Program.Process.CreateManager(null);

    var command = new Program.CommandLine("mock", null, saveManager);
    GetSuspension(new Vector3D(-1, 0, -1), true);
    GetSuspension(new Vector3D(1, 0, -1), false);
    GetSuspension(new Vector3D(-1, 0, 1), true);
    GetSuspension(new Vector3D(1, 0, 1), false);

    var wc = new Program.WheelsController(command, _controller, _gts, ini, saveManager, transformer);

    Assert.That(wc.GetContactPlaneW(), Is.EqualTo(new Vector3D(0, 1, 0)));

    Assert.That(wc.GetPointOfContactW(new Vector3D(0, 0, -1)), Is.EqualTo(new Vector3D(0, -1.75, -1)));

    wc.SetPosition("0.5"); // too anoying to test
  }

  public void SetStrength()
  {
    // TODO test wheel.SetStrength
  }

  public void GetForce()
  {
    // TODO test wheel.GetForce()
  }
}
