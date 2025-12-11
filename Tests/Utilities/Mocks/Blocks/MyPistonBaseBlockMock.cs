namespace Utilities.Mocks.Blocks;

using Sandbox.ModAPI.Ingame;
using System;
using Utilities.Mocks.Base;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

public class MyPistonBaseMock : MyMechanicalConnectionBlockMock, IMyPistonBase
{
  public MyPistonBaseMock(MyCubeGridMock cubeGridMock) : base(cubeGridMock)
  {
    _maxLimit = HighestPosition;
  }

  private float _velocity = 1f;
  public float Velocity
  {
    get => _velocity;
    set => _velocity = Math.Clamp(value, -MaxVelocity, MaxVelocity);
  }

  public float MaxVelocity => 5f;

  private float _minLimit;
  public float MinLimit
  {
    get => _minLimit;
    set => _minLimit = MathHelper.Clamp(value, 0, _maxLimit);
  }

  private float _maxLimit;
  public float MaxLimit
  {
    get => _maxLimit;
    set => _maxLimit = MathHelper.Clamp(value, _minLimit, HighestPosition);
  }

  public float LowestPosition => 0;

  public float HighestPosition => CubeGrid.GridSizeEnum == MyCubeSize.Small ? 2 : 10;

  private float _currentPosition;
  public float CurrentPosition
  {
    get => _currentPosition;
    set => _currentPosition = Math.Clamp(value, LowestPosition, HighestPosition);
  }

  public float NormalizedPosition => CurrentPosition / (MaxLimit - MinLimit);

  public PistonStatus Status
  {
    get
    {
      if (Velocity < 0)
      {
        return NormalizedPosition <= 0 ? PistonStatus.Retracted : PistonStatus.Retracting;
      }

      if (Velocity > 0)
      {
        return NormalizedPosition >= 1 ? PistonStatus.Extended : PistonStatus.Extending;
      }

      return PistonStatus.Stopped;
    }
  }

  public void Extend()
  {
    if (Enabled)
    {
      Velocity = Math.Abs(Velocity);
    }
  }

  public void MoveToPosition(float extent, float speed)
  {
    throw new NotImplementedException();
  }

  public void Retract()
  {
    if (Enabled)
    {
      Velocity = -Math.Abs(Velocity);
    }
  }

  public void Reverse()
  {
    if (Enabled)
    {
      Velocity *= -1;
    }
  }

  public override void Tick()
  {
    if (Enabled)
    {
      CurrentPosition += Velocity / 100;
      CurrentPosition = Math.Clamp(CurrentPosition, MinLimit, MaxLimit);
    }
  }
}
