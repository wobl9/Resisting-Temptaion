using System;
using System.Collections.Generic;

namespace ShatteredForge.Core
{
    /// <summary>
    /// Camp-only equip: move items between <see cref="AccountState.stash"/> and <see cref="AccountState.characterPaperDoll"/>.
    /// </summary>
    public static class CharacterPaperDoll
    {
        /// <summary>
        /// JsonUtility / older saves may leave lists null — normalize before use.
        /// </summary>
        public static void EnsureList(AccountState account)
        {
            if (account == null)
            {
                return;
            }

            if (account.stash == null)
            {
                account.stash = new List<ItemInstance>();
            }

            if (account.skills == null)
            {
                account.skills = new List<SkillInstance>();
            }

            if (account.unlockedNodes == null)
            {
                account.unlockedNodes = new List<string>();
            }

            if (account.characterPaperDoll == null)
            {
                account.characterPaperDoll = new List<CharacterPaperDollRow>();
            }

            CharacterStatsService.EnsureInitialized(account);
        }

        public static bool HasAnyEquippedItem(AccountState account)
        {
            EnsureList(account);
            if (account?.characterPaperDoll == null)
            {
                return false;
            }

            foreach (var row in account.characterPaperDoll)
            {
                if (row?.item != null && !string.IsNullOrEmpty(row.item.templateId))
                {
                    return true;
                }
            }

            return false;
        }

        public static ItemInstance GetEquipped(AccountState account, EquipmentBodySlot slot)
        {
            EnsureList(account);
            var key = SlotKey(slot);
            foreach (var row in account.characterPaperDoll)
            {
                if (row != null && row.slotId == key && row.item != null && !string.IsNullOrEmpty(row.item.templateId))
                {
                    return row.item;
                }
            }

            return null;
        }

        public static bool TryUnequipSlotToStash(AccountState account, EquipmentBodySlot slot)
        {
            EnsureList(account);
            if (account?.stash == null)
            {
                return false;
            }

            var key = SlotKey(slot);
            for (var i = 0; i < account.characterPaperDoll.Count; i++)
            {
                var row = account.characterPaperDoll[i];
                if (row == null || row.slotId != key || row.item == null || string.IsNullOrEmpty(row.item.templateId))
                {
                    continue;
                }

                PutIntoFirstFreeStashSlot(account.stash, row.item);
                account.characterPaperDoll.RemoveAt(i);
                CharacterStatsService.RecalculateForCamp(account);
                return true;
            }

            return false;
        }

        public static bool TryEquipFromStashToSlot(AccountState account, int stashIndex, EquipmentBodySlot slot)
        {
            EnsureList(account);
            if (account?.stash == null || stashIndex < 0 || stashIndex >= account.stash.Count)
            {
                return false;
            }

            var item = account.stash[stashIndex];
            if (item == null || string.IsNullOrEmpty(item.templateId))
            {
                return false;
            }

            if (!CampItemSlotRules.CanWearInBodySlot(item.templateId, slot))
            {
                return false;
            }

            account.stash[stashIndex] = CreateEmptyStashSlot();

            var key = SlotKey(slot);
            for (var i = 0; i < account.characterPaperDoll.Count; i++)
            {
                var row = account.characterPaperDoll[i];
                if (row == null || row.slotId != key)
                {
                    continue;
                }

                if (row.item != null && !string.IsNullOrEmpty(row.item.templateId))
                {
                    PutIntoFirstFreeStashSlot(account.stash, row.item);
                }

                row.item = item;
                CharacterStatsService.RecalculateForCamp(account);
                return true;
            }

            account.characterPaperDoll.Add(new CharacterPaperDollRow { slotId = key, item = item });
            CharacterStatsService.RecalculateForCamp(account);
            return true;
        }

        public static bool TryMoveOrSwapEquippedToStashCell(AccountState account, EquipmentBodySlot slot, int targetStashIndex)
        {
            EnsureList(account);
            if (account?.stash == null || targetStashIndex < 0)
            {
                return false;
            }

            var key = SlotKey(slot);
            var rowIndex = -1;
            for (var i = 0; i < account.characterPaperDoll.Count; i++)
            {
                var row = account.characterPaperDoll[i];
                if (row == null || row.slotId != key || row.item == null || string.IsNullOrWhiteSpace(row.item.templateId))
                {
                    continue;
                }

                rowIndex = i;
                break;
            }

            if (rowIndex < 0)
            {
                return false;
            }

            EnsureStashCapacity(account.stash, targetStashIndex);
            var equipped = account.characterPaperDoll[rowIndex].item;
            var target = account.stash[targetStashIndex];
            var hasTargetItem = target != null && !string.IsNullOrWhiteSpace(target.templateId);

            if (!hasTargetItem)
            {
                account.stash[targetStashIndex] = equipped;
                account.characterPaperDoll.RemoveAt(rowIndex);
                CharacterStatsService.RecalculateForCamp(account);
                return true;
            }

            if (!CampItemSlotRules.CanWearInBodySlot(target.templateId, slot))
            {
                return false;
            }

            account.stash[targetStashIndex] = equipped;
            account.characterPaperDoll[rowIndex].item = target;
            CharacterStatsService.RecalculateForCamp(account);
            return true;
        }

        public static void MoveAllPaperDollToRun(AccountState account, RunState run)
        {
            if (account?.characterPaperDoll == null || run?.equippedLoadout == null)
            {
                return;
            }

            for (var i = account.characterPaperDoll.Count - 1; i >= 0; i--)
            {
                var row = account.characterPaperDoll[i];
                if (row?.item == null || string.IsNullOrEmpty(row.item.templateId))
                {
                    continue;
                }

                run.equippedLoadout.Add(row.item);
                account.characterPaperDoll.RemoveAt(i);
            }
        }

        private static string SlotKey(EquipmentBodySlot slot)
        {
            return slot.ToString();
        }

        private static void PutIntoFirstFreeStashSlot(List<ItemInstance> stash, ItemInstance item)
        {
            if (stash == null || item == null || string.IsNullOrWhiteSpace(item.templateId))
            {
                return;
            }

            for (var i = 0; i < stash.Count; i++)
            {
                var slot = stash[i];
                if (slot == null || string.IsNullOrWhiteSpace(slot.templateId))
                {
                    stash[i] = item;
                    return;
                }
            }

            stash.Add(item);
        }

        private static void EnsureStashCapacity(List<ItemInstance> stash, int targetIndex)
        {
            if (stash == null)
            {
                return;
            }

            while (stash.Count <= targetIndex)
            {
                stash.Add(CreateEmptyStashSlot());
            }
        }

        private static ItemInstance CreateEmptyStashSlot()
        {
            return new ItemInstance
            {
                id = string.Empty,
                templateId = string.Empty,
                rarity = string.Empty,
                enhanceLevel = 0,
                baseDamage = 0f,
                baseArmor = 0f
            };
        }

    }
}
