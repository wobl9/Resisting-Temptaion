using System;
using System.Collections.Generic;
using ShatteredForge.Combat;
using ShatteredForge.Core;
using ShatteredForge.Input;
using ShatteredForge.Menu;
using ShatteredForge.Progression;
using ShatteredForge.Items;
using ShatteredForge.Run;
using ShatteredForge.SceneFlow;
using ShatteredForge.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShatteredForge.Prototype
{
    [RequireComponent(typeof(CombatRoomBootstrap))]
    public class PlayableLoopDemo : MonoBehaviour
    {
        private enum DemoState
        {
            Hub,
            InRun,
            Resolved
        }

        [Header("Run Settings")]
        [SerializeField] private int minRoomsPerAct = 8;
        [SerializeField] private int maxRoomsPerAct = 14;
        [SerializeField] private int startingHpPercent = 100;
        [SerializeField] private bool autoInsureFirstItem = true;

        [Header("Combat (same GameObject as GameBootstrap)")]
        [SerializeField] private CombatRoomBootstrap combatBootstrap;

        [Header("Items")]
        [Tooltip("If null, loads Resources/Items/DefaultItemCatalog.")]
        [SerializeField] private ItemCatalog itemCatalog;

        [Header("Profile storage")]
        [SerializeField] private ProfileStorageMode profileStorageMode = ProfileStorageMode.Local;
        [SerializeField] private string remoteProfileStorageBaseUrl = "";
        [SerializeField] private string remoteProfileStorageAuthBearer = "";

        private IProfileStorage _profilesService;
        private string _profileId;
        private ProfileData _profile;

        private AccountState _account;
        private RunSessionController _runController;
        private RunGenerator _runGenerator;
        private DemoState _state = DemoState.Hub;

        private List<RoomType> _rooms;
        private string _lastOutcome = "No runs yet.";
        private PlayerInventoryPanel _inventoryPanel;

        public bool IsInRun => _state == DemoState.InRun && _runController.CurrentRun != null;
        public RunState CurrentRun => _runController?.CurrentRun;
        public ComputedCharacterStats CurrentComputedStats => _account?.computedStats;

        private void Awake()
        {
            ItemCatalogRuntime.Current = itemCatalog != null
                ? itemCatalog
                : Resources.Load<ItemCatalog>("Items/DefaultItemCatalog");
            ItemStatBonusCatalogRuntime.Current = Resources.Load<ItemStatBonusCatalog>("Items/DefaultItemStatBonusCatalog");
            if (ItemCatalogRuntime.Current == null)
            {
                Debug.LogWarning(
                    $"{nameof(PlayableLoopDemo)}: ItemCatalog not assigned and Resources.Load(\"Items/DefaultItemCatalog\") failed.");
            }

            _profilesService = ProfileStorageFactory.Create(
                profileStorageMode,
                remoteProfileStorageBaseUrl,
                remoteProfileStorageAuthBearer);
            _runController = new RunSessionController(new RiskLossService());
            combatBootstrap = GetComponent<CombatRoomBootstrap>();
            if (combatBootstrap == null)
            {
                combatBootstrap = gameObject.AddComponent<CombatRoomBootstrap>();
            }

            _profileId = PlayerPrefs.GetString(MenuSessionPrefs.ActiveProfileIdKey, string.Empty);
            var resumeExpedition = PlayerPrefs.GetInt(MenuSessionPrefs.ResumeExpeditionKey, 0) == 1;
            PlayerPrefs.DeleteKey(MenuSessionPrefs.ResumeExpeditionKey);
            PlayerPrefs.Save();

            if (string.IsNullOrEmpty(_profileId) || !_profilesService.TryLoadProfile(_profileId, out _profile))
            {
                _profile = null;
                _profileId = string.Empty;
                _account = BuildInitialAccount();
                CharacterPaperDoll.EnsureList(_account);
                CharacterStatsService.RecalculateForCamp(_account);
                _lastOutcome = "No profile loaded (demo fallback).";
                PendingCampDungeonRequest.Consume();
                MenuSessionWriter.ConsumePendingDungeonEntry();
                BootstrapFreshExpedition();
                return;
            }

            _account = LoadOrCreateAccount(_profile);
            CharacterPaperDoll.EnsureList(_account);
            CharacterStatsService.RecalculateForCamp(_account);
            SyncLegacyResourcesFromProfile(_profile, _account);
            ProfileAccountGoldMigration.ApplyMissingGoldFieldOnce(_profile, _account, PersistAccount);

            var savedMinRooms = _profile.expeditionMinRoomsPerAct;
            var savedMaxRooms = _profile.expeditionMaxRoomsPerAct;
            var savedStartingHp = _profile.expeditionStartingHpPercent;

            minRoomsPerAct = savedMinRooms > 0 ? savedMinRooms : minRoomsPerAct;
            maxRoomsPerAct = savedMaxRooms > 0 ? Mathf.Max(minRoomsPerAct, savedMaxRooms) : maxRoomsPerAct;
            startingHpPercent = savedStartingHp > 0 ? Mathf.Clamp(savedStartingHp, 1, 100) : startingHpPercent;
            autoInsureFirstItem = _profile.expeditionAutoInsureFirstItem;

            if (resumeExpedition && _profile.hasActiveExpedition)
            {
                PendingCampDungeonRequest.Reset();
                MenuSessionWriter.ConsumePendingDungeonEntry();
                RestoreExpeditionFromProfile(_profile);
                _lastOutcome = "Expedition resumed.";
                PersistAccount();
                PersistExpedition();
                return;
            }

            if (resumeExpedition && !_profile.hasActiveExpedition)
            {
                _lastOutcome = "Resume requested but no expedition save exists.";
            }

            ClearExpedition(_profile, save: true);
            var enterDungeonFromHub =
                PendingCampDungeonRequest.Consume() || MenuSessionWriter.ConsumePendingDungeonEntry();
            if (enterDungeonFromHub)
            {
                BootstrapFreshExpedition();
                _lastOutcome = _state == DemoState.InRun
                    ? "Expedition started from camp."
                    : "Cannot start run: no gear in stash. Check profile save.";
            }
            else
            {
                _state = DemoState.Hub;
                _lastOutcome = "Camp hub expected before dungeon. Press R to start (editor), or return to menu.";
            }

            PersistAccount();
            PersistExpedition();
        }

        private void Start()
        {
            EnsureInventoryPanel();
            if (IsInRun)
            {
                combatBootstrap?.OnRunStartedOrRoomAdvanced();
            }
        }

        private void EnsureInventoryPanel()
        {
            if (_inventoryPanel != null)
            {
                return;
            }

            _inventoryPanel = GetComponent<PlayerInventoryPanel>();
            if (_inventoryPanel == null)
            {
                _inventoryPanel = gameObject.AddComponent<PlayerInventoryPanel>();
            }

            _inventoryPanel.BindGameplay(
                _account,
                () => _runController?.CurrentRun,
                () =>
                {
                    PersistAccount();
                    PersistExpedition();
                });
        }

        private void Update()
        {
            if (DemoInput.KeyDown(Key.H) && _state == DemoState.Resolved)
            {
                _state = DemoState.Hub;
                _lastOutcome = "Returned to hub. Press R to start another run.";
                ClearExpedition(_profile, save: true);
                PersistAccount();
            }

            if (DemoInput.KeyDown(Key.R))
            {
                TryStartRun();
            }

            if (DemoInput.KeyDown(Key.C) && combatBootstrap == null)
            {
                SimulateClearRoom();
            }

            if (DemoInput.KeyDown(Key.E))
            {
                ExtractRun();
            }

            if (DemoInput.KeyDown(Key.K))
            {
                KillPlayer();
            }
        }

        private void TryStartRun()
        {
            if (_state == DemoState.InRun || !InventoryEquipmentRules.AccountCanStartRun(_account))
            {
                return;
            }

            var runState = new RunState
            {
                hpState = Mathf.Clamp01(startingHpPercent / 100f)
            };

            if (!InventoryEquipmentRules.PopulateStartingLoadout(
                    runState,
                    _account,
                    autoInsureFirstItem,
                    ref _account.insuranceSeal))
            {
                return;
            }

            _runController.StartRun(UnityEngine.Random.Range(1, int.MaxValue), runState);

            _runGenerator = new RunGenerator(_runController.CurrentRun.seed);
            _rooms = _runGenerator.GenerateAct(UnityEngine.Random.Range(minRoomsPerAct, maxRoomsPerAct + 1));
            _state = DemoState.InRun;
            _lastOutcome = "Run started.";

            combatBootstrap?.OnRunStartedOrRoomAdvanced();
            PersistAccount();
            PersistExpedition();
        }

        public bool TryGetCurrentRoom(out RoomType room)
        {
            room = default;
            if (!IsInRun || _rooms == null || _runController.CurrentRun.roomIndex >= _rooms.Count)
            {
                return false;
            }

            room = _rooms[_runController.CurrentRun.roomIndex];
            return true;
        }

        public void ApplyCurrentRoomClearedFromGameplay()
        {
            ApplyRoomClearInternal(useCombatHpRules: combatBootstrap != null);
            combatBootstrap?.OnRunStartedOrRoomAdvanced();
        }

        public void ApplyPlayerDeathFromGameplay()
        {
            KillPlayer();
        }

        private void SimulateClearRoom()
        {
            ApplyRoomClearInternal(useCombatHpRules: false);
        }

        private void ApplyRoomClearInternal(bool useCombatHpRules)
        {
            if (_state != DemoState.InRun || _runController.CurrentRun == null)
            {
                return;
            }

            var run = _runController.CurrentRun;
            if (run.roomIndex >= _rooms.Count)
            {
                _lastOutcome = "All rooms already cleared. Extract now (E).";
                return;
            }

            var room = _rooms[run.roomIndex];
            run.carryLoot.Add(GenerateLootForRoom(room));
            if (!useCombatHpRules)
            {
                run.hpState = Mathf.Clamp01(run.hpState - UnityEngine.Random.Range(0.05f, 0.20f));
            }
            else
            {
                run.hpState = Mathf.Clamp01(run.hpState + 0.03f);
            }

            _runController.AdvanceRoom();

            if (run.hpState <= 0.01f)
            {
                KillPlayer();
                return;
            }

            _lastOutcome = $"Cleared room {run.roomIndex}/{_rooms.Count}. Current HP: {(int)(run.hpState * 100)}%.";
            PersistAccount();
            PersistExpedition();
        }

        private void ExtractRun()
        {
            if (_state != DemoState.InRun || _runController.CurrentRun == null)
            {
                return;
            }

            var carried = _runController.CurrentRun.carryLoot.Count;
            _runController.ResolveExtraction(_account);
            _state = DemoState.Resolved;
            _lastOutcome = $"Extracted successfully with {carried} loot items.";
            PersistAccount();
            PersistExpedition();
        }

        private void KillPlayer()
        {
            if (_state != DemoState.InRun || _runController.CurrentRun == null)
            {
                return;
            }

            var equipped = _runController.CurrentRun.equippedLoadout.Count;
            var loot = _runController.CurrentRun.carryLoot.Count;
            _runController.ResolveDeath(_account);
            _state = DemoState.Resolved;
            _lastOutcome = $"Player died. Lost {equipped} equipped items and {loot} carried loot (except insured).";
            PersistAccount();
            PersistExpedition();
        }

        private void OnGUI()
        {
            const int x = 20;
            var y = 20;
            const int line = 24;

            var roomCount = _rooms != null ? _rooms.Count : 0;
            GUI.Label(new Rect(x, y, 900, line), "Shattered Forge - Playable Loop Demo");
            y += line;
            GUI.Label(new Rect(x, y, 900, line), $"State: {_state}");
            y += line;
            GUI.Label(new Rect(x, y, 900, line), $"Stash items: {_account.stash.Count} | Insurance seals: {_account.insuranceSeal}");
            y += line;

            if (_runController.CurrentRun != null)
            {
                GUI.Label(new Rect(x, y, 900, line), $"Run HP: {(int)(_runController.CurrentRun.hpState * 100)}% | Room: {_runController.CurrentRun.roomIndex}/{roomCount}");
                y += line;
                GUI.Label(new Rect(x, y, 900, line), $"Carry loot: {_runController.CurrentRun.carryLoot.Count}");
                y += line;
            }

            GUI.Label(new Rect(x, y, 1200, line), $"Last outcome: {_lastOutcome}");
            y += line + 8;
            var controls = combatBootstrap != null
                ? "WASD move | Auto-fire at nearest enemy | Tab инвентарь | R Start (hub) | E Extract | K Die | H hub after run"
                : "Controls: Tab инвентарь | R - Start Run, C - Clear Room, E - Extract, K - Die | H hub after run";
            GUI.Label(new Rect(x, y, 1200, line), controls);
        }

        private void BootstrapFreshExpedition()
        {
            if (_state == DemoState.InRun)
            {
                return;
            }

            EnsureMinimalRunGear(_account);
            if (!InventoryEquipmentRules.AccountCanStartRun(_account))
            {
                return;
            }

            var runState = new RunState
            {
                hpState = Mathf.Clamp01(startingHpPercent / 100f)
            };

            if (!InventoryEquipmentRules.PopulateStartingLoadout(
                    runState,
                    _account,
                    autoInsureFirstItem,
                    ref _account.insuranceSeal))
            {
                return;
            }

            _runController.StartRun(UnityEngine.Random.Range(1, int.MaxValue), runState);

            _runGenerator = new RunGenerator(_runController.CurrentRun.seed);
            _rooms = _runGenerator.GenerateAct(UnityEngine.Random.Range(minRoomsPerAct, maxRoomsPerAct + 1));
            _state = DemoState.InRun;
            _lastOutcome = "Run started.";
        }

        private static void EnsureMinimalRunGear(AccountState account)
        {
            CharacterPaperDoll.EnsureList(account);
            if (account == null || account.stash.Count > 0 || CharacterPaperDoll.HasAnyEquippedItem(account))
            {
                return;
            }

            var seed = BuildInitialAccount();
            foreach (var item in seed.stash)
            {
                account.stash.Add(new ItemInstance
                {
                    id = Guid.NewGuid().ToString(),
                    templateId = item.templateId,
                    rarity = item.rarity,
                    enhanceLevel = item.enhanceLevel
                });
            }
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

            account.stash.Add(new ItemInstance
            {
                id = Guid.NewGuid().ToString(),
                templateId = "weapon_simple_sword",
                rarity = "Обычная",
                enhanceLevel = 0
            });
            account.stash.Add(new ItemInstance
            {
                id = Guid.NewGuid().ToString(),
                templateId = "armor_simple_chest",
                rarity = "Обычная",
                enhanceLevel = 0
            });

            return account;
        }

        private static ItemInstance GenerateLootForRoom(RoomType roomType)
        {
            return new ItemInstance
            {
                id = Guid.NewGuid().ToString(),
                templateId = $"loot_{roomType.ToString().ToLowerInvariant()}",
                rarity = roomType == RoomType.Boss ? "Epic" : "Magic",
                enhanceLevel = 0
            };
        }

        private static AccountState LoadOrCreateAccount(ProfileData profile)
        {
            if (!string.IsNullOrWhiteSpace(profile.accountJson))
            {
                try
                {
                    var acc = JsonUtility.FromJson<AccountState>(profile.accountJson) ?? BuildInitialAccount();
                    CharacterPaperDoll.EnsureList(acc);
                    CharacterStatsService.RecalculateForCamp(acc);
                    return acc;
                }
                catch
                {
                    return BuildInitialAccount();
                }
            }

            return BuildInitialAccount();
        }

        private static void SyncLegacyResourcesFromProfile(ProfileData profile, AccountState account)
        {
            if (profile == null || account == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(profile.accountJson))
            {
                account.forgeDust = profile.forgeDust;
                account.emberCore = profile.emberCore;
                account.sigilToken = profile.sigilToken;
                account.insuranceSeal = profile.insuranceSeal;
                account.gold = profile.gold;
            }
        }

        private void PersistAccount()
        {
            if (_profile == null)
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

        private void PersistExpedition()
        {
            if (_profile == null)
            {
                return;
            }

            _profile.expeditionSchemaVersion = 1;
            _profile.expeditionMinRoomsPerAct = minRoomsPerAct;
            _profile.expeditionMaxRoomsPerAct = maxRoomsPerAct;
            _profile.expeditionStartingHpPercent = startingHpPercent;
            _profile.expeditionAutoInsureFirstItem = autoInsureFirstItem;

            if (_runController.CurrentRun == null || _rooms == null)
            {
                _profile.hasActiveExpedition = false;
                _profilesService.SaveProfile(_profile);
                return;
            }

            var run = _runController.CurrentRun;
            _profile.hasActiveExpedition = _state == DemoState.InRun;
            _profile.expeditionDemoState = (int)_state;
            _profile.expeditionRunSeed = run.seed;
            _profile.expeditionRoomIndex = run.roomIndex;
            _profile.expeditionHpState = run.hpState;
            _profile.expeditionRunJson = JsonUtility.ToJson(run);

            _profile.expeditionRoomTypesCount = _rooms.Count;
            _profile.expeditionRoomTypes = new int[_rooms.Count];
            for (var i = 0; i < _rooms.Count; i++)
            {
                _profile.expeditionRoomTypes[i] = (int)_rooms[i];
            }

            _profilesService.SaveProfile(_profile);
        }

        private void ClearExpedition(ProfileData profile, bool save)
        {
            if (profile == null)
            {
                return;
            }

            _runController.ClearRun();
            _rooms = null;

            profile.hasActiveExpedition = false;
            profile.expeditionDemoState = 0;
            profile.expeditionRunSeed = 0;
            profile.expeditionRoomIndex = 0;
            profile.expeditionHpState = 1f;
            profile.expeditionRoomTypesCount = 0;
            profile.expeditionRoomTypes = Array.Empty<int>();
            profile.expeditionRunJson = string.Empty;

            if (save)
            {
                _profilesService.SaveProfile(profile);
            }
        }

        private void RestoreExpeditionFromProfile(ProfileData profile)
        {
            if (!string.IsNullOrWhiteSpace(profile.expeditionRunJson))
            {
                var restored = JsonUtility.FromJson<RunState>(profile.expeditionRunJson);
                _runController.LoadRun(restored);
            }

            _runGenerator = new RunGenerator(profile.expeditionRunSeed);
            _rooms = new List<RoomType>();

            if (profile.expeditionRoomTypes != null &&
                profile.expeditionRoomTypes.Length == profile.expeditionRoomTypesCount &&
                profile.expeditionRoomTypesCount > 0)
            {
                foreach (var rt in profile.expeditionRoomTypes)
                {
                    _rooms.Add((RoomType)rt);
                }
            }
            else
            {
                _rooms = _runGenerator.GenerateAct(UnityEngine.Random.Range(minRoomsPerAct, maxRoomsPerAct + 1));
            }

            if (_runController.CurrentRun == null)
            {
                var runState = new RunState
                {
                    hpState = Mathf.Clamp01(profile.expeditionHpState)
                };

                if (InventoryEquipmentRules.AccountCanStartRun(_account) && profile.expeditionRoomIndex <= 0)
                {
                    InventoryEquipmentRules.PopulateStartingLoadout(
                        runState,
                        _account,
                        autoInsureFirstItem,
                        ref _account.insuranceSeal);
                }

                _runController.StartRun(profile.expeditionRunSeed, runState);
                if (_runController.CurrentRun != null)
                {
                    _runController.CurrentRun.roomIndex = Mathf.Clamp(profile.expeditionRoomIndex, 0, int.MaxValue);
                }
            }

            _state = (DemoState)profile.expeditionDemoState;
            if (_state != DemoState.InRun && _state != DemoState.Resolved)
            {
                _state = DemoState.InRun;
            }

            CharacterStatsService.RecalculateForRun(_account, _runController.CurrentRun);
        }
    }
}
