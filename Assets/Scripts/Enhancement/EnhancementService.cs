using System.Linq;
using ShatteredForge.Core;
using UnityEngine;

namespace ShatteredForge.Enhancement
{
    public enum EnhancementFailType
    {
        None,
        Downgrade,
        Destroyed
    }

    public struct EnhancementResult
    {
        public bool success;
        public EnhancementFailType failType;
        public int previousLevel;
        public int currentLevel;
    }

    public class EnhancementService
    {
        private readonly EnhancementConfig _config;

        public EnhancementService(EnhancementConfig config)
        {
            _config = config;
        }

        public EnhancementResult TryEnhance(ItemInstance item, ref AccountState account, bool useStabilizer, bool useAntiBreakWard)
        {
            var previousLevel = item.enhanceLevel;
            var tier = _config.chances.FirstOrDefault(t => t.fromLevel == previousLevel);
            if (tier.toLevel == 0 && previousLevel != 0)
            {
                return new EnhancementResult
                {
                    success = false,
                    failType = EnhancementFailType.None,
                    previousLevel = previousLevel,
                    currentLevel = item.enhanceLevel
                };
            }

            var chance = tier.successChance + (account.forgePityFailures * _config.pityPerFailure);
            if (useStabilizer)
            {
                chance += _config.stabilizerBonus;
            }

            chance = Mathf.Clamp01(chance);
            if (Random.value <= chance)
            {
                item.enhanceLevel = tier.toLevel;
                account.forgePityFailures = 0;
                return new EnhancementResult
                {
                    success = true,
                    failType = EnhancementFailType.None,
                    previousLevel = previousLevel,
                    currentLevel = item.enhanceLevel
                };
            }

            account.forgePityFailures++;
            if (tier.allowsDestruction && !useAntiBreakWard)
            {
                item.enhanceLevel = 0;
                return new EnhancementResult
                {
                    success = false,
                    failType = EnhancementFailType.Destroyed,
                    previousLevel = previousLevel,
                    currentLevel = item.enhanceLevel
                };
            }

            item.enhanceLevel = Mathf.Max(0, item.enhanceLevel - 1);
            return new EnhancementResult
            {
                success = false,
                failType = EnhancementFailType.Downgrade,
                previousLevel = previousLevel,
                currentLevel = item.enhanceLevel
            };
        }
    }
}
