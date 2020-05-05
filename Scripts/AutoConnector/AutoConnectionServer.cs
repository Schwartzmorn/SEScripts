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
    /// <summary>This class handles the requests made to an <see cref="AutoConnector"/>, sending updates to requestor, handling reconnection, automatic disconnections</summary>
    public struct ConnectionRequest {
      public long Address;
      public MyCubeSize Size;
      public Vector3D Position;
      public Vector3D Orientation;
    }

    public class AutoConnectionServer {
      private static readonly double DISTANCE_CUTOFF = 2;
      private static readonly string INI_SECTION = "connection-server-";

      public string Name => this.connector.Name;
      public ConnectionRequest? CurrentRequest { get; private set; }
      public ConnectionRequest? PreviousRequest { get; private set; }

      readonly AutoConnector connector;
      readonly IMyIntergridCommunicationSystem igc;
      readonly Action<string> logger;
      readonly Process mainProcess;

      public AutoConnectionServer(MyIni ini, IMyIntergridCommunicationSystem igc, AutoConnector connector, IProcessSpawner spawner, Action<string> logger)
          : this(igc, connector, spawner) {
        this.logger = logger;
        this.deserialize(ini);
      }

      public AutoConnectionServer(IMyIntergridCommunicationSystem igc, AutoConnector connector, IProcessSpawner spawner) {
        this.connector = connector;
        this.igc = igc;
        this.mainProcess = spawner.Spawn(p => this.Update(), $"ac-server '{this.Name}'", onDone => this.connector.Stop());
        this.logger?.Invoke($"Connector {this.Name} ready");
      }

      public bool IsInRange(Vector3D position) => this.connector.GetDistance(position) < DISTANCE_CUTOFF;

      public bool HasPendingRequest(long address) => address == this.CurrentRequest?.Address || address == this.PreviousRequest?.Address;

      public void Connect(ConnectionRequest request) {
        if (this.CurrentRequest != null && this.CurrentRequest?.Address == request.Address) {
          this.CurrentRequest = request;
        } else {
          if (this.PreviousRequest != null && this.PreviousRequest.Value.Address != request.Address) {
            this.sendCancel(this.PreviousRequest.Value);
          }
          this.PreviousRequest = this.CurrentRequest;
          this.CurrentRequest = request;
        }
        if(this.PreviousRequest != null) {
          this.sendCancel(this.PreviousRequest.Value);
        }
        this.connector.Connect(this.CurrentRequest.Value.Size, this.CurrentRequest.Value.Position, this.CurrentRequest.Value.Orientation);
        this.startConnectionCallback(this.CurrentRequest.Value);
      }

      public void Disconnect(long address) {
        if (this.PreviousRequest != null && (this.PreviousRequest?.Address == address)) {
          this.PreviousRequest = null;
        } else if (this.CurrentRequest != null && (this.CurrentRequest?.Address == address)) {
          ConnectionRequest requestToNotify = this.CurrentRequest.Value;
          this.CurrentRequest = this.PreviousRequest;
          this.PreviousRequest = null;
          if (this.CurrentRequest != null) {
            this.Connect(this.CurrentRequest.Value);
          } else {
            this.connector.Disconnect();
            this.mainProcess.KillChildren();
          }
          double totalLength = this.connector.GetRemainingLength();
          this.mainProcess.Spawn(p => this.checkDisconnectionProgress(p, requestToNotify, totalLength), "disconnection-progress", p => this.sendMessage(requestToNotify, "-ac-done"), 10);
        }
      }

      public void Reset() {
        this.mainProcess.KillChildren();
        if (this.CurrentRequest != null) {
          this.sendCancel(this.CurrentRequest.Value);
        }
        if(this.PreviousRequest != null) {
          this.sendCancel(this.PreviousRequest.Value);
        }
        this.CurrentRequest = null;
        this.PreviousRequest = null;
        this.connector.Disconnect();
      }

      public void Save(MyIni ini) {
        if(this.CurrentRequest != null) {
          string sectionName = $"{INI_SECTION}{this.connector.Name}";
          this.saveRequest(ini, sectionName, "current", this.CurrentRequest.Value);
          if(this.PreviousRequest != null) {
            this.saveRequest(ini, sectionName, "previous", this.PreviousRequest.Value);
          }
        }
        this.connector.Save(ini);
      }
      public bool Update() => this.connector.Update();

      public void Kill() => this.mainProcess.Kill();

      void startConnectionCallback(ConnectionRequest request) {
        this.mainProcess.KillChildren();
        double length = this.connector.GetRemainingLength();
        this.mainProcess.Spawn(p => this.checkConnectionProgress(p, request, length), "connection-progress", period: 10);
      }

      void checkConnectionProgress(Process p, ConnectionRequest request, double totalLength) {
        float progress = (float)((totalLength - this.connector.GetRemainingLength()) / totalLength);
        if (this.connector.IsMoving()) {
          this.sendMessage(request, $"-ac-progress {MathHelper.Clamp(progress, 0, 1)}");
        } else {
          this.mainProcess.KillChildren();
          if (this.connector.IsConnected()) {
            this.sendMessage(request, "-ac-done");
            this.mainProcess.Spawn(this.checkManualDisconnection, "check-manual-disc", period: 10);
          } else {
            this.sendMessage(request, "-ac-ko");
            this.Disconnect(request.Address);
          }
        }
      }

      void checkManualDisconnection(Process p) {
        if (!this.connector.IsConnected()) {
          this.mainProcess.KillChildren();
          if (this.CurrentRequest != null) {
            this.Disconnect(this.CurrentRequest.Value.Address);
          } else {
            this.Reset();
          }
        }
      }

      void checkDisconnectionProgress(Process p, ConnectionRequest request, double totalLength) {
        float progress = (float)((totalLength - this.connector.GetRemainingLength()) / totalLength) * 3;
        if (progress > 1) {
          p.Kill();
        } else {
          this.sendMessage(request, $"-ac-progress {MathHelper.Clamp(progress, 0, 1)}");
        }
      }

      void sendCancel(ConnectionRequest request) => this.sendMessage(request, "-ac-cancel");

      void sendMessage(ConnectionRequest request, string message) => this.igc.SendUnicastMessage(request.Address, "", message);

      void saveRequest(MyIni ini, string sectionName, string requestName, ConnectionRequest request) {
        ini.Set(sectionName, $"{requestName}-address", request.Address);
        ini.SetVector(sectionName, $"{requestName}-orientation", request.Orientation);
        ini.SetVector(sectionName, $"{requestName}-position", request.Position);
        ini.Set(sectionName, $"{requestName}-size", request.Size.ToString());
      }

      void deserialize(MyIni ini) {
        string sectionName = $"{INI_SECTION}{this.connector.Name}";
        if (ini.ContainsSection(sectionName)) {
          this.CurrentRequest = this.deserializeRequest(ini, sectionName, "current");
          if (ini.ContainsKey(sectionName, "previous")) {
            this.PreviousRequest = this.deserializeRequest(ini, sectionName, "previous");
          }
        }
        if (this.connector.IsConnected()) {
          this.mainProcess.Spawn(this.checkManualDisconnection, "check-manual-disc", period: 10);
        } else if (this.CurrentRequest != null) {
          this.startConnectionCallback(this.CurrentRequest.Value);
        }
      }

      ConnectionRequest deserializeRequest(MyIni ini, string sectionName, string requestName) {
        return new ConnectionRequest {
          Address = ini.GetThrow(sectionName, $"{requestName}-address").ToInt64(),
          Orientation = ini.GetVector(sectionName, $"{requestName}-orientation"),
          Position = ini.GetVector(sectionName, $"{requestName}-position"),
          Size = (MyCubeSize)Enum.Parse(typeof(MyCubeSize), ini.GetThrow(sectionName, $"{requestName}-size").ToString())
        };
      }
    }
  }
}
