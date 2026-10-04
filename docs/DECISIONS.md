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
- **Decision:** Build the game in **Unity 6 (LTS)** using the **Universal Render Pipeline (URP)**. The exact version is pinned in D-014.
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

## D-014 — Pinned Unity version: 6.3 LTS (6000.3.25f1), "Universal 3D" template
- **Status:** Locked
- **Date:** 2026-10-04
- **Decision:** The project uses **Unity 6000.3.25f1** (Unity 6.3 LTS), created from Unity Hub's **Universal 3D** (URP) template. `ProjectSettings/ProjectVersion.txt` is the source of truth. Upgrades (even patch versions) happen only through a new decision entry, on their own branch.
- **Reason:** 6.3 is the newest LTS (supported until December 2027); Unity 6.0 LTS support ends October 2026. Pinning one exact version prevents "works on my machine" differences and accidental project upgrades.
- **Alternatives considered:** Unity 6.0 LTS; newer Update releases (6.5/6.6).
- **Why rejected:** 6.0 is at end of support; Update releases have short support windows we don't need.
- **Consequences / tradeoffs:** Opening the project with a different editor version will prompt an upgrade — decline it. The template added defaults we haven't reviewed yet (sample scene, tutorial readme, and the Unity Version Control, Visual Scripting and Multiplayer Center packages); they are kept untouched for now and removed only by a deliberate, reviewed change.

## D-015 — Player controller: CharacterController + project-wide Input Actions
- **Status:** Locked
- **Date:** 2026-10-04
- **Decision:** The first-person player uses Unity's built-in **CharacterController** driven by one small script (`FirstPersonController`). Input is read from the existing **project-wide Input Actions** asset (`Assets/InputSystem_Actions.inputactions`, actions `Player/Move` and `Player/Look`) via `InputSystem.actions.FindAction`. No `PlayerInput` component, generated wrapper class, or second actions asset.
- **Reason:** CharacterController handles collision, slopes, and steps without fighting the physics engine — the simplest reliable first-person movement. Reusing the one actions asset avoids two competing input setups and needs no Inspector wiring.
- **Alternatives considered:** Rigidbody-based controller; `PlayerInput` component; a new actions asset or generated C# wrapper.
- **Why rejected:** A Rigidbody controller needs more tuning (friction, slopes, jitter) for no P1.1 benefit; the other input options add setup without adding capability.
- **Consequences / tradeoffs:** Actions are looked up by name — renaming them in the asset breaks the script (it fails loudly on start). CharacterController does not push Rigidbodies by itself; P1.2 (grab/throw/push) will need to handle that deliberately.

## D-016 — Player pushes physics objects via OnControllerColliderHit
- **Status:** Locked
- **Date:** 2026-10-04
- **Decision:** The player keeps its CharacterController (D-015). A separate component, `PlayerPushRigidbodies`, uses Unity's `OnControllerColliderHit` to push non-kinematic Rigidbodies the player walks into: a horizontal impulse in the walking direction, scaled by frame time, applied at the contact point. Contacts from above (standing on an object) are ignored. One tunable value: `pushStrength` (default 300 N).
- **Reason:** A CharacterController treats Rigidbodies like walls, so pushing must be added deliberately. An impulse respects mass (light objects scoot, heavy ones resist), works at any frame rate, and pushing at the contact point lets objects tip naturally. A separate component leaves the tested `FirstPersonController` untouched and can be disabled or removed on its own.
- **Alternatives considered:** A Rigidbody-based player; setting the pushed object's velocity directly.
- **Why rejected:** A Rigidbody player means rewriting a working, play-tested controller before we know it's needed. Setting velocity directly ignores mass, so every object would feel the same weight.
- **Consequences / tradeoffs:** Push feel depends on `pushStrength`, object mass, and friction, and may need tuning. Physics objects do not push the player back. Resolves the open point noted in D-015.

## D-017 — P3.1 persistence spike: facts in WorldState, saved as JSON
- **Status:** Locked (spike-scoped). The "pad loads its own copy" shortcut is **superseded by D-020** (shared `WorldSession`); immediate saving remains until P3 proper.
- **Date:** 2026-10-04
- **Decision:** Persistence is pulled forward as **P3.1 — Persistence Spike**, before P1's remaining physics items and P2. A plain C# `WorldState` class holds a list of fact names and owns its own JSON save/load (`ToJson`/`FromJson`, `LoadFromDisk`/`SaveToDisk`/`DeleteSaveFile`) using Unity's built-in `JsonUtility`, writing `world_state.json` to `Application.persistentDataPath`. Facts are **one-way events** (once recorded, they stay recorded). A gameplay component (`CrateTargetPad`) only detects its event and calls `WorldState.Record`. On load, an **authored "after" state** is applied (the crate is placed on the pad) — physics positions are not saved.
- **Reason:** Tests the core identity — "I changed something, and the world remembers" — as early as possible, using the already-proven push mechanic. Keeping JSON handling inside `WorldState` keeps gameplay scripts focused and the persistence logic testable (EditMode tests).
- **Alternatives considered:** Waiting for P2/P3 as originally ordered; saving crate positions; a SaveManager or save-slot system; the Newtonsoft JSON package.
- **Why rejected:** Waiting delays the most important question; saved positions contradict DESIGN (only meaningful state persists); a save framework is premature for one fact; `JsonUtility` handles a list of strings, so Newtonsoft is not needed until facts need values or dictionaries.
- **Consequences / tradeoffs:** Temporary spike shortcuts, to be replaced in P3 proper: saving immediately when a fact changes (DESIGN §13 says save at area transitions), the pad component loading its own copy of the world state, and no `PersistentId`. P1's remaining physics items stay open.

## D-018 — Grab and throw via velocity-based holding
- **Status:** Locked
- **Date:** 2026-10-04
- **Decision:** A separate component, `PlayerGrabThrow`, lets the player pick up, carry, drop, and throw physics objects. Pick-up uses a short sphere cast from the camera and only accepts non-kinematic Rigidbodies up to a mass limit (default 30 kg). A held object is moved each physics step by **setting its velocity** toward a hold point in front of the camera (capped speed, spin damped) — never teleported. While held, its gravity is off and its collisions with the player's CharacterController are ignored; both are restored on drop or throw. It auto-drops if snagged too far from the hold point. Throwing is a forward impulse (mass-aware). Input reuses the existing `Player/Interact` (E, read with `WasPressedThisFrame` so a tap works despite the template's Hold interaction) and `Player/Attack` (left mouse) actions.
- **Reason:** Velocity-based holding keeps the object inside the physics simulation, so it collides with walls instead of passing through them. Ignoring player collision prevents the held object from blocking the player or being shoved by `PlayerPushRigidbodies`. Reusing existing actions avoids a second input setup.
- **Alternatives considered:** Parenting the held object to the camera; a physics joint (spring); new input actions or a separate input asset.
- **Why rejected:** Parenting teleports the object through walls; a joint is springier and harder to tune for a beginner; new input setup duplicates what exists.
- **Consequences / tradeoffs:** Feel depends on several tunable values (hold distance, follow strength, max hold speed, throw impulse). A held object can still jitter when pressed hard against geometry; the speed cap and auto-drop limit this. No crosshair yet, so the sphere cast's thickness makes aiming forgiving.

## D-019 — Impact strength = incoming momentum (P1.4 prototype)
- **Status:** Locked (prototype-scoped)
- **Date:** 2026-10-04
- **Decision:** Physical impacts are measured as **impact strength = incoming object's mass (kg) × impact speed (m/s)** — its momentum — using the physics engine's `Collision.relativeVelocity` at contact. Only moving physics objects count; impacts below a small threshold are ignored. In P1.4 an `ImpactMeter` component logs the strength and adds a horizontal knockback impulse proportional to it (capped), because a 60 kg target barely moves from light hits through collision response alone. There is no health, damage, or combat state.
- **Reason:** Matches DESIGN's "throw damage scales with mass × speed", is simple to explain and tune, and is testable as a pure function. Measuring at contact is more reliable than reading the other object's velocity after the collision is resolved.
- **Alternatives considered:** Kinetic energy (½·m·v²); the solver's `Collision.impulse`; physics-only knockback with no added shove.
- **Why rejected:** Energy makes speed dominate (a fast light crate would outweigh a slow heavy one far more than it feels); `impulse` depends on the target's own mass and solver details, so it's harder to reason about; physics-only knockback was too subtle on a character-sized mass to evaluate.
- **Consequences / tradeoffs:** `relativeVelocity` includes sliding motion, so glancing hits can read slightly stronger than head-on ones. The knockback scale is a feel value, not physics. When real damage arrives (Phase 2), it should build on this measure rather than a new one.

## D-020 — One shared WorldSession; WorldState v2 with destroyed ids
- **Status:** Locked
- **Date:** 2026-10-04
- **Decision:** During play, all code reads and records through `WorldSession.State`: one `WorldState` loaded from disk on first use, kept across scene loads, and reset at the start of every Play session (`RuntimeInitializeOnLoadMethod`). `WorldState` v2 adds a `destroyedIds` list (`MarkDestroyed`/`IsDestroyed`) beside `facts`; version 1 files load with an empty list. `CrateTargetPad` now uses the session instead of loading its own copy. Saving is still immediate on destruction or fact change, plus on every area exit.
- **Reason:** With more than one recorder per session, private copies overwrite each other's saves (lost updates) — the temporary P3.1 shortcut noted in D-017. A single static holder is the smallest fix; destroyed ids are the natural record for destruction.
- **Alternatives considered:** A SaveManager MonoBehaviour/singleton in each scene; a dictionary keyed by area (Newtonsoft JSON).
- **Why rejected:** A scene object must be carried across scene loads and adds setup; a list of globally unique ids works with the built-in `JsonUtility` and needs no new package.
- **Consequences / tradeoffs:** Static state, so it must be reset per Play session (done). Immediate saves are fine at this scale; P3 proper decides final save timing. Supersedes D-017's "pad loads its own copy" shortcut.

## D-021 — Destructible model: intact → pre-placed pieces → rubble, with integrity and support links
- **Status:** Locked
- **Date:** 2026-10-04
- **Decision:** A `Destructible` (with a `PersistentId`) has a collider on its root and three authored children: **Intact**, **Fractured** (inactive, pre-placed piece Rigidbodies), and **Rubble** (inactive, static after-state). Impacts from pushed or thrown physics objects deal damage = impact strength (D-019) above a threshold, reducing **integrity** (structures only — not a health system). At zero it breaks: intact off, pieces on (with an outward burst from the hit point), destruction recorded and saved. An optional `supports` list makes it collapse when all supports are destroyed (the bridge deck). On load, a recorded Destructible shows only its Rubble. Objects currently held by the player deal no damage. The pure rules live in `DestructionRules` and are unit-tested. For the P2 slice the pieces are cube chunks; a Blender-fractured mesh can later replace a Fractured child without code changes.
- **Reason:** Matches DESIGN §7 (authored destruction, authored support graph, persisted state) with no runtime fracture, and keeps the gameplay rules testable outside Unity.
- **Alternatives considered:** Runtime mesh fracture; physics joints that break; an event bus for support notifications; letting held objects ram structures.
- **Why rejected:** Runtime fracture is out of scope (D-006); joints are harder to tune and persist; dependents are found directly when something breaks, which is enough at this scale; ramming with held objects would make throwing pointless.
- **Consequences / tradeoffs:** Every destructible needs hand-authored pieces and rubble. Support notification scans Destructibles when something breaks (fine for dozens, revisit for hundreds).

## D-022 — Debris: freeze in place under a budget; authored rubble after reload
- **Status:** Locked
- **Date:** 2026-10-04
- **Decision:** Broken pieces simulate briefly, then **freeze in place** (kinematic, colliders kept) after a settle time or as soon as they all sleep. A shared `DebrisBudget` caps actively simulating pieces (40); a new break that would exceed it freezes the oldest groups early. When a scene loads already destroyed, the authored **Rubble** is shown instead of pieces — piece positions are never saved.
- **Reason:** Freezing avoids a visible "pop" while the player watches; the budget keeps physics cost bounded; authored rubble keeps saves tiny and matches DESIGN §13 (no saved physics state).
- **Alternatives considered:** Swapping pieces to rubble while the player watches; saving piece positions; fading pieces out.
- **Why rejected:** A visible swap looks wrong; saved positions contradict the save design; fading loses the "world stays changed" feel.
- **Consequences / tradeoffs:** The debris layout after a return differs from what the player saw — accepted, and mostly hidden (bridge debris falls into the chasm). Frozen pieces still cost rendering until the scene unloads.

## D-023 — Minimal area transitions (pulled forward from P3)
- **Status:** Locked
- **Date:** 2026-10-04
- **Decision:** An `AreaExit` trigger saves the session and loads a target scene by name; an `AreaSpawnPoint` with the matching id places the player (briefly disabling the CharacterController to move it). Scenes used this way must be in the build scene list. EditMode scene-validation tests check that exits target build scenes with a matching spawn point, and that every `PersistentId` is filled in and unique.
- **Reason:** "Leave and come back to a changed world" is the core identity test, and needs real scene unloading/loading — stop/Play alone doesn't prove it.
- **Alternatives considered:** Loading screens or additive scene streaming; only testing via stop/Play.
- **Why rejected:** Streaming/loading screens are beyond a prototype (D-005 keeps transitions simple); stop/Play doesn't exercise in-session unload/reload.
- **Consequences / tradeoffs:** A full scene load per transition (fine for small areas). Each scene has its own Player copy; there is no carried-over player state yet (held objects stay behind).

## D-024 — Consequences run on every world change and on area load
- **Status:** Locked
- **Date:** 2026-10-04
- **Decision:** After changing the world state, code calls `WorldSession.Commit()`: it applies the consequence rules, saves once, and raises `WorldSession.Changed`. Each area scene has a `ConsequenceRunner` (runs before other scripts) that registers that scene's `ConsequenceRulebook` with the session and runs the rules once on load (the "world tick"); it saves and notifies only if a rule fired. `Destructible` now calls `Commit()` instead of `Save()`. Reactors subscribe to `Changed` so they update immediately, without a reload. `Changed` is cleared with the rest of the session at the start of each Play session.
- **Reason:** Consequences should be visible as soon as possible ("I changed something, and the world reacts"), and also be correct after a load: a save written before a rule existed (e.g. a P2 save with the bridge already destroyed) still gets its consequences when an area loads.
- **Alternatives considered:** Running rules only on area transitions (DESIGN's world tick); a general event bus; polling the state every frame.
- **Why rejected:** Transition-only delays feedback in the same area, which this prototype is meant to test; an event bus is more infrastructure than one "state changed" event needs; polling wastes work and hides when things change.
- **Consequences / tradeoffs:** Rules run synchronously in the frame of the change (fine for a handful of rules). Delayed consequences and a day counter remain future work (Phase 4). Code that changes state without calling `Commit()` (e.g. P3.1's `CrateTargetPad.Record`) saves but doesn't trigger rules or reactors. That's fine for Bootstrap, which has no rules.

## D-025 — Consequence rules as data: StateCondition + ConsequenceRule in a rulebook asset, each firing once
- **Status:** Locked
- **Date:** 2026-10-04
- **Decision:** A `StateCondition` (required facts, forbidden facts, required destroyed ids, reputation band ranges; all must hold; empty = always) is the one reusable check, used by rules and reactors alike. A `ConsequenceRule` has an id, a description, a condition, and effects (facts to set, reputation changes). Rules live in a `ConsequenceRulebook` ScriptableObject asset, edited in the Inspector. `ConsequenceEngine.Apply` (plain C#, unit-tested) fires every met rule that hasn't fired before, repeating until nothing new fires so rules can chain. Each fired rule is remembered as the fact `rule.<id>`, so it never fires twice, even across saves and reloads. EditMode tests check the rule logic and validate every rulebook asset (unique ids, non-empty conditions, at least one effect) and that every `ConsequenceRunner` has a rulebook.
- **Reason:** DESIGN §10 calls for data-driven rules that content keys off. One shared condition type keeps authoring consistent. Fire-once avoids double reputation penalties every time an area loads.
- **Alternatives considered:** Hard-coded consequence scripts per event; a scripting language or visual graph; storing "already fired" in a separate list.
- **Why rejected:** Per-event scripts don't scale and hide the rules; a language or graph is far too much for a prototype; a fact is already persisted and visible in the debug log, so a separate list adds nothing.
- **Consequences / tradeoffs:** Rule ids must stay stable once shipped, because saves remember them. Conditions only express AND. OR is done with multiple rules or entries. Rules can only add facts and change reputation; removing facts is not supported (facts stay one-way, D-017).

## D-026 — Faction reputation in WorldState v3; reactors (StateGate, ConditionalText); capsule NPCs with text
- **Status:** Locked (reactor display is prototype-scoped)
- **Date:** 2026-10-04
- **Decision:** `WorldState` v3 stores numeric reputation per faction (`Faction` enum: Concord, Delvers, Hollowers — D-008), clamped to −100..100, default 0. Version 1 and 2 files load with neutral reputation. Content checks **bands**, not numbers: Hostile ≤ −40, Unfriendly < −10, Neutral, Friendly ≥ 20 (`ReputationRules`). Two thin reactor components read the session: `StateGate` shows one set of objects when a condition is met and another when it isn't; `ConditionalText` shows the first entry whose condition is met as floating world-space text (barks within a distance, signs always), using Unity's built-in `TextMesh` created at runtime. NPCs in this slice are static capsules with a `ConditionalText`. There is no character art, dialogue, or AI.
- **Reason:** Numeric reputation lets several events combine (e.g. Hollowers +20 for the bridge and −40 for the cache = −20, Unfriendly), while bands keep content simple (DESIGN §10). Generic reactors let one slice show route, NPC, trade, and mission changes without a system per feature. `TextMesh` needs no new package or asset import.
- **Alternatives considered:** Reputation as facts in the fact list; per-feature components (vendor, job board, guard scripts); TextMeshPro or a UI canvas for text.
- **Why rejected:** Facts are one-way strings, while reputation needs to add up; per-feature scripts multiply code for the same "if state, then show" pattern; TextMeshPro needs its essential resources imported and UI/HUD is out of scope for this slice.
- **Consequences / tradeoffs:** Built-in `TextMesh` is low-fidelity and may draw through walls; it will be replaced when real UI or dialogue arrives, while the authored conditions and lines stay. Reputation has no decay or relationships between factions yet (Phase 4).

## D-027 — NPC navigation: Editor-baked NavMesh; routes change by switching NavMesh links with world state
- **Status:** Locked
- **Date:** 2026-10-04
- **Decision:** Each area with moving NPCs has one `NavMeshSurface` (AI Navigation package, already installed), baked in the Unity Editor. Structures whose crossing can disappear (the Span's bridge deck, the bypass gantry) are left out of the bake with a `NavMeshModifier` (ignore from build). Their crossings are `NavMeshLink`s placed under existing `StateGate`s: the bridge link exists only while `p2span.bridge_destroyed` is absent, and the bypass link only while `p2span.bypass_open` is set. Because a link adds itself when enabled and removes itself when disabled, NPCs simply walk whatever path the NavMesh currently allows. There is no route-picking code, no runtime NavMesh rebuild, and no carving. NPCs ask for a fresh path whenever the world state changes.
- **Reason:** It answers the P5 question with DESIGN §14's recommended technique (authored links toggled by state). It reuses the P4 reactors and keeps the world's routes a pure function of saved facts, so a reloaded world routes correctly with no saved NPC data.
- **Alternatives considered:** Rebuilding the NavMesh at runtime after destruction; building it once when the scene loads; per-NPC route lists chosen by conditions; NavMeshObstacle carving.
- **Why rejected:** Runtime rebuilds are what DESIGN rules out; build-on-load blurs the question and costs time per load; condition-picked routes duplicate what the NavMesh already knows; carving is expensive and excluded from this slice.
- **Consequences / tradeoffs:** Each scene must be re-baked in the Editor when its walkable layout changes, **and saved after the bake finishes** (the bake runs in the background; a scene-validation test fails until the saved scene references the baked data). Validated in the P5 play-test (10/10 collapse-and-reroute runs). Link crossings are straight-line moves. Crates are baked as small obstacles where they start, so a moved crate leaves a stale gap and can be walked into. A new rule (`bypass_opens`) was added instead of editing `span_collapse`, because fired rules are remembered in saves (D-025).

## D-028 — NPC behavior: a small plain-C# state machine, disturbance broadcasts, and falling from broken footing
- **Status:** Locked (prototype-scoped)
- **Date:** 2026-10-04
- **Decision:** `NpcBrain` (plain C#, EditMode-tested) picks one state per frame from simple inputs, highest priority first: **Fall** (permanent) > **Stagger** > **Flee** > **Pursue** > **Travel** > **Idle**. `NpcAgent` (MonoBehaviour) gathers the inputs, drives the NavMeshAgent, and acts on state changes. When a `Destructible` breaks it raises a `Disturbance` (position, radius, and the broken object's footprint). NPCs within the radius flee for a few seconds and then return to their route. NPCs standing on the footprint fall: the agent is switched off and the kinematic body becomes a falling physics body. Generic NPCs are never saved (DESIGN §6). On load they start at their scene positions, and which NPCs exist (e.g. Hollower salvagers after the collapse) is decided by `StateGate`.
- **Reason:** DESIGN §10 calls for tiered NPCs with state machines and a cheap disturbance broadcast. Keeping decisions in plain C# makes them testable, and the same brain can grow into Phase 2 enemies.
- **Alternatives considered:** Behaviour trees or a planning AI; per-NPC perception (vision cones, hearing); physics ragdolls; saving NPC positions.
- **Why rejected:** Too much machinery for six NPCs; perception and stealth are out of scope; ragdolls need character rigs; saved positions contradict the save design.
- **Consequences / tradeoffs:** Reactions are radius-based, not line-of-sight. A fallen NPC stays where it landed until the area reloads, then reappears at its start position, which is acceptable for generic NPCs.

## D-029 — Hostile NPCs eject the player; impacts stagger NPCs; no damage
- **Status:** Locked (prototype-scoped)
- **Date:** 2026-10-04
- **Decision:** An `NpcAgent` can have a "hostile when" `StateCondition` (Outpost guards: Concord reputation band Hostile). When it is met and the player comes within the notice radius, the NPC pursues. It keeps chasing until the player is beyond the lose radius, and catching them sends the player to a configured area spawn through `AreaExit.TravelTo`, the same transition exits use (the method was extracted from `AreaExit` without changing exit behavior). Physics impacts use the existing measure (D-019, mass × speed). A hit at or above the stagger threshold, from a non-kinematic object moving at 2 m/s or more and not being carried (`PlayerGrabThrow.CurrentlyHeld`), staggers the NPC: it stops and is shoved back along the NavMesh. There is no health, damage, or death.
- **Reason:** Ejection makes "your choices become the enemy" literal without needing combat, and reuses tested area transitions. Staggering keeps the player's main verb (throwing) meaningful against NPCs. The speed filter stops NPCs from staggering when they walk into crates.
- **Alternatives considered:** Guards dealing damage; guards only shoving the player; ignoring impacts on NPCs.
- **Why rejected:** Damage needs a health system (Phase 2 combat); a shove on a CharacterController needs new player code and is easy to escape without consequence; ignoring impacts makes NPCs feel like walls.
- **Consequences / tradeoffs:** Being caught always means leaving the area. NPCs can't be knocked off ledges (the shove stays on the NavMesh). Phase 2 combat should build health and damage on this stagger and impact measure.

## D-030 — Phase 1 gate review: PASSED
- **Status:** Locked
- **Date:** 2026-10-04
- **Decision:** Phase 1 (prototypes P1–P5) **passes**, and the project goes ahead to Phase 2 unchanged in direction. The next milestone (P6) is chosen separately.
- **Reason:** P1–P5 proved the core technical premise of The Deep, and every planned vertical-slice play-test passed in Unity 6000.3.25f1 with all EditMode tests green:
  - **P1:** physical interaction (move, push, grab and throw, momentum-based impacts) felt good enough to build on.
  - **P3.1, P2:** the player can physically alter the world (authored destruction with support collapse, within a debris budget), and the changes persist by stable id through area transitions, Stop → Play, and full Unity restarts.
  - **P4:** data-driven consequences fire from world state and are visible (route block, reputation, NPC barks, prices, jobs).
  - **P5:** NPCs and navigation react to the changed world with no runtime NavMesh rebuilds: rerouting over the bypass in 10/10 runs, fleeing, falling, staggering, and hostile guards ejecting the player.
- **Alternatives considered:** Simplify (cut a pillar, e.g. destruction or AI); pivot (change the core premise); repeat or extend a prototype before Phase 2.
- **Why rejected:** No prototype failed its question, and no pillar proved too costly at prototype scale.
- **Consequences / tradeoffs:**
  - The prototype shortcuts below are known, not fixed. They are carried into the phases the roadmap already assigns them to:
    - immediate saves, no saved player position or area, in-memory-only save upgrades, no event log or day counter (Phases 3–4)
    - cube-chunk fracture instead of the Blender pipeline
    - legacy floating text, capsule NPCs, no UI
    - no jump or kick
  - P4's "fresh playtester notices 3+ reactions without being told" bar was not tested with an outside player. It remains a useful check, not a gate blocker.
  - Scenes are now maintained in the Unity Editor.
