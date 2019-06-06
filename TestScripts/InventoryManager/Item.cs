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
  public enum ItemType { Ammo, Component, Hydrogen, Ingot, Ore, Oxygen, Tool, Unknown = 0 };

  static class Item {
    static int TSUBSTR = 16; // corresponds to the size of "MyObjectBuilder_"
    static Dictionary<string, ItemType> Types = new Dictionary<string, ItemType> {
      { "AmmoMagazine", ItemType.Ammo },
      { "Component", ItemType.Component },
      { "GasContainerObject", ItemType.Hydrogen },
      { "Ingot", ItemType.Ingot },
      { "Ore", ItemType.Ore },
      { "OxygenContainerObject", ItemType.Oxygen },
      { "PhysicalGunObject", ItemType.Tool }
    };
    public static ItemType GetItemType(this IMyInventoryItem item) {
      ItemType res;
      Types.TryGetValue(item.Content.TypeId.ToString().Substring(TSUBSTR), out res);
      return res;
    }
    public static string GetItemSubype(this IMyInventoryItem item) => item.Content.SubtypeName;
  }
}
