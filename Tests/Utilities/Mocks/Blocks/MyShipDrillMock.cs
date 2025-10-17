namespace Utilities.Mocks.Blocks;

using Sandbox.ModAPI.Ingame;
using Utilities.Mocks.Base;

public class MyShipDrillMock(MyCubeGridMock cubeGrid) : MyShipToolBaseMock(cubeGrid), IMyShipDrill
{
  public bool TerrainClearingMode { get; set; }

  public override void Tick()
  {
  }
}
