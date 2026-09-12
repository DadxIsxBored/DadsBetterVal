using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DadsBetterVal;

[HarmonyPatch(typeof(InventoryGui), "Update")]
internal static class Recycling
{
    internal static bool ReclaimMode;
    internal static Button? ReclaimButton;
    private static readonly System.Reflection.MethodInfo UpdateCrafting = AccessTools.Method(typeof(InventoryGui), "UpdateCraftingPanel", new[] { typeof(bool) });
    private static readonly System.Reflection.MethodInfo AddRecipe = AccessTools.Method(typeof(InventoryGui), "AddRecipeToList");
    private static readonly System.Reflection.MethodInfo SelectRecipe = AccessTools.Method(typeof(InventoryGui), "SetRecipe");
    private static readonly System.Reflection.MethodInfo UpdateRecipeMethod = AccessTools.Method(typeof(InventoryGui), "UpdateRecipe");
    private static readonly System.Reflection.MethodInfo HoveredElement = AccessTools.Method(typeof(InventoryGrid), "GetHoveredElement");
    private static readonly System.Reflection.MethodInfo ElementPosition = AccessTools.Method(typeof(InventoryGrid), "GetElementPos");
    internal static void Refresh(InventoryGui gui, bool focus = true) => UpdateCrafting.Invoke(gui, new object[] { focus });
    internal static void AddReclaimRecipe(InventoryGui gui, Player player, Recipe recipe, ItemDrop.ItemData item) => AddRecipe.Invoke(gui, new object[] { player, recipe, item, true });
    internal static void Select(InventoryGui gui, int index) => SelectRecipe.Invoke(gui, new object[] { index, false });
    internal static void DrawRecipe(InventoryGui gui, Player player) => UpdateRecipeMethod.Invoke(gui, new object[] { player, 0f });
    private static void Postfix(InventoryGui __instance)
    {
        if (!DadsBetterValPlugin.RecycleEnabled.Value || !InventoryGui.IsVisible() || !Input.GetKeyDown(DadsBetterValPlugin.RecycleKey.Value)) return;
        InventoryGrid? grid = __instance.m_playerGrid;
        if (grid == null) return;
        InventoryElement? element = (InventoryElement?)HoveredElement.Invoke(grid, Array.Empty<object>());
        if (element == null) return;
        Vector2i pos = (Vector2i)ElementPosition.Invoke(grid, new object[] { element });
        ItemDrop.ItemData item = grid.GetInventory().GetItemAt(pos.x, pos.y);
        if (item == null || item.m_equipped) return;
        Recycle(item, grid.GetInventory());
    }

    internal static bool Recycle(ItemDrop.ItemData source, Inventory inventory)
    {
        Recipe? recipe = ObjectDB.instance?.GetRecipe(source);
        if (recipe == null || recipe.m_resources == null || recipe.m_resources.Length == 0)
        {
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "No reclaim recipe"); return false;
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
        if (returns.Count == 0) { Player.m_localPlayer.Message(MessageHud.MessageType.Center, "No materials returned"); return false; }

        // Capacity is tested against a clone before the source item is touched.
        Inventory test = new Inventory("DadsBetterVal preflight", null, inventory.GetWidth(), inventory.GetHeight());
        foreach (ItemDrop.ItemData existing in inventory.GetAllItems())
        {
            ItemDrop.ItemData copy = existing.Clone();
            copy.m_stack = existing.m_stack;
            if (!test.AddItem(copy, copy.m_gridPos)) return false;
        }
        ItemDrop.ItemData testSource = test.GetItemAt(source.m_gridPos.x, source.m_gridPos.y);
        if (testSource == null || !test.RemoveItem(testSource)) return false;
        foreach (var entry in returns)
            if (!test.AddItem(entry.prefab, entry.amount)) { Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$inventory_full"); return false; }

        // Commit: source removal must succeed before any return material is inserted.
        if (!inventory.RemoveItem(source)) return false;
        foreach (var entry in returns)
        {
            if (!inventory.AddItem(entry.prefab, entry.amount))
            {
                // The preflight makes this branch exceptional; drop any remainder without duplicating the source.
                ItemDrop.DropItem(entry.prefab.GetComponent<ItemDrop>().m_itemData, entry.amount, Player.m_localPlayer.transform.position + Vector3.up, Quaternion.identity);
            }
        }
        Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Item reclaimed");
        return true;
    }
}

[HarmonyPatch(typeof(InventoryGui), "Awake")]
internal static class ReclaimTabSetup
{
    private static void Postfix(InventoryGui __instance)
    {
        if (!DadsBetterValPlugin.RecycleEnabled.Value || GameObject.Find("DadsBetterVal_ReclaimTab")) return;
        GameObject tab = UnityEngine.Object.Instantiate(__instance.m_tabUpgrade.gameObject, __instance.m_tabUpgrade.transform.parent);
        tab.name = "DadsBetterVal_ReclaimTab";
        RectTransform rect = (RectTransform)tab.transform;
        RectTransform source = (RectTransform)__instance.m_tabUpgrade.transform;
        rect.anchoredPosition = source.anchoredPosition + new Vector2(source.rect.width + 8f, 0f);
        TMP_Text label = tab.GetComponentInChildren<TMP_Text>(); if (label) label.text = "Reclaim";
        Button button = tab.GetComponent<Button>(); Recycling.ReclaimButton = button; button.onClick.RemoveAllListeners();
        button.onClick.AddListener(new UnityAction(() => { Recycling.ReclaimMode = true; button.interactable = false; Recycling.Refresh(__instance); }));
    }
}

[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateCraftingPanel))]
internal static class ReclaimListPatch
{
    private static void Postfix(InventoryGui __instance, List<InventoryGui.RecipeDataPair> ___m_availableRecipes)
    {
        if (!Recycling.ReclaimMode || !DadsBetterValPlugin.RecycleEnabled.Value || Player.m_localPlayer == null) return;
        foreach (InventoryGui.RecipeDataPair pair in ___m_availableRecipes)
            if (pair.InterfaceElement) UnityEngine.Object.Destroy(pair.InterfaceElement);
        ___m_availableRecipes.Clear();
        foreach (ItemDrop.ItemData item in Player.m_localPlayer.GetInventory().GetAllItems())
        {
            if (item.m_equipped) continue;
            Recipe recipe = ObjectDB.instance.GetRecipe(item);
            if (recipe != null && recipe.m_resources != null && recipe.m_resources.Length > 0)
                Recycling.AddReclaimRecipe(__instance, Player.m_localPlayer, recipe, item);
        }
        Recycling.Select(__instance, ___m_availableRecipes.Count > 0 ? 0 : -1);
        Recycling.DrawRecipe(__instance, Player.m_localPlayer);
    }
}

[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
internal static class ReclaimDetailsPatch
{
    private static void Postfix(InventoryGui __instance, InventoryGui.RecipeDataPair ___m_selectedRecipe)
    {
        if (!Recycling.ReclaimMode) return;
        ItemDrop.ItemData item = ___m_selectedRecipe.ItemData;
        if (item == null) return;
        __instance.m_recipeIcon.sprite = item.GetIcon();
        __instance.m_recipeName.text = "Reclaim " + Localization.instance.Localize(item.m_shared.m_name);
        __instance.m_recipeDecription.text = Localization.instance.Localize(ItemDrop.ItemData.GetTooltip(item, item.m_quality, false, Game.m_worldLevel)) + "\n\nReturns configured recipe materials.";
        TMP_Text label = __instance.m_craftButton.GetComponentInChildren<TMP_Text>(); if (label) label.text = "Reclaim";
        __instance.m_craftButton.interactable = true;
        foreach (GameObject requirement in __instance.m_recipeRequirementList) requirement.SetActive(false);
    }
}

[HarmonyPatch(typeof(InventoryGui), "OnCraftPressed")]
internal static class ReclaimActionPatch
{
    private static bool Prefix(InventoryGui __instance, InventoryGui.RecipeDataPair ___m_selectedRecipe)
    {
        if (!Recycling.ReclaimMode) return true;
        ItemDrop.ItemData item = ___m_selectedRecipe.ItemData;
        if (item != null && Recycling.Recycle(item, Player.m_localPlayer.GetInventory())) Recycling.Refresh(__instance);
        return false;
    }
}

[HarmonyPatch(typeof(InventoryGui), "OnTabCraftPressed")]
internal static class ReclaimCraftTabPatch { private static void Prefix() { Recycling.ReclaimMode = false; if (Recycling.ReclaimButton) Recycling.ReclaimButton.interactable = true; } }
[HarmonyPatch(typeof(InventoryGui), "OnTabUpgradePressed")]
internal static class ReclaimUpgradeTabPatch { private static void Prefix() { Recycling.ReclaimMode = false; if (Recycling.ReclaimButton) Recycling.ReclaimButton.interactable = true; } }
