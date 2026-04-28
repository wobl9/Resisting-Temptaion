using System;
using ShatteredForge.Core;
using ShatteredForge.Items;
using UnityEngine;

namespace ShatteredForge.SceneFlow
{
    /// <summary>IMGUI for camp shop and crafting stations (forge / alchemy).</summary>
    internal static class CampHubEconomyDraw
    {
        internal enum EconomyPanelKind
        {
            None,
            Shop,
            Forge,
            Alchemy
        }

        private static Vector2 _shopScroll;
        private static Vector2 _craftScroll;
        private static string _craftStatus = string.Empty;

        internal static bool DrawEconomyPanel(
            EconomyPanelKind kind,
            AccountState account,
            ItemCatalog catalog,
            VendorCatalog vendor,
            CraftingRecipeCatalog recipeBook,
            Action onMutatedPersist)
        {
            if (kind == EconomyPanelKind.None || account == null)
            {
                return false;
            }

            const float w = 520f;
            const float h = 420f;
            var r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.Box(r, GUIContent.none);
            GUILayout.BeginArea(new Rect(r.x + 14f, r.y + 12f, w - 28f, h - 24f));

            var title = kind switch
            {
                EconomyPanelKind.Shop => "Торговец",
                EconomyPanelKind.Forge => "Кузница",
                EconomyPanelKind.Alchemy => "Алхимическая лаборатория",
                _ => ""
            };

            GUILayout.Label(title, new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold });
            GUILayout.Label($"Золото: {account.gold}", GUI.skin.label);
            GUILayout.Space(6f);

            switch (kind)
            {
                case EconomyPanelKind.Shop:
                    DrawShop(account, catalog, vendor, onMutatedPersist);
                    break;
                case EconomyPanelKind.Forge:
                    DrawCrafting(account, catalog, recipeBook, CraftingStation.Forge, onMutatedPersist);
                    break;
                case EconomyPanelKind.Alchemy:
                    DrawCrafting(account, catalog, recipeBook, CraftingStation.Alchemy, onMutatedPersist);
                    break;
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Закрыть (E)", GUILayout.Height(30f)))
            {
                GUILayout.EndArea();
                return true;
            }

            GUILayout.EndArea();
            return false;
        }

        private static void DrawShop(
            AccountState account,
            ItemCatalog catalog,
            VendorCatalog vendor,
            Action onMutatedPersist)
        {
            if (vendor == null || vendor.offers == null || vendor.offers.Count == 0)
            {
                GUILayout.Label("Ассортимент не настроен.");
                return;
            }

            _shopScroll = GUILayout.BeginScrollView(_shopScroll, GUILayout.ExpandHeight(true));
            foreach (var offer in vendor.offers)
            {
                if (offer == null || string.IsNullOrWhiteSpace(offer.templateId))
                {
                    continue;
                }

                var tid = offer.templateId.Trim();
                var price = vendor.ResolvePrice(offer, catalog);
                var name = catalog != null && catalog.TryGet(tid, out var e) && !string.IsNullOrEmpty(e.displayNameRu)
                    ? e.displayNameRu
                    : tid;

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{name} — {price} зол.", GUILayout.ExpandWidth(true));
                GUI.enabled = price > 0 && account.gold >= price;
                if (GUILayout.Button("Купить", GUILayout.Width(90f)))
                {
                    account.gold -= price;
                    account.stash.Add(new ItemInstance
                    {
                        id = Guid.NewGuid().ToString(),
                        templateId = tid,
                        rarity = "Обычная",
                        enhanceLevel = 0
                    });
                    CharacterStatsService.RecalculateForCamp(account);
                    onMutatedPersist?.Invoke();
                }

                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

        private static void DrawCrafting(
            AccountState account,
            ItemCatalog catalog,
            CraftingRecipeCatalog book,
            CraftingStation stationFilter,
            Action onMutatedPersist)
        {
            if (book == null || book.recipes == null || book.recipes.Count == 0)
            {
                GUILayout.Label("Рецепты не настроены.");
                return;
            }

            if (!string.IsNullOrEmpty(_craftStatus))
            {
                GUILayout.Label(_craftStatus, GUI.skin.label);
                GUILayout.Space(4f);
            }

            _craftScroll = GUILayout.BeginScrollView(_craftScroll, GUILayout.ExpandHeight(true));
            foreach (var recipe in book.recipes)
            {
                if (recipe == null || string.IsNullOrWhiteSpace(recipe.recipeId))
                {
                    continue;
                }

                if (!RecipeMatchesStation(recipe, stationFilter))
                {
                    continue;
                }

                var outName = catalog != null && catalog.TryGet(recipe.outputTemplateId, out var oe) &&
                              !string.IsNullOrEmpty(oe.displayNameRu)
                    ? oe.displayNameRu
                    : recipe.outputTemplateId;

                GUILayout.Label(outName, new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
                foreach (var ing in recipe.ingredients)
                {
                    if (ing == null || string.IsNullOrWhiteSpace(ing.templateId))
                    {
                        continue;
                    }

                    var inName = catalog != null && catalog.TryGet(ing.templateId.Trim(), out var ie) &&
                                 !string.IsNullOrEmpty(ie.displayNameRu)
                        ? ie.displayNameRu
                        : ing.templateId;
                    GUILayout.Label($"  · {inName} x{ing.count}", GUI.skin.label);
                }

                var can = CraftingService.CanCraft(account, recipe);
                GUI.enabled = can;
                if (GUILayout.Button("Создать", GUILayout.Height(26f)))
                {
                    if (CraftingService.TryCraft(account, recipe, out var err))
                    {
                        _craftStatus = "Готово.";
                        CharacterStatsService.RecalculateForCamp(account);
                        onMutatedPersist?.Invoke();
                    }
                    else
                    {
                        _craftStatus = err;
                    }
                }

                GUI.enabled = true;
                GUILayout.Space(8f);
            }

            GUILayout.EndScrollView();
        }

        private static bool RecipeMatchesStation(CraftingRecipeEntry recipe, CraftingStation stationFilter)
        {
            if (recipe.station == CraftingStation.Any)
            {
                return true;
            }

            return recipe.station == stationFilter;
        }
    }
}
