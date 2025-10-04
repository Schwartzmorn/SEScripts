using SpaceEngineers.Game.ModAPI.Ingame;
using Utilities.Mocks.Base;

namespace Utilities.Mocks.Blocks;

public class MySolarPanelMock(MyCubeGridMock cubeGridMock) : MyPowerProducerMock(cubeGridMock), IMySolarPanel
{
  public override string ProducerType => "Solar Panel";
}
