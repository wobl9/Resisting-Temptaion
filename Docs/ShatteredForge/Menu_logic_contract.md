# Start Menu Logic Contract

Purpose: this file is the source of truth for start menu behavior.
When code changes, scenarios here must still pass.

Status labels:
- `Approved` - behavior is confirmed and required.
- `Draft` - candidate behavior, not final yet.
- `Deprecated` - old behavior kept for history, not required.

---

## Glossary (RU UI ↔ EN canonical)

Rule: **player-facing strings can be Russian**, but this document’s **canonical identifiers stay English** in backticks (they should map 1:1 to code/state names).

| RU (UI) | EN (canonical) | Meaning |
|---|---|---|
| Новая игра | `New Game` | Starts a **new expedition** for the active profile (fresh run state). |
| Продолжить игру | `Continue Game` | Resumes an **active expedition** from the last saved checkpoint. Must be **hidden** if no active expedition exists. |
| Настройки | `Settings` | Opens settings screen. |
| Выход из игры | `Quit Game` | Exits the application (Editor: stops Play Mode). |
| Профиль | `Profile` | Player identity slot; owns expedition save data and meta progression. |
| Активный профиль | `Active profile` | The profile currently selected for play; persisted in profile index + mirrored to `PlayerPrefs["sf.active_profile_id"]`. |
| Вылазка | `Expedition` | A persisted in-progress run attached to a profile (has checkpoint data). Distinct from “starting gameplay scene” without expedition state. |
| Чекпоинт вылазки | `Expedition checkpoint` | Last saved resume point inside an active expedition. |
| Сохранённые данные | `Saved data` | At minimum: profiles index + per-profile files under `Application.persistentDataPath/ShatteredForge/`. Technical map: `Docs/ShatteredForge/Profile_persistence.md`. |

---

## Core Invariants (Must Always Hold)

1. There is at most one active profile at a time.
2. `Continue Game` is only shown when an active profile exists **and** an active expedition exists for that profile.
3. `New Game` opens gameplay scene. `Continue Game` opens gameplay scene **when it is shown**.
4. Profile operations (create/select/delete) keep storage and active profile state consistent.
5. `Settings` is always reachable from the main menu.
6. `Quit Game` always exits the game (or stops Play Mode in Unity Editor).
7. First-run onboarding: if no saved profiles exist, the main menu shows exactly three actions: `New Game`, `Settings`, `Quit Game`.
8. Returning player: if saved profiles exist, the main menu shows the active profile entry plus `New Game`, `Settings`, `Quit Game`, and conditionally `Continue Game` when an active expedition exists.
9. `Continue Game` must resume from the last saved expedition checkpoint for the active profile (not start a fresh expedition unless explicitly chosen).
10. If no active expedition exists, `Continue Game` must be **hidden** (not disabled-in-place).

---

## Decision Table (Canonical)

| Case ID | Precondition | Action | Expected Result | Status |
|---|---|---|---|---|
| MNU-001 | No profiles exist | Press `New Game` | Profile name prompt opens | Approved |
| MNU-002 | No profiles exist | Attempt `Continue Game` | `Continue Game` is not rendered; no invalid launch | Approved |
| MNU-003 | Profiles exist, active profile set, active expedition exists | Press `Continue Game` | Gameplay scene opens and resumes last saved expedition checkpoint for active profile | Approved |
| MNU-009 | Profiles exist, active profile set, no active expedition | Attempt `Continue Game` | `Continue Game` is not rendered | Approved |
| MNU-004 | Profiles exist | Select profile `P` | `P` becomes active and persisted | Approved |
| MNU-005 | Profiles exist, active `P` | Delete active profile `P` | `P` removed from storage; new active profile chosen or cleared if none left | Approved |
| MNU-006 | Profiles exist, non-active `P` | Delete profile `P` | `P` removed from storage; active profile unchanged | Approved |
| MNU-007 | Any menu state | Open `Settings` | Settings screen opens and returns back safely | Approved |
| MNU-008 | Any menu state | Press `Quit Game` | App quits (build) / Play Mode stops (Editor) | Approved |

---

## Scenario Log (Living Regression Checklist)

### Template
- ID: `MNU-XXX`
- Title:
- Given:
- When:
- Then:
- Notes:
- Status: `Draft | Approved | Deprecated`

### Scenarios

- ID: `MNU-101`
- Title: Create first profile and start game
- Given: no profiles in storage
- When: user presses `New Game`, enters a profile name, confirms
- Then: profile is created, set active, gameplay scene opens immediately after confirm
- Notes: replaces older flow where `Create Game` immediately launched without a dedicated name step
- Status: `Approved`

- ID: `MNU-102`
- Title: Continue with existing active profile
- Given: profile `A` exists and is active, and an active expedition exists for `A`
- When: user presses `Continue Game`
- Then: gameplay scene opens and active profile id remains `A`
- Notes: must resume last expedition checkpoint; must not silently start a fresh expedition
- Status: `Approved`

- ID: `MNU-103`
- Title: Delete active profile fallback
- Given: multiple profiles exist, current active is `A`
- When: user deletes profile `A`
- Then: `A` is removed, another profile becomes active (or active becomes empty if none left)
- Notes: storage index and runtime active key must match
- Status: `Approved`

- ID: `MNU-201`
- Title: First launch main menu (no saved data)
- Given: no profiles exist in storage
- When: user opens the game
- Then: main menu shows exactly three buttons: `New Game`, `Settings`, `Quit Game`
- Notes: no `Continue Game` on first launch
- Status: `Approved`

- ID: `MNU-202`
- Title: First launch profile creation flow
- Given: no profiles exist in storage
- When: user presses `New Game`
- Then: a dedicated screen opens asking for a new profile name; after confirm, gameplay starts immediately
- Notes: user text referenced `Start Game` wording; canonical action is `New Game`
- Status: `Approved`

- ID: `MNU-203`
- Title: Returning player main menu (saved data exists)
- Given: at least one profile exists and an active profile is selected
- When: user opens the game
- Then: main menu shows an entry displaying the active profile name, `New Game`, `Settings`, `Quit Game`, and shows `Continue Game` only if an active expedition exists for the active profile (otherwise it is hidden)
- Notes: `New Game` starts a new expedition; `Continue Game` resumes from last saved expedition state
- Status: `Approved`

- ID: `MNU-204`
- Title: Active profile quick menu
- Given: an active profile exists
- When: user presses the active profile button
- Then: a profile menu opens where user can switch to another profile or delete the current profile
- Notes: must not lose active profile selection accidentally; deletion must update active selection rules from `MNU-103`
- Status: `Approved`

---

## Non-Negotiable Anti-Bugs

1. Never open gameplay scene with an invalid or stale active profile id.
2. Never leave deleted profile entries in index file.
3. Never keep `PlayerPrefs["sf.active_profile_id"]` pointing to a deleted profile.
4. Never allow profile deletion to crash menu rendering.

---

## Change Protocol

Before merging menu-related code:
1. Update this file if behavior changed.
2. Add/adjust scenario IDs for new logic.
3. Re-run all `Approved` scenarios manually.
4. Mark scenario results in session notes or PR description.
