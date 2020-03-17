using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sandbox.ModAPI.Ingame;
using IngameScript.Mockups;
using IngameScript.Mockups.Blocks;

namespace IngameScript.MDK {
  class ConnectionClientTest {
    Program.CommandLine commandLine;
    MockShipConnector connector;
    MockGridTerminalSystem gts;
    MockIntergridCommunicationSystem igc;
    Program.IniWatcher ini;
    MockUnicastListener listener;
    Program.IProcessManager manager;
    Program program;
    public void BeforeEach() {
      this.connector = new MockShipConnector() {
        CustomData = @"
        [connection-client]
        connector-name=connector name
        server-channel=server channel
        client-channel=client channel
        ",
        CustomName = "connector name"
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
      this.program = new Program(this.gts, this.igc);
    }

    Program.ConnectionClient getConnectionClient(string state) {
      if (state != null) {
        this.connector.CustomData += "state=" + state;
      }
      this.ini = new Program.IniWatcher(this.connector, this.manager);
      return new Program.ConnectionClient(this.ini, this.program, this.commandLine, this.manager);
    }

    void tick(int n = 5) {
      foreach (int i in Enumerable.Range(0, n)) {
        this.manager.Tick();
      }
    }

    void startCmd(string cmd) => this.commandLine.StartCmd(cmd, Program.CommandTrigger.User);

    public void CreateNew() {
      Program.ConnectionClient client = this.getConnectionClient(null);

      Assert.AreEqual(client.State, Program.ConnectionState.Ready);
    }

    public void Connection() {
      Program.ConnectionClient client = this.getConnectionClient("Ready");

      this.startCmd("-ac-connect");

      this.tick();

      Assert.AreEqual(client.State, Program.ConnectionState.WaitingCon);
    }
  }
}
