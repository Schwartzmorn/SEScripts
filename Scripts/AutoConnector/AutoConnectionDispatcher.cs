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

namespace IngameScript
{
  partial class Program
  {
    /// <summary>Holds the <see cref="AutoConnectionServer"/>s and dispatches the auto connection requests to the autoconnector in range</summary>
    public class AutoConnectionDispatcher
    {
      static readonly string INI_GENERAL_SECTION = "sac-general";
      static readonly string INI_NAME_KEY = "station-name";
      static readonly string INI_REFERENCE_KEY = "reference-name";

      readonly List<AutoConnectionServer> _autoConnectors = new List<AutoConnectionServer>();
      readonly IMyIntergridCommunicationSystem _igc;
      readonly Action<string> _logger;
      readonly IProcessManager _manager;
      readonly CoordinatesTransformer _transformer;
      readonly string _referenceName;
      readonly string _stationName;

      public AutoConnectionDispatcher(MyGridProgram program, CommandLine command, MyIni ini, Action<string> logger, IProcessManager manager)
      {
        _logger = logger;
        _manager = manager;
        // Station level initialization
        _stationName = ini.GetThrow(INI_GENERAL_SECTION, INI_NAME_KEY).ToString();
        _referenceName = ini.GetThrow(INI_GENERAL_SECTION, INI_REFERENCE_KEY).ToString();
        IMyTerminalBlock reference = program.GridTerminalSystem.GetBlockWithName(_referenceName);
        if (reference == null)
        {
          throw new ArgumentException($"Could not find reference block '{_referenceName}'");
        }
        _igc = program.IGC;
        _transformer = new CoordinatesTransformer(reference, manager);
        _log("initializing");
        // Connectors initialization
        var sections = new List<string>();
        ini.GetSections(sections);
        foreach (string sectionName in sections.Where(s => s.StartsWith(AutoConnector.INI_CONNECTOR_PREFIX)))
        {
          var connector = new AutoConnector(_stationName, sectionName, program, _logger, _transformer, ini);
          _autoConnectors.Add(new AutoConnectionServer(ini, _igc, connector, manager, _logger));
        }
        _log($"has {_autoConnectors.Count} auto connectors");
        _registerCommands(command, program);

        IMyBroadcastListener listener = _igc.RegisterBroadcastListener("StationConnectionRequests");
        _manager.Spawn(p =>
        {
          if (listener.HasPendingMessage)
          {
            MyIGCMessage msg = listener.AcceptMessage();
            command.StartCmd($"{msg.As<string>()} {msg.Source}", CommandTrigger.Antenna);
          }
        }, "ac-dispatcher");

        _manager.AddOnSave(_save);
      }

      void _save(MyIni ini)
      {
        ini.Set(INI_GENERAL_SECTION, INI_NAME_KEY, _stationName);
        ini.Set(INI_GENERAL_SECTION, INI_REFERENCE_KEY, _referenceName);
        ini.SetSectionComment(INI_GENERAL_SECTION, "Automatically generated, do not modify anything beside this section");
        foreach (AutoConnectionServer autoConnector in _autoConnectors)
        {
          autoConnector.Save(ini);
        }
      }

      public void AddNewConnector(string connectorName, MyGridProgram program)
      {
        foreach (AutoConnectionServer con in _autoConnectors)
        {
          if (con.Name == connectorName)
          {
            _log($"connector {connectorName} already exists");
            return;
          }
        }
        try
        {
          var connector = new AutoConnector(_stationName, connectorName, program, _logger, _transformer);
          _autoConnectors.Add(new AutoConnectionServer(_igc, connector, _manager));
        }
        catch (InvalidOperationException e)
        {
          _log($"could not create connector '{connectorName}': {e.Message}");
        }
      }

      void _connect(MyCubeSize size, Vector3D wPos, Vector3D wOrientation, long address)
      {
        Vector3D pos = _transformer.Pos(wPos);
        Vector3D orientation = _transformer.Dir(wOrientation);
        AutoConnectionServer connector = _autoConnectors.FirstOrDefault(con => con.IsInRange(pos));
        if (connector != null)
        {
          _log($"found eligible connector for connection: '{connector.Name}'");
          connector.Connect(new ConnectionRequest
          {
            Address = address,
            Orientation = orientation,
            Position = pos,
            Size = size
          });
        }
        else
        {
          _log("found no eligible connector");
        }
      }

      void _disconnect(long address)
      {
        AutoConnectionServer connector = _autoConnectors.FirstOrDefault(con => con.HasPendingRequest(address));
        if (connector != null)
        {
          _log($"found eligible connector for disconnection: '{connector.Name}'");
          connector.Disconnect(address);
        }
        else
        {
          _log("found no eligible connector");
        }
      }

      void _resetConnector(string name)
      {
        AutoConnectionServer connector = _autoConnectors.FirstOrDefault(c => c.Name == name);
        if (connector != null)
        {
          connector.Reset();
        }
      }

      void _removeConnector(string name)
      {
        int count = 0;
        foreach (AutoConnectionServer server in _autoConnectors.Where(c => c.Name == name))
        {
          server.Kill();
          ++count;
        }
        if (count == 0)
        {
          _log($"could not find connector '{name}'");
        }
        else
        {
          _autoConnectors.RemoveAll(c => c.Name == name);
          _log($"removed {count} connectors");
        }
      }

      void _registerCommands(CommandLine command, MyGridProgram program)
      {
        var cmd = new ParentCommand("acd", "Auto connection dispatcher commands")
          .AddSubCommand(new Command("add", Command.Wrap(s => AddNewConnector(s, program)), @"Adds and initializes an auto connector
Argument is the name of the auto connector.
Pistons need to be named:
  '<base name in CustomData> Piston <Auto connector name> .*'", nArgs: 1))
          .AddSubCommand(new Command("delete", Command.Wrap(_removeConnector), @"Removes an auto connector
Argument is the name of the auto connector", nArgs: 1))
          .AddSubCommand(new Command("connect", Command.Wrap(_connect), @"Requests for an automatic connection to the base
An auto connector will chosen based on proximity
First arg is size (small or large).
Next six are the world position and orientation of the requesting connector.
Last is the address of the requestor", nArgs: 8, requiredTrigger: CommandTrigger.Antenna))
          .AddSubCommand(new Command("disconnect", Command.Wrap(_disconnect), @"Requests for an automatic disconnection from the base.
Will disconnect or cancel a request based on the requestor", nArgs: 1, requiredTrigger: CommandTrigger.Antenna))
          .AddSubCommand(new Command("reset", Command.Wrap(_resetConnector), @"Resets a connector to its starting positions.
Argument is the name of the connector.", nArgs: 1));
        command.RegisterCommand(cmd);
      }

      void _connect(ArgumentsWrapper args)
      {
        _log("received a connection request");
        try
        {
          MyCubeSize size = MyCubeSize.Small;
          Enum.TryParse(args[0], out size);
          var wPos = new Vector3D(double.Parse(args[1]), double.Parse(args[2]), double.Parse(args[3]));
          var wOrientation = new Vector3D(double.Parse(args[4]), double.Parse(args[5]), double.Parse(args[6]));
          long address = long.Parse(args[7]);
          _connect(size, wPos, wOrientation, address);
        }
        catch (Exception e)
        {
          _log($"could not parse value received: {e.Message}");
        }
      }

      void _disconnect(string arg)
      {
        _log("received a disconnection request");
        try
        {
          _disconnect(long.Parse(arg));
        }
        catch (Exception e)
        {
          _log($"could not parse value received: {e.Message}");
        }
      }

      void _log(string log) => _logger?.Invoke($"Dispatcher '{_stationName}': {log}");
    }
  }
}
