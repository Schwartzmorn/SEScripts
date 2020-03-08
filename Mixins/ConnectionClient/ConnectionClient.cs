using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

namespace IngameScript {
partial class Program {
  public enum ConnectionState { Connected, Ready, Standby,  WaitingCon, WaitingDisc };

  public enum FailReason { Cancellation, Failure, User, Timeout, None }

  public class ConnectionClient: JobProvider, IIniConsumer, IPABraker {
    private static readonly ConnectionState[] BRAKE_STATES = { ConnectionState.Connected, ConnectionState.WaitingCon, ConnectionState.WaitingDisc };
    private const double DECO_DISTANCE_SQUARED = 0.1;
    private const string SECTION = "connection-client";

    public string ClientChannel { get; private set; }
    public float Progress { get; private set; } = 0;
    public ConnectionState State { get { return _state; } private set { _setState(value); } }
    public FailReason FailReason { get; private set; } = FailReason.None;

    private readonly List<ScheduledAction> _callbacks = new List<ScheduledAction>(3);
    private readonly CmdLine _cmd = new CmdLine("Connection client listener");
    private IMyShipConnector _con;
    private readonly IMyGridTerminalSystem _gts;
    private readonly IMyIntergridCommunicationSystem _igc;
    private Vector3D? _pos = null;
    private string _srvChannel;
    private readonly IMyUnicastListener _srvListener;
    private ConnectionState _state = ConnectionState.Ready;
    private ScheduledAction _timeOut = null;

    public ConnectionClient(Ini ini, MyGridProgram p, CmdLine cmd) {
      _igc = p.IGC;
      _gts = p.GridTerminalSystem;

      _srvListener = _igc.UnicastListener;
      _cmd.AddCmd(new Cmd("ac-progress", "", s => _progress(s[0]), nArgs: 1));
      _cmd.AddCmd(new Cmd("ac-done", "", _ => _done(), nArgs: 0));
      _cmd.AddCmd(new Cmd("ac-cancel", "", _ => _serverCancel(), nArgs: 0));
      _cmd.AddCmd(new Cmd("ac-ko", "", _ => _ko(), nArgs: 0));
      Schedule(new ScheduledAction(_listen, 5, false, "cc-listen"));

      ini.Add(this);
      Read(ini);
      _addCmds(cmd);
      ScheduleOnSave(_save);
      if (ini.ContainsKey(SECTION, "state")) {
        ConnectionState state;
        Enum.TryParse(ini.Get(SECTION, "state").ToString(), out state);
        State = state;
      }
    }

    public void Read(Ini ini) {
      _con = _gts.GetBlockWithName(ini.GetThrow(SECTION, "connector-name").ToString()) as IMyShipConnector;
      _srvChannel = ini.GetThrow(SECTION, "server-channel").ToString();
      ClientChannel = ini.GetThrow(SECTION, "client-channel").ToString();
    }

    public bool ShouldHandbrake() => BRAKE_STATES.Contains(_state);

    // Client events
    void _connect() {
      if (State != ConnectionState.Connected) {
        State = ConnectionState.WaitingCon;
        _sendCon();
      }
    }
    void _deco() {
      if (State == ConnectionState.WaitingCon || State == ConnectionState.Standby) {
        State = ConnectionState.Ready;
        _setFR(FailReason.Cancellation);
        _sendDisc();
      } else if (State == ConnectionState.Connected) {
        State = ConnectionState.WaitingDisc;
        _sendDisc();
      }
    }
    void _switch() {
      if (State == ConnectionState.Connected)
        _deco();
      else
        _connect();
    }

    // Self events
    void _clientCancel() {
      State = ConnectionState.Ready;
      _setFR(FailReason.Cancellation);
      _sendDisc();
    }
    void _timeout() {
      if (State == ConnectionState.WaitingCon || State == ConnectionState.WaitingDisc) {
        State = ConnectionState.Ready;
        _setFR(FailReason.Timeout);
      }
    }

    // Server events
    void _serverCancel() {
      CancelCallback();
      if (State == ConnectionState.Connected || State == ConnectionState.WaitingCon) {
        State = ConnectionState.Standby;
      } else if (State != ConnectionState.Ready) {
        State = ConnectionState.Ready;
        _setFR(FailReason.Failure);
      }
    }
    void _progress(string p) {
      if (State == ConnectionState.WaitingCon
          || State == ConnectionState.WaitingDisc
          || State == ConnectionState.Standby) {
        if(State == ConnectionState.Standby)
          State = ConnectionState.WaitingCon;
        float pf = 0;
        if(float.TryParse(p, out pf)) {
          Progress = pf;
          _timeOut?.ResetCounter();
        }
      }
    }
    void _done() {
      if (State == ConnectionState.WaitingCon)
        State = ConnectionState.Connected;
      else if (State == ConnectionState.WaitingDisc)
        State = ConnectionState.Ready;
      StopCallback(State == ConnectionState.Connected ? "Connected" : "Disconnected");
    }
    void _reconnect() {
      if (State == ConnectionState.Standby)
        State = ConnectionState.WaitingCon;
    }
      void _ko() {
      CancelCallback();
      State = ConnectionState.Ready;
      _setFR(FailReason.Failure);
    }
    // helpers
    void _save(MyIni ini) {
      ini.Set(SECTION, "client-channel", ClientChannel);
      ini.Set(SECTION, "connector-name", _con.DisplayNameText);
      ini.Set(SECTION, "server-channel", _srvChannel);
      ini.Set(SECTION, "state", State.ToString());
    }
    void _setState(ConnectionState state) {
      _state = state;
      _clrCallbacks();
      if(_state == ConnectionState.Connected)
        _schedule(new ScheduledAction(_checkCon, 10, false, "cc-checkcon"));
      else if(_state == ConnectionState.Standby)
        _startMoveListener();
      else if(_state == ConnectionState.WaitingCon) {
        _startTimeOut();
        _startMoveListener();
      } else if(_state == ConnectionState.WaitingDisc)
        _startTimeOut();
    }

    void _sendCon() {
      var com = new CmdSerializer("ac-con").AddArg(_con.CubeGrid.GridSizeEnum).AddArg(ClientChannel);
      AddVector(_con.GetPosition(), com);
      AddVector(_con.WorldMatrix.Forward, com);
      _igc.SendBroadcastMessage(_srvChannel, com.ToString());
    }

    void _sendDisc() => _igc.SendBroadcastMessage(_srvChannel, new CmdSerializer("ac-disc").AddArg(ClientChannel).ToString());

    void _listen() {
      if (_srvListener.HasPendingMessage)
        _cmd.HandleCmd(_srvListener.AcceptMessage().As<string>(), CmdTrigger.Cmd);
    }

    void _clrCallbacks() {
      Progress = 0;
      FailReason = FailReason.None;
      _pos = null;

      _callbacks.ForEach(a => a.Dispose());
      _callbacks.Clear();

      _timeOut?.Dispose();
      _timeOut = null;
    }

    void _startTimeOut() {
      _timeOut = new ScheduledAction(_timeout, 50, true, "cc-timeout");
      Schedule(_timeOut);
    }

    void _startMoveListener() {
      _pos = _con.GetPosition();
      _schedule(new ScheduledAction(_checkPos, 10, false, "cc-movelistener"));
    }

    void _setFR(FailReason state) {
      FailReason = state;
      _schedule(new ScheduledAction(() => FailReason = FailReason.None, 500, true, "cc-failreset"));
    }

    void _checkPos() {
      if ((_con.GetPosition() - _pos.Value).LengthSquared() > DECO_DISTANCE_SQUARED)
        _clientCancel();
    }

    void _checkCon() {
      if (_con.Status != MyShipConnectorStatus.Connected) {
        State = ConnectionState.Ready;
        _setFR(FailReason.User);
      }
    }

    void _schedule(ScheduledAction cb) {
      _callbacks.Add(cb);
      Schedule(cb);
    }

    void _addCmds(CmdLine cmd) {
      cmd.AddCmd(new Cmd("ac-connect", "Requests for an auto connection", (_, c) => StartJob(_connect, c), nArgs: 0 ));
      cmd.AddCmd(new Cmd("ac-disconnect", "Requests for disconnection", (_, c) => StartJob(_deco, c), nArgs: 0));
      cmd.AddCmd(new Cmd("ac-switch", "Requests for connection/disconnection", (_, c) => StartJob(_switch, c),  nArgs: 0));
    }

    static void AddVector(Vector3D v, CmdSerializer com) => com.AddArg(v.X).AddArg(v.Y).AddArg(v.Z);
  }
}
}
