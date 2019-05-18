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
    public partial class Marshaller {
      private StringBuilder Storage = new StringBuilder();
      public void Marshall(String s) {
        String marshalled = String.Copy(s);
        marshalled.Replace("\\", "\\\\");
        marshalled.Replace(";", "\\;");
        Storage.Append(marshalled).Append(';');
      }
      public void MarshallList<T>(IEnumerable<T> list) {
        Marshall(list.Count().ToString());
        foreach(T val in list) {
          Marshall(val.ToString());
        }
      }
      public void Marshall<T>(T value) => Marshall(value.ToString());
      public String GetStorage() => Storage.ToString();
    }
    partial class Unmarshaller {
      public Unmarshaller(String storage) {
        int startIndex = 0;
        int consecutiveSlashes = 0;
        for (int i = 0; i < storage.Count(); ++i) {
          if (storage[i] == ';' && (consecutiveSlashes % 2 == 0)) {
            consecutiveSlashes = 0;
            String value = storage.Substring(startIndex, i - startIndex);
            startIndex = i + 1;
            value.Replace("\\;", ";");
            value.Replace("\\\\", "\\");
            _storage.Add(value);
          } else if (storage[i] == '\\') {
            ++consecutiveSlashes;
          } else {
            consecutiveSlashes = 0;
          }
        }
      }
      public int UnmarshallInt() {
        int value;
        int.TryParse(GetNextValue("0"), out value);
        return value;
      }
      public String UnmarshallString() => GetNextValue("");
      public double UnmarshallDouble() {
        double value;
        double.TryParse(GetNextValue("0"), out value);
        return value;
      }
      public double[] UnmarshallArrayOfDouble() {
        int size = UnmarshallInt();
        double[] res = new double[size];
        for (int i = 0; i < size; ++i) {
          res[i] = UnmarshallDouble();
        }
        return res;
      }
      public string[] UnmarshallArrayOfString() {
        int size = UnmarshallInt();
        string[] res = new string[size];
        for (int i = 0; i < size; ++i) {
          res[i] = UnmarshallString();
        }
        return res;
      }
      private String GetNextValue(String dflt) {
        if (_index < _storage.Count()) {
          return _storage[_index++];
        } else {
          return "";
        }
      }
      private List<String> _storage = new List<String>();
      private int _index = 0;
    }
  }
}
