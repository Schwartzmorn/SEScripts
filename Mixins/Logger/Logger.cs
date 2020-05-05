using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
  partial class Program {
    class Logger {
      readonly Action<string> echo;
      readonly CircularBuffer<string> messages;
      bool changed;
      readonly IMyTextSurface surface;
      public Logger(IProcessSpawner spawner, IMyTextSurface s, Color? bgdCol = null, Color? col = null, Action<string> echo = null, float size = 0.5f) {
        int nMsgs = 1;
        if (s != null) {
          s.TextPadding = 0;
          s.ContentType = (ContentType)1;
          s.Alignment = 0;
          s.Font = "Monospace";
          s.FontSize = size;
          s.FontColor = col ?? Color.White;
          s.BackgroundColor = bgdCol ?? Color.Black;
          var sb = new StringBuilder("G");
          nMsgs = (int)((GetMultiplier(s) * s.SurfaceSize.Y / s.MeasureStringInPixels(sb, s.Font, s.FontSize).Y) + 0.1f);
          this.surface = s;
        }
        this.messages = new CircularBuffer<string>(nMsgs);
        spawner.Spawn(p => this.flush(), "logger");
        this.echo = echo;
      }
      public void Log(string log) {
        this.changed = true;
        foreach (string l in log.Split('\n')) {
          this.messages.Enqueue(l);
        }

        this.echo?.Invoke(log);
      }
      void flush() {
        if (this.changed) {
          this.changed = false;
          this.surface?.WriteText(string.Join("\n", this.messages));
        }
      }
      static float GetMultiplier(IMyTextSurface s) {
        float sy = s.SurfaceSize.Y;
        if (s is IMyTerminalBlock) {//txt panel
          string t = (s as IMyTerminalBlock).BlockDefinition.SubtypeId;
          if (t.Contains("Corner")) {
            return t.Contains("Large") ? t.Contains("Flat") ? 0.168f : 0.146f : t.Contains("Flat") ? 0.302f : 0.260f;
          }
        } else {
          string nm = s.DisplayName;
          if (nm == "Large Display") {
            return sy < 200 ? 4 : sy < 300 ? 2 : 1;//flt sit,small prog blk
          } else if (nm == "Keyboard") {
            return (sy < 110) ? 4 : 2;//fter cpit and small prog blk
          } else if ((nm ?? "").Contains("Screen")) {
            return (sy < 100 && nm.Contains("Top") && (nm.Contains("Left") || nm.Contains("Right"))) ? 4 : 2;//fter cpit top left right screens
          }
        }
        return 1;
      }
    }
  }
}
