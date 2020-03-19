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

      public string Name => this._connector.Name;
      public ConnectionRequest? CurrentRequest { get; private set; }
      public ConnectionRequest? PreviousRequest { get; private set; }

      private readonly List<ScheduledAction> _callbacks = new List<ScheduledAction>(2);
      private readonly AutoConnector _connector;
      private readonly IMyIntergridCommunicationSystem _igc;

      public AutoConnectionServer(MyIni ini, IMyIntergridCommunicationSystem igc, AutoConnector connector)
          : this(igc, connector) {
        this._deserialize(ini);
      }

      public AutoConnectionServer(IMyIntergridCommunicationSystem igc, AutoConnector connector) {
        this._connector = connector;
        this._igc = igc;
      }

      public bool IsInRange(Vector3D position) => this._connector.GetDistance(position) < DISTANCE_CUTOFF;

      public bool HasPendingRequest(long address) => address == this.CurrentRequest?.Address || address == this.PreviousRequest?.Address;

      public void Connect(ConnectionRequest request) {
        if (this.CurrentRequest != null && this.CurrentRequest?.Address == request.Address) {
          this.CurrentRequest = request;
        } else {
          if (this.PreviousRequest != null && this.PreviousRequest.Value.Address != request.Address) {
            this._sendCancel(this.PreviousRequest.Value);
          }
          this.PreviousRequest = this.CurrentRequest;
          this.CurrentRequest = request;
        }
        if(this.PreviousRequest != null) {
          this._sendCancel(this.PreviousRequest.Value);
        }
        this._connector.Connect(this.CurrentRequest.Value.Size, this.CurrentRequest.Value.Position, this.CurrentRequest.Value.Orientation);
        this._startConnectionCallback(this.CurrentRequest.Value);
      }

      public void Disconnect(long address) {
        Log("Disconnecting");
        if(this.PreviousRequest != null && (this.PreviousRequest?.Address == address)) {
          this.PreviousRequest = null;
        } else if (this.CurrentRequest != null && (this.CurrentRequest?.Address == address)) {
          var requestToNotify = this.CurrentRequest.Value;
          this.CurrentRequest = this.PreviousRequest;
          this.PreviousRequest = null;
          if (this.CurrentRequest != null) {
            this.Connect(this.CurrentRequest.Value);
          } else {
            this._connector.Disconnect();
            this._clearCallbacks();
          }
          double totalLength = this._connector.GetRemainingLength();
          this._scheduleCallback(new ScheduledAction(
              () => this._checkDisconnectionProgress(requestToNotify, totalLength),
              period: 20,
              name: $"con-serv-prog-{this._connector.Name}"));
        }
      }

      public void Reset() {
        this._clearCallbacks();
        if (this.CurrentRequest != null) {
          this._sendCancel(this.CurrentRequest.Value);
        }
        if(this.PreviousRequest != null) {
          this._sendCancel(this.PreviousRequest.Value);
        }
        this.CurrentRequest = null;
        this.PreviousRequest = null;
        this._connector.Disconnect();
      }

      public void Save(MyIni ini) {
        if(this.CurrentRequest != null) {
          string sectionName = $"{INI_SECTION}{this._connector.Name}";
          this._saveRequest(ini, sectionName, "current", this.CurrentRequest.Value);
          if(this.PreviousRequest != null) {
            this._saveRequest(ini, sectionName, "previous", this.PreviousRequest.Value);
          }
        }
        this._connector.Save(ini);
      }

      public bool Update() => this._connector.Update();

      private void _startConnectionCallback(ConnectionRequest request) {
        this._clearCallbacks();
        double length = this._connector.GetRemainingLength();
        this._scheduleCallback(new ScheduledAction(() => this._checkConnectionProgress(request, length), period: 20));
      }

      private void _checkConnectionProgress(ConnectionRequest request, double totalLength) {
        float progress = (float)((totalLength - this._connector.GetRemainingLength()) / totalLength);
        if (this._connector.IsMoving()) {
          this._sendMessage(request, $"-ac-progress {MathHelper.Clamp(progress, 0, 1)}");
        } else {
          this._clearCallbacks();
          if (this._connector.IsConnected()) {
            this._sendMessage(request, "-ac-done");
            this._scheduleCallback(new ScheduledAction(_checkManualDisconnection, period: 20));
          } else {
            this._sendMessage(request, "-ac-ko");
            this.Disconnect(request.Address);
          }
        }
      }

      private void _checkManualDisconnection() {
        if (!this._connector.IsConnected()) {
          this._clearCallbacks();
          if (this.CurrentRequest != null) {
            this.Disconnect(this.CurrentRequest.Value.Address);
          } else {
            this.Reset();
          }
        }
      }

      private void _checkDisconnectionProgress(ConnectionRequest request, double totalLength) {
        float progress = (float)((totalLength - this._connector.GetRemainingLength()) / totalLength) * 3;
        if (progress > 1) {
          Scheduler.Inst.Remove($"con-serv-prog-{this._connector.Name}");
          this._sendMessage(request, "-ac-done");
        } else {
          this._sendMessage(request, $"-ac-progress {MathHelper.Clamp(progress, 0, 1)}");
        }
      }

      private void _sendCancel(ConnectionRequest request) => this._sendMessage(request, "-ac-cancel");

      private void _sendMessage(ConnectionRequest request, string message) => this._igc.SendUnicastMessage(request.Address, request.Channel, message);

      private void _clearCallbacks() {
        this._callbacks.ForEach(c => c.Dispose());
        this._callbacks.Clear();
      }

      private void _scheduleCallback(ScheduledAction action) {
        this._callbacks.Add(action);
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
        string sectionName = $"{INI_SECTION}{this._connector.Name}";
        if (ini.ContainsSection(sectionName)) {
          this.CurrentRequest = this._deserializeRequest(ini, sectionName, "current");
          if (ini.ContainsKey(sectionName, "previous")) {
            this.PreviousRequest = this._deserializeRequest(ini, sectionName, "previous");
          }
        }
        if (this._connector.IsConnected()) {
          this._scheduleCallback(new ScheduledAction(_checkManualDisconnection, period: 20));
        } else if (this.CurrentRequest != null) {
          this._startConnectionCallback(this.CurrentRequest.Value);
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
