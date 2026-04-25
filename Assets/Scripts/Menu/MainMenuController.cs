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
            NewProfile,
            NewExpeditionConfirm,
            Settings
        }

        [Header("Scene routing")]
        [SerializeField] private string gameplaySceneName = "";

        private ScreenState _state = ScreenState.Main;
        private ProfileStorageService _profilesService;
        private List<ProfileSummary> _profiles = new();
        private string _activeProfileId;

        private string _deleteCandidateProfileId;
        private string _newProfileName = "Новый охотник";
        private string _status = "Добро пожаловать в Shattered Forge";

        private bool _cachedHasActiveExpedition;

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
            DrawActiveProfileEntry();

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
                if (GUILayout.Button("Новая игра", ButtonOptions()))
                {
                    _state = ScreenState.NewProfile;
                }

                if (GUILayout.Button("Настройки", ButtonOptions()))
                {
                    _state = ScreenState.Settings;
                }

                if (GUILayout.Button("Выход из игры", ButtonOptions()))
                {
                    QuitGame();
                }

                return;
            }

            if (!hasActiveProfile)
            {
                GUILayout.Label("Нет активного профиля. Выберите профиль.");
                if (GUILayout.Button("Выбрать профиль", ButtonOptions()))
                {
                    ReloadProfiles();
                    _state = ScreenState.Profiles;
                }

                GUILayout.Space(10f);
                if (GUILayout.Button("Настройки", ButtonOptions()))
                {
                    _state = ScreenState.Settings;
                }

                if (GUILayout.Button("Выход из игры", ButtonOptions()))
                {
                    QuitGame();
                }

                return;
            }

            if (_cachedHasActiveExpedition && GUILayout.Button("Продолжить игру", ButtonOptions()))
            {
                TryEnterGameplay(_activeProfileId, resumeExpedition: true);
            }

            if (GUILayout.Button("Новая игра", ButtonOptions()))
            {
                if (_cachedHasActiveExpedition)
                {
                    _state = ScreenState.NewExpeditionConfirm;
                    return;
                }

                TryEnterGameplay(_activeProfileId, resumeExpedition: false);
            }

            if (GUILayout.Button("Настройки", ButtonOptions()))
            {
                _state = ScreenState.Settings;
            }

            if (GUILayout.Button("Выход из игры", ButtonOptions()))
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
            if (GUI.Button(rect, $"Профиль: {GetActiveProfileName()}", buttonStyle))
            {
                _state = ScreenState.ProfileActions;
                _status = "Меню профиля.";
            }
        }

        private void DrawProfileActionsMenu()
        {
            GUILayout.Label("Профиль", HeaderStyle());
            GUILayout.Space(8f);
            GUILayout.Label($"Текущий: {GetActiveProfileName()}");
            GUILayout.Space(12f);

            if (GUILayout.Button("Сменить профиль", ButtonOptions()))
            {
                ReloadProfiles();
                _state = ScreenState.Profiles;
            }

            if (GUILayout.Button("Создать новый профиль", ButtonOptions()))
            {
                _state = ScreenState.NewProfile;
            }

            GUILayout.Space(8f);
            DrawDeleteProfileControls();

            GUILayout.Space(14f);
            if (GUILayout.Button("Назад", ButtonOptions()))
            {
                _state = ScreenState.Main;
            }
        }

        private void DrawProfilesMenu()
        {
            GUILayout.Label("Выбор профиля", HeaderStyle());
            GUILayout.Space(8f);

            if (_profiles.Count == 0)
            {
                GUILayout.Label("Профилей нет. Создайте новую игру.");
            }
            else
            {
                foreach (var profile in _profiles)
                {
                    var isActive = profile.id == _activeProfileId;
                    var title = isActive ? $"{profile.displayName}  (АКТИВНЫЙ)" : profile.displayName;
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(title, ButtonOptions()))
                    {
                        _profilesService.SetActiveProfile(profile.id);
                        ReloadProfiles();
                        _status = $"Профиль выбран: {profile.displayName}";
                        _state = ScreenState.Main;
                    }

                    if (GUILayout.Button("Удалить", GUILayout.Height(36f), GUILayout.Width(110f)))
                    {
                        _deleteCandidateProfileId = profile.id;
                    }

                    GUILayout.EndHorizontal();

                    if (_deleteCandidateProfileId == profile.id)
                    {
                        GUILayout.Label($"Удалить профиль '{profile.displayName}'?");
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Да, удалить", GUILayout.Height(30f)))
                        {
                            DeleteProfile(profile.id);
                        }

                        if (GUILayout.Button("Отмена", GUILayout.Height(30f)))
                        {
                            _deleteCandidateProfileId = null;
                        }

                        GUILayout.EndHorizontal();
                    }
                }
            }

            GUILayout.Space(14f);
            if (GUILayout.Button("Назад", ButtonOptions()))
            {
                _state = ScreenState.Main;
            }
        }

        private void DrawNewProfileMenu()
        {
            GUILayout.Label("Новая игра", HeaderStyle());
            GUILayout.Space(6f);
            GUILayout.Label("Имя профиля:");
            _newProfileName = GUILayout.TextField(_newProfileName, GUILayout.Height(28f));

            GUILayout.Space(10f);
            if (GUILayout.Button("Подтвердить и начать", ButtonOptions()))
            {
                var profileId = _profilesService.CreateProfile(_newProfileName);
                ReloadProfiles();
                _status = $"Создан профиль: {GetActiveProfileName()}";
                TryEnterGameplay(profileId, resumeExpedition: false);
            }

            if (GUILayout.Button("Назад", ButtonOptions()))
            {
                _state = ScreenState.Main;
            }
        }

        private void DrawNewExpeditionConfirmMenu()
        {
            GUILayout.Label("Новая вылазка", HeaderStyle());
            GUILayout.Space(8f);
            GUILayout.Label($"Профиль: {GetActiveProfileName()}");
            GUILayout.Label("У вас есть активная вылазка. Начать новую вылазку и заменить текущую?");
            GUILayout.Space(10f);

            if (GUILayout.Button("Да, начать новую вылазку", ButtonOptions()))
            {
                TryEnterGameplay(_activeProfileId, resumeExpedition: false);
            }

            if (GUILayout.Button("Назад", ButtonOptions()))
            {
                _state = ScreenState.Main;
            }
        }

        private void DrawSettingsMenu()
        {
            GUILayout.Label("Настройки", HeaderStyle());
            GUILayout.Space(8f);

            GUILayout.Label($"Громкость: {Mathf.RoundToInt(_masterVolume * 100f)}%");
            var newVolume = GUILayout.HorizontalSlider(_masterVolume, 0f, 1f, GUILayout.Height(24f));
            if (!Mathf.Approximately(newVolume, _masterVolume))
            {
                _masterVolume = newVolume;
                AudioListener.volume = _masterVolume;
            }

            GUILayout.Space(8f);
            var fullscreenLabel = _fullscreen ? "Полный экран: ВКЛ" : "Полный экран: ВЫКЛ";
            if (GUILayout.Button(fullscreenLabel, ButtonOptions()))
            {
                _fullscreen = !_fullscreen;
                Screen.fullScreen = _fullscreen;
            }

            if (_resolutions != null && _resolutions.Length > 0)
            {
                GUILayout.Space(8f);
                GUILayout.Label($"Разрешение: {GetResolutionLabel(_resolutionIndex)}");
                if (GUILayout.Button("Следующее разрешение", ButtonOptions()))
                {
                    _resolutionIndex = (_resolutionIndex + 1) % _resolutions.Length;
                    var r = _resolutions[_resolutionIndex];
                    Screen.SetResolution(r.width, r.height, _fullscreen);
                }
            }

            GUILayout.Space(14f);
            if (GUILayout.Button("Назад", ButtonOptions()))
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
                return "нет";
            }

            foreach (var profile in _profiles)
            {
                if (profile.id == _activeProfileId)
                {
                    return profile.displayName;
                }
            }

            return "неизвестно";
        }

        private void TryEnterGameplay(string profileId, bool resumeExpedition)
        {
            if (resumeExpedition && !_profilesService.HasActiveExpedition(profileId))
            {
                _status = "Нет активной вылазки для продолжения.";
                return;
            }

            MenuSessionWriter.WriteGameplayLaunchIntent(profileId, resumeExpedition);

            var sceneToLoad = gameplaySceneName;
            if (string.IsNullOrWhiteSpace(sceneToLoad))
            {
                sceneToLoad = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            }

            if (!string.IsNullOrWhiteSpace(sceneToLoad))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoad);
                return;
            }

            _state = ScreenState.Main;
            _status = "Не удалось запустить игру: не настроена сцена геймплея.";
        }

        private void DrawDeleteProfileControls()
        {
            if (string.IsNullOrEmpty(_activeProfileId))
            {
                GUILayout.Label("Нет активного профиля для удаления.");
                return;
            }

            if (_deleteCandidateProfileId != _activeProfileId)
            {
                if (GUILayout.Button("Удалить текущий профиль", ButtonOptions()))
                {
                    _deleteCandidateProfileId = _activeProfileId;
                }

                return;
            }

            GUILayout.Label($"Удалить активный профиль '{GetActiveProfileName()}'?");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Да, удалить", GUILayout.Height(30f)))
            {
                DeleteProfile(_activeProfileId);
            }

            if (GUILayout.Button("Отмена", GUILayout.Height(30f)))
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
                _status = "Удаление не удалось: профиль не найден.";
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
            _status = "Профиль удалён.";
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
                return "н/д";
            }

            var r = _resolutions[Mathf.Clamp(index, 0, _resolutions.Length - 1)];
            return $"{r.width} x {r.height} @ {Mathf.RoundToInt((float)r.refreshRateRatio.value)}Гц";
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
