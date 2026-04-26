using System;
using System.Collections.Generic;
using ShatteredForge.Core;
using UnityEngine;

namespace ShatteredForge.Combat
{
    [CreateAssetMenu(menuName = "Shattered Forge/Combat/Enemy Stat Profile Catalog", fileName = "EnemyStatProfileCatalog")]
    public sealed class EnemyStatProfileCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [Tooltip("Profile key, e.g. grunt / elite / boss.")]
            public string id;
            [Min(0.1f)] public float health = 3f;
            [Min(0.1f)] public float moveSpeed = 2.2f;
            [Min(0f)] public float contactDamage = 0.06f;
            [Min(0.05f)] public float contactCooldown = 0.9f;
            public CharacterPrimaryStats primaryStats = CharacterPrimaryStats.CreateDefault();
            public FlatStatBonuses flatBonuses = new();
        }

        [SerializeField] private List<Entry> entries = new();

        private Dictionary<string, Entry> _byId;

        private void OnEnable()
        {
            RebuildMap();
        }

        private void OnValidate()
        {
            RebuildMap();
        }

        public bool TryGet(string id, out Entry entry)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                entry = null;
                return false;
            }

            if (_byId == null)
            {
                RebuildMap();
            }

            return _byId.TryGetValue(id.Trim(), out entry);
        }

        private void RebuildMap()
        {
            _byId = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.id))
                {
                    continue;
                }

                _byId[entry.id.Trim()] = entry;
            }
        }
    }
}
