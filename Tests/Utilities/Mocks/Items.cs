namespace Utilities.Mocks;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sandbox.Definitions;
using VRage;
using VRage.FileSystem;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.ModAPI.Ingame;
using VRage.ObjectBuilders;
using VRage.Utils;

/// <summary>
/// Static class to help with instanciating objects
/// </summary>
public static class Items
{
  /// <summary>
  /// While this is essentially the same mapping than in the Item mixin, I cannot reference mixins in this project
  /// </summary>
  static readonly Dictionary<string, string> TYPES_SHORT = new() {
    { "MyObjectBuilder_AmmoMagazine", "Ammo" },
    { "MyObjectBuilder_Component", "Component" },
    { "MyObjectBuilder_ConsumableItem", "Consumable" },
    { "MyObjectBuilder_GasContainerObject", "Hydrogen" },
    { "MyObjectBuilder_Ingot", "Ingot" },
    { "MyObjectBuilder_Ore", "Ore" },
    { "MyObjectBuilder_OxygenContainerObject", "Oxygen" },
    { "MyObjectBuilder_PhysicalGunObject", "Tool" },
  };

  private static readonly Dictionary<string, MyItemType> SUBTYPES_DIC = [];

  /// <summary>
  /// Experiments done to (fail to) load more information from objects and allow the use of "GetItemInfo(this MyItemType)"
  /// </summary>
  /// <param name="assembly"></param>
  public static void Experiment(Assembly assembly)
  {
    // the MDK plugin should probably know that already
    MyLog.Default = new MyLog(false);
    MyFileSystem.Init("C:\\Program Files (x86)\\Steam\\steamapps\\common\\SpaceEngineers", "C:\\Users\\BigBro\\AppData\\Roaming\\SpaceEngineers");
    MyDefinitionManager.Static.PreloadDefinitions();

    var otherAssembly = Assembly.Load("VRage.Game.XmlSerializers");
    MyDefinitionManagerBase.RegisterTypesFromAssembly(assembly);
  }

  static Items()
  {
    // necessary before being able to MyItemType
    var assembly = Assembly.Load("VRage.Game");
    MyObjectBuilderType.RegisterFromAssembly(assembly);

    // Experiment(assembly);

    // list compiled from SE 1.207 that does not contain consumable object for some reason
    // some objects excluded because the corresponding MyObjectBuilderType did not exist
    List<MyItemType> itemTypes = [
      new MyItemType("MyObjectBuilder_AmmoMagazine", "SemiAutoPistolMagazine"),
      new MyItemType("MyObjectBuilder_AmmoMagazine", "FullAutoPistolMagazine"),
      new MyItemType("MyObjectBuilder_AmmoMagazine", "ElitePistolMagazine"),
      new MyItemType("MyObjectBuilder_AmmoMagazine", "FlareClip"),
      new MyItemType("MyObjectBuilder_AmmoMagazine", "FireworksBoxBlue"),
      new MyItemType("MyObjectBuilder_AmmoMagazine", "FireworksBoxGreen"),
      new MyItemType("MyObjectBuilder_AmmoMagazine", "FireworksBoxRed"),
      new MyItemType("MyObjectBuilder_AmmoMagazine", "FireworksBoxPink"),
      new MyItemType("MyObjectBuilder_AmmoMagazine", "FireworksBoxYellow"),
      new MyItemType("MyObjectBuilder_AmmoMagazine", "FireworksBoxRainbow"),
      new MyItemType("MyObjectBuilder_AmmoMagazine", "AutomaticRifleGun_Mag_20rd"),
      new MyItemType("MyObjectBuilder_AmmoMagazine", "RapidFireAutomaticRifleGun_Mag_50rd"),
      new MyItemType("MyObjectBuilder_AmmoMagazine", "PreciseAutomaticRifleGun_Mag_5rd"),
      new MyItemType("MyObjectBuilder_AmmoMagazine", "UltimateAutomaticRifleGun_Mag_30rd"),
      new MyItemType("MyObjectBuilder_AmmoMagazine", "NATO_5p56x45mm"),
      new MyItemType("MyObjectBuilder_AmmoMagazine", "AutocannonClip"),
      new MyItemType("MyObjectBuilder_AmmoMagazine", "NATO_25x184mm"),
      new MyItemType("MyObjectBuilder_AmmoMagazine", "Missile200mm"),
      new MyItemType("MyObjectBuilder_AmmoMagazine", "LargeCalibreAmmo"),
      new MyItemType("MyObjectBuilder_AmmoMagazine", "MediumCalibreAmmo"),
      new MyItemType("MyObjectBuilder_AmmoMagazine", "LargeRailgunAmmo"),
      new MyItemType("MyObjectBuilder_AmmoMagazine", "SmallRailgunAmmo"),
      new MyItemType("MyObjectBuilder_Component", "Construction"),
      new MyItemType("MyObjectBuilder_Component", "MetalGrid"),
      new MyItemType("MyObjectBuilder_Component", "InteriorPlate"),
      new MyItemType("MyObjectBuilder_Component", "SteelPlate"),
      new MyItemType("MyObjectBuilder_Component", "Girder"),
      new MyItemType("MyObjectBuilder_Component", "SmallTube"),
      new MyItemType("MyObjectBuilder_Component", "LargeTube"),
      new MyItemType("MyObjectBuilder_Component", "Motor"),
      new MyItemType("MyObjectBuilder_Component", "Display"),
      new MyItemType("MyObjectBuilder_Component", "BulletproofGlass"),
      new MyItemType("MyObjectBuilder_Component", "Superconductor"),
      new MyItemType("MyObjectBuilder_Component", "Computer"),
      new MyItemType("MyObjectBuilder_Component", "Reactor"),
      new MyItemType("MyObjectBuilder_Component", "Thrust"),
      new MyItemType("MyObjectBuilder_Component", "GravityGenerator"),
      new MyItemType("MyObjectBuilder_Component", "Medical"),
      new MyItemType("MyObjectBuilder_Component", "RadioCommunication"),
      new MyItemType("MyObjectBuilder_Component", "Detector"),
      new MyItemType("MyObjectBuilder_Component", "Explosives"),
      new MyItemType("MyObjectBuilder_Component", "SolarCell"),
      new MyItemType("MyObjectBuilder_Component", "PowerCell"),
      new MyItemType("MyObjectBuilder_Component", "Canvas"),
      new MyItemType("MyObjectBuilder_Component", "EngineerPlushie"),
      new MyItemType("MyObjectBuilder_Component", "SabiroidPlushie"),
      new MyItemType("MyObjectBuilder_Component", "PrototechFrame"),
      new MyItemType("MyObjectBuilder_Component", "PrototechPanel"),
      new MyItemType("MyObjectBuilder_Component", "PrototechCapacitor"),
      new MyItemType("MyObjectBuilder_Component", "PrototechPropulsionUnit"),
      new MyItemType("MyObjectBuilder_Component", "PrototechMachinery"),
      new MyItemType("MyObjectBuilder_Component", "PrototechCircuitry"),
      new MyItemType("MyObjectBuilder_Component", "PrototechCoolingUnit"),
      new MyItemType("MyObjectBuilder_Component", "ZoneChip"),
      // new MyItemType("MyObjectBuilder_GasContainerObject", "HydrogenBottle"),
      new MyItemType("MyObjectBuilder_Ingot", "Stone"),
      new MyItemType("MyObjectBuilder_Ingot", "Iron"),
      new MyItemType("MyObjectBuilder_Ingot", "Nickel"),
      new MyItemType("MyObjectBuilder_Ingot", "Cobalt"),
      new MyItemType("MyObjectBuilder_Ingot", "Magnesium"),
      new MyItemType("MyObjectBuilder_Ingot", "Silicon"),
      new MyItemType("MyObjectBuilder_Ingot", "Silver"),
      new MyItemType("MyObjectBuilder_Ingot", "Gold"),
      new MyItemType("MyObjectBuilder_Ingot", "Platinum"),
      new MyItemType("MyObjectBuilder_Ingot", "Uranium"),
      new MyItemType("MyObjectBuilder_Ingot", "PrototechScrap"),
      new MyItemType("MyObjectBuilder_Ingot", "Scrap"),
      new MyItemType("MyObjectBuilder_Ore", "Stone"),
      new MyItemType("MyObjectBuilder_Ore", "Ice"),
      new MyItemType("MyObjectBuilder_Ore", "Iron"),
      new MyItemType("MyObjectBuilder_Ore", "Nickel"),
      new MyItemType("MyObjectBuilder_Ore", "Cobalt"),
      new MyItemType("MyObjectBuilder_Ore", "Magnesium"),
      new MyItemType("MyObjectBuilder_Ore", "Silicon"),
      new MyItemType("MyObjectBuilder_Ore", "Silver"),
      new MyItemType("MyObjectBuilder_Ore", "Gold"),
      new MyItemType("MyObjectBuilder_Ore", "Platinum"),
      new MyItemType("MyObjectBuilder_Ore", "Uranium"),
      new MyItemType("MyObjectBuilder_Ore", "Scrap"),
      new MyItemType("MyObjectBuilder_Ore", "Organic"),
      // new MyItemType("MyObjectBuilder_OxygenContainerObject", "OxygenBottle"),
      new MyItemType("MyObjectBuilder_PhysicalGunObject", "GoodAIRewardPunishmentTool"),
      new MyItemType("MyObjectBuilder_PhysicalGunObject", "WelderItem"),
      new MyItemType("MyObjectBuilder_PhysicalGunObject", "Welder2Item"),
      new MyItemType("MyObjectBuilder_PhysicalGunObject", "Welder3Item"),
      new MyItemType("MyObjectBuilder_PhysicalGunObject", "Welder4Item"),
      new MyItemType("MyObjectBuilder_PhysicalGunObject", "AngleGrinderItem"),
      new MyItemType("MyObjectBuilder_PhysicalGunObject", "AngleGrinder2Item"),
      new MyItemType("MyObjectBuilder_PhysicalGunObject", "AngleGrinder3Item"),
      new MyItemType("MyObjectBuilder_PhysicalGunObject", "AngleGrinder4Item"),
      new MyItemType("MyObjectBuilder_PhysicalGunObject", "HandDrillItem"),
      new MyItemType("MyObjectBuilder_PhysicalGunObject", "HandDrill2Item"),
      new MyItemType("MyObjectBuilder_PhysicalGunObject", "HandDrill3Item"),
      new MyItemType("MyObjectBuilder_PhysicalGunObject", "HandDrill4Item"),
      new MyItemType("MyObjectBuilder_PhysicalGunObject", "FlareGunItem"),
      new MyItemType("MyObjectBuilder_PhysicalGunObject", "SemiAutoPistolItem"),
      new MyItemType("MyObjectBuilder_PhysicalGunObject", "FullAutoPistolItem"),
      new MyItemType("MyObjectBuilder_PhysicalGunObject", "ElitePistolItem"),
      new MyItemType("MyObjectBuilder_PhysicalGunObject", "AutomaticRifleItem"),
      new MyItemType("MyObjectBuilder_PhysicalGunObject", "PreciseAutomaticRifleItem"),
      new MyItemType("MyObjectBuilder_PhysicalGunObject", "RapidFireAutomaticRifleItem"),
      new MyItemType("MyObjectBuilder_PhysicalGunObject", "UltimateAutomaticRifleItem"),
      new MyItemType("MyObjectBuilder_PhysicalGunObject", "BasicHandHeldLauncherItem"),
      new MyItemType("MyObjectBuilder_PhysicalGunObject", "AdvancedHandHeldLauncherItem"),
      new MyItemType("MyObjectBuilder_PhysicalGunObject", "CubePlacerItem"),
    ];

    foreach (var type in itemTypes)
    {
      var shortHand = $"{TYPES_SHORT[type.TypeId]}/{type.SubtypeId}";
      SUBTYPES_DIC.Add(shortHand, type);
    }
  }

  public static MyItemType FromShorthand(string subtypeId)
  {
    return SUBTYPES_DIC[subtypeId];
  }

  public static HashSet<MyItemType> FromShorthands(List<string> subtypeIds)
  {
    return [.. subtypeIds.Select(FromShorthand)];
  }

  /// <summary>
  /// returns the volume of one unit of the item
  /// </summary>
  /// <param name="type"></param>
  /// <returns>the volume, in m^3</returns>
  public static MyFixedPoint UnitaryVolume(this MyItemType type)
  {
    // TODO improve if necessary
    var res = new MyFixedPoint();
    if (type.TypeId.Contains("Ore") || type.TypeId.Contains("Ingot"))
    {
      // one Liter
      res.RawValue = 1000;
    }
    else
    {
      // 10 Liters
      res.RawValue = 10000;
    }

    return res;
  }

  /// <summary>
  /// returns the mass of one unit of the item
  /// </summary>
  /// <param name="type"></param>
  /// <returns>the mass, in kg</returns>
  public static MyFixedPoint UnitaryMass(this MyItemType type)
  {
    // TODO improve if necessary (this is only accurate for ingots and ores)
    return type.TypeId.Contains("Ore") || type.TypeId.Contains("Ingot") ? 1 : 10;
  }

  public static bool IsFractionable(this MyItemType type)
  {
    return type.TypeId.Contains("Ore") || type.TypeId.Contains("Ingot");
  }
}
