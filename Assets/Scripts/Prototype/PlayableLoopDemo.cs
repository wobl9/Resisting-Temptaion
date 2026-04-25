using System;
using System.Collections.Generic;
using ShatteredForge.Combat;
using ShatteredForge.Core;
using ShatteredForge.Input;
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
            _account = BuildInitialAccount();
            _runController = new RunSessionController(new RiskLossService());
            combatBootstrap = GetComponent<CombatRoomBootstrap>();
            if (combatBootstrap == null)
            {
                combatBootstrap = gameObject.AddComponent<CombatRoomBootstrap>();
            }
        }

        private void Update()
        {
            if (DemoInput.KeyDown(Key.H) && _state == DemoState.Resolved)
            {
                _state = DemoState.Hub;
                _lastOutcome = "Returned to hub. Press R to start another run.";
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
                ? "WASD move | Auto-fire at nearest enemy | R Start | E Extract | K Die | Space non-combat rooms | H hub after run"
                : "Controls: R - Start Run, C - Clear Room, E - Extract, K - Die | H hub after run";
            GUI.Label(new Rect(x, y, 1200, line), controls);
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
    }
}
