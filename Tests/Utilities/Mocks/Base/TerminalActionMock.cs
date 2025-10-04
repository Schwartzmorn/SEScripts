namespace Utilities.Mocks.Base;

using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using System.Text;
using VRage.Collections;
using VRage.Game.ModAPI.Ingame;

public class TerminalActionMock : ITerminalAction
{
  public string Id => throw new System.NotImplementedException();

  public string Icon => "Some_Icon";

  public StringBuilder Name => throw new System.NotImplementedException();

  public void Apply(IMyCubeBlock block)
  {
    throw new System.NotImplementedException();
  }

  public void Apply(IMyCubeBlock block, ListReader<TerminalActionParameter> terminalActionParameters)
  {
    throw new System.NotImplementedException();
  }

  public bool IsEnabled(IMyCubeBlock block)
  {
    throw new System.NotImplementedException();
  }

  public void WriteValue(IMyCubeBlock block, StringBuilder appendTo)
  {
    throw new System.NotImplementedException();
  }
}
