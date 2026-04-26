using System;
using System.Collections;
using System.Collections.Generic;
using ShatteredForge.Localization;
using ShatteredForge.SceneFlow;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

namespace ShatteredForge.Menu
{
    public class MainMenuController : MonoBehaviour
    {
        private static readonly Color MenuBackdropColor = new Color(0.078431375f, 0.078431375f, 0.09019608f, 1f);

        private enum ScreenState
        {
            Main,
            ProfileActions,
            Profiles,
            NewProfile,
            NewExpeditionConfirm,
            Settings
        }

        [Header("Scene routing")]
        [SerializeField] private string gameplaySceneName = "";

        [Tooltip("Loaded for a new expedition (not Continue). Leave empty to use CampHub.")]
        [SerializeField] private string hubSceneName = "";

        [Header("Profile storage")]
        [SerializeField] private ProfileStorageMode profileStorageMode = ProfileStorageMode.Local;
        [SerializeField] private string remoteProfileStorageBaseUrl = "";
        [SerializeField] private string remoteProfileStorageAuthBearer = "";

        private ScreenState _state = ScreenState.Main;
        private IProfileStorage _profilesService;
        private List<ProfileSummary> _profiles = new();
        private string _activeProfileId;

        private string _deleteCandidateProfileId;
        private string _newProfileName = string.Empty;
        private string _status = string.Empty;

        private bool _cachedHasActiveExpedition;

        private float _masterVolume = 1f;
        private bool _fullscreen = true;
        private Resolution[] _resolutions;
        private int _resolutionIndex;

        private bool _profilesReady;

        private void Awake()
        {
            PendingCampDungeonRequest.Reset();

            if (string.IsNullOrEmpty(_newProfileName))
            {
                _newProfileName = MenuWarmStrings.DefaultNewProfileName;
            }

            if (string.IsNullOrEmpty(_status))
            {
                _status = MenuWarmStrings.Welcome;
            }

            _masterVolume = AudioListener.volume;
            _fullscreen = Screen.fullScreen;
            _resolutions = Screen.resolutions;
            _resolutionIndex = FindCurrentResolutionIndex();
        }

        private void Start()
        {
            StartCoroutine(LoadProfilesAfterFirstFrame());
        }

        private IEnumerator LoadProfilesAfterFirstFrame()
        {
            yield return null;
            _profilesService = ProfileStorageFactory.Create(
                profileStorageMode,
                remoteProfileStorageBaseUrl,
                remoteProfileStorageAuthBearer);
            ReloadProfiles();
            _profilesReady = true;
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.Repaint)
            {
                DrawMenuBackdrop();
            }

            if (!_profilesReady)
            {
                if (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout)
                {
                    Event.current.Use();
                }

                if (Event.current.type == EventType.Repaint)
                {
                    DrawMenuLoadingShell();
                }

                return;
            }

            DrawActiveProfileEntry();

            const float width = 560f;
            var x = (Screen.width - width) * 0.5f;
            var y = 60f;
            var rect = new Rect(x, y, width, Screen.height - 120f);

            // Do not use GUI.Box here: it registers a full-rect control and steals mouse hover/clicks from GUILayout buttons inside BeginArea.
            if (Event.current.type == EventType.Repaint)
            {
                GUI.skin.box.Draw(rect, GUIContent.none, false, false, false, false);
            }

            GUILayout.BeginArea(new Rect(rect.x + 24f, rect.y + 24f, rect.width - 48f, rect.height - 48f));
            GUILayout.Label(Loc.Ui(UiKeys.GameTitle), HeaderStyle());
            GUILayout.Space(8f);
            GUILayout.Label(_status);
            GUILayout.Space(18f);

            switch (_state)
            {
                case ScreenState.Main:
                    DrawMainMenu();
                    break;
                case ScreenState.ProfileActions:
                    DrawProfileActionsMenu();
                    break;
                case ScreenState.Profiles:
                    DrawProfilesMenu();
                    break;
                case ScreenState.NewProfile:
                    DrawNewProfileMenu();
                    break;
                case ScreenState.NewExpeditionConfirm:
                    DrawNewExpeditionConfirmMenu();
                    break;
                case ScreenState.Settings:
                    DrawSettingsMenu();
                    break;
            }

            GUILayout.EndArea();
        }

        private void DrawMainMenu()
        {
            var hasAnyProfiles = _profiles.Count > 0;
            var hasActiveProfile = !string.IsNullOrEmpty(_activeProfileId);

            if (!hasAnyProfiles)
            {
                if (GUILayout.Button(Loc.Ui(UiKeys.NewGame), ButtonOptions()))
                {
                    _state = ScreenState.NewProfile;
                }

                if (GUILayout.Button(Loc.Ui(UiKeys.Settings), ButtonOptions()))
                {
                    _state = ScreenState.Settings;
                }

                if (GUILayout.Button(Loc.Ui(UiKeys.QuitGame), ButtonOptions()))
                {
                    QuitGame();
                }

                return;
            }

            if (!hasActiveProfile)
            {
                GUILayout.Label(Loc.Ui(UiKeys.NoActiveProfile));
                if (GUILayout.Button(Loc.Ui(UiKeys.SelectProfile), ButtonOptions()))
                {
                    ReloadProfiles();
                    _state = ScreenState.Profiles;
                }

                GUILayout.Space(10f);
                if (GUILayout.Button(Loc.Ui(UiKeys.Settings), ButtonOptions()))
                {
                    _state = ScreenState.Settings;
                }

                if (GUILayout.Button(Loc.Ui(UiKeys.QuitGame), ButtonOptions()))
                {
                    QuitGame();
                }

                return;
            }

            if (_cachedHasActiveExpedition && GUILayout.Button(Loc.Ui(UiKeys.ContinueGame), ButtonOptions()))
            {
                TryEnterGameplay(_activeProfileId, resumeExpedition: true);
            }

            if (GUILayout.Button(Loc.Ui(UiKeys.NewGame), ButtonOptions()))
            {
                if (_cachedHasActiveExpedition)
                {
                    _state = ScreenState.NewExpeditionConfirm;
                    return;
                }

                TryEnterGameplay(_activeProfileId, resumeExpedition: false);
            }

            if (GUILayout.Button(Loc.Ui(UiKeys.Settings), ButtonOptions()))
            {
                _state = ScreenState.Settings;
            }

            if (GUILayout.Button(Loc.Ui(UiKeys.QuitGame), ButtonOptions()))
            {
                QuitGame();
            }
        }

        private void DrawActiveProfileEntry()
        {
            if (string.IsNullOrEmpty(_activeProfileId) || _profiles.Count == 0)
            {
                return;
            }

            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter
            };

            var rect = new Rect((Screen.width - 520f) * 0.5f, 12f, 520f, 28f);
            if (GUI.Button(rect, Loc.UiFormat(UiKeys.ProfileButton, GetActiveProfileName()), buttonStyle))
            {
                _state = ScreenState.ProfileActions;
                _status = Loc.Ui(UiKeys.ProfileMenuOpened);
            }
        }

        private void DrawProfileActionsMenu()
        {
            GUILayout.Label(Loc.Ui(UiKeys.ProfileMenuTitle), HeaderStyle());
            GUILayout.Space(8f);
            GUILayout.Label(Loc.UiFormat(UiKeys.ProfileMenuCurrent, GetActiveProfileName()));
            GUILayout.Space(12f);

            if (GUILayout.Button(Loc.Ui(UiKeys.SwitchProfile), ButtonOptions()))
            {
                ReloadProfiles();
                _state = ScreenState.Profiles;
            }

            if (GUILayout.Button(Loc.Ui(UiKeys.CreateNewProfile), ButtonOptions()))
            {
                _state = ScreenState.NewProfile;
            }

            GUILayout.Space(8f);
            DrawDeleteProfileControls();

            GUILayout.Space(14f);
            if (GUILayout.Button(Loc.Ui(UiKeys.Back), ButtonOptions()))
            {
                _state = ScreenState.Main;
            }
        }

        private void DrawProfilesMenu()
        {
            GUILayout.Label(Loc.Ui(UiKeys.ProfilesTitle), HeaderStyle());
            GUILayout.Space(8f);

            if (_profiles.Count == 0)
            {
                GUILayout.Label(Loc.Ui(UiKeys.ProfilesEmpty));
            }
            else
            {
                foreach (var profile in _profiles)
                {
                    var isActive = profile.id == _activeProfileId;
                    var title = isActive
                        ? profile.displayName + Loc.Ui(UiKeys.ActiveSuffix)
                        : profile.displayName;
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(title, ButtonOptions()))
                    {
                        _profilesService.SetActiveProfile(profile.id);
                        ReloadProfiles();
                        _status = Loc.UiFormat(UiKeys.ProfileSelected, profile.displayName);
                        _state = ScreenState.Main;
                    }

                    if (GUILayout.Button(Loc.Ui(UiKeys.Delete), GUILayout.Height(36f), GUILayout.Width(110f)))
                    {
                        _deleteCandidateProfileId = profile.id;
                    }

                    GUILayout.EndHorizontal();

                    if (_deleteCandidateProfileId == profile.id)
                    {
                        GUILayout.Label(Loc.UiFormat(UiKeys.DeleteProfilePrompt, profile.displayName));
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button(Loc.Ui(UiKeys.YesDelete), GUILayout.Height(30f)))
                        {
                            DeleteProfile(profile.id);
                        }

                        if (GUILayout.Button(Loc.Ui(UiKeys.Cancel), GUILayout.Height(30f)))
                        {
                            _deleteCandidateProfileId = null;
                        }

                        GUILayout.EndHorizontal();
                    }
                }
            }

            GUILayout.Space(14f);
            if (GUILayout.Button(Loc.Ui(UiKeys.Back), ButtonOptions()))
            {
                _state = ScreenState.Main;
            }
        }

        private void DrawNewProfileMenu()
        {
            GUILayout.Label(Loc.Ui(UiKeys.NewProfileTitle), HeaderStyle());
            GUILayout.Space(6f);
            GUILayout.Label(Loc.Ui(UiKeys.ProfileNameLabel));
            _newProfileName = GUILayout.TextField(_newProfileName, GUILayout.Height(28f));

            GUILayout.Space(10f);
            if (GUILayout.Button(Loc.Ui(UiKeys.ConfirmAndStart), ButtonOptions()))
            {
                var profileId = _profilesService.CreateProfile(_newProfileName);
                ReloadProfiles();
                _status = Loc.UiFormat(UiKeys.StatusProfileCreated, GetActiveProfileName());
                TryEnterGameplay(profileId, resumeExpedition: false);
            }

            if (GUILayout.Button(Loc.Ui(UiKeys.Back), ButtonOptions()))
            {
                _state = ScreenState.Main;
            }
        }

        private void DrawNewExpeditionConfirmMenu()
        {
            GUILayout.Label(Loc.Ui(UiKeys.NewExpeditionTitle), HeaderStyle());
            GUILayout.Space(8f);
            GUILayout.Label(Loc.UiFormat(UiKeys.NewExpeditionProfileLine, GetActiveProfileName()));
            GUILayout.Label(Loc.Ui(UiKeys.NewExpeditionPrompt));
            GUILayout.Space(10f);

            if (GUILayout.Button(Loc.Ui(UiKeys.NewExpeditionConfirm), ButtonOptions()))
            {
                TryEnterGameplay(_activeProfileId, resumeExpedition: false);
            }

            if (GUILayout.Button(Loc.Ui(UiKeys.Back), ButtonOptions()))
            {
                _state = ScreenState.Main;
            }
        }

        private void DrawSettingsMenu()
        {
            GUILayout.Label(Loc.Ui(UiKeys.SettingsTitle), HeaderStyle());
            GUILayout.Space(8f);

            GUILayout.Label(Loc.UiFormat(UiKeys.VolumeLabel, Mathf.RoundToInt(_masterVolume * 100f).ToString()));
            var newVolume = GUILayout.HorizontalSlider(_masterVolume, 0f, 1f, GUILayout.Height(24f));
            if (!Mathf.Approximately(newVolume, _masterVolume))
            {
                _masterVolume = newVolume;
                AudioListener.volume = _masterVolume;
            }

            GUILayout.Space(8f);
            var fullscreenLabel = _fullscreen ? Loc.Ui(UiKeys.FullscreenOn) : Loc.Ui(UiKeys.FullscreenOff);
            if (GUILayout.Button(fullscreenLabel, ButtonOptions()))
            {
                _fullscreen = !_fullscreen;
                Screen.fullScreen = _fullscreen;
            }

            if (_resolutions != null && _resolutions.Length > 0)
            {
                GUILayout.Space(8f);
                GUILayout.Label(Loc.UiFormat(UiKeys.ResolutionLabel, GetResolutionLabel(_resolutionIndex)));
                if (GUILayout.Button(Loc.Ui(UiKeys.NextResolution), ButtonOptions()))
                {
                    _resolutionIndex = (_resolutionIndex + 1) % _resolutions.Length;
                    var r = _resolutions[_resolutionIndex];
                    Screen.SetResolution(r.width, r.height, _fullscreen);
                }
            }

            GUILayout.Space(10f);
            if (LocalizationSettings.HasSettings && LocalizationBootstrap.AreTablesReady)
            {
                var locales = LocalizationSettings.AvailableLocales.Locales;
                if (locales != null && locales.Count > 0)
                {
                    var langCode = LocalizationSettings.SelectedLocale != null
                        ? LocalizationSettings.SelectedLocale.Identifier.Code
                        : LocalizationPreferences.GetSelectedLocaleCodeOrEmpty();
                    if (string.IsNullOrEmpty(langCode))
                    {
                        langCode = LocalePreferencePreview.PreferCyrillicUi() ? "ru" : "en";
                    }

                    GUILayout.Label(Loc.UiFormat(UiKeys.LanguageLabel, langCode));

                    var codes = new string[locales.Count];
                    var selectedIndex = 0;
                    for (var i = 0; i < locales.Count; i++)
                    {
                        var loc = locales[i];
                        var code = loc != null ? loc.Identifier.Code : "?";
                        codes[i] = code;
                        if (code == langCode)
                        {
                            selectedIndex = i;
                        }
                    }

                    var cols = locales.Count <= 4 ? locales.Count : 4;
                    var newIndex = GUILayout.SelectionGrid(selectedIndex, codes, cols, GUILayout.Height(32f));
                    if (newIndex != selectedIndex && newIndex >= 0 && newIndex < locales.Count)
                    {
                        var picked = locales[newIndex];
                        if (picked != null)
                        {
                            LocalizationSettings.SelectedLocale = picked;
                            LocalizationPreferences.SetSelectedLocaleCode(picked.Identifier.Code);
                        }
                    }
                }
            }

            GUILayout.Space(14f);
            if (GUILayout.Button(Loc.Ui(UiKeys.Back), ButtonOptions()))
            {
                _state = ScreenState.Main;
            }
        }

        private void ReloadProfiles()
        {
            _profiles = _profilesService.LoadProfiles(out _activeProfileId);

            if (_profiles.Count > 0 && string.IsNullOrEmpty(_activeProfileId))
            {
                _profilesService.SetActiveProfile(_profiles[0].id);
                _profiles = _profilesService.LoadProfiles(out _activeProfileId);
            }

            _cachedHasActiveExpedition = !string.IsNullOrEmpty(_activeProfileId) &&
                                       _profilesService.HasActiveExpedition(_activeProfileId);

            if (_deleteCandidateProfileId != null)
            {
                var found = false;
                foreach (var profile in _profiles)
                {
                    if (profile.id == _deleteCandidateProfileId)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    _deleteCandidateProfileId = null;
                }
            }
        }

        private string GetActiveProfileName()
        {
            if (string.IsNullOrEmpty(_activeProfileId))
            {
                return Loc.Ui(UiKeys.CommonNone);
            }

            foreach (var profile in _profiles)
            {
                if (profile.id == _activeProfileId)
                {
                    return profile.displayName;
                }
            }

            return Loc.Ui(UiKeys.CommonUnknown);
        }

        private void TryEnterGameplay(string profileId, bool resumeExpedition)
        {
            if (SceneNavigation.IsBusy)
            {
                return;
            }

            if (resumeExpedition && !_profilesService.HasActiveExpedition(profileId))
            {
                _status = Loc.Ui(UiKeys.ErrorNoExpeditionToContinue);
                return;
            }

            MenuSessionWriter.WriteGameplayLaunchIntent(profileId, resumeExpedition);

            if (resumeExpedition)
            {
                var gameplayTarget = gameplaySceneName;
                if (string.IsNullOrWhiteSpace(gameplayTarget))
                {
                    gameplayTarget = SceneManager.GetActiveScene().name;
                }

                if (string.IsNullOrWhiteSpace(gameplayTarget) || !IsSceneInBuildSettings(gameplayTarget))
                {
                    _state = ScreenState.Main;
                    _status = Loc.Ui(UiKeys.ErrorGameplaySceneMissing);
                    return;
                }

                SceneNavigation.GoTo(gameplayTarget);
                return;
            }

            var hubTarget = hubSceneName;
            if (string.IsNullOrWhiteSpace(hubTarget))
            {
                hubTarget = SceneNames.CampHub;
            }

            if (!IsSceneInBuildSettings(hubTarget))
            {
                _state = ScreenState.Main;
                _status = Loc.Ui(UiKeys.ErrorHubSceneMissing);
                return;
            }

            SceneNavigation.GoTo(hubTarget);
        }

        private static bool IsSceneInBuildSettings(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
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
                if (string.Equals(name, sceneName, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void DrawMenuBackdrop()
        {
            var prevDepth = GUI.depth;
            GUI.depth = 10000;
            GUI.DrawTexture(
                new Rect(0f, 0f, Screen.width, Screen.height),
                Texture2D.whiteTexture,
                ScaleMode.StretchToFill,
                false,
                0f,
                MenuBackdropColor,
                0f,
                0f);
            GUI.depth = prevDepth;
        }

        private static void DrawMenuLoadingShell()
        {
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            var title = MenuWarmStrings.GameTitle;
            var titleH = titleStyle.CalcHeight(new GUIContent(title), Screen.width);
            GUI.Label(new Rect(0f, Screen.height * 0.4f, Screen.width, titleH + 8f), title, titleStyle);

            var subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.75f, 0.75f, 0.78f, 1f) }
            };
            var sub = Loc.Ui(UiKeys.LoadingGameplay);
            var subH = subStyle.CalcHeight(new GUIContent(sub), Screen.width);
            GUI.Label(new Rect(0f, Screen.height * 0.4f + titleH + 10f, Screen.width, subH + 8f), sub, subStyle);

            var cx = Screen.width * 0.5f;
            var cy = Screen.height * 0.68f;
            const float orbitR = 22f;
            const float dotSize = 6f;
            var t = Time.unscaledTime;
            for (var i = 0; i < 6; i++)
            {
                var a = (float)(t * 1.65 + i * (Mathf.PI * 2f / 6f));
                var px = cx + Mathf.Cos(a) * orbitR - dotSize * 0.5f;
                var py = cy + Mathf.Sin(a) * orbitR - dotSize * 0.5f;
                var pulse = 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin((float)(t * 2.8 + i * 0.9)));
                var col = new Color(0.45f, 0.65f, 0.9f, pulse);
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

        private void DrawDeleteProfileControls()
        {
            if (string.IsNullOrEmpty(_activeProfileId))
            {
                GUILayout.Label(Loc.Ui(UiKeys.NoActiveProfileToDelete));
                return;
            }

            if (_deleteCandidateProfileId != _activeProfileId)
            {
                if (GUILayout.Button(Loc.Ui(UiKeys.DeleteCurrentProfile), ButtonOptions()))
                {
                    _deleteCandidateProfileId = _activeProfileId;
                }

                return;
            }

            GUILayout.Label(Loc.UiFormat(UiKeys.DeleteActiveProfilePrompt, GetActiveProfileName()));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Loc.Ui(UiKeys.YesDelete), GUILayout.Height(30f)))
            {
                DeleteProfile(_activeProfileId);
            }

            if (GUILayout.Button(Loc.Ui(UiKeys.Cancel), GUILayout.Height(30f)))
            {
                _deleteCandidateProfileId = null;
            }

            GUILayout.EndHorizontal();
        }

        private void DeleteProfile(string profileId)
        {
            var wasDeleted = _profilesService.DeleteProfile(profileId);
            if (!wasDeleted)
            {
                _status = Loc.Ui(UiKeys.ErrorDeleteNotFound);
                _deleteCandidateProfileId = null;
                return;
            }

            ReloadProfiles();
            _deleteCandidateProfileId = null;

            if (string.IsNullOrEmpty(_activeProfileId))
            {
                PlayerPrefs.DeleteKey(MenuSessionPrefs.ActiveProfileIdKey);
                PlayerPrefs.DeleteKey(MenuSessionPrefs.ResumeExpeditionKey);
            }
            else
            {
                PlayerPrefs.SetString(MenuSessionPrefs.ActiveProfileIdKey, _activeProfileId);
            }

            PlayerPrefs.Save();
            _status = Loc.Ui(UiKeys.StatusProfileDeleted);
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private int FindCurrentResolutionIndex()
        {
            if (_resolutions == null || _resolutions.Length == 0)
            {
                return 0;
            }

            for (var i = 0; i < _resolutions.Length; i++)
            {
                var r = _resolutions[i];
                if (r.width == Screen.currentResolution.width && r.height == Screen.currentResolution.height)
                {
                    return i;
                }
            }

            return _resolutions.Length - 1;
        }

        private string GetResolutionLabel(int index)
        {
            if (_resolutions == null || _resolutions.Length == 0)
            {
                return Loc.Ui(UiKeys.CommonNotApplicable);
            }

            var r = _resolutions[Mathf.Clamp(index, 0, _resolutions.Length - 1)];
            return $"{r.width} x {r.height} @ {Mathf.RoundToInt((float)r.refreshRateRatio.value)}Hz";
        }

        private static GUIStyle HeaderStyle()
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
        }

        private static GUILayoutOption[] ButtonOptions()
        {
            return new[] { GUILayout.Height(36f) };
        }
    }
}
