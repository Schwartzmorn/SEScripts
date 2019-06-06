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
    public struct ConnectionRequest {
      public long Address;
      public string Channel;
      public MyCubeSize Size;
      public Vector3D Position;
      public Vector3D Orientation;
    }

    public class AutoConnectionServer {
      private static readonly double DISTANCE_CUTOFF = 2;
      private static readonly string INI_SECTION = "connection-server-";

      public string Name => _connector.Name;
      public ConnectionRequest? CurrentRequest { get; private set; }
      public ConnectionRequest? PreviousRequest { get; private set; }

      private readonly List<ScheduledAction> _callbacks = new List<ScheduledAction>(2);
      private readonly AutoConnector _connector;
      private readonly IMyIntergridCommunicationSystem _igc;

      public AutoConnectionServer(MyIni ini, IMyIntergridCommunicationSystem igc, AutoConnector connector)
          : this(igc, connector) {
        _deserialize(ini);
      }

      public AutoConnectionServer(IMyIntergridCommunicationSystem igc, AutoConnector connector) {
        _connector = connector;
        _igc = igc;
      }

      public bool IsInRange(Vector3D position) => _connector.GetDistance(position) < DISTANCE_CUTOFF;

      public bool HasPendingRequest(long address) => address == CurrentRequest?.Address || address == PreviousRequest?.Address;

      public void Connect(ConnectionRequest request) {
        if (CurrentRequest != null && CurrentRequest?.Address == request.Address) {
          CurrentRequest = request;
        } else {
          if (PreviousRequest != null && PreviousRequest.Value.Address != request.Address) {
            _sendCancel(PreviousRequest.Value);
          }
          PreviousRequest = CurrentRequest;
          CurrentRequest = request;
        }
        if(PreviousRequest != null) {
          _sendCancel(PreviousRequest.Value);
        }
        _connector.Connect(CurrentRequest.Value.Size, CurrentRequest.Value.Position, CurrentRequest.Value.Orientation);
        _startConnectionCallback(CurrentRequest.Value);
      }

      public void Disconnect(long address) {
        Log("Disconnecting");
        if(PreviousRequest != null && (PreviousRequest?.Address == address)) {
          PreviousRequest = null;
        } else if (CurrentRequest != null && (CurrentRequest?.Address == address)) {
          var requestToNotify = CurrentRequest.Value;
          CurrentRequest = PreviousRequest;
          PreviousRequest = null;
          if (CurrentRequest != null) {
            Connect(CurrentRequest.Value);
          } else {
            _connector.Disconnect();
            _clearCallbacks();
          }
          double totalLength = _connector.GetRemainingLength();
          _scheduleCallback(new ScheduledAction(
              () => _checkDisconnectionProgress(requestToNotify, totalLength),
              period: 20,
              name: $"con-serv-prog-{_connector.Name}"));
        }
      }

      public void Reset() {
        _clearCallbacks();
        if (CurrentRequest != null) {
          _sendCancel(CurrentRequest.Value);
        }
        if(PreviousRequest != null) {
          _sendCancel(PreviousRequest.Value);
        }
        CurrentRequest = null;
        PreviousRequest = null;
        _connector.Disconnect();
      }

      public void Save(MyIni ini) {
        if(CurrentRequest != null) {
          string sectionName = $"{INI_SECTION}{_connector.Name}";
          _saveRequest(ini, sectionName, "current", CurrentRequest.Value);
          if(PreviousRequest != null) {
            _saveRequest(ini, sectionName, "previous", PreviousRequest.Value);
          }
        }
        _connector.Save(ini);
      }

      public bool Update() => _connector.Update();

      private void _startConnectionCallback(ConnectionRequest request) {
        _clearCallbacks();
        double length = _connector.GetRemainingLength();
        _scheduleCallback(new ScheduledAction(() => _checkConnectionProgress(request, length), period: 20));
      }

      private void _checkConnectionProgress(ConnectionRequest request, double totalLength) {
        float progress = (float)((totalLength - _connector.GetRemainingLength()) / totalLength);
        if (_connector.IsMoving()) {
          _sendMessage(request, $"-ac-progress {MathHelper.Clamp(progress, 0, 1)}");
        } else {
          _clearCallbacks();
          if (_connector.IsConnected()) {
            _sendMessage(request, "-ac-done");
            _scheduleCallback(new ScheduledAction(_checkManualDisconnection, period: 20));
          } else {
            _sendMessage(request, "-ac-ko");
            Disconnect(request.Address);
          }
        }
      }

      private void _checkManualDisconnection() {
        if (!_connector.IsConnected()) {
          _clearCallbacks();
          if (CurrentRequest != null) {
            Disconnect(CurrentRequest.Value.Address);
          } else {
            Reset();
          }
        }
      }

      private void _checkDisconnectionProgress(ConnectionRequest request, double totalLength) {
        float progress = (float)((totalLength - _connector.GetRemainingLength()) / totalLength) * 3;
        if (progress > 1) {
          Scheduler.Inst.Remove($"con-serv-prog-{_connector.Name}");
          _sendMessage(request, "-ac-done");
        } else {
          _sendMessage(request, $"-ac-progress {MathHelper.Clamp(progress, 0, 1)}");
        }
      }

      private void _sendCancel(ConnectionRequest request) => _sendMessage(request, "-ac-cancel");

      private void _sendMessage(ConnectionRequest request, string message) => _igc.SendUnicastMessage(request.Address, request.Channel, message);

      private void _clearCallbacks() {
        _callbacks.ForEach(c => c.Dispose());
        _callbacks.Clear();
      }

      private void _scheduleCallback(ScheduledAction action) {
        _callbacks.Add(action);
        Schedule(action);
      }

      private void _saveRequest(MyIni ini, string sectionName, string requestName, ConnectionRequest request) {
        ini.Set(sectionName, $"{requestName}-address", request.Address);
        ini.Set(sectionName, $"{requestName}-channel", request.Channel);
        ini.SetVector(sectionName, $"{requestName}-orientation", request.Orientation);
        ini.SetVector(sectionName, $"{requestName}-position", request.Position);
        ini.Set(sectionName, $"{requestName}-size", request.Size.ToString());
      }

      private void _deserialize(MyIni ini) {
        string sectionName = $"{INI_SECTION}{_connector.Name}";
        if (ini.ContainsSection(sectionName)) {
          CurrentRequest = _deserializeRequest(ini, sectionName, "current");
          if (ini.ContainsKey(sectionName, "previous")) {
            PreviousRequest = _deserializeRequest(ini, sectionName, "previous");
          }
        }
        if (_connector.IsConnected()) {
          _scheduleCallback(new ScheduledAction(_checkManualDisconnection, period: 20));
        } else if (CurrentRequest != null) {
          _startConnectionCallback(CurrentRequest.Value);
        }
      }

      private ConnectionRequest _deserializeRequest(MyIni ini, string sectionName, string requestName) {
        return new ConnectionRequest {
          Address = ini.GetThrow(sectionName, $"{requestName}-address").ToInt64(),
          Channel = ini.GetThrow(sectionName, $"{requestName}-channel").ToString(),
          Orientation = ini.GetVector(sectionName, $"{requestName}-orientation"),
          Position = ini.GetVector(sectionName, $"{requestName}-position"),
          Size = (MyCubeSize)Enum.Parse(typeof(MyCubeSize), ini.GetThrow(sectionName, $"{requestName}-size").ToString())
        };
      }
    }
  }
}
