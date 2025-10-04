namespace Utilities.Mocks.Base;

using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRage.ObjectBuilders;
using VRageMath;

public abstract class MyCubeBlockMock : MyEntityMock, IMyCubeBlock
{
  public MyCubeBlockMock(MyCubeGridMock myCubeGridMock) : base(myCubeGridMock.GridTerminalSystem.TestBed.GetNextEntityId())
  {
    CubeGridMock = myCubeGridMock;
    CubeGridMock.CubeBlockMocks.Add(this);
    Position = CubeGridMock.Max + new Vector3I(1, 0, 0);
    Min = Position;
    Max = Position;
  }

  public MyCubeGridMock CubeGridMock { get; set; }

  public SerializableDefinitionId BlockDefinition => throw new System.NotImplementedException();

  public IMyCubeGrid CubeGrid => CubeGridMock;

  public string DefinitionDisplayNameText => throw new System.NotImplementedException();

  public float DisassembleRatio => throw new System.NotImplementedException();

  public string DisplayNameText => throw new System.NotSupportedException("Use CustomName instead");

  public bool IsBeingHacked => throw new System.NotImplementedException();

  public bool IsFunctional { get; set; } = true;

  public bool IsWorking { get; set; } = true;

  public Vector3I Max { get; set; }

  public float Mass => throw new System.NotImplementedException();

  public Vector3I Min { get; set; }

  public int NumberInGrid => throw new System.NotImplementedException();

  public MyBlockOrientation Orientation => throw new System.NotImplementedException();

  public long OwnerId => throw new System.NotImplementedException();

  public Vector3I Position { get; set; }

  public string GetOwnerFactionTag()
  {
    throw new System.NotImplementedException();
  }

  public MyRelationsBetweenPlayerAndBlock GetPlayerRelationToOwner()
  {
    throw new System.NotImplementedException();
  }

  public MyRelationsBetweenPlayerAndBlock GetUserRelationToOwner(long playerId, MyRelationsBetweenPlayerAndBlock defaultNoUser = MyRelationsBetweenPlayerAndBlock.NoOwnership)
  {
    throw new System.NotImplementedException();
  }

  public void UpdateIsWorking()
  {
    throw new System.NotImplementedException();
  }

  public void UpdateVisual()
  {
    throw new System.NotImplementedException();
  }
}
