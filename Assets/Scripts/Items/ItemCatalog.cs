using System;
using System.Collections.Generic;
using ShatteredForge.Core;
using UnityEngine;

namespace ShatteredForge.Items
{
    /// <summary>High-level catalog classification for UI and economy rules.</summary>
    public enum CatalogItemKind
    {
        Gear = 0,
        Material = 1
    }

    /// <summary>
    /// Bootstrap-time catalog reference for <see cref="ItemCatalogRuntime.Current"/> (set from gameplay / camp).
    /// </summary>
    public static class ItemCatalogRuntime
    {
        public static ItemCatalog Current { get; set; }
    }

    /// <summary>
    /// Data-only item definitions keyed by <see cref="ItemInstance.templateId"/> (server/local JSON friendly).
    /// </summary>
    [CreateAssetMenu(menuName = "Shattered Forge/Items/Item Catalog", fileName = "ItemCatalog")]
    public sealed class ItemCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [Tooltip("Must match ItemInstance.templateId in saves.")]
            public string templateId;

            [Tooltip("Short RU label for IMGUI / future UI.")]
            public string displayNameRu;

            public ItemEquipmentKind equipKind = ItemEquipmentKind.None;

            [Tooltip("Camp paper-doll slot; weapons still allowed in MainHand or OffHand when this is MainHand.")]
            public EquipmentBodySlot preferredBodySlot = EquipmentBodySlot.None;

            [Tooltip("Buy price in gold (shop / future vendors).")]
            [Min(0)]
            public int buyGoldPrice;

            public CatalogItemKind catalogKind = CatalogItemKind.Gear;
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
            foreach (var e in entries)
            {
                if (e == null || string.IsNullOrWhiteSpace(e.templateId))
                {
                    continue;
                }

                _byId[e.templateId.Trim()] = e;
            }
        }

        public bool TryGet(string templateId, out Entry entry)
        {
            if (string.IsNullOrEmpty(templateId))
            {
                entry = null;
                return false;
            }

            if (_byId == null)
            {
                RebuildMap();
            }

            return _byId.TryGetValue(templateId, out entry);
        }

        public int GetBuyGoldPrice(string templateId)
        {
            return TryGet(templateId, out var e) ? Mathf.Max(0, e.buyGoldPrice) : 0;
        }

        public bool IsMaterial(string templateId)
        {
            return TryGet(templateId, out var e) && e.catalogKind == CatalogItemKind.Material;
        }
    }

    [Serializable]
    public sealed class VendorOfferEntry
    {
        public string templateId;

        [Tooltip("-1 = use ItemCatalog.buyGoldPrice")]
        public int priceGoldOverride = -1;
    }

    [CreateAssetMenu(menuName = "Shattered Forge/Economy/Vendor Catalog", fileName = "VendorCatalog")]
    public sealed class VendorCatalog : ScriptableObject
    {
        public List<VendorOfferEntry> offers = new List<VendorOfferEntry>();

        public int ResolvePrice(VendorOfferEntry offer, ItemCatalog catalog)
        {
            if (offer == null || string.IsNullOrWhiteSpace(offer.templateId))
            {
                return 0;
            }

            if (offer.priceGoldOverride >= 0)
            {
                return offer.priceGoldOverride;
            }

            return catalog != null ? catalog.GetBuyGoldPrice(offer.templateId.Trim()) : 0;
        }
    }
}
