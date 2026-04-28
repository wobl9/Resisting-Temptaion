namespace ShatteredForge.UI
{
    /// <summary>
    /// Generic pause menu view alias for cross-scene reuse.
    /// Keeps backward compatibility with existing CampPauseMenuView prefab/script.
    /// </summary>
    public sealed class PauseMenuView : CampPauseMenuView
    {
        public const string DefaultViewResourcesPath = "UI/PauseMenuUi";
        public const string LegacyViewResourcesPath = CampPauseMenuView.DefaultViewResourcesPath;
    }
}
