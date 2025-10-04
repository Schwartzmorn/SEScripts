namespace Utilities.Mocks.Base;

using System.Collections.Generic;
using VRage;

public static class ConversionHelper
{
  static readonly List<MyTuple<char, float>> PREFIXES = new List<MyTuple<char, float>> { MyTuple.Create('G', 1000000000f), MyTuple.Create('M', 1000000f), MyTuple.Create('K', 1000f) };

  /// <summary>
  /// Formats Amounts in the same way SE does. Copied from ConversionHelper as importing shared items causes issues down the line
  /// </summary>
  /// <param name="amount"></param>
  /// <param name="unit"></param>
  /// <returns></returns>
  public static string FormatAmount(this float amount, string unit)
  {
    foreach (var prefix in PREFIXES)
    {
      if (amount >= prefix.Item2)
      {
        amount /= prefix.Item2;
        unit = prefix.Item1 + unit;
        break;
      }
    }
    return $"{amount:.##} {unit}";
  }
}
