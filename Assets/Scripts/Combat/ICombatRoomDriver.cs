using ShatteredForge.Core;
using ShatteredForge.Run;

namespace ShatteredForge.Combat
{
    public interface ICombatRoomDriver
    {
        bool IsInRun { get; }
        RunState CurrentRun { get; }
        ComputedCharacterStats CurrentComputedStats { get; }

        bool TryGetCurrentRoom(out RoomType room);
        void ApplyCurrentRoomClearedFromGameplay();
        void ApplyPlayerDeathFromGameplay();
        void NotifyEnemyKillLoot(string enemyProfileId);
    }
}
