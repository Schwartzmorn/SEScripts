using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
  /// <summary>Small extension class to the MyInI class</summary>
  static class IniHelper
  {
    public static readonly char[] SEP = new char[] { ',' };
    /// <summary>Returns the value requested and throws if it does not exist.</summary>
    /// <param name="ini">This</param>
    /// <param name="section">Name of the section</param>
    /// <param name="key">Key</param>
    /// <returns>The value corresponding to the key in the section</returns>
    public static MyIniValue GetThrow(this MyIni ini, string section, string key)
    {
      MyIniValue res = ini.Get(section, key);
      if (res.IsEmpty)
      {
        throw new InvalidOperationException($"Need key '{key}' in section '{section}' in custom data");
      }
      return res;
    }
    /// <summary>Serializes a vector in an ini file.</summary>
    /// <param name="ini">This</param>
    /// <param name="section">Section where to put the vector</param>
    /// <param name="name">Key where to put the vector</param>
    /// <param name="v">Vector to serialize</param>
    public static void SetVector(this MyIni ini, string section, string name, Vector3D v)
    {
      ini.Set(section, name, $"{v.X:F2},{v.Y:F2},{v.Z:F2}");
    }
    /// <summary>Deserializes a vector stored in the same way than with <see cref="SetVector(MyIni, string, string, Vector3D)"/>. Throws if incomplete.</summary>
    /// <param name="ini">This</param>
    /// <param name="section">Section where the vector is</param>
    /// <param name="name">Key used when serializing</param>
    /// <returns>The vector</returns>
    public static Vector3D GetVector(this MyIni ini, string section, string name)
    {
      var vec = ini.GetThrow(section, name).ToString().Split(",");
      return new Vector3D(double.Parse(vec[0]), double.Parse(vec[1]), double.Parse(vec[2]));
    }
    /// <summary>Parses the text and throws if the parse was unsucessful.</summary>
    /// <param name="ini">This</param>
    /// <param name="data">Serialized ini</param>
    public static void Parse(this MyIni ini, string data)
    {
      MyIniParseResult res;
      if (!ini.TryParse(data, out res))
      {
        throw new InvalidOperationException($"Error '{res.Error}' at line {res.LineNo} when parsing");
      }
    }
  }
}
