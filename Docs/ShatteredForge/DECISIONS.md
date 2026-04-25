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
