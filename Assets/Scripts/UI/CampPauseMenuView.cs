using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ShatteredForge.UI
{
    [DefaultExecutionOrder(15)]
    public class CampPauseMenuView : MonoBehaviour, IPauseMenuView
    {
        public const string DefaultViewResourcesPath = "UI/CampPauseMenuUi";

        [Header("Root (optional - auto-built when null)")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private GameObject panelRoot;

        [Header("Main page")]
        [SerializeField] private GameObject mainPage;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitToMainMenuButton;

        [Header("Settings page")]
        [SerializeField] private GameObject settingsPage;
        [SerializeField] private Text volumeLabel;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Button fullscreenButton;
        [SerializeField] private Text resolutionLabel;
        [SerializeField] private Button nextResolutionButton;
        [SerializeField] private Button backButton;

        private Action _onContinue;
        private Action _onOpenSettings;
        private Action _onExitToMainMenu;
        private Action<float> _onVolumeChanged;
        private Action _onToggleFullscreen;
        private Action _onNextResolution;
        private Action _onBackFromSettings;

        private bool _built;
        private bool _isUpdatingUi;

        public void EnsureBuilt()
        {
            if (_built)
            {
                return;
            }

            TryAutoWireExistingHierarchy();
            if (!IsHierarchyComplete())
            {
                BuildDefaultUi(startHidden: true);
            }

            TryAutoWireExistingHierarchy();
            _built = IsHierarchyComplete();
            if (!_built)
            {
                Debug.LogWarning($"{nameof(CampPauseMenuView)}: UI references are incomplete.");
            }
        }

        private void Awake()
        {
            EnsureBuilt();
        }

        private void OnEnable()
        {
            EnsureUiEventSystemExists();
        }

        public void Bind(
            Action onContinue,
            Action onOpenSettings,
            Action onExitToMainMenu,
            Action<float> onVolumeChanged,
            Action onToggleFullscreen,
            Action onNextResolution,
            Action onBackFromSettings)
        {
            Bind(new PauseMenuBinding
            {
                onContinue = onContinue,
                onOpenSettings = onOpenSettings,
                onExit = onExitToMainMenu,
                onVolumeChanged = onVolumeChanged,
                onToggleFullscreen = onToggleFullscreen,
                onNextResolution = onNextResolution,
                onBackFromSettings = onBackFromSettings
            });
        }

        public void Bind(PauseMenuBinding binding)
        {
            _onContinue = binding?.onContinue;
            _onOpenSettings = binding?.onOpenSettings;
            _onExitToMainMenu = binding?.onExit;
            _onVolumeChanged = binding?.onVolumeChanged;
            _onToggleFullscreen = binding?.onToggleFullscreen;
            _onNextResolution = binding?.onNextResolution;
            _onBackFromSettings = binding?.onBackFromSettings;
            WireEvents();
        }

        public void Configure(PauseMenuConfig config)
        {
            if (config == null)
            {
                return;
            }

            if (continueButton != null && !string.IsNullOrWhiteSpace(config.continueLabel))
            {
                var text = continueButton.GetComponentInChildren<Text>(true);
                if (text != null)
                {
                    text.text = config.continueLabel;
                }
            }

            if (settingsButton != null)
            {
                settingsButton.gameObject.SetActive(config.showSettingsButton);
                if (!string.IsNullOrWhiteSpace(config.settingsLabel))
                {
                    var text = settingsButton.GetComponentInChildren<Text>(true);
                    if (text != null)
                    {
                        text.text = config.settingsLabel;
                    }
                }
            }

            if (exitToMainMenuButton != null)
            {
                exitToMainMenuButton.gameObject.SetActive(config.showExitButton);
                if (!string.IsNullOrWhiteSpace(config.exitLabel))
                {
                    var text = exitToMainMenuButton.GetComponentInChildren<Text>(true);
                    if (text != null)
                    {
                        text.text = config.exitLabel;
                    }
                }
            }
        }

        public void SetOpen(bool open)
        {
            if (rootCanvas != null)
            {
                rootCanvas.gameObject.SetActive(open);
            }
            else if (panelRoot != null)
            {
                panelRoot.SetActive(open);
            }
        }

        public void ShowMainPage()
        {
            if (mainPage != null)
            {
                mainPage.SetActive(true);
            }

            if (settingsPage != null)
            {
                settingsPage.SetActive(false);
            }
        }

        public void ShowSettingsPage(float volume, bool fullscreen, string resolutionText)
        {
            if (mainPage != null)
            {
                mainPage.SetActive(false);
            }

            if (settingsPage != null)
            {
                settingsPage.SetActive(true);
            }

            _isUpdatingUi = true;
            if (volumeLabel != null)
            {
                volumeLabel.text = $"Громкость: {Mathf.RoundToInt(Mathf.Clamp01(volume) * 100f)}%";
            }

            if (volumeSlider != null)
            {
                volumeSlider.value = Mathf.Clamp01(volume);
            }

            if (fullscreenButton != null)
            {
                var txt = fullscreenButton.GetComponentInChildren<Text>(true);
                if (txt != null)
                {
                    txt.text = fullscreen ? "Полноэкранный: Вкл" : "Полноэкранный: Выкл";
                }
            }

            if (resolutionLabel != null)
            {
                resolutionLabel.text = $"Разрешение: {resolutionText}";
            }

            _isUpdatingUi = false;
        }

        private void WireEvents()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(() => _onContinue?.Invoke());
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveAllListeners();
                settingsButton.onClick.AddListener(() => _onOpenSettings?.Invoke());
            }

            if (exitToMainMenuButton != null)
            {
                exitToMainMenuButton.onClick.RemoveAllListeners();
                exitToMainMenuButton.onClick.AddListener(() => _onExitToMainMenu?.Invoke());
            }

            if (volumeSlider != null)
            {
                volumeSlider.onValueChanged.RemoveAllListeners();
                volumeSlider.onValueChanged.AddListener(v =>
                {
                    if (_isUpdatingUi)
                    {
                        return;
                    }

                    _onVolumeChanged?.Invoke(v);
                });
            }

            if (fullscreenButton != null)
            {
                fullscreenButton.onClick.RemoveAllListeners();
                fullscreenButton.onClick.AddListener(() => _onToggleFullscreen?.Invoke());
            }

            if (nextResolutionButton != null)
            {
                nextResolutionButton.onClick.RemoveAllListeners();
                nextResolutionButton.onClick.AddListener(() => _onNextResolution?.Invoke());
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(() => _onBackFromSettings?.Invoke());
            }
        }

        private void TryAutoWireExistingHierarchy()
        {
            if (rootCanvas == null)
            {
                rootCanvas = GetComponentInChildren<Canvas>(true);
            }

            if (panelRoot == null && rootCanvas != null)
            {
                var p = rootCanvas.transform.Find("Panel");
                panelRoot = p != null ? p.gameObject : null;
            }

            if (mainPage == null && panelRoot != null)
            {
                mainPage = panelRoot.transform.Find("MainPage")?.gameObject;
            }

            if (settingsPage == null && panelRoot != null)
            {
                settingsPage = panelRoot.transform.Find("SettingsPage")?.gameObject;
            }

            if (continueButton == null && mainPage != null)
            {
                continueButton = mainPage.transform.Find("ContinueButton")?.GetComponent<Button>();
            }

            if (settingsButton == null && mainPage != null)
            {
                settingsButton = mainPage.transform.Find("SettingsButton")?.GetComponent<Button>();
            }

            if (exitToMainMenuButton == null && mainPage != null)
            {
                exitToMainMenuButton = mainPage.transform.Find("ExitToMainMenuButton")?.GetComponent<Button>();
            }

            if (volumeLabel == null && settingsPage != null)
            {
                volumeLabel = settingsPage.transform.Find("VolumeLabel")?.GetComponent<Text>();
            }

            if (volumeSlider == null && settingsPage != null)
            {
                volumeSlider = settingsPage.transform.Find("VolumeSlider")?.GetComponent<Slider>();
            }

            if (fullscreenButton == null && settingsPage != null)
            {
                fullscreenButton = settingsPage.transform.Find("FullscreenButton")?.GetComponent<Button>();
            }

            if (resolutionLabel == null && settingsPage != null)
            {
                resolutionLabel = settingsPage.transform.Find("ResolutionLabel")?.GetComponent<Text>();
            }

            if (nextResolutionButton == null && settingsPage != null)
            {
                nextResolutionButton = settingsPage.transform.Find("NextResolutionButton")?.GetComponent<Button>();
            }

            if (backButton == null && settingsPage != null)
            {
                backButton = settingsPage.transform.Find("BackButton")?.GetComponent<Button>();
            }
        }

        private bool IsHierarchyComplete()
        {
            return rootCanvas != null &&
                   panelRoot != null &&
                   mainPage != null &&
                   settingsPage != null &&
                   continueButton != null &&
                   settingsButton != null &&
                   exitToMainMenuButton != null &&
                   volumeLabel != null &&
                   volumeSlider != null &&
                   fullscreenButton != null &&
                   resolutionLabel != null &&
                   nextResolutionButton != null &&
                   backButton != null;
        }

        private void BuildDefaultUi(bool startHidden)
        {
            var canvasGo = new GameObject("_CampPauseCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            rootCanvas = canvasGo.AddComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.sortingOrder = 900;
            canvasGo.AddComponent<GraphicRaycaster>();
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            panelRoot = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            var panelRt = panelRoot.GetComponent<RectTransform>();
            panelRt.SetParent(canvasGo.transform, false);
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(520f, 420f);
            panelRoot.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.12f, 0.96f);

            var title = CreateText(panelRoot.transform, "Title", new Vector2(0f, -34f), new Vector2(400f, 40f), 34, FontStyle.Bold, TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.text = "Пауза";

            mainPage = new GameObject("MainPage", typeof(RectTransform));
            var mainRt = mainPage.GetComponent<RectTransform>();
            mainRt.SetParent(panelRoot.transform, false);
            Stretch(mainRt, 32f, 32f, 90f, 32f);

            continueButton = CreateButton(mainPage.transform, "ContinueButton", "Продолжить", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(0.5f, 1f), new Vector2(360f, 56f));
            settingsButton = CreateButton(mainPage.transform, "SettingsButton", "Настройки", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -94f), new Vector2(0.5f, 1f), new Vector2(360f, 56f));
            exitToMainMenuButton = CreateButton(mainPage.transform, "ExitToMainMenuButton", "Выход в главное меню", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -168f), new Vector2(0.5f, 1f), new Vector2(360f, 56f));

            settingsPage = new GameObject("SettingsPage", typeof(RectTransform));
            var settingsRt = settingsPage.GetComponent<RectTransform>();
            settingsRt.SetParent(panelRoot.transform, false);
            Stretch(settingsRt, 32f, 32f, 90f, 32f);

            volumeLabel = CreateText(settingsPage.transform, "VolumeLabel", new Vector2(0f, -8f), new Vector2(420f, 36f), 22, FontStyle.Bold, TextAnchor.MiddleLeft);
            volumeLabel.rectTransform.anchorMin = volumeLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            volumeLabel.rectTransform.pivot = new Vector2(0.5f, 1f);

            volumeSlider = CreateSlider(settingsPage.transform, "VolumeSlider", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -52f), new Vector2(0.5f, 1f), new Vector2(420f, 24f));
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;

            fullscreenButton = CreateButton(settingsPage.transform, "FullscreenButton", "Полноэкранный: Вкл", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(0.5f, 1f), new Vector2(420f, 48f));
            resolutionLabel = CreateText(settingsPage.transform, "ResolutionLabel", new Vector2(0f, -164f), new Vector2(420f, 32f), 18, FontStyle.Normal, TextAnchor.MiddleLeft);
            resolutionLabel.rectTransform.anchorMin = resolutionLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            resolutionLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            nextResolutionButton = CreateButton(settingsPage.transform, "NextResolutionButton", "Следующее разрешение", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -202f), new Vector2(0.5f, 1f), new Vector2(420f, 44f));
            backButton = CreateButton(settingsPage.transform, "BackButton", "Назад", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -258f), new Vector2(0.5f, 1f), new Vector2(420f, 44f));

            settingsPage.SetActive(false);
            if (startHidden)
            {
                rootCanvas.gameObject.SetActive(false);
            }
        }

        private static void Stretch(RectTransform rt, float left, float right, float top, float bottom)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        private static Text CreateText(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = new Color(0.93f, 0.93f, 0.93f, 1f);
            text.alignment = align;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string caption,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 pivot,
            Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;

            var image = go.GetComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.23f, 1f);

            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.34f, 0.34f, 0.38f, 1f);
            colors.pressedColor = new Color(0.43f, 0.43f, 0.47f, 1f);
            button.colors = colors;
            button.targetGraphic = image;

            var label = CreateText(go.transform, "Label", Vector2.zero, sizeDelta, 22, FontStyle.Bold, TextAnchor.MiddleCenter);
            var labelRt = label.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            label.text = caption;
            return button;
        }

        private static Slider CreateSlider(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 pivot,
            Vector2 sizeDelta)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Slider));
            var rt = root.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            var bgRt = background.GetComponent<RectTransform>();
            bgRt.SetParent(root.transform, false);
            bgRt.anchorMin = new Vector2(0f, 0.25f);
            bgRt.anchorMax = new Vector2(1f, 0.75f);
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImage = background.GetComponent<Image>();
            bgImage.color = new Color(0.14f, 0.14f, 0.16f, 1f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            var fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.SetParent(root.transform, false);
            fillAreaRt.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRt.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRt.offsetMin = new Vector2(8f, 0f);
            fillAreaRt.offsetMax = new Vector2(-18f, 0f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.SetParent(fillArea.transform, false);
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(1f, 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fillImage = fill.GetComponent<Image>();
            fillImage.color = new Color(0.57f, 0.63f, 0.84f, 1f);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            var handleAreaRt = handleArea.GetComponent<RectTransform>();
            handleAreaRt.SetParent(root.transform, false);
            handleAreaRt.anchorMin = new Vector2(0f, 0f);
            handleAreaRt.anchorMax = new Vector2(1f, 1f);
            handleAreaRt.offsetMin = new Vector2(10f, 0f);
            handleAreaRt.offsetMax = new Vector2(-10f, 0f);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            var handleRt = handle.GetComponent<RectTransform>();
            handleRt.SetParent(handleArea.transform, false);
            handleRt.sizeDelta = new Vector2(16f, 30f);
            var handleImage = handle.GetComponent<Image>();
            handleImage.color = new Color(0.9f, 0.9f, 0.93f, 1f);

            var slider = root.GetComponent<Slider>();
            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.value = 1f;
            return slider;
        }

        private static void EnsureUiEventSystemExists()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }
    }
}
