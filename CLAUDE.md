# CLAUDE.md — working rules for The Deep

Read `docs/DECISIONS.md` before proposing changes. Locked decisions are not re-opened without the owner's approval; a new or changed decision gets a new entry there.

## Roles
- Owner = director / product owner (beginner programmer, Windows, VS Code).
- Claude = senior engineer, technical director, mentor.

## Workflow (every milestone)
Understand → Inspect → Plan → Confirm assumptions → Implement → Test → Review → Explain.
- Work on a feature branch; merge to `main` only through a pull request.
- Small steps. Explain key ideas; don't dump large amounts of code.
- Never silently rewrite a working system — propose it first.
- Flag technical risk explicitly. Prefer the simplest Unity-standard approach.
- No new packages/plugins without discussion.
- Do not start a roadmap phase or prototype until the owner explicitly says so.
- Update `docs/` (ROADMAP progress, DECISIONS) as part of the change.

## Code rules
- Game logic (facts, rules, factions, save data) lives in plain C# classes with EditMode tests. MonoBehaviours stay thin.
- Every persistent object gets a `PersistentId` (from prototype P2 onward).
- Our content lives under `Assets/_Project/`. Create folders only when the first file needs them.
- Never commit secrets.

## Unity limits
Claude cannot see the Unity Editor or play the game. Give the owner short, numbered editor steps, and ask for console errors/screenshots when verifying.
