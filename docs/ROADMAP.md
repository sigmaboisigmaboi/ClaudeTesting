# The Deep — Prototype Plan & Development Roadmap

See [DESIGN.md](DESIGN.md) for the game and technical design and [DECISIONS.md](DECISIONS.md) for locked decisions.

**Guiding rule:** prove *"I changed something, and the world remembers"* as early as possible. Prototypes are small, grey-box, and timeboxed. We do not polish physics combat before the persistent-consequence idea is proven.

Times are rough part-time estimates with big uncertainty. Total ≈ 31–49 weeks (≈ 7–12 months).

Each phase is broken into small milestones (one to a few sessions each), built on a feature branch and merged to `main` by pull request. **No phase or prototype starts until the owner says so.**

## Progress

| Step | Status |
|---|---|
| Planning, locked decisions, repo setup | ✅ Done |
| Phase 0 — Setup | ✅ Done (Unity 6000.3.25f1, folders, EditMode sanity test, Bootstrap scene) |
| Phase 1 — Prototypes P1–P5 | 🔧 In progress — P1.1 ✅ done (first-person movement/look, play-tested in Unity 2026-10-04); P1.2 ✅ done (pushable physics objects, play-tested in Unity 2026-10-04); P3.1 ✅ done (persistence spike, pulled forward; play-tested in Unity 2026-10-04); P1.3 ✅ done (grab, hold, drop and throw; play-tested in Unity 2026-10-04); P1.4 ✅ done (knockback dummy, play-tested in Unity 2026-10-04). **P1 complete.** ✅ P2 — Destruction Vertical Slice done (play-tested in Unity 2026-10-04). ✅ P4 — Consequences Vertical Slice done (play-tested in Unity 2026-10-04). Next milestone not chosen yet |
| Phases 2–6 | — |

---

## Phase 0 — Setup (1–2 weeks)
- **Goal:** a working Unity project under version control.
- **Features:** install Unity 6 LTS (URP template) via Unity Hub; create the project and move `Assets/`, `Packages/`, `ProjectSettings/` into the repo root; `git lfs install`; confirm Unity defaults (Visible Meta Files, Force Text serialization); create `Assets/_Project/` with `Scenes/`, `Scripts/` and `Tests/EditMode/` (+ test assembly definition; the runtime assembly definition is added with the first real script in P1, because Unity warns about an assembly definition with no scripts); one trivial passing EditMode test; record the exact Unity version in DECISIONS.md.
- **Dependencies:** none.
- **Test:** project opens cleanly; empty scene plays; the test passes in the Test Runner; `git status` shows no `Library/` or `Temp/` files; commit/push works.
- **What could go wrong:** wrong Unity version/template; `Library/` committed; LFS not installed so binaries go into normal Git.
- **Done:** clean project on `main` via PR, one test green, docs updated.

## Phase 1 — Prototype track (6–10 weeks total)

Grey-box only. P1 uses the existing Bootstrap test room; later prototypes may get their own scenes under `Assets/_Project/Scenes/Prototypes/` if they need a separate setup. Each prototype answers **one** question and is timeboxed — if it runs over, we stop and decide (simplify, pivot, or extend) rather than drifting.

**Prototype code policy:** pure-logic code (facts, rules, save data) is written to be kept and hardened later. Scene/feel code may be rewritten in later phases — but any rewrite is proposed first, never done silently.

### P1 — Physics (~1 week, intentionally small)
- **Question:** *Is the physics interaction good enough to build on?*
- **Features:** first-person move/look/jump; grab, hold, and throw objects; push/kick; one breakable crate (simple swap to pieces); one knockback dummy that takes damage. **No enemy AI, no weapons, no polish.**
- **Dependencies:** Phase 0.
- **Test:** checklist — walk slopes/stairs/ledges; throw light and heavy objects; held objects don't clip through walls or launch the player; crate breaks; dummy reacts to hits.
- **What could go wrong:** held objects jitter or pass through walls (use velocity-based holding, not teleporting); controller feel eats time — don't tune beyond "good enough".
- **Done:** the checklist passes and the interaction feels promising. Not polished.
- **Progress:**
  - ✅ **P1.1 — First-person player foundation** (2026-10-04): `FirstPersonController` (CharacterController) with WASD movement, mouse look with vertical clamp, gravity/grounding, and collision, in a small Bootstrap test room. Play-tested in Unity 6000.3.25f1: spawn on floor, movement, look and clamp, gravity, wall and block collision, cursor lock/release all passed. Jump deliberately deferred to a separate small step.
  - ✅ **P1.2 — Pushable physics objects** (2026-10-04): `PlayerPushRigidbodies` (separate component, mass-aware horizontal pushes) and three crates in the Bootstrap room — light 5 kg, medium 25 kg, heavy 150 kg. Play-tested in Unity 6000.3.25f1: light/medium/heavy crates each behaved as intended, crate–environment and crate–crate collisions worked, no tunneling or instability, P1.1 movement unchanged, Console clean, EditMode tests passed — and pushing felt good.
  - ✅ **P1.3 — Grab, Hold, Drop and Throw** (2026-10-04): `PlayerGrabThrow` (separate component). E picks up / drops, left mouse throws; velocity-based holding with gravity off and player collision ignored while held; mass limit 30 kg (light and medium crates grabbable, heavy not); mass-aware throw; auto-drop if snagged. Play-tested in Unity 6000.3.25f1: light crate grab/carry/drop/throw and medium crate grab/carry/throw worked; heavy crate could not be grabbed but could still be pushed; crate–crate physics worked; carrying the medium crate onto the P3.1 pad recorded the fact, and stop/Play persistence and Delete Saved World State still worked; P1.1 and P1.2 behavior unchanged; EditMode tests passed; Console clean.
  - ✅ **P1.4 — Knockback Dummy (impact test target, no health)** (2026-10-04): a 60 kg upright capsule (`KnockbackDummy`, X/Z rotation locked) with an `ImpactMeter` component. Impacts from physics objects are measured as incoming mass × impact speed, logged to the Console, and turned into a horizontal knockback shove (capped). No health, damage, or combat state. Play-tested in Unity 6000.3.25f1 and working; P1.1–P1.3 and the P3.1 pad unchanged.
  - ⏳ **Not planned as further P1 steps:** kick (low new value; revisit with Phase 2 melee) and the breakable crate (belongs to P2 destruction). Persistence was pulled forward first (see P3.1).

### P2 — Destruction (1–2 weeks)
- **Question:** *Can our Blender → Unity pre-fractured pipeline look good and stay performant?*
- **Features:** a wall fractured in Blender (Cell Fracture) that swaps to physics pieces; a catwalk held up by 2 support struts (break both → catwalk falls); a small bridge collapse; debris cap; settle-then-swap to static rubble; **`PersistentId` component introduced** with a duplicate-ID check.
- **Dependencies:** P1.
- **Test:** blow up everything in the room — frame rate stays at target; collapses behave predictably; no pieces flying off from overlapping colliders.
- **What could go wrong:** the fracture/export workflow is too slow to author → fewer, bigger pieces; physics spikes → pre-instantiate pieces, limit counts.
- **Done:** wall, catwalk, and bridge all break reliably within the debris budget; workflow written down.
- **Progress:**
  - ✅ **P2 — Destruction Vertical Slice** (2026-10-04, approved as one large milestone): built in new scenes `Scenes/Prototypes/P2_Span` and `P2_Annex`; Bootstrap untouched. Thrown/pushed crates damage destructible infrastructure (`Destructible`: integrity, intact → pre-placed cube-chunk pieces → static rubble); a breakable alcove wall, two support posts, and a bridge deck that collapses when both posts are gone (records `p2span.bridge_destroyed`). Debris freezes in place after settling, under a 40-piece budget. Destroyed `PersistentId`s are saved in `WorldState` v2 through one shared `WorldSession`; leaving to the Annex and returning, stopping/restarting Play, or restarting Unity shows the world still destroyed. Dev reset via the `_Dev` object's Delete Saved World State. Held crates deal no damage. Cube-chunk fracture stands in for the Blender pipeline (an optional later content step). Play-tested in Unity 6000.3.25f1: wall destruction, both support posts destroyed, bridge collapse after both supports, debris settling, persistence through Stop → Play, a full Unity restart, and P2_Span → P2_Annex → P2_Span, world-state reset restoring the wall and bridge, all EditMode tests green, Bootstrap/P1 unchanged.
  - ⏳ **Still open from the original P2 question:** the Blender → Unity fracture pipeline (optional content step; code needs no change).

### P3 — Persistence (1–2 weeks)
- **Question:** *Do stable IDs + delta saves reliably remember destruction?*
- **Features:** two grey-box areas with an area transition; `WorldState` (facts); JSON save/load of destroyed IDs and facts; on load, destroyed objects restore to their rubble state.
- **Dependencies:** P2 (`PersistentId`).
- **Test:** EditMode tests for facts and save round-trip; manual: destroy → leave → return → save → quit → load, repeated many times.
- **What could go wrong:** duplicate/missing IDs; load-order bugs (objects reading state before it's loaded).
- **Done:** 100% reliable across 20 manual round-trips; tests green.
- **Pulled into P2:** `PersistentId`, the shared `WorldSession`, `WorldState` v2 (destroyed ids), and a minimal area transition (`AreaExit`/`AreaSpawnPoint`, P2_Span ↔ P2_Annex). P3 proper still owns save timing (transitions/checkpoints vs. immediate), save-format versioning beyond v2, and the 20-round-trip reliability bar.
- **Progress:**
  - ✅ **P3.1 — Persistence Spike (pulled forward)** (2026-10-04): done early, before P1's remaining items and P2, to test the core identity ("I changed something, and the world remembers") as soon as possible. One remembered fact, `bootstrap.crate_on_pad`: pushing `Crate_Medium` onto the corner pad records it, `WorldState` saves it as JSON immediately, and on the next start the crate is placed on the pad (authored "after" state). No `PersistentId`, second scene, or save system yet — those remain P3 proper (D-017). Play-tested in Unity 6000.3.25f1: the fact was recorded and saved; the crate returned to the pad after stop/Play, after being pushed off, and after a full Unity restart; Delete Saved World State reset it to its original authored position; all EditMode tests passed.

### P4 — Consequences (~2 weeks) — the identity test
- **Question:** *Does "the world remembers" actually feel good?*
- **Features:** the full bridge chain end to end: destroy bridge → event recorded → fact set → world tick on area transition → data-driven rules → **route change** (crossing blocked, bypass opens) + **NPC reaction** (bark changes) + **faction reputation change** + **trade change** (vendor price/stock) + **mission availability change**. Simple debug overlay showing facts, rep, and which rules fired. Dev cheats ("destroy bridge", "advance day").
- **Dependencies:** P3.
- **Test:** EditMode tests for rule evaluation and reputation; scripted scenario with written expected outcomes; save/load in the middle of the chain.
- **What could go wrong:** consequences feel flat or invisible → improve *visibility* (signage, barks, journal) before adding systems; rules interacting unexpectedly → rules fire once by default and the overlay shows why.
- **Done:** a playtester who destroys the bridge notices at least 3 different reactions without being told.
- **Progress:**
  - ✅ **P4 — Consequences Vertical Slice** (2026-10-04, approved as one large milestone): named P4 because it answers this prototype's question, and built on top of P2. Destroying the Span bridge or breaking into the Hollower cache (the alcove wall) now has consequences elsewhere. `WorldSession.Commit()` applies data-driven **consequence rules** (`ConsequenceRulebook` asset `Data/Consequences/P4_Rulebook`) after every world change, and each area's `ConsequenceRunner` runs them on load (the "world tick"). Rules fire once, set new facts, and change **faction reputation** (`WorldState` v3, numeric −100..100 shown as bands Hostile/Unfriendly/Neutral/Friendly). Reactors update live: `StateGate` (show/hide objects) and `ConditionalText` (barks, signs and boards as floating text). A new area `Scenes/Prototypes/P3_Outpost` (Concord outpost, reached by a second doorway in P2_Span) shows the reactions: a guard who blocks the storeroom when the Concord is Hostile (**route change**), NPC barks keyed to facts and reputation bands (**NPC reaction**, **reputation**), a price board with rationing prices (**trade change**), a job board whose job changes (**mission availability**), and a notice board. P2_Span gets signs and a Concord Warden who react too. NPCs are plain capsules with text labels. Dev shortcuts on `_Dev` → WorldStateDebug: Record Bridge Destroyed / Record Cache Looted / Delete Saved World State; the Console logs facts, reputation, and each rule that fires. Bootstrap and P2_Annex unchanged. Play-tested in Unity 6000.3.25f1: fresh/neutral Outpost; support-post destruction and bridge collapse; Warden and sign reactions; Outpost guard, reputation, trade and job reactions; cache-wall consequence; persistence through Stop → Play and a full Unity restart; save reset; all EditMode tests green.
  - **Deliberately not in this slice:** dialogue trees, real AI/navigation (P5), combat, quests, an economy, UI/HUD, delayed consequences and the day counter, audio, VFX, localization, alternate bypass route.

### P5 — AI (1–2 weeks)
- **Question:** *Can AI handle authored world changes without runtime NavMesh rebuilds?*
- **Features:** NavMesh agents patrol across the bridge; after it collapses they take the alternate route via NavMeshLink toggled by world state; agents flee from a "disturbance" (explosion/collapse).
- **Dependencies:** P2, P4.
- **Test:** agents never path onto the destroyed bridge; rerouting works after save/load; fleeing triggers in radius.
- **What could go wrong:** links/gates unreliable → restrict which structures affect navigation.
- **Done:** agents adapt correctly in 10 out of 10 test runs.

### Gate review (end of Phase 1)
Review what each prototype answered. Decide: **go**, **simplify**, or **pivot**. Record the outcome in DECISIONS.md before starting Phase 2.

---

## Phase 2 — Core gameplay (3–5 weeks)
- **Goal:** production-quality player and basic combat.
- **Features:** player controller hardened from P1; Health/Damage/Knockback; one melee tool; one ranged weapon; throwable explosives; enemy state machine (Idle/Patrol/Alert/Combat/Flee) on NavMesh; **Scrapper** and **Shieldbearer** enemies; simple death physics.
- **Dependencies:** Phase 1 gate.
- **Test:** each enemy can be beaten with ≥2 physics approaches; stable frame rate with 8 enemies + debris.
- **What could go wrong:** ragdolls are fiddly (fallback: simple death push); AI gets stuck.
- **Done:** a grey-box arena fight that's fun and repeatable.

## Phase 3 — Persistent world (4–6 weeks)
- **Goal:** production destruction and persistence across real areas.
- **Features:** destruction components hardened from P2 (`Destructible`, `FracturedSwap`, `SupportLink`, `DebrisManager`); breakable cover; hazards (gas tank, hanging load); persistence hardened from P3 with save versioning/migration; area loader with diegetic transitions; exits gated by facts; editor tool flagging duplicate IDs.
- **Dependencies:** Phase 2.
- **Test:** worst-case destruction performance; save/load round-trips across all areas; old save loads after a format change.
- **What could go wrong:** lighting after destruction looks wrong (see DESIGN §14); load-order bugs.
- **Done:** destruction and persistence are reliable in production areas; tests green.

## Phase 4 — Consequences, factions & NPC reactions (5–8 weeks)
- **Goal:** "your choices become the enemy" works for both MVP nodes.
- **Features:** rule engine hardened from P4; world tick and day counter; infrastructure nodes (Bridge, Pump) → resources → settlement condition; 3 factions with reputation, values, relationship table, and area-control facts; tiered NPC reactions (T0 flee, T1 react to rep bands, T2 named dialogue keyed by facts); StateGate, SpawnSet, Barks, vendor stock reacting to facts; scheduled (delayed) events; mission availability from world state.
- **Dependencies:** Phase 3.
- **Test:** unit tests for rules, reputation, settlement conditions; scripted scenarios ("destroy bridge", "destroy pump", "destroy both", "protect both") with expected outcomes written down; all survive save/load.
- **What could go wrong:** unexpected rule interactions; consequences invisible to the player.
- **Done:** Bridge and Pump chains each produce visible, distinct changes in at least 3 systems, and survive save/load.

## Phase 5 — MVP content (8–12 weeks)
- **Goal:** the 4-area Upper Works, playable start to finish.
- **Features:** area blockouts → modular-kit art pass (industrial brutalism); ~6–10 missions (hand-authored + 2–3 templates); named NPCs; **Heavy** and **Sniper** enemies; journal of major events.
- **Dependencies:** Phases 2–4.
- **Test:** full playthroughs with different choices (protect everything / destroy everything / mixed); outside playtesters if possible.
- **What could go wrong:** content takes far longer than systems; scope creep — keep a "later" list.
- **Done:** a stranger can play for 1–2 hours and notices the world reacting to them.

## Phase 6 — Polish & MVP release (4–6 weeks)
- **Goal:** a shippable MVP/demo.
- **Features:** audio, VFX, UI/menus/settings, tutorialization, performance pass, bug fixing, Windows build.
- **Done:** a standalone Windows build runs on a second PC with no blocker bugs.

## Phase 7+ — Toward 1.0
Layers 2 and 3, contested territory, more infrastructure nodes, rail travel, endings. Any new major faction needs a decision entry. Each new system gets its own prototype first.
