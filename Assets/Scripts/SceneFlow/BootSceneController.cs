using System;
using System.Collections;
using ShatteredForge.Localization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShatteredForge.SceneFlow
{
    /// <summary>
    /// Cold-start splash before the main menu: distinct visuals from <see cref="LoadingSceneController"/>.
    /// </summary>
    [DefaultExecutionOrder(-2000)]
    public sealed class BootSceneController : MonoBehaviour
    {
        [Header("Copy (no Unity Localization package on boot)")]
        [SerializeField] private string bootTitle = "SHATTERED FORGE";

        [SerializeField] private string subtitleRu = "Кузня пробуждается…";

        [SerializeField] private string subtitleEn = "The forge stirs…";

        [SerializeField] private string errorTitleRu = "Ошибка загрузки сцены";

        [SerializeField] private string errorTitleEn = "Scene load error";

        [SerializeField] private string backToMenuRu = "В меню";

        [SerializeField] private string backToMenuEn = "Back to menu";

        [Header("Colors")]
        [SerializeField] private Color topGradient = new Color(0.16f, 0.06f, 0.09f, 1f);

        [SerializeField] private Color bottomGradient = new Color(0.02f, 0.02f, 0.03f, 1f);

        [SerializeField] private Color accentGold = new Color(0.79f, 0.64f, 0.15f, 1f);

        [SerializeField] private Color mutedText = new Color(0.55f, 0.52f, 0.48f, 1f);

        [Header("Timing")]
        [SerializeField] [Min(0f)] private float minDisplaySeconds = 0.85f;

        [SerializeField] [Min(1f)] private float maxWaitSeconds = 45f;

        private string _errorMessage;
        private bool _finished;

        private int _cachedScreenW = -1;
        private int _cachedScreenH = -1;
        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _errorTitleStyle;
        private float _cachedTitleH;
        private float _cachedSubtitleH;

        private void Start()
        {
            SceneNavigation.ResetBusy();
            if (!IsSceneInBuildSettings(SceneNames.DefaultMenu))
            {
                _errorMessage =
                    $"Menu scene '{SceneNames.DefaultMenu}' is missing from build settings.";
                Debug.LogError(_errorMessage);
                return;
            }

            StartCoroutine(BootToMenuCoroutine());
        }

        private void OnDestroy()
        {
            SceneNavigation.ResetBusy();
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.Repaint)
            {
                DrawGradientBackdrop();
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
                DrawBootChrome();
            }
        }

        private void DrawGradientBackdrop()
        {
            var w = Screen.width;
            var h = Screen.height;
            var band = h / 3f;
            for (var i = 0; i < 3; i++)
            {
                var t = i == 0 ? 0.12f : i == 1 ? 0.5f : 0.88f;
                var c = Color.Lerp(topGradient, bottomGradient, t);
                GUI.DrawTexture(
                    new Rect(0f, i * band, w, band + 1f),
                    Texture2D.whiteTexture,
                    ScaleMode.StretchToFill,
                    false,
                    0f,
                    c,
                    0f,
                    0f);
            }
        }

        private void EnsureChromeLayout()
        {
            if (Screen.width == _cachedScreenW && Screen.height == _cachedScreenH && _titleStyle != null)
            {
                return;
            }

            _cachedScreenW = Screen.width;
            _cachedScreenH = Screen.height;

            var titleFont = Mathf.Clamp(34 + _cachedScreenW / 80, 28, 46);
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = titleFont,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = accentGold }
            };

            _subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = mutedText }
            };

            _errorTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = accentGold }
            };

            var sub = SubtitleForPreviewLocale();
            _cachedTitleH = _titleStyle.CalcHeight(new GUIContent(bootTitle), _cachedScreenW);
            _cachedSubtitleH = _subtitleStyle.CalcHeight(new GUIContent(sub), _cachedScreenW);
        }

        private void DrawBootChrome()
        {
            EnsureChromeLayout();

            var lineW = Mathf.Min(280f, Screen.width - 120f);
            var lineX = (Screen.width - lineW) * 0.5f;
            var lineY = Screen.height * 0.58f;
            GUI.DrawTexture(
                new Rect(lineX, lineY, lineW, 2f),
                Texture2D.whiteTexture,
                ScaleMode.StretchToFill,
                false,
                0f,
                accentGold * new Color(1f, 1f, 1f, 0.85f),
                0f,
                0f);

            GUI.Label(
                new Rect(0f, Screen.height * 0.36f, Screen.width, _cachedTitleH + 12f),
                bootTitle,
                _titleStyle);

            var sub = SubtitleForPreviewLocale();
            GUI.Label(
                new Rect(0f, Screen.height * 0.36f + _cachedTitleH + 18f, Screen.width, _cachedSubtitleH + 8f),
                sub,
                _subtitleStyle);

            DrawOrbitalDots();
        }

        private string SubtitleForPreviewLocale()
        {
            return LocalePreferencePreview.PreferCyrillicUi() ? subtitleRu : subtitleEn;
        }

        private static void DrawOrbitalDots()
        {
            var cx = Screen.width * 0.5f;
            var cy = Screen.height * 0.74f;
            const float orbitR = 26f;
            const float dotSize = 7f;
            var t = Time.unscaledTime;

            for (var i = 0; i < 6; i++)
            {
                var a = (float)(t * 1.65 + i * (Mathf.PI * 2f / 6f));
                var px = cx + Mathf.Cos(a) * orbitR - dotSize * 0.5f;
                var py = cy + Mathf.Sin(a) * orbitR - dotSize * 0.5f;
                var pulse = 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin((float)(t * 2.8 + i * 0.9)));
                var col = new Color(0.92f, 0.72f, 0.38f, pulse);
                GUI.DrawTexture(
                    new Rect(px, py, dotSize, dotSize),
                    Texture2D.whiteTexture,
                    ScaleMode.StretchToFill,
                    false,
                    0f,
                    col,
                    0f,
                    0f);
            }
        }

        private void DrawErrorUi()
        {
            EnsureChromeLayout();

            const float width = 520f;
            var x = (Screen.width - width) * 0.5f;
            var y = Screen.height * 0.38f;
            GUILayout.BeginArea(new Rect(x, y, width, 200f));
            var errTitle = LocalePreferencePreview.PreferCyrillicUi() ? errorTitleRu : errorTitleEn;
            GUILayout.Label(errTitle, _errorTitleStyle);
            GUILayout.Space(10f);
            GUILayout.TextArea(_errorMessage, GUILayout.Height(72f));
            GUILayout.Space(10f);
            var back = LocalePreferencePreview.PreferCyrillicUi() ? backToMenuRu : backToMenuEn;
            if (GUILayout.Button(back, GUILayout.Height(32f)))
            {
                SceneManager.LoadScene(SceneNames.DefaultMenu, LoadSceneMode.Single);
            }

            GUILayout.EndArea();
        }

        private IEnumerator BootToMenuCoroutine()
        {
            AsyncOperation op;
            try
            {
                op = SceneManager.LoadSceneAsync(SceneNames.DefaultMenu, LoadSceneMode.Single);
            }
            catch (Exception ex)
            {
                _errorMessage = ex.Message;
                yield break;
            }

            if (op == null)
            {
                _errorMessage = "LoadSceneAsync returned null.";
                yield break;
            }

            op.allowSceneActivation = false;
            yield return null;

            var t0 = Time.unscaledTime;
            while (true)
            {
                var loadReady = op.progress >= 0.9f;
                var minShown = Time.unscaledTime - t0 >= minDisplaySeconds;
                if (loadReady && minShown)
                {
                    break;
                }

                if (Time.unscaledTime - t0 > maxWaitSeconds)
                {
                    Debug.LogWarning(
                        $"{nameof(BootSceneController)}: menu load did not report readiness in time; activating anyway.");
                    break;
                }

                yield return null;
            }

            yield return RunOptionalWarmupBeforeMenu();
            op.allowSceneActivation = true;

            var frames = 0;
            while (!op.isDone && frames < 600)
            {
                frames++;
                yield return null;
            }

            if (!op.isDone)
            {
                Debug.LogError(
                    $"{nameof(BootSceneController)}: AsyncOperation did not finish; forcing synchronous menu load.");
                SceneManager.LoadScene(SceneNames.DefaultMenu, LoadSceneMode.Single);
            }

            _finished = true;
        }

        private static IEnumerator RunOptionalWarmupBeforeMenu()
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
    }
}
