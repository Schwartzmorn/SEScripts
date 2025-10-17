namespace InventoryManagerTest;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using FakeItEasy;
using IngameScript;
using NUnit.Framework;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Utilities;
using Utilities.Mocks;
using Utilities.Mocks.Base;
using Utilities.Mocks.Blocks;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRage.ObjectBuilders;
using VRage.Utils;

/// <summary>
/// As the relevance of the testing of InventoryManager relies on the accuracy of MyInventoryMock, we test it here
/// </summary>
[TestFixture]
public class MyInventoryMockTest
{
  private TestBed _testBed;
  private MyCubeGridMock _cubeGrid;

  private static string _formatInventory(MyInventoryMock inventory)
  {
    var output = new List<string>();
    for (var i = 0; i < inventory.ItemCount; ++i)
    {
      var item = inventory.GetItemAt(i).Value;
      output.Add($"{item.ItemId}({item.Type.SubtypeId})x{item.Amount}");
    }
    return string.Join(' ', output);
  }

  [SetUp]
  public void SetUp()
  {
    _testBed = new TestBed();
    var gts = new MyGridTerminalSystemMock(_testBed);
    _cubeGrid = new MyCubeGridMock(gts)
    {
      GridSizeEnum = MyCubeSize.Large
    };
  }

  [Test]
  public void The_Mock_Works_When_Moving_In_The_Same_Inventory()
  {
    // use cases checked in Space Engineers
    var cargoA = new MyCargoContainerMock(_cubeGrid);
    var invA = cargoA.InventoryMocks[0];
    invA.AddNewItem("Ore/Stone", 10);
    invA.AddNewItem("Ore/Iron", 10);
    invA.AddNewItem("Ore/Cobalt", 10);
    invA.AddNewItem("Ore/Silver", 10);
    invA.AddNewItem("Ore/Stone", 10);

    Assert.That(_formatInventory(invA), Is.EqualTo("0(Stone)x10 1(Iron)x10 2(Cobalt)x10 3(Silver)x10 4(Stone)x10"));

    invA.TransferItemTo(invA, 0);

    Assert.That(_formatInventory(invA), Is.EqualTo("0(Stone)x10 1(Iron)x10 2(Cobalt)x10 3(Silver)x10 4(Stone)x10"));

    invA.TransferItemTo(invA, 0, targetItemIndex: 2);

    Assert.That(_formatInventory(invA), Is.EqualTo("2(Cobalt)x10 1(Iron)x10 0(Stone)x10 3(Silver)x10 4(Stone)x10"));

    invA.TransferItemTo(invA, 4);

    Assert.That(_formatInventory(invA), Is.EqualTo("2(Cobalt)x10 1(Iron)x10 0(Stone)x20 3(Silver)x10"));

    invA.TransferItemTo(invA, 1);

    Assert.That(_formatInventory(invA), Is.EqualTo("2(Cobalt)x10 1(Iron)x10 0(Stone)x20 3(Silver)x10"));

    invA.TransferItemTo(invA, 1, targetItemIndex: 10);

    Assert.That(_formatInventory(invA), Is.EqualTo("2(Cobalt)x10 0(Stone)x20 3(Silver)x10 5(Iron)x10"));
  }

  public void The_Mock_Works_When_Moving_To_Another_Inventory()
  {
    // use cases checked in Space Engineers
    var cargoA = new MyCargoContainerMock(_cubeGrid);
    var invA = cargoA.InventoryMocks[0];
    invA.AddNewItem("Ore/Stone", 10);
    invA.AddNewItem("Ore/Iron", 10);
    invA.AddNewItem("Ore/Cobalt", 10);
    invA.AddNewItem("Ore/Silver", 10);

    var cargoB = new MyCargoContainerMock(_cubeGrid);
    var invB = cargoB.InventoryMocks[0];
    invB.AddNewItem("Ore/Iron", 10);
    invB.AddNewItem("Ore/Stone", 10);
    invB.AddNewItem("Ore/Silver", 10);
    invB.AddNewItem("Ore/Stone", 10);
    invB.AddNewItem("Ore/Cobalt", 10);

    Assert.That(_formatInventory(invA), Is.EqualTo("0(Stone)x10 1(Iron)x10 2(Cobalt)x10 3(Silver)x10"));
    Assert.That(_formatInventory(invA), Is.EqualTo("0(Iron)x10 1(Stone)x10 2(Silver)x10 3(Stone)x10 4(Cobalt)x10"));

    invA.TransferItemTo(invB, 0, amount: 5);

    Assert.That(_formatInventory(invA), Is.EqualTo("0(Stone)x5 1(Iron)x10 2(Cobalt)x10 3(Silver)x10"));
    Assert.That(_formatInventory(invA), Is.EqualTo("0(Iron)x10 1(Stone)x15 2(Silver)x10 3(Stone)x10 4(Cobalt)x10"));

    invA.TransferItemTo(invB, 0, targetItemIndex: 0, amount: 5);

    Assert.That(_formatInventory(invA), Is.EqualTo("1(Iron)x10 2(Cobalt)x10 3(Silver)x10"));
    Assert.That(_formatInventory(invA), Is.EqualTo("5(Stone)x5 0(Iron)x10 1(Stone)x15 2(Silver)x10 3(Stone)x10 4(Cobalt)x10"));

    invA.TransferItemTo(invB, 2, targetItemIndex: 10, amount: 15);

    Assert.That(_formatInventory(invA), Is.EqualTo("1(Iron)x10 3(Silver)x10"));
    Assert.That(_formatInventory(invA), Is.EqualTo("5(Stone)x5 0(Iron)x10 1(Stone)x15 2(Silver)x10 3(Stone)x10 4(Cobalt)x10 6(Cobalt)x10"));

    invA.TransferItemTo(invB, 1, targetItemIndex: 3);

    Assert.That(_formatInventory(invA), Is.EqualTo("1(Iron)x10"));
    Assert.That(_formatInventory(invA), Is.EqualTo("5(Stone)x5 0(Iron)x10 1(Stone)x15 2(Silver)x20 3(Stone)x10 4(Cobalt)x10 6(Cobalt)x10"));

    invA.TransferItemTo(invB, 0, targetItemIndex: 3);

    Assert.That(_formatInventory(invA), Is.EqualTo(""));
    Assert.That(_formatInventory(invA), Is.EqualTo("5(Stone)x5 0(Iron)x10 1(Stone)x15 7(Iron)x10 2(Silver)x20 3(Stone)x10 4(Cobalt)x10 6(Cobalt)x10"));
  }

  [Test]
  public void The_Mock_Checks_Volume()
  {
    var sourceCargo = new MyCargoContainerMock(_cubeGrid);
    var sourceInv = sourceCargo.InventoryMocks[0];
    sourceInv.AddNewItem("Ore/Stone", 100); // volume of 1 per unit, fractionable
    sourceInv.AddNewItem("Component/Medical", 10); // Volume of 10 per unit, not fractionable

    var destCargoA = new MyCargoContainerMock(_cubeGrid);
    var destInvA = destCargoA.InventoryMocks[0];
    destInvA.MaxVolume = 1;
    destInvA.AddNewItem("Ore/Stone", new VRage.MyFixedPoint() { RawValue = 975_500_000 }); // should leave enough place for 24.5 ore

    var destCargoB = new MyCargoContainerMock(_cubeGrid);
    var destInvB = destCargoB.InventoryMocks[0];
    destInvB.MaxVolume = 1;
    destInvB.AddNewItem("Ore/Stone", new VRage.MyFixedPoint() { RawValue = 975_000_000 }); // should leave enough space for 2.5 components, so 2 as not fractionable

    Assert.That(_formatInventory(destInvA), Is.EqualTo("0(Stone)x975.5"));

    sourceInv.TransferItemTo(destInvA, 0);

    Assert.That(_formatInventory(sourceInv), Is.EqualTo("0(Stone)x75.5 1(Medical)x10"));
    Assert.That(_formatInventory(destInvA), Is.EqualTo("0(Stone)x1000"));

    sourceInv.TransferItemTo(destInvB, 1);

    Assert.That(_formatInventory(sourceInv), Is.EqualTo("0(Stone)x75.5 1(Medical)x8"));
    Assert.That(_formatInventory(destInvB), Is.EqualTo("0(Stone)x975 1(Medical)x2"));
  }
}




