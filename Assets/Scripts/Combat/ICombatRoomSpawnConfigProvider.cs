using ShatteredForge.Run;

namespace ShatteredForge.Combat
{
    public readonly struct CombatRoomSpawnConfig
    {
        public readonly string combatProfileId;
        public readonly string eliteProfileId;
        public readonly string bossProfileId;
        public readonly int regularEnemyCountOverride;
        public readonly float healthMultiplier;
        public readonly float damageMultiplier;
        public readonly float moveSpeedMultiplier;
        public readonly float attackSpeedMultiplier;

        public CombatRoomSpawnConfig(
            string combatProfileId,
            string eliteProfileId,
            string bossProfileId,
            int regularEnemyCountOverride,
            float healthMultiplier,
            float damageMultiplier,
            float moveSpeedMultiplier,
            float attackSpeedMultiplier)
        {
            this.combatProfileId = combatProfileId;
            this.eliteProfileId = eliteProfileId;
            this.bossProfileId = bossProfileId;
            this.regularEnemyCountOverride = regularEnemyCountOverride;
            this.healthMultiplier = healthMultiplier;
            this.damageMultiplier = damageMultiplier;
            this.moveSpeedMultiplier = moveSpeedMultiplier;
            this.attackSpeedMultiplier = attackSpeedMultiplier;
        }
    }

    public interface ICombatRoomSpawnConfigProvider
    {
        bool TryGetSpawnConfig(RoomType roomType, out CombatRoomSpawnConfig config);
    }
}
