namespace Utilities.Mocks.Blocks;

using Sandbox.ModAPI.Ingame;
using System;
using Utilities.Mocks.Base;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

public class MyMotorStatorMock : MyMechanicalConnectionBlockMock, IMyMotorStator
{
  public MyMotorStatorMock(MyCubeGridMock cubeGridMock, float angle = 0) : base(cubeGridMock)
  {
    Angle = Math.Clamp(angle, 0, MathHelper.TwoPi);
  }

  public float Angle { get; set; }

  // values from 0 to 33 600 000, but not enforced ?
  public float Torque { get; set; }
  public float BrakingTorque { get; set; }
  public float TargetVelocityRad
  {
    get => TargetVelocityRPM * MathHelper.RPMToRadiansPerSecond;
    set => TargetVelocityRPM = value * MathHelper.RadiansPerSecondToRPM;
  }
  // values in [-60,60] for small rotors and [-30,30] for big ones
  private float _targetVelocityRPM;
  public float TargetVelocityRPM
  {
    get => _targetVelocityRPM;
    set
    {
      if (CubeGridMock == null)
        return;
      if (CubeGridMock.GridSizeEnum == MyCubeSize.Small)
      {
        _targetVelocityRPM = Math.Clamp(value, -60, 60);
      }
      else
      {
        _targetVelocityRPM = Math.Clamp(value, -30, 30);
      }
    }
  }
  private float _lowerLimitRad = float.NegativeInfinity;
  public float LowerLimitRad
  {
    get => _lowerLimitRad;
    set
    {
      if (value <= -MathHelper.TwoPi)
        _lowerLimitRad = float.NegativeInfinity;
      else
        _lowerLimitRad = Math.Clamp(value, -MathHelper.TwoPi, MathHelper.TwoPi);
    }
  }
  public float LowerLimitDeg
  {
    get => MathHelper.ToDegrees(LowerLimitRad);
    set => LowerLimitRad = MathHelper.ToRadians(value);
  }
  private float _upperLimitRad = float.PositiveInfinity;
  public float UpperLimitRad
  {
    get => _upperLimitRad;
    set
    {
      if (value >= MathHelper.TwoPi)
        _upperLimitRad = float.PositiveInfinity;
      else
        _upperLimitRad = Math.Clamp(value, -MathHelper.TwoPi, MathHelper.TwoPi);
    }
  }
  public float UpperLimitDeg
  {
    get => MathHelper.ToDegrees(UpperLimitRad);
    set => UpperLimitRad = MathHelper.ToRadians(value);
  }
  // -40 to 20 cm if large grid is attached, -11 to +11 otherwise
  private float _displacement;
  public float Displacement
  {
    get => _displacement;
    set
    {
      if (TopGrid == null)
        return;
      if (TopGrid.GridSizeEnum == MyCubeSize.Small)
      {
        _displacement = Math.Clamp(0f, -0.11f, 0.11f);
      }
      else
      {
        _displacement = Math.Clamp(0f, -0.4f, 0.2f);
      }
    }
  }
  public bool RotorLock { get; set; }

  public void RotateToAngle(MyRotationDirection dir, float desiredAng, float velAbsRpm)
  {
    throw new NotImplementedException();
  }

  public override void Tick()
  {
  }
}
