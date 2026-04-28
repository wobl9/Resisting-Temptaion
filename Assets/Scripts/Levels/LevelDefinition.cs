using System.Collections.Generic;
using System.Linq;
using ShatteredForge.Items;
using UnityEngine;

namespace ShatteredForge.Levels
{
    [CreateAssetMenu(menuName = "Shattered Forge/Levels/Level Definition", fileName = "Level")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string levelId;
        public string displayName;
        public string biome;
        public LevelTierDefinition tier;
        [TextArea] public string description;
        public int recommendedPower;

        [Header("Mini-run shape")]
        [Min(3)] public int minRooms = 3;
        [Min(3)] public int maxRooms = 5;
        [Min(1)] public int regularEnemiesPerRoom = 4;
        [Min(0)] public int eliteRoomCount;

        [Header("Enemies")]
        public EnemyPoolDefinition regularPool;
        public EnemyPoolDefinition bossPool;
        public List<string> requiredTags = new();

        [Header("Loot (drops only on boss kill)")]
        public List<string> guaranteedDropTemplateIds = new();
        public LootTableDefinition randomLootTable;
        [Min(0)] public int randomDropRolls = 3;

        [Header("Scene override")]
        public GameObject customArenaPrefab;

        [Header("Future")]
        public List<string> modifierIdsReserved = new();

        private void OnValidate()
        {
            if (regularPool == null)
            {
                Debug.LogWarning($"[{nameof(LevelDefinition)} '{name}'] regularPool is null.", this);
            }
            else if (regularPool.entries == null || regularPool.entries.Count == 0)
            {
                Debug.LogWarning($"[{nameof(LevelDefinition)} '{name}'] regularPool is empty.", this);
            }

            if (bossPool == null)
            {
                Debug.LogWarning($"[{nameof(LevelDefinition)} '{name}'] bossPool is null.", this);
            }
            else if (bossPool.entries == null || !bossPool.entries.Any(e => e != null && e.isBoss))
            {
                Debug.LogWarning($"[{nameof(LevelDefinition)} '{name}'] bossPool has no entry with isBoss=true.", this);
            }

            if (tier == null)
            {
                Debug.LogWarning($"[{nameof(LevelDefinition)} '{name}'] tier is null; multipliers default to 1.0 at runtime.", this);
            }

            if (maxRooms < minRooms)
            {
                maxRooms = minRooms;
            }

            if (guaranteedDropTemplateIds == null || ItemCatalogRuntime.Current == null)
            {
                return;
            }

            foreach (var id in guaranteedDropTemplateIds)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                if (!ItemCatalogRuntime.Current.TryGet(id.Trim(), out _))
                {
                    Debug.LogWarning(
                        $"[{nameof(LevelDefinition)} '{name}'] guaranteed drop '{id}' not found in ItemCatalog.",
                        this);
                }
            }
        }
    }
}
