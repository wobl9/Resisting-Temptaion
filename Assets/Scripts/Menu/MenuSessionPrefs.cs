namespace ShatteredForge.Menu
{
    public static class MenuSessionPrefs
    {
        public const string ActiveProfileIdKey = "sf.active_profile_id";
        public const string ResumeExpeditionKey = "sf.resume_expedition";

        /// <summary>
        /// Set by camp hub before loading gameplay; consumed by <c>PlayableLoopDemo</c> to start a fresh run.
        /// </summary>
        public const string PendingDungeonEntryKey = "sf.pending_dungeon_entry";
    }
}
