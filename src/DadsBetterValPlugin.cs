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
    public const string Version = "1.2.11";
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
    internal static ConfigEntry<bool> BypassCropGrowingRestrictions = null!;
    internal static ConfigEntry<int> SmelterOre = null!, SmelterFuel = null!, BlastOre = null!, BlastFuel = null!, KilnInput = null!, FrostKilnInput = null!, WindmillInput = null!, SpinningInput = null!, EitrInput = null!, EitrFuel = null!, HoneyMax = null!, SapExtractorCapacity = null!, StoneOvenCapacity = null!;
    internal static ConfigEntry<int> GlobalSpeed = null!, SmelterSpeed = null!, BlastSpeed = null!, KilnSpeed = null!, FrostKilnSpeed = null!, WindmillSpeed = null!, SpinningSpeed = null!, EitrSpeed = null!, FermenterSpeed = null!, HoneySpeed = null!, SapExtractorSpeed = null!, StoneOvenSpeed = null!;
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
        BypassCropGrowingRestrictions = Config.Bind("Build Restrictions", "Bypass Crop Growing Restrictions", false, "Allow cultivated crops to grow regardless of biome, cultivated ground, roof, spacing, or temperature.");
        SmelterOre = Capacity("Smelter Ore", 10); SmelterFuel = Capacity("Smelter Fuel", 20);
        BlastOre = Capacity("Blast Furnace Ore", 10); BlastFuel = Capacity("Blast Furnace Fuel", 20);
        KilnInput = Capacity("Charcoal Kiln Input", 0); FrostKilnInput = Capacity("Frost Kiln Input", 0); WindmillInput = Capacity("Windmill Input", 0);
        SpinningInput = Capacity("Spinning Wheel Input", 0); EitrInput = Capacity("Eitr Refinery Input", 0); EitrFuel = Capacity("Eitr Refinery Fuel", 0); HoneyMax = Capacity("Beehive Honey", 4);
        SapExtractorCapacity = Capacity("Sap Extractor Capacity", 0);
        StoneOvenCapacity = Config.Bind("Smelting - Capacity", "Stone Oven Capacity", 0, new ConfigDescription("Total stone oven cooking slots. Zero preserves the game value; values below the game value keep the existing slots.", new AcceptableValueRange<int>(0, 20)));
        GlobalSpeed = Speed("Global Multiplier"); SmelterSpeed = Speed("Smelter Multiplier"); BlastSpeed = Speed("Blast Furnace Multiplier"); KilnSpeed = Speed("Charcoal Kiln Multiplier"); FrostKilnSpeed = Speed("Frost Kiln Multiplier"); WindmillSpeed = Speed("Windmill Multiplier"); SpinningSpeed = Speed("Spinning Wheel Multiplier"); EitrSpeed = Speed("Eitr Refinery Multiplier"); FermenterSpeed = Speed("Fermenter Multiplier"); HoneySpeed = Speed("Beehive Multiplier");
        SapExtractorSpeed = Speed("Sap Extractor Multiplier");
        StoneOvenSpeed = Speed("Stone Oven Multiplier");
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
    private ConfigEntry<int> Capacity(string key, int value) => Config.Bind("Smelting - Capacity", key, value, new ConfigDescription("Zero preserves the game value.", new AcceptableValueRange<int>(0, 1000)));
    private ConfigEntry<int> Speed(string key)
    {
        string description = key == "Global Multiplier"
            ? "Whole-number speed multiplier for smelters, blast furnaces, kilns, windmills, spinning wheels, and eitr refineries. Does not affect stone ovens."
            : key == "Stone Oven Multiplier"
                ? "Whole-number stone oven speed multiplier, independent of Global Multiplier. One is native cooking speed."
                : "Whole-number processing speed multiplier. Smelters, blast furnaces, kilns, windmills, spinning wheels, and eitr refineries also use Global Multiplier.";
        return Config.Bind("Smelting - Speed", key, 1, new ConfigDescription(description, new AcceptableValueRange<int>(1, 100)));
    }
    private void OnDestroy() { _harmony?.UnpatchSelf(); }
}
