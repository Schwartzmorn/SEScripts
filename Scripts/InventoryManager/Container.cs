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
      public static int CompareTo(Container A, Container B, MyItemType item)
      {
        if (A == B)
        {
          return 0;
        }
        var affA = A.GetIntrinsicAffinity(item);
        var affB = B.GetIntrinsicAffinity(item);
        if (affA != affB)
        {
          return affB - affA;
        }
        return B._hasItem(item) - A._hasItem(item);
      }
      public int GetAffinity(MyItemType item)
      {
        return GetIntrinsicAffinity(item) + _hasItem(item);
      }
      public IMyInventory GetInventory() => _cargo?.GetInventory();
      private int _hasItem(MyItemType item) => GetInventory().FindItem(item) == null ? 0 : 1;
      public int GetIntrinsicAffinity(MyItemType item)
      {
        if (_isOutput)
        {
          return -6;
        }
        else if (_types.Count == 0 && _subtypes.Count == 0)
        {
          return 2;
        }
        else if (_whitelist)
        {
          if (_subtypes.Contains(item.SubtypeId.ToLower()))
          {
            return 8;
          }
          if (_types.Contains(item.GetItemType()))
          {
            return 6;
          }
          // is KO with whitelist
          return 0;
        }
        else
        { // blacklist
          if (_subtypes.Contains(item.SubtypeId.ToLower()))
          {
            return -4;
          }
          if (_types.Contains(item.GetItemType()))
          {
            return -2;
          }
          // is OK with blacklist
          return 4;
        }

      }
    }
  }
}
