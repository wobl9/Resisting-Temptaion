# Main Menu Setup

1. Open/create your boot scene (menu scene).
2. Create an empty GameObject named `MainMenuBootstrap`.
3. Add component `MainMenuController`.
4. Optional: set `Gameplay Scene Name` in inspector to auto-load gameplay after profile select/new game.

Main menu features implemented:
- Select profile (progress is bound to selected profile).
- Create new game (creates new profile + marks it active).
- Open settings (volume, fullscreen, resolution).
- Quit game.

Profile save location:
- `Application.persistentDataPath/ShatteredForge/`
  - `profiles_index.json`
  - `Profiles/profile_<id>.json`

Runtime key:
- Active profile id is also mirrored to `PlayerPrefs["sf.active_profile_id"]` for quick access by gameplay systems.
