using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
  /// <summary>Enum more convenient to handle for the object main type</summary>
  public enum ItemType { Unknown = 0, Ammo, Component, Hydrogen, Ingot, Ore, Oxygen, Tool };

  static class Item {
    static readonly Dictionary<string, ItemType> TYPES = new Dictionary<string, ItemType> {
    { "MyObjectBuilder_AmmoMagazine", ItemType.Ammo },
    { "MyObjectBuilder_Component", ItemType.Component },
    { "MyObjectBuilder_GasContainerObject", ItemType.Hydrogen },
    { "MyObjectBuilder_Ingot", ItemType.Ingot },
    { "MyObjectBuilder_Ore", ItemType.Ore },
    { "MyObjectBuilder_OxygenContainerObject", ItemType.Oxygen },
    { "MyObjectBuilder_PhysicalGunObject", ItemType.Tool }
  };
    /// <summary>Returns the main type of the item, as an <see cref="ItemType"/></summary>
    /// <param name="item">This</param>
    /// <returns>The item type</returns>
    public static ItemType GetItemType(this MyInventoryItem item) {
      ItemType res;
      TYPES.TryGetValue(item.Type.TypeId, out res);
      return res;
    }
    /// <summary>Returns the actual type of the object</summary>
    /// <param name="item">This</param>
    /// <returns>The type, as a string</returns>
    public static string GetItemSubtype(this MyInventoryItem item) => item.Type.SubtypeId;
  }
}
