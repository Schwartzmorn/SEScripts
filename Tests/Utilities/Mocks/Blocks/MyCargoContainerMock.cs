namespace Utilities.Mocks.Blocks;

using Sandbox.ModAPI.Ingame;
using Utilities.Mocks.Base;

public class MyCargoContainerMock : MyTerminalBlockMock, IMyCargoContainer
{
  public MyCargoContainerMock(MyCubeGridMock cubeGrid) : base(cubeGrid)
  {
    InventoryMocks.Add(new MyInventoryMock(this));
  }

  public override void Tick()
  {
  }
}
