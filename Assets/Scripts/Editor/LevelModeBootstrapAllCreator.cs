using UnityEditor;
using UnityEngine;

namespace ShatteredForge.EditorTools
{
    public static class LevelModeBootstrapAllCreator
    {
        [MenuItem("ShatteredForge/Levels/Bootstrap All (Scene + Content + Pause Prefabs)", priority = 30)]
        public static void BootstrapAll()
        {
            LevelSceneCreator.CreateLevelScene();
            LevelContentBootstrapCreator.BootstrapDemoContent();
            CampPauseMenuPrefabCreator.BakePrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"{nameof(LevelModeBootstrapAllCreator)}: finished bootstrapping level mode assets.");
        }
    }
}
