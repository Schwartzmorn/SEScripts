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
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;
using VRageRender;
using VRageRender.ExternalApp;

namespace IngameScript
{
  partial class Program
  {
    // public for unit testing
    public readonly static LogSettings LOG_SETTINGS = new LogSettings();

    /// <summary>
    /// This class is a compromise between what would make sense and the weird way Space Engineers count the instructions
    /// </summary>
    public class LogSettings : IIniConsumer
    {
      public static readonly string DEFAULT_NAME = "global";
      static readonly LogLevel DEFAULT_LEVEL = LogLevel.Info;
      static readonly string SECTION = "logging";
      public enum LogLevel
      {
        Debug = 0,
        Info = 1,
        Error = 2,
        Always = 3,
      }

      public Action<string> _logger;

      public LogLevel Level { get; set; } = LogLevel.Info;

      private Dictionary<string, LogLevel> _levels = new Dictionary<string, LogLevel>();

      public void Init(Action<string> logger, IniWatcher ini = null, CommandLine cmd = null, IProcessManager manager = null)
      {
        _logger = logger;
        if (ini != null)
        {
          Read(ini);
        }
        ini?.Add(this);
        manager?.AddOnSave(_save);
        cmd?.RegisterCommand(new ParentCommand("log", "To set the log level")
          .AddSubCommand(
            new Command("set",
              Command.Wrap(_setLevel),
              "Sets the log level for a logger.\nFirst argument is the name of the logger (use global for default level).\nSecond argument is the log level (Do not set for default level).\nPossible values are Debug, Info, Error, Always.",
              minArgs: 1,
              maxArgs: 2
            )
          ).AddSubCommand(
            new Command("reset", Command.Wrap(_reset), "Resets all levels to 0", nArgs: 0)
          ));
      }

      public void Read(MyIni ini)
      {
        var global = ini.Get(SECTION, DEFAULT_NAME);
        var level = LogLevel.Info;
        Level = level;
        if (!global.IsEmpty && Enum.TryParse(global.ToString(), out level))
        {
          Level = level;
        }
        _levels.Clear();
        foreach (var l in Enum.GetValues(typeof(LogLevel)))
        {
          var value = ini.Get(SECTION, level.ToString().ToLower()).ToString().Split(IniHelper.SEP, StringSplitOptions.RemoveEmptyEntries);
          foreach (var log in value)
          {
            SetLevel(log, (LogLevel)l);
          }
        }
      }

      private void _save(MyIni ini)
      {
        if (Level != DEFAULT_LEVEL)
        {
          ini.Set(SECTION, DEFAULT_NAME, Level.ToString());
          foreach (var l in Enum.GetValues(typeof(LogLevel)))
          {
            var levels = _levels.Where(e => e.Value == (LogLevel)l).Select(e => e.Key);
            if (levels.Any())
            {
              ini.Set(SECTION, l.ToString(), string.Join(",", levels));
            }
          }
        }
      }

      private void _reset()
      {
        Level = LogLevel.Info;
        _levels.Clear();
      }

      private void _setLevel(ArgumentsWrapper args)
      {
        LogLevel? level = null;
        if (args.Count() == 2)
        {
          level = (LogLevel)Enum.Parse(typeof(LogLevel), args[1], true);
        }
        SetLevel(args[0], level);
      }

      public void SetLevel(string name, LogLevel? level = null)
      {
        if (level == null)
        {
          if (name == DEFAULT_NAME)
          {
            Level = LogLevel.Info;
          }
          else
          {
            _levels.Remove(name);
          }
        }
        else
        {
          if (name == DEFAULT_NAME)
          {
            Level = level.Value;
          }
          else
          {
            _levels.Add(name, level.Value);
          }
        }
      }

      public bool IsActivated(string name, LogLevel logLevel)
      {
        LogLevel level;
        if (name == null || !_levels.TryGetValue(name, out level))
        {
          level = Level;
        }
        return level <= logLevel;
      }
    }

    /// <summary>
    /// Implemented this way to avoid increasing the instructions count too much
    /// </summary>
    public class Log
    {
      readonly string _prefix;
      readonly string _name;
      readonly LogSettings _settings;
      // LCD Color characters
      readonly static string DEBUG = ((char)57673).ToString();
      readonly static string INFO = ((char)57607).ToString();
      readonly static string ERROR = ((char)58057).ToString();
      readonly static string ALWAYS = ((char)58111).ToString();

      public Log(string name, LogSettings settings)
      {
        if (LogSettings.DEFAULT_NAME == name?.ToLower())
        {
          throw new ArgumentException($"Log name 'global' is reserved. Please rename your log. ({name})");
        }
        if (name?.Contains(",") ?? false)
        {
          throw new ArgumentException($"Log name cannot contain ',' ({name}).");
        }
        _prefix = (name != null && name.Count() > 0) ? $"{name}: " : "";
        _name = name;
        _settings = settings;
      }

      public void Debug(string msg)
      {
        if (_settings.IsActivated(_name, LogSettings.LogLevel.Debug))
        {
          _settings._logger?.Invoke($"{DEBUG}{_prefix}{msg}");
        }
      }

      public void Info(string msg)
      {
        if (_settings.IsActivated(_name, LogSettings.LogLevel.Info))
        {
          _settings._logger?.Invoke($"{INFO}{_prefix}{msg}");
        }
      }

      public void Error(string msg)
      {
        if (_settings.IsActivated(_name, LogSettings.LogLevel.Error))
        {
          _settings._logger?.Invoke($"{ERROR}{_prefix}{msg}");
        }
      }

      public void Always(string msg)
      {
        _settings._logger?.Invoke($"{ALWAYS}{_prefix}{msg}");
      }

      public static Log GetLog(string name)
      {
        return new Log(name, LOG_SETTINGS);
      }
    }
  }
}