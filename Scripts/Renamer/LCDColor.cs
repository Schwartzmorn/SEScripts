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

namespace IngameScript
{
  partial class Program
  {
    public class LCDColor
    {
      public LCDColor(byte red = 0, byte green = 0, byte blue = 0)
      {
        _r = Math.Min(red, (byte)7);
        _g = Math.Min(green, (byte)7);
        _b = Math.Min(blue, (byte)7);
        Char = (char)(0xe100 + (_r << 6) + (_g << 3) + _b);
        String = Char.ToString();
      }
      readonly byte _r;
      readonly byte _g;
      readonly byte _b;
      public readonly string String;
      public readonly char Char;

      public override string ToString() => String;

      public static readonly LCDColor BLACK = new LCDColor(0, 0, 0);
      public static readonly LCDColor DARK_GREY = new LCDColor(1, 1, 1);
      public static readonly LCDColor GREEN = new LCDColor(red: 1, green: 7, blue: 1);
      public static readonly LCDColor GREY = new LCDColor(3, 3, 3);
      public static readonly LCDColor RED = new LCDColor(red: 7, green: 1, blue: 1);
      public static readonly LCDColor ORANGE = new LCDColor(red: 7, green: 4);
      public static readonly LCDColor WHITE = new LCDColor(7, 7, 7);
      public static readonly LCDColor YELLOW = new LCDColor(red: 7, green: 7);
      /// <summary>
      /// Empirically observed size ration color char / normal char
      /// </summary>
      public static readonly float CHAR_RATIO = (float)74 / 50;
    }
  }
}