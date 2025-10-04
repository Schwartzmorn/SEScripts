namespace Utilities.Mocks.Blocks;

using System;
using Sandbox.ModAPI.Ingame;
using Utilities.Mocks.Base;
using VRageMath;

public class MyMotorSuspensionMock : MyMechanicalConnectionBlockMock, IMyMotorSuspension
{
  public MyMotorSuspensionMock(MyCubeGridMock cubeGridMock) : base(cubeGridMock)
  {
    var topGrid = new MyCubeGridMock(cubeGridMock.GridTerminalSystem)
    {
      GridSizeEnum = cubeGridMock.GridSizeEnum
    };
    Top = new MyAttachableBlockMock(topGrid, this)
    {
      Max = Position + new Vector3I(1, 1, 1),
      Min = Position + new Vector3I(-1, -1, -1),
    };
    TerminalProperties = [
        new TerminalPropertyMock<MyMotorSuspensionMock, float>("Propulsion override", b => b.PropulsionOverride, (b, v) => b.PropulsionOverride = v),
        new TerminalPropertyMock<MyMotorSuspensionMock, float>("Steer override", b => b.SteeringOverride, (b, v) => b.SteeringOverride = v),
        new TerminalPropertyMock<MyMotorSuspensionMock, float>("MaxSteerAngle", b => b.MaxSteerAngle, (b, v) => b.MaxSteerAngle = v)
    ];
  }

  public MyMotorSuspensionMock(MyCubeGridMock cubeGridMock, IMyAttachableTopBlock top) : base(cubeGridMock)
  {
    Top = top;
  }

  public bool Steering { get; set; }
  public bool Propulsion { get; set; }
  public bool InvertSteer { get; set; }
  public bool InvertPropulsion { get; set; }
  public bool IsParkingEnabled { get; set; }

  public float Damping => throw new System.NotImplementedException();

  public float Strength { get; set; }
  public float Friction { get; set; }
  public float Power { get; set; }
  private float _height;
  public float Height { get => _height; set { _height = MathHelper.Clamp(value, -2, 2); } }

  public float SteerAngle { get; set; }

  public float MaxSteerAngle { get; set; }

  public float SteerSpeed => throw new System.NotImplementedException();

  public float SteerReturnSpeed => throw new System.NotImplementedException();

  public float SuspensionTravel => throw new System.NotImplementedException();

  public bool Brake { get; set; }
  public bool AirShockEnabled { get; set; }
  public float SteeringOverride { get; set; }
  public float PropulsionOverride { get; set; }

  public override bool IsLocked => throw new System.NotImplementedException();

  public override void Tick()
  {
    throw new System.NotImplementedException();
  }
}
