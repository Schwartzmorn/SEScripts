using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript {
  partial class Program {
    public class Container {
      public string DisplayName => this._cargo.DisplayNameText;

      private readonly IMyCargoContainer _cargo;
      private readonly bool _isOutput;
      private readonly HashSet<string> _subtypes = new HashSet<string>();
      private readonly HashSet<ItemType> _types = new HashSet<ItemType>();
      private readonly bool _whitelist;

      public Container(IMyCargoContainer block) {
        this._cargo = block;
        var ini = new MyIni();
        ini.TryParse(block.CustomData);
        this._isOutput = ini.Get("filter", "is-output").ToBoolean(false);
        if (!this._isOutput) {
          this._whitelist = ini.Get("filter", "type").ToString("whitelist") == "whitelist";
          string[] types = ini.Get("filter", "item-types").ToString().Split(SPLIT_VALUES_CHAR, StringSplitOptions.RemoveEmptyEntries);
          string[] subtypes = ini.Get("filter", "item-subtypes").ToString().ToLower().Split(SPLIT_VALUES_CHAR, StringSplitOptions.RemoveEmptyEntries);
          this._subtypes.UnionWith(subtypes);
          this._types.UnionWith(types.Select(t => {
            ItemType i = ItemType.Unknown;
            Enum.TryParse(t, true, out i);
            return i;
          }).Where(i => i != ItemType.Unknown));
        }
      }
      public static int CompareTo(Container A, Container B, MyInventoryItem item) => A == B ? 0 : B.GetAffinity(item) - A.GetAffinity(item);
      public int GetAffinity(MyInventoryItem item) {
        int hasItem = this._hasItem(item) ? 1 : 0;
        if (this._isOutput) {
          return hasItem - 6;
        } else if (this._isGeneric()) {
          return hasItem + 2;
        } else if (this._whitelist) {
          if(this._subtypes.Contains(item.GetItemSubtype().ToLower())) {
            return hasItem + 8;
          }
          if(this._types.Contains(item.GetItemType())) {
            return hasItem + 6;
          }
          // is KO with whitelist
          return hasItem;
        } else { // blacklist
          if(this._subtypes.Contains(item.GetItemSubtype().ToLower())) {
            return hasItem - 4;
          }
          if(this._types.Contains(item.GetItemType())) {
            return hasItem - 2;
          }
          // is OK with blacklist
          return hasItem + 4;
        }
      }
      public IMyInventory GetInventory() => this._cargo?.GetInventory();
      private bool _isGeneric() => this._types.Count == 0 && this._subtypes.Count == 0;
      private bool _hasItem(MyInventoryItem item) => this._cargo.GetInventory().FindItem(item.Type) != null;
    }
  }
}
