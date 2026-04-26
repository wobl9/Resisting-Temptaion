using System;
using System.Collections;
using ShatteredForge.Localization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShatteredForge.SceneFlow
{
    /// <summary>
    /// Lives in the loading scene: reads <see cref="PendingSceneLoad"/>, async-loads the target with a progress gate.
    /// </summary>
    public sealed class LoadingSceneController : MonoBehaviour
    {
        private static readonly Color BackdropColor = new Color(0.078431375f, 0.078431375f, 0.09019608f, 1f);

        private string _targetSceneName;
        private float _loadProgress;
        private string _errorMessage;
        private bool _finished;

        private void Start()
        {
            SceneNavigation.ResetBusy();
            _targetSceneName = PendingSceneLoad.TargetSceneName;
            PendingSceneLoad.Clear();

            if (string.IsNullOrWhiteSpace(_targetSceneName))
            {
                Debug.LogWarning($"{nameof(LoadingSceneController)}: no pending scene; returning to {SceneNames.DefaultMenu}.");
                SceneManager.LoadScene(SceneNames.DefaultMenu, LoadSceneMode.Single);
                return;
            }

            if (!IsSceneInBuildSettings(_targetSceneName))
            {
                _errorMessage =
                    $"Scene '{_targetSceneName}' is not in build settings. Open File → Build Settings and add it.";
                Debug.LogError(_errorMessage);
                return;
            }

            StartCoroutine(LoadTargetSceneCoroutine(_targetSceneName));
        }

        private void OnDestroy()
        {
            SceneNavigation.ResetBusy();
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.Repaint)
            {
                GUI.DrawTexture(
                    new Rect(0f, 0f, Screen.width, Screen.height),
                    Texture2D.whiteTexture,
                    ScaleMode.StretchToFill,
                    false,
                    0f,
                    BackdropColor,
                    0f,
                    0f);
            }

            if (!string.IsNullOrEmpty(_errorMessage))
            {
                DrawErrorUi();
                return;
            }

            if (_finished)
            {
                return;
            }

            if (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout)
            {
                Event.current.Use();
            }

            if (Event.current.type == EventType.Repaint)
            {
                DrawLoadingUi();
            }
        }

        private void DrawLoadingUi()
        {
            var dim = new Color(0f, 0f, 0f, 0.35f);
            var prevDepth = GUI.depth;
            GUI.depth = 5000;
            GUI.DrawTexture(
                new Rect(0f, 0f, Screen.width, Screen.height),
                Texture2D.whiteTexture,
                ScaleMode.StretchToFill,
                false,
                0f,
                dim,
                0f,
                0f);

            var title = LoadingCopy.LoadingTitle;
            var style = HeaderStyle();
            style.alignment = TextAnchor.MiddleCenter;
            var titleH = style.CalcHeight(new GUIContent(title), Screen.width);
            GUI.Label(new Rect(0f, Screen.height * 0.42f, Screen.width, titleH + 8f), title, style);

            var barW = Mathf.Min(480f, Screen.width - 80f);
            var barX = (Screen.width - barW) * 0.5f;
            var barY = Screen.height * 0.52f;
            var barRect = new Rect(barX, barY, barW, 12f);
            GUI.DrawTexture(
                barRect,
                Texture2D.whiteTexture,
                ScaleMode.StretchToFill,
                false,
                0f,
                new Color(0.15f, 0.15f, 0.18f, 0.9f),
                0f,
                0f);
            var inner = new Rect(barRect.x + 2f, barRect.y + 2f, (barRect.width - 4f) * _loadProgress, barRect.height - 4f);
            if (inner.width > 0.5f)
            {
                GUI.DrawTexture(
                    inner,
                    Texture2D.whiteTexture,
                    ScaleMode.StretchToFill,
                    false,
                    0f,
                    new Color(0.45f, 0.65f, 0.9f, 1f),
                    0f,
                    0f);
            }

            GUI.depth = prevDepth;
        }

        private void DrawErrorUi()
        {
            const float width = 520f;
            var x = (Screen.width - width) * 0.5f;
            var y = Screen.height * 0.35f;
            GUILayout.BeginArea(new Rect(x, y, width, 220f));
            var style = HeaderStyle();
            GUILayout.Label(LoadingCopy.ErrorTitle, style);
            GUILayout.Space(12f);
            GUILayout.TextArea(_errorMessage, GUILayout.Height(80f));
            GUILayout.Space(12f);
            if (GUILayout.Button(LoadingCopy.BackToMenu, GUILayout.Height(32f)))
            {
                SceneManager.LoadScene(SceneNames.DefaultMenu, LoadSceneMode.Single);
            }

            GUILayout.EndArea();
        }

        private static GUIStyle HeaderStyle()
        {
            var s = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold
            };
            return s;
        }

        private IEnumerator LoadTargetSceneCoroutine(string sceneName)
        {
            _loadProgress = 0f;
            yield return null;

            AsyncOperation op;
            try
            {
                op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            }
            catch (Exception ex)
            {
                _errorMessage = ex.Message;
                yield break;
            }

            if (op == null)
            {
                _errorMessage = $"LoadSceneAsync returned null for '{sceneName}'.";
                yield break;
            }

            op.allowSceneActivation = false;
            while (op.progress < 0.9f)
            {
                _loadProgress = Mathf.Clamp01(op.progress / 0.9f);
                yield return null;
            }

            _loadProgress = 1f;
            yield return null;
            yield return RunOptionalWarmup();
            op.allowSceneActivation = true;
            yield return op;
            _finished = true;
        }

        /// <summary>
        /// Hook for warmup (Addressables, shaders, etc.) after load reaches 0.9 but before activation.
        /// </summary>
        private static IEnumerator RunOptionalWarmup()
        {
            yield break;
        }

        private static bool IsSceneInBuildSettings(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return false;
            }

            for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var path = SceneUtility.GetScenePathByBuildIndex(i);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                var name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (string.Equals(name, sceneName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static class LoadingCopy
        {
            public static string LoadingTitle =>
                PreferCyrillicUi() ? "Загрузка..." : "Loading...";

            public static string ErrorTitle =>
                PreferCyrillicUi() ? "Ошибка загрузки сцены" : "Scene load error";

            public static string BackToMenu =>
                PreferCyrillicUi() ? "В меню" : "Back to menu";

            private static bool PreferCyrillicUi() => LocalePreferencePreview.PreferCyrillicUi();
        }
    }
}
