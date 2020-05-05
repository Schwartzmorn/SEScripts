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
  static class DetailedInfoHelper {
    static readonly char[] SEP_LINE = new char[]{ '\n' };
    // does not really work for CustomInfo: CustomInfo is only updated when the block is opened in the terminal
    public static string GetDetailedInfo(this IMyTerminalBlock block, string name) {
      string lineStart = name + ": ";
      string line = (block.DetailedInfo + "\n" + block.CustomInfo).Split(SEP_LINE).FirstOrDefault(l => l.StartsWith(lineStart));
      return line?.Substring(lineStart.Count());
    }
  }
}
