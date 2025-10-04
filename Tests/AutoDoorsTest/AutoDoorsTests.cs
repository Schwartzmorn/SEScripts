namespace AutoDoorsTest;

using IngameScript;
using NUnit.Framework;
using Sandbox.ModAPI.Ingame;
using System;
using System.Linq;
using Utilities;
using Utilities.Mocks.Blocks;

[TestFixture]
public class InstancingTests
{
  private TestBed _testBed;
  private ProgramWrapper _programWrapper;

  [SetUp]
  public void Setup()
  {
    _testBed = new TestBed();
    _programWrapper = _testBed.CreateProgram<Program>();
  }

  [Test]
  public void It_Automatically_Closes_Door()
  {
    var door = new MyDoorMock(_programWrapper.CubeGridMock)
    {
      CustomName = "A door"
    };

    _testBed.Tick();

    door.OpenDoor();

    Assert.That(door.Status, Is.EqualTo(DoorStatus.Opening));

    _testBed.Tick((int)(door.DoorTicksToClose + 1));

    Assert.That(door.Status, Is.EqualTo(DoorStatus.Open));

    _testBed.Tick(71);

    Assert.That(door.Status, Is.EqualTo(DoorStatus.Closing));
  }

  [Test]
  public void It_Locks_Sas_Doors()
  {
    var innerDoor = new MyDoorMock(_programWrapper.CubeGridMock)
    {
      CustomName = "A test sas - Inner"
    };
    var outerDoor = new MyDoorMock(_programWrapper.CubeGridMock)
    {
      CustomName = "A test sas - Outer"
    };
    _testBed.Tick();

    // Sanity checks on the initial conditions
    Assert.That(innerDoor.Status, Is.EqualTo(DoorStatus.Closed));
    Assert.That(outerDoor.Status, Is.EqualTo(DoorStatus.Closed));
    Assert.That(innerDoor.Enabled);
    Assert.That(outerDoor.Enabled);

    innerDoor.OpenDoor();

    _testBed.Tick();

    // Check the sas has been locked
    Assert.That(_programWrapper.EchoMessages.Last(), Contains.Substring("A test sas"));
    Assert.That(innerDoor.Status, Is.EqualTo(DoorStatus.Opening));
    Assert.That(outerDoor.Status, Is.EqualTo(DoorStatus.Closed));
    Assert.That(innerDoor.Enabled);
    Assert.That(outerDoor.Enabled, Is.False);

    _testBed.Tick(50);

    Assert.That(innerDoor.Status, Is.EqualTo(DoorStatus.Open));
    Assert.That(outerDoor.Status, Is.EqualTo(DoorStatus.Closed));
    Assert.That(innerDoor.Enabled);
    Assert.That(outerDoor.Enabled, Is.False);

    _testBed.Tick(75);

    // Check the sas is automatically closing
    Assert.That(innerDoor.Status, Is.EqualTo(DoorStatus.Closing));
    Assert.That(outerDoor.Status, Is.EqualTo(DoorStatus.Closed));
    Assert.That(innerDoor.Enabled);
    Assert.That(outerDoor.Enabled, Is.False);

    _testBed.Tick(80);

    // Check the sas is automatically unlocked
    Assert.That(innerDoor.Status, Is.EqualTo(DoorStatus.Closed));
    Assert.That(outerDoor.Status, Is.EqualTo(DoorStatus.Closed));
    Assert.That(innerDoor.Enabled);
    Assert.That(outerDoor.Enabled);
  }
}
