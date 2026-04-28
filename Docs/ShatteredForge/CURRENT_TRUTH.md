# Current Truth - Shattered Forge

Last updated: `2026-04-28`

## Scope of this document
This file describes the **actual current behavior** of the project right now.  
It is intentionally short and excludes historical notes.

---

## 1) Runtime status (playable prototype)

- Prototype loop is playable: `hub/menu -> run -> room progression -> extract or death`.
- Core combat sandbox exists:
  - player movement + auto-fire
  - enemy spawning by room type
  - room completion progression
- Run loss behavior is active:
  - death removes run loadout and carried loot
  - insured item exception is supported in prototype logic

Key scripts:
- `Assets/Scripts/Prototype/PlayableLoopDemo.cs`
- `Assets/Scripts/Combat/CombatRoomBootstrap.cs`
- `Assets/Scripts/Run/RunSessionController.cs`
- `Assets/Scripts/Progression/RiskLossService.cs`

---

## 2) Main menu behavior (current)

Main actions:
- `Continue Game`
- `New Game`
- `Settings`
- `Quit Game`

Profile UX:
- Active profile is shown in the top-right corner as a clickable entry.
- Clicking active profile opens a profile menu with:
  - switch profile
  - create new profile

New Game behavior:
- If active profile exists: starts new game on that profile immediately (no profile-name prompt).
- If no profile exists: opens profile creation flow.

Continue behavior:
- Uses active profile id and starts gameplay transition.
- If explicit gameplay scene is configured, it loads that scene.
- If not configured, it reloads the current scene (fallback).

Key script:
- `Assets/Scripts/Menu/MainMenuController.cs`

---

## 3) Profile persistence (current)

- Profile persistence is implemented via profile storage abstractions in `Assets/Scripts/Menu/`.
- Active profile is mirrored into:
  - `PlayerPrefs["sf.active_profile_id"]`
- Local profile files are stored under:
  - `Application.persistentDataPath/ShatteredForge/`

Canonical technical map:
- `Docs/ShatteredForge/Profile_persistence.md`

---

## 4) Camera status (current)

- Current camera mode in combat prototype is **static original angle** (non-follow), restored per latest request.
- Camera experiments (top-down, diablo-like, follow) were tested but are not the current final state.

Key script:
- `Assets/Scripts/Combat/CombatRoomBootstrap.cs`

---

## 5) Input status (current)

- Prototype input path is compatible with Unity Input System package.
- Legacy `UnityEngine.Input` runtime path was removed from prototype interaction flow.

---

## 6) Documentation and rules status

Primary docs to trust now:
- `Docs/ShatteredForge/GDD_v1.md`
- `Docs/ShatteredForge/Menu_logic_contract.md`
- `Docs/ShatteredForge/Profile_persistence.md`
- `Docs/ShatteredForge/Scene_loading_transitions.md`
- `Docs/ShatteredForge/PROGRESS.md`
- `Docs/ShatteredForge/DECISIONS.md`
- `Docs/ShatteredForge/CHANGELOG.md`

Relevant Cursor rules:
- `.cursor/rules/shattered-forge-menu-contract.mdc`
- `.cursor/rules/shattered-forge-profile-persistence.mdc`
- `.cursor/rules/shattered-forge-scene-transitions.mdc`

---

## 7) Known alignment gap to resolve next

- Menu contract and production `Continue Game` semantics should be finalized around true expedition checkpoint resume.
- Current fallback scene-reload behavior is acceptable for prototype, but should be replaced with full scene-flow + expedition state restore once fully wired.
