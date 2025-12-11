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
      public string DisplayName => Cargo.CustomName;

      public readonly IMyCargoContainer Cargo;
      private readonly HashSet<string> _subtypes = new HashSet<string>();
      private readonly HashSet<ItemType> _types = new HashSet<ItemType>();
      private readonly Dictionary<MyItemType, int> _memoizedAffinities = new Dictionary<MyItemType, int>();
      private bool _isOutput;
      private bool _whitelist;
      private string _ini;

      public Container(IMyCargoContainer block)
      {
        Cargo = block;
        ParseIni();
      }

      public void ParseIni()
      {
        if (_ini == Cargo.CustomData)
        {
          return;
        }
        _ini = Cargo.CustomData;
        var ini = new MyIni();
        ini.TryParse(_ini);
        _isOutput = ini.Get("filter", "is-output").ToBoolean(false);
        if (!_isOutput)
        {
          _whitelist = ini.Get("filter", "type").ToString("whitelist") == "whitelist";
          string[] subtypes = ini.Get("filter", "item-subtypes").ToString().ToLower().Split(SPLIT_VALUES_CHAR, StringSplitOptions.RemoveEmptyEntries);
          _subtypes.Clear();
          _subtypes.UnionWith(subtypes);
          _types.Clear();
          string[] types = ini.Get("filter", "item-types").ToString().Split(SPLIT_VALUES_CHAR, StringSplitOptions.RemoveEmptyEntries);
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
      public IMyInventory GetInventory() => Cargo?.GetInventory();
      private int _hasItem(MyItemType item) => GetInventory().FindItem(item) == null ? 0 : 1;
      public int GetIntrinsicAffinity(MyItemType item)
      {
        int affinity;
        if (_memoizedAffinities.TryGetValue(item, out affinity))
        {
          return affinity;
        }
        if (_isOutput)
        {
          affinity = -6;
        }
        else if (_types.Count == 0 && _subtypes.Count == 0)
        {
          // Generic cargo
          affinity = 2;
        }
        else if (_whitelist)
        {
          if (_subtypes.Contains(item.SubtypeId.ToLower()))
          {
            affinity = 8;
          }
          else if (_types.Contains(item.GetItemType()))
          {
            affinity = 6;
          }
          else
          {
            // is KO with whitelist
            affinity = 0;

          }
        }
        else
        { // blacklist
          if (_subtypes.Contains(item.SubtypeId.ToLower()))
          {
            affinity = -4;
          }
          else if (_types.Contains(item.GetItemType()))
          {
            affinity = -2;
          }
          else
          {
            // is OK with blacklist
            affinity = 4;

          }
        }
        _memoizedAffinities[item] = affinity;
        return affinity;
      }
    }
  }
}
