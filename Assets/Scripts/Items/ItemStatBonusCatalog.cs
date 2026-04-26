using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShatteredForge.Items
{
    public static class ItemStatBonusCatalogRuntime
    {
        public static ItemStatBonusCatalog Current { get; set; }
    }

    [CreateAssetMenu(menuName = "Shattered Forge/Items/Item Stat Bonus Catalog", fileName = "ItemStatBonusCatalog")]
    public sealed class ItemStatBonusCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [Tooltip("Must match ItemInstance.templateId in saves.")]
            public string templateId;
            [Header("Primary stats (flat)")]
            public int strength;
            public int agility;
            public int vitality;
            public int intellect;
            [Header("Combat stats (flat)")]
            public int damage;
            public int armor;
            public int fireResistance;
            public int coldResistance;
            public int lightningResistance;
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

        public void RebuildMap()
        {
            _byId = new Dictionary<string, Entry>(StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.templateId))
                {
                    continue;
                }

                _byId[entry.templateId.Trim()] = entry;
            }
        }

        public bool TryGet(string templateId, out Entry entry)
        {
            if (string.IsNullOrWhiteSpace(templateId))
            {
                entry = null;
                return false;
            }

            if (_byId == null)
            {
                RebuildMap();
            }

            return _byId.TryGetValue(templateId.Trim(), out entry);
        }
    }
}
