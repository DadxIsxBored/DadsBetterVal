using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace DadsBetterVal;

[HarmonyPatch(typeof(Player), nameof(Player.Repair))]
internal static class AreaRepair
{
    private static bool Prefix(Player __instance, ItemDrop.ItemData toolItem, ref bool __result)
    {
        if (!DadsBetterValPlugin.AreaRepairEnabled.Value || !__instance.InRepairMode()) return true;
        var origin = __instance.GetHoveringPiece();
        Vector3 point = origin ? origin.transform.position : __instance.transform.position;
        var pieces = new List<Piece>();
        foreach (Piece piece in Piece.s_allPieces)
            if (piece && piece.gameObject.layer != Piece.s_ghostLayer && Vector3.Distance(point, piece.transform.position) <= DadsBetterValPlugin.AreaRepairRadius.Value)
                pieces.Add(piece);
        int count = 0;
        foreach (Piece piece in pieces)
        {
            if (!__instance.InPlaceMode() || !__instance.CheckCanRemovePiece(piece) || !PrivateArea.CheckAccess(piece.transform.position, 0f, true, false)) continue;
            if (!__instance.HaveStamina(toolItem.m_shared.m_attack.m_attackStamina) || (toolItem.m_shared.m_useDurability && toolItem.m_durability <= 0f)) break;
            var wear = piece.GetComponent<WearNTear>();
            if (!wear || !wear.Repair()) continue;
            count++;
            __instance.UseStamina(toolItem.m_shared.m_attack.m_attackStamina);
            __instance.UseEitr(toolItem.m_shared.m_attack.m_attackEitr);
            if (toolItem.m_shared.m_useDurability) toolItem.m_durability -= toolItem.m_shared.m_useDurabilityDrain;
            piece.m_placeEffect.Create(piece.transform.position, piece.transform.rotation, null, 1f, -1);
        }
        __instance.Message(MessageHud.MessageType.TopLeft, $"{count} pieces repaired");
        __result = count > 0;
        return false;
    }
}

