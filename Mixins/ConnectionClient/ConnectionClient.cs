using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

namespace IngameScript
{
  partial class Program
  {
    public enum ConnectionState { Connected, Ready, Standby, WaitingCon, WaitingDisc };

    public enum FailReason { Cancellation, Failure, User, Timeout, None }

    public class ConnectionClient : IIniConsumer, IPABraker
    {
      static readonly ConnectionState[] BRAKE_STATES = { ConnectionState.Connected, ConnectionState.WaitingCon, ConnectionState.WaitingDisc };
      const double DECO_DISTANCE_SQUARED = 0.04;
      const string SECTION = "connection-client";

      public float Progress { get; private set; } = 0;
      public ConnectionState State { get { return _state; } private set { _setState(value); } }
      public FailReason FailReason { get; private set; } = FailReason.None;

      IMyShipConnector _connector;
      readonly IMyGridTerminalSystem _gts;
      readonly IMyIntergridCommunicationSystem _igc;
      readonly IMyUnicastListener _listener;
      readonly CommandLine _listenerCmd;
      readonly Action<string> _logger;
      readonly Process _mainProcess;
      Vector3D? _position = null;
      string _serverChannel;
      ConnectionState _state = ConnectionState.Ready;
      Process _timeOutProcess = null;

      public ConnectionClient(IniWatcher ini, IMyGridTerminalSystem gts, IMyIntergridCommunicationSystem igc, CommandLine commandLine, IProcessManager manager, Action<string> logger)
      {
        _gts = gts;
        _igc = igc;
        _logger = logger;

        _mainProcess = manager.Spawn(_listen, "cc-listen", period: 5);
        _listenerCmd = new CommandLine("Connection client listener", null, _mainProcess);
        _listenerCmd.RegisterCommand(new Command("ac-progress", Command.Wrap(_progress), "", nArgs: 1));
        _listenerCmd.RegisterCommand(new Command("ac-done", Command.Wrap(_done), ""));
        _listenerCmd.RegisterCommand(new Command("ac-cancel", Command.Wrap(_serverCancel), ""));
        _listenerCmd.RegisterCommand(new Command("ac-ko", Command.Wrap(_ko), ""));
        _listener = _igc.UnicastListener;

        ini.Add(this);
        Read(ini);
        _addCmds(commandLine);

        manager.AddOnSave(_save);
        if (ini.ContainsKey(SECTION, "state"))
        {
          ConnectionState state;
          Enum.TryParse(ini.Get(SECTION, "state").ToString(), out state);
          State = state;
        }
      }

      public void Read(MyIni ini)
      {
        _connector = _gts.GetBlockWithName(ini.GetThrow(SECTION, "connector-name").ToString()) as IMyShipConnector;
        _serverChannel = ini.GetThrow(SECTION, "server-channel").ToString();
      }

      public bool ShouldHandbrake() => BRAKE_STATES.Contains(_state);

      // Client events
      void _connect()
      {
        _log("sending connection request");
        if (State != ConnectionState.Connected)
        {
          State = ConnectionState.WaitingCon;
          _sendCon();
        }
      }
      void _deco()
      {
        _log("sending disconnection request");
        if (State == ConnectionState.WaitingCon || State == ConnectionState.Standby)
        {
          State = ConnectionState.Ready;
          _setFailReason(FailReason.Cancellation);
          _sendDisc();
        }
        else if (State == ConnectionState.Connected)
        {
          State = ConnectionState.WaitingDisc;
          _sendDisc();
        }
      }
      void _switchConnection()
      {
        if (State == ConnectionState.Connected)
        {
          _deco();
        }
        else
        {
          _connect();
        }
      }

      // Self events
      void _clientCancel()
      {
        _log("cancelled by client");
        State = ConnectionState.Ready;
        _setFailReason(FailReason.Cancellation);
        _sendDisc();
      }
      void _timeout(Process p)
      {
        _log("query timeouted");
        if (State == ConnectionState.WaitingCon || State == ConnectionState.WaitingDisc)
        {
          State = ConnectionState.Ready;
          _setFailReason(FailReason.Timeout);
        }
      }

      // Server events
      void _serverCancel()
      {
        if (State == ConnectionState.Connected || State == ConnectionState.WaitingCon)
        {
          _log("received standby signal");
          State = ConnectionState.Standby;
        }
        else if (State != ConnectionState.Ready)
        {
          _log("received cancel signal");
          State = ConnectionState.Ready;
          _setFailReason(FailReason.Failure);
        }
      }
      void _progress(string progress)
      {
        if (State == ConnectionState.WaitingCon
            || State == ConnectionState.WaitingDisc
            || State == ConnectionState.Standby)
        {
          if (State == ConnectionState.Standby)
          {
            State = ConnectionState.WaitingCon;
          }
          float pf;
          if (float.TryParse(progress, out pf))
          {
            Progress = pf;
            _timeOutProcess?.ResetCounter();
          }
        }
      }
      void _done()
      {
        _log("received done signal");
        if (State == ConnectionState.WaitingCon)
        {
          State = ConnectionState.Connected;
        }
        else if (State == ConnectionState.WaitingDisc)
        {
          State = ConnectionState.Ready;
        }
      }
      void _ko()
      {
        _log("received ko signal");
        State = ConnectionState.Ready;
        _setFailReason(FailReason.Failure);
      }
      // helpers
      void _save(MyIni ini)
      {
        ini.Set(SECTION, "connector-name", _connector.CustomName);
        ini.Set(SECTION, "server-channel", _serverChannel);
        ini.Set(SECTION, "state", State.ToString());
      }
      void _setState(ConnectionState state)
      {
        _state = state;
        _clearCallbacks();
        if (_state == ConnectionState.Connected)
        {
          _mainProcess.Spawn(_checkCon, "cc-checkcon", period: 10, useOnce: false);
        }
        else if (_state == ConnectionState.Standby)
        {
          _startMoveListener();
        }
        else if (_state == ConnectionState.WaitingCon)
        {
          _startTimeOut();
          _startMoveListener();
        }
        else if (_state == ConnectionState.WaitingDisc)
        {
          _startTimeOut();
        }
      }

      void _sendCon()
      {
        CommandSerializer com = new CommandSerializer("ac-con").AddArg(_connector.CubeGrid.GridSizeEnum);
        _addVector(_connector.GetPosition(), com);
        _addVector(_connector.WorldMatrix.Forward, com);
        _igc.SendBroadcastMessage(_serverChannel, com.ToString());
      }

      void _sendDisc() => _igc.SendBroadcastMessage(_serverChannel, new CommandSerializer("ac-disc").ToString());

      void _listen(Process p)
      {
        if (_listener.HasPendingMessage)
        {
          _listenerCmd.StartCmd(_listener.AcceptMessage().As<string>(), CommandTrigger.Cmd);
        }
      }

      void _clearCallbacks()
      {
        Progress = 0;
        _position = null;

        _mainProcess.KillChildren();

        _timeOutProcess = null;
      }

      void _startTimeOut() => _timeOutProcess = _mainProcess.Spawn(_timeout, "cc-timeout", period: 50, useOnce: true);

      void _startMoveListener()
      {
        _position = _connector.GetPosition();
        _mainProcess.Spawn(_checkPos, "cc-movelistener", period: 10, useOnce: false);
      }

      void _setFailReason(FailReason state)
      {
        FailReason = state;
        _mainProcess.Spawn(p => FailReason = FailReason.None, "cc-failreset", p => FailReason = FailReason.None, 500, true);
      }

      void _checkPos(Process p)
      {
        if ((_connector.GetPosition() - _position.Value).LengthSquared() > DECO_DISTANCE_SQUARED)
        {
          _clientCancel();
        }
      }

      void _checkCon(Process p)
      {
        if (_connector.Status != MyShipConnectorStatus.Connected)
        {
          State = ConnectionState.Ready;
          _setFailReason(FailReason.User);
        }
      }

      void _addCmds(CommandLine cmd)
      {
        cmd.RegisterCommand(new Command("ac-connect", Command.Wrap(_connect), "Requests for an auto connection", nArgs: 0));
        cmd.RegisterCommand(new Command("ac-disconnect", Command.Wrap(_deco), "Requests for disconnection", nArgs: 0));
        cmd.RegisterCommand(new Command("ac-switch", Command.Wrap(_switchConnection), "Requests for connection/disconnection", nArgs: 0));
      }

      void _log(string s) => _logger?.Invoke("cc: " + s);


      static void _addVector(Vector3D v, CommandSerializer com) => com.AddArg(v.X).AddArg(v.Y).AddArg(v.Z);
    }
  }
}
