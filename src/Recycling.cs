using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace DadsBetterVal;

[HarmonyPatch(typeof(InventoryGui), "Update")]
internal static class Recycling
{
    private static void Postfix(InventoryGui __instance)
    {
        if (!DadsBetterValPlugin.RecycleEnabled.Value || !InventoryGui.IsVisible() || !Input.GetKeyDown(DadsBetterValPlugin.RecycleKey.Value)) return;
        InventoryGrid grid = __instance.m_playerGrid;
        InventoryElement element = grid?.GetHoveredElement();
        if (element == null) return;
        Vector2i pos = grid.GetElementPos(element);
        ItemDrop.ItemData item = grid.GetInventory().GetItemAt(pos.x, pos.y);
        if (item == null || item.m_equipped) return;
        Recycle(item, grid.GetInventory());
    }

    private static void Recycle(ItemDrop.ItemData source, Inventory inventory)
    {
        Recipe recipe = ObjectDB.instance?.GetRecipe(source);
        if (recipe == null || recipe.m_resources == null || recipe.m_resources.Length == 0)
        {
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "No reclaim recipe"); return;
        }
        var returns = new List<(GameObject prefab, int amount)>();
        foreach (Piece.Requirement req in recipe.m_resources)
        {
            if (!req.m_resItem) continue;
            int total = 0;
            for (int quality = 1; quality <= Math.Max(1, source.m_quality); quality++) total += req.GetAmount(quality);
            int amount = Mathf.FloorToInt(total * DadsBetterValPlugin.RecycleRate.Value) * source.m_stack;
            if (amount > 0) returns.Add((req.m_resItem.gameObject, amount));
        }
        if (returns.Count == 0) { Player.m_localPlayer.Message(MessageHud.MessageType.Center, "No materials returned"); return; }

        // Capacity is tested against a clone before the source item is touched.
        Inventory test = new Inventory("DadsBetterVal preflight", null, inventory.GetWidth(), inventory.GetHeight());
        foreach (ItemDrop.ItemData existing in inventory.GetAllItems())
        {
            ItemDrop.ItemData copy = existing.Clone();
            copy.m_stack = existing.m_stack;
            if (!test.AddItem(copy, copy.m_stack, copy.m_gridPos.x, copy.m_gridPos.y, true)) return;
        }
        ItemDrop.ItemData testSource = test.GetItemAt(source.m_gridPos.x, source.m_gridPos.y);
        if (testSource == null || !test.RemoveItem(testSource)) return;
        foreach (var entry in returns)
            if (!test.AddItem(entry.prefab, entry.amount)) { Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$inventory_full"); return; }

        // Commit: source removal must succeed before any return material is inserted.
        if (!inventory.RemoveItem(source)) return;
        foreach (var entry in returns)
        {
            if (!inventory.AddItem(entry.prefab, entry.amount))
            {
                // The preflight makes this branch exceptional; drop any remainder without duplicating the source.
                ItemDrop.DropItem(entry.prefab.GetComponent<ItemDrop>().m_itemData, entry.amount, Player.m_localPlayer.transform.position + Vector3.up, Quaternion.identity);
            }
        }
        inventory.Changed();
        Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Item reclaimed");
    }
}
