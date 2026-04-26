using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShatteredForge.SceneFlow
{
    /// <summary>
    /// Single entry point for switching scenes through the lightweight loading scene.
    /// </summary>
    public static class SceneNavigation
    {
        private static bool _busy;

        public static bool IsBusy => _busy;

        /// <summary>
        /// Clears the busy flag (e.g. after loading scene finishes or aborts). Prefer calling from <see cref="LoadingSceneController"/>.
        /// </summary>
        public static void ResetBusy()
        {
            _busy = false;
        }

        /// <summary>
        /// Stores <paramref name="targetSceneName"/> and loads the loading scene in single mode.
        /// </summary>
        public static void GoTo(string targetSceneName)
        {
            GoTo(targetSceneName, SceneNames.Loading);
        }

        /// <summary>
        /// Same as <see cref="GoTo(string)"/> but allows overriding the loading scene name (tests / tooling).
        /// </summary>
        public static void GoTo(string targetSceneName, string loadingSceneName)
        {
            if (_busy)
            {
                Debug.LogWarning($"{nameof(SceneNavigation)}.{nameof(GoTo)} ignored: navigation already in progress.");
                return;
            }

            if (string.IsNullOrWhiteSpace(targetSceneName))
            {
                Debug.LogError($"{nameof(SceneNavigation)}.{nameof(GoTo)}: target scene name is empty.");
                return;
            }

            if (string.IsNullOrWhiteSpace(loadingSceneName))
            {
                Debug.LogError($"{nameof(SceneNavigation)}.{nameof(GoTo)}: loading scene name is empty.");
                return;
            }

            _busy = true;
            PendingSceneLoad.TargetSceneName = targetSceneName.Trim();
            SceneManager.LoadScene(loadingSceneName.Trim(), LoadSceneMode.Single);
        }
    }
}
