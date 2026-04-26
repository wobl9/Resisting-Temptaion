using System;
using System.Collections;
using System.Collections.Generic;
using ShatteredForge.Localization;
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

        private bool _loadingGameplay;
        private float _loadProgress;

        private void Awake()
        {
            _profilesService = ProfileStorageFactory.Create(
                profileStorageMode,
                remoteProfileStorageBaseUrl,
                remoteProfileStorageAuthBearer);
            ReloadProfiles();

            if (string.IsNullOrEmpty(_newProfileName))
            {
                _newProfileName = Loc.Ui(UiKeys.DefaultNewProfileName);
            }

            if (string.IsNullOrEmpty(_status))
            {
                _status = Loc.Ui(UiKeys.Welcome);
            }

            _masterVolume = AudioListener.volume;
            _fullscreen = Screen.fullScreen;
            _resolutions = Screen.resolutions;
            _resolutionIndex = FindCurrentResolutionIndex();
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.Repaint)
            {
                DrawMenuBackdrop();
            }

            if (_loadingGameplay)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    DrawLoadingOverlay();
                }
                else if (Event.current.type != EventType.Layout)
                {
                    Event.current.Use();
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
            if (LocalizationSettings.HasSettings && LocalizationSettings.SelectedLocale != null)
            {
                GUILayout.Label(Loc.UiFormat(UiKeys.LanguageLabel, LocalizationSettings.SelectedLocale.Identifier.Code));
                if (GUILayout.Button(Loc.Ui(UiKeys.NextLanguage), ButtonOptions()))
                {
                    CycleSelectedLocale();
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

        private static void CycleSelectedLocale()
        {
            if (!LocalizationSettings.HasSettings)
            {
                return;
            }

            var locales = LocalizationSettings.AvailableLocales.Locales;
            if (locales == null || locales.Count == 0)
            {
                return;
            }

            var currentCode = LocalizationSettings.SelectedLocale != null
                ? LocalizationSettings.SelectedLocale.Identifier.Code
                : string.Empty;

            var index = 0;
            for (var i = 0; i < locales.Count; i++)
            {
                if (locales[i] != null && locales[i].Identifier.Code == currentCode)
                {
                    index = i;
                    break;
                }
            }

            var next = locales[(index + 1) % locales.Count];
            LocalizationSettings.SelectedLocale = next;
            LocalizationPreferences.SetSelectedLocaleCode(next.Identifier.Code);
        }

        private void TryEnterGameplay(string profileId, bool resumeExpedition)
        {
            if (_loadingGameplay)
            {
                return;
            }

            if (resumeExpedition && !_profilesService.HasActiveExpedition(profileId))
            {
                _status = Loc.Ui(UiKeys.ErrorNoExpeditionToContinue);
                return;
            }

            MenuSessionWriter.WriteGameplayLaunchIntent(profileId, resumeExpedition);

            var sceneToLoad = gameplaySceneName;
            if (string.IsNullOrWhiteSpace(sceneToLoad))
            {
                sceneToLoad = SceneManager.GetActiveScene().name;
            }

            if (!string.IsNullOrWhiteSpace(sceneToLoad))
            {
                StartCoroutine(LoadGameplaySceneCoroutine(sceneToLoad));
                return;
            }

            _state = ScreenState.Main;
            _status = Loc.Ui(UiKeys.ErrorGameplaySceneMissing);
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

        private void DrawLoadingOverlay()
        {
            var dim = new Color(0f, 0f, 0f, 0.5f);
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

            var title = Loc.Ui(UiKeys.LoadingGameplay);
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

        private IEnumerator LoadGameplaySceneCoroutine(string sceneName)
        {
            _loadingGameplay = true;
            _loadProgress = 0f;
            yield return null;

            AsyncOperation op;
            try
            {
                op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            }
            catch (Exception)
            {
                _loadingGameplay = false;
                _loadProgress = 0f;
                _status = Loc.Ui(UiKeys.ErrorGameplaySceneMissing);
                yield break;
            }

            if (op == null)
            {
                _loadingGameplay = false;
                _loadProgress = 0f;
                _status = Loc.Ui(UiKeys.ErrorGameplaySceneMissing);
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
            op.allowSceneActivation = true;
            yield return op;
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
