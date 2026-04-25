# Shattered Forge - Game Design Document (v1)

## Product Vision
`Shattered Forge` is a session-based action roguelike with extraction pressure.  
The player prepares a risky build in the hub, enters a run (15-60 min), and chooses between pushing deeper or extracting early.  
The core tension is item investment versus permaloss: if the player dies, all equipped run items and carried loot are lost.

## Pillars
1. **High stakes progression**: upgrades matter and can be lost.
2. **Build expression**: weapon sharpening, attribute sockets, and skill tuning create many viable builds.
3. **Readable combat**: high mobility, clear enemy telegraphs, fast room resolution.
4. **Short-to-medium runs**: each run should feel meaningful in one sitting.

## Target Experience
- Pre-run: "I commit to this build and risk strong upgrades."
- Mid-run: "Each room choice affects survival and extraction value."
- Post-run: "I either banked gains or learned from a costly death."

## Genre, Platform, Audience
- Genre: action roguelike + extraction-lite.
- Platform MVP: PC (keyboard/mouse and controller).
- Audience: players of Slay the Spire, Hades, Archero-style runs, and risk-heavy progression systems.

## Core Gameplay Loop
1. **Hub Preparation**
   - Choose hero archetype.
   - Equip loadout.
   - Sharpen equipment and tune skills.
   - Choose run risk tier.
2. **Run Execution**
   - Clear a branching room map.
   - Collect temporary combat modifiers and persistent loot.
   - Decide to continue, take elite routes, or extract.
3. **Resolution**
   - Extract: keep carried loot and progress meta.
   - Death: lose active loadout and carried loot.
4. **Meta Growth**
   - Keep account-level unlocks and currencies only.

## Session Structure
- 3 acts per full run.
- 8-14 rooms per act.
- Typical room duration: 1.5-4 minutes.
- Full run target: 20-50 minutes.
- Short mode (single act): 10-20 minutes.

### Room Types (target distribution)
- Combat: 60%
- Event/Shrine: 10%
- Field Forge: 8%
- Shop: 8%
- Elite: 8%
- Rest: 4%
- Boss/Miniboss: 2%

## Combat Design
- Isometric arena rooms.
- Auto attack from equipped weapon.
- One active skill with cooldown.
- One dash with i-frames.
- Two passive module slots.
- Threat scales by act and room depth.

## Progression Systems

### 1) Weapon and Armor Sharpening
- Levels: `+0` to `+15`.
- Success rates:
  - `+1..+3`: 100%
  - `+4..+6`: 85 / 70 / 55%
  - `+7..+9`: 40 / 30 / 22%
  - `+10..+12`: 16 / 12 / 9%
  - `+13..+15`: 6 / 4 / 2%
- Failure outcomes:
  - soft fail: level `-1`
  - hard fail (high tiers, unprotected): item destroyed
- Mitigation:
  - safe zone to `+3`
  - stabilizer consumable
  - pity modifier after failure streak
  - anti-destruction ward item
  - floor-anchor item (minimum fallback level)

### 2) Attribute Socketing
- Socket counts:
  - weapon: 2
  - chest: 2
  - helm/gloves/boots: 1 each
- Attribute classes:
  - elemental (fire/ice/lightning/poison/shadow/light)
  - utility (lifesteal, cooldown reduction, movement speed, thorns)
  - conditional (vs elite, vs boss, vs packs)
- Rules:
  - incompatible attribute pairs blocked
  - repeated effect uses diminishing returns
  - set-tag synergies at 2/4/6 thresholds

### 3) Skill Tuning
- Skill has:
  - account mastery level (persistent)
  - risk rank (run-bound investment item)
- Skills can receive runes that alter behavior.
- Rare runes in risk slots are lost on death.

## Risk and Loss Rules
- Run loadout includes:
  - 1 weapon
  - 4-5 armor pieces
  - 1 active skill item
  - 2 passive modules
  - consumables
- On death: all equipped run loadout + carried run loot removed.
- On extraction: all carried loot transferred to hub.

### Frustration Controls
- Starter insured slot (one protected item).
- Emergency extraction (1/run, heavy reward penalty).
- Training mode with low rewards and reduced loss severity.

## Economy
- `ForgeDust`: baseline enhancement currency.
- `EmberCore`: high-tier enhancement and reroll currency.
- `SigilToken`: account progression.
- `InsuranceSeal`: temporary run protection.

Design target: a typical player can rebuild a combat-ready kit within 3-4 runs after one major loss.

## Content Scope (MVP)
- 3 hero archetypes:
  - Vanguard (tank/counter)
  - Ranger (mobility/crit)
  - Arcanist (elemental control)
- 3 biomes:
  - Ruined Keep
  - Ember Mines
  - Hollow Grove
- Per biome:
  - 6 normal enemies
  - 2 elite enemies
  - 1 boss

## UX Requirements
- Explicit warning before run start about full-loss risk.
- Enhancement UI shows exact chance and fail outcomes.
- Reroll comparison panel (before/after).
- Death recap with top damage sources and failure timeline.

## Technical Direction
- Engine: Unity LTS.
- Data:
  - ScriptableObjects for static content definitions.
  - Save file for account progression.
  - Isolated runtime run state snapshot.
- Suggested systems:
  - `CombatSystem`
  - `RunGenerator`
  - `ItemizationSystem`
  - `EnhancementSystem`
  - `RiskLossSystem`
  - `MetaProgressionSystem`
  - `EconomyService`
  - `SaveLoadService`
  - `TelemetryService`

## Telemetry (must-have in first playable)
- `run_started`, `room_cleared`, `enhance_attempted`, `enhance_failed`, `player_died`, `run_extracted`, `loot_lost`.
- Dashboard metrics:
  - average run length
  - death rate per act
  - loadout loss ratio
  - enhancement distribution
  - extraction success ratio

## Acceptance Criteria (MVP)
- End-to-end loop works (hub -> run -> death or extraction).
- Loss logic is consistent and cannot duplicate items.
- Enhancement + sockets + skill tuning are understandable and usable.
- Median run duration remains in 15-60 minute target.
- Economy allows recovery after setbacks.
