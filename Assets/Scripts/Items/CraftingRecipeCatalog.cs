using System;
using System.Collections.Generic;
using ShatteredForge.Core;
using UnityEngine;

namespace ShatteredForge.Items
{
    public enum CraftingStation
    {
        Forge = 0,
        Alchemy = 1,
        Any = 2
    }

    [Serializable]
    public sealed class CraftingIngredient
    {
        public string templateId;
        [Min(1)]
        public int count = 1;
    }

    [Serializable]
    public sealed class CraftingRecipeEntry
    {
        public string recipeId;
        public CraftingStation station = CraftingStation.Forge;
        public string outputTemplateId;
        [Min(1)]
        public int outputCount = 1;

        [Tooltip("Empty = no gate.")]
        public string requiredUnlockedNodeId = "";

        public List<CraftingIngredient> ingredients = new List<CraftingIngredient>();
    }

    [CreateAssetMenu(menuName = "Shattered Forge/Economy/Crafting Recipe Catalog", fileName = "CraftingRecipeCatalog")]
    public sealed class CraftingRecipeCatalog : ScriptableObject
    {
        public List<CraftingRecipeEntry> recipes = new List<CraftingRecipeEntry>();

        /// <summary>
        /// Baked defaults when <see cref="Resources"/> asset is missing or deserializes with an empty list.
        /// </summary>
        public static CraftingRecipeCatalog CreateWithDefaultRecipes()
        {
            var c = CreateInstance<CraftingRecipeCatalog>();
            c.recipes = BuildDefaultRecipeList();
            return c;
        }

        public static List<CraftingRecipeEntry> BuildDefaultRecipeList()
        {
            return new List<CraftingRecipeEntry>
            {
                new CraftingRecipeEntry
                {
                    recipeId = "forge_t1_sword",
                    station = CraftingStation.Forge,
                    outputTemplateId = "weapon_sword_t1",
                    outputCount = 1,
                    ingredients = new List<CraftingIngredient>
                    {
                        new CraftingIngredient { templateId = "mat_bone_shard", count = 3 },
                        new CraftingIngredient { templateId = "mat_ember_dust", count = 2 }
                    }
                },
                new CraftingRecipeEntry
                {
                    recipeId = "alchemy_void_sliver",
                    station = CraftingStation.Alchemy,
                    outputTemplateId = "mat_void_sliver",
                    outputCount = 1,
                    ingredients = new List<CraftingIngredient>
                    {
                        new CraftingIngredient { templateId = "mat_bone_shard", count = 2 },
                        new CraftingIngredient { templateId = "mat_ember_dust", count = 2 }
                    }
                },
                new CraftingRecipeEntry
                {
                    recipeId = "forge_t1_armor",
                    station = CraftingStation.Forge,
                    outputTemplateId = "armor_chest_t1",
                    outputCount = 1,
                    ingredients = new List<CraftingIngredient>
                    {
                        new CraftingIngredient { templateId = "mat_bone_shard", count = 5 }
                    }
                }
            };
        }
    }

    /// <summary>Consumes matching items from <see cref="AccountState.stash"/> and appends crafted outputs.</summary>
    public static class CraftingService
    {
        public static bool CanCraft(AccountState account, CraftingRecipeEntry recipe)
        {
            return TryCraft(account, recipe, out _, dryRun: true);
        }

        public static bool TryCraft(AccountState account, CraftingRecipeEntry recipe, out string failureReason, bool dryRun = false)
        {
            failureReason = string.Empty;
            if (account?.stash == null || recipe == null)
            {
                failureReason = "Нет данных.";
                return false;
            }

            if (!string.IsNullOrEmpty(recipe.requiredUnlockedNodeId) &&
                (account.unlockedNodes == null || !account.unlockedNodes.Contains(recipe.requiredUnlockedNodeId)))
            {
                failureReason = "Узел не разблокирован.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(recipe.outputTemplateId) || recipe.outputCount < 1)
            {
                failureReason = "Неверный рецепт.";
                return false;
            }

            if (recipe.ingredients == null || recipe.ingredients.Count == 0)
            {
                failureReason = "Нет ингредиентов.";
                return false;
            }

            var need = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var ing in recipe.ingredients)
            {
                if (ing == null || string.IsNullOrWhiteSpace(ing.templateId) || ing.count < 1)
                {
                    failureReason = "Неверный ингредиент.";
                    return false;
                }

                var id = ing.templateId.Trim();
                need[id] = need.TryGetValue(id, out var c) ? c + ing.count : ing.count;
            }

            var have = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var it in account.stash)
            {
                if (it == null || string.IsNullOrWhiteSpace(it.templateId))
                {
                    continue;
                }

                var id = it.templateId.Trim();
                have[id] = have.TryGetValue(id, out var c) ? c + 1 : 1;
            }

            foreach (var kv in need)
            {
                if (!have.TryGetValue(kv.Key, out var hc) || hc < kv.Value)
                {
                    failureReason = "Не хватает материалов.";
                    return false;
                }
            }

            if (dryRun)
            {
                return true;
            }

            foreach (var kv in need)
            {
                RemoveFromStash(account.stash, kv.Key, kv.Value);
            }

            for (var i = 0; i < recipe.outputCount; i++)
            {
                account.stash.Add(new ItemInstance
                {
                    id = Guid.NewGuid().ToString(),
                    templateId = recipe.outputTemplateId.Trim(),
                    rarity = "Обычная",
                    enhanceLevel = 0
                });
            }

            return true;
        }

        private static void RemoveFromStash(List<ItemInstance> stash, string templateId, int count)
        {
            var remaining = count;
            for (var i = stash.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var it = stash[i];
                if (it == null || !string.Equals(it.templateId?.Trim(), templateId, StringComparison.Ordinal))
                {
                    continue;
                }

                stash.RemoveAt(i);
                remaining--;
            }
        }
    }
}
