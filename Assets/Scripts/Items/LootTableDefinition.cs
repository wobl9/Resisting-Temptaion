using System;
using System.Collections.Generic;
using ShatteredForge.Run;
using UnityEngine;

namespace ShatteredForge.Items
{
    /// <summary>
    /// Weighted loot for room clears and optional per-enemy kills (profile id matches <see cref="EnemyStatProfileCatalog.Entry.id"/>).
    /// </summary>
    [CreateAssetMenu(menuName = "Shattered Forge/Economy/Loot Table", fileName = "LootTable")]
    public sealed class LootTableDefinition : ScriptableObject
    {
        [Serializable]
        public sealed class RoomRow
        {
            public string templateId;

            [Min(0)]
            public int weight = 1;

            public int minCount = 1;
            public int maxCount = 1;

            [Tooltip("Empty = row applies to every room type.")]
            public List<RoomType> appliesToRoomTypes = new();
        }

        [Serializable]
        public sealed class KillRow
        {
            [Tooltip("grunt / elite / boss — same id as enemy stat profile.")]
            public string enemyProfileId = "grunt";

            public string templateId;

            [Min(0)]
            public int weight = 1;

            public int minCount = 1;
            public int maxCount = 1;
        }

        public List<RoomRow> roomClearRows = new();
        public List<KillRow> enemyKillRows = new();
    }
}
