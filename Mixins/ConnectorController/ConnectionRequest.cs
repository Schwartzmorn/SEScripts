using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
  partial class Program {

    public class ConnectionRequest {
      public readonly bool IsConnection;
      public bool IsDone { get; private set; }

      private readonly CommandLine _command;
      private readonly IConnectionEventListener _eventListener;
      private readonly IMyIntergridCommunicationSystem _igc;
      private readonly ScheduledAction _listeningAction;
      private readonly IMyBroadcastListener _messageListener;
      private readonly ScheduledAction _timeoutAction;
      private readonly ConnectionRequestor _requestor;
      public ConnectionRequest(ConnectionRequestor requestor, IConnectionEventListener eventListener,
          IMyIntergridCommunicationSystem igc, string channel, bool isConnection, bool isDone = false) {
        IsConnection = isConnection;
        IsDone = isDone;
        _igc = igc;
        _eventListener = eventListener;
        _messageListener = igc.RegisterBroadcastListener(channel);
        _listeningAction = new ScheduledAction(_listen);
        Scheduler.Instance.AddAction(_listeningAction);
        _requestor = requestor;
        _timeoutAction = new ScheduledAction(_timeout, period: 50, useOnce: true);
        if (!isDone) {
          Scheduler.Instance.AddAction(_timeoutAction);
        }
        _command = new CommandLine("ConnectionStatus", s => { });
        _command.AddCommand(new Command("ac-progess", "Connection progress", _onProgress, minArgs: 1, maxArgs: 1));
        _command.AddCommand(new Command("ac-done", "When the request is done", _onDone, maxArgs: 0));
        _command.AddCommand(new Command("ac-cancel", "When the request has been cancelled", _onCancel, maxArgs: 0));
      }

      public void Cancel() {
        _eventListener.OnCancel(IsConnection, true);
        _dispose();
      }

      private void _onProgress(List<string> args) {
        float progress;
        float.TryParse(args[0], out progress);
        _eventListener.OnProgress(IsConnection, progress);
        _timeoutAction.ResetCounter();
      }

      private void _onDone(List<string> args) {
        IsDone = true;
        _eventListener.OnDone(IsConnection);
        _timeoutAction.Dispose();
        // We want to keep the listener around when connected
        if (!IsConnection) {
          _dispose();
        }
      }

      private void _onCancel(List<string> args) {
        _eventListener.OnCancel(IsConnection, false);
        _dispose();
      }

      private void _listen() {
        if (_messageListener.HasPendingMessage) {
          _timeoutAction.ResetCounter();
          string message = _messageListener.AcceptMessage().As<string>();
          _command.HandleCommandLine(message, true);
        }
      }

      private void _timeout() {
        _eventListener.OnTimeout(IsConnection);
        _dispose();
      }

      private void _dispose() {
        _igc.DisableBroadcastListener(_messageListener);
        _listeningAction.Dispose();
        _timeoutAction.Dispose();
        _requestor.DisposeRequest();
      }
    }
  }
}
