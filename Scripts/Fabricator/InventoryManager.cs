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
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
  partial class Program
  {

    public enum ComponentStatusLevel { OK, WARNING, ERROR };

    public enum InventoryStatus { Nominal, MandatoryLow, OptionalLow, NotReady };

    public struct ComponentType
    {
      public readonly string Subtype;
      public readonly string DisplayName;
      public readonly int RecommendedAmount;
      public ComponentType(string subtype, string displayName, int recommendedAmount)
      {
        Subtype = subtype;
        DisplayName = displayName;
        RecommendedAmount = recommendedAmount;
      }
    }

    public class ComponentStatus
    {
      public readonly ComponentType Type;
      public int Amount = 0;
      public ComponentStatus(ComponentType type)
      {
        Type = type;
      }
      public ComponentStatusLevel Status
      {
        get
        {
          if (Amount == 0)
          {
            return ComponentStatusLevel.ERROR;
          }
          else if (Amount < Type.RecommendedAmount)
          {
            return ComponentStatusLevel.WARNING;
          }
          else
          {
            return ComponentStatusLevel.OK;
          }
        }
      }
    }

    public class InventoryManager
    {
      public readonly List<ComponentStatus> MandatoryItems = new List<ComponentStatus>{
        new ComponentStatus(new ComponentType("BulletproofGlass", "Bulletproof glass", 100)),
        new ComponentStatus(new ComponentType("Computer", "Computers", 100)),
        new ComponentStatus(new ComponentType("Construction", "Construction components", 500)),
        new ComponentStatus(new ComponentType("Detector", "Detector components", 30)),
        new ComponentStatus(new ComponentType("Display", "Displays", 100)),
        new ComponentStatus(new ComponentType("Girder", "Girders", 100)),
        new ComponentStatus(new ComponentType("InteriorPlate", "Interior plates", 500)),
        new ComponentStatus(new ComponentType("LargeTube", "Large steel tubes", 50)),
        new ComponentStatus(new ComponentType("MetalGrid", "Metal grids", 50)),
        new ComponentStatus(new ComponentType("Motor", "Motors", 200)),
        new ComponentStatus(new ComponentType("PowerCell", "Power cells", 100)),
        new ComponentStatus(new ComponentType("RadioCommunication", "Radio components", 20)),
        new ComponentStatus(new ComponentType("SmallTube", "Small steel tubes", 300)),
        new ComponentStatus(new ComponentType("SteelPlate", "Steel plates", 1000)),
      };

      public readonly List<ComponentStatus> OtherItems = new List<ComponentStatus> {
        new ComponentStatus(new ComponentType("Canvas", "Canvases", 20)),
        new ComponentStatus(new ComponentType("GravityGenerator", "Gravity generator parts", 20)),
        new ComponentStatus(new ComponentType("Medical", "Medical components", 20)),
        new ComponentStatus(new ComponentType("Explosives", "Explosives", 100)),
        new ComponentStatus(new ComponentType("NATO_25x184mm", "Ammo containers", 100)),
        new ComponentStatus(new ComponentType("NATO_5p56x45mm", "Ammo magazines", 100)),
        new ComponentStatus(new ComponentType("Missile200mm", "Missiles", 100)),
        new ComponentStatus(new ComponentType("Reactor", "Reactor components", 100)),
        new ComponentStatus(new ComponentType("SolarCell", "Solar cells", 200)),
        new ComponentStatus(new ComponentType("Superconductor", "Superconductors", 100)),
        new ComponentStatus(new ComponentType("Thrust", "Thruster components", 100)),
      };

      readonly List<IMyCargoContainer> _containers = new List<IMyCargoContainer>();

      readonly List<MyInventoryItem> _items = new List<MyInventoryItem>(); // temporary list

      public InventoryStatus GetStatus()
      {
        bool hasLow = false;
        foreach (ComponentStatus status in MandatoryItems)
        {
          if (status.Status == ComponentStatusLevel.ERROR)
          {
            return InventoryStatus.NotReady;
          }
          else if (status.Status == ComponentStatusLevel.WARNING)
          {
            hasLow = true;
          }
        }
        return hasLow
          ? InventoryStatus.MandatoryLow
          : OtherItems.All(c => c.Status == ComponentStatusLevel.OK)
            ? InventoryStatus.Nominal
            : InventoryStatus.OptionalLow;
      }

      public InventoryManager(IProcessSpawner spawner, Action<List<IMyCargoContainer>> containersGetter)
      {
        containersGetter(_containers);
        spawner.Spawn(p => _updateStatus(containersGetter), "inventory-process", period: 100);
      }

      void _updateStatus(Action<List<IMyCargoContainer>> getter)
      {
        _containers.Clear();
        getter(_containers);

        var amountPerType = new Dictionary<string, int>();

        foreach (IMyCargoContainer container in _containers)
        {
          _items.Clear();
          container.GetInventory().GetItems(_items, i => i.GetItemType() == ItemType.Component || i.GetItemType() == ItemType.Ammo);

          foreach (MyInventoryItem item in _items)
          {
            int amount;
            amountPerType.TryGetValue(item.GetItemSubtype(), out amount);
            amount += item.Amount.ToIntSafe();
            amountPerType[item.GetItemSubtype()] = amount;
          }
        }

        foreach (ComponentStatus status in MandatoryItems.Concat(OtherItems))
        {
          int amount;
          amountPerType.TryGetValue(status.Type.Subtype, out amount);
          status.Amount = amount;
        }
      }
    }
  }
}
