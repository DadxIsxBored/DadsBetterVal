using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace DadsBetterVal;

[BepInPlugin(Guid, Name, Version)]
public sealed class DadsBetterValPlugin : BaseUnityPlugin
{
    public const string Guid = "com.dadisbored.dadsbetterval";
    public const string Name = "DadsBetterVal";
    public const string Version = "1.2.1";
    internal static DadsBetterValPlugin Instance = null!;
    internal static ConfigEntry<bool> AreaRepairEnabled = null!;
    internal static ConfigEntry<float> AreaRepairRadius = null!;
    internal static ConfigEntry<bool> RecycleEnabled = null!;
    internal static ConfigEntry<KeyCode> RecycleKey = null!;
    internal static ConfigEntry<float> RecycleRate = null!;
    internal static ConfigEntry<bool> ClockEnabled = null!;
    internal static ConfigEntry<string> ClockFormat = null!;
    internal static ConfigEntry<int> ClockFontSize = null!;
    internal static ConfigEntry<bool> BuildRestrictionsRemoved = null!;
    internal static ConfigEntry<int> SmelterOre = null!, SmelterFuel = null!, BlastOre = null!, BlastFuel = null!, KilnInput = null!, WindmillInput = null!, SpinningInput = null!, EitrInput = null!, EitrFuel = null!, HoneyMax = null!;
    internal static ConfigEntry<float> GlobalSpeed = null!, SmelterSpeed = null!, BlastSpeed = null!, KilnSpeed = null!, WindmillSpeed = null!, SpinningSpeed = null!, EitrSpeed = null!, FermenterSpeed = null!, HoneySpeed = null!;
    internal static ConfigEntry<bool> AllOresInBlast = null!, WindmillIgnoresWind = null!;
    internal static ConfigEntry<bool> QuickInsert = null!, AlternativeFuel = null!;
    internal static ConfigEntry<KeyCode> QuickInsertKey = null!;
    internal static ConfigEntry<string> SmelterFuelItems = null!, BlastFuelItems = null!;
    internal static ConfigEntry<int> WoodFuel = null!, FineWoodFuel = null!, CoreWoodFuel = null!, SurtlingCoreFuel = null!, BlackCoreFuel = null!;

    private Harmony _harmony = null!;
    private void Awake()
    {
        Instance = this;
        AreaRepairEnabled = Config.Bind("Area Repair", "Enabled", true, "Repair every valid placed piece within the configured radius.");
        AreaRepairRadius = Config.Bind("Area Repair", "Radius", 15f, new ConfigDescription("Repair radius in metres.", new AcceptableValueRange<float>(1f, 100f)));
        RecycleEnabled = Config.Bind("Recycle and Reclaim", "Enabled", true, "Recycle the inventory item under the pointer.");
        RecycleKey = Config.Bind("Recycle and Reclaim", "Recycle Key", KeyCode.Delete, "Recycle the inventory item under the pointer while inventory is open.");
        RecycleRate = Config.Bind("Recycle and Reclaim", "Return Rate", 0.5f, new ConfigDescription("Fraction of recipe materials returned.", new AcceptableValueRange<float>(0f, 1f)));
        ClockEnabled = Config.Bind("Clock", "Enabled", true, "Show the in-game clock.");
        ClockFormat = Config.Bind("Clock", "Format", "hh:mm tt", "Clock format: HH, hh, mm, ss, tt, DAY.");
        ClockFontSize = Config.Bind("Clock", "Font Size", 24, new ConfigDescription("Clock text size.", new AcceptableValueRange<int>(10, 72)));
        BuildRestrictionsRemoved = Config.Bind("Build Restrictions", "Remove All Restrictions", true, "Make placement valid in restricted locations and conditions.");
        SmelterOre = Int("Smelting - Capacity", "Smelter Ore", 10); SmelterFuel = Int("Smelting - Capacity", "Smelter Fuel", 20);
        BlastOre = Int("Smelting - Capacity", "Blast Furnace Ore", 10); BlastFuel = Int("Smelting - Capacity", "Blast Furnace Fuel", 20);
        KilnInput = Int("Smelting - Capacity", "Charcoal Kiln Input", 0); WindmillInput = Int("Smelting - Capacity", "Windmill Input", 0);
        SpinningInput = Int("Smelting - Capacity", "Spinning Wheel Input", 0); EitrInput = Int("Smelting - Capacity", "Eitr Refinery Input", 0); EitrFuel = Int("Smelting - Capacity", "Eitr Refinery Fuel", 0); HoneyMax = Int("Smelting - Capacity", "Beehive Honey", 4);
        GlobalSpeed = Float("Smelting - Speed", "Global Multiplier", 1f); SmelterSpeed = Float("Smelting - Speed", "Smelter Multiplier", 1f); BlastSpeed = Float("Smelting - Speed", "Blast Furnace Multiplier", 1f); KilnSpeed = Float("Smelting - Speed", "Charcoal Kiln Multiplier", 1f); WindmillSpeed = Float("Smelting - Speed", "Windmill Multiplier", 1f); SpinningSpeed = Float("Smelting - Speed", "Spinning Wheel Multiplier", 1f); EitrSpeed = Float("Smelting - Speed", "Eitr Refinery Multiplier", 1f); FermenterSpeed = Float("Smelting - Speed", "Fermenter Multiplier", 1f); HoneySpeed = Float("Smelting - Speed", "Beehive Multiplier", 1f);
        AllOresInBlast = Config.Bind("Smelting", "All Ores In Blast Furnace", true, "Allow normal smelter conversions in the blast furnace.");
        WindmillIgnoresWind = Config.Bind("Smelting", "Windmill Ignores Wind", false, "Run windmills at full power.");
        QuickInsert = Config.Bind("Smelting", "Quick Insert", true, "Hold the configured key while using a machine to fill it.");
        QuickInsertKey = Config.Bind("Smelting", "Quick Insert Key", KeyCode.LeftShift, "Modifier held while using a machine.");
        AlternativeFuel = Config.Bind("Smelting - Alternative Fuel", "Enabled", true, "Accept configured prefab or localized item names as furnace fuel.");
        SmelterFuelItems = Config.Bind("Smelting - Alternative Fuel", "Smelter Fuel Items", "Coal,Wood,FineWood,RoundLog,SurtlingCore,BlackCore", "Comma-separated acceptable prefab or localized item names.");
        BlastFuelItems = Config.Bind("Smelting - Alternative Fuel", "Blast Furnace Fuel Items", "Coal,Wood,FineWood,RoundLog,SurtlingCore,BlackCore", "Comma-separated acceptable prefab or localized item names.");
        WoodFuel = Int("Smelting - Alternative Fuel", "Wood Fuel Value", 1); FineWoodFuel = Int("Smelting - Alternative Fuel", "Fine Wood Fuel Value", 2); CoreWoodFuel = Int("Smelting - Alternative Fuel", "Core Wood Fuel Value", 3); SurtlingCoreFuel = Int("Smelting - Alternative Fuel", "Surtling Core Fuel Value", 5); BlackCoreFuel = Int("Smelting - Alternative Fuel", "Black Core Fuel Value", 10);
        _harmony = new Harmony(Guid); _harmony.PatchAll();
    }
    private ConfigEntry<int> Int(string section, string key, int value) => Config.Bind(section, key, value, new ConfigDescription("Zero preserves the game value.", new AcceptableValueRange<int>(0, 10000)));
    private ConfigEntry<float> Float(string section, string key, float value) => Config.Bind(section, key, value, new ConfigDescription("Processing speed multiplier.", new AcceptableValueRange<float>(0.01f, 100f)));
    private void OnDestroy() { _harmony?.UnpatchSelf(); }
}
