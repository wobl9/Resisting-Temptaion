using ShatteredForge.Core;
using ShatteredForge.Progression;
using UnityEngine;

namespace ShatteredForge.Run
{
    public class RunSessionController
    {
        private readonly RiskLossService _riskLossService;
        public RunState CurrentRun { get; private set; }

        public RunSessionController(RiskLossService riskLossService)
        {
            _riskLossService = riskLossService;
        }

        public void StartRun(int seed, RunState loadoutSnapshot)
        {
            CurrentRun = loadoutSnapshot;
            CurrentRun.seed = seed;
            CurrentRun.actIndex = 0;
            CurrentRun.roomIndex = 0;
            CurrentRun.hpState = Mathf.Clamp01(loadoutSnapshot.hpState);
            CurrentRun.extractionStatus = false;
        }

        public void AdvanceRoom()
        {
            if (CurrentRun == null)
            {
                return;
            }

            CurrentRun.roomIndex++;
        }

        public void ClearRun()
        {
            CurrentRun = null;
        }

        public void LoadRun(RunState runState)
        {
            if (runState == null)
            {
                CurrentRun = null;
                return;
            }

            CurrentRun = runState;
            CurrentRun.hpState = Mathf.Clamp01(CurrentRun.hpState);
        }

        public void ResolveDeath(AccountState account)
        {
            if (CurrentRun == null)
            {
                return;
            }

            _riskLossService.ApplyDeathLoss(CurrentRun, account);
            CurrentRun = null;
        }

        public void ResolveExtraction(AccountState account)
        {
            if (CurrentRun == null)
            {
                return;
            }

            _riskLossService.ApplyExtraction(CurrentRun, account);
            CurrentRun = null;
        }
    }
}
