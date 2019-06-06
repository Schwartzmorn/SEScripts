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

namespace IngameScript {
static class IniHelper {
public static readonly char[] SEP = new char[] { ',' };
public static MyIniValue GetThrow(this MyIni ini, string section, string key) {
var res = ini.Get(section, key);
if(res.IsEmpty)
  throw new InvalidOperationException($"Need key '{key}' in section '{section}' in custom data");
return res;
}

public static void SetVector(this MyIni ini, string section, string name, Vector3D v) {
ini.Set(section, $"{name}-x", v.X);
ini.Set(section, $"{name}-y", v.Y);
ini.Set(section, $"{name}-z", v.Z);
}

public static Vector3D GetVector(this MyIni ini, string section, string name) => new Vector3D(
ini.GetThrow(section, $"{name}-x").ToDouble(),
ini.GetThrow(section, $"{name}-y").ToDouble(),
ini.GetThrow(section, $"{name}-z").ToDouble());

public static void Parse(this MyIni ini, string data) {
MyIniParseResult res;
if (!ini.TryParse(data, out res))
  throw new InvalidOperationException($"Error '{res.Error}' at line {res.LineNo} when parsing");
}
}
}
