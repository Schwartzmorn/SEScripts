namespace Utilities.Mocks.Blocks;

using Sandbox.ModAPI.Ingame;
using System;
using Utilities.Mocks.Base;

public class MyDoorMock : MyFunctionalBlockMock, IMyDoor
{
  public MyDoorMock(MyCubeGridMock cubeGridMock) : base(cubeGridMock)
  {
    CustomName = "Door " + EntityId;
  }

  // Average number of ticks it takes to fully open or close the door
  public float DoorTicksToClose { get; set; } = 50f;

  public bool Open => throw new NotSupportedException("Obsolete: use \"OpenRatio\" property instead");

  public DoorStatus Status { get; set; } = DoorStatus.Closed;

  public float OpenRatio { get; set; } = 0f;

  public void CloseDoor()
  {
    if (!Enabled)
      return;
    if (Status == DoorStatus.Closed || Status == DoorStatus.Closing)
      return;
    Status = DoorStatus.Closing;
  }

  public void OpenDoor()
  {
    if (!Enabled)
      return;
    if (Status == DoorStatus.Open || Status == DoorStatus.Opening)
      return;
    Status = DoorStatus.Opening;
  }

  public void ToggleDoor()
  {
    if (Status == DoorStatus.Closed || Status == DoorStatus.Closing)
      OpenDoor();
    else if (Status == DoorStatus.Open || Status == DoorStatus.Opening)
      CloseDoor();
  }

  public override void Tick()
  {
    if (!Enabled)
      return;
    if (Status == DoorStatus.Opening)
    {
      OpenRatio += 1f / DoorTicksToClose;
      if (OpenRatio >= 1f)
      {
        OpenRatio = 1f;
        Status = DoorStatus.Open;
      }
    }
    else if (Status == DoorStatus.Closing)
    {
      OpenRatio -= 1f / DoorTicksToClose;
      if (OpenRatio <= 0f)
      {
        OpenRatio = 0f;
        Status = DoorStatus.Closed;
      }
    }
  }
}
