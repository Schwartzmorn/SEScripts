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
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
  partial class Program {
    public class Container {
      public Container(IMyCargoContainer block) {
        _cargo = block;
        string[] options = _cargo.CustomData.Split(SPLIT_OPTIONS_CHAR, StringSplitOptions.RemoveEmptyEntries);
        foreach (string option in options) {
          string[] kV = option.Split('=');
          if (kV.Length == 2) {
            string[] values = kV[1].Split(SPLIT_VALUES_CHAR, StringSplitOptions.RemoveEmptyEntries);
            if (kV[0] == "types") {
              foreach (string value in values) {
                string val = value[0].ToString().ToUpper() + value.Substring(1);
                ItemType type;
                if (Enum.TryParse(val, out type)) {
                  _containedTypes.Add(type);
                }
              }
            } else if (kV[0] == "subtypes") {
              _containedSubtypes.UnionWith(values.ToList().ConvertAll(value => value.ToLower()));
            }
          }
        }
      }
      public static int CompareTo(Container A, Container B, IMyInventoryItem item) {
        if (A == B) {
          return 0;
        }
        return B.GetAffinity(item) - A.GetAffinity(item);
      }
      public override string ToString() {
        String result = "Container '" + (_cargo == null ? "null" : _cargo.DisplayNameText) + "':";
        if (_containedTypes.Count > 0) {
          result += "\ntypes: " + _containedTypes.ToList().ConvertAll(type => type.ToString()).Aggregate((a, b) => a + ", " + b);
        }
        if (_containedSubtypes.Count > 0) {
          result += "\nsubtypes: " + _containedSubtypes.ToList().Aggregate((a, b) => a + ", " + b);
        }
        return result;
      }
      public int GetAffinity(IMyInventoryItem item) {
        int hasItem = HasItem(item) ? 1 : 0;
        if (IsGeneric()) {
          return hasItem + 2;
        }
        if (_containedSubtypes.Contains(item.GetItemSubype().ToLower())) {
          return hasItem + 6;
        }
        if (_containedTypes.Contains(item.GetItemType())) {
          return hasItem + 4;
        }
        return hasItem;
      }
      public IMyInventory GetInventory() => _cargo?.GetInventory();
      private bool IsGeneric() => _containedTypes.Count == 0 && _containedSubtypes.Count == 0;
      private bool HasItem(IMyInventoryItem item) {
        if (_cargo == null) {
          return false;
        }
        // TODO use FindItem instead ?
        return _cargo.GetInventory().GetItems().Any(it => it.Content.SubtypeName == item.Content.SubtypeName);
      }
      HashSet<ItemType> _containedTypes = new HashSet<ItemType>();
      HashSet<String> _containedSubtypes = new HashSet<String>();
      IMyCargoContainer _cargo;
    }
  }
}
