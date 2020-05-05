using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

namespace IngameScript {
  partial class Program {
    public enum ConnectionState { Connected, Ready, Standby, WaitingCon, WaitingDisc };

    public enum FailReason { Cancellation, Failure, User, Timeout, None }

    public class ConnectionClient : IIniConsumer, IPABraker {
      static readonly ConnectionState[] BRAKE_STATES = { ConnectionState.Connected, ConnectionState.WaitingCon, ConnectionState.WaitingDisc };
      const double DECO_DISTANCE_SQUARED = 0.04;
      const string SECTION = "connection-client";

      public float Progress { get; private set; } = 0;
      public ConnectionState State { get { return this.state; } private set { this.setState(value); } }
      public FailReason FailReason { get; private set; } = FailReason.None;

      IMyShipConnector connector;
      readonly IMyGridTerminalSystem gts;
      readonly IMyIntergridCommunicationSystem igc;
      readonly IMyUnicastListener listener;
      readonly CommandLine listenerCmd;
      readonly Action<string> logger;
      readonly Process mainProcess;
      Vector3D? position = null;
      string serverChannel;
      ConnectionState state = ConnectionState.Ready;
      Process timeOutProcess = null;

      public ConnectionClient(IniWatcher ini, IMyGridTerminalSystem gts, IMyIntergridCommunicationSystem igc, CommandLine commandLine, IProcessManager manager, Action<string> logger) {
        this.gts = gts;
        this.igc = igc;
        this.logger = logger;

        this.mainProcess = manager.Spawn(this.listen, "cc-listen", period: 5);
        this.listenerCmd = new CommandLine("Connection client listener", null, this.mainProcess);
        this.listenerCmd.RegisterCommand(new Command("ac-progress", Command.Wrap(this.progress), "", nArgs: 1));
        this.listenerCmd.RegisterCommand(new Command("ac-done", Command.Wrap(this.done), ""));
        this.listenerCmd.RegisterCommand(new Command("ac-cancel", Command.Wrap(this.serverCancel), ""));
        this.listenerCmd.RegisterCommand(new Command("ac-ko", Command.Wrap(this.ko), ""));
        this.listener = this.igc.UnicastListener;

        ini.Add(this);
        this.Read(ini);
        this.addCmds(commandLine);

        manager.AddOnSave(this.save);
        if (ini.ContainsKey(SECTION, "state")) {
          ConnectionState state;
          Enum.TryParse(ini.Get(SECTION, "state").ToString(), out state);
          this.State = state;
        }
      }

      public void Read(MyIni ini) {
        this.connector = this.gts.GetBlockWithName(ini.GetThrow(SECTION, "connector-name").ToString()) as IMyShipConnector;
        this.serverChannel = ini.GetThrow(SECTION, "server-channel").ToString();
      }

      public bool ShouldHandbrake() => BRAKE_STATES.Contains(this.state);

      // Client events
      void connect() {
        this.log("sending connection request");
        if (this.State != ConnectionState.Connected) {
          this.State = ConnectionState.WaitingCon;
          this.sendCon();
        }
      }
      void deco() {
        this.log("sending disconnection request");
        if (this.State == ConnectionState.WaitingCon || this.State == ConnectionState.Standby) {
          this.State = ConnectionState.Ready;
          this.setFailReason(FailReason.Cancellation);
          this.sendDisc();
        } else if (this.State == ConnectionState.Connected) {
          this.State = ConnectionState.WaitingDisc;
          this.sendDisc();
        }
      }
      void switchConnection() {
        if (this.State == ConnectionState.Connected) {
          this.deco();
        } else {
          this.connect();
        }
      }

      // Self events
      void clientCancel() {
        this.log("cancelled by client");
        this.State = ConnectionState.Ready;
        this.setFailReason(FailReason.Cancellation);
        this.sendDisc();
      }
      void timeout(Process p) {
        this.log("query timeouted");
        if (this.State == ConnectionState.WaitingCon || this.State == ConnectionState.WaitingDisc) {
          this.State = ConnectionState.Ready;
          this.setFailReason(FailReason.Timeout);
        }
      }

      // Server events
      void serverCancel() {
        if (this.State == ConnectionState.Connected || this.State == ConnectionState.WaitingCon) {
          this.log("received standby signal");
          this.State = ConnectionState.Standby;
        } else if (this.State != ConnectionState.Ready) {
          this.log("received cancel signal");
          this.State = ConnectionState.Ready;
          this.setFailReason(FailReason.Failure);
        }
      }
      void progress(string progress) {
        if (this.State == ConnectionState.WaitingCon
            || this.State == ConnectionState.WaitingDisc
            || this.State == ConnectionState.Standby) {
          if (this.State == ConnectionState.Standby) {
            this.State = ConnectionState.WaitingCon;
          }
          float pf;
          if (float.TryParse(progress, out pf)) {
            this.Progress = pf;
            this.timeOutProcess?.ResetCounter();
          }
        }
      }
      void done() {
        this.log("received done signal");
        if (this.State == ConnectionState.WaitingCon) {
          this.State = ConnectionState.Connected;
        } else if (this.State == ConnectionState.WaitingDisc) {
          this.State = ConnectionState.Ready;
        }
      }
      void ko() {
        this.log("received ko signal");
        this.State = ConnectionState.Ready;
        this.setFailReason(FailReason.Failure);
      }
      // helpers
      void save(MyIni ini) {
        ini.Set(SECTION, "connector-name", this.connector.DisplayNameText);
        ini.Set(SECTION, "server-channel", this.serverChannel);
        ini.Set(SECTION, "state", this.State.ToString());
      }
      void setState(ConnectionState state) {
        this.state = state;
        this.clearCallbacks();
        if (this.state == ConnectionState.Connected) {
          this.mainProcess.Spawn(this.checkCon, "cc-checkcon", period: 10, useOnce: false);
        } else if (this.state == ConnectionState.Standby) {
          this.startMoveListener();
        } else if (this.state == ConnectionState.WaitingCon) {
          this.startTimeOut();
          this.startMoveListener();
        } else if (this.state == ConnectionState.WaitingDisc) {
          this.startTimeOut();
        }
      }

      void sendCon() {
        CommandSerializer com = new CommandSerializer("ac-con").AddArg(this.connector.CubeGrid.GridSizeEnum);
        AddVector(this.connector.GetPosition(), com);
        AddVector(this.connector.WorldMatrix.Forward, com);
        this.igc.SendBroadcastMessage(this.serverChannel, com.ToString());
      }

      void sendDisc() => this.igc.SendBroadcastMessage(this.serverChannel, new CommandSerializer("ac-disc").ToString());

      void listen(Process p) {
        if (this.listener.HasPendingMessage) {
          this.listenerCmd.StartCmd(this.listener.AcceptMessage().As<string>(), CommandTrigger.Cmd);
        }
      }

      void clearCallbacks() {
        this.Progress = 0;
        this.position = null;

        this.mainProcess.KillChildren();

        this.timeOutProcess = null;
      }

      void startTimeOut() => this.timeOutProcess = this.mainProcess.Spawn(this.timeout, "cc-timeout", period: 50, useOnce: true);

      void startMoveListener() {
        this.position = this.connector.GetPosition();
        this.mainProcess.Spawn(this.checkPos, "cc-movelistener", period: 10, useOnce: false);
      }

      void setFailReason(FailReason state) {
        this.FailReason = state;
        this.mainProcess.Spawn(p => this.FailReason = FailReason.None, "cc-failreset", p => this.FailReason = FailReason.None, 500, true);
      }

      void checkPos(Process p) {
        if ((this.connector.GetPosition() - this.position.Value).LengthSquared() > DECO_DISTANCE_SQUARED) {
          this.clientCancel();
        }
      }

      void checkCon(Process p) {
        if (this.connector.Status != MyShipConnectorStatus.Connected) {
          this.State = ConnectionState.Ready;
          this.setFailReason(FailReason.User);
        }
      }

      void addCmds(CommandLine cmd) {
        cmd.RegisterCommand(new Command("ac-connect", Command.Wrap(this.connect), "Requests for an auto connection", nArgs: 0));
        cmd.RegisterCommand(new Command("ac-disconnect", Command.Wrap(this.deco), "Requests for disconnection", nArgs: 0));
        cmd.RegisterCommand(new Command("ac-switch", Command.Wrap(this.switchConnection), "Requests for connection/disconnection", nArgs: 0));
      }

      void log(string s) => this.logger?.Invoke("cc: " + s);


      static void AddVector(Vector3D v, CommandSerializer com) => com.AddArg(v.X).AddArg(v.Y).AddArg(v.Z);
    }
  }
}
