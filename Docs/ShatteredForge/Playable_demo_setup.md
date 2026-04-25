# Playable Demo Setup (Unity)

1. Create a new empty scene.
2. Create an empty GameObject named `GameBootstrap`.
3. Add component `PlayableLoopDemo` only.  
   Unity will automatically add `CombatRoomBootstrap` on the same GameObject (`RequireComponent`).  
   If you ever remove it by hand, `PlayableLoopDemo` will re-add it at runtime in `Awake`.
4. Press Play.
6. Use keyboard controls:

**With `CombatRoomBootstrap` (visual combat)**

Input uses the **Input System** package (not legacy `UnityEngine.Input`), matching Player Settings when active input is *Input System Package* only.

On first combat load, **Main Camera** is switched to **orthographic top-down** (full arena in frame). If your scene already had a camera, it is reconfigured the same way.

- `R` — start run from hub
- `WASD` (or arrows) — move; **gamepad** left stick also works
- Auto-fire at nearest enemy
- Clear combat rooms by defeating all enemies; for Shop / Forge / Event / Rest press `Space`
- `E` — extract run (bank loot)
- `K` — force death (lose loadout/loot except insured item)
- `H` — return to hub after a finished run (`Resolved`)

**Without combat (data-only loop)**

- `R` — start run
- `C` — clear current room (no arena)
- `E` / `K` / `H` — same as above

This demo validates the required core loop:

- hub stash -> run start
- room progression and carry loot
- extraction transfer to stash
- death loss with insurance exception
