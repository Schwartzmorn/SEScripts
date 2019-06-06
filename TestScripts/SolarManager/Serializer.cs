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
    public class Deserializer {
      public Deserializer(String serializedString) {
        String[] values = serializedString.Split('\n');
        foreach (String value in values) {
          String[] keyValue = value.Split('=');
          if (keyValue.Count() == 2) {
            _values[keyValue[0]] = keyValue[1];
          }
        }
      }
      public float GetAsFloat(String s) {
        float val;
        float.TryParse(Get(s), out val);
        return val;
      }
      public int GetAsInt(String s) {
        int val;
        int.TryParse(Get(s), out val);
        return val;
      }
      public double GetAsDouble(String s) {
        double val;
        double.TryParse(Get(s), out val);
        return val;
      }

      public T GetAsEnum<T>(String s, T dflt) where T: struct {
        T val;
        if (Enum.TryParse(Get(s), out val)) {
          return val;
        } else {
          return dflt;
        }
      }
      public string Get(String s, String dflt = "") {
        String value;
        if (_values.TryGetValue(s, out value)) {
          return value;
        } else {
          return dflt;
        }
      }
      private Dictionary<String, String> _values = new Dictionary<String, String>();
    }
    public class Serializer {
      public Serializer() {}
      public void Serialize<T>(String key, T value) => Values[key] = value.ToString();
      public String GetSerializedString() {
        StringBuilder data = new StringBuilder();
        foreach(var keyValue in Values) {
          data.Append(keyValue.Key).Append("=").Append(keyValue.Value).Append("\n");
        }
        return data.ToString();
      }
      private Dictionary<String, String> Values = new Dictionary<String, String>();
    }
  }
}
