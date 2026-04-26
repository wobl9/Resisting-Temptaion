# AI Context - Shattered Forge

## Game Identity
- Title: `Shattered Forge`
- Genre: session-based action roguelike + extraction-lite
- Engine: Unity LTS
- Target platform (MVP): PC

## Core Loop (Always Keep Intact)
1. Hub preparation (choose build and risk)
2. Run execution (room progression + loot decisions)
3. Resolution (extract or die)
4. Meta growth (account-level progression only)

## Core Tension
- Player invests items and power into a run.
- On death: equipped run loadout and carried run loot are lost.
- On extraction: carried loot is banked in hub stash.

## Combat Pillars
- Fast, readable isometric room combat
- High mobility (dash + i-frames)
- Auto attack + one active skill + passive modules

## Progression Pillars
- Weapon/armor sharpening with risk
- Attribute sockets and synergies
- Skill tuning (persistent mastery + run-bound risk rank)

## Design Constraints (Do Not Change Silently)
- Full-loss pressure on death is a core identity.
- Runs must feel meaningful in one sitting (short-to-medium sessions).
- Build diversity must be preserved (no single dominant path by design).
- Recovery after loss should remain possible (target: 3-4 runs to rebuild).

## Current Focus
- Deliver a stable playable demo loop:
  - hub -> run -> room progression -> extraction/death -> hub

## Canonical Docs
- Vision and systems: `Docs/ShatteredForge/GDD_v1.md`
- Demo boot/setup: `Docs/ShatteredForge/Playable_demo_setup.md`
- Delivery specs: `Docs/ShatteredForge/VerticalSlice_delivery_spec.md`
- Start menu logic contract: `Docs/ShatteredForge/Menu_logic_contract.md`
- Profile save system (`IProfileStorage`, disk paths, prefs bridge, future remote): `Docs/ShatteredForge/Profile_persistence.md`
