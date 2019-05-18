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
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
  partial class Program {
    public class LCDColor {
      public LCDColor(byte red = 0, byte green = 0, byte blue = 0) {
        _r = Math.Min(red, (byte)7);
        _g = Math.Min(green, (byte)7);
        _b = Math.Min(blue, (byte)7);
      }
      public char ToChar() => (char)(0xe100 + ((int)_r << 6) + ((int)_g << 3) + (int)_b);
      byte _r;
      byte _g;
      byte _b;

      public static LCDColor Black = new LCDColor(0, 0, 0);
      public static LCDColor DarkGrey = new LCDColor(1, 1, 1);
      public static LCDColor Green = new LCDColor(red: 1, green: 7, blue: 1);
      public static LCDColor Grey = new LCDColor(3, 3, 3);
      public static LCDColor Red = new LCDColor(red: 7, green: 1, blue: 1);
      public static LCDColor Orange = new LCDColor(red: 7, green: 4);
      public static LCDColor White = new LCDColor(7, 7, 7);
      public static LCDColor Yellow = new LCDColor(red: 7, green: 7);
    }
  }
}
