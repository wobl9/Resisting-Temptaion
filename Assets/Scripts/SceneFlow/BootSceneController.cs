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
    [DefaultExecutionOrder(500)]
    public sealed class BootSceneController : MonoBehaviour
    {
        private static readonly Color TopGradient = new Color(0.16f, 0.06f, 0.09f, 1f);
        private static readonly Color BottomGradient = new Color(0.02f, 0.02f, 0.03f, 1f);
        private static readonly Color AccentGold = new Color(0.79f, 0.64f, 0.15f, 1f);
        private static readonly Color MutedText = new Color(0.55f, 0.52f, 0.48f, 1f);

        [SerializeField] [Min(0f)] private float minDisplaySeconds = 0.85f;

        [SerializeField] [Min(1f)] private float maxWaitSeconds = 45f;

        private string _errorMessage;
        private bool _finished;

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

        private static void DrawGradientBackdrop()
        {
            const int strips = 24;
            var h = Screen.height / (float)strips;
            for (var i = 0; i < strips; i++)
            {
                var t = i / (float)(strips - 1);
                var c = Color.Lerp(TopGradient, BottomGradient, t);
                GUI.DrawTexture(
                    new Rect(0f, i * h, Screen.width, h + 1f),
                    Texture2D.whiteTexture,
                    ScaleMode.StretchToFill,
                    false,
                    0f,
                    c,
                    0f,
                    0f);
            }
        }

        private void DrawBootChrome()
        {
            var lineW = Mathf.Min(280f, Screen.width - 120f);
            var lineX = (Screen.width - lineW) * 0.5f;
            var lineY = Screen.height * 0.58f;
            GUI.DrawTexture(
                new Rect(lineX, lineY, lineW, 2f),
                Texture2D.whiteTexture,
                ScaleMode.StretchToFill,
                false,
                0f,
                AccentGold * new Color(1f, 1f, 1f, 0.85f),
                0f,
                0f);

            var title = Loc.Ui(UiKeys.GameTitle);
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(34 + Screen.width / 80, 28, 46),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = AccentGold }
            };
            var titleH = titleStyle.CalcHeight(new GUIContent(title), Screen.width);
            GUI.Label(new Rect(0f, Screen.height * 0.36f, Screen.width, titleH + 12f), title, titleStyle);

            var subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = MutedText }
            };
            var sub = Loc.Ui(UiKeys.BootOpeningLine);
            var subH = subStyle.CalcHeight(new GUIContent(sub), Screen.width);
            GUI.Label(new Rect(0f, Screen.height * 0.36f + titleH + 18f, Screen.width, subH + 8f), sub, subStyle);

            DrawOrbitalDots();
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
            const float width = 520f;
            var x = (Screen.width - width) * 0.5f;
            var y = Screen.height * 0.38f;
            GUILayout.BeginArea(new Rect(x, y, width, 200f));
            var h = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, normal = { textColor = AccentGold } };
            GUILayout.Label(Loc.Ui(UiKeys.LoadingErrorTitle), h);
            GUILayout.Space(10f);
            GUILayout.TextArea(_errorMessage, GUILayout.Height(72f));
            GUILayout.Space(10f);
            if (GUILayout.Button(Loc.Ui(UiKeys.LoadingBackToMenu), GUILayout.Height(32f)))
            {
                SceneManager.LoadScene(SceneNames.DefaultMenu, LoadSceneMode.Single);
            }

            GUILayout.EndArea();
        }

        private IEnumerator BootToMenuCoroutine()
        {
            yield return null;

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
