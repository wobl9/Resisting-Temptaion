using ShatteredForge.Core;
using ShatteredForge.Progression;

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
            CurrentRun.hpState = 1f;
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
