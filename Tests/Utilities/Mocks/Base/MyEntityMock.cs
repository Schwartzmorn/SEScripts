namespace Utilities.Mocks.Base;

using System;
using System.Collections.Generic;
using VRage.Game.Components.Interfaces;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

public class MyEntityMock(long entityId) : IMyEntity
{
  public Vector3D WorldPositionMock
  {
    get { return WorldMatrix.Translation; }
    set { _worldMatrix.Translation = value; }
  }

  public readonly List<MyInventoryMock> InventoryMocks = [];

  IMyEntityComponentContainer IMyEntity.Components => throw new NotImplementedException();

  public long EntityId { get; } = entityId;

  public string Name { get => EntityId.ToString(); }

  public string DisplayName => throw new NotImplementedException();

  public bool Closed => false;

  public bool HasInventory => InventoryMocks.Count > 0;

  public int InventoryCount => InventoryMocks.Count;

  public IMyInventory GetInventory()
  {
    return InventoryMocks?.Count == 0 ? null : InventoryMocks[0];
  }

  public IMyInventory GetInventory(int index)
  {
    return index < InventoryMocks?.Count ? InventoryMocks[index] : null;
  }

  public BoundingBoxD WorldAABB { get; set; }

  public BoundingBoxD WorldAABBHr => throw new System.NotImplementedException();

  private MatrixD _worldMatrix = MatrixD.Identity;
  public MatrixD WorldMatrix { get => _worldMatrix; set => _worldMatrix = value; }

  public BoundingSphereD WorldVolume => throw new System.NotImplementedException();

  public BoundingSphereD WorldVolumeHr => throw new System.NotImplementedException();

  // TODO check this
  public Vector3D GetPosition() => WorldMatrix.Translation;
}
