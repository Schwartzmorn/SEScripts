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
      const double DECO_DISTANCE_SQUARED = 0.1;
      const string SECTION = "connection-client";

      public string ClientChannel { get; private set; }
      public float Progress { get; private set; } = 0;
      public ConnectionState State { get { return this.state; } private set { this.setState(value); } }
      public FailReason FailReason { get; private set; } = FailReason.None;

      IMyShipConnector connector;
      readonly IMyGridTerminalSystem gts;
      readonly IMyIntergridCommunicationSystem igc;
      readonly IMyUnicastListener listener;
      readonly CommandLine listenerCommandLine;
      readonly Process listenerProcess;
      Vector3D? position = null;
      readonly List<Process> processes = new List<Process>(3);
      string serverChannel;
      ConnectionState state = ConnectionState.Ready;
      Process timeOutProcess = null;

      public ConnectionClient(IniWatcher ini, MyGridProgram p, CommandLine commandLine, ISaveManager manager) {
        this.igc = p.IGC;
        this.gts = p.GridTerminalSystem;

        this.listenerProcess = manager.Spawn(this.listen, "cc-listen", period: 5);
        this.listenerCommandLine = new CommandLine("Connection client listener", null, this.listenerProcess);
        this.listenerCommandLine.RegisterCommand(new Command("ac-progress", Command.Wrap(this.progress), "", nArgs: 1));
        this.listenerCommandLine.RegisterCommand(new Command("ac-done", Command.Wrap(this.done), ""));
        this.listenerCommandLine.RegisterCommand(new Command("ac-cancel", Command.Wrap(this.serverCancel), ""));
        this.listenerCommandLine.RegisterCommand(new Command("ac-ko", Command.Wrap(this.ko), ""));
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
        this.ClientChannel = ini.GetThrow(SECTION, "client-channel").ToString();
      }

      public bool ShouldHandbrake() => BRAKE_STATES.Contains(this.state);

      // Client events
      void connect() {
        if (this.State != ConnectionState.Connected) {
          this.State = ConnectionState.WaitingCon;
          this.sendCon();
        }
      }
      void deco() {
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
        this.State = ConnectionState.Ready;
        this.setFailReason(FailReason.Cancellation);
        this.sendDisc();
      }
      void timeout(Process p) {
        if (this.State == ConnectionState.WaitingCon || this.State == ConnectionState.WaitingDisc) {
          this.State = ConnectionState.Ready;
          this.setFailReason(FailReason.Timeout);
        }
      }

      // Server events
      void serverCancel() {
        if (this.State == ConnectionState.Connected || this.State == ConnectionState.WaitingCon) {
          this.State = ConnectionState.Standby;
        } else if (this.State != ConnectionState.Ready) {
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
        if (this.State == ConnectionState.WaitingCon) {
          this.State = ConnectionState.Connected;
        } else if (this.State == ConnectionState.WaitingDisc) {
          this.State = ConnectionState.Ready;
        }
      }
      void ko() {
        this.State = ConnectionState.Ready;
        this.setFailReason(FailReason.Failure);
      }
      // helpers
      void save(MyIni ini) {
        ini.Set(SECTION, "client-channel", this.ClientChannel);
        ini.Set(SECTION, "connector-name", this.connector.DisplayNameText);
        ini.Set(SECTION, "server-channel", this.serverChannel);
        ini.Set(SECTION, "state", this.State.ToString());
      }
      void setState(ConnectionState state) {
        this.state = state;
        this.clearCallbacks();
        if (this.state == ConnectionState.Connected) {
          this.spawn(this.checkCon, "cc-checkcon", period: 10, useOnce: false);
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
        CommandSerializer com = new CommandSerializer("ac-con").AddArg(this.connector.CubeGrid.GridSizeEnum).AddArg(this.ClientChannel);
        AddVector(this.connector.GetPosition(), com);
        AddVector(this.connector.WorldMatrix.Forward, com);
        this.igc.SendBroadcastMessage(this.serverChannel, com.ToString());
      }

      void sendDisc() => this.igc.SendBroadcastMessage(this.serverChannel, new CommandSerializer("ac-disc").AddArg(this.ClientChannel).ToString());

      void listen(Process p) {
        if (this.listener.HasPendingMessage) {
          this.listenerCommandLine.StartCmd(this.listener.AcceptMessage().As<string>(), CommandTrigger.Cmd);
        }
      }

      void clearCallbacks() {
        this.Progress = 0;
        this.FailReason = FailReason.None;
        this.position = null;


        this.processes.ForEach(p => p.Kill());
        this.processes.Clear();

        this.timeOutProcess?.Kill();
      }

      void startTimeOut() => this.timeOutProcess = this.listenerProcess.Spawn(this.timeout, "cc-timeout", period: 50, useOnce: true);

      void startMoveListener() {
        this.position = this.connector.GetPosition();
        this.spawn(this.checkPos, "cc-movelistener", 10, false);
      }

      void setFailReason(FailReason state) {
        this.FailReason = state;
        this.spawn(p => this.FailReason = FailReason.None, "cc-failreset", 500, true);
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

      void spawn(Action<Process> action, string name, int period, bool useOnce) => this.processes.Add(this.listenerProcess.Spawn(action, name, period: period, useOnce: useOnce));

      void addCmds(CommandLine cmd) {
        cmd.RegisterCommand(new Command("ac-connect", Command.Wrap(this.connect), "Requests for an auto connection", nArgs: 0));
        cmd.RegisterCommand(new Command("ac-disconnect", Command.Wrap(this.deco), "Requests for disconnection", nArgs: 0));
        cmd.RegisterCommand(new Command("ac-switch", Command.Wrap(this.switchConnection), "Requests for connection/disconnection", nArgs: 0));
      }

      static void AddVector(Vector3D v, CommandSerializer com) => com.AddArg(v.X).AddArg(v.Y).AddArg(v.Z);
    }
  }
}
