namespace PilotAssistTest;

using IngameScript;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Utilities;
using Utilities.Mocks;
using Utilities.Mocks.Base;

[TestFixture]
class PilotAssistTest
{

  TestBed _testBed;
  MyShipControllerMock _controller1;
  MyShipControllerMock _controller2;
  MyGridTerminalSystemMock _gts;
  MyCubeGridMock _grid;
  Program.WheelsController _mockWheelsController;
  Program.IProcessManager _spawner;

  [SetUp]
  public void SetUp()
  {
    _testBed = new TestBed();
    _gts = new MyGridTerminalSystemMock(_testBed);
    _grid = new MyCubeGridMock(_gts);
    _controller1 = new MyShipControllerMock(_grid)
    {
      CustomName = "Controller 1",
    };
    _controller2 = new MyShipControllerMock(_grid)
    {
      CustomName = "Controller 2",
    };
    _mockWheelsController = new Program.WheelsController();
    _spawner = Program.Process.CreateManager();
  }

  [Test]
  public void InitWithoutController()
  {
    _controller1.CustomData = @"[pilot-assist]
      controllers=Controller 3";
    var ini = new Program.IniWatcher(_controller1, _spawner);
    try
    {
      _ = new Program.PilotAssist(_controller1, _gts, ini, null, _spawner, _mockWheelsController);
      Assert.Fail("Should have raised an error as there is no valid controller");
    }
    catch { }
  }

  // Assist has been removed
  // [Test]
  // public void InitWithAssistWithoutHandbrake()
  // {
  //   _controller1.CustomData = @"[pilot-assist]
  //     assist=true
  //     controllers=Controller 1,Controller 2";
  //   var ini = new Program.IniWatcher(_controller1, _spawner);

  //   Assert.That(_controller2.ControlWheels);
  //   Assert.That(_controller1.HandBrake, Is.False);

  //   var pa = new Program.PilotAssist(_controller1, _gts, ini, null, _spawner, _mockWheelsController);

  //   Assert.That(_controller1.ControlWheels, Is.False, "When assist is true, the controllers no longer control the wheels directly");
  //   Assert.That(_controller2.ControlWheels, Is.False);
  //   Assert.That(_controller1.HandBrake, Is.False);

  //   Assert.That(pa.ManuallyBraked, Is.False);
  // }

  // [Test]
  // public void InitWithoutAssistWithHandbrake()
  // {
  //   _controller1.CustomData = @"[pilot-assist]
  //     controllers=Controller 2";
  //   var ini = new Program.IniWatcher(_controller1, _spawner);

  //   _controller2.ControlWheels = false;
  //   _controller2.HandBrake = true;

  //   var pa = new Program.PilotAssist(_controller1, _gts, ini, null, _spawner, _mockWheelsController);

  //   Assert.That(_controller2.ControlWheels);
  //   Assert.That(_controller2.HandBrake);
  //   Assert.That(pa.ManuallyBraked);
  // }

  [Test]
  public void Save()
  {
    _controller1.CustomData = @"[pilot-assist]
      assist=true
      controllers=Controller 1,Controller 2,Controller 3
      sensitivity=4";
    var ini = new Program.IniWatcher(_controller1, _spawner);

    var pa = new Program.PilotAssist(_controller1, _gts, ini, null, _spawner, _mockWheelsController);

    string saved = null;
    _spawner.Save(s => saved = s);

    Assert.That("[pilot-assist]\nassist=true\ncontrollers=Controller 1,Controller 2\nsensitivity=4", Is.EqualTo(saved.Trim()));
  }

  [Test]
  public void DetectHandbrake()
  {
    _controller1.CustomData = @"[pilot-assist]
      controllers=Controller 1";
    var ini = new Program.IniWatcher(_controller1, _spawner);
    _controller1.IsUnderControl = true;

    var pa = new Program.PilotAssist(_controller1, _gts, ini, null, _spawner, _mockWheelsController);

    _spawner.Tick();

    Assert.That(pa.ManuallyBraked, Is.False);

    _controller1.HandBrake = true;
    _spawner.Tick();

    Assert.That(pa.ManuallyBraked);

    _controller1.HandBrake = false;
    _spawner.Tick();

    Assert.That(pa.ManuallyBraked, Is.False);
  }

  [Test]
  public void AutoBrake()
  {
    _controller1.CustomData = @"[pilot-assist]
      controllers=Controller 1";
    var ini = new Program.IniWatcher(_controller1, _spawner);
    _controller1.IsUnderControl = true;
    var handbraker = new Program.MockBraker();
    var deactivator = new Program.MockDeactivator();

    var pa = new Program.PilotAssist(_controller1, _gts, ini, null, _spawner, _mockWheelsController);
    pa.AddBraker(handbraker);
    pa.AddDeactivator(deactivator);

    Assert.That(_controller1.IsUnderControl);

    _spawner.Tick();

    Assert.That(_controller1.HandBrake, Is.False);

    _controller1.IsUnderControl = false;
    _spawner.Tick();

    Assert.That(_controller1.HandBrake);

    _controller1.IsUnderControl = true;
    _spawner.Tick();

    Assert.That(_controller1.HandBrake, Is.False);

    handbraker.Handbrake = true;
    _spawner.Tick();

    Assert.That(_controller1.HandBrake);

    deactivator.Deactivate = true;
    _spawner.Tick();

    Assert.That(_controller1.HandBrake, "When deactivated, the handbrake is neither engaged nor disengaged automatically");

    handbraker.Handbrake = false;
    deactivator.Deactivate = false;
    _spawner.Tick();

    Assert.That(_controller1.HandBrake, Is.False);

    handbraker.Handbrake = true;
    deactivator.Deactivate = true;
    _controller1.IsUnderControl = false;
    _spawner.Tick();

    Assert.That(_controller1.HandBrake, Is.False, "When deactivated, the handbrake is neither engaged nor disengaged automatically");
  }

  [Test]
  public void PilotAssist()
  {
    _controller1.CustomData = @"[pilot-assist]
      assist=true
      controllers=Controller 1
      sensitivity=5";
    var ini = new Program.IniWatcher(_controller1, _spawner);
    var deactivator = new Program.MockDeactivator();

    var pa = new Program.PilotAssist(_controller1, _gts, ini, null, _spawner, _mockWheelsController);
    pa.AddDeactivator(deactivator);

    _controller1.MoveIndicator = new VRageMath.Vector3(0, 0, 2);
    _spawner.Tick();

    Assert.That(-0.4f, Is.EqualTo(_mockWheelsController.Power));
    Assert.That(0, Is.EqualTo(_mockWheelsController.Steer));

    _controller1.MoveIndicator = new VRageMath.Vector3(4, 0, 0);
    _spawner.Tick();

    Assert.That(0, Is.EqualTo(_mockWheelsController.Power));
    Assert.That(0.8f, Is.EqualTo(_mockWheelsController.Steer));

    deactivator.Deactivate = true;

    _controller1.MoveIndicator = new VRageMath.Vector3(0, 0, 4);
    _spawner.Tick();

    Assert.That(0, Is.EqualTo(_mockWheelsController.Power), "When deactivated, the controls are left as is");
    Assert.That(0.8f, Is.EqualTo(_mockWheelsController.Steer));
  }
}
