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

    public class ConnectionRequestor {
      public static readonly string INI_SECTION = "connection-request";
      public static readonly string INI_KEY = "is-connection";
      public static readonly string INI_DONE_KEY = "is-done";
      public static readonly string NO_CHANNEL = "$IGNORE";
      private readonly List<IMyShipConnector> _connectors = new List<IMyShipConnector>();
      private ConnectionRequest _currentRequest = null;
      private IConnectionEventListener _eventListener;
      private readonly string _requestChannel;
      private readonly string _responseChannel;

      public ConnectionRequestor(MyGridProgram program, CommandLine command, string requestChannel,
          IConnectionEventListener eventListener = null, MyIni ini = null, string responseChannel = null) {
        _requestChannel = requestChannel;
        _responseChannel = responseChannel ?? NO_CHANNEL;
        _eventListener = eventListener;
        if (Scheduler.Instance != null) {
          Scheduler.Instance.AddActionOnSave(_save);
        } else if (eventListener != null || ini != null) {
          throw new ArgumentException("A scheduler must be set to respond to events");
        }
        program.GridTerminalSystem.GetBlocksOfType(_connectors, c => c.CubeGrid == program.Me.CubeGrid);

        command.AddCommand(new Command(
          "auto-connect", "Requests for an auto connection",
          ss => _connect(program.IGC),
          detailedHelp: $@"Picks a random connector on the grid and requests for its connection
It will broadcast on the channel '{requestChannel}'",
          maxArgs: 0
          ));
        command.AddCommand(new Command(
          "auto-disconnect", "Requests for disconnection",
          ss => _disconnect(program.IGC),
          detailedHelp: $@"Disconnects a connected connector and notifies of the disconnection
It will broadcast on the channel '{requestChannel}'",
          maxArgs: 0
          ));
        command.AddCommand(new Command(
          "auto-switch", "Requests for connection/disconnection",
          ss => _switchConnection(program.IGC),
          detailedHelp: $@"Depending on whether it is connected or not:
It will request a disconnection or a connection
It will broadcast on the channel '{requestChannel}'",
          maxArgs: 0
          ));

        if (ini != null && _eventListener != null && ini.ContainsKey(INI_SECTION, INI_KEY)) {
          _currentRequest = new ConnectionRequest(this, _eventListener, program.IGC,
              _responseChannel, ini.Get(INI_SECTION, INI_KEY).ToBoolean(), ini.Get(INI_SECTION, INI_DONE_KEY).ToBoolean());
        }
      }

      private void _save(MyIni ini) {
        if (_currentRequest != null) {
          ini.Set(INI_SECTION, INI_KEY, _currentRequest.IsConnection);
          ini.Set(INI_SECTION, INI_DONE_KEY, _currentRequest.IsDone);
        }
      }

      private void _cancelCurrentRequest() {
        if (_currentRequest != null) {
          _currentRequest.Cancel();
        }
      }

      private void _connect(IMyIntergridCommunicationSystem igc) {
        var con = _connectors.Find(c => c.IsWorking);
        if (con != null) {
          _cancelCurrentRequest();
          var com = new CommandSerializer("ac-con");
          com.AddArgument(con.CubeGrid.GridSizeEnum);
          com.AddArgument(_responseChannel);
          SerializeVector(con.GetPosition(), com);
          SerializeVector(con.WorldMatrix.Forward, com);
          igc.SendBroadcastMessage(_requestChannel, com.ToString());
          if (_eventListener != null) {
            _eventListener.OnStart(true);
            _currentRequest = new ConnectionRequest(this, _eventListener, igc, _responseChannel, true);
          }
        }
      }

      private void _disconnect(IMyIntergridCommunicationSystem igc) {
        var con = _connectors.Find(c => c.Status == MyShipConnectorStatus.Connected);
        if (con != null) {
          _cancelCurrentRequest();
          var com = new CommandSerializer("ac-disc");
          com.AddArgument(_responseChannel);
          SerializeVector(con.GetPosition(), com);
          igc.SendBroadcastMessage(_requestChannel, com.ToString());
          if(_eventListener != null) {
            _eventListener.OnStart(false);
            _currentRequest = new ConnectionRequest(this, _eventListener, igc, _responseChannel, false);
          }
        }
      }

      private void _switchConnection(IMyIntergridCommunicationSystem igc) {
        var con = _connectors.Find(c => c.Status == MyShipConnectorStatus.Connected);
        if (con != null) {
          _disconnect(igc);
        } else {
          _connect(igc);
        }
      }

      private static void SerializeVector(Vector3D v, CommandSerializer com) {
        com.AddArgument(v.X);
        com.AddArgument(v.Y);
        com.AddArgument(v.Z);
      }

      public void DisposeRequest() => _currentRequest = null;
    }
  }
}
