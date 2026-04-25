# Shattered Forge - Prototype Plan and Playtest Protocol

## Prototype Goal (4-6 weeks)
Validate the core loop:
- prepare risky loadout in hub
- enter run and clear rooms
- choose extraction or continue
- on death lose run loadout and carried loot
- on extraction bank loot for meta progress

## Prototype Scope
- 1 hero archetype (Ranger baseline).
- 1 biome.
- 1 act.
- 20 items.
- enhancement to `+10`.
- 1 active skill + 2 passive modules.
- no narrative systems and no liveops.

## Implemented Starter Code in This Project
- Data models:
  - `Assets/Scripts/Core/GameModels.cs`
- Enhancement logic:
  - `Assets/Scripts/Enhancement/EnhancementConfig.cs`
  - `Assets/Scripts/Enhancement/EnhancementService.cs`
- Risk-loss flow:
  - `Assets/Scripts/Progression/RiskLossService.cs`
  - `Assets/Scripts/Run/RunSessionController.cs`
- Prototype harness:
  - `Assets/Scripts/Prototype/PrototypeBootstrap.cs`

## Playtest Design
### Test Cohorts
- 6 new players (no prior project context).
- 6 experienced roguelike players.

### Session Structure
1. 5 min onboarding.
2. 3 prototype runs per tester.
3. 10 min post-run interview.

### Metrics to Capture
- run duration
- room clear time
- death rate by room depth
- extraction rate
- enhancements attempted/success/fail
- rage-quit indicator after death loss
- self-reported tension/fun score (1-10)

### Success Targets
- median run in 15-30 min (prototype content size).
- extraction rate 35-55%.
- no more than 20% testers quit after first full-loss death.
- average fun score >= 7.
- average clarity score of enhancement UI >= 7.

## Prototype Exit Criteria
- Core loss and extraction logic verified in gameplay.
- At least one build archetype feels viable.
- Economy supports rebuild within 2-3 prototype runs.
- Top 5 friction points documented and prioritized for vertical slice.
