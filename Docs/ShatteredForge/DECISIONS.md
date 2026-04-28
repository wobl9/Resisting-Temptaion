# Decisions Log - Shattered Forge

Use this format for every decision:
- Date: `YYYY-MM-DD`
- Area: `Gameplay | UX | Tech | Economy | Content`
- Decision: short statement
- Why: reason and trade-off
- Impact: what changes because of this
- Follow-up: optional next action

---

## Decisions

- Date: `2026-04-25`
- Area: `Tech`
- Decision: Keep a dedicated AI context and progress tracking docs (`AI_CONTEXT.md`, `PROGRESS.md`, `DECISIONS.md`).
- Why: Reduce repeated project scanning and keep assistant context stable across sessions.
- Impact: Faster onboarding each session, clearer implementation priorities.
- Follow-up: Maintain docs after every meaningful coding pass.

- Date: `2026-04-25`
- Area: `Gameplay`
- Decision: Preserve full-loss extraction pressure as a non-negotiable core pillar.
- Why: This is the primary identity and tension of `Shattered Forge`.
- Impact: New systems must not remove death risk; mitigation should be limited and explicit.
- Follow-up: Review new mechanics against this pillar before merging.

- Date: `2026-04-28`
- Area: `UX`
- Decision: Active profile is represented as a top-corner interactive entry that opens profile actions (switch/create), instead of a dedicated always-visible `Select Profile` main button when profile already exists.
- Why: Reduce primary menu clutter and keep profile management available contextually.
- Impact: Main menu adapts by profile state; profile operations remain reachable but move into a focused submenu.
- Follow-up: Keep `Menu_logic_contract.md` and menu rule in sync with this interaction model.

- Date: `2026-04-28`
- Area: `Tech`
- Decision: `TryEnterGameplay` must always trigger scene load; if explicit gameplay scene is not configured, fallback to reloading active scene.
- Why: Avoid UX where buttons appear to do nothing except show status text.
- Impact: `Continue Game` and `New Game` always perform a concrete transition/start action in prototype setup.
- Follow-up: Replace fallback with canonical `SceneNavigation` path once all target scenes are finalized in build.
