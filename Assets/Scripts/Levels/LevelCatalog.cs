using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShatteredForge.Levels
{
    [CreateAssetMenu(menuName = "Shattered Forge/Levels/Level Catalog", fileName = "LevelCatalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        public List<LevelDefinition> levels = new();
        public List<LevelTierDefinition> tiers = new();

        public LevelDefinition Find(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId) || levels == null)
            {
                return null;
            }

            var needle = levelId.Trim();
            for (var i = 0; i < levels.Count; i++)
            {
                var level = levels[i];
                if (level == null || string.IsNullOrWhiteSpace(level.levelId))
                {
                    continue;
                }

                if (string.Equals(level.levelId.Trim(), needle, StringComparison.OrdinalIgnoreCase))
                {
                    return level;
                }
            }

            return null;
        }

        public IEnumerable<LevelDefinition> ByTier(LevelTierDefinition tier)
        {
            if (levels == null || tier == null)
            {
                yield break;
            }

            for (var i = 0; i < levels.Count; i++)
            {
                var level = levels[i];
                if (level != null && level.tier == tier)
                {
                    yield return level;
                }
            }
        }

        public LevelDefinition PickRandom(LevelTierDefinition tier, int seed)
        {
            if (tier == null || levels == null || levels.Count == 0)
            {
                return null;
            }

            var candidates = new List<LevelDefinition>();
            for (var i = 0; i < levels.Count; i++)
            {
                var level = levels[i];
                if (level != null && level.tier == tier)
                {
                    candidates.Add(level);
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            var rng = new System.Random(seed == 0 ? 1 : seed);
            return candidates[rng.Next(0, candidates.Count)];
        }
    }
}
