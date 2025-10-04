namespace Utilities.Mocks;

using System;
using System.Collections.Generic;
using Utilities.Mocks.Base;
public static class BlockHelper
{
  public static void GetBlocksOfType<S, T>(List<S> allBlocks, List<T> filteredBlocks, Func<T, bool> collect = null)
    where T : class
    where S : MyCubeBlockMock
  {
    foreach (var block in allBlocks)
    {
      if (block is T tBlock && (collect == null || collect(tBlock)))
      {
        filteredBlocks.Add(tBlock);
      }
    }
  }
}
