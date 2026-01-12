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

namespace IngameScript
{
  partial class Program
  {
    // public for unit testing
    public readonly static LogSettings LOG_SETTINGS = new LogSettings();

    /// <summary>
    /// This class is a compromise between what would make sense and the weird way Space Engineers count the instructions
    /// </summary>
    public class LogSettings
    {
      public enum LogLevel
      {
        Debug = 0,
        Info = 1,
        Error = 2,
        Always = 3,
      }

      public Action<string> _logger;

      public LogLevel Level { get; set; } = LogLevel.Info;

      public void SetLogger(Action<string> logger)
      {
        _logger = logger;
      }
    }

    /// <summary>
    /// Implemented this way to avoid increasing the instructions count too much
    /// </summary>
    public class Log
    {
      readonly string _name;
      readonly LogSettings _settings;
      // LCD Color characters
      readonly static string DEBUG = ((char)57673).ToString();
      readonly static string INFO = ((char)57607).ToString();
      readonly static string ERROR = ((char)58057).ToString();
      readonly static string ALWAYS = ((char)58111).ToString();

      public Log(string name, LogSettings settings)
      {
        _name = name;
        _settings = settings;
      }

      public void Debug(string msg)
      {
        if (_settings.Level == LogSettings.LogLevel.Debug)
        {
          _settings._logger?.Invoke($"{DEBUG}{_name}: {msg}");
        }
      }

      public void Info(string msg)
      {
        if (_settings.Level <= LogSettings.LogLevel.Info)
        {
          _settings._logger?.Invoke($"{INFO}{_name}: {msg}");
        }
      }

      public void Error(string msg)
      {
        if (_settings.Level <= LogSettings.LogLevel.Error)
        {
          _settings._logger?.Invoke($"{ERROR}{_name}: {msg}");
        }
      }

      public void Always(string msg)
      {
        _settings._logger?.Invoke($"{ALWAYS}{_name}: {msg}");
      }

      public static Log GetLog(string name)
      {
        return new Log(name, LOG_SETTINGS);
      }
    }
  }
}