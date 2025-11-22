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
using VRageRender;
using System.Collections.ObjectModel;

namespace IngameScript
{
  partial class Program
  {
    public class Metrics
    {
      readonly Dictionary<string, Dictionary<string, double>> _metrics = new Dictionary<string, Dictionary<string, double>>();

      public Metrics()
      {
        _metrics = new Dictionary<string, Dictionary<string, double>>();
      }

      public void Increment(string name, string label, double value)
      {
        var dict = _getOrDefault(name);
        dict[label] = dict.GetValueOrDefault(label) + value;
      }

      public void Set(string name, string label, double value)
      {
        _getOrDefault(name)[label] = value;
      }

      public double Get(string name, string label)
      {
        var dict = _metrics[name];
        if (dict == null)
        {
          return 0;
        }
        return dict.GetValueOrDefault(label);
      }

      public void AddMax(string name, string label, double value)
      {
        var dict = _getOrDefault(name);
        dict[label] = Math.Max(dict.GetValueOrDefault(label), value);
      }

      public IEnumerable<KeyValuePair<string, double>> Get(string name)
      {
        return _metrics[name];
      }

      public static string Normalize(string s) => s.Replace("#", "_").Replace(";", "_");

      public string Serialize()
      {
        var sb = new StringBuilder();
        foreach (var m in _metrics)
        {
          sb.Append(m.Key).Append("#");
          foreach (var n in m.Value)
          {
            sb.Append(n.Key).Append("#").Append(n.Value.ToString()).Append("#");
          }
          sb.Append(";");
        }
        return sb.ToString();
      }

      private Dictionary<string, double> _getOrDefault(string name)
      {
        Dictionary<string, double> dict;
        if (!_metrics.TryGetValue(name, out dict))
        {
          dict = new Dictionary<string, double>();
          _metrics[name] = dict;
        }
        return dict;
      }
    }
  }
}
