using System;
using System.Collections.Generic;
using ShatteredForge.Combat;
using ShatteredForge.Core;
using ShatteredForge.Input;
using ShatteredForge.Menu;
using ShatteredForge.Progression;
using ShatteredForge.Run;
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

        public bool IsInRun => _state == DemoState.InRun && _runController.CurrentRun != null;
        public RunState CurrentRun => _runController?.CurrentRun;

        private void Awake()
        {
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
                _lastOutcome = "No profile loaded (demo fallback).";
                MenuSessionWriter.ConsumePendingDungeonEntry();
                BootstrapFreshExpedition();
                return;
            }

            _account = LoadOrCreateAccount(_profile);
            SyncLegacyResourcesFromProfile(_profile, _account);

            var savedMinRooms = _profile.expeditionMinRoomsPerAct;
            var savedMaxRooms = _profile.expeditionMaxRoomsPerAct;
            var savedStartingHp = _profile.expeditionStartingHpPercent;

            minRoomsPerAct = savedMinRooms > 0 ? savedMinRooms : minRoomsPerAct;
            maxRoomsPerAct = savedMaxRooms > 0 ? Mathf.Max(minRoomsPerAct, savedMaxRooms) : maxRoomsPerAct;
            startingHpPercent = savedStartingHp > 0 ? Mathf.Clamp(savedStartingHp, 1, 100) : startingHpPercent;
            autoInsureFirstItem = _profile.expeditionAutoInsureFirstItem;

            if (resumeExpedition && _profile.hasActiveExpedition)
            {
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
            var enterDungeonFromHub = MenuSessionWriter.ConsumePendingDungeonEntry();
            if (enterDungeonFromHub)
            {
                BootstrapFreshExpedition();
                _lastOutcome = "Expedition started from camp.";
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
            if (IsInRun)
            {
                combatBootstrap?.OnRunStartedOrRoomAdvanced();
            }
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
            if (_state == DemoState.InRun || _account.stash.Count == 0)
            {
                return;
            }

            var runState = new RunState
            {
                hpState = Mathf.Clamp01(startingHpPercent / 100f)
            };

            var equippedWeapon = _account.stash[0];
            _account.stash.RemoveAt(0);
            equippedWeapon.isInsuredForRun = autoInsureFirstItem && _account.insuranceSeal > 0;
            if (equippedWeapon.isInsuredForRun)
            {
                _account.insuranceSeal--;
            }

            runState.equippedLoadout.Add(equippedWeapon);
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
                ? "WASD move | Auto-fire at nearest enemy | R Start (hub) | E Extract | K Die | H hub after run"
                : "Controls: R - Start Run, C - Clear Room, E - Extract, K - Die | H hub after run";
            GUI.Label(new Rect(x, y, 1200, line), controls);
        }

        private void BootstrapFreshExpedition()
        {
            if (_state == DemoState.InRun || _account.stash.Count == 0)
            {
                return;
            }

            var runState = new RunState
            {
                hpState = Mathf.Clamp01(startingHpPercent / 100f)
            };

            var equippedWeapon = _account.stash[0];
            _account.stash.RemoveAt(0);
            equippedWeapon.isInsuredForRun = autoInsureFirstItem && _account.insuranceSeal > 0;
            if (equippedWeapon.isInsuredForRun)
            {
                _account.insuranceSeal--;
            }

            runState.equippedLoadout.Add(equippedWeapon);
            _runController.StartRun(UnityEngine.Random.Range(1, int.MaxValue), runState);

            _runGenerator = new RunGenerator(_runController.CurrentRun.seed);
            _rooms = _runGenerator.GenerateAct(UnityEngine.Random.Range(minRoomsPerAct, maxRoomsPerAct + 1));
            _state = DemoState.InRun;
            _lastOutcome = "Run started.";
        }

        private static AccountState BuildInitialAccount()
        {
            var account = new AccountState
            {
                forgeDust = 2500,
                emberCore = 5,
                sigilToken = 20,
                insuranceSeal = 1
            };

            account.stash.Add(new ItemInstance
            {
                id = Guid.NewGuid().ToString(),
                templateId = "weapon_sword_t1",
                rarity = "Rare",
                enhanceLevel = 5
            });
            account.stash.Add(new ItemInstance
            {
                id = Guid.NewGuid().ToString(),
                templateId = "armor_chest_t1",
                rarity = "Magic",
                enhanceLevel = 3
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
                    return JsonUtility.FromJson<AccountState>(profile.accountJson) ?? BuildInitialAccount();
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

                if (_account.stash.Count > 0 && profile.expeditionRoomIndex <= 0)
                {
                    var equippedWeapon = _account.stash[0];
                    _account.stash.RemoveAt(0);
                    equippedWeapon.isInsuredForRun = autoInsureFirstItem && _account.insuranceSeal > 0;
                    if (equippedWeapon.isInsuredForRun)
                    {
                        _account.insuranceSeal--;
                    }

                    runState.equippedLoadout.Add(equippedWeapon);
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
        }
    }
}
