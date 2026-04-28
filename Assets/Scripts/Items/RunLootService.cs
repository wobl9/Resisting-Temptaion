using System;
using System.Collections.Generic;
using ShatteredForge.Core;
using ShatteredForge.Run;
using UnityEngine;

namespace ShatteredForge.Items
{
    /// <summary>
    /// Deterministic rolls for expedition loot using <see cref="RunState.seed"/> and room / kill counters.
    /// </summary>
    public static class RunLootService
    {
        public static List<ItemInstance> RollRoomClearLoot(LootTableDefinition table, RoomType roomType, int runSeed, int roomIndex)
        {
            var list = new List<ItemInstance>();
            if (table == null || table.roomClearRows == null || table.roomClearRows.Count == 0)
            {
                return list;
            }

            var rng = new System.Random(MixSeed(runSeed, roomIndex, 31));
            var applicable = new List<LootTableDefinition.RoomRow>();
            var total = 0;
            foreach (var row in table.roomClearRows)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.templateId) || row.weight <= 0)
                {
                    continue;
                }

                if (row.appliesToRoomTypes != null && row.appliesToRoomTypes.Count > 0 &&
                    !row.appliesToRoomTypes.Contains(roomType))
                {
                    continue;
                }

                applicable.Add(row);
                total += row.weight;
            }

            if (applicable.Count == 0 || total <= 0)
            {
                return list;
            }

            var rolls = RollCountForRoom(roomType);
            for (var r = 0; r < rolls; r++)
            {
                var pick = rng.Next(0, total);
                var acc = 0;
                LootTableDefinition.RoomRow chosen = null;
                foreach (var row in applicable)
                {
                    acc += row.weight;
                    if (pick < acc)
                    {
                        chosen = row;
                        break;
                    }
                }

                if (chosen == null)
                {
                    chosen = applicable[applicable.Count - 1];
                }

                var lo = Mathf.Min(chosen.minCount, chosen.maxCount);
                var hi = Mathf.Max(chosen.minCount, chosen.maxCount);
                var n = Mathf.Max(1, rng.Next(lo, hi + 1));
                for (var i = 0; i < n; i++)
                {
                    list.Add(CreateLootInstance(chosen.templateId.Trim(), roomType));
                }
            }

            return list;
        }

        public static List<ItemInstance> RollEnemyKillLoot(
            LootTableDefinition table,
            string enemyProfileId,
            int runSeed,
            int killNonce)
        {
            var list = new List<ItemInstance>();
            if (table == null || string.IsNullOrWhiteSpace(enemyProfileId) ||
                table.enemyKillRows == null || table.enemyKillRows.Count == 0)
            {
                return list;
            }

            var applicable = new List<LootTableDefinition.KillRow>();
            var total = 0;
            foreach (var row in table.enemyKillRows)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.templateId) || row.weight <= 0)
                {
                    continue;
                }

                if (!string.Equals(row.enemyProfileId?.Trim(), enemyProfileId.Trim(), StringComparison.Ordinal))
                {
                    continue;
                }

                applicable.Add(row);
                total += row.weight;
            }

            if (applicable.Count == 0 || total <= 0)
            {
                return list;
            }

            var rng = new System.Random(MixSeed(runSeed, killNonce, 97));
            var pick = rng.Next(0, total);
            var acc = 0;
            LootTableDefinition.KillRow chosen = null;
            foreach (var row in applicable)
            {
                acc += row.weight;
                if (pick < acc)
                {
                    chosen = row;
                    break;
                }
            }

            if (chosen == null)
            {
                chosen = applicable[applicable.Count - 1];
            }

            var lo = Mathf.Min(chosen.minCount, chosen.maxCount);
            var hi = Mathf.Max(chosen.minCount, chosen.maxCount);
            var n = Mathf.Max(1, rng.Next(lo, hi + 1));
            for (var i = 0; i < n; i++)
            {
                list.Add(CreateLootInstance(chosen.templateId.Trim(), RoomType.Combat));
            }

            return list;
        }

        private static int RollCountForRoom(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Elite => 2,
                RoomType.Boss => 3,
                _ => 1
            };
        }

        private static ItemInstance CreateLootInstance(string templateId, RoomType roomType)
        {
            return ItemInstanceFactory.Create(
                templateId,
                roomType == RoomType.Boss ? "Редкая" : roomType == RoomType.Elite ? "Необычная" : "Обычная");
        }

        private static int MixSeed(int runSeed, int a, int salt)
        {
            unchecked
            {
                var h = runSeed;
                h ^= (h << 13);
                h ^= (h >> 17);
                h ^= (h << 5);
                h ^= a * 73856093;
                h ^= salt * 19349663;
                return h == 0 ? 1 : h;
            }
        }
    }
}
