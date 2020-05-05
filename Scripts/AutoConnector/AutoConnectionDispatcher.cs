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
    /// <summary>Holds the <see cref="AutoConnectionServer"/>s and dispatches the auto connection requests to the autoconnector in range</summary>
    public class AutoConnectionDispatcher {
      static readonly string INI_GENERAL_SECTION = "sac-general";
      static readonly string INI_NAME_KEY = "station-name";
      static readonly string INI_REFERENCE_KEY = "reference-name";

      readonly List<AutoConnectionServer> autoConnectors = new List<AutoConnectionServer>();
      readonly IMyIntergridCommunicationSystem igc;
      readonly Action<string> logger;
      readonly IProcessManager manager;
      readonly CoordinatesTransformer transformer;
      readonly string referenceName;
      readonly string stationName;

      public AutoConnectionDispatcher(MyGridProgram program, CommandLine command, MyIni ini, Action<string> logger, IProcessManager manager) {
        this.logger = logger;
        this.manager = manager;
        // Station level initialization
        this.stationName = ini.GetThrow(INI_GENERAL_SECTION, INI_NAME_KEY).ToString();
        this.referenceName = ini.GetThrow(INI_GENERAL_SECTION, INI_REFERENCE_KEY).ToString();
        IMyTerminalBlock reference = program.GridTerminalSystem.GetBlockWithName(this.referenceName);
        if (reference == null) {
          throw new ArgumentException($"Could not find reference block '{this.referenceName}'");
        }
        this.igc = program.IGC;
        this.transformer = new CoordinatesTransformer(reference, manager);
        this.log("initializing");
        // Connectors initialization
        var sections = new List<string>();
        ini.GetSections(sections);
        foreach (string sectionName in sections.Where(s => s.StartsWith(AutoConnector.IniConnectorPrefix))) {
          var connector = new AutoConnector(this.stationName, sectionName, program, this.logger, this.transformer, ini);
          this.autoConnectors.Add(new AutoConnectionServer(ini, this.igc, connector, manager, this.logger));
        }
        this.log($"has {this.autoConnectors.Count} auto connectors");
        this.registerCommands(command, program);

        IMyBroadcastListener listener = this.igc.RegisterBroadcastListener("StationConnectionRequests");
        this.manager.Spawn(p => {
          if (listener.HasPendingMessage) {
            MyIGCMessage msg = listener.AcceptMessage();
            command.StartCmd($"{msg.As<string>()} {msg.Source}", CommandTrigger.Antenna);
          }
        }, "ac-dispatcher");

        this.manager.AddOnSave(save);
      }

      void save(MyIni ini) {
        ini.Set(INI_GENERAL_SECTION, INI_NAME_KEY, this.stationName);
        ini.Set(INI_GENERAL_SECTION, INI_REFERENCE_KEY, this.referenceName);
        ini.SetSectionComment(INI_GENERAL_SECTION, "Automatically generated, do not modify anything beside this section");
        foreach (AutoConnectionServer autoConnector in this.autoConnectors) {
          autoConnector.Save(ini);
        }
      }

      public void AddNewConnector(string connectorName, MyGridProgram program) {
        foreach(AutoConnectionServer con in this.autoConnectors) {
          if (con.Name == connectorName) {
            this.log($"connector {connectorName} already exists");
            return;
          }
        }
        try {
          var connector = new AutoConnector(this.stationName, connectorName, program, this.logger, this.transformer);
          this.autoConnectors.Add(new AutoConnectionServer(this.igc, connector, this.manager));
        } catch (InvalidOperationException e) {
          this.log($"could not create connector '{connectorName}': {e.Message}");
        }
      }

      void connect(MyCubeSize size, Vector3D wPos, Vector3D wOrientation, long address) {
        Vector3D pos = this.transformer.Pos(wPos);
        Vector3D orientation = this.transformer.Dir(wOrientation);
        AutoConnectionServer connector = this.autoConnectors.FirstOrDefault(con => con.IsInRange(pos));
        if (connector != null) {
          this.log($"found eligible connector for connection: '{connector.Name}'");
          connector.Connect(new ConnectionRequest {
            Address = address,
            Orientation = orientation,
            Position = pos,
            Size = size
          });
        } else {
          this.log("found no eligible connector");
        }
      }

      void disconnect(long address) {
        AutoConnectionServer connector = this.autoConnectors.FirstOrDefault(con => con.HasPendingRequest(address));
        if (connector != null) {
          this.log($"found eligible connector for disconnection: '{connector.Name}'");
          connector.Disconnect(address);
        } else {
          this.log("found no eligible connector");
        }
      }

      void resetConnector(string name) {
        AutoConnectionServer connector = this.autoConnectors.FirstOrDefault(c => c.Name == name);
        if (connector != null) {
          connector.Reset();
        }
      }

      void removeConnector(string name) {
        int count = 0;
        foreach(AutoConnectionServer server in this.autoConnectors.Where(c => c.Name == name)) {
          server.Kill();
          ++count;
        }
        if (count == 0) {
          this.log($"could not find connector '{name}'");
        } else {
          this.autoConnectors.RemoveAll(c => c.Name == name);
          this.log($"removed {count} connectors");
        }
      }

      void registerCommands(CommandLine command, MyGridProgram program) {
        command.RegisterCommand(new Command("ac-add", Command.Wrap(s => this.AddNewConnector(s, program)), "Adds and initializes an auto connector",
          detailedHelp: @"Argument is the name of the auto connector.
Pistons need to be named:
  '<base name in CustomData> Piston <Auto connector name> .*'", nArgs: 1));

        command.RegisterCommand(new Command("ac-del", Command.Wrap(this.removeConnector), "Removes an auto connector",
          detailedHelp: @"Argument is the name of the auto connector", nArgs: 1));
        
        command.RegisterCommand(new Command("ac-con", Command.Wrap(this.connect), "Requests a connection",
          detailedHelp: @"Requests for an automatic connection to the base
An auto connector will chosen based on proximity
First arg is size (small or large).
Next six are the world position and orientation of the requesting connector.
Last is the address of the requestor", nArgs: 8, requiredTrigger: CommandTrigger.Antenna));

        command.RegisterCommand(new Command("ac-disc", Command.Wrap(this.disconnect), "Requests a disconnection",
          detailedHelp: @"Requests for an automatic disconnection from the base
Will disconnect or cancel a request based on the requestor", nArgs: 1, requiredTrigger: CommandTrigger.Antenna));

        command.RegisterCommand(new Command("ac-reset", Command.Wrap(this.resetConnector), "Resets a connector to its starting positions",
          detailedHelp: @"Resets the connector with the given name", nArgs: 1));
      }

      void connect(List<string> args) {
        this.log("received a connection request");
        try {
          MyCubeSize size = MyCubeSize.Small;
          Enum.TryParse(args[0], out size);
          var wPos = new Vector3D(double.Parse(args[1]), double.Parse(args[2]), double.Parse(args[3]));
          var wOrientation = new Vector3D(double.Parse(args[4]), double.Parse(args[5]), double.Parse(args[6]));
          long address = long.Parse(args[7]);
          this.connect(size, wPos, wOrientation, address);
        } catch (Exception e) {
          this.log($"could not parse value received: {e.Message}");
        }
      }

      void disconnect(string arg) {
        this.log("received a disconnection request");
        try {
          this.disconnect(long.Parse(arg));
        } catch (Exception e) {
          this.log($"could not parse value received: {e.Message}");
        }
      }

      void log(string log) => this.logger?.Invoke($"Dispatcher '{this.stationName}': {log}");
    }
  }
}
