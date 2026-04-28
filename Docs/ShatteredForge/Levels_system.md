# Levels System (Mission Mode)

## Goal

Add a camp portal flow where player selects a mission level (mini-run 3-7 rooms) and enters `LevelScene`.

## Runtime flow

- `CampHubController` opens `CampLevelPortalView` near dungeon anchor.
- Portal picks a specific level id or quick-picks by tier.
- Selection is passed via `PendingLevelRequest`.
- Scene transition goes through `SceneNavigation` to `SceneNames.LevelScene`.
- `LevelSessionController` loads selected `LevelDefinition` from `LevelCatalog`.
- `CombatRoomBootstrap` drives room combat and asks driver for room type / spawn config.

## Data assets

- `LevelTierDefinition` (`Assets/Scripts/Levels/LevelTierDefinition.cs`)
  - data-driven multipliers for hp/damage/move speed/attack speed
  - encounter density and bonus loot rolls
- `EnemyPoolDefinition` (`Assets/Scripts/Levels/EnemyPoolDefinition.cs`)
  - weighted enemy ids with tags and elite/boss flags
- `LevelDefinition` (`Assets/Scripts/Levels/LevelDefinition.cs`)
  - mission shape, pools, tier, loot, optional custom arena prefab
- `LevelCatalog` (`Assets/Scripts/Levels/LevelCatalog.cs`)
  - list of levels and visible quick-play tiers
- `LevelEnemyResolver` (`Assets/Scripts/Levels/LevelEnemyResolver.cs`)
  - pool filtering + weighted enemy pick + fallback ids

Resources load path:

- Catalog: `Resources.Load<LevelCatalog>("Levels/DefaultLevelCatalog")`
- Tiers/levels/pools are referenced by the catalog.

## No-risk prototype rules

- Level mode does not use `RunSessionController` or `RiskLossService`.
- Paper-doll/loadout remains untouched.
- Drops are awarded directly to `_account.stash` on boss clear.
- Death/abort returns to camp without equipment loss.

## Pause menu reuse

- `IPauseMenuView` + `PauseMenuBinding` + `PauseMenuConfig` in `Assets/Scripts/UI/IPauseMenuView.cs`
- `PauseMenuView` is the generic alias; legacy `CampPauseMenuView` still supported.
- Runtime loading order:
  1. `UI/PauseMenuUi`
  2. fallback `UI/CampPauseMenuUi`

## Editor tools

- Create level scene:
  - menu: `ShatteredForge/Scenes/Create Level Scene`
  - script: `Assets/Scripts/Editor/LevelSceneCreator.cs`
- Bootstrap demo level content:
  - menu: `ShatteredForge/Levels/Bootstrap Demo Level Content`
  - script: `Assets/Scripts/Editor/LevelContentBootstrapCreator.cs`
- Bootstrap everything in one click:
  - menu: `ShatteredForge/Levels/Bootstrap All (Scene + Content + Pause Prefabs)`
  - script: `Assets/Scripts/Editor/LevelModeBootstrapAllCreator.cs`
- Bake pause menu prefabs:
  - menu: `ShatteredForge/UI/Bake Camp Pause Menu UI Prefab`
  - script: `Assets/Scripts/Editor/CampPauseMenuPrefabCreator.cs`
  - outputs both `CampPauseMenuUi.prefab` and `PauseMenuUi.prefab`

## Manual checklist

- Open camp, approach dungeon marker, press `E` -> portal appears.
- Press quick-play tier button -> transitions to `LevelScene`.
- Clear boss -> guaranteed + random loot appears in stash after return.
- Press `Esc` in level -> pause opens, abandon returns to camp.
