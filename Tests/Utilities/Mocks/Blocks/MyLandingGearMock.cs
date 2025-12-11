namespace Utilities.Mocks.Blocks;

using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using Utilities.Mocks.Base;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

public class MyLandingGearMock : MyFunctionalBlockMock, IMyLandingGear
{
  public MyLandingGearMock(MyCubeGridMock cubeGridMock) : base(cubeGridMock)
  {
  }

  public bool IsBreakable
  {
    get => throw new NotSupportedException("Obsolete, not breakable anymore.");
  }

  public bool IsLocked { get; private set; }

  public bool AutoLock { get; set; }
  public bool IsParkingEnabled { get; set; }

  /// <summary>
  /// Mock property to simulate whether the landing gear is in range to lock or not
  /// </summary>
  public bool CanLockMock { get; set; }

  public LandingGearMode LockMode
  {
    get
    {
      if (IsLocked)
      {
        return LandingGearMode.Locked;
      }
      if (CanLockMock)
      {
        return LandingGearMode.ReadyToLock;
      }
      return LandingGearMode.Unlocked;
    }
  }

  public void Lock()
  {
    if (Enabled && LockMode == LandingGearMode.ReadyToLock)
    {
      IsLocked = true;
    }
  }

  public void ResetAutoLock()
  {
    throw new NotImplementedException();
  }

  public void ToggleLock()
  {
    if (LockMode == LandingGearMode.ReadyToLock)
    {
      IsLocked = true;
    }
    if (LockMode == LandingGearMode.Locked)
    {
      IsLocked = false;
    }
  }

  public void Unlock()
  {
    IsLocked = false;
  }

  public override void Tick()
  {
  }
}
