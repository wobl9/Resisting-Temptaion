using System;
using ShatteredForge.Core;

namespace ShatteredForge.Menu
{
    internal static class ProfileAccountGoldMigration
    {
        /// <summary>
        /// Pre-<c>gold</c> saves deserialize as 0; grant starter purse once and persist.
        /// </summary>
        public static void ApplyMissingGoldFieldOnce(ProfileData profile, AccountState account, Action persist)
        {
            if (profile == null || account == null || profile.accountGoldMigrated)
            {
                return;
            }

            if (account.gold == 0)
            {
                account.gold = AccountEconomy.StarterGoldPurse;
            }

            profile.accountGoldMigrated = true;
            persist?.Invoke();
        }
    }
}
