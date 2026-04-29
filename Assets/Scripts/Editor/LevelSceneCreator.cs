using ShatteredForge.Combat;
using ShatteredForge.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ShatteredForge.EditorTools
{
    public static class LevelSceneCreator
    {
        private const string ScenePath = "Assets/Scenes/LevelScene.unity";

        [MenuItem("Shattered Forge/Scenes/Create Level Scene", priority = 31)]
        public static void CreateLevelScene()
        {
            EnsureFolder("Assets/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("LevelBootstrap");
            root.AddComponent<LevelSessionController>();
            root.AddComponent<CombatRoomBootstrap>();
            root.AddComponent<ShatteredForge.UI.PlayerInventoryPanel>();

            var saved = EditorSceneManager.SaveScene(scene, ScenePath);
            if (!saved)
            {
                Debug.LogError($"{nameof(LevelSceneCreator)}: failed to save {ScenePath}.");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EnsureSceneInBuildSettings(ScenePath);
            Debug.Log($"{nameof(LevelSceneCreator)}: created {ScenePath}");
        }

        private static void EnsureSceneInBuildSettings(string scenePath)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (var i = 0; i < scenes.Count; i++)
            {
                if (!string.Equals(scenes[i].path, scenePath, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                scenes[i].enabled = true;
                EditorBuildSettings.scenes = scenes.ToArray();
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = System.IO.Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
