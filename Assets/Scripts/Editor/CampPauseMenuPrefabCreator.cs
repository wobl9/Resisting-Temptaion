using ShatteredForge.UI;
using UnityEditor;
using UnityEngine;

namespace ShatteredForge.EditorTools
{
    /// <summary>
    /// Bakes the default pause menu UI to legacy and generic resource paths:
    /// <c>Assets/Resources/UI/CampPauseMenuUi.prefab</c> and <c>Assets/Resources/UI/PauseMenuUi.prefab</c>
    /// for runtime loading via <see cref="Resources"/>.
    /// </summary>
    public static class CampPauseMenuPrefabCreator
    {
        private const string LegacyPrefabPath = "Assets/Resources/UI/CampPauseMenuUi.prefab";
        private const string GenericPrefabPath = "Assets/Resources/UI/PauseMenuUi.prefab";

        [MenuItem("ShatteredForge/UI/Bake Camp Pause Menu UI Prefab", priority = 52)]
        public static void BakePrefab()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/UI");

            var legacyRoot = new GameObject("CampPauseMenuUi");
            var genericRoot = new GameObject("PauseMenuUi");
            try
            {
                var legacyView = legacyRoot.AddComponent<CampPauseMenuView>();
                legacyView.EnsureBuilt();
                legacyView.SetOpen(true);
                legacyView.ShowMainPage();

                var genericView = genericRoot.AddComponent<PauseMenuView>();
                genericView.EnsureBuilt();
                genericView.SetOpen(true);
                genericView.ShowMainPage();

                PrefabUtility.SaveAsPrefabAsset(legacyRoot, LegacyPrefabPath);
                PrefabUtility.SaveAsPrefabAsset(genericRoot, GenericPrefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                var savedGeneric = AssetDatabase.LoadAssetAtPath<GameObject>(GenericPrefabPath);
                if (savedGeneric != null)
                {
                    EditorUtility.SetDirty(savedGeneric);
                    EditorGUIUtility.PingObject(savedGeneric);
                }

                Debug.Log($"{nameof(CampPauseMenuPrefabCreator)}: saved {LegacyPrefabPath} and {GenericPrefabPath}");
            }
            finally
            {
                Object.DestroyImmediate(legacyRoot);
                Object.DestroyImmediate(genericRoot);
            }
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
