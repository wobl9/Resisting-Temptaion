using System;
using System.Collections.Generic;

namespace ShatteredForge.Core
{
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

    [Serializable]
    public class AccountState
    {
        public int forgeDust;
        public int emberCore;
        public int sigilToken;
        public int insuranceSeal;
        public int forgePityFailures;
        public List<string> unlockedNodes = new();
        public List<SkillInstance> skills = new();
        public List<ItemInstance> stash = new();
    }
}
