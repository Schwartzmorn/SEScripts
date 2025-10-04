namespace Utilities.Mocks.Base;

using Sandbox.ModAPI.Ingame;

public abstract class MyFunctionalBlockMock(MyCubeGridMock cubeGridMock) : MyTerminalBlockMock(cubeGridMock), IMyFunctionalBlock
{
  public bool Enabled { get; set; } = true;

  public void RequestEnable(bool enable)
  {
    throw new System.NotSupportedException("Obsolete: use \"Enabled\" property instead");
  }
}
