using System.IO;
using System.Text;
using UnityEngine;

namespace ShatteredForge.Menu
{
    /// <summary>
    /// Lightweight, runtime-safe smoke checks for menu contract assumptions.
    /// Intended for MCP `execute_code` (CodeDom) and quick CI-like sanity checks.
    /// </summary>
    public static class MenuContractSmokeTestRunner
    {
        public const string DefaultSceneAssetPath = "Assets/Scenes/SampleScene.unity";

        public static string RunDefault()
        {
            return Run(DefaultSceneAssetPath);
        }

        public static string Run(string sceneAssetPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("MenuContractSmokeTestRunner");
            sb.AppendLine("- sceneAssetPath: " + sceneAssetPath);

            var profiles = new ProfileStorageService();
            var snapshot = MenuContractAssertions.BuildSnapshot(profiles);

            sb.AppendLine("- profileCount: " + snapshot.profileCount);
            sb.AppendLine("- hasActiveProfileId: " + snapshot.hasActiveProfileId);
            sb.AppendLine("- hasActiveExpeditionForActiveProfile: " + snapshot.hasActiveExpeditionForActiveProfile);
            sb.AppendLine("- expectFirstLaunchThreeButtonLayout: " + snapshot.expectFirstLaunchThreeButtonLayout);
            sb.AppendLine("- expectProfilePicker: " + snapshot.expectProfilePicker);
            sb.AppendLine("- expectContinueVisible: " + snapshot.expectContinueVisible);
            sb.AppendLine("- expectProfileEntryVisible: " + snapshot.expectProfileEntryVisible);

            if (!File.Exists(sceneAssetPath))
            {
                sb.AppendLine("ISSUE: scene asset file not found on disk (expected Unity project-relative path).");
                return sb.ToString();
            }

            var yaml = File.ReadAllText(sceneAssetPath);
            if (yaml.IndexOf("MainMenuController") < 0)
            {
                sb.AppendLine("ISSUE: SampleScene YAML does not reference MainMenuController (script not attached?).");
            }
            else
            {
                sb.AppendLine("OK: SampleScene references MainMenuController.");
            }

            // PlayerPrefs writer roundtrip (should not persist after test)
            const string tempProfile = "__smoke_profile__";
            MenuSessionWriter.WriteGameplayLaunchIntent(tempProfile, true);
            var idOk = PlayerPrefs.GetString(MenuSessionPrefs.ActiveProfileIdKey, string.Empty) == tempProfile;
            var resumeOk = PlayerPrefs.GetInt(MenuSessionPrefs.ResumeExpeditionKey, 0) == 1;
            MenuSessionWriter.ClearResumeIntent();
            var resumeCleared = !PlayerPrefs.HasKey(MenuSessionPrefs.ResumeExpeditionKey);
            PlayerPrefs.DeleteKey(MenuSessionPrefs.ActiveProfileIdKey);
            PlayerPrefs.Save();

            if (!idOk || !resumeOk || !resumeCleared)
            {
                sb.AppendLine("ISSUE: MenuSessionWriter prefs roundtrip failed (idOk=" + idOk + ", resumeOk=" + resumeOk + ", resumeCleared=" + resumeCleared + ").");
            }
            else
            {
                sb.AppendLine("OK: MenuSessionWriter prefs roundtrip.");
            }

            return sb.ToString();
        }
    }
}
