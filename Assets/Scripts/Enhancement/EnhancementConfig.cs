using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShatteredForge.Enhancement
{
    [CreateAssetMenu(fileName = "EnhancementConfig", menuName = "ShatteredForge/Enhancement Config")]
    public class EnhancementConfig : ScriptableObject
    {
        [Serializable]
        public struct TierChance
        {
            public int fromLevel;
            public int toLevel;
            [Range(0f, 1f)] public float successChance;
            public bool allowsDestruction;
        }

        public List<TierChance> chances = new()
        {
            new TierChance { fromLevel = 0, toLevel = 1, successChance = 1f, allowsDestruction = false },
            new TierChance { fromLevel = 1, toLevel = 2, successChance = 1f, allowsDestruction = false },
            new TierChance { fromLevel = 2, toLevel = 3, successChance = 1f, allowsDestruction = false },
            new TierChance { fromLevel = 3, toLevel = 4, successChance = 0.85f, allowsDestruction = false },
            new TierChance { fromLevel = 4, toLevel = 5, successChance = 0.70f, allowsDestruction = false },
            new TierChance { fromLevel = 5, toLevel = 6, successChance = 0.55f, allowsDestruction = false },
            new TierChance { fromLevel = 6, toLevel = 7, successChance = 0.40f, allowsDestruction = false },
            new TierChance { fromLevel = 7, toLevel = 8, successChance = 0.30f, allowsDestruction = false },
            new TierChance { fromLevel = 8, toLevel = 9, successChance = 0.22f, allowsDestruction = false },
            new TierChance { fromLevel = 9, toLevel = 10, successChance = 0.16f, allowsDestruction = false },
            new TierChance { fromLevel = 10, toLevel = 11, successChance = 0.12f, allowsDestruction = true },
            new TierChance { fromLevel = 11, toLevel = 12, successChance = 0.09f, allowsDestruction = true },
            new TierChance { fromLevel = 12, toLevel = 13, successChance = 0.06f, allowsDestruction = true },
            new TierChance { fromLevel = 13, toLevel = 14, successChance = 0.04f, allowsDestruction = true },
            new TierChance { fromLevel = 14, toLevel = 15, successChance = 0.02f, allowsDestruction = true },
        };

        [Range(0f, 0.25f)] public float pityPerFailure = 0.01f;
        [Range(0f, 0.25f)] public float stabilizerBonus = 0.05f;
    }
}
