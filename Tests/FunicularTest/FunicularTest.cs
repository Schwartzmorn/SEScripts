namespace FunicularTest;

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
using VRageMath;

[TestFixture]
public class FunicularTest
{
  private TestBed _testBed;
  private ProgramWrapper _wrapper;

  private readonly List<MyLandingGearMock> _landingGears = [];
  private readonly List<MyPistonBaseMock> _pistons = [];
  private readonly List<MyMotorStatorMock> _rotors = [];
  private MyShipConnectorMock _funicularConnector;
  private MyShipConnectorMock _stationConnector;

  void _createLocker(string name)
  {
    var topGrid = new MyCubeGridMock(_wrapper.GridTerminalSystemMock);
    _landingGears.Add(new MyLandingGearMock(topGrid)
    {
      CanLockMock = true
    });
    _pistons.Add(new MyPistonBaseMock(_wrapper.CubeGridMock)
    {
      Top = new MyAttachableBlockMock(topGrid),
      MaxLimit = 2,
      CustomName = name
    });
  }

  void _createRotor(string name, bool left)
  {
    _rotors.Add(new MyMotorStatorMock(_wrapper.CubeGridMock)
    {
      CustomName = name,
      WorldMatrix = new MatrixD(
        0, left ? 1 : -1, 0, 0,
        left ? -1 : 1, 0, 0, 0,
        0, 0, 1, 0,
        0, 0, 0, 1
      )
    });
  }

  [SetUp]
  public void SetUp()
  {
    _testBed = new TestBed();
    _wrapper = _testBed.CreateProgram<Program>();
    _landingGears.Clear();
    _pistons.Clear();
    _rotors.Clear();

    // Pistons + magnetic plates
    _createLocker("Piston A");
    _createLocker("Piston B");

    // rotors
    _createRotor("Rotor A", true);
    _createRotor("Rotor A", true);
    _createRotor("Rotor B", false);
    _createRotor("Rotor B", false);

    // add connector
    _funicularConnector = new MyShipConnectorMock(_wrapper.CubeGridMock);
    var stationGrid = new MyCubeGridMock(_wrapper.GridTerminalSystemMock);
    _stationConnector = new MyShipConnectorMock(stationGrid);

    // add displays ?
  }

  [Test]
  public void It_Saves_And_Loads_Its_State()
  {
    _testBed.Tick();
    _wrapper.Run("funicular move down");

    // We check the acceleration is correct
    foreach (var r in _rotors)
    {
      if (r.CustomName == "Rotor A")
      {
        Assert.That(r.TargetVelocityRPM, Is.EqualTo(0.1).Within(0.001));
      }
      else
      {
        Assert.That(r.CustomName, Is.EqualTo("Rotor B"));
        Assert.That(r.TargetVelocityRPM, Is.EqualTo(-0.1).Within(0.001));
      }
    }

    _testBed.Tick(100);

    // We check it uses the default safe speed
    foreach (var r in _rotors)
    {
      Assert.That(r.TargetVelocityRPM, Is.EqualTo(5).Or.EqualTo(-5));
    }

    _wrapper.RunOnSave();

    // The state is correctly saved
    Assert.That(_wrapper.ProgrammableBlockMock.CustomData, Is.EqualTo(@"[Funicular]
state=MoveDown
decelerationDistance=5
maxAcceleration=0.1
maxSpeed=20
safeSpeed=5
"));

    _wrapper.ProgrammableBlockMock.CustomData = _wrapper.ProgrammableBlockMock.CustomData.Replace("safeSpeed=5", "safeSpeed=10");
    _testBed.Tick(200);

    // The safe speed was correctly updated
    foreach (var r in _rotors)
    {
      Assert.That(r.TargetVelocityRPM, Is.EqualTo(10).Or.EqualTo(-10));
    }

    _wrapper.ProgrammableBlockMock.WorldPositionMock = new Vector3D(25, 10, 0);
    _wrapper.Run("funicular save down");
    _wrapper.RunOnSave();

    // The state is correctly saved
    Assert.That(_wrapper.ProgrammableBlockMock.CustomData, Is.EqualTo(@"[Funicular]
state=MoveDown
decelerationDistance=5
maxAcceleration=0.1
maxSpeed=20
safeSpeed=10
bottomPosition-x=25
bottomPosition-y=10
bottomPosition-z=0
"));

    _wrapper.Run("funicular forget down");
    _wrapper.RunOnSave();
    Assert.That(_wrapper.ProgrammableBlockMock.CustomData, Is.EqualTo(@"[Funicular]
state=Stop
decelerationDistance=5
maxAcceleration=0.1
maxSpeed=20
safeSpeed=10
"));
  }

  [Test]
  public void It_Works()
  {
    _wrapper.ProgrammableBlockMock.CustomData = @"[Funicular]
state=MoveDown
decelerationDistance=5
maxAcceleration=0.1
maxSpeed=15
safeSpeed=10
bottomPosition-x=0
bottomPosition-y=0
bottomPosition-z=10
topPosition-x=50
topPosition-y=25
topPosition-z=10
";
    _wrapper.ProgrammableBlockMock.WorldPositionMock = new Vector3D(0, 0, 10);
    _pistons.ForEach(p => p.Enabled = false);
    _rotors.ForEach(r => r.RotorLock = true);
    _rotors.ForEach(r => r.Enabled = false);
    _landingGears.ForEach(l => l.Enabled = false);
    _landingGears.ForEach(l => l.Unlock());
    _funicularConnector.Enabled = false;
    _funicularConnector.PendingOtherConnector = _stationConnector;
    _testBed.Tick(250);

    _pistons.ForEach(p =>
    {
      Assert.That(p.Enabled);
      Assert.That(p.NormalizedPosition, Is.EqualTo(1));
    });
    _landingGears.ForEach(l =>
    {
      Assert.That(l.Enabled);
      Assert.That(l.IsLocked);
    });
    _rotors.ForEach(r =>
    {
      Assert.That(!r.Enabled);
      Assert.That(r.RotorLock);
    });
    Assert.That(_funicularConnector.Enabled);
    Assert.That(_funicularConnector.IsConnected);

    _wrapper.Run("funicular move up");
    _testBed.Tick();

    // Disconnecting
    _pistons.ForEach(p =>
    {
      Assert.That(p.Enabled);
      Assert.That(p.Velocity, Is.EqualTo(-1));
    });
    _landingGears.ForEach(l =>
    {
      Assert.That(!l.Enabled);
      Assert.That(!l.IsLocked);
    });
    _rotors.ForEach(r =>
    {
      Assert.That(!r.Enabled);
      Assert.That(r.RotorLock);
    });
    Assert.That(_funicularConnector.Enabled);
    Assert.That(_funicularConnector.IsConnected);

    _testBed.Tick(250);

    // moving up
    _pistons.ForEach(p =>
    {
      Assert.That(!p.Enabled);
      Assert.That(p.NormalizedPosition, Is.EqualTo(0).Within(0.01));
    });
    _landingGears.ForEach(l =>
    {
      Assert.That(!l.Enabled);
      Assert.That(!l.IsLocked);
    });
    _rotors.ForEach(r =>
    {
      Assert.That(r.Enabled);
      Assert.That(!r.RotorLock);
    });
    Assert.That(!_funicularConnector.Enabled);
    Assert.That(!_funicularConnector.IsConnected);
  }
}
