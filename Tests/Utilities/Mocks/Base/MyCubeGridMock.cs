namespace Utilities.Mocks.Base;

using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

public class MyCubeGridMock : MyEntityMock, IMyCubeGrid
{
  public MyCubeGridMock(MyGridTerminalSystemMock gridTerminalSystem) : base(gridTerminalSystem.TestBed.GetNextEntityId())
  {
    GridTerminalSystem = gridTerminalSystem;
    GridTerminalSystem.CubeGridMocks.Add(this);
  }

  public List<MyTerminalBlockMock> TerminalBlockMocks { get; } = [];
  public List<MyCubeBlockMock> CubeBlockMocks { get; } = [];

  public MyGridTerminalSystemMock GridTerminalSystem { get; set; }

  public string CustomName { get; set; } = "CubeGrid";

  public float GridSize
  {
    get
    {
      if (GridSizeEnum == MyCubeSize.Small)
      {
        return 0.5f;
      }
      return 2.5f;
    }
  }

  public MyCubeSize GridSizeEnum { get; init; } = MyCubeSize.Large;

  public bool IsStatic { get; set; } = false;

  public Vector3I Max
  {
    get
    {
      if (TerminalBlockMocks.Count == 0)
      {
        return new Vector3I();
      }
      // This assumes there are only terminal blocks
      return new Vector3I(
        TerminalBlockMocks.Select(b => b.Max.X).Max(),
        TerminalBlockMocks.Select(b => b.Max.Y).Max(),
        TerminalBlockMocks.Select(b => b.Max.Z).Max()
      );
    }
  }

  public Vector3I Min
  {
    get
    {
      if (TerminalBlockMocks.Count == 0)
      {
        return new Vector3I();
      }
      // This assumes there are only terminal blocks
      return new Vector3I(
        TerminalBlockMocks.Select(b => b.Min.X).Max(),
        TerminalBlockMocks.Select(b => b.Min.Y).Max(),
        TerminalBlockMocks.Select(b => b.Min.Z).Max()
      );
    }
  }

  public Vector3 LinearVelocity => throw new System.NotImplementedException();

  public float Speed => LinearVelocity.Length();

  public bool CubeExists(Vector3I pos)
  {
    // This assumes there are only terminal blocks
    foreach (var block in TerminalBlockMocks)
    {
      if (block.Position == pos)
      {
        return true;
      }
    }
    return false;
  }

  public IMySlimBlock GetCubeBlock(Vector3I pos)
  {
    // As far as I could experiment, this method only returns terminal blocks
    throw new System.NotImplementedException();
  }

  public Vector3D GridIntegerToWorld(Vector3I gridCoords)
  {
    throw new System.NotImplementedException();
  }

  public bool IsSameConstructAs(IMyCubeGrid other)
  {
    if (this == other)
    {
      return true;
    }

    return _findGrid(other.EntityId, [EntityId]);
  }

  private bool _findGrid(long otherGridId, HashSet<long> foundGrids)
  {
    var gridsToExplore = new List<MyCubeGridMock>();

    var baseBlocks = new List<MyMechanicalConnectionBlockMock>();
    BlockHelper.GetBlocksOfType(TerminalBlockMocks, baseBlocks, b => b.Top != null);

    foreach (var block in baseBlocks)
    {
      long currentGridId = block.Top.CubeGrid.EntityId;
      if (currentGridId == otherGridId)
      {
        return true;
      }
      if (!foundGrids.Contains(currentGridId))
      {
        foundGrids.Add(currentGridId);
        gridsToExplore.Add(block.Top as MyCubeGridMock);
      }
    }

    var topBlocks = new List<MyAttachableBlockMock>();
    BlockHelper.GetBlocksOfType(CubeBlockMocks, topBlocks, b => b.Base != null);

    foreach (var block in topBlocks)
    {
      long currentGridId = block.Base.CubeGrid.EntityId;
      if (currentGridId == otherGridId)
      {
        return true;
      }
      if (!foundGrids.Contains(currentGridId))
      {
        foundGrids.Add(currentGridId);
        gridsToExplore.Add(block.Base as MyCubeGridMock);
      }
    }

    return gridsToExplore.Any(grid => grid._findGrid(otherGridId, foundGrids));
  }

  public Vector3I WorldToGridInteger(Vector3D coords)
  {
    throw new System.NotImplementedException();
  }
}
