using HarmonyLib;

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
