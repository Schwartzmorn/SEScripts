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

namespace IngameScript {
  public static class ConversionHelper {
    public static readonly System.Text.RegularExpressions.Regex AMOUNT_REGEX = new System.Text.RegularExpressions.Regex("([0-9.,]+)\\s*([A-Za-z]+)");
    static readonly List<MyTuple<char, float>> PREFIXES = new List<MyTuple<char, float>> { MyTuple.Create('G', 1000000000f), MyTuple.Create('M', 1000000f), MyTuple.Create('K', 1000f) };
    public static float GetAmount(this string amountString) {
      if (amountString == null) {
        return 0;
      }
      var res = AMOUNT_REGEX.Match(amountString);
      if (!res.Success) {
        return 0;
      }
      float amount = float.Parse(res.Groups[1].Value);
      foreach(var prefix in PREFIXES) {
        if (res.Groups[2].Value[0] == prefix.Item1) {
          amount *= prefix.Item2;
          break;
        }
      }
      return amount;
    }

    public static string FormatAmount(this float amount, string unit) {
      foreach (var prefix in PREFIXES) {
        if (amount >= prefix.Item2) {
          amount /= prefix.Item2;
          unit = prefix.Item1 + unit;
          break;
        }
      }
      return $"{amount:.##} {unit}";
    }
  }
}
