using ShatteredForge.Items;

namespace ShatteredForge.Core
{
    /// <summary>
    /// Which body slot an item may use in camp (catalog preferred slot + weapon hands / armor chest default).
    /// </summary>
    public static class CampItemSlotRules
    {
        public static bool CanWearInBodySlot(string templateId, EquipmentBodySlot slot)
        {
            if (string.IsNullOrEmpty(templateId) || slot == EquipmentBodySlot.None)
            {
                return false;
            }

            var kind = InventoryEquipmentRules.Classify(templateId);
            if (kind == ItemEquipmentKind.None)
            {
                return false;
            }

            var catalog = ItemCatalogRuntime.Current;
            if (catalog != null && catalog.TryGet(templateId, out var entry) && entry.preferredBodySlot != EquipmentBodySlot.None)
            {
                if (kind == ItemEquipmentKind.Weapon)
                {
                    return slot == EquipmentBodySlot.MainHand || slot == EquipmentBodySlot.OffHand;
                }

                if (kind == ItemEquipmentKind.Ring)
                {
                    if (entry.preferredBodySlot == EquipmentBodySlot.Ring2)
                    {
                        return slot == EquipmentBodySlot.Ring2;
                    }

                    if (entry.preferredBodySlot == EquipmentBodySlot.Ring)
                    {
                        return slot == EquipmentBodySlot.Ring;
                    }

                    return slot == EquipmentBodySlot.Ring || slot == EquipmentBodySlot.Ring2;
                }

                if (kind == ItemEquipmentKind.Amulet)
                {
                    return slot == EquipmentBodySlot.Amulet;
                }

                return slot == entry.preferredBodySlot;
            }

            if (kind == ItemEquipmentKind.Weapon)
            {
                return slot == EquipmentBodySlot.MainHand || slot == EquipmentBodySlot.OffHand;
            }

            if (kind == ItemEquipmentKind.Armor)
            {
                return slot == EquipmentBodySlot.Chest;
            }

            if (kind == ItemEquipmentKind.Ring)
            {
                return slot == EquipmentBodySlot.Ring || slot == EquipmentBodySlot.Ring2;
            }

            if (kind == ItemEquipmentKind.Amulet)
            {
                return slot == EquipmentBodySlot.Amulet;
            }

            return false;
        }
    }
}
