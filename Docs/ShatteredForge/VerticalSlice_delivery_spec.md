# Shattered Forge - Vertical Slice Delivery Spec

## Goal
Deliver a fully playable vertical slice with representative content and all core systems active:
- 3 heroes
- 3 biomes
- complete enhancement + socket + skill tuning loop
- extraction and death-loss flow
- telemetry and balance instrumentation

## Delivery Scope
### Content
- Heroes: Vanguard, Ranger, Arcanist.
- Biomes:
  - Ruined Keep
  - Ember Mines
  - Hollow Grove
- Enemies per biome: 6 normal + 2 elites + 1 boss.
- Item pool: 120+ items across rarity tiers.
- Skill runes: minimum 24 behavioral modifiers.

### Systems
- Room graph generation and weighted node routing.
- Combat loop with mobility, active skill, passives.
- Enhancement to `+15` with pity and protection mechanics.
- Socketing and attribute rerolls.
- Skill mastery (persistent) + risk rank (run-bound).
- Loss logic and extraction transfer.
- Meta progression with unlock tree and currencies.

### Non-Functional
- 60 FPS target on baseline test machine.
- Run startup under 10 seconds.
- No critical blocker bugs in end-to-end loop.

## Existing Project Foundation Implemented
- Models:
  - `Assets/Scripts/Core/GameModels.cs`
- Enhancement:
  - `Assets/Scripts/Enhancement/EnhancementConfig.cs`
  - `Assets/Scripts/Enhancement/EnhancementService.cs`
- Risk/loss:
  - `Assets/Scripts/Progression/RiskLossService.cs`
  - `Assets/Scripts/Run/RunSessionController.cs`
- Run generation starter:
  - `Assets/Scripts/Run/RunGenerator.cs`
- Telemetry:
  - `Assets/Scripts/Telemetry/TelemetryEventIds.cs`
  - `Assets/Scripts/Telemetry/TelemetryService.cs`
- Prototype harness:
  - `Assets/Scripts/Prototype/PrototypeBootstrap.cs`

## Work Breakdown (8-12 weeks)
1. **Combat Content Pass**
   - enemy kits, attack telegraphs, elite modifiers, boss phase scripts.
2. **Itemization Pass**
   - item definitions, affix tables, socket compatibility matrix.
3. **Progression Pass**
   - unlock tree, mastery progression, economy balancing.
4. **UX Pass**
   - enhancement warnings, death recap, extraction decision prompts.
5. **Stability and Metrics**
   - telemetry dashboard and balancing loops using playtest data.

## Acceptance Criteria
- At least 20 complete runs from internal QA without progression blockers.
- Enhancement and socket systems clearly understood by test players (>= 75% task completion in UX tests).
- Extraction success ratio stabilizes to 30-50% across mixed-skill playtesters.
- No item duplication exploit found in death/extraction state transitions.

## Handoff Package
- GDD:
  - `Docs/ShatteredForge/GDD_v1.md`
- Balance:
  - `Docs/ShatteredForge/balance_enhancement.csv`
  - `Docs/ShatteredForge/balance_drops.csv`
  - `Docs/ShatteredForge/balance_economy.csv`
- Scope and stack:
  - `Docs/ShatteredForge/MVP_scope_and_tech_stack.md`
- Prototype plan:
  - `Docs/ShatteredForge/Prototype_plan_and_playtest.md`
