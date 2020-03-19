using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
  partial class Program {
    public class AutoConnectionDispatcher {
      private static readonly string INI_GENERAL_SECTION = "sac-general";
      private static readonly string INI_NAME_KEY = "station-name";
      private static readonly string INI_REFERENCE_KEY = "reference-name";

      private readonly List<AutoConnectionServer> _autoConnectors = new List<AutoConnectionServer>();
      private readonly IMyIntergridCommunicationSystem _igc;
      private readonly CoordsTransformer _transformer;
      private readonly string _referenceName;
      private readonly string _stationName;
      private readonly ScheduledAction _updateAction;

      public AutoConnectionDispatcher(MyGridProgram program, CmdLine command, MyIni ini) {
        // Station level initialization
        this._stationName = ini.GetThrow(INI_GENERAL_SECTION, INI_NAME_KEY).ToString();
        this._referenceName = ini.GetThrow(INI_GENERAL_SECTION, INI_REFERENCE_KEY).ToString();
        var reference = program.GridTerminalSystem.GetBlockWithName(this._referenceName);
        if (reference == null) {
          throw new ArgumentException($"Could not find reference block '{this._referenceName}'");
        }
        this._igc = program.IGC;
        this._transformer = new CoordsTransformer(reference, false);
        this._log($"initializing");
        // Connectors initialization
        var sections = new List<string>();
        ini.GetSections(sections);
        foreach (string sectionName in sections.Where(s => s.StartsWith(AutoConnector.IniConnectorPrefix))) {
          var connector = new AutoConnector(this._stationName, sectionName, program, this._transformer.Pos, this._transformer.Dir, ini);
          this._autoConnectors.Add(new AutoConnectionServer(ini, this._igc, connector));
        }
        this._log($"has {this._autoConnectors.Count} auto connectors");
        this._registerCommands(command, program);
        this._updateAction = new ScheduledAction(_update);
        Schedule(this._updateAction);

        var listener = this._igc.RegisterBroadcastListener("StationConnectionRequests");
        Schedule(() => {
          if(listener.HasPendingMessage) {
            var msg = listener.AcceptMessage();
            command.HandleCmd($"{msg.As<string>()} {msg.Source}", false);
          }
        });

        ScheduleOnSave(_save);
      }

      private void _save(MyIni ini) {
        ini.Set(INI_GENERAL_SECTION, INI_NAME_KEY, this._stationName);
        ini.Set(INI_GENERAL_SECTION, INI_REFERENCE_KEY, this._referenceName);
        ini.SetSectionComment(INI_GENERAL_SECTION, "Automatically generated, do not modify anything beside this section");
        foreach (var autoConnector in this._autoConnectors) {
          autoConnector.Save(ini);
        }
      }

      public void AddNewConnector(string connectorName, MyGridProgram program) {
        foreach(var con in this._autoConnectors) {
          if (con.Name == connectorName) {
            this._log($"connector {connectorName} already exists");
            return;
          }
        }
        try {

          var connector = new AutoConnector(this._stationName, connectorName, program, this._transformer.Pos, this._transformer.Dir);
          this._autoConnectors.Add(new AutoConnectionServer(this._igc, connector));
        } catch (InvalidOperationException e) {
          this._log($"could not create connector '{connectorName}': {e.Message}");
        }
      }

      private void _connect(MyCubeSize size, string channel, Vector3D wPos, Vector3D wOrientation, long address) {
        Vector3D pos = this._transformer.Pos(wPos);
        Vector3D orientation = this._transformer.Dir(wOrientation);
        var connector = this._autoConnectors.FirstOrDefault(con => con.IsInRange(pos));
        if (connector != null) {
          this._log($"found eligible connector for connection: '{connector.Name}'");
          connector.Connect(new ConnectionRequest {
            Address = address,
            Channel = channel,
            Orientation = orientation,
            Position = pos,
            Size = size
          });
        } else {
          this._log($"found no eligible connector");
        }
      }

      private void _disconnect(string channel, long address) {
        var connector = this._autoConnectors.FirstOrDefault(con => con.HasPendingRequest(address));
        if (connector != null) {
          this._log($"found eligible connector for disconnection: '{connector.Name}'");
          connector.Disconnect(address);
        } else {
          this._log($"found no eligible connector");
        }
      }

      private void _resetConnector(string name) {
        var connector = this._autoConnectors.FirstOrDefault(c => c.Name == name);
        if (connector != null) {
          connector.Reset();
        }
      }

      private void _removeConnector(string name) {
        int count = this._autoConnectors.Count;
        this._autoConnectors.RemoveAll(c => c.Name == name);
        count -= this._autoConnectors.Count;
        if (count == 0) {
          this._log($"could not find connector '{name}'");
        } else {
          this._log($"removed {count} connectors");
        }
      }

      private void _registerCommands(CmdLine command, MyGridProgram program) {
        command.AddCmd(new Cmd("ac-add", "Adds and initializes an auto connector", ss => AddNewConnector(ss[0], program),
          detailedHelp: @"Argument is the name of the auto connector.
Pistons need to be named:
  '<base name in CustomData> Piston <Auto connector name> .*'", minArgs: 1, maxArgs: 1));

        command.AddCmd(new Cmd("ac-del", "Removes an auto connector", ss => _removeConnector(ss[0]),
          detailedHelp: @"Argument is the name of the auto connector", minArgs: 1, maxArgs: 1));

        command.AddCmd(new Cmd("ac-con", "Requests a connection", ss => _connect(ss),
          detailedHelp: @"Requests for an automatic connection to the base
An auto connector will chosen based on proximity
First arg is size (small or large).
Next six are the world position and orientation of the requesting connector.
Last is the address of the requestor", minArgs: 9, maxArgs: 9, requirePermission: false));

        command.AddCmd(new Cmd("ac-disc", "Requests a disconnection", ss => _disconnect(ss),
          detailedHelp: @"Requests for an automatic disconnection from the base
Will disconnect or cancel a request based on the requestor", minArgs: 2, maxArgs: 2, requirePermission: false));

        command.AddCmd(new Cmd("ac-reset", "Resets a connector to its starting positions", ss => _resetConnector(ss[0]),
          detailedHelp: @"Resets the connector with the given name", minArgs: 1, maxArgs: 1, requirePermission: true));
      }

      private void _connect(List<string> args) {
        this._log($"received a connection request");
        try {
          var size = MyCubeSize.Small;
          Enum.TryParse(args[0], out size);
          var wPos = new Vector3D(double.Parse(args[2]), double.Parse(args[3]), double.Parse(args[4]));
          var wOrientation = new Vector3D(double.Parse(args[5]), double.Parse(args[6]), double.Parse(args[7]));
          long address = long.Parse(args[8]);
          this._connect(size, args[1], wPos, wOrientation, address);
        } catch (Exception e) {
          this._log($"could not parse value received: {e.Message}");
        }
      }

      private void _disconnect(List<string> args) {
        this._log($"received a disconnection request");
        try {
          this._disconnect(args[0], long.Parse(args[1]));
        } catch (Exception e) {
          this._log($"could not parse value received: {e.Message}");
        }
      }

      private void _update() {
        bool hasUpdated = false;
        foreach (var connector in this._autoConnectors) {
          hasUpdated |= connector.Update();
        }
        this._updateAction.Period = hasUpdated ? 1 : 10;
      }

      private void _log(string log) => Log($"Dispatcher '{this._stationName}': {log}");
    }
  }
}
