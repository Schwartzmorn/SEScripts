namespace Utilities.Mocks.Base;

using Sandbox.ModAPI.Ingame;

public abstract class MyShipToolBaseMock(MyCubeGridMock cubeGrid) : MyFunctionalBlockMock(cubeGrid), IMyShipToolBase
{
  public bool UseConveyorSystem { get; set; }

  public bool IsActivated { get; set; }
}
