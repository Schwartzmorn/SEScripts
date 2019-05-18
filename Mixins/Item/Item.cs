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
    public static ItemType GetItemType(this MyInventoryItem item) {
      ItemType res;
      TYPES.TryGetValue(item.Type.TypeId, out res);
      return res;
    }
    public static string GetItemSubype(this MyInventoryItem item) => item.Type.SubtypeId;
  }
}
