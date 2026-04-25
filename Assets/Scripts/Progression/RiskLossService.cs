using System.Collections.Generic;
using ShatteredForge.Core;

namespace ShatteredForge.Progression
{
    public class RiskLossService
    {
        public void ApplyDeathLoss(RunState runState, AccountState account)
        {
            if (runState == null || account == null)
            {
                return;
            }

            var survivors = new List<ItemInstance>();
            foreach (var item in runState.equippedLoadout)
            {
                if (item.isInsuredForRun)
                {
                    survivors.Add(item);
                }
            }

            account.stash.AddRange(survivors);
            runState.equippedLoadout.Clear();
            runState.carryLoot.Clear();
            runState.extractionStatus = false;
        }

        public void ApplyExtraction(RunState runState, AccountState account)
        {
            if (runState == null || account == null)
            {
                return;
            }

            account.stash.AddRange(runState.equippedLoadout);
            account.stash.AddRange(runState.carryLoot);
            runState.equippedLoadout.Clear();
            runState.carryLoot.Clear();
            runState.extractionStatus = true;
        }
    }
}
