using System;

namespace ShatteredForge.Core
{
    /// <summary>Shared economy constants (starting purse, etc.).</summary>
    public static class AccountEconomy
    {
        public const int StarterGoldPurse = 20;

        /// <summary>Demo-friendly starter mats so forge recipes are usable before first shop.</summary>
        public static void AppendStarterCraftMaterials(AccountState account)
        {
            if (account?.stash == null)
            {
                return;
            }

            for (var i = 0; i < 3; i++)
            {
                account.stash.Add(CreateMaterial("mat_bone_shard"));
            }

            for (var i = 0; i < 2; i++)
            {
                account.stash.Add(CreateMaterial("mat_ember_dust"));
            }
        }

        private static ItemInstance CreateMaterial(string templateId)
        {
            return new ItemInstance
            {
                id = Guid.NewGuid().ToString(),
                templateId = templateId,
                rarity = "Обычная",
                enhanceLevel = 0
            };
        }
    }
}
