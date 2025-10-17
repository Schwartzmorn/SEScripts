namespace Utilities.Mocks.Base;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

public class MyInventoryMock(MyEntityMock owner) : IMyInventory
{
  private uint _nextId = 0;

  public void AddNewItem(string itemShorthand, MyFixedPoint amount, int? target = null)
  {
    AddNewItem(Items.FromShorthand(itemShorthand), amount, target);
  }

  public void AddNewItem(MyItemType type, MyFixedPoint amount, int? target = null)
  {
    if (target == null || target >= _inventoryItems.Count)
    {
      _inventoryItems.Add(new MyInventoryItem(type, _nextId++, amount));
    }
    else
    {
      _inventoryItems.Insert(target.Value, new MyInventoryItem(type, _nextId++, amount));
    }
  }

  public MyTuple<MyItemType, MyFixedPoint> AddItem(int target, MyFixedPoint amount)
  {
    // convenience method to ensure we are adding an amount
    Debug.Assert(amount >= 0);
    return ChangeItemAmount(target, amount);
  }

  public MyTuple<MyItemType, MyFixedPoint> RemoveItem(int target, MyFixedPoint? amount = null)
  {
    // convenience method to ensure we are removing an amount (or the whole item)
    Debug.Assert(amount >= 0);
    return ChangeItemAmount(target, (-amount) ?? MyFixedPoint.MinValue);
  }

  public MyTuple<MyItemType, MyFixedPoint> ChangeItemAmount(int target, MyFixedPoint amount)
  {
    Debug.Assert(target >= 0);
    Debug.Assert(target < _inventoryItems.Count);

    var currentItem = _inventoryItems[target];

    bool removeWholeItem = -amount >= currentItem.Amount;
    var actualAmount = removeWholeItem ? -currentItem.Amount : amount;
    if (removeWholeItem)
    {
      _inventoryItems.RemoveAt(target);
    }
    else
    {
      _inventoryItems[target] = new MyInventoryItem(currentItem.Type, currentItem.ItemId, currentItem.Amount + actualAmount);
    }
    return MyTuple.Create(currentItem.Type, -actualAmount);
  }

  public IMyEntity Owner { get; } = owner;

  public bool IsFull => CurrentVolume >= MaxVolume;

  public MyFixedPoint CurrentMass
  {
    get
    {
      return _inventoryItems
          .Select(item => item.Type.UnitaryMass() * item.Amount)
          .Aggregate(MyFixedPoint.Zero, (prev, next) => prev + next);
    }
  }

  public MyFixedPoint MaxVolume { get; set; } = 10;

  public MyFixedPoint CurrentVolume
  {
    get
    {
      return _inventoryItems
          .Select(item => item.Type.UnitaryVolume() * item.Amount)
          .Aggregate(MyFixedPoint.Zero, (prev, next) => prev + next);
    }
  }


  public int ItemCount => _inventoryItems.Count;

  public float VolumeFillFactor => (float)CurrentVolume.RawValue / MaxVolume.RawValue;

  public bool CanPutItems => throw new NotImplementedException();

  private readonly List<MyInventoryItem> _inventoryItems = [];

  private MyCubeGridMock _grid
  {
    get
    {
      if (Owner is MyCubeBlockMock ownerCube)
      {
        return ownerCube.CubeGridMock;
      }
      return null;
    }
  }

  public bool CanItemsBeAdded(MyFixedPoint amount, MyItemType itemType)
  {
    // this only checks the item type and quantity
    // TODO check if type is accepted by inventory
    var necessaryVolume = itemType.UnitaryVolume() * amount;
    return MaxVolume - CurrentVolume >= necessaryVolume;
  }

  public bool CanTransferItemTo(IMyInventory otherInventory, MyItemType itemType)
  {
    // this only check the connection and the size of the item type
    // TODO check item type ?
    return IsConnectedTo(otherInventory);
  }

  public bool ContainItems(MyFixedPoint amount, MyItemType itemType) => amount <= GetItemAmount(itemType);

  public MyInventoryItem? FindItem(MyItemType itemType)
  {
    // Cannot use find as it will return the default MyInventoryItem
    foreach (var item in _inventoryItems)
    {
      if (item.Type == itemType)
      {
        return item;
      }
    }
    return null;
  }

  public void GetAcceptedItems(List<MyItemType> itemsTypes, Func<MyItemType, bool> filter = null)
  {
    // The current one is buggy in SE and does not return Consumables, so we should not rely on it
    throw new NotImplementedException();
  }

  public MyFixedPoint GetItemAmount(MyItemType itemType)
  {
    return _inventoryItems
        .FindAll(item => item.Type == itemType)
        .Select(item => item.Amount)
        .Aggregate(MyFixedPoint.Zero, (prev, next) => prev + next);
  }

  public MyInventoryItem? GetItemAt(int index) => index < _inventoryItems.Count ? _inventoryItems[index] : null;

  public MyInventoryItem? GetItemByID(uint id)
  {
    foreach (var item in _inventoryItems)
    {
      if (item.ItemId == id)
      {
        return item;
      }
    }
    return null;
  }

  public void GetItems(List<MyInventoryItem> items, Func<MyInventoryItem, bool> filter = null)
  {
    items.Clear();
    foreach (var item in _inventoryItems)
    {
      if (filter == null || filter(item))
      {
        items.Add(item);
      }
    }
  }

  public bool IsConnectedTo(IMyInventory otherInventory)
  {
    // TODO improve this
    return true;
  }

  public bool IsItemAt(int position) => position >= 0 && position < _inventoryItems.Count;

  public bool TransferItemFrom(IMyInventory sourceInventory, MyInventoryItem item, MyFixedPoint? amount = null)
  {
    return sourceInventory.TransferItemTo(this, item, amount);
  }

  public bool TransferItemFrom(IMyInventory sourceInventory, int sourceItemIndex, int? targetItemIndex = null, bool? stackIfPossible = null, MyFixedPoint? amount = null)
  {
    return sourceInventory.TransferItemTo(this, sourceItemIndex, targetItemIndex, stackIfPossible, amount);
  }

  public bool TransferItemTo(IMyInventory dstInventory, MyInventoryItem item, MyFixedPoint? amount = null)
  {
    for (var i = 0; i < _inventoryItems.Count; ++i)
    {
      if (_inventoryItems[i] == item)
      {
        TransferItemTo(dstInventory, i);
      }
    }
    return false;
  }

  public bool TransferItemTo(IMyInventory dst, int sourceItemIndex, int? targetItemIndex = null, bool? stackIfPossible = null, MyFixedPoint? amount = null)
  {
    Debug.Assert(amount == null || amount.Value >= 0);
    // stackIfPossible seems to have no effect
    if (!IsConnectedTo(dst))
    {
      return false;
    }
    // TODO check if type matches what the inventory can accept

    if (sourceItemIndex >= ItemCount)
    {
      return false;
    }

    if (dst == this && sourceItemIndex == targetItemIndex)
    {
      return true;
    }

    var destination = dst as MyInventoryMock;
    MyInventoryItem itemToMove = _inventoryItems[sourceItemIndex];
    bool wholeItem = amount == null || amount.Value >= itemToMove.Amount;
    MyFixedPoint actualAmount = wholeItem ? itemToMove.Amount : amount.Value;
    if (dst != this)
    {
      // we need to check inventory
      var freeVolume = dst.MaxVolume - dst.CurrentVolume;
      if (freeVolume <= 0)
      {
        // safeguard, mostly to avoid weird issues if free volume < 0
        return true;
      }

      var neededVolume = actualAmount * itemToMove.Type.UnitaryVolume();

      if (neededVolume > freeVolume)
      {
        if (itemToMove.Type.IsFractionable())
        {
          actualAmount.RawValue = (freeVolume.RawValue * 1_000_000) / itemToMove.Type.UnitaryVolume().RawValue;
        }
        else
        {
          actualAmount = (int)(freeVolume.RawValue / itemToMove.Type.UnitaryVolume().RawValue);
        }
      }
    }

    if (targetItemIndex.HasValue)
    {
      if (targetItemIndex.Value >= destination._inventoryItems.Count)
      {
        destination.AddNewItem(itemToMove.Type, actualAmount);
        RemoveItem(sourceItemIndex, actualAmount);
      }
      else if (this == dst)
      {
        // if in the same cargo container the items are swapped, unless they are of the same type
        if (itemToMove.Type == _inventoryItems[targetItemIndex.Value].Type)
        {
          AddItem(targetItemIndex.Value, actualAmount);
          RemoveItem(sourceItemIndex, actualAmount);
        }
        else
        {
          // this may be wrong if not moving the whole item
          _inventoryItems[sourceItemIndex] = _inventoryItems[targetItemIndex.Value];
          _inventoryItems[targetItemIndex.Value] = itemToMove;
        }
      }
      else
      {
        // if different cargo containers and targetItemIndex points to an item, the others are pushed back, unless of the same type
        if (_inventoryItems[sourceItemIndex].Type == destination._inventoryItems[targetItemIndex.Value].Type)
        {
          destination.AddItem(targetItemIndex.Value, actualAmount);
        }
        else
        {
          destination.AddNewItem(itemToMove.Type, actualAmount, targetItemIndex.Value);
        }
        RemoveItem(sourceItemIndex, actualAmount);
      }
    }
    else
    {
      int? index = null;
      for (var i = 0; i < destination._inventoryItems.Count; ++i)
      {
        if (destination._inventoryItems[i].Type == itemToMove.Type)
        {
          index = i;
          break;
        }
      }
      // /!\ Add then remove, otherwise the index may be screwed up
      // Add the item to destination
      if (index.HasValue)
      {
        destination.AddItem(index.Value, actualAmount);
      }
      else
      {
        destination.AddNewItem(itemToMove.Type, actualAmount);
      }
      // Remove the item from source
      RemoveItem(sourceItemIndex, actualAmount);
    }

    // AFAIK Space engineers always returns true
    return true;
  }
}
