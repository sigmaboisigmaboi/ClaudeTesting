# The Deep — Prototype Plan & Development Roadmap

See [DESIGN.md](DESIGN.md) for the full game and technical design.

## 15. Prototype plan (do these before committing to the full game)

Built in a `Scenes/Prototypes/` folder with **grey-box geometry** (no art). Each answers a yes/no question.

| Order | Prototype | Question it answers | Kill/pivot signal |
|---|---|---|---|
| **1** | **P1 Physics Combat Sandbox** — FP controller, grab/throw, kick, 2 dumb enemies, explosive barrel, breakable crate | *Is physics combat fun and do I enjoy building it?* | If throwing things at enemies isn't fun after tuning, rethink combat before anything else |
| **2** | **P2 Destruction & Collapse** — Blender pre-fractured wall + catwalk held by 2 support struts + a small bridge; debris cap; rubble swap | *Can we make destruction look good and stay performant with our pipeline?* | If the Blender→Unity fracture pipeline is too slow to author, simplify to fewer, bigger pieces |
| **3** | **P3 Persistence** — two grey-box scenes; destroy things in A, go to B, come back; save, quit, load | *Do stable IDs + delta saves work reliably?* | Must be 100% reliable; this is foundational |
| **4** | **P4 Consequence Chain** — destroy bridge → fact → rule at transition → vendor prices change, NPC bark changes, faction rep drops, alternate route opens, new mission appears | *Does "the world reacts to me" actually feel good?* | If it feels flat, improve *visibility* of consequences before adding more systems |
| **5** | **P5 AI vs Changed World** — enemies path across bridge; bridge falls; they use alternate route; enemies flee falling debris | *Can AI handle authored world changes without runtime NavMesh rebuilds?* | If links/gates are unreliable, restrict which structures affect navigation |

Order rationale: P1 first because **fun** is the biggest risk and you'll learn Unity basics there. P2 and P3 are the biggest **technical** risks. P4 is the soul of the game but is mostly logic (easier to get right with tests). P5 is a known-hard area worth proving before building real levels.

## 16. Development roadmap

Each phase is broken into small milestones (each roughly one or a few sessions). Times are rough part-time guesses.

### Phase 0 — Setup & Foundations (1–2 weeks)
- **Goal:** working Unity project under version control, docs scaffold.
- **Features:** Unity 6 LTS + URP project; Git + LFS + `.gitignore`; folder structure; `CLAUDE.md`, `docs/`; Test Framework with one passing test; Unity command-line test run documented.
- **Dependencies:** none.
- **Test:** project opens; test passes; commit/push works with LFS.
- **What could go wrong:** LFS misconfigured → huge repo; wrong Unity version installed.
- **Done:** clean repo, empty scene runs, one test green, docs in place.

### Phase 1 — Player & Physics Feel (P1) (3–5 weeks)
- **Goal:** fun first-person physics interaction.
- **Features:** FP controller, look, jump, crouch; grab/hold/throw; kick; health/damage; `PersistentId` component created now; basic HUD.
- **Dependencies:** Phase 0.
- **Test:** movement checklist (slopes, stairs, ledges); throw objects of various masses; no clipping through walls while holding objects.
- **What could go wrong:** held objects jittering through walls (classic problem — use physics joint/velocity-based holding, not teleporting); controller feel.
- **Done:** 10 minutes in the sandbox is fun; checklist passes.

### Phase 2 — Combat & Enemies (P1 cont. + P5 basics) (4–6 weeks)
- **Goal:** enemies that make physics combat meaningful.
- **Features:** Health/Damage/Knockback system; melee tool; one ranged weapon; explosives; enemy FSM with NavMesh; Scrapper + Shieldbearer; disturbance events; ragdoll or simple death physics.
- **Dependencies:** Phase 1.
- **Test:** each enemy can be beaten with ≥2 physics approaches; FPS stable with 8 enemies + debris.
- **What could go wrong:** ragdolls are fiddly (fallback: simple death animation + push); AI getting stuck.
- **Done:** a grey-box arena fight that's fun and repeatable.

### Phase 3 — Destruction (P2) (4–6 weeks)
- **Goal:** L1–L3 destruction pipeline.
- **Features:** Blender fracture workflow doc; `Destructible`, `FracturedSwap`, `SupportLink`, `DebrisManager` (cap, sleep, rubble swap); breakable cover; hazards (gas tank, hanging load).
- **Dependencies:** Phases 1–2.
- **Test:** performance under worst case (blow up everything in the room); collapse chains behave predictably; enemies react.
- **What could go wrong:** physics explosions (pieces flying away) from overlapping colliders; frame spikes. Mitigate with piece count limits and pre-instantiation.
- **Done:** a room with breakable walls, a collapsible catwalk, and one bridge; stays above target FPS.

### Phase 4 — Persistence & Zones (P3) (3–5 weeks)
- **Goal:** the world remembers.
- **Features:** WorldState (facts), EventLog, SaveSystem (JSON, versioned), ZoneLoader with diegetic transitions, ZoneExit/RouteGate, rubble-state restore, debug overlay.
- **Dependencies:** Phase 3, `PersistentId` from Phase 1.
- **Test:** EditMode tests for save round-trip and facts; manual: destroy → leave → return → save → quit → load, repeated across zones.
- **What could go wrong:** duplicated/missing IDs (add an editor validation tool that flags duplicates); load-order bugs.
- **Done:** 100% reliable across 20 manual round-trips; tests green.

### Phase 5 — Consequences & Factions (P4 + P5) (5–8 weeks)
- **Goal:** "your choices become the enemy" works end to end.
- **Features:** ConsequenceRule assets + RuleEngine + world tick; infrastructure nodes & settlement conditions; 3 factions with rep, values, relationship table; StateGate, SpawnSet, Barks, vendor stock reacting to facts; scheduled events; NavMeshLink toggling; witness system (simple: were faction NPCs within line of sight?).
- **Dependencies:** Phase 4.
- **Test:** unit tests for rule evaluation and reputation; scripted test scenarios via dev cheats ("destroy bridge", "advance 2 days") with expected outcomes written down.
- **What could go wrong:** rules interacting unexpectedly (mitigate: rules are "once" by default, debug overlay shows which rules fired and why); consequences invisible to player.
- **Done:** both Bridge and Pump chains produce visible, distinct changes in at least 3 systems each, and survive save/load.

### Phase 6 — Vertical Slice / MVP Content (8–12 weeks)
- **Goal:** the 4-zone Upper Works, playable start to finish.
- **Features:** zone blockouts → art pass; missions (hand-authored + 2–3 templates); named NPCs with fact-keyed dialogue; Heavy + Sniper enemies; journal showing major events.
- **Dependencies:** Phases 1–5.
- **Test:** full playthroughs with different choices (protect everything / destroy everything / mixed); outside playtesters if possible.
- **What could go wrong:** content takes far longer than systems (it always does); scope creep. Keep a "later" list.
- **Done:** a stranger can play the MVP for 1–2 hours and notice the world reacting to them.

### Phase 7 — Polish & MVP Release (4–6 weeks)
- **Goal:** shippable MVP/demo.
- **Features:** audio, VFX, UI/menus/settings, tutorialization, performance pass, bug fixing, build pipeline.
- **Done:** builds a standalone Windows exe that runs on a second PC; no blocker bugs.

### Phase 8+ — Toward 1.0
Layer 2 & 3, territory, more factions/nodes, rail travel, endings — each new system gets its own prototype first.

