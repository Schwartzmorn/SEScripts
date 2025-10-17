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

namespace IngameScript
{
  partial class Program
  {
    public class Container
    {
      public string DisplayName => _cargo.CustomName;

      private readonly IMyCargoContainer _cargo;
      private readonly bool _isOutput;
      private readonly HashSet<string> _subtypes = new HashSet<string>();
      private readonly HashSet<ItemType> _types = new HashSet<ItemType>();
      private readonly bool _whitelist;

      public Container(IMyCargoContainer block)
      {
        _cargo = block;
        var ini = new MyIni();
        ini.TryParse(block.CustomData);
        _isOutput = ini.Get("filter", "is-output").ToBoolean(false);
        if (!_isOutput)
        {
          _whitelist = ini.Get("filter", "type").ToString("whitelist") == "whitelist";
          string[] types = ini.Get("filter", "item-types").ToString().Split(SPLIT_VALUES_CHAR, StringSplitOptions.RemoveEmptyEntries);
          string[] subtypes = ini.Get("filter", "item-subtypes").ToString().ToLower().Split(SPLIT_VALUES_CHAR, StringSplitOptions.RemoveEmptyEntries);
          _subtypes.UnionWith(subtypes);
          _types.UnionWith(types.Select(t =>
          {
            ItemType i = ItemType.Unknown;
            Enum.TryParse(t, true, out i);
            return i;
          }).Where(i => i != ItemType.Unknown));
        }
      }
      public static int CompareTo(Container A, Container B, MyItemType item) => A == B ? 0 : B.GetAffinity(item) - A.GetAffinity(item);
      public int GetAffinity(MyItemType item)
      {
        int hasItem = _hasItem(item) ? 1 : 0;
        if (_isOutput)
        {
          return hasItem - 6;
        }
        else if (_isGeneric())
        {
          return hasItem + 2;
        }
        else if (_whitelist)
        {
          if (_subtypes.Contains(item.SubtypeId.ToLower()))
          {
            return hasItem + 8;
          }
          if (_types.Contains(item.GetItemType()))
          {
            return hasItem + 6;
          }
          // is KO with whitelist
          return hasItem;
        }
        else
        { // blacklist
          if (_subtypes.Contains(item.SubtypeId.ToLower()))
          {
            return hasItem - 4;
          }
          if (_types.Contains(item.GetItemType()))
          {
            return hasItem - 2;
          }
          // is OK with blacklist
          return hasItem + 4;
        }
      }
      public IMyInventory GetInventory() => _cargo?.GetInventory();
      private bool _isGeneric() => _types.Count == 0 && _subtypes.Count == 0;
      private bool _hasItem(MyItemType item) => GetInventory().FindItem(item) != null;
    }
  }
}
