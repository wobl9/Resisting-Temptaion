using System.Collections.Generic;
using System.Text;
using ShatteredForge.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShatteredForge.EditorTools
{
    public static class MenuContractSmokeTest
    {
        private const string TargetScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Shattered Forge/QA/Run Menu Contract Smoke Test (SampleScene)")]
        public static void RunFromMenu()
        {
            var report = Run();
            Debug.Log(report);
        }

        public static string Run()
        {
            var previousScenePath = SceneManager.GetActiveScene().path;

            EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);

            var menu = Object.FindFirstObjectByType<MainMenuController>();
            var issues = new List<string>();

            if (menu == null)
            {
                issues.Add($"Scene '{TargetScenePath}' does not contain a {nameof(MainMenuController)}.");
            }

            var runnerReport = MenuContractSmokeTestRunner.Run(TargetScenePath);
            if (!string.IsNullOrEmpty(previousScenePath))
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            }

            var sb = new StringBuilder();
            sb.AppendLine("Menu contract smoke test (SampleScene)");
            sb.AppendLine($"- targetScene: {TargetScenePath}");
            sb.AppendLine($"- mainMenuPresent: {menu != null}");
            sb.AppendLine($"- issues: {issues.Count}");

            foreach (var issue in issues)
            {
                sb.AppendLine($"  - {issue}");
            }

            sb.AppendLine();
            sb.AppendLine("--- Runtime runner report ---");
            sb.AppendLine(runnerReport);

            return sb.ToString();
        }
    }
}
