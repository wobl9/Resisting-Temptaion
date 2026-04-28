# Camp Character Sheet Layout Tool

## What this solves
- Manual layout tuning without editing code.
- Fast overlap checks for body slots.
- Repeatable bake flow from one layout asset.

## Files
- Layout asset type: `Assets/Scripts/UI/CampCharacterSheetLayoutAsset.cs`
- Skin asset type: `Assets/Scripts/UI/CampCharacterSheetSkinAsset.cs`
- Editor window: `ShatteredForge/UI/Character Sheet Layout Window`
- Prefab bake menu:
  - `ShatteredForge/UI/Bake Camp Character Sheet UI Prefab (Default)`
  - `ShatteredForge/UI/Bake Camp Character Sheet UI Prefab (Using Active Layout)`

## Workflow (edit -> validate -> apply -> bake -> play)
1. Open `ShatteredForge/UI/Character Sheet Layout Window`.
2. Create or select a `CampCharacterSheetLayoutAsset`.
3. (Optional) create/select `CampCharacterSheetSkinAsset` and assign frame sprites.
4. Edit `PaperDoll`, `Stash`, `Chrome` values.
5. Click `Validate` and fix any overlap warnings.
6. Click `Apply To View` (when a scene object with `CampCharacterSheetView` is selected).
7. Click `Apply To Prefab` to rebuild `Assets/Resources/UI/CampCharacterSheetUi.prefab`.
8. Enter Play mode and verify.

## See result immediately
- Select a scene object that has `CampCharacterSheetView`.
- In the layout window, enable `Live Preview`.
- Every edit in the asset is immediately applied to the selected view.
- Use `Auto Layout Slots` for one-click non-overlapping body-slot placement.

## Useful buttons
- `Load From View`: pull current scene layout back into asset.
- `Reset Defaults`: restore default values.
- `Apply Diablo Preset`: apply a Diablo-like structural preset (slot topology + stash below).
- `Create Default Diablo Skin`: creates a skin asset with tuned default tints (then assign your sprites).
- `Auto-assign by filename`: scans `Sprite Folder` and auto-fills skin fields by keywords in sprite names.
- `Bake Default`: rebuild prefab from built-in defaults (ignores active layout).
- `Absolute -> Normalized`: convert current slot/torso absolute coordinates to normalized mode.
- `Normalized -> Absolute`: bake normalized values into absolute coordinates and disable normalized mode.

## Notes
- `CampCharacterSheetPanel` first tries the assigned view/prefab, then Resources fallback.
- If no custom layout is assigned, runtime uses internal defaults.
- If a skin asset is assigned, panel/slots/stash cells/tooltip use skin sprites automatically.
- Auto-assign keyword hints:
  - panel: `panel`, `frame`, `window`, `inventory_bg`
  - slot: `slot`, `equip_slot`
  - stash: `stash`, `grid`, `cell`
  - tooltip: `tooltip`, `hint`, `popup`
  - torso: `torso`, `body`, `silhouette`
- For body slots and torso you can use relative coordinates:
  - `normalizedPosition` / `torsoNormalizedPosition` are in `0..1` range.
  - `x`: left -> right, `y`: top -> bottom.
  - With `useNormalizedPosition` enabled, structure stays stable when paper-doll size changes.
