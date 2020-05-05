using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sandbox.ModAPI.Ingame;
using IngameScript.Mockups;
using IngameScript.Mockups.Blocks;
using VRageMath;

namespace IngameScript.MDK {
  class ConnectionClientTest {
    Program.CommandLine commandLine;
    MockShipConnector connector;
    MockGridTerminalSystem gts;
    MockIntergridCommunicationSystem igc;
    Program.IniWatcher ini;
    MockUnicastListener listener;
    Program.IProcessManager manager;
    public void BeforeEach() {
      this.connector = new MockShipConnector() {
        CubeGrid = new MockCubeGrid() {
          GridSizeEnum = VRage.Game.MyCubeSize.Small
        },
        CustomData = @"[connection-client]
  connector-name=connector name
  server-channel=server channel
",
        CustomName = "connector name",
        DisplayNameText = "connector name",
        WorldMatrix = MatrixD.Identity,
        WorldPosition = new Vector3D(5, 15, 25)
      };
      this.gts = new MockGridTerminalSystem() { 
        this.connector
      };
      this.listener = new MockUnicastListener();
      this.igc = new MockIntergridCommunicationSystem() {
        UnicastListener = this.listener
      };
      this.manager = Program.Process.CreateManager(null);
      this.commandLine = new Program.CommandLine("test", null, this.manager);
    }

    Program.ConnectionClient getConnectionClient(string state) {
      if (state != null) {
        this.connector.CustomData += "state=" + state;
      }
      this.ini = new Program.IniWatcher(this.connector, this.manager);
      return new Program.ConnectionClient(this.ini, this.gts, this.igc, this.commandLine, this.manager, null);
    }

    void tick(int n = 5) {
      foreach (int i in Enumerable.Range(0, n)) {
        this.manager.Tick();
      }
    }

    void startCmd(string cmd) => this.commandLine.StartCmd(cmd, Program.CommandTrigger.User);

    public void CreateNew() {
      Program.ConnectionClient client = this.getConnectionClient(null);

      Assert.AreEqual(Program.ConnectionState.Ready, client.State);
    }

    public void CreateNewWithState() {
      Program.ConnectionClient client = this.getConnectionClient("Standby");

      Assert.AreEqual(Program.ConnectionState.Standby, client.State);
    }

    public void Connection() {
      Program.ConnectionClient client = this.getConnectionClient(null);

      this.startCmd("-ac-connect");

      this.tick(1);

      Assert.AreEqual(0, client.Progress);
      Assert.AreEqual(Program.ConnectionState.WaitingCon, client.State);
      Assert.AreEqual("server channel", this.igc.LastMessage.Item1);
      Assert.AreEqual("-ac-con \"Small\" \"5\" \"15\" \"25\" \"0\" \"0\" \"-1\"", this.igc.LastMessage.Item2);

      this.listener.QueueMessage("-ac-progress 0.25");

      this.tick();

      Assert.AreEqual(0.25f, client.Progress);
      Assert.AreEqual(Program.ConnectionState.WaitingCon, client.State);

      this.listener.QueueMessage("-ac-done");
      this.tick();

      Assert.AreEqual(Program.ConnectionState.Connected, client.State);
      Assert.AreEqual(0, client.Progress);
    }

    public void Timeout() {
      Program.ConnectionClient client = this.getConnectionClient(null);

      this.startCmd("-ac-connect");

      this.tick(55); // enough to timeout
      Assert.AreEqual(Program.ConnectionState.Ready, client.State);
      Assert.AreEqual(Program.FailReason.Timeout, client.FailReason);
      Assert.AreEqual(0, client.Progress);

      this.tick(500); // enough to for the fail reason to be reset
      Assert.AreEqual(Program.FailReason.None, client.FailReason);
    }

    public void LongProgress() {
      Program.ConnectionClient client = this.getConnectionClient(null);

      this.startCmd("-ac-connect");

      this.tick(40); // not enought to timeout

      this.listener.QueueMessage("-ac-progress 0.125");

      this.tick(40);

      Assert.AreEqual(Program.ConnectionState.WaitingCon, client.State);

      this.listener.QueueMessage("-ac-progress 0.25");

      this.tick(40);

      Assert.AreEqual(Program.ConnectionState.WaitingCon, client.State);
    }

    public void Disconnect() {
      Program.ConnectionClient client = this.getConnectionClient("Connected");

      this.startCmd("-ac-disconnect");

      this.tick(1);

      Assert.AreEqual(0, client.Progress);
      Assert.AreEqual(Program.ConnectionState.WaitingDisc, client.State);
      Assert.AreEqual("server channel", this.igc.LastMessage.Item1);
      Assert.AreEqual("-ac-disc", this.igc.LastMessage.Item2);

      this.listener.QueueMessage("-ac-progress 0.25");

      this.tick();

      Assert.AreEqual(0.25f, client.Progress);
      Assert.AreEqual(Program.ConnectionState.WaitingDisc, client.State);

      this.listener.QueueMessage("-ac-done");
      this.tick();

      Assert.AreEqual(Program.ConnectionState.Ready, client.State);
      Assert.AreEqual(0, client.Progress);
    }

    public void Standby() {
      Program.ConnectionClient client = this.getConnectionClient("Connected");

      this.listener.QueueMessage("-ac-cancel");

      this.tick(6);

      Assert.AreEqual(0, client.Progress);
      Assert.AreEqual(Program.ConnectionState.Standby, client.State);

      this.tick(200);

      Assert.AreEqual(0, client.Progress);
      Assert.AreEqual(Program.ConnectionState.Standby, client.State);

      this.listener.QueueMessage("-ac-progress 0.5");

      this.tick(6);

      Assert.AreEqual(0.5f, client.Progress);
      Assert.AreEqual(Program.ConnectionState.WaitingCon, client.State);
    }
  }
}
