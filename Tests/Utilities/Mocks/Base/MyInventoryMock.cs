namespace Utilities.Mocks.Base;

using System;
using System.Collections.Generic;
using VRage;
using VRage.Game.ModAPI.Ingame;

public class MyInventoryMock(IMyEntity owner) : IMyInventory
{
  public IMyEntity Owner { get; } = owner;

  public bool IsFull => throw new NotImplementedException();

  public MyFixedPoint CurrentMass => throw new NotImplementedException();

  public MyFixedPoint MaxVolume => throw new NotImplementedException();

  public MyFixedPoint CurrentVolume => throw new NotImplementedException();

  public int ItemCount => throw new NotImplementedException();

  public float VolumeFillFactor => throw new NotImplementedException();

  public bool CanPutItems => throw new NotImplementedException();

  public bool CanItemsBeAdded(MyFixedPoint amount, MyItemType itemType)
  {
    throw new NotImplementedException();
  }

  public bool CanTransferItemTo(IMyInventory otherInventory, MyItemType itemType)
  {
    throw new NotImplementedException();
  }

  public bool ContainItems(MyFixedPoint amount, MyItemType itemType)
  {
    throw new NotImplementedException();
  }

  public MyInventoryItem? FindItem(MyItemType itemType)
  {
    throw new NotImplementedException();
  }

  public void GetAcceptedItems(List<MyItemType> itemsTypes, Func<MyItemType, bool> filter = null)
  {
    throw new NotImplementedException();
  }

  public MyFixedPoint GetItemAmount(MyItemType itemType)
  {
    throw new NotImplementedException();
  }

  public MyInventoryItem? GetItemAt(int index)
  {
    throw new NotImplementedException();
  }

  public MyInventoryItem? GetItemByID(uint id)
  {
    throw new NotImplementedException();
  }

  public void GetItems(List<MyInventoryItem> items, Func<MyInventoryItem, bool> filter = null)
  {
    throw new NotImplementedException();
  }

  public bool IsConnectedTo(IMyInventory otherInventory)
  {
    throw new NotImplementedException();
  }

  public bool IsItemAt(int position)
  {
    throw new NotImplementedException();
  }

  public bool TransferItemFrom(IMyInventory sourceInventory, MyInventoryItem item, MyFixedPoint? amount = null)
  {
    throw new NotImplementedException();
  }

  public bool TransferItemFrom(IMyInventory sourceInventory, int sourceItemIndex, int? targetItemIndex = null, bool? stackIfPossible = null, MyFixedPoint? amount = null)
  {
    throw new NotImplementedException();
  }

  public bool TransferItemTo(IMyInventory dstInventory, MyInventoryItem item, MyFixedPoint? amount = null)
  {
    throw new NotImplementedException();
  }

  public bool TransferItemTo(IMyInventory dst, int sourceItemIndex, int? targetItemIndex = null, bool? stackIfPossible = null, MyFixedPoint? amount = null)
  {
    throw new NotImplementedException();
  }
}
