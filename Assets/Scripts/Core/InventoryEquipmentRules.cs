using System;
using System.Collections.Generic;
using ShatteredForge.Items;

namespace ShatteredForge.Core
{
    public enum ItemEquipmentKind
    {
        None,
        Weapon,
        Armor
    }

    /// <summary>
    /// Equip rules: prefer <see cref="ItemCatalogRuntime.Current"/> entry for <see cref="ItemInstance.templateId"/>;
    /// otherwise <c>weapon_</c> / <c>armor_</c> prefix fallback (legacy saves).
    /// </summary>
    public static class InventoryEquipmentRules
    {
        private const string StarterWeaponTemplateId = "weapon_simple_sword";
        private const string StarterArmorTemplateId = "armor_simple_chest";

        public static ItemEquipmentKind Classify(string templateId)
        {
            if (string.IsNullOrEmpty(templateId))
            {
                return ItemEquipmentKind.None;
            }

            var catalog = ItemCatalogRuntime.Current;
            if (catalog != null &&
                catalog.TryGet(templateId, out var entry) &&
                entry.equipKind != ItemEquipmentKind.None)
            {
                return entry.equipKind;
            }

            return ClassifyByPrefix(templateId);
        }

        private static ItemEquipmentKind ClassifyByPrefix(string templateId)
        {
            if (templateId.StartsWith("weapon_", StringComparison.Ordinal))
            {
                return ItemEquipmentKind.Weapon;
            }

            if (templateId.StartsWith("armor_", StringComparison.Ordinal))
            {
                return ItemEquipmentKind.Armor;
            }

            return ItemEquipmentKind.None;
        }

        /// <summary>
        /// True if an expedition may start. Starter simple sword + chest are created on the run if missing
        /// (see <see cref="PopulateStartingLoadout"/>), so an empty stash is allowed.
        /// </summary>
        public static bool AccountCanStartRun(AccountState account)
        {
            if (account == null)
            {
                return false;
            }

            CharacterPaperDoll.EnsureList(account);
            return true;
        }

        /// <summary>
        /// Moves camp paper doll into <paramref name="run"/>, then pulls missing weapon/armor from stash, then <c>stash[0]</c> if run still empty.
        /// Finally normalizes the run to <c>weapon_simple_sword</c> + <c>armor_simple_chest</c> equipped (other weapons/armor go to stash; missing pieces are taken from stash or created).
        /// Insurance applies only to the first item actually moved from the doll / stash pull phase (before normalization).
        /// </summary>
        public static bool PopulateStartingLoadout(
            RunState run,
            AccountState account,
            bool autoInsureFirstItem,
            ref int insuranceSealCount)
        {
            if (run?.equippedLoadout == null || account == null)
            {
                return false;
            }

            CharacterPaperDoll.EnsureList(account);
            if (account.stash == null)
            {
                return false;
            }

            run.equippedLoadout.Clear();

            var moved = 0;
            CharacterPaperDoll.MoveAllPaperDollToRun(account, run);
            for (var i = 0; i < run.equippedLoadout.Count; i++)
            {
                ApplyInsuranceForFirstMove(moved, run.equippedLoadout[i], autoInsureFirstItem, ref insuranceSealCount);
                moved++;
            }

            if (!RunContainsKind(run.equippedLoadout, ItemEquipmentKind.Weapon))
            {
                TryPullFirstOfKind(account.stash, run.equippedLoadout, ItemEquipmentKind.Weapon, autoInsureFirstItem, ref insuranceSealCount, ref moved);
            }

            if (!RunContainsKind(run.equippedLoadout, ItemEquipmentKind.Armor))
            {
                TryPullFirstOfKind(account.stash, run.equippedLoadout, ItemEquipmentKind.Armor, autoInsureFirstItem, ref insuranceSealCount, ref moved);
            }

            if (run.equippedLoadout.Count == 0 && account.stash.Count > 0)
            {
                var item = account.stash[0];
                account.stash.RemoveAt(0);
                ApplyInsuranceForFirstMove(moved, item, autoInsureFirstItem, ref insuranceSealCount);
                run.equippedLoadout.Add(item);
                moved++;
            }

            EnsureStarterSimpleWeaponAndArmorEquipped(run, account);
            CharacterStatsService.RecalculateForRun(account, run);
            return RunHasTemplateEquipped(run, StarterWeaponTemplateId) &&
                   RunHasTemplateEquipped(run, StarterArmorTemplateId);
        }

        private static bool RunHasTemplateEquipped(RunState run, string templateId)
        {
            for (var i = 0; i < run.equippedLoadout.Count; i++)
            {
                if (run.equippedLoadout[i] != null && run.equippedLoadout[i].templateId == templateId)
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureStarterSimpleWeaponAndArmorEquipped(RunState run, AccountState account)
        {
            EnsureStarterTemplateEquipped(run, account, StarterWeaponTemplateId, ItemEquipmentKind.Weapon);
            EnsureStarterTemplateEquipped(run, account, StarterArmorTemplateId, ItemEquipmentKind.Armor);
        }

        private static void EnsureStarterTemplateEquipped(
            RunState run,
            AccountState account,
            string templateId,
            ItemEquipmentKind kind)
        {
            DedupeExactTemplateKeepFirst(run, account, templateId);

            for (var i = run.equippedLoadout.Count - 1; i >= 0; i--)
            {
                var it = run.equippedLoadout[i];
                if (it == null || string.IsNullOrEmpty(it.templateId))
                {
                    continue;
                }

                if (Classify(it.templateId) == kind && it.templateId != templateId)
                {
                    run.equippedLoadout.RemoveAt(i);
                    account.stash.Add(it);
                }
            }

            if (RunHasTemplateEquipped(run, templateId))
            {
                return;
            }

            for (var i = 0; i < account.stash.Count; i++)
            {
                var it = account.stash[i];
                if (it != null && it.templateId == templateId)
                {
                    account.stash.RemoveAt(i);
                    run.equippedLoadout.Add(it);
                    return;
                }
            }

            run.equippedLoadout.Add(CreateStarterItemInstance(templateId));
        }

        private static void DedupeExactTemplateKeepFirst(RunState run, AccountState account, string templateId)
        {
            var kept = false;
            for (var i = 0; i < run.equippedLoadout.Count; i++)
            {
                var it = run.equippedLoadout[i];
                if (it == null || it.templateId != templateId)
                {
                    continue;
                }

                if (!kept)
                {
                    kept = true;
                    continue;
                }

                run.equippedLoadout.RemoveAt(i);
                account.stash.Add(it);
                i--;
            }
        }

        private static ItemInstance CreateStarterItemInstance(string templateId)
        {
            return new ItemInstance
            {
                id = Guid.NewGuid().ToString(),
                templateId = templateId,
                rarity = "Обычная",
                enhanceLevel = 0,
                isInsuredForRun = false
            };
        }

        private static bool RunContainsKind(IReadOnlyList<ItemInstance> equipped, ItemEquipmentKind kind)
        {
            for (var i = 0; i < equipped.Count; i++)
            {
                if (Classify(equipped[i].templateId) == kind)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryEquipFromStashIndex(AccountState account, RunState run, int stashIndex)
        {
            if (account?.stash == null || run?.equippedLoadout == null)
            {
                return false;
            }

            if (stashIndex < 0 || stashIndex >= account.stash.Count)
            {
                return false;
            }

            var item = account.stash[stashIndex];
            var kind = Classify(item.templateId);
            if (kind == ItemEquipmentKind.None)
            {
                return false;
            }

            account.stash.RemoveAt(stashIndex);

            var swap = FindEquippedIndexByKind(run.equippedLoadout, kind);
            if (swap >= 0)
            {
                var old = run.equippedLoadout[swap];
                run.equippedLoadout.RemoveAt(swap);
                account.stash.Add(old);
            }

            run.equippedLoadout.Add(item);
            CharacterStatsService.RecalculateForRun(account, run);
            return true;
        }

        public static bool TryUnequipIndex(AccountState account, RunState run, int equippedIndex)
        {
            if (account?.stash == null || run?.equippedLoadout == null)
            {
                return false;
            }

            if (equippedIndex < 0 || equippedIndex >= run.equippedLoadout.Count)
            {
                return false;
            }

            var item = run.equippedLoadout[equippedIndex];
            run.equippedLoadout.RemoveAt(equippedIndex);
            account.stash.Add(item);
            CharacterStatsService.RecalculateForRun(account, run);
            return true;
        }

        private static void TryPullFirstOfKind(
            List<ItemInstance> stash,
            List<ItemInstance> equipped,
            ItemEquipmentKind kind,
            bool autoInsureFirstItem,
            ref int insuranceSealCount,
            ref int movedCount)
        {
            for (var i = 0; i < stash.Count; i++)
            {
                if (Classify(stash[i].templateId) != kind)
                {
                    continue;
                }

                var item = stash[i];
                stash.RemoveAt(i);
                ApplyInsuranceForFirstMove(movedCount, item, autoInsureFirstItem, ref insuranceSealCount);
                equipped.Add(item);
                movedCount++;
                return;
            }
        }

        private static void ApplyInsuranceForFirstMove(
            int movedCountBefore,
            ItemInstance item,
            bool autoInsureFirstItem,
            ref int insuranceSealCount)
        {
            if (movedCountBefore == 0 && autoInsureFirstItem && insuranceSealCount > 0)
            {
                item.isInsuredForRun = true;
                insuranceSealCount--;
            }
            else
            {
                item.isInsuredForRun = false;
            }
        }

        private static int FindEquippedIndexByKind(IReadOnlyList<ItemInstance> equipped, ItemEquipmentKind kind)
        {
            for (var i = 0; i < equipped.Count; i++)
            {
                if (Classify(equipped[i].templateId) == kind)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
