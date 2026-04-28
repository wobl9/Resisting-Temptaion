# Progress - Shattered Forge

Legend: `Done` | `In Progress` | `Planned` | `Blocked`

## Current Milestone
- [ ] Main menu flow wired to playable loop
- [ ] Profile slot flow fully validated
- [ ] Core loop polish pass

## Feature Status
- Playable loop (demo end-to-end: hub -> run -> extract/death): `Done`
- Playable loop (GDD-aligned: multi-act, branching, full loadout, profile-backed): `In Progress`
- Combat room bootstrap and room progression: `Done`
- Run loss rules with insurance exception: `Done`
- Main menu controller integration: `In Progress`
- Profile persistence (`IProfileStorage`, local JSON + optional remote stub): `In Progress` (see `Docs/ShatteredForge/Profile_persistence.md`)
- Scene transitions via loading scene (`SceneNavigation`, Boot vs Loading): `Done` (see `Docs/ShatteredForge/Scene_loading_transitions.md`)
- Mission levels system (camp portal -> level scene, tier/pool driven): `In Progress` (see `Docs/ShatteredForge/Levels_system.md`)
- Economy balance pass: `Planned`
- Sharpening full implementation (+0..+15 rules): `Planned`
- Socketing and skill tuning production pass: `Planned`
- Telemetry events and dashboard wiring: `Planned`

## Last Updates
- Added consolidated change log: `Docs/ShatteredForge/CHANGELOG.md`.
- Main menu UX updated: active profile is shown in the corner and opens profile actions (switch/create).
- Main menu behavior updated: with active profile, show `Continue Game` + `New Game`; `New Game` starts immediately on active profile.
- Gameplay start fallback updated: if gameplay scene name is empty, current scene reloads instead of only showing status text.
- Menu bootstrap object was created and wired in `SampleScene` through Unity MCP.
- Camera behavior was iterated and finally returned to original static mode per latest request.
- Added `MainMenuController` script scaffold.
- Documented profile persistence architecture in `Docs/ShatteredForge/Profile_persistence.md` (`IProfileStorage`, `LocalJsonProfileStorage`, factory defaults = local only; remote for later).
- Added/updated sample scene for menu and loop setup.
- Confirmed playable loop status split: demo loop is `Done`, GDD-aligned loop remains `In Progress`.
- Implemented profile deletion flow (with confirmation) in menu and storage service; pending manual play-mode verification.
- Updated main menu + profile persistence to match `Menu_logic_contract.md` (RU UI, hidden Continue, expedition save/resume handoff).
- Documented loading-scene flow and `SceneNavigation` usage in `Docs/ShatteredForge/Scene_loading_transitions.md` (linked from this file).
- Added mission level foundations: `LevelDefinition`/`LevelCatalog`/`EnemyPoolDefinition`/`LevelTierDefinition`, `PendingLevelRequest`, and `LevelSessionController`.
- Integrated camp dungeon marker with `CampLevelPortalView` (card select + quick-play by tier) and added pause-menu interface reuse (`IPauseMenuView`).
- Added editor bootstrap tools for `LevelScene` creation, demo level content assets, and dual pause-menu prefab bake (`CampPauseMenuUi` + `PauseMenuUi`).

## Next 3 Priorities
1. Reconcile menu code with `Menu_logic_contract.md` expedition semantics (`Continue` visibility and true resume state).
2. Wire menu profile actions to `IProfileStorage` implementation consistently (local/remote factory path).
3. Continue hub/camp integration and mission-level loop validation end-to-end.

## Start Menu Definition of Done
- [ ] Can select, create, and delete a profile.
- [ ] Has `Create Game` and `Continue Game` buttons that both open the gameplay scene.
- [ ] Has a working `Settings` button.
- [ ] Has a working `Quit Game` button.

## Blockers
- None recorded.

## Verification Notes
- Current truth snapshot (single source for now): `Docs/ShatteredForge/CURRENT_TRUTH.md`
- Playable demo setup reference: `Docs/ShatteredForge/Playable_demo_setup.md`
- Scene / loading screen transitions (API, build list, exceptions): `Docs/ShatteredForge/Scene_loading_transitions.md`
- Test result summary (manual): `TBD`

## Update Rule
- After each meaningful coding session:
  - update `Feature Status`
  - append 1-3 bullets in `Last Updates`
  - refresh `Next 3 Priorities`
  - check/uncheck `Start Menu Definition of Done`
