namespace Utilities.Mocks;

using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using Utilities.Mocks.Base;
using VRage.Game.ModAPI.Ingame;

public class MyGridTerminalSystemMock(TestBed testBed) : IMyGridTerminalSystem
{
  private readonly Dictionary<string, MyBlockGroupMock> _blockGroups = [];
  public List<MyCubeGridMock> CubeGridMocks { get; } = [];

  public TestBed TestBed { get; } = testBed;

  public bool CanAccess(IMyTerminalBlock block, MyTerminalAccessScope scope = MyTerminalAccessScope.All)
  {
    throw new NotImplementedException();
  }

  public bool CanAccess(IMyCubeGrid grid, MyTerminalAccessScope scope = MyTerminalAccessScope.All)
  {
    throw new NotImplementedException();
  }

  public void GetBlockGroups(List<IMyBlockGroup> blockGroups, Func<IMyBlockGroup, bool> collect = null)
  {
    foreach (var group in _blockGroups.Values)
    {
      if (collect == null || collect(group))
      {
        blockGroups.Add(group);
      }
    }
  }
  public IMyBlockGroup GetBlockGroupWithName(string name)
  {
    return _blockGroups.TryGetValue(name, out var group) ? group : null;
  }

  public void GetBlocks(List<IMyTerminalBlock> blocks)
  {
    foreach (var grid in CubeGridMocks)
    {
      blocks.AddRange(grid.TerminalBlockMocks);
    }
  }

  public void GetBlocksOfType<T>(List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect = null) where T : class
  {
    foreach (var grid in CubeGridMocks)
    {
      BlockHelper.GetBlocksOfType(grid.TerminalBlockMocks, blocks, collect);
    }
  }

  public void GetBlocksOfType<T>(List<T> blocks, Func<T, bool> collect = null) where T : class
  {
    foreach (var grid in CubeGridMocks)
    {
      BlockHelper.GetBlocksOfType(grid.TerminalBlockMocks, blocks, collect);
    }
  }

  public IMyTerminalBlock GetBlockWithId(long id)
  {
    return CubeGridMocks.SelectMany(g => g.TerminalBlockMocks.FindAll(b => b.EntityId == id)).FirstOrDefault();
  }

  public IMyTerminalBlock GetBlockWithName(string name)
  {
    return CubeGridMocks.SelectMany(g => g.TerminalBlockMocks.FindAll(b => b.CustomName == name)).FirstOrDefault();
  }

  public void SearchBlocksOfName(string name, List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect = null)
  {
    CubeGridMocks.SelectMany(g => g.TerminalBlockMocks.FindAll(b => (b.CustomName == name) && (collect == null || collect(b)))).ForEach(blocks.Add);
  }
}
