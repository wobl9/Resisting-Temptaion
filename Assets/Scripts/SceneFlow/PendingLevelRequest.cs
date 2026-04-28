namespace ShatteredForge.SceneFlow
{
    /// <summary>
    /// In-memory handoff from camp portal to LevelScene.
    /// </summary>
    public static class PendingLevelRequest
    {
        public static string SelectedLevelId { get; private set; } = string.Empty;

        public static void SetSelected(string levelId)
        {
            SelectedLevelId = levelId?.Trim() ?? string.Empty;
        }

        public static void Reset()
        {
            SelectedLevelId = string.Empty;
        }

        public static bool TryConsume(out string levelId)
        {
            if (string.IsNullOrWhiteSpace(SelectedLevelId))
            {
                levelId = string.Empty;
                return false;
            }

            levelId = SelectedLevelId;
            SelectedLevelId = string.Empty;
            return true;
        }
    }
}
