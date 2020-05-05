using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;
using IngameScript.Mockups;
using IngameScript.Mockups.Blocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestRunner;

namespace IngameScript.MDK {
  class PowerWheelTest {
    MockShipController controller;
    Program.WheelBase wheelBase;
    public void BeforeEach() {
      // this controller's coodrinates will be the same as the world coordinates
      this.controller = new MockShipController {
        WorldPosition = Vector3D.Zero,
        WorldMatrix = MatrixD.Identity
      };
      this.wheelBase = new Program.WheelBase();
    }

    public static MockMotorSuspension GetSuspension(Vector3D pos, bool left, MockCubeGrid grid = null) {
      return new MockMotorSuspension {
        CubeGrid = grid ?? new MockCubeGrid {
          GridSizeEnum = VRage.Game.MyCubeSize.Small
        },
        CustomName = "Power",
        DisplayNameText = "Power",
        MaxSteerAngle = 30f,
        Strength = 1,
        Top = new MockAttachableTopBlock {
          Max = new Vector3I(1, 1, 1),
          Min = new Vector3I(-1, -1, -1),
          WorldPosition = pos + new Vector3D(0, -1, 0)
        },
        // This should be the world matrix in case the wheels are in the correct orientation
        WorldMatrix = new MatrixD(
                     0,     0, left ? 1 : -1, 0,
         left ? -1 : 1,     0,             0, 0,
                     0,    -1,             0, 0,
                 pos.X, pos.Y,         pos.Z, 1),
        WorldPosition = pos
      };
    }

    public void Create() {
      var transformer = new Program.CoordinatesTransformer(this.controller);
      MockMotorSuspension leftWheel = GetSuspension(new Vector3D(-1, 0, 1), true);
      MockMotorSuspension rightWheel = GetSuspension(new Vector3D(1, 0, 1), false);
      (rightWheel.Top as MockAttachableTopBlock).WorldPosition = new Vector3D(1, 1, 1);
      var leftPowerWheel = new Program.PowerWheel(leftWheel, this.wheelBase, transformer);
      var rightPowerWheel = new Program.PowerWheel(rightWheel, this.wheelBase, transformer);

      Assert.AreEqual(3, leftPowerWheel.WheelSize);
      // We are in the wheel's referential, so it should be -1, 0, 1 but it is offset to take into account the point around which the wheel actually rotates
      Assert.AreEqual(new Vector3D(-1.5, 0, 1), leftPowerWheel.Position);
      Assert.AreEqual(205, leftPowerWheel.Mass);
      Assert.AreEqual(0.25, leftPowerWheel.GetCompressionRatio());

      Assert.AreEqual(new Vector3D(1.5, 0, 1), rightPowerWheel.Position);
      Assert.AreEqual(0.75, rightPowerWheel.GetCompressionRatio());

      // to go forward, we need to invert the power on the right wheels
      leftPowerWheel.Power = 1;
      rightPowerWheel.Power = 1;
      Assert.AreEqual(1, leftWheel.PropulsionOverride);
      Assert.AreEqual(-1, rightWheel.PropulsionOverride);

      // center of mass is forward of the wheels
      leftPowerWheel.Strafe(-1);
      rightPowerWheel.Strafe(-1);
      Assert.IsTrue(leftWheel.InvertSteer);
      Assert.IsTrue(rightWheel.InvertSteer);
      leftPowerWheel.Strafe(1);
      rightPowerWheel.Strafe(1);
      Assert.IsFalse(leftWheel.InvertSteer);
      Assert.IsFalse(rightWheel.InvertSteer);

      // Test GetPointOfContactW
      Assert.AreEqual(new Vector3D(-1, -1.75, 1), leftPowerWheel.GetPointOfContactW());
      Assert.AreEqual(new Vector3D(1, 0.25, 1), rightPowerWheel.GetPointOfContactW());

      // Test roll
      leftPowerWheel.Roll(0.25f);
      rightPowerWheel.Roll(0.25f);
      Assert.AreEqual(-2, leftWheel.Height);
      Assert.AreEqual(-1.75f, rightWheel.Height);
      leftPowerWheel.Roll(0);
      rightPowerWheel.Roll(0);
      Assert.AreEqual(-2, leftWheel.Height);
      Assert.AreEqual(-2, rightWheel.Height);
    }

    public void WheelBase() {
      var transformer = new Program.CoordinatesTransformer(this.controller);
      MockMotorSuspension frontLeftWheel = GetSuspension(new Vector3D(-1, 0, -4), true);
      var frontLeft = new Program.PowerWheel(frontLeftWheel, this.wheelBase, transformer);
      MockMotorSuspension frontRightWheel = GetSuspension(new Vector3D(1, 0, -4), false);
      var frontRight = new Program.PowerWheel(frontRightWheel, this.wheelBase, transformer);

      MockMotorSuspension middleLeftWheel = GetSuspension(new Vector3D(-1, 0, -1), true);
      var middleLeft = new Program.PowerWheel(middleLeftWheel, this.wheelBase, transformer);
      MockMotorSuspension middleRightWheel = GetSuspension(new Vector3D(1, 0, -1), false);
      var middleRight = new Program.PowerWheel(middleRightWheel, this.wheelBase, transformer);

      MockMotorSuspension rearLeftWheel = GetSuspension(new Vector3D(-1, 0, 0), true);
      var rearLeft = new Program.PowerWheel(rearLeftWheel, this.wheelBase, transformer);
      MockMotorSuspension rearRightWheel = GetSuspension(new Vector3D(1, 0, 0), false);
      var rearRight = new Program.PowerWheel(rearRightWheel, this.wheelBase, transformer);

      var powerWheels = new List<Tuple<Program.PowerWheel, MockMotorSuspension>>{ 
        Tuple.Create(frontLeft, frontLeftWheel),
        Tuple.Create(frontRight, frontRightWheel),
        Tuple.Create(middleLeft, middleLeftWheel),
        Tuple.Create(middleRight, middleRightWheel),
        Tuple.Create(rearLeft, rearLeftWheel),
        Tuple.Create(rearRight, rearRightWheel)
      };

      Assert.AreEqual(-4, this.wheelBase.MinZ);
      Assert.AreEqual(0, this.wheelBase.MaxZ);

      Assert.AreEqual(-2, this.wheelBase.CenterOfTurnZ);

      Assert.AreEqual(5.5, this.wheelBase.TurnRadius);

      Assert.AreEqual(new Vector3D(-5.5, 0, -2), this.wheelBase.LeftCenterOfTurn);
      Assert.AreEqual(new Vector3D(5.5, 0, -2), this.wheelBase.RightCenterOfTurn);

      int nInverted = 0;
      foreach (Tuple<Program.PowerWheel, MockMotorSuspension> p in powerWheels) {
        Program.PowerWheel powerWheel = p.Item1;
        MockMotorSuspension wheel = p.Item2;
        powerWheel.Turn(-2);
        Assert.IsFalse(wheel.InvertSteer, $"Wheel {powerWheel.Position}");
        powerWheel.Turn(-0.5);
        if (powerWheel.Position.Z == -1) {
          ++nInverted;
          Assert.IsTrue(wheel.InvertSteer, $"Wheel {powerWheel.Position}");
        } else {
          Assert.IsFalse(wheel.InvertSteer, $"Wheel {powerWheel.Position}");
        }
      }
      Assert.AreEqual(2, nInverted);
    }

    public void WheelBaseTurn() {
      var transformer = new Program.CoordinatesTransformer(this.controller);
      MockMotorSuspension frontLeftWheel = GetSuspension(new Vector3D(-1.5, 0, -2), true);
      var frontLeft = new Program.PowerWheel(frontLeftWheel, this.wheelBase, transformer);
      MockMotorSuspension frontRightWheel = GetSuspension(new Vector3D(1.5, 0, -2), false);
      var frontRight = new Program.PowerWheel(frontRightWheel, this.wheelBase, transformer);

      MockMotorSuspension middleLeftWheel = GetSuspension(new Vector3D(-1.5, 0, 0), true);
      var middleLeft = new Program.PowerWheel(middleLeftWheel, this.wheelBase, transformer);
      MockMotorSuspension middleRightWheel = GetSuspension(new Vector3D(1.5, 0, 0), false);
      var middleRight = new Program.PowerWheel(middleRightWheel, this.wheelBase, transformer);

      MockMotorSuspension rearLeftWheel = GetSuspension(new Vector3D(-1.5, 0, 2), true);
      var rearLeft = new Program.PowerWheel(rearLeftWheel, this.wheelBase, transformer);
      MockMotorSuspension rearRightWheel = GetSuspension(new Vector3D(1.5, 0, 2), false);
      var rearRight = new Program.PowerWheel(rearRightWheel, this.wheelBase, transformer);

      Assert.AreEqual(6, this.wheelBase.TurnRadius);

      Assert.AreEqual(new Vector3D(-6, 0, 0), this.wheelBase.LeftCenterOfTurn);
      Assert.AreEqual(new Vector3D(6, 0, 0), this.wheelBase.RightCenterOfTurn);

      this.wheelBase.TurnRadiusOverride = 4;

      Assert.AreEqual(4, this.wheelBase.TurnRadius);

      Assert.AreEqual(new Vector3D(-4, 0, 0), this.wheelBase.LeftCenterOfTurn);
      Assert.AreEqual(new Vector3D(4, 0, 0), this.wheelBase.RightCenterOfTurn);

      Asserts.AreClose(18.43f, 0.01f, this.wheelBase.GetAngle(frontLeft, false));
      Assert.AreEqual(0, this.wheelBase.GetAngle(middleLeft, false));
      Asserts.AreClose(18.43f, 0.01f, this.wheelBase.GetAngle(rearLeft, false));
      Assert.AreEqual(45, this.wheelBase.GetAngle(frontRight, false));
      Assert.AreEqual(0, this.wheelBase.GetAngle(middleRight, false));
      Assert.AreEqual(45, this.wheelBase.GetAngle(rearRight, false));

      Assert.AreEqual(45, this.wheelBase.GetAngle(frontLeft, true));
      Assert.AreEqual(0, this.wheelBase.GetAngle(middleLeft, true));
      Assert.AreEqual(45, this.wheelBase.GetAngle(rearLeft, true));
      Asserts.AreClose(18.43f, 0.01f, this.wheelBase.GetAngle(frontRight, true));
      Assert.AreEqual(0, this.wheelBase.GetAngle(middleRight, true));
      Asserts.AreClose(18.43f, 0.01f, this.wheelBase.GetAngle(rearRight, true));
    }

    public void SetStrength() {
      // TODO test wheel.SetStrength
    }

    public void GetForce() {
      // TODO test wheel.GetForce()
    }
  }
}
