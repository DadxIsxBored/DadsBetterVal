using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace DadsBetterVal;

[HarmonyPatch(typeof(Smelter))]
internal static class BetterSmelting
{
    private static string Prefab(Smelter s) => Utils.GetPrefabName(s.gameObject).ToLowerInvariant();
    private static bool Blast(Smelter s) => Prefab(s).Contains("blastfurnace");
    private static bool AlternativeFuelMachine(Smelter s)
    {
        string name = Prefab(s);
        return name.Contains("smelter") || name.Contains("blastfurnace");
    }
    private static int QueueSize(ZNetView view) => view != null && view.IsValid() ? view.GetZDO().GetInt(ZDOVars.s_queued) : 0;
    private static float FuelAmount(ZNetView view) => view != null && view.IsValid() ? view.GetZDO().GetFloat(ZDOVars.s_fuel) : 0f;
    private static ItemDrop.ItemData? Cookable(Smelter smelter, Inventory inventory)
    {
        foreach (Smelter.ItemConversion conversion in smelter.m_conversion)
        {
            if (!conversion?.m_from) continue;
            ItemDrop.ItemData item = inventory.GetItem(conversion.m_from.m_itemData.m_shared.m_name, -1, false);
            if (item != null) return item;
        }
        return null;
    }
    private static bool FuelMatches(Smelter s, ItemDrop.ItemData? item)
    {
        if (!DadsBetterValPlugin.AlternativeFuel.Value || item == null) return false;
        string list = Blast(s) ? DadsBetterValPlugin.BlastFuelItems.Value : DadsBetterValPlugin.SmelterFuelItems.Value;
        string prefab = item.m_dropPrefab ? item.m_dropPrefab.name : "";
        foreach (string raw in list.Split(','))
        {
            string value = raw.Trim();
            if (value.Equals(prefab, StringComparison.OrdinalIgnoreCase) || value.Equals(item.m_shared.m_name, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
    private static int FuelValue(ItemDrop.ItemData item)
    {
        string name = (item.m_dropPrefab ? item.m_dropPrefab.name : item.m_shared.m_name).ToLowerInvariant();
        if (name.Contains("blackcore") || name.Contains("black_core")) return DadsBetterValPlugin.BlackCoreFuel.Value;
        if (name.Contains("surtling")) return DadsBetterValPlugin.SurtlingCoreFuel.Value;
        if (name.Contains("finewood") || name.Contains("fine_wood")) return DadsBetterValPlugin.FineWoodFuel.Value;
        if (name.Contains("roundlog") || name.Contains("corewood") || name.Contains("core_wood")) return DadsBetterValPlugin.CoreWoodFuel.Value;
        if (name.Contains("wood")) return DadsBetterValPlugin.WoodFuel.Value;
        return 1;
    }

    [HarmonyPatch("Awake"), HarmonyPostfix]
    private static void AwakePostfix(Smelter __instance)
    {
        string name = Prefab(__instance);
        if (name == "charcoal_kiln") SetOre(__instance, DadsBetterValPlugin.KilnInput.Value);
        else if (__instance.m_windmill) SetOre(__instance, DadsBetterValPlugin.WindmillInput.Value);
        else if (name == "piece_spinningwheel") SetOre(__instance, DadsBetterValPlugin.SpinningInput.Value);
        else if (name == "eitrrefinery") { SetOre(__instance, DadsBetterValPlugin.EitrInput.Value); SetFuel(__instance, DadsBetterValPlugin.EitrFuel.Value); }
        else if (Blast(__instance)) { SetOre(__instance, DadsBetterValPlugin.BlastOre.Value); SetFuel(__instance, DadsBetterValPlugin.BlastFuel.Value); AddNormalConversions(__instance); }
        else if (name.Contains("smelter")) { SetOre(__instance, DadsBetterValPlugin.SmelterOre.Value); SetFuel(__instance, DadsBetterValPlugin.SmelterFuel.Value); }
    }

    private static void SetOre(Smelter s, int value) { if (value > 0) s.m_maxOre = value; }
    private static void SetFuel(Smelter s, int value) { if (value > 0) s.m_maxFuel = value; }
    private static void AddNormalConversions(Smelter blast)
    {
        if (!DadsBetterValPlugin.AllOresInBlast.Value) return;
        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Smelter.ItemConversion conversion in blast.m_conversion) if (conversion?.m_from) known.Add(conversion.m_from.m_itemData.m_shared.m_name);
        foreach (Smelter smelter in Resources.FindObjectsOfTypeAll<Smelter>())
        {
            string name = Prefab(smelter);
            if (!name.Contains("smelter") || Blast(smelter)) continue;
            foreach (Smelter.ItemConversion conversion in smelter.m_conversion)
                if (conversion?.m_from && known.Add(conversion.m_from.m_itemData.m_shared.m_name)) blast.m_conversion.Add(conversion);
            break;
        }
    }

    [HarmonyPatch("GetDeltaTime"), HarmonyPostfix]
    private static void DeltaPostfix(Smelter __instance, ref double __result)
    {
        string name = Prefab(__instance); float multiplier = DadsBetterValPlugin.GlobalSpeed.Value;
        if (name == "charcoal_kiln") multiplier *= DadsBetterValPlugin.KilnSpeed.Value;
        else if (__instance.m_windmill) multiplier *= DadsBetterValPlugin.WindmillSpeed.Value;
        else if (name == "piece_spinningwheel") multiplier *= DadsBetterValPlugin.SpinningSpeed.Value;
        else if (name == "eitrrefinery") multiplier *= DadsBetterValPlugin.EitrSpeed.Value;
        else if (Blast(__instance)) multiplier *= DadsBetterValPlugin.BlastSpeed.Value;
        else if (name.Contains("smelter")) multiplier *= DadsBetterValPlugin.SmelterSpeed.Value;
        __result *= multiplier;
    }

    [HarmonyPatch("OnAddOre"), HarmonyPrefix]
    private static bool AddOrePrefix(Smelter __instance, Humanoid user, ZNetView ___m_nview, ref bool __result)
    {
        if (!DadsBetterValPlugin.QuickInsert.Value || !Input.GetKey(DadsBetterValPlugin.QuickInsertKey.Value)) return true;
        int inserted = 0; Inventory inventory = user.GetInventory();
        int remaining = Math.Max(0, __instance.m_maxOre - QueueSize(___m_nview));
        while (remaining-- > 0)
        {
            ItemDrop.ItemData? item = Cookable(__instance, inventory);
            if (item == null || !inventory.RemoveItem(item, 1)) break;
            ___m_nview.InvokeRPC("RPC_AddOre", item.m_dropPrefab.name, item.m_cheated);
            inserted++;
        }
        if (inserted == 0) return true;
        user.Message(MessageHud.MessageType.Center, $"Added {inserted}"); __result = true; return false;
    }

    [HarmonyPatch("OnAddFuel"), HarmonyPrefix]
    private static bool AddFuelPrefix(Smelter __instance, Humanoid user, ItemDrop.ItemData? item, ZNetView ___m_nview, ref bool __result)
    {
        bool quick = DadsBetterValPlugin.QuickInsert.Value && Input.GetKey(DadsBetterValPlugin.QuickInsertKey.Value);
        bool alternative = DadsBetterValPlugin.AlternativeFuel.Value && AlternativeFuelMachine(__instance);
        if (!quick && !alternative) return true;
        Inventory inventory = user.GetInventory(); int inserted = 0;
        int remaining = Math.Max(0, __instance.m_maxFuel - Mathf.CeilToInt(FuelAmount(___m_nview)));
        while (remaining-- > 0)
        {
            ItemDrop.ItemData? fuel = item;
            if (fuel != null && fuel.m_shared.m_name != __instance.m_fuelItem.m_itemData.m_shared.m_name && !FuelMatches(__instance, fuel)) fuel = null;
            if (fuel == null || !inventory.ContainsItem(fuel))
            {
                fuel = null;
                foreach (ItemDrop.ItemData candidate in inventory.GetAllItems())
                    if (candidate.m_shared.m_name == __instance.m_fuelItem.m_itemData.m_shared.m_name || FuelMatches(__instance, candidate)) { fuel = candidate; break; }
            }
            if (fuel == null || (!FuelMatches(__instance, fuel) && fuel.m_shared.m_name != __instance.m_fuelItem.m_itemData.m_shared.m_name) || !inventory.RemoveItem(fuel, 1)) break;
            int fuelValue = FuelMatches(__instance, fuel) ? Math.Max(1, FuelValue(fuel)) : 1;
            int applied = Math.Min(fuelValue, remaining + 1);
            for (int i = 0; i < applied; i++) ___m_nview.InvokeRPC("RPC_AddFuel");
            inserted += applied;
            remaining -= applied - 1;
            if (!quick) break;
            item = null;
        }
        if (inserted == 0) return true;
        user.Message(MessageHud.MessageType.Center, $"Added {inserted} fuel"); __result = true; return false;
    }
}

[HarmonyPatch(typeof(Beehive), "Awake")]
internal static class BeehivePatch
{
    private static void Postfix(Beehive __instance)
    {
        if (DadsBetterValPlugin.HoneyMax.Value > 0) __instance.m_maxHoney = DadsBetterValPlugin.HoneyMax.Value;
        __instance.m_secPerUnit /= DadsBetterValPlugin.HoneySpeed.Value;
    }
}

[HarmonyPatch(typeof(Fermenter), "Awake")]
internal static class FermenterPatch { private static void Postfix(Fermenter __instance) => __instance.m_fermentationDuration /= DadsBetterValPlugin.FermenterSpeed.Value; }

[HarmonyPatch(typeof(Windmill), "GetPowerOutput")]
internal static class WindmillPatch { private static void Postfix(ref float __result) { if (DadsBetterValPlugin.WindmillIgnoresWind.Value) __result = 1f; } }
