using System;
using System.Collections.Generic;

namespace ShatteredForge.Core
{
    /// <summary>
    /// Serializable item stack row: stable for JSON saves and future server sync (see item catalog by <see cref="templateId"/>).
    /// </summary>
    [Serializable]
    public class ItemInstance
    {
        public string id;
        public string templateId;
        public string rarity;
        public int enhanceLevel;
        public List<string> affixes = new();
        public List<string> sockets = new();
        public bool isInsuredForRun;
    }

    [Serializable]
    public class SkillInstance
    {
        public string id;
        public string baseSkillId;
        public int masteryLevel;
        public int sharpenRank;
        public List<string> equippedRunes = new();
    }

    [Serializable]
    public sealed class CharacterPrimaryStats
    {
        public int strength;
        public int agility;
        public int vitality;
        public int intellect;

        public static CharacterPrimaryStats CreateDefault()
        {
            return new CharacterPrimaryStats
            {
                strength = 10,
                agility = 10,
                vitality = 10,
                intellect = 10
            };
        }
    }

    [Serializable]
    public sealed class ElementalResistanceProfile
    {
        public int fire;
        public int cold;
        public int lightning;
    }

    [Serializable]
    public sealed class ComputedCharacterStats
    {
        public int damage;
        public int armor;
        public float attackSpeed;
        public float critChance;
        public int mana;
        public int magicPower;
        public ElementalResistanceProfile elementalResists = new();
    }

    [Serializable]
    public sealed class FlatStatBonuses
    {
        public int strength;
        public int agility;
        public int vitality;
        public int intellect;
        public int damage;
        public int armor;
        public int fireResistance;
        public int coldResistance;
        public int lightningResistance;
        public float attackSpeed;
        public float critChance;
        public int mana;
        public int magicPower;
    }

    [Serializable]
    public class RunState
    {
        public int seed;
        public int actIndex;
        public int roomIndex;
        public float hpState;
        public bool extractionStatus;
        public List<ItemInstance> equippedLoadout = new();
        public List<ItemInstance> carryLoot = new();
    }

    /// <summary>
    /// One occupied paper-doll slot on <see cref="AccountState.characterPaperDoll"/> (JsonUtility-friendly).
    /// </summary>
    [Serializable]
    public sealed class CharacterPaperDollRow
    {
        public string slotId;
        public ItemInstance item;
    }

    [Serializable]
    public class AccountState
    {
        /// <summary>Vendor / shop currency (persisted in <c>accountJson</c>).</summary>
        public int gold;

        public int forgeDust;
        public int emberCore;
        public int sigilToken;
        public int insuranceSeal;
        public int forgePityFailures;
        public List<string> unlockedNodes = new();
        public List<SkillInstance> skills = new();
        public List<ItemInstance> stash = new();

        /// <summary>
        /// Camp / menu paper doll (between expeditions). Items here are not in <see cref="stash"/>.
        /// </summary>
        public List<CharacterPaperDollRow> characterPaperDoll = new();

        /// <summary>Persisted base stats (saved in <c>accountJson</c>).</summary>
        public CharacterPrimaryStats primaryStats = CharacterPrimaryStats.CreateDefault();

        /// <summary>Runtime-only derived stats, recalculated from base stats + gear.</summary>
        [NonSerialized] public ComputedCharacterStats computedStats = new();
    }
}
