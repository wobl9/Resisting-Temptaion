# Main Menu Setup

1. Open/create your boot scene (menu scene).
2. Create an empty GameObject named `MainMenuBootstrap`.
3. Add component `MainMenuController`.
4. Set `Gameplay Scene Name` to the dungeon/combat scene (e.g. `GameplayScene`).
5. Optional: set `Hub Scene Name` for a **new** expedition (default empty → `CampHub` from `SceneNames`). `Continue Game` always loads the gameplay scene directly. **Термин:** сцену `CampHub` в команде называем **«лагерь»** (англ. camp); имя ассета/сцены в Unity остаётся `CampHub`.

**Атмосфера лагеря (визуал):** на объекте `CampHubSystems` висит `CampHubCameraRig` — камера от третьего лица за героем (сглаживание), линейный туман и сплошной цвет очистки вместо «пустого» неба редактора. Купол `CampSkyDome` использует материал [`CampAtmosphere.mat`](Assets/Materials/CampAtmosphere.mat): позже на него можно повесить текстуру в слот **Base Map** (или заменить шейдер на skybox).
6. Add both scenes to **File → Build Settings** (see `EditorBuildSettings`).

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
- `PlayerPrefs["sf.pending_dungeon_entry"]` — set to `1` in the camp hub when entering the dungeon; consumed when `PlayableLoopDemo` starts a new run (`MenuSessionPrefs.PendingDungeonEntryKey`).

Localization (Unity Localization Package):
- Package dependency: `com.unity.localization` in `Packages/manifest.json`.
- One-time project setup (creates `Localization Settings`, `ru` + `en` locales, `UI` string table, sets startup selectors, adds `LocalizationBootstrap` to `SampleScene` if missing):
  - Unity menu: `Shattered Forge/Localization/Initialize (ru default + UI table)`
- Runtime locale preference:
  - `PlayerPrefs["sf.selected_locale_code"]` (`LocalizationPreferences.SelectedLocaleCodeKey`) — written when changing language in Settings.
