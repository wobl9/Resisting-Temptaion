# Main Menu Setup

1. Open/create your boot scene (menu scene).
2. Create an empty GameObject named `MainMenuBootstrap`.
3. Add component `MainMenuController`.
4. Optional: set `Gameplay Scene Name` in inspector to auto-load gameplay after profile select/new game.

Main menu features implemented:
- Select profile (progress is bound to selected profile).
- Create new game (creates new profile + marks it active).
- Delete profile (with confirmation).
- Open settings (volume, fullscreen, resolution).
- Quit game.

Profile save location:
- `Application.persistentDataPath/ShatteredForge/`
  - `profiles_index.json`
  - `Profiles/profile_<id>.json`

Runtime keys (menu → gameplay handoff):
- `PlayerPrefs["sf.active_profile_id"]` — active profile id (`MenuSessionPrefs.ActiveProfileIdKey`).
- `PlayerPrefs["sf.resume_expedition"]` — `1` to resume an active expedition, `0` to start fresh (`MenuSessionPrefs.ResumeExpeditionKey`). Cleared by gameplay bootstrap after read.

Localization (Unity Localization Package):
- Package dependency: `com.unity.localization` in `Packages/manifest.json`.
- One-time project setup (creates `Localization Settings`, `ru` + `en` locales, `UI` string table, sets startup selectors, adds `LocalizationBootstrap` to `SampleScene` if missing):
  - Unity menu: `Shattered Forge/Localization/Initialize (ru default + UI table)`
- Runtime locale preference:
  - `PlayerPrefs["sf.selected_locale_code"]` (`LocalizationPreferences.SelectedLocaleCodeKey`) — written when changing language in Settings.
