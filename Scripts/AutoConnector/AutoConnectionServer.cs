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

namespace IngameScript
{
  partial class Program
  {
    /// <summary>This class handles the requests made to an <see cref="AutoConnector"/>, sending updates to requestor, handling reconnection, automatic disconnections</summary>
    public struct ConnectionRequest
    {
      public long Address;
      public MyCubeSize Size;
      public Vector3D Position;
      public Vector3D Orientation;
    }

    public class AutoConnectionServer
    {
      static readonly double DISTANCE_CUTOFF = 2;
      static readonly string INI_SECTION = "connection-server-";
      static readonly Log LOG = Log.GetLog("ACS");

      public string Name => _connector.Name;
      public ConnectionRequest? CurrentRequest { get; private set; }
      public ConnectionRequest? PreviousRequest { get; private set; }

      readonly AutoConnector _connector;
      readonly IMyIntergridCommunicationSystem _igc;
      readonly Process _mainProcess;

      public AutoConnectionServer(MyIni ini, IMyIntergridCommunicationSystem igc, AutoConnector connector, IProcessSpawner spawner)
          : this(igc, connector, spawner)
      {
        _deserialize(ini);
      }

      public AutoConnectionServer(IMyIntergridCommunicationSystem igc, AutoConnector connector, IProcessSpawner spawner)
      {
        _connector = connector;
        _igc = igc;
        _mainProcess = spawner.Spawn(p => Update(), $"ac-server '{Name}'", onDone => _connector.Stop());
        LOG.Info($"Connector {Name} ready");
      }

      public bool IsInRange(Vector3D position) => _connector.GetDistance(position) < DISTANCE_CUTOFF;

      public bool HasPendingRequest(long address) => address == CurrentRequest?.Address || address == PreviousRequest?.Address;

      public void Connect(ConnectionRequest request)
      {
        if (CurrentRequest != null && CurrentRequest?.Address == request.Address)
        {
          CurrentRequest = request;
        }
        else
        {
          if (PreviousRequest != null && PreviousRequest.Value.Address != request.Address)
          {
            _sendCancel(PreviousRequest.Value);
          }
          PreviousRequest = CurrentRequest;
          CurrentRequest = request;
        }
        if (PreviousRequest != null)
        {
          _sendCancel(PreviousRequest.Value);
        }
        _connector.Connect(CurrentRequest.Value.Size, CurrentRequest.Value.Position, CurrentRequest.Value.Orientation);
        _startConnectionCallback(CurrentRequest.Value);
      }

      public void Disconnect(long address)
      {
        if (PreviousRequest != null && (PreviousRequest?.Address == address))
        {
          PreviousRequest = null;
        }
        else if (CurrentRequest != null && (CurrentRequest?.Address == address))
        {
          ConnectionRequest requestToNotify = CurrentRequest.Value;
          CurrentRequest = PreviousRequest;
          PreviousRequest = null;
          if (CurrentRequest != null)
          {
            Connect(CurrentRequest.Value);
          }
          else
          {
            _connector.Disconnect();
            _mainProcess.KillChildren();
          }
          double totalLength = _connector.GetRemainingLength();
          _mainProcess.Spawn(p => _checkDisconnectionProgress(p, requestToNotify, totalLength), "disconnection-progress", p => _sendMessage(requestToNotify, "cc-done"), 10);
        }
      }

      public void Reset()
      {
        _mainProcess.KillChildren();
        if (CurrentRequest != null)
        {
          _sendCancel(CurrentRequest.Value);
        }
        if (PreviousRequest != null)
        {
          _sendCancel(PreviousRequest.Value);
        }
        CurrentRequest = null;
        PreviousRequest = null;
        _connector.Disconnect();
      }

      public void Save(MyIni ini)
      {
        if (CurrentRequest != null)
        {
          string sectionName = $"{INI_SECTION}{_connector.Name}";
          _saveRequest(ini, sectionName, "current", CurrentRequest.Value);
          if (PreviousRequest != null)
          {
            _saveRequest(ini, sectionName, "previous", PreviousRequest.Value);
          }
        }
        _connector.Save(ini);
      }
      public bool Update() => _connector.Update();

      public void Kill() => _mainProcess.Kill();

      void _startConnectionCallback(ConnectionRequest request)
      {
        _mainProcess.KillChildren();
        double length = _connector.GetRemainingLength();
        _mainProcess.Spawn(p => _checkConnectionProgress(p, request, length), "connection-progress", period: 10);
      }

      void _checkConnectionProgress(Process p, ConnectionRequest request, double totalLength)
      {
        float progress = (float)((totalLength - _connector.GetRemainingLength()) / totalLength);
        if (_connector.IsMoving())
        {
          _sendMessage(request, $"cc-progress {MathHelper.Clamp(progress, 0, 1)}");
        }
        else
        {
          _mainProcess.KillChildren();
          if (_connector.IsConnected())
          {
            _sendMessage(request, "cc-done");
            _mainProcess.Spawn(_checkManualDisconnection, "check-manual-disc", period: 10);
          }
          else
          {
            _sendMessage(request, "cc-ko");
            Disconnect(request.Address);
          }
        }
      }

      void _checkManualDisconnection(Process p)
      {
        if (!_connector.IsConnected())
        {
          _mainProcess.KillChildren();
          if (CurrentRequest != null)
          {
            Disconnect(CurrentRequest.Value.Address);
          }
          else
          {
            Reset();
          }
        }
      }

      void _checkDisconnectionProgress(Process p, ConnectionRequest request, double totalLength)
      {
        float progress = (float)((totalLength - _connector.GetRemainingLength()) / totalLength) * 3;
        if (progress > 1)
        {
          p.Kill();
        }
        else
        {
          _sendMessage(request, $"cc-progress {MathHelper.Clamp(progress, 0, 1)}");
        }
      }

      void _sendCancel(ConnectionRequest request) => _sendMessage(request, "cc-cancel");

      void _sendMessage(ConnectionRequest request, string message) => _igc.SendUnicastMessage(request.Address, "", message);

      void _saveRequest(MyIni ini, string sectionName, string requestName, ConnectionRequest request)
      {
        ini.Set(sectionName, $"{requestName}-address", request.Address);
        ini.SetVector(sectionName, $"{requestName}-orientation", request.Orientation);
        ini.SetVector(sectionName, $"{requestName}-position", request.Position);
        ini.Set(sectionName, $"{requestName}-size", request.Size.ToString());
      }

      void _deserialize(MyIni ini)
      {
        string sectionName = $"{INI_SECTION}{_connector.Name}";
        if (ini.ContainsSection(sectionName))
        {
          CurrentRequest = _deserializeRequest(ini, sectionName, "current");
          if (ini.ContainsKey(sectionName, "previous"))
          {
            PreviousRequest = _deserializeRequest(ini, sectionName, "previous");
          }
        }
        if (_connector.IsConnected())
        {
          _mainProcess.Spawn(_checkManualDisconnection, "check-manual-disc", period: 10);
        }
        else if (CurrentRequest != null)
        {
          _startConnectionCallback(CurrentRequest.Value);
        }
      }

      ConnectionRequest _deserializeRequest(MyIni ini, string sectionName, string requestName)
      {
        return new ConnectionRequest
        {
          Address = ini.GetThrow(sectionName, $"{requestName}-address").ToInt64(),
          Orientation = ini.GetVector(sectionName, $"{requestName}-orientation"),
          Position = ini.GetVector(sectionName, $"{requestName}-position"),
          Size = (MyCubeSize)Enum.Parse(typeof(MyCubeSize), ini.GetThrow(sectionName, $"{requestName}-size").ToString())
        };
      }
    }
  }
}
