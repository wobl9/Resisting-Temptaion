namespace ShatteredForge.SceneFlow
{
    /// <summary>
    /// Holds the next scene to load after <see cref="SceneNames.Loading"/> activates.
    /// Cleared by <see cref="LoadingSceneController"/> once consumed.
    /// </summary>
    public static class PendingSceneLoad
    {
        public static string TargetSceneName { get; set; }

        public static void Clear()
        {
            TargetSceneName = null;
        }
    }
}
