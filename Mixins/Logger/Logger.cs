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
      public Logger(IProcessSpawner spawner, IMyTextSurface surface, Color? bgdCol = null, Color? col = null, Action<string> echo = null, float size = 0.5f) {
        int nMsgs = 1;
        if (surface != null) {
          surface.TextPadding = 0;
          surface.ContentType = (ContentType)1;
          surface.Alignment = (TextAlignment)0;
          surface.Font = "Monospace";
          surface.FontSize = size;
          surface.FontColor = col ?? Color.White;
          surface.BackgroundColor = bgdCol ?? Color.Black;
          var sb = new StringBuilder("G");
          nMsgs = (int)((GetMultiplier(surface) * surface.SurfaceSize.Y / surface.MeasureStringInPixels(sb, surface.Font, surface.FontSize).Y) + 0.1f);
          this.surface = surface;
        }
        this.messages = new CircularBuffer<string>(nMsgs);
        spawner.Spawn(this.flush, "logger");
        this.echo = echo;
      }
      public void Log(string log) {
        this.changed = true;
        string[] logs = log.Split('\n');
        foreach (string l in logs) {
          this.messages.Enqueue(l + '\n');
        }

        this.echo?.Invoke(log);
      }
      void flush(Process p) {
        if (this.changed) {
          this.changed = false;
          this.surface?.WriteText(this.messages.ToString());
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
          } else if (nm.Contains("Screen")) {
            return (sy < 100 && nm.Contains("Top") && (nm.Contains("Left") || nm.Contains("Right"))) ? 4 : 2;//fter cpit top left right screens
          }
        }
        return 1;
      }
    }
  }
}
