# Changelog - Shattered Forge

## 2026-04-28

### Implemented
- Added playable prototype loop (`hub -> run -> room progression -> extract/death`).
- Added base combat sandbox with spawned arena, player capsule, enemy waves, and projectile combat.
- Added run-loss behavior with insurance exception.
- Added enhancement scaffolding (`EnhancementConfig`, `EnhancementService`) and core runtime models.
- Added telemetry scaffolding (`TelemetryEventIds`, `TelemetryService`).
- Added weighted room generator (`RunGenerator`).

### Menu / profile UX
- Added main menu controller with:
  - `Continue Game`
  - `New Game`
  - `Settings`
  - `Quit Game`
- Added active profile corner entry/button.
- Active profile button now opens profile actions:
  - switch current profile
  - create new profile
- If an active profile exists, `New Game` now starts immediately on that profile (no profile-name prompt).
- `Continue Game` uses current active profile and starts gameplay flow.

### Scene/runtime behavior
- `TryEnterGameplay(...)` fallback behavior updated:
  - if `Gameplay Scene Name` is empty, reload current scene instead of only showing status text.
- Added Unity scene object wiring through MCP for menu bootstrap:
  - created `MainMenuBootstrap`
  - attached `MainMenuController`
  - saved `Assets/Scenes/SampleScene.unity`

### Camera iteration summary
- Implemented and tested multiple camera modes:
  - static original angle
  - top-down orthographic
  - tilted diablo-like framing
  - follow/behind-player experiments
- Final requested state restored to original static camera settings.

### Input system fixes
- Replaced legacy `UnityEngine.Input` usage in prototype flow with Input System-compatible handling (`Keyboard` / `Gamepad` path).

### Project hygiene
- Added root `.gitignore` tuned for Unity, IDE artifacts, Cursor local data, OS junk, and secret-like files.

## Notes
- This changelog records implemented behavior in the current prototype branch/state.
- When changing menu behavior, also update:
  - `Docs/ShatteredForge/Menu_logic_contract.md`
  - `.cursor/rules/shattered-forge-menu-contract.mdc`
