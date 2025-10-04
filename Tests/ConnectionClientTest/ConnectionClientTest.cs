namespace ConnectionClientTest;

using System;
using System.Linq;
using IngameScript;
using NUnit.Framework;
using Utilities;
using Utilities.Mocks;
using Utilities.Mocks.Base;
using Utilities.Mocks.Blocks;
using VRageMath;

[TestFixture]
class ConnectionClientTest
{
  Program.CommandLine _commandLine;
  MyShipConnectorMock _connector;
  MyGridTerminalSystemMock _gts;
  MyIntergridCommunicationSystemMock _igc;
  MyUnicastListenerMock _listener;
  Program.IniWatcher _ini;
  Program.IProcessManager _manager;

  private Program.ConnectionClient _getConnectionClient(string state)
  {
    if (state != null)
    {
      _connector.CustomData += "state=" + state;
    }
    _ini = new Program.IniWatcher(_connector, _manager);
    return new Program.ConnectionClient(_ini, _gts, _igc, _commandLine, _manager, null);
  }

  private void _tick(int n = 5)
  {
    foreach (int i in Enumerable.Range(0, n))
    {
      _manager.Tick();
    }
  }

  private void _startCmd(string cmd) => _commandLine.StartCmd(cmd, Program.CommandTrigger.User);

  [SetUp]
  public void SetUp()
  {
    var testBed = new TestBed();

    _gts = new MyGridTerminalSystemMock(testBed);

    var cubeGrid = new MyCubeGridMock(_gts)
    {
      GridSizeEnum = VRage.Game.MyCubeSize.Small
    };

    _connector = new MyShipConnectorMock(cubeGrid)
    {
      CustomData = @"[connection-client]
  connector-name=connector name
  server-channel=server channel
",
      CustomName = "connector name",
      WorldMatrix = MatrixD.Identity,
      WorldPositionMock = new Vector3D(5, 15, 25)
    };
    _igc = new MyIntergridCommunicationSystemMock(testBed);
    _listener = _igc.UnicastListenerMock;
    _manager = Program.Process.CreateManager(null);
    _commandLine = new Program.CommandLine("test", null, _manager);
  }

  [Test]
  public void It_Is_Created_Ready()
  {
    Program.ConnectionClient client = _getConnectionClient(null);

    Assert.That(client.State, Is.EqualTo(Program.ConnectionState.Ready));
  }

  [Test]
  public void It_Can_Be_Created_With_A_Given_State()
  {
    Program.ConnectionClient client = _getConnectionClient("Standby");

    Assert.That(client.State, Is.EqualTo(Program.ConnectionState.Standby));
  }

  [Test]
  public void It_Allows_Connecting()
  {
    Program.ConnectionClient client = _getConnectionClient(null);

    _startCmd("-ac-connect");

    _tick(1);

    Assert.That(client.Progress, Is.EqualTo(0));
    Assert.That(client.State, Is.EqualTo(Program.ConnectionState.WaitingCon));
    Assert.That(_igc.LastBroadcastMessage.Item1, Is.EqualTo("server channel"));
    // 0 - 0 = -0, allegedly
    Assert.That(_igc.LastBroadcastMessage.Item2, Is.EqualTo("-ac-con \"Small\" \"5\" \"15\" \"25\" \"-0\" \"-0\" \"-1\""));

    _listener.QueueMessage("-ac-progress 0.25");

    _tick();

    Assert.That(client.Progress, Is.EqualTo(0.25f));
    Assert.That(client.State, Is.EqualTo(Program.ConnectionState.WaitingCon));

    _listener.QueueMessage("-ac-done");
    _tick();

    Assert.That(client.State, Is.EqualTo(Program.ConnectionState.Connected));
    Assert.That(client.Progress, Is.EqualTo(0));
  }

  [Test]
  public void It_Handles_Timouts()
  {
    Program.ConnectionClient client = _getConnectionClient(null);

    _startCmd("-ac-connect");

    _tick(55); // enough to timeout
    Assert.That(client.State, Is.EqualTo(Program.ConnectionState.Ready));
    Assert.That(client.FailReason, Is.EqualTo(Program.FailReason.Timeout));
    Assert.That(client.Progress, Is.EqualTo(0));

    _tick(500); // enough to for the fail reason to be reset
    Assert.That(client.FailReason, Is.EqualTo(Program.FailReason.None));
  }

  [Test]
  public void It_Tracks_Progress()
  {
    Program.ConnectionClient client = _getConnectionClient(null);

    _startCmd("-ac-connect");

    _tick(40); // not enought to timeout

    _listener.QueueMessage("-ac-progress 0.125");

    _tick(40);

    Assert.That(client.State, Is.EqualTo(Program.ConnectionState.WaitingCon));

    _listener.QueueMessage("-ac-progress 0.25");

    _tick(40);

    Assert.That(client.State, Is.EqualTo(Program.ConnectionState.WaitingCon));
  }

  [Test]
  public void It_Allows_Disconnecting()
  {
    Program.ConnectionClient client = _getConnectionClient("Connected");

    _startCmd("-ac-disconnect");

    _tick(1);

    Assert.That(client.Progress, Is.EqualTo(0));
    Assert.That(client.State, Is.EqualTo(Program.ConnectionState.WaitingDisc));
    Assert.That(_igc.LastBroadcastMessage.Item1, Is.EqualTo("server channel"));
    Assert.That(_igc.LastBroadcastMessage.Item2, Is.EqualTo("-ac-disc"));

    _listener.QueueMessage("-ac-progress 0.25");

    _tick();

    Assert.That(client.Progress, Is.EqualTo(0.25f));
    Assert.That(client.State, Is.EqualTo(Program.ConnectionState.WaitingDisc));

    _listener.QueueMessage("-ac-done");
    _tick();

    Assert.That(client.State, Is.EqualTo(Program.ConnectionState.Ready));
    Assert.That(client.Progress, Is.EqualTo(0));
  }

  [Test]
  public void It_Stands_By_When_Disconnected_By_A_Third_Party()
  {
    Program.ConnectionClient client = _getConnectionClient("Connected");

    _listener.QueueMessage("-ac-cancel");

    _tick(6);

    Assert.That(client.Progress, Is.EqualTo(0));
    Assert.That(client.State, Is.EqualTo(Program.ConnectionState.Standby));

    _tick(200);

    Assert.That(client.Progress, Is.EqualTo(0));
    Assert.That(client.State, Is.EqualTo(Program.ConnectionState.Standby));

    _listener.QueueMessage("-ac-progress 0.5");

    _tick(6);

    Assert.That(client.Progress, Is.EqualTo(0.5f));
    Assert.That(client.State, Is.EqualTo(Program.ConnectionState.WaitingCon));
  }
}
