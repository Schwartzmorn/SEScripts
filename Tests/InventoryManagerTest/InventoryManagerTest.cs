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
using Sandbox.ModAPI.Ingame;
using Utilities;
using Utilities.Mocks;
using Utilities.Mocks.Base;
using Utilities.Mocks.Blocks;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRage.ObjectBuilders;
using VRage.Utils;

[TestFixture]
public class InventoryManagerTest
{
  private TestBed _testBed;
  private ProgramWrapper _wrapper;

  private static string _formatInventory(MyInventoryMock inventory)
  {
    var output = new List<string>();
    for (var i = 0; i < inventory.ItemCount; ++i)
    {
      var item = inventory.GetItemAt(i).Value;
      output.Add($"({item.Type.SubtypeId})x{item.Amount}");
    }
    return string.Join(' ', output);
  }

  [SetUp]
  public void SetUp()
  {
    _testBed = new TestBed();
    _wrapper = _testBed.CreateProgram<Program>();
  }

  [Test]
  public void It_Rationalizes_Cargos_On_Its_Metagrid()
  {
    var cargoA = new MyCargoContainerMock(_wrapper.CubeGridMock)
    {
      CustomData = @"[filter]
item-types=Ore"
    };

    var invA = cargoA.InventoryMocks[0];
    invA.AddNewItem("Ore/Stone", 10);
    invA.AddNewItem("Ore/Iron", 10);
    invA.AddNewItem("Component/SteelPlate", 10);
    invA.AddNewItem("Ore/Stone", 10);
    invA.AddNewItem("Ore/Iron", 10);

    var cargoB = new MyCargoContainerMock(_wrapper.CubeGridMock);
    var invB = cargoB.InventoryMocks[0];
    invB.AddNewItem("Ore/Iron", 10);

    var cargoC = new MyCargoContainerMock(_wrapper.CubeGridMock);
    var invC = cargoC.InventoryMocks[0];
    invC.AddNewItem("Component/SteelPlate", 10);

    _testBed.Tick(110);

    Assert.That(_formatInventory(invA), Is.EqualTo("(Stone)x20 (Iron)x30"));
    Assert.That(_formatInventory(invB), Is.EqualTo(""));
    Assert.That(_formatInventory(invC), Is.EqualTo("(SteelPlate)x20"));
  }

  [Test]
  public void It_Allows_Excluding_Grids()
  {
    _wrapper.ProgrammableBlockMock.CustomData = @"[grid-manager]
excluded-grids=Excluded grid
    ";
    // The container that shoulde receive it all
    var cargoA = new MyCargoContainerMock(_wrapper.CubeGridMock)
    {
      CustomData = @"[filter]
item-types=Ore"
    };
    // we create a grid that InventoryManager manages with a single cargo container linked with a connector
    var managedGrid = new MyCubeGridMock(_wrapper.GridTerminalSystemMock)
    {
      CustomName = "Managed grid"
    };
    var managedCargo = new MyCargoContainerMock(managedGrid);
    managedCargo.InventoryMocks[0].AddNewItem("Ore/Stone", 10);
    new MyShipConnectorMock(_wrapper.CubeGridMock)
    {
      PendingOtherConnector = new MyShipConnectorMock(managedGrid)
    }.Connect();
    // we create a grid that InventoryManager does not manage with a single cargo container linked with a connector
    var excludedGrid = new MyCubeGridMock(_wrapper.GridTerminalSystemMock)
    {
      CustomName = "Excluded grid"
    };
    var excludedCargo = new MyCargoContainerMock(excludedGrid);
    excludedCargo.InventoryMocks[0].AddNewItem("Ore/Stone", 10);
    new MyShipConnectorMock(_wrapper.CubeGridMock)
    {
      PendingOtherConnector = new MyShipConnectorMock(excludedGrid)
    }.Connect();

    _testBed.Tick(210);

    Assert.That(_formatInventory(cargoA.InventoryMocks[0]), Is.EqualTo("(Stone)x10"));
    Assert.That(_formatInventory(managedCargo.InventoryMocks[0]), Is.EqualTo(""));
    Assert.That(_formatInventory(excludedCargo.InventoryMocks[0]), Is.EqualTo("(Stone)x10"));
  }
}



