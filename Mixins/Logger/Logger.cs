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
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
  partial class Program {
    class Logger {
      public static Logger Inst { get; private set; }

      private static readonly List<Logger> INSTANCES = new List<Logger>();

      private Action<string> _echo;
      private readonly CircularBuffer<string> _msgs;
      private bool _changed = false;
      private readonly bool _dbg = true;
      private readonly IMyTextSurface _s;

      public static void SetupGlobalInstance(Logger logger, Action<string> echo) {
        if (Inst != null) {
          Inst._echo = null;
        }
        logger._echo = echo;
        Inst = logger;
      }

      public Logger(IMyTextSurface s, float fontSize = 0.5f, Color? fontColor = null, Color? bgdColor = null, bool debug = false) {
        _dbg = debug;
        int maxMessages = 20;
        if (s != null) {
          s.TextPadding = 0;
          s.ContentType = ContentType.TEXT_AND_IMAGE;
          s.Alignment = TextAlignment.LEFT;
          s.Font = "Monospace";
          s.FontSize = fontSize;
          s.FontColor = fontColor ?? Color.White;
          s.BackgroundColor = bgdColor ?? Color.Black;

          maxMessages = ComputeMaxMessages(s);

          _s = s;
        }
        _msgs = new CircularBuffer<string>(maxMessages);
        INSTANCES.Add(this);
      }

      public void Log(string log) {
        _changed = true;
        string[] logs = log.Split('\n');
        foreach (string l in logs) {
          _msgs.Enqueue(l + '\n');
        }
        _echo?.Invoke(log);
        if (_dbg) {
          _flush();
        }
      }

      public static void Flush() {
        foreach (var logger in INSTANCES) {
          logger._flush();
        }
      }

      private void _flush() {
        if (_changed) {
          _changed = false;
          string log = _msgs.ToString();
          _s?.WriteText(log);
         }
      }

      static private int ComputeMaxMessages(IMyTextSurface s) {
        var sb = new StringBuilder();
        sb.Append("G");
        float sy = s.SurfaceSize.Y;
        float nMsg = sy / s.MeasureStringInPixels(sb, s.Font, s.FontSize).Y;

        if (s is IMyTerminalBlock) {
          string t = (s as IMyTerminalBlock).BlockDefinition.SubtypeId;
          // txt panel
          if (t.Contains("Corner")) {
            if (t.Contains("Large")) {
              if (t.Contains("Flat")) {
                nMsg *= 0.168f;
              } else { // corner
                nMsg *= 0.146f;
              }
            } else { // small
              if (t.Contains("Flat")) {
                nMsg *= 0.302f;
              } else { // corner
                nMsg *= 0.260f;
              }
            }
          }
        } else {
          string nm = s.DisplayName;
          if (nm == "Large Display") {
            if (sy < 200) {
              // flight seat
              nMsg *= 4f;
            } else if (sy < 300) {
              // small prog block
              nMsg *= 2f;
            }
          } else if (nm == "Keyboard") {
            if (sy < 110f) {
              // fighter cpit and small prog block
              nMsg *= 4f;
            } else {
              nMsg *= 2f;
            }
          } else if (nm.Contains("Screen")) {
            if (nm.Contains("Top")
                && (nm.Contains("Left") || nm.Contains("Right"))
                && sy < 100) {
              // fighter cpit top left right screens
              nMsg *= 4f;
            } else {
              nMsg *= 2f;
            }
          }
        }
        return (int)(nMsg + 0.1f);
      }
    }
  }
}
