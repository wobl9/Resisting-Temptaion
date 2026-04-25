# Shattered Forge - MVP Scope Lock and Tech Stack

## Scope Lock (MVP)
Included:
- PvE solo only.
- 3 heroes: Vanguard, Ranger, Arcanist.
- 3 biomes: Ruined Keep, Ember Mines, Hollow Grove.
- Run modes: short (1 act) and full (3 acts).
- Core systems:
  - loadout + inventory
  - enhancement (+0..+15 with fail states and protection)
  - attributes + sockets
  - skill mastery and risk rank
  - extraction vs death-loss resolution
  - account meta progression

Excluded (post-MVP):
- co-op
- PvP
- guilds/clans
- player economy/auction
- seasonal ladder backend

## Success Criteria for MVP
- Stable full loop from hub to run outcome.
- Average run length in target 15-60 minute band.
- Item loss logic reproducible and exploit-resistant.
- New players can recover after one failed high-risk run in 3-4 average runs.

## Technical Stack
- Engine: Unity LTS (URP).
- Language: C#.
- Input: Unity Input System.
- Data authoring:
  - ScriptableObjects for static content
  - CSV import pipeline for tuning tables
- Persistence:
  - account save: JSON with versioning
  - run snapshot: transactional local file
- Telemetry:
  - event bus + local buffer + export to CSV/JSON

## Runtime Architecture
- `CombatSystem`: player actions, enemy AI, damage pipeline.
- `RunGenerator`: room graph generation by seeds and weighted room pools.
- `ItemizationSystem`: item instances, rarity, affixes, sockets.
- `EnhancementSystem`: sharpening rules, RNG, pity, protection logic.
- `RiskLossSystem`: death-loss and extraction transfer.
- `MetaProgressionSystem`: persistent unlocks and mastery.
- `SaveLoadService`: versioned serialization and integrity checks.
- `EconomyService`: reward and sink calculators.
- `TelemetryService`: event schema and metric batching.

## Project Structure (recommended)
- `Assets/Scripts/Core`
- `Assets/Scripts/Combat`
- `Assets/Scripts/Run`
- `Assets/Scripts/Items`
- `Assets/Scripts/Enhancement`
- `Assets/Scripts/Progression`
- `Assets/Scripts/Services`
- `Assets/Data/Definitions`
- `Assets/Data/Balance`

## Definition of Done (for MVP epics)
- Feature implemented.
- Automated tests for critical rules.
- Telemetry events emitted.
- Editor validation for malformed data.
- QA checklist executed without blocker bugs.
