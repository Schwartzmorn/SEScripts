﻿using Sandbox.Game.EntityComponents;
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
    /// <summary>Enum to categorize what triggered a given command, for some limited access control.</summary>
    public enum CommandTrigger { Antenna = 0, User = 1, Cmd = 2 };

    /// <summary>
    /// Class that parses and dispatches textual commands.
    /// <para>To summon a <see cref="Command"/>, the method <see cref="StartCmd(string, CommandTrigger, Action{ProcessResult})"/> must be called with the string $"-{<see cref="Command.Name"/>}" followed by its space separated arguments</para>
    /// </summary>
    public class CommandLine {
      private readonly Dictionary<string, Command> commands = new Dictionary<string, Command>();
      private readonly Action<string> logger;
      private readonly string name;
      private readonly MyCommandLine parser = new MyCommandLine();
      private readonly IProcessSpawner spawner;
      /// <summary>Create a <see cref="CommandLine"/> able to parse and dispatch commands to its registered <see cref="Command"/>.</summary>
      /// <param name="name">Name of the command line.</param>
      /// <param name="logger">Optional logger to log some events.</param>
      /// <param name="spawner">Default spawner used for the processes.</param>
      public CommandLine(string name, Action<string> logger, IProcessSpawner spawner) {
        this.name = name;
        this.logger = logger;
        this.spawner = spawner;

        this.RegisterCommand(new Command("help", this.help, "Displays this help or help on a command", detailedHelp: @"If no argument, gives the list of available command
Else, gives the detailed help on the command", maxArgs: 1));
        this.log($"'{this.name}' initialized. Run '-help' for more info");
        if ((spawner as IProcessManager) != null) {
          this.RegisterCommand(new Command("kill", this.kill, "Kills processes by pid or by name", nArgs: 1, detailedHelp: @"If a number is provided, it will kill the process with the given process id.
Otherwise, it kills all the process with the given name."));
          this.RegisterCommand(new Command("ps", this.ps, "Lists alive processes", nArgs: 0));
        }
      }
      /// <summary>Registers a <see cref="Command"/> so the <see cref="CommandLine"/> recognizes it.</summary>
      /// <param name="command"><see cref="Command"/> to register</param>
      public void RegisterCommand(Command command) => this.commands.Add(command.Name, command);
      /// <summary> Parses the command and spawns the corresponding process.</summary>
      /// <param name="cmd">Command with the command name and its arguments</param>
      /// <param name="trigger">What triggered the command, for some limited access control</param>
      /// <param name="onDone">Callback the process will call on termination</param>
      /// <param name="spawner">The spawner (typically a <see cref="Process"/>) used to spawn the process. If null, <see cref="CommandLine.spawner"/> will be used.</param>
      /// <returns>The spawned <see cref="Process"/>. Can be null if it failed or was executed immediately.</returns>
      public Process StartCmd(string cmd, CommandTrigger trigger, Action<Process> onDone = null, IProcessSpawner spawner = null) {
        if (string.IsNullOrWhiteSpace(cmd) || !this.parser.TryParse(cmd) || this.parser.Switches.Count != 1) {
          this.log($"Failed to parse {cmd}");
        } else {
          string cmdName = this.parser.Switches.ElementAt(0);
          Command command;
          if (this.commands.TryGetValue(cmdName, out command)) {
            var args = new List<string>();
            int i = -1;
            string a;
            while ((a = this.parser.Switch(cmdName, ++i)) != null) {
              args.Add(a);
            }
            return command.Spawn(args, this.log, onDone, spawner ?? this.spawner, trigger);
          } else {
            this.log($"Unknown command {cmdName}");
          }
        }
        this.log("Run -help for more info.");
        return null;
      }

      private VRage.MyTuple<int, bool, Action<Process>> help(List<string> args, Action<string> log) {
        return VRage.MyTuple.Create<int, bool, Action<Process>>(1, true, p => {
          if (args.Count > 0) {
            Command command;
            if (this.commands.TryGetValue(args[0], out command)) {
              command.DetailedHelp(log);
            } else {
              log($"Unknown command: {args[0]}");
            }
          } else {
            log($"Commands available on {this.name}:");
            foreach (var kv in this.commands) {
              log($"-{kv.Key}: {kv.Value.BriefHelp}");
            }
          }
        });
      }

      private VRage.MyTuple<int, bool, Action<Process>> kill(List<string> args, Action<string> log) {
        int pid;
        return int.TryParse(args[0], out pid) && pid > 0
          ? VRage.MyTuple.Create<int, bool, Action<Process>>(1, true, p => (this.spawner as IProcessManager).Kill(pid))
          : VRage.MyTuple.Create<int, bool, Action<Process>>(1, true, p => (this.spawner as IProcessManager).KillAll(args[0]));
      }

      private VRage.MyTuple<int, bool, Action<Process>> ps(List<string> args, Action<string> log) {
        return VRage.MyTuple.Create<int, bool, Action<Process>>(1, true, p => {
          (this.spawner as IProcessManager).Log(log);
        });
      }

      private void log(string s) => this.logger?.Invoke(s);
    }
  }
}
