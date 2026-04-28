using System;
using System.Collections.Generic;
using ShatteredForge.Run;
using UnityEngine;

namespace ShatteredForge.Levels
{
    public static class LevelEnemyResolver
    {
        public static string PickEnemyId(LevelDefinition level, RoomType roomType, System.Random rng)
        {
            if (level == null)
            {
                return DefaultIdFor(roomType);
            }

            var pool = roomType == RoomType.Boss ? level.bossPool : level.regularPool;
            if (pool == null || pool.entries == null || pool.entries.Count == 0)
            {
                Debug.LogWarning(
                    $"[{nameof(LevelEnemyResolver)}] Level '{level.levelId}' has no pool for room '{roomType}'. Falling back to '{DefaultIdFor(roomType)}'.");
                return DefaultIdFor(roomType);
            }

            var applicable = new List<EnemyPoolDefinition.Entry>();
            var totalWeight = 0;
            for (var i = 0; i < pool.entries.Count; i++)
            {
                var entry = pool.entries[i];
                if (entry == null || entry.weight <= 0 || string.IsNullOrWhiteSpace(entry.enemyProfileId))
                {
                    continue;
                }

                if (roomType == RoomType.Boss && !entry.isBoss)
                {
                    continue;
                }

                if (roomType != RoomType.Boss && entry.isBoss)
                {
                    continue;
                }

                if (roomType != RoomType.Boss && roomType != RoomType.Elite && entry.isElite)
                {
                    continue;
                }

                if (!HasRequiredTags(level.requiredTags, entry.tags))
                {
                    continue;
                }

                applicable.Add(entry);
                totalWeight += entry.weight;
            }

            if (applicable.Count == 0 || totalWeight <= 0)
            {
                Debug.LogWarning(
                    $"[{nameof(LevelEnemyResolver)}] No applicable enemies for level '{level.levelId}' room '{roomType}'. Falling back to '{DefaultIdFor(roomType)}'.");
                return DefaultIdFor(roomType);
            }

            var safeRng = rng ?? new System.Random(1);
            var roll = safeRng.Next(0, totalWeight);
            var acc = 0;
            for (var i = 0; i < applicable.Count; i++)
            {
                acc += applicable[i].weight;
                if (roll < acc)
                {
                    return applicable[i].enemyProfileId.Trim();
                }
            }

            return applicable[applicable.Count - 1].enemyProfileId.Trim();
        }

        private static bool HasRequiredTags(List<string> requiredTags, List<string> entryTags)
        {
            if (requiredTags == null || requiredTags.Count == 0)
            {
                return true;
            }

            if (entryTags == null || entryTags.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < requiredTags.Count; i++)
            {
                var required = requiredTags[i];
                if (string.IsNullOrWhiteSpace(required))
                {
                    continue;
                }

                var matched = false;
                for (var j = 0; j < entryTags.Count; j++)
                {
                    if (string.Equals(required.Trim(), entryTags[j]?.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                {
                    return false;
                }
            }

            return true;
        }

        private static string DefaultIdFor(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Boss => "boss",
                RoomType.Elite => "elite",
                _ => "grunt"
            };
        }
    }
}
