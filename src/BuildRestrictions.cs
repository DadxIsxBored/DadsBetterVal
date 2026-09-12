using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace DadsBetterVal;

[HarmonyPatch(typeof(Location), nameof(Location.IsInsideNoBuildLocation))]
internal static class NoBuildZonePatch { private static void Postfix(ref bool __result) { if (DadsBetterValPlugin.BuildRestrictionsRemoved.Value) __result = false; } }

[HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
internal static class PlacementPatch
{
    private static readonly MethodInfo SetValid = AccessTools.Method(typeof(Player), "SetPlacementGhostValid", new[] { typeof(bool) });
    private static void Postfix(Player __instance, GameObject ___m_placementGhost, ref Player.PlacementStatus ___m_placementStatus)
    {
        if (!DadsBetterValPlugin.BuildRestrictionsRemoved.Value || !___m_placementGhost) return;
        ___m_placementStatus = Player.PlacementStatus.Valid;
        SetValid.Invoke(__instance, new object[] { true });
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
