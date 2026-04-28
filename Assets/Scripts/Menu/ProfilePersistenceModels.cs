using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShatteredForge.Menu
{
    [Serializable]
    public class ProfileSummary
    {
        public string id;
        public string displayName;
        public string createdAtUtc;
        public string lastPlayedAtUtc;
    }

    [Serializable]
    internal class ProfileIndexData
    {
        public string activeProfileId;
        public List<ProfileSummary> profiles = new();
    }

    [Serializable]
    public class ProfileData
    {
        public string profileId;
        public string profileName;

        /// <summary>Mirror of account gold when <c>accountJson</c> is empty; overwritten on save from account.</summary>
        public int gold = 20;

        /// <summary>One-time migration for saves created before <c>AccountState.gold</c> existed (JSON omitted field → 0).</summary>
        public bool accountGoldMigrated;

        public int forgeDust = 2500;
        public int emberCore = 5;
        public int sigilToken = 20;
        public int insuranceSeal = 1;
        public string createdAtUtc;
        public string updatedAtUtc;

        /// <summary>
        /// Monotonic revision for optimistic concurrency (local increments on save; server may overwrite).
        /// </summary>
        public int profileRevision;

        // Serialized gameplay account (stash/currencies/skills/etc).
        public string accountJson = string.Empty;

        // Expedition (in-run) persistence.
        public bool hasActiveExpedition;
        public int expeditionSchemaVersion = 1;
        public int expeditionDemoState; // maps to PlayableLoopDemo.DemoState enum int
        public int expeditionRunSeed;
        public int expeditionRoomIndex;
        public float expeditionHpState = 1f;
        public int expeditionMinRoomsPerAct = 8;
        public int expeditionMaxRoomsPerAct = 14;
        public int expeditionStartingHpPercent = 100;
        public bool expeditionAutoInsureFirstItem = true;
        public int expeditionRoomTypesCount;
        public int[] expeditionRoomTypes = Array.Empty<int>();

        // Full serialized RunState for expedition resume fidelity.
        public string expeditionRunJson = string.Empty;

        // Mission-level mode progression (separate from expedition fields above).
        public string lastPlayedLevelId = string.Empty;
        public List<string> clearedLevelIds = new();
    }
}
