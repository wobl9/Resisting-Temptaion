using ShatteredForge.Core;

namespace ShatteredForge.UI
{
    /// <summary>
    /// Shared paper-doll slot order / labels for camp character sheet (IMGUI + uGUI).
    /// </summary>
    public static class CampCharacterSheetMetadata
    {
        public static readonly (EquipmentBodySlot slot, string label)[] DollSlotUiOrder =
        {
            (EquipmentBodySlot.Head, "Шлем"),
            (EquipmentBodySlot.Amulet, "Амулет"),
            (EquipmentBodySlot.Chest, "Броня"),
            (EquipmentBodySlot.MainHand, "Слева"),
            (EquipmentBodySlot.OffHand, "Справа"),
            (EquipmentBodySlot.Gloves, "Перчатки"),
            (EquipmentBodySlot.Ring, "Кольцо слева"),
            (EquipmentBodySlot.Ring2, "Кольцо справа"),
            (EquipmentBodySlot.Boots, "Ботинки")
        };
    }
}
