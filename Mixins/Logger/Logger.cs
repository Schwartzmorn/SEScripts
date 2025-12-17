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
    public class Log
    {
      public enum LogLevel
      {
        Debug = 0,
        Info = 1,
        Error = 2,
        Always = 3,
      }

      private Action<string> _logger;
      // private Action<LogLevel, string> _richLogger;

      public LogLevel Level { get; set; } = LogLevel.Debug;

      public void SetLogger(Action<string> logger)
      {
        _logger = logger;
      }

      private void _log(LogLevel level, string format, params object[] args)
      {
        if (level >= Level)
        {
          var msg = string.Format(format, args);
          _logger?.Invoke(msg);
          // _richLogger?.Invoke(level, msg);
        }
      }

      public void Debug(string format, params object[] args)
      {
        _log(LogLevel.Debug, format, args);
      }

      public void Info(string format, params object[] args)
      {
        _log(LogLevel.Info, format, args);
      }

      public void Error(string format, params object[] args)
      {
        _log(LogLevel.Error, format, args);
      }

      public void Always(string format, params object[] args)
      {
        _log(LogLevel.Always, format, args);
      }
    }
  }
}