namespace HelpersTest;

using IngameScript;
using NUnit.Framework;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using Utilities;
using Utilities.Mocks;
using Utilities.Mocks.Base;
using Utilities.Mocks.Blocks;
using VRage.Game;
using VRageMath;

[TestFixture]
public class RotorHelperTest
{
  static readonly float TOLERANCE = 0.0001f;
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
  public void Test_With_No_Limit()
  {
    var stator = new MyMotorStatorMock(_smallGrid);

    Assert.That(stator.AngleProxy(0), Is.EqualTo(0f));
    Assert.That(stator.AngleProxy(MathHelper.ToRadians(150)), Is.EqualTo(MathHelper.ToRadians(150)).Within(TOLERANCE));
    Assert.That(stator.AngleProxy(MathHelper.ToRadians(-150)), Is.EqualTo(MathHelper.ToRadians(-150)).Within(TOLERANCE));
    Assert.That(stator.AngleProxy(MathHelper.ToRadians(300)), Is.EqualTo(MathHelper.ToRadians(-60)).Within(TOLERANCE));

    stator.Angle = MathHelper.ToRadians(300);

    Assert.That(stator.AngleProxy(MathHelper.ToRadians(0)), Is.EqualTo(MathHelper.ToRadians(60)).Within(TOLERANCE));

    stator.Angle = MathHelper.ToRadians(150);

    Assert.That(stator.AngleProxy(MathHelper.ToRadians(-150)), Is.EqualTo(MathHelper.ToRadians(60)).Within(TOLERANCE));

    stator.Angle = MathHelper.ToRadians(-150);

    Assert.That(stator.AngleProxy(MathHelper.ToRadians(150)), Is.EqualTo(MathHelper.ToRadians(-60)).Within(TOLERANCE));
  }

  [Test]
  public void Test_With_Upper_Limit()
  {
    var stator = new MyMotorStatorMock(_smallGrid)
    {
      Angle = MathHelper.ToRadians(300),
      UpperLimitDeg = 330f
    };

    Assert.That(stator.AngleProxy(MathHelper.ToRadians(0)), Is.EqualTo(MathHelper.ToRadians(-300)).Within(TOLERANCE));
  }

  [Test]
  public void Test_With_Lower_Limit()
  {
    var stator = new MyMotorStatorMock(_smallGrid)
    {
      Angle = MathHelper.ToRadians(60),
      LowerLimitDeg = 30f
    };

    Assert.That(stator.AngleProxy(MathHelper.ToRadians(0)), Is.EqualTo(MathHelper.ToRadians(300)).Within(TOLERANCE));
  }
}
