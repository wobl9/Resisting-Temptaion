using System;
using System.Collections.Generic;
using ShatteredForge.Combat;
using ShatteredForge.Core;
using ShatteredForge.Input;
using ShatteredForge.Items;
using ShatteredForge.Menu;
using ShatteredForge.Run;
using ShatteredForge.SceneFlow;
using ShatteredForge.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShatteredForge.Levels
{
    [RequireComponent(typeof(CombatRoomBootstrap))]
    public sealed class LevelSessionController : MonoBehaviour, ICombatRoomDriver, ICombatRoomSpawnConfigProvider
    {
        [Header("Data")]
        [Tooltip("If null, loads Resources/Levels/DefaultLevelCatalog.")]
        [SerializeField] private LevelCatalog levelCatalog;

        [Header("Profile storage")]
        [SerializeField] private ProfileStorageMode profileStorageMode = ProfileStorageMode.Local;
        [SerializeField] private string remoteProfileStorageBaseUrl = "";
        [SerializeField] private string remoteProfileStorageAuthBearer = "";

        private IProfileStorage _profilesService;
        private ProfileData _profile;
        private AccountState _account;
        private CombatRoomBootstrap _combatBootstrap;
        private RunState _runState;
        private List<RoomType> _rooms;
        private LevelDefinition _selectedLevel;

        private IPauseMenuView _pauseMenuView;
        private bool _pauseOpen;
        private bool _pauseSettingsOpen;
        private float _savedTimeScale = 1f;
        private float _masterVolume = 1f;
        private bool _fullscreen = true;
        private Resolution[] _resolutions = Array.Empty<Resolution>();
        private int _resolutionIndex;

        public bool IsInRun => _runState != null;
        public RunState CurrentRun => _runState;
        public ComputedCharacterStats CurrentComputedStats => _account?.computedStats;

        private void Awake()
        {
            _combatBootstrap = GetComponent<CombatRoomBootstrap>();
            _combatBootstrap.BindDriver(this);

            levelCatalog = levelCatalog != null
                ? levelCatalog
                : Resources.Load<LevelCatalog>("Levels/DefaultLevelCatalog");
            if (levelCatalog == null)
            {
                Debug.LogWarning($"{nameof(LevelSessionController)}: levelCatalog not found at Resources/Levels/DefaultLevelCatalog.");
                ReturnToCamp(success: false);
                return;
            }

            if (!PendingLevelRequest.TryConsume(out var levelId))
            {
                Debug.LogWarning($"{nameof(LevelSessionController)}: no pending level id. Returning to camp.");
                ReturnToCamp(success: false);
                return;
            }

            _selectedLevel = levelCatalog.Find(levelId);
            if (_selectedLevel == null)
            {
                Debug.LogWarning($"{nameof(LevelSessionController)}: level '{levelId}' not found in catalog.");
                ReturnToCamp(success: false);
                return;
            }

            _profilesService = ProfileStorageFactory.Create(
                profileStorageMode,
                remoteProfileStorageBaseUrl,
                remoteProfileStorageAuthBearer);
            LoadOrCreateAccount();
            CharacterStatsService.RecalculateForCamp(_account);

            _runState = new RunState
            {
                seed = UnityEngine.Random.Range(1, int.MaxValue),
                roomIndex = 0,
                hpState = 1f
            };
            _rooms = BuildRooms(_selectedLevel, _runState.seed);

            _masterVolume = AudioListener.volume;
            _fullscreen = Screen.fullScreen;
            _resolutions = Screen.resolutions;
            _resolutionIndex = FindCurrentResolutionIndex();
            EnsurePauseMenuView();

            _combatBootstrap.SetArenaOverride(_selectedLevel.customArenaPrefab);
            _combatBootstrap.OnRunStartedOrRoomAdvanced();
        }

        private void Update()
        {
            if (DemoInput.KeyDown(Key.Escape))
            {
                if (_pauseOpen && _pauseSettingsOpen)
                {
                    BackFromPauseSettings();
                    return;
                }

                SetPauseOpen(!_pauseOpen);
            }
        }

        public bool TryGetCurrentRoom(out RoomType room)
        {
            room = default;
            if (_runState == null || _rooms == null || _runState.roomIndex < 0 || _runState.roomIndex >= _rooms.Count)
            {
                return false;
            }

            room = _rooms[_runState.roomIndex];
            return true;
        }

        public void ApplyCurrentRoomClearedFromGameplay()
        {
            if (_runState == null || _rooms == null)
            {
                return;
            }

            if (!TryGetCurrentRoom(out var room))
            {
                ReturnToCamp(success: false);
                return;
            }

            var wasBoss = room == RoomType.Boss;
            _runState.roomIndex++;

            if (wasBoss)
            {
                AwardLevelLoot();
                ReturnToCamp(success: true);
                return;
            }

            _combatBootstrap.OnRunStartedOrRoomAdvanced();
        }

        public void ApplyPlayerDeathFromGameplay()
        {
            ReturnToCamp(success: false);
        }

        public void NotifyEnemyKillLoot(string enemyProfileId)
        {
            // Boss-only drop mode for this prototype.
        }

        public bool TryGetSpawnConfig(RoomType roomType, out CombatRoomSpawnConfig config)
        {
            config = default;
            if (_selectedLevel == null || _runState == null)
            {
                return false;
            }

            var tier = _selectedLevel.tier;
            var regularOverride = tier != null && tier.regularEnemyCountOverride > 0
                ? tier.regularEnemyCountOverride
                : _selectedLevel.regularEnemiesPerRoom;

            var combatId = LevelEnemyResolver.PickEnemyId(_selectedLevel, RoomType.Combat, new System.Random(_runState.seed + _runState.roomIndex + 11));
            var eliteId = LevelEnemyResolver.PickEnemyId(_selectedLevel, RoomType.Elite, new System.Random(_runState.seed + _runState.roomIndex + 29));
            var bossId = LevelEnemyResolver.PickEnemyId(_selectedLevel, RoomType.Boss, new System.Random(_runState.seed + _runState.roomIndex + 47));

            var hpMul = tier != null ? tier.enemyHealthMultiplier : 1f;
            var dmgMul = tier != null ? tier.enemyDamageMultiplier : 1f;
            var speedMul = tier != null ? tier.enemyMoveSpeedMultiplier : 1f;
            var atkSpeedMul = tier != null ? tier.enemyAttackSpeedMultiplier : 1f;

            config = new CombatRoomSpawnConfig(
                combatProfileId: combatId,
                eliteProfileId: eliteId,
                bossProfileId: bossId,
                regularEnemyCountOverride: regularOverride,
                healthMultiplier: hpMul,
                damageMultiplier: dmgMul,
                moveSpeedMultiplier: speedMul,
                attackSpeedMultiplier: atkSpeedMul);
            return true;
        }

        private List<RoomType> BuildRooms(LevelDefinition level, int seed)
        {
            var rng = new System.Random(seed == 0 ? 1 : seed);
            var min = Mathf.Max(3, level.minRooms);
            var max = Mathf.Max(min, level.maxRooms);
            var count = rng.Next(min, max + 1);
            var rooms = new List<RoomType>(count);

            for (var i = 0; i < count - 1; i++)
            {
                rooms.Add(RoomType.Combat);
            }

            var extraElite = level.tier != null ? level.tier.extraEliteSlots : 0;
            var eliteCount = Mathf.Clamp(level.eliteRoomCount + extraElite, 0, Mathf.Max(0, count - 1));
            for (var i = 0; i < eliteCount; i++)
            {
                if (rooms.Count == 0)
                {
                    break;
                }

                var idx = rng.Next(0, rooms.Count);
                rooms[idx] = RoomType.Elite;
            }

            rooms.Add(RoomType.Boss);
            return rooms;
        }

        private void AwardLevelLoot()
        {
            if (_selectedLevel == null || _account == null)
            {
                return;
            }

            if (_selectedLevel.guaranteedDropTemplateIds != null)
            {
                for (var i = 0; i < _selectedLevel.guaranteedDropTemplateIds.Count; i++)
                {
                    var templateId = _selectedLevel.guaranteedDropTemplateIds[i];
                    if (string.IsNullOrWhiteSpace(templateId))
                    {
                        continue;
                    }

                    _account.stash.Add(ItemInstanceFactory.Create(templateId.Trim()));
                }
            }

            var tierBonusRolls = _selectedLevel.tier != null ? _selectedLevel.tier.extraRandomLootRolls : 0;
            var rolls = Mathf.Max(0, _selectedLevel.randomDropRolls + tierBonusRolls);
            for (var i = 0; i < rolls; i++)
            {
                var rolled = RunLootService.RollRoomClearLoot(
                    _selectedLevel.randomLootTable,
                    RoomType.Boss,
                    _runState.seed,
                    _runState.roomIndex + i);
                _account.stash.AddRange(rolled);
            }

            if (_profile != null)
            {
                _profile.lastPlayedLevelId = _selectedLevel.levelId ?? string.Empty;
                _profile.clearedLevelIds ??= new List<string>();
                if (!string.IsNullOrWhiteSpace(_selectedLevel.levelId) &&
                    !_profile.clearedLevelIds.Contains(_selectedLevel.levelId))
                {
                    _profile.clearedLevelIds.Add(_selectedLevel.levelId);
                }
            }

            PersistAccount();
        }

        private void LoadOrCreateAccount()
        {
            var profileId = PlayerPrefs.GetString(MenuSessionPrefs.ActiveProfileIdKey, string.Empty);
            if (string.IsNullOrEmpty(profileId) || !_profilesService.TryLoadProfile(profileId, out _profile))
            {
                _profile = null;
                _account = BuildInitialAccount();
                return;
            }

            if (!string.IsNullOrWhiteSpace(_profile.accountJson))
            {
                try
                {
                    _account = JsonUtility.FromJson<AccountState>(_profile.accountJson) ?? BuildInitialAccount();
                    return;
                }
                catch
                {
                    // ignored
                }
            }

            _account = BuildInitialAccount();
        }

        private static AccountState BuildInitialAccount()
        {
            var account = new AccountState
            {
                gold = AccountEconomy.StarterGoldPurse,
                forgeDust = 2500,
                emberCore = 5,
                sigilToken = 20,
                insuranceSeal = 1,
                primaryStats = CharacterPrimaryStats.CreateDefault()
            };
            CharacterPaperDoll.EnsureList(account);
            return account;
        }

        private void PersistAccount()
        {
            if (_profile == null || _profilesService == null || _account == null)
            {
                return;
            }

            _profile.forgeDust = _account.forgeDust;
            _profile.emberCore = _account.emberCore;
            _profile.sigilToken = _account.sigilToken;
            _profile.insuranceSeal = _account.insuranceSeal;
            _profile.gold = _account.gold;
            _profile.accountJson = JsonUtility.ToJson(_account);
            _profilesService.SaveProfile(_profile);
        }

        private void ReturnToCamp(bool success)
        {
            Time.timeScale = 1f;
            _pauseOpen = false;
            _pauseSettingsOpen = false;
            _runState = null;
            PendingLevelRequest.Reset();
            MenuSessionWriter.ClearResumeIntent();
            if (success)
            {
                PersistAccount();
            }

            if (!SceneNavigation.IsBusy)
            {
                SceneNavigation.GoTo(SceneNames.CampHub);
            }
        }

        private void EnsurePauseMenuView()
        {
            var prefab = Resources.Load<PauseMenuView>(PauseMenuView.DefaultViewResourcesPath);
            if (prefab == null)
            {
                prefab = Resources.Load<PauseMenuView>(PauseMenuView.LegacyViewResourcesPath);
            }

            PauseMenuView concrete;
            if (prefab != null)
            {
                concrete = Instantiate(prefab, transform);
                concrete.name = "LevelPauseMenuUi";
            }
            else
            {
                var holder = new GameObject("LevelPauseMenuUi");
                holder.transform.SetParent(transform, false);
                concrete = holder.AddComponent<PauseMenuView>();
            }

            _pauseMenuView = concrete;
            _pauseMenuView.EnsureBuilt();
            _pauseMenuView.Configure(new PauseMenuConfig
            {
                title = "Pause",
                continueLabel = "Continue",
                settingsLabel = "Settings",
                exitLabel = "Abandon level",
                showSettingsButton = true,
                showExitButton = true
            });
            _pauseMenuView.Bind(new PauseMenuBinding
            {
                onContinue = () => SetPauseOpen(false),
                onOpenSettings = OpenPauseSettings,
                onExit = () => ReturnToCamp(success: false),
                onVolumeChanged = ApplyMasterVolume,
                onToggleFullscreen = ToggleFullscreen,
                onNextResolution = CycleResolution,
                onBackFromSettings = BackFromPauseSettings
            });
            _pauseMenuView.SetOpen(false);
        }

        private void SetPauseOpen(bool open)
        {
            if (_pauseOpen == open || _pauseMenuView == null)
            {
                return;
            }

            _pauseOpen = open;
            if (open)
            {
                _pauseSettingsOpen = false;
                _savedTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                _pauseMenuView.ShowMainPage();
            }
            else
            {
                _pauseSettingsOpen = false;
                Time.timeScale = Mathf.Approximately(_savedTimeScale, 0f) ? 1f : _savedTimeScale;
            }

            _pauseMenuView.SetOpen(open);
        }

        private void OpenPauseSettings()
        {
            if (!_pauseOpen || _pauseMenuView == null)
            {
                return;
            }

            _pauseSettingsOpen = true;
            _pauseMenuView.ShowSettingsPage(_masterVolume, _fullscreen, GetResolutionLabel(_resolutionIndex));
        }

        private void BackFromPauseSettings()
        {
            if (!_pauseOpen || _pauseMenuView == null)
            {
                return;
            }

            _pauseSettingsOpen = false;
            _pauseMenuView.ShowMainPage();
        }

        private void ApplyMasterVolume(float value)
        {
            _masterVolume = Mathf.Clamp01(value);
            AudioListener.volume = _masterVolume;
            if (_pauseOpen && _pauseSettingsOpen && _pauseMenuView != null)
            {
                _pauseMenuView.ShowSettingsPage(_masterVolume, _fullscreen, GetResolutionLabel(_resolutionIndex));
            }
        }

        private void ToggleFullscreen()
        {
            _fullscreen = !_fullscreen;
            Screen.fullScreen = _fullscreen;
            if (_pauseOpen && _pauseSettingsOpen && _pauseMenuView != null)
            {
                _pauseMenuView.ShowSettingsPage(_masterVolume, _fullscreen, GetResolutionLabel(_resolutionIndex));
            }
        }

        private void CycleResolution()
        {
            if (_resolutions == null || _resolutions.Length == 0)
            {
                return;
            }

            _resolutionIndex = (_resolutionIndex + 1) % _resolutions.Length;
            var resolution = _resolutions[_resolutionIndex];
            Screen.SetResolution(resolution.width, resolution.height, _fullscreen);
            if (_pauseOpen && _pauseSettingsOpen && _pauseMenuView != null)
            {
                _pauseMenuView.ShowSettingsPage(_masterVolume, _fullscreen, GetResolutionLabel(_resolutionIndex));
            }
        }

        private int FindCurrentResolutionIndex()
        {
            if (_resolutions == null || _resolutions.Length == 0)
            {
                return 0;
            }

            for (var i = 0; i < _resolutions.Length; i++)
            {
                var resolution = _resolutions[i];
                if (resolution.width == Screen.currentResolution.width &&
                    resolution.height == Screen.currentResolution.height)
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
                return "N/A";
            }

            var resolution = _resolutions[Mathf.Clamp(index, 0, _resolutions.Length - 1)];
            return $"{resolution.width} x {resolution.height} @ {Mathf.RoundToInt((float)resolution.refreshRateRatio.value)}Hz";
        }
    }
}
