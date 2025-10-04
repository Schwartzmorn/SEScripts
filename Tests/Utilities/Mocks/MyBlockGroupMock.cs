namespace Utilities.Mocks;

using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using Utilities.Mocks.Base;

public class MyBlockGroupMock(string name) : IMyBlockGroup
{
  public string Name { get; } = name;
  private readonly List<MyTerminalBlockMock> _blocks = [];

  public void Add(MyTerminalBlockMock block)
  {
    _blocks.Add(block);
  }

  public void GetBlocks(List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect = null)
  {
    BlockHelper.GetBlocksOfType(_blocks, blocks, collect);
  }

  public void GetBlocksOfType<T>(List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect = null) where T : class
  {
    BlockHelper.GetBlocksOfType(_blocks, blocks, collect);
  }

  public void GetBlocksOfType<T>(List<T> blocks, Func<T, bool> collect = null) where T : class
  {
    BlockHelper.GetBlocksOfType(_blocks, blocks, collect);
  }
}
