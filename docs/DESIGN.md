# The Deep — Game Design & Technical Design

## Status

**Pre-production — design decisions are locked** (2026-10-04). Each locked decision, with its reasons and tradeoffs, is in [DECISIONS.md](DECISIONS.md). The prototype plan and phases are in [ROADMAP.md](ROADMAP.md). No gameplay code exists yet.

Locked in short: Unity 6 + URP + C# · first-person · stylized industrial sci-fi / underground brutalism · connected areas (not open world) · authored/pre-fractured destruction · event-driven simulation · three factions · single-player · hand-built levels · solo part-time developer working with Claude Code, PC (Windows).

## Core identity — "Your choices become the enemy"

This is the feature we protect above all others. **The most important feature is not the size of the world; it is: "I changed something, and the world remembers."**

The canonical example, which every system must be able to support:

```
Player destroys the bridge
  → world event recorded
  → route changes (crossing blocked, Old Shaft bypass opens)
  → NPCs react (barks, fleeing, ration lines)
  → faction reputation changes
  → trade changes (prices, vendor stock)
  → mission availability changes
  → player meets the consequences later
```

**Scope rule (tie-breaker):** a small area that reacts convincingly beats a giant world with superficial systems. When choosing between "more content" and "deeper reaction to what already exists," choose reaction (D-011).

## Glossary

| Term | Meaning |
|---|---|
| **District** | A group of connected areas. The MVP has one: the **Upper Works**. |
| **Area** | One playable place = one Unity scene (e.g., Concord Market). Areas connect through **exits**. |
| **Infrastructure node** | A small, fixed set of important structures whose state drives consequences (MVP: the **Bridge** and the **Pump**). States: Intact / Damaged / Destroyed. |
| **Fact** | One named value in the world state, e.g. `span_bridge.state = destroyed`. Saved. |
| **Event** | Something meaningful that happened (e.g., `StructureDestroyed span_bridge`). Recorded in the event log and may set facts. |
| **World tick** | The moment consequence rules run: area transition, resting, mission completion. Nothing off-screen changes between ticks. |
| **Day** | The in-game day counter, used for delayed consequences. MVP default: advances when the player rests or completes a mission (see open items). |
| **Settlement condition** | Stable / Strained / Crisis / Abandoned — derived from resource supply. |

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

## 1. Engine comparison & decision

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

**Decision (locked, D-001/D-002): Unity 6 LTS with URP (Universal Render Pipeline), C#.**

Why:
- C# is beginner-friendly and the code lives in plain text files — the best fit for working with Claude Code, which can write, read, and unit-test that code.
- The largest library of tutorials/answers in existence for exactly the things we need (character controllers, rigidbodies, NavMesh, save systems).
- Pre-fractured destruction (our chosen technique) works fine in Unity — we don't need Unreal's Chaos.
- You already want to learn Unity, and the skills transfer.
- Honest tradeoff: Unreal has better destruction and large-world tech out of the box, but Blueprints are invisible to Claude Code and C++ is a rough first language. Godot is the runner-up (text scenes are very AI-friendly) but has a thinner 3D ecosystem for a physics/destruction-heavy game.

**Known Unity limitation for our workflow:** Claude Code cannot see the editor or play the game. Scenes and prefabs must be set up by you in the editor, guided step by step. Claude can write scripts, data, tests, and docs — and can run EditMode tests via Unity's command line if we set that up.

## 2. Tech stack

| Area | Choice | Reason |
|---|---|---|
| Engine | Unity 6 LTS (exact version recorded in DECISIONS.md at Phase 0) | Above |
| Render pipeline | URP | Lighter, simpler than HDRP; suits a stylized, lighting-driven look |
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

**One-sentence MVP:** *A single underground district of ~4 connected areas where the player can fight with physics, break key structures, and see that destruction permanently change routes, a faction's attitude, NPC dialogue, and the next mission — and all of it survives save/load.*

MVP target (locked): 1 district · ~4 connected areas · 3 factions · 2 major infrastructure/destruction systems · physics-based interaction · basic combat · persistent world state · faction reactions · NPC reactions · mission consequences · save/load persistence.

MVP content:
- **Setting:** "The Upper Works" — the shallowest layer.
- **4 areas** (one Unity scene each):
  1. **Concord Market** (hub town, vendors, faction HQ)
  2. **The Span** (a chasm crossed by a destructible bridge — the trade route)
  3. **Pumpworks** (a water facility that can be sabotaged or defended)
  4. **Old Shaft** (mine/ruin combat area, alternate route that opens if the bridge falls)
- **3 factions:** Concord (authority), Delvers' Union (miners), Hollowers (scavengers).
- **2 infrastructure nodes** with persistent states: the Bridge and the Pump.
- **3–4 enemy types**, 1 melee tool, 1 ranged weapon, explosives, grab/throw.
- **~6–10 missions** (some generated from templates by world state).
- **Save/load** at checkpoints and area transitions.
- **The consequence chain must work end to end** for both nodes. That is the whole point of the MVP.

What the MVP proves: "The world reacts to me" is fun, technically sound, and buildable.

Not in the MVP: going deeper. The MVP loop ends at "return to a changed district"; descending to new layers starts in 1.0.

Rough time estimate (honest, with big uncertainty): **7–12 months part-time** (the sum of the ROADMAP phase estimates) for a beginner with AI help, assuming scope holds. Scope creep is the main threat, not the tech.

### Version 1.0
- 3 depth layers (Upper Works, The Sump/industrial + farms, The Ruins), ~12–18 areas
- Possibly 4–5 factions (Garrison/military, Deep-dwellers) — each requires a new decision entry (D-008)
- 6–10 infrastructure nodes (rail depot, power plant, farm caverns, dam, gates)
- Territory: more areas with contested control; factions act on territory goals
- Longer delayed-consequence chains ("3 days later, refugees arrive at the Market")
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
- Small procedural cave sections between authored areas
- New Game+ / alternate starts
- Modding via data files

## 4. Art direction

**Locked (D-004): stylized industrial sci-fi / underground brutalism.** The game should look stylized and *intentional*, never "cheap low-poly."

Visual themes:
- Massive concrete architecture — monumental, heavy, carved into rock
- Rusty industrial machinery, pipes and cables everywhere
- Mining equipment: drills, carts, cranes, conveyor belts
- Industrial hazards: gas tanks, steam vents, exposed wiring, hanging loads
- Strong lighting and signage: work lights, warning stripes, painted faction marks, stencilled directions
- Deeper = older and stranger: ancient structures and unfamiliar geometry replace human engineering

How we make it look intentional on a small budget:
- **Big, simple shapes** with chamfered edges, and few small details. The silhouette carries the scale.
- A **modular kit** (wall, floor, pillar, pipe, catwalk, door, sign pieces) reused everywhere, with variety from materials and decals.
- **Lighting does the heavy lifting**: dark caves with strong colored practical lights. Each faction has a color, which also helps readability.
- **Decals and signage** for storytelling (graffiti after the bridge falls, ration notices during a water crisis). These are cheap ways to *show* consequences.
- **Consistent "breakable" language**: wood, rusted metal, cracked concrete and hazard paint mean "this can break." Raw rock means it can't.

Tradeoff: a lighting-driven look makes lighting after destruction harder (see §14 Performance).

## 5. Core game loop

```
        ┌────────────────────────────────────────────┐
        ▼                                            │
  Enter an area ─► Read the situation (who's here,   │
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
  (1.0+) Unlock / be pushed toward the next depth layer
```

Design notes:
- **"Read the situation" step:** consequences only feel meaningful if the player can *see* what's at stake *before* acting (e.g., "this bridge carries all water to the Market"). Informed choices > surprise punishment.
- **Consequences resolve at world ticks** (area transition, rest, mission completion), not continuously — this is the technical trick that makes the whole game feasible (see §12 World Simulation).
- **Multiple approaches** to each situation, so destruction is a choice, not the only tool.

Why it's fun:
1. **Short loop** (combat, seconds): physics makes every fight a little different — improvising with the room is satisfying.
2. **Medium loop** (a mission, minutes): choosing *how* to solve a problem, with visible stakes.
3. **Long loop** (hours): returning to a place you changed and seeing the ripple. This is the hook — "my choices did that." It also creates *emergent stories* players retell.
4. **Tension:** destruction is powerful in the moment but costs you later. Power with a price is a classic recipe for interesting decisions.

## 6. World design

- **Structure (locked, D-005): connected areas** (think Metro Exodus / Dishonored / Metroid-style regions), **not** open world. Each area is a hand-built Unity scene; areas connect via tunnels, elevators, and rail.
- **Size:** MVP ~4 areas, each ~5–15 minutes to traverse. 1.0: ~15 areas across 3 layers. A small world with strong systemic reactions beats a big world with shallow interaction (D-011).
- **Connectivity:** a graph. Each area has named **exits**; each exit can be open/blocked depending on world facts (bridge destroyed → Span east exit blocked; Old Shaft bypass opens).
- **Loading:** area transitions are **diegetic loading moments** (elevator ride, long tunnel, rail car). Load the next scene, unload the previous. No seamless streaming — it's a huge complexity jump with little gameplay value in a cave world where tunnels naturally hide transitions.
- **Depth = danger:** each layer deeper adds stranger enemies, fewer friendly factions, harsher hazards. Gates between layers are story/world-state gates.

### What persists when the player leaves an area
| Persists (saved) | Resets / simulated away |
|---|---|
| Level 2–4 destruction (gameplay objects, structures, major events) | Level 1 small debris, loose props, bodies |
| Dead **named/important** NPCs | Generic enemies (respawn based on area state rules) |
| Looted containers, opened doors, discovered locations | Projectiles, particles, physics positions of most props |
| World facts & faction state | Ambient NPC positions |

### How we avoid simulating the whole civilization
Only the **current area** runs real physics, AI, and NPCs. Everything else exists only as **data** (world facts, faction numbers, node states). When the player arrives somewhere, the area **reads the data and configures itself** to match. The world "moves on" only at world ticks (area transitions, resting, mission completion). This is the same approach used by Fallout/Elder Scrolls cells and most immersive sims.

## 7. Destruction system

**Locked (D-006): practical authored / pre-fractured destruction, and it must be gameplay-relevant.** The MVP's two major destruction systems are the **Bridge** (The Span) and the **Water Pump** (Pumpworks). We do **not** attempt runtime-fracturing buildings, fully destructible environments, or destruction of everything.

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

## 8. Consequence system — "Your choices become the enemy"

### Architecture

```
Player action (break bridge, kill leader, help miners)
      │
      ▼
GameEvent  { type: "StructureDestroyed", id: "span_bridge", area, day, causedByPlayer }
      │
      ▼
WorldState.SetFact("span_bridge.state", "destroyed")   + append to EventLog
      │
      ▼  (at world tick: area transition / rest / mission end)
RuleEngine evaluates ConsequenceRules (data assets)
      │
      ├──► Faction changes   (Concord rep −30, Delvers rep +10)
      ├──► Resource changes  (Market.trade −50)
      ├──► Scheduled events  (in 2 days: "price_spike")
      ├──► Mission generation (template "Escort supplies through Old Shaft")
      └──► New facts          ("concord.hunting_player" = true)
      │
      ▼
When an area loads, components READ facts and configure the scene:
  StateGate (show/hide objects), RouteGate (exits & NavMeshLinks),
  SpawnSet (which enemies/NPCs), Barks (what NPCs say), Vendor stock
```

### What is a meaningful event?
An event is meaningful if **at least one rule or piece of content reacts to it.** Concretely, only **tagged** objects/NPCs emit persistent events: L2+ destruction with an ID, named NPC deaths, mission outcomes, infrastructure node state changes, attacks on faction members. Breaking a bottle is not an event.

### How events/facts are stored
- **Facts:** a flat dictionary `string → value` (bool/int/float/string). E.g. `span_bridge.state = "destroyed"`, `faction.concord.rep = -20`, `pump.output = 0`, `npc.foreman_hale.dead = true`. Simple to save, debug, and query.
- **Event log:** an append-only list of what happened (type, id, day). Used for NPC dialogue ("You're the one who dropped the Span") and for history/journal. Capped/summarized so it doesn't grow forever.

### When consequences happen (MVP default — see open items)
- **Immediate consequences** (route blocked, rep change, new facts) resolve at the **next world tick** — usually the next area transition — so the player sees a reaction soon after acting.
- **Delayed consequences** ("price spike in 1 day", "Crisis 2 days later") are scheduled in **days**. The day counter advances when the player **rests** or **completes a mission**.
- **Blame:** in the MVP the player is **always** blamed for major (L4) destruction they cause. A witness/evidence system is deferred to 1.0 — it adds complexity and makes consequences less reliable to test.

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
Pump → Destroyed ⇒ `water` supply to Market drops ⇒ Market condition → Strained (next tick) → Crisis (2 days later) ⇒ Content reacting to *Market: Crisis*: ration lines, higher prices, Concord patrols increase, Hollowers raid water caravans (new enemy spawn set), mission template "Repair the Pump" or "Escort water" appears, refugees move to Old Shaft (SpawnSet change there), NPC barks blame "the saboteur", Hollowers take control of Pumpworks (`area.pumpworks.controller = hollowers`). Concord rep drops (MVP: player always blamed for L4 destruction).

## 9. Faction system

| Element | MVP | 1.0 | Later |
|---|---|---|---|
| Reputation (player ↔ faction, −100..100, banded: Hostile/Unfriendly/Neutral/Friendly/Allied) | ✅ | ✅ | |
| Faction values (what each faction cares about → automatic rep reactions) | ✅ | ✅ | |
| Faction ↔ faction relationships | ✅ fixed table | ✅ changes via events | |
| Resources (abstract numbers: water, trade, ore, scrap) | ✅ via nodes | ✅ | Economy sim |
| Territory | ✅ area-control fact (`area.<id>.controller`), changed by rules | ✅ more contested areas, faction territory goals | Dynamic wars |
| Goals (faction "wants" that generate missions) | ❌ hand-authored missions | ✅ simple goal list | Planning AI |
| Alliances/conflicts that shift | ❌ | ✅ rule-driven | |

**Locked (D-008): three MVP factions** — Concord, Delvers' Union, Hollowers. Additional major factions need a new decision entry.

Factions (MVP in **bold**; *italic* = future candidates, not committed):
- **The Concord** — city authority; controls water and trade. Values: order, infrastructure.
- **Delvers' Union** — miners; exploited by the Concord. Values: workers, mines; hates Concord taxes.
- **The Hollowers** — scavengers living in abandoned works; thrive on collapse. Values: salvage; *benefit* when infrastructure fails (great for "your choices become the enemy" — your destruction makes them stronger).
- *Garrison* (1.0) — military; arrives if Concord is in crisis.
- *The Deep-dwellers / Pale* (1.0) — something below, drawn upward by noise and collapse.
- *The Old Machines* (later) — ancient automated systems.

Why these: each faction has a clear relationship to **infrastructure**, which is what the player destroys or protects. That makes faction reactions follow naturally from the core mechanic.

### How each MVP faction connects to the systems (proposed — tune during P4/Phase 4)

| | The Concord | Delvers' Union | The Hollowers |
|---|---|---|---|
| **Infrastructure** | Maintains the Bridge and the Pump; damage hurts them most | Depend on the Bridge to move ore out of the Old Shaft | Profit when infrastructure fails (salvage); may hire the player to sabotage |
| **Resources** | Water and trade (controls distribution and prices) | Ore (sold to the Market over the Bridge) | Scrap (gained from any destruction) |
| **Territory (MVP)** | Controls Concord Market and Pumpworks | Controls the Old Shaft | Holds derelict corners of The Span; can **take over a failed area** (e.g., Pumpworks after the Pump is destroyed) |
| **Reacts to player actions** | Punishes infrastructure damage; rewards repairs and defense | Rewards help with mine safety and resisting Concord control; angered by losing the Bridge too | Rewards sabotage and salvage; angered by attacks on scavengers or rebuilding |
| **Default relationship** | Rival of the Union (taxes), hostile to Hollowers | Rival of the Concord, wary of Hollowers | Hostile to the Concord, opportunistic toward the Union |

This creates real dilemmas: destroying the Bridge hurts **both** the Concord and the Union while strengthening the Hollowers; destroying the Pump hurts everyone in the Market, including Union families.

## 10. NPC system

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

## 11. Combat

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

## 12. World simulation: continuous vs event-driven

**Locked (D-007): event-driven, state-based simulation.** Only the loaded area is simulated in real time. Systems are only evaluated when meaningful gameplay events require it.

| System | Approach |
|---|---|
| NPCs off-screen | Not simulated. Spawn sets chosen from state on area load. |
| Factions | Numbers updated by rules at world ticks. |
| Economy | Not simulated. Per-settlement resource supply levels from node states. |
| Resources | Node output → settlement supply, recalculated at ticks. |
| Population | Settlement Condition band (Stable/Strained/Crisis/Abandoned). |
| Infrastructure | Node state machine (Intact/Damaged/Destroyed), changed by events or repair missions. |
| Territory | Area-control fact changed by rules (MVP); contested territory in 1.0. |

**World tick** = whenever time passes meaningfully: area transition, resting, mission completion. The tick: process events since last tick → run rules → advance scheduled events → recalc settlement conditions. Deterministic, testable, cheap, savable. Continuous simulation would be expensive, hard to debug, and mostly invisible to the player.

## 13. Save system

- **Philosophy:** the scene file is the **baseline**; the save file stores only the **differences** (deltas) from that baseline. That keeps saves tiny.
- **Stable IDs:** every persistent object (L2+ destructible, door, container, named NPC) has a `PersistentId` component holding a unique string ID, assigned in the editor. **Introduced in prototype P2, before the first persistent destructible is placed** — retrofitting IDs later is painful.
- **Save file (JSON):**
```json
{
  "version": 1,
  "day": 4,
  "currentArea": "concord_market",
  "player": { "position": [..], "health": 80, "inventory": [..] },
  "facts": { "span_bridge.state": "destroyed", "faction.concord.rep": -20 },
  "areas": {
    "the_span": { "destroyed": ["wall_03", "strut_a"], "opened": ["gate_1"], "looted": ["crate_7"] }
  },
  "deadNpcs": ["foreman_hale"],
  "eventLog": [ { "type": "StructureDestroyed", "id": "span_bridge", "day": 2 } ],
  "scheduled": [ { "event": "price_spike", "day": 5 } ],
  "missions": { "escort_water": "completed" },
  "discovered": ["old_shaft"]
}
```
- **When to save (MVP):** autosave at area transitions and checkpoints only — never mid-collapse. Avoids saving moving physics state (hard and buggy).
- **Loading:** load scene → `PersistentId` objects look themselves up in the save → destroyed ones swap to rubble state, opened doors open, dead NPCs don't spawn → StateGates read facts.
- **Versioning:** `version` field + a small migration step so old saves don't break as the game evolves.
- **Size:** kilobytes, even for long games.

## 14. Performance — risks & mitigations

| Problem | Mitigation |
|---|---|
| Too many active rigidbodies | Debris cap; pieces sleep/freeze after settling; swap to static rubble; physics layers & collision matrix (debris doesn't collide with debris) |
| Destruction spikes (100 pieces spawning at once) | Pre-instantiate fractured pieces disabled; object pooling for common debris; keep piece counts low (10–30 per object) |
| AI cost | Few active enemies per encounter (≤8–10); FSM not heavy planning; AI updates staggered; disable AI far from player |
| NavMesh after destruction | Don't rebuild NavMesh at runtime; use **NavMeshLinks/obstacles toggled by state** for authored changes |
| Large environments | Area-based scenes; occlusion culling (caves are ideal for it); LODs; static batching |
| Lighting in dark caves | Baked/mixed lighting + few realtime lights; Adaptive Probe Volumes (Unity 6) for dynamic objects; accept that destroyed areas use realtime local lights |
| Persistent objects | Only IDs + states stored; no per-object physics state |
| Save files | Deltas only; save at transitions |
| Simulation | Event-driven ticks, never per-frame off-screen sim |
| Rendering | URP, stylized art, texture atlases, profiling with Unity Profiler from Phase 0 |

**Rule:** profile early and every phase. Set a target (e.g., 60 FPS on a mid-range PC) and a "debris budget" per encounter.

## 15. Project architecture

Keep it small, flat, and readable. The folder tree below is the **target**; folders are created only when their first file exists (D-013). **One rule:** game logic (rules, facts, factions, save data) lives in **plain C# classes** that don't depend on Unity scenes, so it can be unit-tested. MonoBehaviours are thin wrappers that connect it to the game world.

```
Assets/_Project/          (our content; underscore keeps it first and separate from imported assets)
  Scripts/                (+ one runtime assembly definition)
    Core/          GameManager, GameEvents (simple event hub), WorldClock
    Player/        PlayerController, PlayerLook, GrabTool, PlayerHealth
    Combat/        Health, Damage, Weapon, Explosion, Knockback
    Destruction/   Destructible, FracturedSwap, SupportLink, DebrisManager
    World/         AreaLoader, AreaExit, StateGate, RouteGate, SpawnSet
    State/         WorldState (facts), EventLog, ConsequenceRule, RuleEngine
    Factions/      FactionDefinition, FactionState, Reputation
    NPC/           NpcBrain (FSM), Barks, Disturbance
    Save/          PersistentId, SaveData, SaveSystem
    UI/            HUD, Journal, DebugOverlay
  Data/            ScriptableObject assets: factions, rules, nodes, missions, barks
  Scenes/          Boot, Area_ConcordMarket, Area_TheSpan, ...  Prototypes/ (Phase 1)
  Prefabs/  Materials/  Models/  Audio/
  Tests/EditMode/  WorldState, RuleEngine, SaveSystem, Reputation tests (+ test assembly definition)
ArtSource/         Blender .blend sources (outside Assets so Unity doesn't import them; Git LFS)
Packages/  ProjectSettings/   (Unity-generated, committed)
docs/
  DESIGN.md  ROADMAP.md  DECISIONS.md
README.md  CLAUDE.md  .gitignore  .gitattributes
```

The two assembly definitions (one for game code, one for EditMode tests) are the only "extra" structure: Unity tests can't reference game code without them.

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
- **Testing strategy:** EditMode unit tests for all pure logic (facts, rules, reputation, save round-trip, migration). Manual **test checklists** per milestone for feel/physics (in ROADMAP.md; moved to a `docs/TESTING.md` only if they outgrow it). A **debug overlay** (toggle key) showing current facts, rep, node states — essential for verifying consequences. Dev cheats: "destroy bridge", "advance day", "set rep".

> The prototype plan and development roadmap live in [ROADMAP.md](ROADMAP.md).

## 16. Claude Code workflow

**Roles:** you = director/product owner; Claude = senior engineer, technical director, mentor.

**How each milestone works:**
1. **You describe** the goal in plain language.
2. **Claude inspects** the current project, then proposes a short plan (what files, why, risks).
3. **You approve** (or redirect).
4. **Claude implements** a small piece — scripts, data, tests — and explains the key ideas (not every line).
5. **Claude gives you editor steps** for anything requiring the Unity Editor (scene setup, prefabs, components), numbered and short.
6. **You playtest** using the milestone's checklist; Claude runs EditMode tests where possible.
7. **Review together:** Claude summarizes the diff and what to watch for.
8. **Commit** with a clear message; update `docs/` (ROADMAP progress, DECISIONS).

**Git workflow (D-012):**
```
main  ──►  feature branch (one per milestone, e.g. P1)  ──►  pull request  ──►  review  ──►  merge into main
```
`main` only receives reviewed work. Nothing is force-pushed or deleted without an explicit warning first.

**Ground rules (kept in `CLAUDE.md`):**
- Small milestones; never more than a few files changed at once without explanation.
- Never silently rewrite a working system; propose changes first.
- Flag technical risk explicitly.
- Prefer simple Unity-standard approaches; no new packages without discussion.
- Pure logic in plain C# with tests.
- Every persistent object gets a `PersistentId`.
- Keep `docs/DECISIONS.md` updated with "what we chose and why."
- Claude can't see the editor or run the game — so you report what you see (screenshots/console errors help a lot).

**Learning track alongside development:** each step introduces a few concepts (P1: components, Update vs FixedUpdate, Rigidbody; P3: serialization, IDs; P4: data-driven design, events). Claude explains them in context when they first appear.

---

## Summary

1. **Engine:** Unity 6 LTS + URP, C# (locked).
2. **Tech stack:** Unity 6 + URP, C#, AI Navigation package, Input System, Newtonsoft JSON (Unity package), ScriptableObjects, Unity Test Framework, Blender (Cell Fracture), Git + LFS, VS Code. No paid plugins.
3. **MVP:** one district (Upper Works) of ~4 connected areas, 3 factions, 2 infrastructure nodes (Bridge, Pump) with full consequence chains, physics interaction and basic combat, faction and NPC reactions, mission consequences, save/load. First-person, single-player.
4. **Core loop:** Enter area → read the stakes → choose approach → physics combat/destruction → outcome becomes world facts → world tick → return to a changed district (→ go deeper in 1.0).
5. **Major systems:** Player & GrabTool, Combat, Destruction (tiered L1–L4), Areas & Gates, WorldState + EventLog, RuleEngine (consequences), Infrastructure nodes & settlement conditions, Factions, Tiered NPCs, Save (delta-based), Debug overlay.
6. **Hardest technical problems:** reliable persistence of destruction (stable IDs); destruction performance; AI navigation through changed geometry; lighting after destruction; making consequences *visible* and readable; keeping content scope under control.
7. **Prototype plan:** P1 Physics → P2 Destruction → P3 Persistence → P4 Consequences → P5 AI, each small and timeboxed (see ROADMAP.md).
8. **Roadmap:** Phase 0 Setup → 1 Prototypes P1–P5 + gate review → 2 Core gameplay → 3 Persistent world → 4 Consequences, factions & NPC reactions → 5 MVP content → 6 Polish & release → 7+ toward 1.0.
9. **Architecture:** flat `Scripts/` folders by system; pure-C# logic core (WorldState, RuleEngine, Factions, SaveData) with thin MonoBehaviour wrappers; one simple `GameEvents` hub; ScriptableObject data; EditMode tests.
10. **Biggest risks:** scope creep (#1); consequences feeling invisible; physics interaction not being fun; destruction performance; beginner learning curve in the Unity Editor; content production time.
11. **Do NOT build yet:** multiplayer; open world / seamless streaming; procedural world generation; runtime/voxel fracture; destroy-anything buildings; fluid simulation; continuous economy/population sim; NPC daily schedules and per-NPC memory; witness/evidence system; AI/LLM-generated dialogue; vehicles; territory warfare; crafting, skill trees, inventory depth; third-person animation; deeper layers (forest/ocean/the Deep); voice acting; paid plugins.
12. **Build first:** Phase 0 setup, then a deliberately small P1.

## Open items (defaults written above; confirm or change)
- **How time passes:** day advances on rest or mission completion; immediate consequences at the next area transition (§8).
- **Blame:** player always blamed for L4 destruction they cause; witnesses deferred to 1.0 (§8).
- **Faction × systems table** (§9) is a proposal to tune during P4/Phase 4.
