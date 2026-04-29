using ShatteredForge.UI;
using UnityEditor;
using UnityEngine;

namespace ShatteredForge.EditorTools
{
    /// <summary>
    /// Bakes the default camp character sheet UI to <c>Assets/Resources/UI/CampCharacterSheetUi.prefab</c>
    /// so layout can be edited and loaded at runtime via <see cref="UnityEngine.Resources"/>.
    /// </summary>
    public static class CampCharacterSheetPrefabCreator
    {
        private const string PrefabPath = "Assets/Resources/UI/CampCharacterSheetUi.prefab";
        private const string ActiveLayoutEditorPref = "ShatteredForge.CampCharacterSheet.ActiveLayout";
        private const string ActiveSkinEditorPref = "ShatteredForge.CampCharacterSheet.ActiveSkin";

        [MenuItem("Shattered Forge/UI/Bake Camp Character Sheet UI Prefab (Default)", priority = 50)]
        public static void BakePrefabDefault()
        {
            BakePrefab(null);
        }

        [MenuItem("Shattered Forge/UI/Bake Camp Character Sheet UI Prefab (Using Active Layout)", priority = 51)]
        public static void BakePrefabUsingActiveLayout()
        {
            var layout = LoadActiveLayout();
            var skin = LoadActiveSkin();
            if (layout == null)
            {
                Debug.LogWarning($"{nameof(CampCharacterSheetPrefabCreator)}: active layout is not set.");
            }

            BakePrefab(layout, skin);
        }

        public static void SetActiveLayout(CampCharacterSheetLayoutAsset layout)
        {
            if (layout == null)
            {
                EditorPrefs.DeleteKey(ActiveLayoutEditorPref);
                return;
            }

            var path = AssetDatabase.GetAssetPath(layout);
            if (!string.IsNullOrEmpty(path))
            {
                EditorPrefs.SetString(ActiveLayoutEditorPref, path);
            }
        }

        public static CampCharacterSheetLayoutAsset LoadActiveLayout()
        {
            var path = EditorPrefs.GetString(ActiveLayoutEditorPref, string.Empty);
            return string.IsNullOrEmpty(path)
                ? null
                : AssetDatabase.LoadAssetAtPath<CampCharacterSheetLayoutAsset>(path);
        }

        public static void SetActiveSkin(CampCharacterSheetSkinAsset skin)
        {
            if (skin == null)
            {
                EditorPrefs.DeleteKey(ActiveSkinEditorPref);
                return;
            }

            var path = AssetDatabase.GetAssetPath(skin);
            if (!string.IsNullOrEmpty(path))
            {
                EditorPrefs.SetString(ActiveSkinEditorPref, path);
            }
        }

        public static CampCharacterSheetSkinAsset LoadActiveSkin()
        {
            var path = EditorPrefs.GetString(ActiveSkinEditorPref, string.Empty);
            return string.IsNullOrEmpty(path)
                ? null
                : AssetDatabase.LoadAssetAtPath<CampCharacterSheetSkinAsset>(path);
        }

        public static void BakePrefab(CampCharacterSheetLayoutAsset layoutAsset)
        {
            BakePrefab(layoutAsset, null);
        }

        public static void BakePrefab(CampCharacterSheetLayoutAsset layoutAsset, CampCharacterSheetSkinAsset skinAsset)
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/UI");

            var root = new GameObject("CampCharacterSheetUi");
            try
            {
                var view = root.AddComponent<CampCharacterSheetView>();
                if (layoutAsset != null)
                {
                    view.EditorSetLayoutAsset(layoutAsset);
                }

                if (skinAsset != null)
                {
                    view.EditorSetSkinAsset(skinAsset);
                }

                view.EditorBakeDefaultUiForPrefab();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                var saved = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                if (saved != null)
                {
                    EditorUtility.SetDirty(saved);
                    EditorGUIUtility.PingObject(saved);
                }

                Debug.Log($"{nameof(CampCharacterSheetPrefabCreator)}: saved {PrefabPath}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
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
