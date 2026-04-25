using System.Collections.Generic;
using UnityEngine;

namespace ShatteredForge.Menu
{
    public class MainMenuController : MonoBehaviour
    {
        private enum ScreenState
        {
            Main,
            ProfileActions,
            Profiles,
            NewGame,
            Settings
        }

        [Header("Scene routing")]
        [SerializeField] private string gameplaySceneName = "";

        private ScreenState _state = ScreenState.Main;
        private ProfileStorageService _profilesService;
        private List<ProfileSummary> _profiles = new();
        private string _activeProfileId;
        private string _newProfileName = "New Hunter";
        private string _status = "Welcome to Shattered Forge";

        private float _masterVolume = 1f;
        private bool _fullscreen = true;
        private Resolution[] _resolutions;
        private int _resolutionIndex;

        private void Awake()
        {
            _profilesService = new ProfileStorageService();
            ReloadProfiles();

            _masterVolume = AudioListener.volume;
            _fullscreen = Screen.fullScreen;
            _resolutions = Screen.resolutions;
            _resolutionIndex = FindCurrentResolutionIndex();
        }

        private void OnGUI()
        {
            DrawActiveProfileCornerLabel();

            const float width = 560f;
            var x = (Screen.width - width) * 0.5f;
            var y = 60f;
            var rect = new Rect(x, y, width, Screen.height - 120f);

            GUI.Box(rect, "");
            GUILayout.BeginArea(new Rect(rect.x + 24f, rect.y + 24f, rect.width - 48f, rect.height - 48f));
            GUILayout.Label("SHATTERED FORGE", HeaderStyle());
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
                case ScreenState.NewGame:
                    DrawNewGameMenu();
                    break;
                case ScreenState.Settings:
                    DrawSettingsMenu();
                    break;
            }

            GUILayout.EndArea();
        }

        private void DrawMainMenu()
        {
            var hasActiveProfile = !string.IsNullOrEmpty(_activeProfileId);

            if (hasActiveProfile)
            {
                if (GUILayout.Button("1) Continue Game", ButtonOptions()))
                {
                    _status = $"Continuing as {GetActiveProfileName()}";
                    TryEnterGameplay(_activeProfileId);
                }
            }
            else if (GUILayout.Button("1) Select Profile", ButtonOptions()))
            {
                ReloadProfiles();
                _state = ScreenState.Profiles;
            }

            var newGameButtonText = hasActiveProfile ? "2) New Game" : "2) Create New Game";
            if (GUILayout.Button(newGameButtonText, ButtonOptions()))
            {
                if (hasActiveProfile)
                {
                    StartNewGameForActiveProfile();
                }
                else
                {
                    _state = ScreenState.NewGame;
                }
            }

            var settingsButtonText = hasActiveProfile ? "3) Settings" : "3) Settings";
            if (GUILayout.Button(settingsButtonText, ButtonOptions()))
            {
                _state = ScreenState.Settings;
            }

            var quitButtonText = hasActiveProfile ? "4) Quit Game" : "4) Quit Game";
            if (GUILayout.Button(quitButtonText, ButtonOptions()))
            {
                QuitGame();
            }

            GUILayout.Space(12f);
            GUILayout.Label($"Active profile: {GetActiveProfileName()}");
        }

        private void DrawActiveProfileCornerLabel()
        {
            if (string.IsNullOrEmpty(_activeProfileId))
            {
                return;
            }

            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperRight
            };

            var rect = new Rect(Screen.width - 320f, 12f, 300f, 24f);
            if (GUI.Button(rect, $"Profile: {GetActiveProfileName()}", buttonStyle))
            {
                _state = ScreenState.ProfileActions;
                _status = "Profile menu opened.";
            }
        }

        private void DrawProfileActionsMenu()
        {
            GUILayout.Label("Profile Menu", HeaderStyle());
            GUILayout.Space(8f);
            GUILayout.Label($"Current: {GetActiveProfileName()}");
            GUILayout.Space(12f);

            if (GUILayout.Button("Switch Current Profile", ButtonOptions()))
            {
                ReloadProfiles();
                _state = ScreenState.Profiles;
            }

            if (GUILayout.Button("Create New Profile", ButtonOptions()))
            {
                _state = ScreenState.NewGame;
            }

            GUILayout.Space(14f);
            if (GUILayout.Button("Back", ButtonOptions()))
            {
                _state = ScreenState.Main;
            }
        }

        private void DrawProfilesMenu()
        {
            GUILayout.Label("Select Profile", HeaderStyle());
            GUILayout.Space(8f);

            if (_profiles.Count == 0)
            {
                GUILayout.Label("No profiles yet. Create a new game first.");
            }
            else
            {
                foreach (var profile in _profiles)
                {
                    var isActive = profile.id == _activeProfileId;
                    var title = isActive ? $"{profile.displayName}  (ACTIVE)" : profile.displayName;
                    if (GUILayout.Button(title, ButtonOptions()))
                    {
                        _profilesService.SetActiveProfile(profile.id);
                        ReloadProfiles();
                        _status = $"Profile selected: {profile.displayName}";
                        TryEnterGameplay(profile.id);
                    }
                }
            }

            GUILayout.Space(14f);
            if (GUILayout.Button("Back", ButtonOptions()))
            {
                _state = ScreenState.Main;
            }
        }

        private void DrawNewGameMenu()
        {
            if (!string.IsNullOrEmpty(_activeProfileId))
            {
                GUILayout.Label("New Game", HeaderStyle());
                GUILayout.Space(8f);
                GUILayout.Label($"Profile: {GetActiveProfileName()}");
                GUILayout.Label("A new run will start on this profile.");
                GUILayout.Space(10f);
                if (GUILayout.Button("Start New Game", ButtonOptions()))
                {
                    StartNewGameForActiveProfile();
                }

                if (GUILayout.Button("Back", ButtonOptions()))
                {
                    _state = ScreenState.Main;
                }

                return;
            }

            GUILayout.Label("Create New Game", HeaderStyle());
            GUILayout.Space(6f);
            GUILayout.Label("Profile name:");
            _newProfileName = GUILayout.TextField(_newProfileName, GUILayout.Height(28f));

            GUILayout.Space(10f);
            if (GUILayout.Button("Create Profile and Start", ButtonOptions()))
            {
                var profileId = _profilesService.CreateProfile(_newProfileName);
                ReloadProfiles();
                _status = $"Created profile: {GetActiveProfileName()}";
                TryEnterGameplay(profileId);
            }

            if (GUILayout.Button("Back", ButtonOptions()))
            {
                _state = ScreenState.Main;
            }
        }

        private void DrawSettingsMenu()
        {
            GUILayout.Label("Settings", HeaderStyle());
            GUILayout.Space(8f);

            GUILayout.Label($"Master volume: {Mathf.RoundToInt(_masterVolume * 100f)}%");
            var newVolume = GUILayout.HorizontalSlider(_masterVolume, 0f, 1f, GUILayout.Height(24f));
            if (!Mathf.Approximately(newVolume, _masterVolume))
            {
                _masterVolume = newVolume;
                AudioListener.volume = _masterVolume;
            }

            GUILayout.Space(8f);
            var fullscreenLabel = _fullscreen ? "Fullscreen: ON" : "Fullscreen: OFF";
            if (GUILayout.Button(fullscreenLabel, ButtonOptions()))
            {
                _fullscreen = !_fullscreen;
                Screen.fullScreen = _fullscreen;
            }

            if (_resolutions != null && _resolutions.Length > 0)
            {
                GUILayout.Space(8f);
                GUILayout.Label($"Resolution: {GetResolutionLabel(_resolutionIndex)}");
                if (GUILayout.Button("Cycle Resolution", ButtonOptions()))
                {
                    _resolutionIndex = (_resolutionIndex + 1) % _resolutions.Length;
                    var r = _resolutions[_resolutionIndex];
                    Screen.SetResolution(r.width, r.height, _fullscreen);
                }
            }

            GUILayout.Space(14f);
            if (GUILayout.Button("Back", ButtonOptions()))
            {
                _state = ScreenState.Main;
            }
        }

        private void ReloadProfiles()
        {
            _profiles = _profilesService.LoadProfiles(out _activeProfileId);
        }

        private string GetActiveProfileName()
        {
            if (string.IsNullOrEmpty(_activeProfileId))
            {
                return "none";
            }

            foreach (var profile in _profiles)
            {
                if (profile.id == _activeProfileId)
                {
                    return profile.displayName;
                }
            }

            return "unknown";
        }

        private void TryEnterGameplay(string profileId)
        {
            PlayerPrefs.SetString("sf.active_profile_id", profileId);
            PlayerPrefs.Save();

            var sceneToLoad = gameplaySceneName;
            if (string.IsNullOrWhiteSpace(sceneToLoad))
            {
                // Fallback: if no explicit gameplay scene is set, restart current scene
                // so New Game / Continue always start gameplay instead of only showing status text.
                sceneToLoad = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            }

            if (!string.IsNullOrWhiteSpace(sceneToLoad))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoad);
                return;
            }

            _state = ScreenState.Main;
            _status = "Failed to start game: gameplay scene is not configured.";
        }

        private void StartNewGameForActiveProfile()
        {
            if (string.IsNullOrEmpty(_activeProfileId))
            {
                _state = ScreenState.NewGame;
                return;
            }

            _status = $"Starting new game on profile: {GetActiveProfileName()}";
            TryEnterGameplay(_activeProfileId);
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
                return "n/a";
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
