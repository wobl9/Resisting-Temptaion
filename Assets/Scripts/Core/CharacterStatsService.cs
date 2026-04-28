using System.Collections.Generic;
using ShatteredForge.Items;

namespace ShatteredForge.Core
{
    /// <summary>
    /// Centralized stat normalization + derived stat recalculation.
    /// </summary>
    public static class CharacterStatsService
    {
        private const int BaseDamage = 5;
        private const int BaseArmor = 2;
        private const int BaseElementalResistance = 3;
        private const float BaseAttackSpeed = 1f;
        private const float BaseCritChance = 0.05f;
        private const int BaseMana = 20;

        public static void EnsureInitialized(AccountState account)
        {
            if (account == null)
            {
                return;
            }

            account.primaryStats ??= CharacterPrimaryStats.CreateDefault();
            account.computedStats ??= new ComputedCharacterStats();
            account.computedStats.elementalResists ??= new ElementalResistanceProfile();
        }

        public static void RecalculateForCamp(AccountState account)
        {
            EnsureInitialized(account);
            if (account == null)
            {
                return;
            }

            var equipped = new List<ItemInstance>();
            if (account.characterPaperDoll != null)
            {
                for (var i = 0; i < account.characterPaperDoll.Count; i++)
                {
                    var row = account.characterPaperDoll[i];
                    if (row?.item != null && !string.IsNullOrEmpty(row.item.templateId))
                    {
                        equipped.Add(row.item);
                    }
                }
            }

            account.computedStats = BuildComputed(account.primaryStats, equipped);
        }

        public static void RecalculateForRun(AccountState account, RunState run)
        {
            EnsureInitialized(account);
            if (account == null)
            {
                return;
            }

            var equipped = run?.equippedLoadout ?? new List<ItemInstance>();
            account.computedStats = BuildComputed(account.primaryStats, equipped);
        }

        public static ComputedCharacterStats BuildComputed(CharacterPrimaryStats baseStats, IReadOnlyList<ItemInstance> equippedItems)
        {
            var safeBase = baseStats ?? CharacterPrimaryStats.CreateDefault();
            var totalStrength = safeBase.strength;
            var totalAgility = safeBase.agility;
            var totalVitality = safeBase.vitality;
            var totalIntellect = safeBase.intellect;

            var bonusDamage = 0;
            var bonusArmor = 0;
            var bonusFire = 0;
            var bonusCold = 0;
            var bonusLightning = 0;
            var bonusAttackSpeed = 0f;
            var bonusCritChance = 0f;
            var bonusMana = 0;
            var bonusMagicPower = 0;

            var bonusCatalog = ItemStatBonusCatalogRuntime.Current;
            if (equippedItems != null && bonusCatalog != null)
            {
                for (var i = 0; i < equippedItems.Count; i++)
                {
                    var item = equippedItems[i];
                    if (item == null || string.IsNullOrEmpty(item.templateId))
                    {
                        continue;
                    }

                    if (!bonusCatalog.TryGet(item.templateId, out var bonus))
                    {
                        continue;
                    }

                    totalStrength += bonus.strength;
                    totalAgility += bonus.agility;
                    totalVitality += bonus.vitality;
                    totalIntellect += bonus.intellect;
                    bonusDamage += bonus.damage;
                    bonusArmor += bonus.armor;
                    bonusFire += bonus.fireResistance;
                    bonusCold += bonus.coldResistance;
                    bonusLightning += bonus.lightningResistance;
                    bonusAttackSpeed += bonus.attackSpeed;
                    bonusCritChance += bonus.critChance;
                    bonusMana += bonus.mana;
                    bonusMagicPower += bonus.magicPower;
                }
            }

            return BuildComputedFromResolvedTotals(
                totalStrength,
                totalAgility,
                totalVitality,
                totalIntellect,
                bonusDamage,
                bonusArmor,
                bonusFire,
                bonusCold,
                bonusLightning,
                bonusAttackSpeed,
                bonusCritChance,
                bonusMana,
                bonusMagicPower);
        }

        public static ComputedCharacterStats BuildComputed(CharacterPrimaryStats baseStats, FlatStatBonuses bonuses)
        {
            var safeBase = baseStats ?? CharacterPrimaryStats.CreateDefault();
            bonuses ??= new FlatStatBonuses();
            return BuildComputedFromResolvedTotals(
                safeBase.strength + bonuses.strength,
                safeBase.agility + bonuses.agility,
                safeBase.vitality + bonuses.vitality,
                safeBase.intellect + bonuses.intellect,
                bonuses.damage,
                bonuses.armor,
                bonuses.fireResistance,
                bonuses.coldResistance,
                bonuses.lightningResistance,
                bonuses.attackSpeed,
                bonuses.critChance,
                bonuses.mana,
                bonuses.magicPower);
        }

        private static ComputedCharacterStats BuildComputedFromResolvedTotals(
            int totalStrength,
            int totalAgility,
            int totalVitality,
            int totalIntellect,
            int bonusDamage,
            int bonusArmor,
            int bonusFire,
            int bonusCold,
            int bonusLightning,
            float bonusAttackSpeed,
            float bonusCritChance,
            int bonusMana,
            int bonusMagicPower)
        {
            return new ComputedCharacterStats
            {
                damage = BaseDamage + totalStrength + (totalAgility / 2) + bonusDamage,
                armor = BaseArmor + totalVitality + (totalStrength / 3) + bonusArmor,
                attackSpeed = BaseAttackSpeed + totalAgility * 0.01f + bonusAttackSpeed,
                critChance = UnityEngine.Mathf.Clamp01(BaseCritChance + totalAgility * 0.0025f + bonusCritChance),
                mana = BaseMana + totalIntellect * 5 + bonusMana,
                magicPower = totalIntellect * 2 + bonusMagicPower,
                elementalResists = new ElementalResistanceProfile
                {
                    fire = BaseElementalResistance + totalIntellect + (totalVitality / 3) + bonusFire,
                    cold = BaseElementalResistance + totalIntellect + (totalAgility / 3) + bonusCold,
                    lightning = BaseElementalResistance + totalIntellect + (totalAgility / 2) + bonusLightning
                }
            };
        }
    }
}
