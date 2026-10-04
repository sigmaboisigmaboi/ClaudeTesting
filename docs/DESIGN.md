# The Deep — Game Design, Technical Design & Development Roadmap

## Context

"The Deep" is a new, original game. The repo (`sigmaboisigmaboi/ClaudeTesting`, branch `claude/funny-lamport-y8jchs`) is currently **empty** — no commits, no code — so there is nothing existing to reuse or preserve. This document is the planning deliverable requested: no code is written in this phase. Its job is to turn an ambitious concept into something a **beginner working with AI assistance** can actually ship, by being honest about difficulty, cutting scope aggressively for the first version, and proving the risky technology with small prototypes first.

### Assumptions I made (please confirm or correct during review)

| # | Assumption | Why it matters |
|---|---|---|
| A1 | **3D, single-player, PC (Windows)** | Multiplayer + physics + persistent destruction is an order of magnitude harder (networked physics is a famously unsolved-for-indies problem). |
| A2 | **First-person camera** for MVP | Removes the single biggest beginner time-sink: character animation (rigging, blending, IK, third-person camera collision in tight tunnels). Physics grab/throw also *feels* better in first person (Half-Life 2, Prey, Amid Evil). Third-person can be revisited later but would be a big change. |
| A3 | **Stylized/low-poly art**, modeled in Blender | Realistic art would dominate the schedule. Stylized also hides imperfect destruction. |
| A4 | Solo developer, part-time, with Claude Code | Drives every scope decision below. |
| A5 | Hand-built levels, not procedural | See "Procedural generation" below. |

---

## Honest difficulty assessment

| Feature | Difficulty | Notes |
|---|---|---|
| First-person movement + camera | Easy | Many references; Unity has a starter controller. |
| Pick up / throw / push objects | Easy–Moderate | Getting it to *feel good* takes tuning. |
| Explosions with knockback | Easy | `AddExplosionForce` is built in. |
| Breakable props swapped for pre-broken pieces | Easy–Moderate | Fracture in Blender, swap at runtime. |
| Remembering what was destroyed across scenes/saves | Moderate | Needs stable object IDs — very doable if designed in from day 1, painful if retrofitted. |
| World facts + data-driven consequence rules | Moderate | Core of the game; conceptually simple, needs discipline. |
| Faction reputation + reactions | Moderate | |
| Basic enemy AI (patrol, chase, attack, flee) | Moderate | Unity NavMesh + simple state machines. |
| Structural collapse (remove supports → thing falls) | Difficult | Feasible if *authored* (designer marks supports). Not feasible as true structural engineering simulation. |
| AI pathing around changed geometry | Difficult | NavMesh must update when bridges fall. Solvable with authored "links" toggled by state. |
| Lighting that survives destruction | Difficult | Baked lighting breaks when walls disappear. Needs care. |
| Large interconnected world with seamless streaming | Difficult–Extremely difficult | Avoid; use loading transitions instead. |
| Full off-screen simulation of economy, population, NPC lives | Extremely difficult | Replace with event-driven abstraction (below). |
| Runtime fracture of *anything* (voxels/procedural cutting) | Extremely difficult | Teardown-style tech took a specialist years. Do not attempt. |
| Every building fully destructible with real physics | **Unrealistic for indie** | Performance + level-design nightmare (players delete your level). |
| Every NPC with schedules, jobs, memory, relationships | **Unrealistic for MVP** | Use tiered NPCs. |
| Multiplayer | **Unrealistic for this project** | Not planned. |
| Procedurally generated civilization | **Unrealistic as the core** | Fights against "world remembers authored places." Maybe small procedural caves much later. |

---

## 1. Engine comparison & recommendation

| Criterion | Unity 6 (C#) | Unreal 5 (C++/Blueprints) | Godot 4 (GDScript/C#) |
|---|---|---|---|
| Physics | PhysX, mature, well-documented | Chaos, excellent | Jolt (4.4+), good, younger |
| Destruction | Pre-fractured meshes easy; paid plugins (RayFire) | **Best built-in** (Chaos Destruction / Geometry Collections) | Pre-fractured only, fewer tools |
| Large environments | Additive scene loading, good | Best (World Partition) | Adequate, less tooling |
| AI | NavMesh (AI Navigation package), huge tutorial base | Behavior Trees, EQS — powerful | Basic NavigationServer |
| Procedural gen | Fine | Fine (PCG framework) | Fine |
| Tooling/editor | Very good, ScriptableObjects for data | Excellent but heavy | Lightweight, fast |
| Performance | Good | Best ceiling, heavy baseline | Good for small/medium 3D |
| Beginner ease | **Good** — C# is readable, most tutorials exist | Hard — C++ or Blueprints (visual) | Very good |
| Claude Code compatibility | **Good** — logic lives in plain `.cs` files Claude can read/write/test | **Poor** for Blueprints (binary visual graphs Claude can't edit); C++ is heavy for a beginner | **Very good** — even scenes are text |
| Plugins/assets | **Largest ecosystem** (Asset Store) | Large (Fab) | Smaller |
| Learning curve | Moderate | Steep | Gentle |
| Your interest | ✅ listed | — | — |

**Recommendation: Unity 6 LTS with URP (Universal Render Pipeline).**

Why:
- C# is beginner-friendly and the code lives in plain text files — the best fit for working with Claude Code, which can write, read, and unit-test that code.
- The largest library of tutorials/answers in existence for exactly the things we need (character controllers, rigidbodies, NavMesh, save systems).
- Pre-fractured destruction (our chosen technique) works fine in Unity — we don't need Unreal's Chaos.
- You already want to learn Unity, and the skills transfer.
- Honest tradeoff: Unreal has better destruction and large-world tech out of the box, but Blueprints are invisible to Claude Code and C++ is a rough first language. Godot is the runner-up (text scenes are very AI-friendly) but has a thinner 3D ecosystem for a physics/destruction-heavy game.

**Known Unity limitation for our workflow:** Claude Code cannot see the editor or play the game. Scenes and prefabs must be set up by you in the editor, guided step by step. Claude can write scripts, data, tests, and docs — and can run EditMode tests via Unity's command line if we set that up.

## 2. Recommended tech stack

| Area | Choice | Reason |
|---|---|---|
| Engine | Unity 6 LTS | Above |
| Render pipeline | URP | Lighter, simpler than HDRP; good for dark scenes with stylized art |
| Language | C# | Unity standard |
| Navigation | `com.unity.ai.navigation` (official) | NavMesh surfaces, NavMeshLinks for bridges |
| Input | New Input System (official) | Rebinding, gamepad support later |
| Save format | JSON via `com.unity.nuget.newtonsoft-json` (official Unity package) | Unity's built-in `JsonUtility` can't save dictionaries; Newtonsoft is Unity-supported, not a random dependency |
| Data/config | ScriptableObjects | Designer-editable data assets in the editor |
| Tests | Unity Test Framework (built in), EditMode tests for logic | |
| 3D modeling + fracture | Blender (free) with built-in **Cell Fracture** add-on | Pre-fracture walls/bridges/props |
| Version control | Git + **Git LFS** for binaries, Unity `.gitignore` | Unity projects are large; LFS keeps the repo healthy |
| Editor | VS Code (C# Dev Kit + Unity extension) | Your main editor |
| Paid plugins | **None for now.** Revisit RayFire only if the prototype proves we need it | Avoid dependencies until needed |

## 3. MVP definition

**One-sentence MVP:** *A single underground district of ~4 connected zones where the player can fight with physics, break key structures, and see that destruction permanently change routes, a faction's attitude, NPC dialogue, and the next mission — and all of it survives save/load.*

MVP content:
- **Setting:** "The Upper Works" — the shallowest layer.
- **4 zones** (separate Unity scenes):
  1. **Concord Market** (hub town, vendors, faction HQ)
  2. **The Span** (a chasm crossed by a destructible bridge — the trade route)
  3. **Pumpworks** (a water facility that can be sabotaged or defended)
  4. **Old Shaft** (mine/ruin combat area, alternate route that opens if the bridge falls)
- **3 factions:** Concord (authority), Delvers' Union (miners), Hollowers (scavengers).
- **2 infrastructure nodes** with persistent states: the Bridge and the Pump.
- **3–4 enemy types**, 1 melee tool, 1 ranged weapon, explosives, grab/throw.
- **~6–10 missions** (some generated from templates by world state).
- **Save/load** at checkpoints and zone transitions.
- **The consequence chain must work end to end** for both nodes. That is the whole point of the MVP.

What the MVP proves: "The world reacts to me" is fun, technically sound, and buildable.

Rough time estimate (honest, with big uncertainty): **6–12 months part-time** for a beginner with AI help, assuming scope holds. Scope creep is the main threat, not the tech.

### Version 1.0
- 3 depth layers (Upper Works, The Sump/industrial + farms, The Ruins), ~12–18 zones
- 4–5 factions (adds Garrison/military and the Deep-dwellers at the bottom)
- 6–10 infrastructure nodes (rail depot, power plant, farm caverns, dam, gates)
- Territory: zones can change controlling faction based on events
- Delayed consequences ("3 days later, refugees arrive at the Market")
- Named NPCs who reference past events
- Rail system as fast travel between hubs
- 8–12 enemy types, ~8 weapons/tools, more chain-reaction hazards
- Endings shaped by world state
- Polish: audio, UI, settings, accessibility, optimization

### Future expansion (post-1.0)
- Deeper layers: underground forest, underground ocean, "the Deep" proper
- Ancient automated machines faction
- Vehicles (mine carts, drilling rigs)
- Richer economy simulation, trade caravans as physical entities
- NPC schedules for named characters
- Small procedural cave sections between authored zones
- New Game+ / alternate starts
- Modding via data files

## 4. Core game loop

```
        ┌────────────────────────────────────────────┐
        ▼                                            │
  Enter a zone ──► Read the situation (who's here,   │
                   what's at stake, what's breakable)│
        │                                            │
        ▼                                            │
  Choose an approach: fight / sneak / sabotage /     │
  bargain / use the environment                      │
        │                                            │
        ▼                                            │
  Physics combat & destruction                       │
  (throw, collapse, blow up, knock off ledges)       │
        │                                            │
        ▼                                            │
  Outcome recorded as WORLD FACTS                    │
  (bridge_destroyed, pump_sabotaged, rep changes)    │
        │                                            │
        ▼                                            │
  Leave / rest / travel  ◄── consequences resolve    │
  here ("world tick")                                │
        │                                            │
        ▼                                            │
  Return to a CHANGED world: new routes, new         │
  prices, new enemies, new missions, NPCs talk ──────┘
        │
        ▼
  Unlock / be pushed toward the next depth layer
```

Changes from your draft:
- **"Read the situation" step:** consequences only feel meaningful if the player can *see* what's at stake *before* acting (e.g., "this bridge carries all water to the Market"). Informed choices > surprise punishment.
- **Consequences resolve at transitions ("world tick")**, not continuously — this is the technical trick that makes the whole game feasible (see World Simulation).
- **Multiple approaches** to each situation, so destruction is a choice, not the only tool.

Why it's fun:
1. **Short loop** (combat, seconds): physics makes every fight a little different — improvising with the room is satisfying.
2. **Medium loop** (a mission, minutes): choosing *how* to solve a problem, with visible stakes.
3. **Long loop** (hours): returning to a place you changed and seeing the ripple. This is the hook — "my choices did that." It also creates *emergent stories* players retell.
4. **Tension:** destruction is powerful in the moment but costs you later. Power with a price is a classic recipe for interesting decisions.

## 5. World design

- **Structure: semi-open "connected hubs"** (think Metro Exodus / Dishonored / Metroid-style regions), **not** open world. Each zone is a hand-built Unity scene; zones connect via tunnels, elevators, and rail.
- **Size:** MVP 4 zones, each ~5–15 minutes to traverse. 1.0: ~15 zones across 3 layers.
- **Connectivity:** a graph. Each zone has named **exits**; each exit can be open/blocked depending on world facts (bridge destroyed → Span east exit blocked; Old Shaft bypass opens).
- **Loading:** zone transitions are **diegetic loading moments** (elevator ride, long tunnel, rail car). Load the next scene, unload the previous. No seamless streaming — it's a huge complexity jump with little gameplay value in a cave world where tunnels naturally hide transitions.
- **Depth = danger:** each layer deeper adds stranger enemies, fewer friendly factions, harsher hazards. Gates between layers are story/world-state gates.

### What persists when the player leaves a zone
| Persists (saved) | Resets / simulated away |
|---|---|
| Level 2–4 destruction (gameplay objects, structures, major events) | Level 1 small debris, loose props, bodies |
| Dead **named/important** NPCs | Generic enemies (respawn based on zone state rules) |
| Looted containers, opened doors, discovered locations | Projectiles, particles, physics positions of most props |
| World facts & faction state | Ambient NPC positions |

### How we avoid simulating the whole civilization
Only the **current zone** runs real physics, AI, and NPCs. Everything else exists only as **data** (world facts, faction numbers, node states). When the player arrives somewhere, the zone **reads the data and configures itself** to match. The world "moves on" only at defined moments (zone transitions, resting, mission completion). This is the same approach used by Fallout/Elder Scrolls cells and most immersive sims.

## 6. Destruction system

### Destruction levels
| Level | What | Technique | Persisted? | MVP? |
|---|---|---|---|---|
| **L1 — Props** | Bottles, crates, chairs, barrels | Rigidbody props; break = swap to pre-broken pieces; debris fades | No | ✅ |
| **L2 — Gameplay objects** | Doors, explosive tanks, support struts, cover, gates, valves, lamps | Pre-fractured prefab + health; break triggers gameplay (explode, open path, drop thing) | **Yes** (by ID) | ✅ |
| **L3 — Structural** | Walls marked breakable, catwalks, small bridges, pillars | Pre-fractured pieces + **authored support links** ("this catwalk depends on these 2 struts") — break supports → dependent piece collapses | **Yes** | ✅ (a handful, hand-placed) |
| **L4 — Major environmental** | The Bridge, tunnel collapse, Pump failure, flooding | **Authored set piece**: trigger condition → play collapse (physics pieces and/or animation) → swap scene to a permanent "after" state | **Yes** (as world fact) | ✅ (2 events: Bridge, Pump) |

### Illusion techniques (the important part)
1. **Pre-fractured meshes from Blender.** Model intact wall → Cell Fracture → export pieces. At runtime: hide intact mesh, enable pieces as rigidbodies. Cheap, looks great.
2. **Swap to "rubble" state.** After debris settles (or after N seconds), replace physics pieces with a single **static rubble mesh**. Saves performance and is what we persist.
3. **Authored support graph, not simulation.** Designers (you) mark what holds what up. It looks like structural physics; it's a simple list.
4. **Set pieces for big moments.** The Bridge collapse can be a mix of physics pieces and animation. Players remember the moment, not the accuracy.
5. **Most of the level is not destructible** — natural rock, main tunnel walls. Clearly communicate breakable things via **visual language** (cracked material, wooden supports, rusted metal, warning paint). Players learn the rules and feel powerful within them.
6. **Particles, dust, camera shake, sound** sell destruction more than physics accuracy does.
7. **Debris limits:** max active debris pieces; oldest pieces freeze or fade.

### Not in MVP
Runtime cutting, voxel terrain, digging anywhere, destructible every-building, fluids simulation (flooding is a rising animated water plane, not a fluid sim).

## 7. Consequence system — "Your choices become the enemy"

### Architecture

```
Player action (break bridge, kill leader, help miners)
      │
      ▼
GameEvent  { type: "StructureDestroyed", id: "span_bridge", zone, time, causedByPlayer }
      │
      ▼
WorldState.SetFact("span_bridge.state", "destroyed")   + append to EventLog
      │
      ▼  (at world tick: zone transition / rest / mission end)
RuleEngine evaluates ConsequenceRules (data assets)
      │
      ├──► Faction changes   (Concord rep −30, Delvers rep +10)
      ├──► Resource changes  (Market.trade −50)
      ├──► Scheduled events  (in 2 days: "price_spike")
      ├──► Mission generation (template "Escort supplies through Old Shaft")
      └──► New facts          ("concord.hunting_player" = true)
      │
      ▼
When a zone loads, components READ facts and configure the scene:
  StateGate (show/hide objects), RouteGate (exits & NavMeshLinks),
  SpawnSet (which enemies/NPCs), Barks (what NPCs say), Vendor stock
```

### What is a meaningful event?
An event is meaningful if **at least one rule or piece of content reacts to it.** Concretely, only **tagged** objects/NPCs emit persistent events: L2+ destruction with an ID, named NPC deaths, mission outcomes, infrastructure node state changes, faction-relevant crimes (witnessed). Breaking a bottle is not an event.

### How events/facts are stored
- **Facts:** a flat dictionary `string → value` (bool/int/float/string). E.g. `span_bridge.state = "destroyed"`, `faction.concord.rep = -20`, `pump.output = 0`, `npc.foreman_hale.dead = true`. Simple to save, debug, and query.
- **Event log:** an append-only list of what happened (type, id, day). Used for NPC dialogue ("You're the one who dropped the Span") and for history/journal. Capped/summarized so it doesn't grow forever.

### How consequences are triggered
**Data-driven rules** (ScriptableObjects), not code per consequence:
```
Rule: "Bridge loss hurts trade"
  When:  span_bridge.state == destroyed
  Once:  true
  Then:  market.trade -= 50
         faction.concord.rep -= 30   (only if player_caused)
         schedule "price_spike" in 1 day
         set route.span_east = blocked
         set route.old_shaft_bypass = open
```

### How we prevent an impossible number of outcomes
1. **Infrastructure nodes, not arbitrary objects.** Big consequences only flow from a **small, fixed set of nodes** (Bridge, Pump; later Rail Depot, Power Plant…). Each node has 3 states: **Intact / Damaged / Destroyed**.
2. **Nodes produce resources; settlements consume them.** The Pump makes *water*; the Bridge carries *trade*. Settlements have a **need level** per resource. Low supply → **Settlement Condition** drops (Stable → Strained → Crisis → Abandoned). This is a tiny systemic graph: any node failing automatically affects the right places — no handcrafted branch per combination.
3. **Content keys off *bands*, not exact histories.** NPC dialogue, vendor stock, enemy spawns, and missions check "Market is in Crisis" or "Concord rep is Hostile" — not "player destroyed bridge on day 3 and then…". Many histories collapse into a few states.
4. **Faction reactions are systemic:** each faction has values (what it cares about). Damage to something they value → rep penalty; damage to an enemy's asset → rep bonus. One rule covers many situations.
5. **Hand-author only the highlights:** a few bespoke reactions for the biggest events (the Bridge falling gets a unique NPC speech). Systemic rules cover everything else.

This gives a dynamic feel from **combinations of a few simple systems**, which is how emergent games actually work.

### Example: destroy the Pump
Pump → Destroyed ⇒ `water` supply to Market drops ⇒ Market condition → Strained (next tick) → Crisis (2 ticks later) ⇒ Content reacting to *Market: Crisis*: ration lines, higher prices, Concord patrols increase, Hollowers raid water caravans (new enemy spawn set), mission template "Repair the Pump" or "Escort water" appears, refugees move to Old Shaft (SpawnSet change there), NPC barks blame "the saboteur". Concord rep drops only if the player was **seen** or evidence links them.

## 8. Faction system

| Element | MVP | 1.0 | Later |
|---|---|---|---|
| Reputation (player ↔ faction, −100..100, banded: Hostile/Unfriendly/Neutral/Friendly/Allied) | ✅ | ✅ | |
| Faction values (what each faction cares about → automatic rep reactions) | ✅ | ✅ | |
| Faction ↔ faction relationships | ✅ fixed table | ✅ changes via events | |
| Resources (abstract numbers: water, trade, ore, scrap) | ✅ via nodes | ✅ | Economy sim |
| Territory (zone owner) | ❌ | ✅ | Dynamic wars |
| Goals (faction "wants" that generate missions) | ❌ hand-authored missions | ✅ simple goal list | Planning AI |
| Alliances/conflicts that shift | ❌ | ✅ rule-driven | |

Proposed factions (MVP in **bold**):
- **The Concord** — city authority; controls water and trade. Values: order, infrastructure.
- **Delvers' Union** — miners; exploited by the Concord. Values: workers, mines; hates Concord taxes.
- **The Hollowers** — scavengers living in abandoned works; thrive on collapse. Values: salvage; *benefit* when infrastructure fails (great for "your choices become the enemy" — your destruction makes them stronger).
- *Garrison* (1.0) — military; arrives if Concord is in crisis.
- *The Deep-dwellers / Pale* (1.0) — something below, drawn upward by noise and collapse.
- *The Old Machines* (later) — ancient automated systems.

Why these: each faction has a clear relationship to **infrastructure**, which is what the player destroys or protects. That makes faction reactions follow naturally from the core mechanic.

## 9. NPC system

**Tiered NPCs** — sophistication only where players notice it:

| Tier | Who | Behavior | Memory |
|---|---|---|---|
| **T0 Ambient** | Crowds, workers | Idle/wander points, flee from explosions/combat, play work animations | None |
| **T1 Functional** | Guards, vendors, enemies, quest givers | State machine (Idle/Patrol/Alert/Combat/Flee), faction membership, react to rep bands | Reads world facts |
| **T2 Named** | ~5–10 important characters | Hand-authored dialogue variants keyed by facts; can die permanently | World facts + event log ("you killed my brother") |

- **Schedules:** not in MVP. Instead, **state-based placement**: which NPCs appear where depends on world state ("Market in Crisis" → ration line spawn set). Feels alive, costs a fraction.
- **Memory:** global world facts, not per-NPC brains. "NPCs remember" = dialogue checks facts. Players can't tell the difference.
- **Fear/reaction to destruction:** a simple **"disturbance" event** (explosions, collapses broadcast a radius); T0/T1 NPCs in range flee or go alert. Cheap and very effective.
- **Combat AI:** NavMesh + finite state machine. Where sophistication **is** valuable: **enemy awareness of the environment** — e.g., enemies avoid standing under cracked ceilings, take cover behind breakable cover (and get exposed when it breaks), use alternate routes when bridges fall. That's where AI reinforces the core idea.

## 10. Combat

Design goal: **the room is your weapon.** Interesting interactions over bigger health bars.

- **Grab/throw (core verb):** a "gravity tool"-style grab for medium objects; throw damage scales with mass × speed.
- **Kick:** short-range knockback — knock enemies off ledges, into hazards, knock over shelving.
- **Melee:** a heavy tool (mining pick/hammer) — also breaks L2 objects and supports.
- **Ranged:** one simple firearm (rivet gun / scrap rifle) — limited ammo encourages environment use. Can shoot supports, tanks, chains.
- **Explosives:** throwable charges; huge knockback; break structures; **loud** (disturbance attracts enemies and, deeper down, worse things).
- **Environmental hazards:** gas tanks, steam pipes, hanging loads on chains, conveyor belts, mine carts on rails, unstable ceilings, electrified water.
- **Cover:** some cover is breakable — dynamic for both sides.

Enemy types (each designed to *demand* a physics answer):
| Enemy | Counter |
|---|---|
| **Scrapper** (basic melee) | Anything; teaches kick/throw |
| **Shieldbearer** | Shield blocks front; throw heavy object to knock shield away, or drop something from above |
| **Heavy / Loader** (too heavy to knock back) | Lure under hanging loads, collapse floor/catwalk under it |
| **Sniper on catwalk** | Destroy catwalk supports |
| **Swarm crawlers** (deeper, 1.0) | Explosives, chokepoints, collapse tunnels behind you |

Avoid: damage-sponge enemies, too many weapon types, a complex combo system.

## 11. World simulation: continuous vs event-driven

**Recommendation: event-driven abstraction.** Only the loaded zone is simulated in real time.

| System | Approach |
|---|---|
| NPCs off-screen | Not simulated. Spawn sets chosen from state on zone load. |
| Factions | Numbers updated by rules at world ticks. |
| Economy | Not simulated. Per-settlement resource supply levels from node states. |
| Resources | Node output → settlement supply, recalculated at ticks. |
| Population | Settlement Condition band (Stable/Strained/Crisis/Abandoned). |
| Infrastructure | Node state machine (Intact/Damaged/Destroyed), changed by events or repair missions. |
| Territory (1.0) | Zone owner fact changed by rules. |

**World tick** = whenever time passes meaningfully: zone transition, resting, mission completion. The tick: process events since last tick → run rules → advance scheduled events → recalc settlement conditions. Deterministic, testable, cheap, savable. Continuous simulation would be expensive, hard to debug, and mostly invisible to the player.

## 12. Save system

- **Philosophy:** the scene file is the **baseline**; the save file stores only the **differences** (deltas) from that baseline. That keeps saves tiny.
- **Stable IDs:** every persistent object (L2+ destructible, door, container, named NPC) has a `PersistentId` component holding a unique string ID, assigned in the editor. **This must exist from Phase 1** — retrofitting IDs later is painful.
- **Save file (JSON):**
```json
{
  "version": 1,
  "day": 4,
  "currentZone": "concord_market",
  "player": { "position": [..], "health": 80, "inventory": [..] },
  "facts": { "span_bridge.state": "destroyed", "faction.concord.rep": -20 },
  "zones": {
    "the_span": { "destroyed": ["wall_03", "strut_a"], "opened": ["gate_1"], "looted": ["crate_7"] }
  },
  "deadNpcs": ["foreman_hale"],
  "eventLog": [ { "type": "StructureDestroyed", "id": "span_bridge", "day": 2 } ],
  "scheduled": [ { "event": "price_spike", "day": 5 } ],
  "missions": { "escort_water": "completed" },
  "discovered": ["old_shaft"]
}
```
- **When to save (MVP):** autosave at zone transitions and checkpoints only — never mid-collapse. Avoids saving moving physics state (hard and buggy).
- **Loading:** load scene → `PersistentId` objects look themselves up in the save → destroyed ones swap to rubble state, opened doors open, dead NPCs don't spawn → StateGates read facts.
- **Versioning:** `version` field + a small migration step so old saves don't break as the game evolves.
- **Size:** kilobytes, even for long games.

## 13. Performance — risks & mitigations

| Problem | Mitigation |
|---|---|
| Too many active rigidbodies | Debris cap; pieces sleep/freeze after settling; swap to static rubble; physics layers & collision matrix (debris doesn't collide with debris) |
| Destruction spikes (100 pieces spawning at once) | Pre-instantiate fractured pieces disabled; object pooling for common debris; keep piece counts low (10–30 per object) |
| AI cost | Few active enemies per encounter (≤8–10); FSM not heavy planning; AI updates staggered; disable AI far from player |
| NavMesh after destruction | Don't rebuild NavMesh at runtime; use **NavMeshLinks/obstacles toggled by state** for authored changes |
| Large environments | Zone-based scenes; occlusion culling (caves are ideal for it); LODs; static batching |
| Lighting in dark caves | Baked/mixed lighting + few realtime lights; Adaptive Probe Volumes (Unity 6) for dynamic objects; accept that destroyed areas use realtime local lights |
| Persistent objects | Only IDs + states stored; no per-object physics state |
| Save files | Deltas only; save at transitions |
| Simulation | Event-driven ticks, never per-frame off-screen sim |
| Rendering | URP, stylized art, texture atlases, profiling with Unity Profiler from Phase 0 |

**Rule:** profile early and every phase. Set a target (e.g., 60 FPS on a mid-range PC) and a "debris budget" per encounter.

## 14. Project architecture

Keep it small, flat, and readable. **One rule:** game logic (rules, facts, factions, save data) lives in **plain C# classes** that don't depend on Unity scenes, so it can be unit-tested. MonoBehaviours are thin wrappers that connect it to the game world.

```
Assets/_Project/
  Scripts/
    Core/          GameManager, GameEvents (simple event hub), WorldClock
    Player/        PlayerController, PlayerLook, GrabTool, PlayerHealth
    Combat/        Health, Damage, Weapon, Explosion, Knockback
    Destruction/   Destructible, FracturedSwap, SupportLink, DebrisManager
    World/         ZoneLoader, ZoneExit, StateGate, RouteGate, SpawnSet
    State/         WorldState (facts), EventLog, ConsequenceRule, RuleEngine
    Factions/      FactionDefinition, FactionState, Reputation
    NPC/           NpcBrain (FSM), Barks, Disturbance
    Save/          PersistentId, SaveData, SaveSystem
    UI/            HUD, Journal, DebugOverlay
  Data/            ScriptableObject assets: factions, rules, nodes, missions, barks
  Scenes/          Boot, Zone_ConcordMarket, Zone_TheSpan, ...  Prototypes/
  Art/  Audio/  Prefabs/
  Tests/EditMode/  WorldState, RuleEngine, SaveSystem, Reputation tests
docs/
  DESIGN.md  TECH.md  ROADMAP.md  DECISIONS.md (why we chose things)  TESTING.md
CLAUDE.md          (project rules for Claude Code)
```

**Dependency direction (simple):**
```
Player / Combat / Destruction / NPC / World   (scene stuff)
                    │ raise events, read facts
                    ▼
          GameEvents ──► WorldState ◄── RuleEngine ◄── Data (rules)
                    │                      │
                    ▼                      ▼
                SaveSystem              Factions
```
- **Event system:** one small `GameEvents` class with C# events (e.g., `OnStructureDestroyed`). No third-party event frameworks.
- **Avoid:** dependency-injection frameworks, ECS/DOTS, deep inheritance hierarchies, "manager of managers". Unity singletons used sparingly and only for true globals (WorldState, SaveSystem).
- **Testing strategy:** EditMode unit tests for all pure logic (facts, rules, reputation, save round-trip, migration). Manual **test checklists** per milestone for feel/physics (written in `docs/TESTING.md`). A **debug overlay** (toggle key) showing current facts, rep, node states — essential for verifying consequences. Dev cheats: "destroy bridge", "advance day", "set rep".

> Prototype plan and development roadmap (sections 15–16) live in [ROADMAP.md](ROADMAP.md).

## 17. Claude Code workflow

**Roles:** you = director/product owner; Claude = senior engineer, technical director, mentor.

**How each milestone works:**
1. **You describe** the goal in plain language.
2. **Claude inspects** the current project, then proposes a short plan (what files, why, risks).
3. **You approve** (or redirect).
4. **Claude implements** a small piece — scripts, data, tests — and explains the key ideas (not every line).
5. **Claude gives you editor steps** for anything requiring the Unity Editor (scene setup, prefabs, components), numbered and short.
6. **You playtest** using the milestone's checklist; Claude runs EditMode tests where possible.
7. **Review together:** Claude summarizes the diff and what to watch for.
8. **Commit** with a clear message; update `docs/` (ROADMAP progress, DECISIONS, TESTING checklist).

**Ground rules (to put in `CLAUDE.md`):**
- Small milestones; never more than a few files changed at once without explanation.
- Never silently rewrite a working system; propose changes first.
- Flag technical risk explicitly.
- Prefer simple Unity-standard approaches; no new packages without discussion.
- Pure logic in plain C# with tests.
- Every persistent object gets a `PersistentId`.
- Keep `docs/DECISIONS.md` updated with "what we chose and why."
- Claude can't see the editor or run the game — so you report what you see (screenshots/console errors help a lot).

**Learning track alongside development:** each phase introduces a few concepts (Phase 1: components, Update vs FixedUpdate, Rigidbody; Phase 4: serialization, IDs; Phase 5: data-driven design, events). Claude explains them in context when they first appear.

---

## Summary (the 12 requested items)

1. **Recommended engine:** Unity 6 LTS (URP).
2. **Tech stack:** Unity 6 + URP, C#, AI Navigation package, Input System, Newtonsoft JSON (Unity package), ScriptableObjects, Unity Test Framework, Blender (Cell Fracture), Git + LFS, VS Code. No paid plugins.
3. **MVP:** 4-zone Upper Works district, 3 factions, 2 infrastructure nodes (Bridge, Pump) with full consequence chains, physics combat with grab/throw/kick/explosives, 3–4 enemy types, checkpoint save/load. First-person, single-player.
4. **Core loop:** Enter zone → read the stakes → choose approach → physics combat/destruction → outcome becomes world facts → world tick on leaving/resting → return to a changed world → go deeper.
5. **Major systems:** Player & GrabTool, Combat, Destruction (tiered L1–L4), Zones & Gates, WorldState + EventLog, RuleEngine (consequences), Infrastructure nodes & settlement conditions, Factions, Tiered NPCs, Save (delta-based), Debug overlay.
6. **Hardest technical problems:** reliable persistence of destruction (stable IDs); destruction performance; AI navigation through changed geometry; lighting after destruction; making consequences *visible* and readable; keeping content scope under control.
7. **Prototype plan:** P1 Physics combat sandbox → P2 Destruction & collapse → P3 Persistence → P4 Consequence chain → P5 AI vs changed world.
8. **Roadmap:** Phase 0 Setup → 1 Player & physics feel → 2 Combat & enemies → 3 Destruction → 4 Persistence & zones → 5 Consequences & factions → 6 MVP content → 7 Polish → 8+ toward 1.0.
9. **Architecture:** flat `Scripts/` folders by system; pure-C# logic core (WorldState, RuleEngine, Factions, SaveData) with thin MonoBehaviour wrappers; one simple `GameEvents` hub; ScriptableObject data; EditMode tests; `docs/` + `CLAUDE.md`.
10. **Biggest risks:** scope creep (#1); physics combat not being fun; consequences feeling invisible; destruction performance; beginner learning curve in Unity editor; content production time.
11. **Do NOT build yet:** multiplayer; open world / seamless streaming; procedural world generation; runtime/voxel fracture; destroy-anything buildings; fluid simulation; continuous economy/population sim; NPC daily schedules and per-NPC memory; AI/LLM-generated dialogue; vehicles; territory warfare; crafting, skill trees, inventory depth; third-person animation; deeper layers (forest/ocean/the Deep); voice acting; paid plugins.
12. **Build first:** **Phase 0 setup, then Prototype P1** — a grey-box room with a first-person controller, grab/throw/kick, an explosive barrel, a breakable crate, and two simple enemies. It teaches you Unity fundamentals and answers the most important question: *is throwing the world at enemies fun?*

## Open questions for your review
- First-person OK for the MVP? (A2)
- Stylized/low-poly art direction OK? (A3)
- Faction concepts (Concord / Delvers' Union / Hollowers) — keep, rename, or rethink?
- Where should the Unity project live — this repo (`ClaudeTesting`) or a new dedicated repo?
- After approval, I'd save this plan into the repo as `docs/DESIGN.md` + `docs/ROADMAP.md` as the first commit (no code).

## Verification (for this planning phase)
No code exists yet. The plan is "verified" by your review. Each later phase has its own test steps and definition of done above; prototypes P1–P5 are the real verification that the design is technically sound.
