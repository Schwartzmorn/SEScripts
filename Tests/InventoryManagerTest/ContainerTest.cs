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
using Utilities;
using Utilities.Mocks;
using Utilities.Mocks.Base;
using Utilities.Mocks.Blocks;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRage.ObjectBuilders;
using VRage.Utils;

[TestFixture]
public class ContainerTest
{
  private TestBed _testBed;
  private MyCubeGridMock _cubeGrid;

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
  public void Affinity_Works_As_Intended()
  {
    var whitelistCargo = new MyCargoContainerMock(_cubeGrid)
    {
      CustomData = @"[filter]
item-types=Ore,Ingot
item-subtypes=Medical,Detector
      "
    };
    whitelistCargo.InventoryMocks[0].AddNewItem(Items.FromShorthand("Ore/Stone"), 10);
    whitelistCargo.InventoryMocks[0].AddNewItem(Items.FromShorthand("Component/Detector"), 10);
    whitelistCargo.InventoryMocks[0].AddNewItem(Items.FromShorthand("Component/Display"), 10);
    var whitelistContainer = new Program.Container(whitelistCargo);

    var blacklistCargo = new MyCargoContainerMock(_cubeGrid)
    {
      CustomData = @"[filter]
type=blacklist
item-types=Ore,Ingot
item-subtypes=Medical,Detector
      "
    };
    blacklistCargo.InventoryMocks[0].AddNewItem(Items.FromShorthand("Ore/Stone"), 10);
    blacklistCargo.InventoryMocks[0].AddNewItem(Items.FromShorthand("Component/Detector"), 10);
    blacklistCargo.InventoryMocks[0].AddNewItem(Items.FromShorthand("Component/Display"), 10);
    var blacklistContainer = new Program.Container(blacklistCargo);

    var genericCargo = new MyCargoContainerMock(_cubeGrid);
    genericCargo.InventoryMocks[0].AddNewItem(Items.FromShorthand("Component/Display"), 10);
    var genericContainer = new Program.Container(genericCargo);

    Assert.That(whitelistContainer.GetAffinity(Items.FromShorthand("Component/Detector")), Is.EqualTo(9));
    Assert.That(whitelistContainer.GetAffinity(Items.FromShorthand("Component/Medical")), Is.EqualTo(8));
    Assert.That(whitelistContainer.GetAffinity(Items.FromShorthand("Ore/Stone")), Is.EqualTo(7));
    Assert.That(whitelistContainer.GetAffinity(Items.FromShorthand("Ingot/Iron")), Is.EqualTo(6));
    Assert.That(blacklistContainer.GetAffinity(Items.FromShorthand("Component/Display")), Is.EqualTo(5));
    Assert.That(blacklistContainer.GetAffinity(Items.FromShorthand("Component/SteelPlate")), Is.EqualTo(4));
    Assert.That(genericContainer.GetAffinity(Items.FromShorthand("Component/Display")), Is.EqualTo(3));
    Assert.That(genericContainer.GetAffinity(Items.FromShorthand("Component/SteelPlate")), Is.EqualTo(2));
    Assert.That(whitelistContainer.GetAffinity(Items.FromShorthand("Component/Display")), Is.EqualTo(1));
    Assert.That(whitelistContainer.GetAffinity(Items.FromShorthand("Component/SteelPlate")), Is.EqualTo(0));
    Assert.That(blacklistContainer.GetAffinity(Items.FromShorthand("Ore/Stone")), Is.EqualTo(-1));
    Assert.That(blacklistContainer.GetAffinity(Items.FromShorthand("Ingot/Iron")), Is.EqualTo(-2));
    Assert.That(blacklistContainer.GetAffinity(Items.FromShorthand("Component/Detector")), Is.EqualTo(-3));
    Assert.That(blacklistContainer.GetAffinity(Items.FromShorthand("Component/Medical")), Is.EqualTo(-4));

  }
}



