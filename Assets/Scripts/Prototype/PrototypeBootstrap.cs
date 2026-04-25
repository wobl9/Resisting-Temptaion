using System;
using ShatteredForge.Core;
using ShatteredForge.Enhancement;
using ShatteredForge.Progression;
using ShatteredForge.Run;
using UnityEngine;

namespace ShatteredForge.Prototype
{
    public class PrototypeBootstrap : MonoBehaviour
    {
        [SerializeField] private EnhancementConfig enhancementConfig;

        private AccountState _account;
        private RunSessionController _runController;
        private EnhancementService _enhancementService;

        private void Awake()
        {
            _account = new AccountState
            {
                forgeDust = 2500,
                emberCore = 5,
                sigilToken = 20,
                insuranceSeal = 1
            };

            _runController = new RunSessionController(new RiskLossService());
            _enhancementService = new EnhancementService(enhancementConfig);
        }

        [ContextMenu("Prototype/SimulateRunDeath")]
        public void SimulateRunDeath()
        {
            var weapon = new ItemInstance
            {
                id = Guid.NewGuid().ToString(),
                templateId = "weapon_sword_t1",
                rarity = "Rare",
                enhanceLevel = 5,
                isInsuredForRun = true
            };

            var runState = new RunState();
            runState.equippedLoadout.Add(weapon);
            runState.carryLoot.Add(new ItemInstance { id = Guid.NewGuid().ToString(), templateId = "loot_core", rarity = "Magic" });
            _runController.StartRun(UnityEngine.Random.Range(1, int.MaxValue), runState);
            _runController.ResolveDeath(_account);
            Debug.Log($"Death resolved. Stash count: {_account.stash.Count} (insured items only expected).");
        }

        [ContextMenu("Prototype/SimulateEnhancement")]
        public void SimulateEnhancement()
        {
            var item = new ItemInstance
            {
                id = Guid.NewGuid().ToString(),
                templateId = "weapon_bow_t2",
                rarity = "Epic",
                enhanceLevel = 9
            };

            var result = _enhancementService.TryEnhance(item, ref _account, useStabilizer: true, useAntiBreakWard: false);
            Debug.Log($"Enhancement result: success={result.success}, failType={result.failType}, level={result.currentLevel}");
        }
    }
}
