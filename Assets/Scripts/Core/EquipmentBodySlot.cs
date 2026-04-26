namespace ShatteredForge.Core
{
    /// <summary>
    /// Camp / meta paper-doll slots (persisted on <see cref="AccountState"/>). Run uses flat list; on expedition start items move from paper doll to <see cref="RunState.equippedLoadout"/>.
    /// </summary>
    public enum EquipmentBodySlot
    {
        None = 0,
        Head = 1,
        Chest = 2,
        Boots = 3,
        Gloves = 4,
        MainHand = 5,
        OffHand = 6,
        Ring = 7,
        Amulet = 8
    }
}
