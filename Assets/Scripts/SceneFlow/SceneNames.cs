namespace ShatteredForge.SceneFlow
{
    /// <summary>
    /// Build-time scene names (must match entries in Editor Build Settings).
    /// </summary>
    public static class SceneNames
    {
        /// <summary>
        /// Cold-start splash (build index 0). Not used for in-game transitions.
        /// </summary>
        public const string Boot = "Boot";

        public const string Loading = "Loading";

        /// <summary>
        /// RU: «лагерь» — сцена подготовки к вылазке перед подземельем (файл <c>CampHub.unity</c>, имя сцены в билде).
        /// </summary>
        public const string CampHub = "CampHub";

        /// <summary>
        /// First menu scene used when the loading scene has no valid pending target.
        /// </summary>
        public const string DefaultMenu = "SampleScene";
    }
}
