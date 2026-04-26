namespace ShatteredForge.SceneFlow
{
    /// <summary>
    /// In-memory handoff: camp requested gameplay after the loading scene (PlayerPrefs can be stale across editor sessions).
    /// </summary>
    public static class PendingCampDungeonRequest
    {
        private static bool _pending;

        public static void Set()
        {
            _pending = true;
        }

        public static void Reset()
        {
            _pending = false;
        }

        public static bool Consume()
        {
            if (!_pending)
            {
                return false;
            }

            _pending = false;
            return true;
        }
    }
}
