using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

namespace IngameScript {
  partial class Program {
    public enum ConnectionState { Connected, Ready, Standby,  WaitingCon, WaitingDisc };

    public enum FailReason { Cancellation, Failure, User, Timeout, None }

    public class ConnectionClient: IAHBraker {
      static readonly ConnectionState[] BRAKE_STATES = { ConnectionState.Connected, ConnectionState.WaitingCon, ConnectionState.WaitingDisc };
      const double DECO_DISTANCE_SQUARED = 0.1;
      const string SECTION = "connection-client";

      public readonly string ClientChannel;
      public float CurrentProgress { get; set; } = 0;
      public ConnectionState State { get { return _state; } set { _setState(value); } }
      public FailReason FailureState { get; set; } = FailReason.None;

      readonly List<ScheduledAction> _callbacks = new List<ScheduledAction>(3);
      readonly CmdLine _cmd = new CmdLine("Connection client listener");
      readonly IMyShipConnector _con;
      readonly IMyIntergridCommunicationSystem _igc;
      Vector3D? _pos = null;
      readonly string _srvChannel;
      readonly IMyUnicastListener _srvListener;
      ConnectionState _state = ConnectionState.Ready;
      ScheduledAction _timeOut = null;

      public ConnectionClient(MyGridProgram program, MyIni ini, CmdLine command, string srvChannel, string clientChannel) {
        ClientChannel = clientChannel;
        _con = program.GridTerminalSystem.GetBlockWithName(ini.GetThrow(SECTION, "connector-name").ToString()) as IMyShipConnector;
        _igc = program.IGC;
        _srvChannel = srvChannel;
        _addCommands(command);
        _registerListener(clientChannel, out _srvListener);
        Scheduler.Inst.AddActionOnSave(_save);
        if (ini.ContainsKey(SECTION, "state")) {
          ConnectionState state;
          Enum.TryParse(ini.Get(SECTION, "state").ToString(), out state);
          State = state;
        }
      }

      public bool ShouldHandbrake() => BRAKE_STATES.Contains(_state);

      // Client events
      protected void Connect() {
        if (State != ConnectionState.Connected) {
          State = ConnectionState.WaitingCon;
          _sendConnection();
        }
      }
      protected void Disconnect() {
        if (State == ConnectionState.WaitingCon || State == ConnectionState.Standby) {
          State = ConnectionState.Ready;
          _setFailureState(FailReason.Cancellation);
          _sendDisconnection();
        } else if (State == ConnectionState.Connected) {
          State = ConnectionState.WaitingDisc;
          _sendDisconnection();
        }
      }
      protected void Switch() {
        if (State == ConnectionState.Connected) {
          Disconnect();
        } else {
          Connect();
        }
      }

      // Self events
      protected void CancelByClient() {
        State = ConnectionState.Ready;
        _setFailureState(FailReason.Cancellation);
        _sendDisconnection();
      }
      protected void Timeout() {
        if (State == ConnectionState.WaitingCon || State == ConnectionState.WaitingDisc) {
          State = ConnectionState.Ready;
          _setFailureState(FailReason.Timeout);
        }
      }

      // Server events
      protected void CancelByServer() {
        if (State == ConnectionState.Connected || State == ConnectionState.WaitingCon) {
          State = ConnectionState.Standby;
        } else if (State != ConnectionState.Ready) {
          State = ConnectionState.Ready;
          _setFailureState(FailReason.Failure);
        }
      }
      protected void Progress(string p) {
        if (State == ConnectionState.WaitingCon
            || State == ConnectionState.WaitingDisc
            || State == ConnectionState.Standby) {
          if(State == ConnectionState.Standby) {
            State = ConnectionState.WaitingCon;
          }
          float pf = 0;
          if(float.TryParse(p, out pf)) {
            CurrentProgress = pf;
            _timeOut?.ResetCounter();
          }
        }
      }
      protected void Done() {
        if (State == ConnectionState.WaitingCon) {
          State = ConnectionState.Connected;
        } else if (State == ConnectionState.WaitingDisc) {
          State = ConnectionState.Ready;
        }
      }
      protected void Reconnect() {
        if (State == ConnectionState.Standby) {
          State = ConnectionState.WaitingCon;
        }
      }
      protected void KO() {
        State = ConnectionState.Ready;
        _setFailureState(FailReason.Failure);
      }

      // helpers
      void _save(MyIni ini) {
        ini.Set(SECTION, "connector-name", _con.DisplayNameText);
        ini.Set(SECTION, "state", State.ToString());
      }

      void _setState(ConnectionState state) {
        _state = state;
        _resetCallbacks();
        if(_state == ConnectionState.Connected) {
          _scheduleCallback(new ScheduledAction(_checkConnection, period: 10));
        } else if(_state == ConnectionState.Ready) {
          // nada
        } else if(_state == ConnectionState.Standby) {
          _startMoveListener();
        } else if(_state == ConnectionState.WaitingCon) {
          _startTimeOut();
          _startMoveListener();
        } else if(_state == ConnectionState.WaitingDisc) {
          _startTimeOut();
        }
      }

      void _sendConnection() {
        var com = new CmdSerializer("ac-con");
        com.AddArg(_con.CubeGrid.GridSizeEnum);
        com.AddArg(ClientChannel);
        SerializeVector(_con.GetPosition(), com);
        SerializeVector(_con.WorldMatrix.Forward, com);
        _igc.SendBroadcastMessage(_srvChannel, com.ToString());
      }

      void _sendDisconnection() {
        var com = new CmdSerializer("ac-disc");
        com.AddArg(ClientChannel);
        _igc.SendBroadcastMessage(_srvChannel, com.ToString());
      }

      void _listenServer() {
        if (_srvListener.HasPendingMessage) {
          _cmd.HandleCmd(_srvListener.AcceptMessage().As<string>(), true);
        }
      }

      void _resetCallbacks() {
        CurrentProgress = 0;
        FailureState = FailReason.None;
        _pos = null;

        _callbacks.ForEach(a => a.Dispose());
        _callbacks.Clear();

        _timeOut?.Dispose();
        _timeOut = null;
      }

      void _startTimeOut() {
        _timeOut = new ScheduledAction(Timeout, period: 50, useOnce: true);
        Scheduler.Inst.AddAction(_timeOut);
      }

      void _startMoveListener() {
        _pos = _con.GetPosition();
        _scheduleCallback(new ScheduledAction(_checkPosition, period: 10));
      }

      void _setFailureState(FailReason state) {
        FailureState = state;
        _scheduleCallback(new ScheduledAction(() => FailureState = FailReason.None, period: 500, useOnce: true));
      }

      void _checkPosition() {
        if ((_con.GetPosition() - _pos.Value).LengthSquared() > DECO_DISTANCE_SQUARED) {
          CancelByClient();
        }
      }

      void _checkConnection() {
        if (_con.Status != MyShipConnectorStatus.Connected) {
          State = ConnectionState.Ready;
          _setFailureState(FailReason.User);
        }
      }

      void _scheduleCallback(ScheduledAction callback) {
        _callbacks.Add(callback);
        Scheduler.Inst.AddAction(callback);
      }

      void _addCommands(CmdLine command) {
        command.AddCmd(new Cmd(
          "ac-connect", "Requests for an auto connection",
          _ => Connect(),
          detailedHelp: $@"Picks a random connector on the grid and requests for its connection
It will broadcast on the channel '{_srvChannel}'",
          maxArgs: 0
          ));
        command.AddCmd(new Cmd(
          "ac-disconnect", "Requests for disconnection",
          _ => Disconnect(),
          detailedHelp: $@"Disconnects a connected connector and notifies of the disconnection
It will broadcast on the channel '{_srvChannel}'",
          maxArgs: 0
          ));
        command.AddCmd(new Cmd(
          "ac-switch", "Requests for connection/disconnection",
          _ => Switch(),
          detailedHelp: $@"Depending on whether it is connected or not:
It will request a disconnection or a connection
It will broadcast on the channel '{_srvChannel}'",
          maxArgs: 0
          ));
      }

      void _registerListener(string channel, out IMyUnicastListener listener) {
        listener = _igc.UnicastListener;
        _cmd.AddCmd(new Cmd("ac-progress", "Connection progress", s => Progress(s[0]), minArgs: 1, maxArgs: 1));
        _cmd.AddCmd(new Cmd("ac-done", "When the request is done", _ => Done(), maxArgs: 0));
        _cmd.AddCmd(new Cmd("ac-cancel", "When the request has been cancelled", _ => CancelByServer(), maxArgs: 0));
        _cmd.AddCmd(new Cmd("ac-ko", "When the request failed", _ => KO(), maxArgs: 0));
        Scheduler.Inst.AddAction(new ScheduledAction(_listenServer, period: 5));
      }

      static void SerializeVector(Vector3D v, CmdSerializer com) {
        com.AddArg(v.X);
        com.AddArg(v.Y);
        com.AddArg(v.Z);
      }
    }
  }
}
