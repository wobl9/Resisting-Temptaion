using ShatteredForge.UI;
using UnityEditor;
using UnityEngine;

namespace ShatteredForge.EditorTools
{
    /// <summary>
    /// Bakes the default camp pause menu UI to <c>Assets/Resources/UI/CampPauseMenuUi.prefab</c>
    /// for runtime loading via <see cref="Resources"/>.
    /// </summary>
    public static class CampPauseMenuPrefabCreator
    {
        private const string PrefabPath = "Assets/Resources/UI/CampPauseMenuUi.prefab";

        [MenuItem("ShatteredForge/UI/Bake Camp Pause Menu UI Prefab", priority = 52)]
        public static void BakePrefab()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/UI");

            var root = new GameObject("CampPauseMenuUi");
            try
            {
                var view = root.AddComponent<CampPauseMenuView>();
                view.EnsureBuilt();
                view.SetOpen(true);
                view.ShowMainPage();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                var saved = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                if (saved != null)
                {
                    EditorUtility.SetDirty(saved);
                    EditorGUIUtility.PingObject(saved);
                }

                Debug.Log($"{nameof(CampPauseMenuPrefabCreator)}: saved {PrefabPath}");
            }
            finally
            {
                Object.DestroyImmediate(root);
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
