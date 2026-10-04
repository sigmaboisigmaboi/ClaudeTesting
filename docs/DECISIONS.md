# The Deep — Decision Record

This file records the project's major design and architecture decisions: **what** we chose, **why**, and **what it costs us**. It is the source of truth when a question comes up again ("why aren't we using Unreal?").

Related docs: [DESIGN.md](DESIGN.md) · [ROADMAP.md](ROADMAP.md)

## How to use this file

- **Locked** decisions are not re-opened casually. Changing one requires the owner's approval and a **new** entry that supersedes the old one (mark the old one `Superseded by D-0xx`). Don't edit history.
- Add a new entry whenever we make a choice that would be expensive to reverse (engine features, packages, data formats, scope cuts, gate reviews).
- Keep entries short.

Template:

```
## D-0xx — Title
- Status: Proposed | Locked | Superseded by D-0yy
- Date: YYYY-MM-DD
- Decision:
- Reason:
- Alternatives considered:
- Why rejected:
- Consequences / tradeoffs:
```

---

## D-001 — Unity 6 with URP
- **Status:** Locked
- **Date:** 2026-10-04
- **Decision:** Build the game in **Unity 6 (LTS)** using the **Universal Render Pipeline (URP)**. The exact LTS version is recorded here when the project is created in Phase 0.
- **Reason:** Mature physics (PhysX), the largest tutorial/answer base for exactly the systems we need (character controllers, rigidbodies, NavMesh, save systems), good tooling for data (ScriptableObjects), and logic written in plain text C# files that Claude Code can read, write, and test. The owner already wants to learn Unity. URP is lighter and simpler than HDRP and suits a stylized look.
- **Alternatives considered:** Unreal Engine 5; Godot 4; Unity with HDRP.
- **Why rejected:**
  - *Unreal:* best built-in destruction (Chaos) and large-world tools, but Blueprints are visual graphs Claude Code can't edit, and C++ is a hard first language.
  - *Godot:* very AI-friendly (text scenes) and easy to learn, but a thinner 3D/physics/destruction ecosystem for this kind of game.
  - *HDRP:* heavier, more complex, aimed at high-end realism we aren't pursuing.
- **Consequences / tradeoffs:** No Chaos-style destruction out of the box — we build authored destruction ourselves (D-006). Claude can't see the Unity Editor, so scene/prefab work is done by the owner with step-by-step guidance.

## D-002 — C# as the only programming language
- **Status:** Locked
- **Date:** 2026-10-04
- **Decision:** All game code is C#.
- **Reason:** Unity's standard language; readable for a beginner; text-based so Claude Code can work with it and we can unit-test pure logic with the Unity Test Framework.
- **Alternatives considered:** Unity Visual Scripting; mixing in other languages.
- **Why rejected:** Visual Scripting graphs are hard to review, diff, and edit with AI help; mixing languages adds complexity for no gain.
- **Consequences / tradeoffs:** The owner learns C# alongside the project. Claude explains new concepts when they first appear.

## D-003 — First-person perspective for the MVP
- **Status:** Locked
- **Date:** 2026-10-04
- **Decision:** The MVP is first-person. We do not design around third-person character animation.
- **Reason:** Removes the biggest beginner time sink — character animation (rigging, blending, IK, animation state machines) and third-person camera collision in tight tunnels. Physics grab/throw also reads best in first person.
- **Alternatives considered:** Third-person; switchable camera.
- **Why rejected:** Both multiply animation and camera work before the core idea is proven.
- **Consequences / tradeoffs:** The player character is barely visible; character identity comes from hands/tools, voice, and the world. Switching to third-person later would be a large change, not a toggle.

## D-004 — Art direction: stylized industrial sci-fi / underground brutalism
- **Status:** Locked
- **Date:** 2026-10-04
- **Decision:** Stylized, intentional art: massive concrete architecture, rusty industrial machinery, pipes and cables, mining equipment, industrial hazards, strong lighting and signage. Deeper regions become ancient and increasingly strange.
- **Reason:** A strong, consistent style is achievable for a small team, makes "breakable" objects easy to communicate (wood, rust, cracked concrete, warning paint), and hides imperfections in authored destruction.
- **Alternatives considered:** Realistic art; generic "cheap low-poly".
- **Why rejected:** Realism is far too expensive to produce. Generic low-poly reads as unintentional and undersells the setting.
- **Consequences / tradeoffs:** The look depends heavily on **lighting** and a reusable **modular kit** (walls, pipes, catwalks, signage, decals). Lighting-heavy scenes make lighting *after* destruction harder (see DESIGN §14 Performance) — we accept some realtime local lights in destructible areas.

## D-005 — Connected areas instead of a seamless open world
- **Status:** Locked
- **Date:** 2026-10-04
- **Decision:** The world is a graph of hand-built **areas** (one Unity scene each) connected by tunnels, elevators, and rail. The MVP is one district of ~4 areas.
- **Reason:** Scene-per-area loading is simple and reliable, keeps performance predictable, and fits an underground world where tunnels naturally hide transitions.
- **Alternatives considered:** Seamless open world with streaming; one giant scene.
- **Why rejected:** Streaming is a large technical investment with little gameplay benefit here; one giant scene doesn't scale and is hard to work in.
- **Consequences / tradeoffs:** Loading transitions exist — we make them diegetic (elevator, long tunnel, rail car). Exits between areas can be opened/blocked by world state.

## D-006 — Authored / pre-fractured destruction
- **Status:** Locked
- **Date:** 2026-10-04
- **Decision:** Destruction uses meshes pre-fractured in Blender, swapped in at runtime, with hand-authored support links and scripted set pieces for major events (MVP: the **Bridge** and the **Water Pump**). Destruction must be gameplay-relevant.
- **Reason:** Looks good, performs predictably, and is buildable by a beginner. Persisting "this ID is destroyed" is simple.
- **Alternatives considered:** Runtime fracture of buildings; voxel/fully destructible environments; making everything destructible.
- **Why rejected:** Each is extremely difficult, performance-heavy, and lets players break level design. Not achievable for this team.
- **Consequences / tradeoffs:** Every breakable object is hand-made. Most of the world (natural rock, main tunnels) is not destructible; players learn a visual language for what can break.

## D-007 — Event-driven, state-based world simulation
- **Status:** Locked
- **Date:** 2026-10-04
- **Decision:** Only the current area runs real-time physics/AI. The rest of the world exists as data (**facts**, faction values, infrastructure node states) and changes only at **world ticks** (area transitions, resting, mission completion), when consequence rules run.
- **Reason:** Cheap, deterministic, testable, and easy to save. It is the standard approach of immersive sims and RPGs.
- **Alternatives considered:** Continuous off-screen simulation of NPCs, economy, population, and factions.
- **Why rejected:** Expensive, hard to debug, and mostly invisible to the player.
- **Consequences / tradeoffs:** The world changes in steps rather than live. Emergence comes from combining a few simple systems (nodes → resources → settlement condition → faction/NPC content) rather than from a deep simulation.

## D-008 — Three initial factions
- **Status:** Locked
- **Date:** 2026-10-04
- **Decision:** The MVP has exactly three factions: **The Concord**, **The Delvers' Union**, **The Hollowers**. Each has a mechanically meaningful relationship with infrastructure, resources, territory, and player actions (see DESIGN §9).
- **Reason:** Three is enough for real dilemmas (helping one hurts another) while staying small enough to make every reaction convincing.
- **Alternatives considered:** Five or more factions (Garrison, Deep-dwellers, Old Machines) in the MVP.
- **Why rejected:** More factions dilute reactions and multiply content.
- **Consequences / tradeoffs:** Additional major factions require a new decision entry; candidates are listed in DESIGN for 1.0+.

## D-009 — No multiplayer
- **Status:** Locked
- **Date:** 2026-10-04
- **Decision:** Single-player only.
- **Reason:** Networked physics plus persistent destruction plus a shared world state is an order of magnitude harder and would consume the project.
- **Alternatives considered:** Co-op; multiplayer later via retrofit.
- **Why rejected:** Out of reach for this team; retrofitting networking is notoriously painful, so we don't pretend to keep the door open.
- **Consequences / tradeoffs:** We can use simple single-player patterns (global world state, local saves) freely.

## D-010 — No procedural world generation for the MVP
- **Status:** Locked
- **Date:** 2026-10-04
- **Decision:** All MVP areas are hand-built.
- **Reason:** The core idea is that the world *remembers* specific places. Authored places make consequences legible and save/load simple.
- **Alternatives considered:** Procedurally generated districts or caves.
- **Why rejected:** Fights authored persistence and consequences, and is a large system on its own.
- **Consequences / tradeoffs:** Content takes hand-work. Small procedural cave sections may be considered after 1.0.

## D-011 — Small reactive world over large static world (core identity)
- **Status:** Locked
- **Date:** 2026-10-04
- **Decision:** The game's identity is **"Your choices become the enemy."** A small area that reacts convincingly beats a large world with superficial systems. This is the tie-breaker for every scope decision.
- **Reason:** The memorable experience is "I changed something, and the world remembers." World size doesn't deliver that; systemic reactions do.
- **Alternatives considered:** Larger world with lighter reactions.
- **Why rejected:** Spreads a small team thin and produces shallow interaction.
- **Consequences / tradeoffs:** When choosing between "more content" and "deeper reaction to what already exists," choose reaction. The canonical test is the bridge chain: destroy bridge → event recorded → route changes → NPCs react → faction reputation changes → trade changes → missions change → player meets consequences later.

## D-012 — Git workflow: main + feature branches + pull requests, Git LFS for binaries
- **Status:** Locked
- **Date:** 2026-10-04
- **Decision:** `main` holds reviewed work. Each milestone is built on a feature branch and merged through a pull request. Binary assets (models, textures, audio, .blend) are stored with Git LFS (`.gitattributes`). Unity-generated folders are ignored (`.gitignore`).
- **Reason:** PRs give a review point and a safe undo; LFS keeps the repository fast as art arrives.
- **Alternatives considered:** Committing directly to `main`; no LFS.
- **Why rejected:** Direct commits remove the review step; large binaries in normal Git bloat history permanently.
- **Consequences / tradeoffs:** Each PC needs `git lfs install` once (Git for Windows includes LFS). GitHub's free LFS storage/bandwidth quota is limited — check it once art volume grows.

## D-013 — Repository layout
- **Status:** Locked
- **Date:** 2026-10-04
- **Decision:** The Unity project lives at the repository root (`Assets/`, `Packages/`, `ProjectSettings/`). Our content goes under `Assets/_Project/`. Blender source files go in `ArtSource/` (outside `Assets/` so Unity doesn't import them). Folders are created only when their first file exists. One runtime assembly definition plus one EditMode test assembly — needed so tests can reference game code.
- **Reason:** Standard Unity layout; `_Project` keeps our files separate from imported packages/assets; no empty-folder clutter.
- **Alternatives considered:** Unity project in a subfolder; a full folder tree up front.
- **Why rejected:** A subfolder adds a path level to everything; an up-front tree creates empty folders that may never be used.
- **Consequences / tradeoffs:** Unity Hub creates projects in a new folder and may refuse an existing non-empty one, so Phase 0 creates the project elsewhere and moves `Assets/`, `Packages/`, `ProjectSettings/` into the repo (step-by-step instructions given then).
