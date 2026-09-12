using HarmonyLib;
using UnityEngine;

namespace DadsBetterVal;

[HarmonyPatch(typeof(Location), nameof(Location.IsInsideNoBuildLocation))]
internal static class NoBuildZonePatch { private static void Postfix(ref bool __result) { if (DadsBetterValPlugin.BuildRestrictionsRemoved.Value) __result = false; } }

[HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
internal static class PlacementPatch
{
    private static void Postfix(Player __instance)
    {
        if (!DadsBetterValPlugin.BuildRestrictionsRemoved.Value || !__instance.m_placementGhost) return;
        __instance.m_placementStatus = Player.PlacementStatus.Valid;
        __instance.SetPlacementGhostValid(true);
    }
}

[HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.HaveBuildStationInRange))]
internal static class StationRangePatch
{
    private static CraftingStation? _station;
    private static void Postfix(string name, ref CraftingStation __result)
    {
        if (!DadsBetterValPlugin.BuildRestrictionsRemoved.Value || __result) return;
        if (!_station)
        {
            var holder = new GameObject("DadsBetterVal unrestricted station") { hideFlags = HideFlags.HideAndDontSave };
            _station = holder.AddComponent<CraftingStation>();
        }
        _station.m_name = name;
        __result = _station;
    }
}
