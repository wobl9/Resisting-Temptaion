using System;
using ShatteredForge.Core;

namespace ShatteredForge.Items
{
    public static class ItemInstanceFactory
    {
        public static ItemInstance Create(
            string templateId,
            string rarity = "Обычная",
            int enhanceLevel = 0,
            bool isInsuredForRun = false,
            bool rerollBaseCombat = true)
        {
            var item = new ItemInstance
            {
                id = Guid.NewGuid().ToString(),
                templateId = templateId,
                rarity = rarity,
                enhanceLevel = enhanceLevel,
                isInsuredForRun = isInsuredForRun
            };

            var catalog = ItemCatalogRuntime.Current;
            if (catalog != null)
            {
                catalog.ApplyBaseCombatStats(item, rerollBaseCombat);
            }
            else
            {
                if (templateId.StartsWith("weapon_", StringComparison.Ordinal))
                {
                    item.baseDamage = 1f;
                    item.baseArmor = 0f;
                }
                else if (templateId.StartsWith("armor_", StringComparison.Ordinal))
                {
                    item.baseDamage = 0f;
                    item.baseArmor = 1f;
                }
            }
            return item;
        }
    }
}
