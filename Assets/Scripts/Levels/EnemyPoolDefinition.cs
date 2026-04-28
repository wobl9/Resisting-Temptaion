using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShatteredForge.Levels
{
    [CreateAssetMenu(menuName = "Shattered Forge/Levels/Enemy Pool", fileName = "EnemyPool")]
    public sealed class EnemyPoolDefinition : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string enemyProfileId;
            [Min(0)] public int weight = 1;
            public List<string> tags = new();
            public bool isElite;
            public bool isBoss;
        }

        public List<Entry> entries = new();

        private void OnValidate()
        {
            if (entries == null)
            {
                return;
            }

            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.enemyProfileId))
                {
                    Debug.LogWarning($"[{nameof(EnemyPoolDefinition)} '{name}'] Entry #{i} has empty enemyProfileId.", this);
                }
                else if (!seenIds.Add(entry.enemyProfileId.Trim()))
                {
                    Debug.LogWarning($"[{nameof(EnemyPoolDefinition)} '{name}'] Duplicate enemyProfileId '{entry.enemyProfileId}'.", this);
                }

                if (entry.weight <= 0)
                {
                    Debug.LogWarning($"[{nameof(EnemyPoolDefinition)} '{name}'] Entry '{entry.enemyProfileId}' has non-positive weight ({entry.weight}).", this);
                }
            }
        }
    }
}
