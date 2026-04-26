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
- Economy balance pass: `Planned`
- Sharpening full implementation (+0..+15 rules): `Planned`
- Socketing and skill tuning production pass: `Planned`
- Telemetry events and dashboard wiring: `Planned`

## Last Updates
- Added `MainMenuController` script scaffold.
- Documented profile persistence architecture in `Docs/ShatteredForge/Profile_persistence.md` (`IProfileStorage`, `LocalJsonProfileStorage`, factory defaults = local only; remote for later).
- Added/updated sample scene for menu and loop setup.
- Confirmed playable loop status split: demo loop is `Done`, GDD-aligned loop remains `In Progress`.
- Implemented profile deletion flow (with confirmation) in menu and storage service; pending manual play-mode verification.
- Updated main menu + profile persistence to match `Menu_logic_contract.md` (RU UI, hidden Continue, expedition save/resume handoff).

## Next 3 Priorities
1. Build a fully working start menu flow.
2. Validate save/load behavior for profile slots.
3. Add telemetry for run start, extraction, death, and loot loss.

## Start Menu Definition of Done
- [ ] Can select, create, and delete a profile.
- [ ] Has `Create Game` and `Continue Game` buttons that both open the gameplay scene.
- [ ] Has a working `Settings` button.
- [ ] Has a working `Quit Game` button.

## Blockers
- None recorded.

## Verification Notes
- Playable demo setup reference: `Docs/ShatteredForge/Playable_demo_setup.md`
- Test result summary (manual): `TBD`

## Update Rule
- After each meaningful coding session:
  - update `Feature Status`
  - append 1-3 bullets in `Last Updates`
  - refresh `Next 3 Priorities`
  - check/uncheck `Start Menu Definition of Done`
