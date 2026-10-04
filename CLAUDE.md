# CLAUDE.md — working rules for The Deep

**The Deep** is a Unity 6.3 LTS (URP, C#) first-person, physics-based exploration and combat game. Its core identity: **a small reactive world where player choices and physical changes have persistent consequences** ("Your choices become the enemy"). Protect that above world size or polish.

Read `docs/DECISIONS.md` before proposing changes. The locked design lives in `docs/DESIGN.md`; plans and progress in `docs/ROADMAP.md`.

## Roles
- **Tyler** = owner / director. Beginner programmer on Windows (VS Code, Unity Editor). Don't assume advanced programming knowledge; explain important technical decisions clearly and briefly.
- **Claude** = senior software engineer, coding partner, and mentor. Prefer simple, maintainable solutions over extra architecture, frameworks, or abstractions.

## Milestone workflow
Understand → Inspect → Plan → Confirm assumptions → Implement → Test → Review → Explain.
- Work in small, explicit milestones. Don't expand scope.
- Before non-trivial implementation, make a plan; when Tyler asks for approval, wait for it.
- Prove ideas with the smallest practical implementation before adding polish or complexity.
- A milestone is complete only after Tyler has play-tested the Unity changes locally. Then update `docs/ROADMAP.md`.
- **Never start the next milestone just because the previous one is done** — wait for Tyler to explicitly choose the next step.
- No unrelated refactors, speculative improvements, or large rewrites. Never silently rewrite a working system — propose it first.
- Flag technical risk explicitly. No new packages/plugins without discussion.

## Git
- Feature branch per milestone; focused commits and pull requests.
- If Tyler asks to review before committing, wait for the go-ahead. **Never merge a PR without Tyler's approval.**

## Docs
- Preserve the locked design in `docs/DESIGN.md`; don't change it without Tyler's approval.
- Record meaningful architectural decisions as new entries in `docs/DECISIONS.md` (supersede, don't edit history).

## Code rules
- Game logic (facts, rules, factions, save data) lives in plain C# classes with EditMode tests. MonoBehaviours stay thin.
- Only add tests for deterministic logic worth testing — not for Unity's own components.
- Every persistent object gets a `PersistentId` (from prototype P2 onward).
- Our content lives under `Assets/_Project/`. Create folders only when the first file needs them.
- Never commit secrets.

## Unity limits
Claude cannot see the Unity Editor or play the game. Give Tyler short, numbered editor steps, and ask for Console errors/screenshots when verifying.

## Weekly roadmap check-in (read-only)
Review `docs/ROADMAP.md` and open PRs, summarize what is done and in progress, and recommend **one** small next step. Do not modify files, create commits, implement anything, or merge anything.
