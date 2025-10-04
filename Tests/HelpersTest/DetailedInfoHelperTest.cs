namespace HelpersTest;

using IngameScript;
using NUnit.Framework;
using System;
using Utilities;
using Utilities.Mocks;
using Utilities.Mocks.Base;
using Utilities.Mocks.Blocks;
using VRage.Game;

[TestFixture]
public class DetailedInfoHelperTest
{
  MyCubeGridMock _smallGrid;
  TestBed _testBed;

  [SetUp]
  public void SetUp()
  {
    _testBed = new TestBed();
    var gts = new MyGridTerminalSystemMock(_testBed);
    _smallGrid = new MyCubeGridMock(gts)
    {
      GridSizeEnum = MyCubeSize.Small
    };
  }

  [Test]
  public void GetDetailedInfo_Works()
  {
    var solarPanel = new MySolarPanelMock(_smallGrid)
    {
      MaxOutput = 10000,
      CurrentOutput = 500
    };

    Assert.That(solarPanel.GetDetailedInfo("Current Output"), Is.EqualTo("500 W"));
    Assert.That(solarPanel.GetDetailedInfo("Type"), Is.EqualTo("Solar Panel"));
  }
}
