namespace Utilities.Mocks.Base;

using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;


public abstract class MyTerminalBlockMock : MyCubeBlockMock, IMyTerminalBlock
{
  public MyTerminalBlockMock(MyCubeGridMock myCubeGridMock) : base(myCubeGridMock)
  {
    myCubeGridMock.TerminalBlockMocks.Add(this);
  }

  public abstract void Tick();

  public string CustomName { get; set; } = "";


  public string CustomNameWithFaction => throw new NotImplementedException();

  public string DetailedInfo { get; set; } = "";


  public string CustomInfo { get; set; }

  public string CustomData { get; set; } = "";

  public bool ShowOnHUD { get; set; } = false;
  public bool ShowInTerminal { get; set; } = true;
  public bool ShowInToolbarConfig { get; set; } = true;
  public bool ShowInInventory { get; set; } = true;

  protected List<ITerminalProperty> TerminalProperties { get; init; } = [];

  public void GetActions(List<ITerminalAction> resultList, Func<ITerminalAction, bool> collect = null)
  {
    throw new NotImplementedException();
  }

  public ITerminalAction GetActionWithName(string name)
  {
    throw new NotImplementedException();
  }

  public void GetProperties(List<ITerminalProperty> resultList, Func<ITerminalProperty, bool> collect = null)
  {
    if (resultList == null)
    {
      return;
    }
    resultList.Clear();

    TerminalProperties.Where(t => collect == null || collect(t)).ForEach(resultList.Add);
  }

  public ITerminalProperty GetProperty(string id)
  {
    return TerminalProperties.Where(t => t.Id == id).FirstOrDefault();
  }

  public bool HasLocalPlayerAccess()
  {
    throw new NotImplementedException();
  }

  public bool HasNobodyPlayerAccessToBlock()
  {
    throw new NotImplementedException();
  }

  public bool HasPlayerAccess(long playerId, MyRelationsBetweenPlayerAndBlock defaultNoUser = MyRelationsBetweenPlayerAndBlock.NoOwnership)
  {
    throw new NotImplementedException();
  }

  public bool HasPlayerAccessWithNobodyCheck(long playerId, bool isForPB = false)
  {
    throw new NotImplementedException();
  }

  public bool IsSameConstructAs(IMyTerminalBlock other)
  {
    return other.CubeGrid.IsSameConstructAs(other.CubeGrid);
  }

  public void SearchActionsOfName(string name, List<ITerminalAction> resultList, Func<ITerminalAction, bool> collect = null)
  {
    throw new NotImplementedException();
  }

  public void SetCustomName(string text)
  {
    throw new NotSupportedException("Obsolete: use CustomName property instead");
  }

  public void SetCustomName(StringBuilder text)
  {
    throw new NotSupportedException("Obsolete: use CustomName property instead");
  }
}
