using System.Collections.Generic;
using UnityEngine;

namespace ShatteredForge.Levels
{
    [CreateAssetMenu(menuName = "Shattered Forge/Levels/Level Tier", fileName = "LevelTier")]
    public sealed class LevelTierDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string tierId = "medium";
        public string displayName = "Medium";
        public Color uiColor = Color.white;
        public int sortOrder;

        [Header("Enemy stat multipliers (1.0 = baseline)")]
        // Applies only to direct profile values (health/contactDamage/moveSpeed/contactCooldown).
        // Stat-derived damage from primaryStats/flatBonuses is intentionally not scaled in prototype.
        [Min(0.1f)] public float enemyHealthMultiplier = 1f;
        [Min(0f)] public float enemyDamageMultiplier = 1f;
        [Min(0.1f)] public float enemyMoveSpeedMultiplier = 1f;
        [Min(0.1f)] public float enemyAttackSpeedMultiplier = 1f;

        [Header("Encounter density")]
        [Min(0)] public int regularEnemyCountOverride;
        [Min(0)] public int extraEliteSlots;

        [Header("Loot multipliers")]
        [Min(0)] public int extraRandomLootRolls;
        [Min(0f)] public float lootRarityBias;

        [Header("Future")]
        public List<string> reservedAffixIds = new();
    }
}
