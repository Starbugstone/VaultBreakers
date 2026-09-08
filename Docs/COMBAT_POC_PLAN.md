# Current scope override

See [DUNGEON_POC.md](DUNGEON_POC.md): the user explicitly requested the dungeon redesign after rejecting the arena. The phases below remain the combat safety baseline; the new scene retains locked combat zones and the Shatterbelt lore, with connected transitions and rebuilt art.

# Vaultbreakers Combat POC Plan

**Status:** Remaining POC implementation authorized on 2026-09-08; human acceptance remains pending
**Prepared from:** `Docs/PROJECT.md`  
**Repository audit:** 2026-08-01  
**Primary focus:** Single-player movement and combat  
**Target:** Windows PC, Xbox-style controller, stable 60 FPS

---

## 1. Purpose

This is the execution plan for the first playable proof of concept (POC) for **Vaultbreakers in the Shatterbelt**. It translates the larger design in `PROJECT.md` into a small, ordered implementation backlog.

The POC has one job: prove that controlling a Vaultbreaker and fighting in a fixed isometric arena is immediate, readable, and fun.

The POC is successful when a new player can enter one arena, learn the controls without a tutorial screen, use movement, melee, ranged fire, shield, and dodge deliberately, defeat three distinct enemy roles, die and retry cleanly, and want to play again.

This plan deliberately stops before the larger progression loop. Gems, gauges, cards, scanning, pets, co-op, saves, QR, NFC, and production content begin only after the combat gate passes.

---

The 2026-09-08 completion request authorizes finishing Phases 9–13 and rebuilding the
3D assets. Earlier human feel gates carry forward explicitly; no automated result is a
substitute for their sign-off. The user's updated art direction is clean and stylized,
with flashy, dynamic, high-contrast arcade lighting and feedback. See `PROJECT_STATUS.md`
and `POC_RESULTS.md` for current implementation, validation evidence and remaining gates.

## 2. Starting repository assessment (2026-08-01)

> **This section is history, not status.** It records the state the plan was written against, and the
> "required action" column is why the phases below are ordered as they are. Phases 0 to 7 have since
> been implemented and every row here has been actioned. For what the repository contains today, read
> `Docs/PROJECT_STATUS.md`.

At the time of the audit the project was not completely empty, but it contained only template-level
setup.

| Area | Current state | Required action |
|---|---|---|
| Unity editor | Unity `6000.4.4f1` | Keep this editor version for the POC |
| Render pipeline | URP package `17.4.0`, configured with a **2D Renderer** | Convert to a Universal/Forward 3D renderer before gameplay work |
| Input | Input System `1.19.0`; template action asset exists | Create a game-specific action asset; do not modify the generic template asset |
| Tests | Unity Test Framework `1.6.0` | Add EditMode and PlayMode coverage for deterministic combat rules |
| Scene | Default `SampleScene` from the 2D template | Create a separate 3D prototype arena |
| Gameplay scripts | None | Build only the combat-oriented runtime described below |
| Art | No player, enemy, weapon, animation, or arena assets | Use primitives first; build/import only validation assets after mechanics work |
| AI Assistant | Pre-release package installed | Optional tooling; it must not become a runtime dependency |
| Source control | Git repository with existing uncommitted setup changes | Preserve those changes and commit by coherent implementation slice |

### Important mismatch (resolved in Phase 1)

`PROJECT.md` calls for a 3D URP game, while the project was created from the 2D URP template. Do not build 3D gameplay on top of `Renderer2D.asset`. Create a 3D Universal Renderer Data asset, assign it to the URP pipeline asset, verify opaque meshes, shadows, depth, and camera output, and retain the old 2D assets until the conversion is proven. Removing template assets is cleanup, not a prerequisite.

### Package policy

No new package is required for the first combat POC.

- Use the installed Input System, URP, uGUI, and Test Framework.
- Use a fixed camera implemented with a normal Unity Camera; Cinemachine is unnecessary at this stage.
- Use primitive arena geometry; ProBuilder is optional and not required.
- Use direct steering in an open arena; AI Navigation is unnecessary.
- Do not add a dependency injection framework, behavior-tree framework, tween library, or third-party combat framework.

---

## 3. Locked POC scope

### Included

- One local player using an Xbox-style controller.
- Keyboard fallback for development and automated/manual testing.
- Fixed isometric 3D arena with visible bounds.
- Camera-relative movement on the XZ plane.
- Hybrid facing: movement by default, right-stick precision when used.
- Melee attack on X / West.
- Repeating ranged fire on RT.
- Directional shield on LT.
- Dodge on A / South.
- Health, damage, hit reaction, death, and wave retry.
- Shield stability, shield break, and recovery.
- One melee grunt, one ranged shooter, and one heavy bruiser.
- A short sequence of combat test waves.
- Minimal HUD: health, shield stability, current wave, retry state.
- Combat feedback using simple animation, color, shape, VFX, audio placeholders, and optional short hit-stop.
- Debug controls and combat gizmos.
- Automated tests for rules that can regress silently.
- One Windows development build used for a clean-controller playtest.

### Explicitly excluded

- Gems and liquid gauges.
- Relic power.
- Cards, loadouts, scanning, and equipment transitions.
- Pets.
- Local or online multiplayer.
- Save/load, progression, rewards, economy, and missions.
- QR, NFC, account, cloud, and backend features.
- Procedural levels, boss fights, cinematics, and production UI.
- Production-quality character art, environment art, shaders, or animation sets.

### Compatibility seams kept now

The excluded systems must not be implemented speculatively. The POC keeps only these inexpensive seams:

- gameplay values live in a central balance asset rather than scattered constants;
- damage travels through a small, source-aware damage API;
- weapons act through attack definitions/parameters rather than input code dealing damage directly;
- player visuals contain stable attachment transforms for the right-hand melee module and left-arm ranged/shield module;
- input, motor, facing, combat, health, and presentation remain separate responsibilities;
- enemies and waves are prefab/data driven enough to tune without code edits.

These seams allow cards, gauges, modular equipment, and co-op to be added later without building them now.

---

## 4. Core combat contract

The following rules come directly from the design and are treated as POC invariants.

### Input and response

| Action | Controller | Keyboard fallback | Rule |
|---|---|---|---|
| Move | Left stick | WASD | Camera-relative movement |
| Aim | Right stick | Arrow keys | Overrides movement facing above dead zone |
| Melee | X / West | Left mouse | Immediate short-range strike |
| Ranged | RT | E | Hold for repeated fire |
| Shield | LT | Right mouse | Hold to face and block a frontal arc |
| Dodge | A / South | Space | Directional burst with brief invulnerability |
| Pause | Menu / Start | Escape | Pauses local simulation |
| Debug restart | Development only | R | Restarts the active test wave |

The keyboard column is the binding set currently in `VaultbreakersInputActions.inputactions`. Controller
behavior is authoritative and keyboard bindings may still change; update this table with the asset.

Keyboard aim is a four-way arrow composite rather than mouse world aim. A pointer-delta binding only
produces a value while the mouse is physically moving and its magnitude depends on movement speed, so
it cannot drive an absolute facing direction or be reasoned about against the aim dead zone. Mouse
world aim needs a camera-to-ground projection, which is gameplay translation and does not belong in
the input reader; it is deferred until a camera service exists.

The aim dead zone is applied exactly once, by `PlayerFacing`. The input asset deliberately carries no
aim processor: a `StickDeadzone` there rescales the remaining range, so combining the two would push
the real threshold well past the documented 0.25 while both places still read as "0.25". Move keeps a
`StickDeadzone(min=0.18)` in the asset because no gameplay code applies one.

### Action conflicts

- Ranged fire cannot start or continue while shielding.
- Raising the shield stops ranged fire immediately.
- Melee temporarily suppresses the shield for the attack and recovery window.
- Dodge cancels the shield immediately.
- Dodge has priority over melee and ranged when it successfully starts.
- Death cancels all active actions.
- Presentation may not delay the moment an accepted input becomes gameplay-active.

### Facing

Facing priority is:

1. right-stick aim above its dead zone;
2. active movement direction;
3. optional small target correction during attack execution;
4. last valid combat-facing direction.

While shielding, movement remains independent. Shield facing begins from the current combat-facing direction and changes only from deliberate right-stick aim. Releasing shield returns to normal facing priority.

### Starting tuning values

All values are exposed in `PrototypeBalance.asset` and are starting points, not promises.

| System | Value |
|---|---:|
| Move speed | 5 units/sec |
| Move acceleration/deceleration | Responsive; tune in play |
| Move dead zone | 0.18 |
| Aim dead zone | 0.25 |
| Player max health | 100 |
| Melee damage | 10 |
| Melee cooldown | 0.3 sec — deliberately equal to the swing, so swings chain with no dead window |
| Melee input buffer | 0.15 sec |
| Melee range / radius | 2.5 / 1.5 units |
| Melee startup / active / recovery | 0.06 / 0.08 / 0.16 sec |
| Melee target correction | Maximum 15 degrees |
| Melee knockback | 4 units |
| Hit-stop on a connecting hit | 0.05 sec unscaled |
| Projectile damage | 8 |
| Projectile speed | 32 units/sec |
| Projectile lifetime | 1.5 sec |
| Projectile sweep radius | 0.12 units |
| Projectile pool size | 32 |
| Fire cooldown | 0.3 sec |
| Shield stability | 100 |
| Shield arc | 120 degrees |
| Light / heavy stability damage | 10 / 30 |
| Lowered shield regeneration | 12/sec |
| Raised idle regeneration | 4/sec |
| Shield break lockout | 2.5 sec — the whole cost of a break |
| Shield stability on return from a break | 30, refilled during the lockout |
| Shield move multiplier | 0.85 |
| Dodge distance / duration | 3 units / 0.2 sec, on a quadratic ease-out |
| Dodge cooldown | 1 sec |
| Dodge invulnerability | 0.2 sec — the whole movement duration |
| Dodge input buffer | 0.15 sec |

### Damage resolution order

Every incoming hit follows one predictable path:

1. Ignore the hit if the receiver is dead or currently invulnerable.
2. If the shield is raised and available, calculate the horizontal direction from player to damage source.
3. Compare that direction against shield facing and half of the shield arc.
4. If inside the arc, consume shield stability, play a block response, and apply no health damage for the POC.
5. Break the shield at zero stability and start its lockout.
6. Otherwise apply health damage, hit feedback, and optional knockback.
7. At zero health, cancel actions and enter death exactly once.

Direction math must be testable without scene objects. Hits whose source is unavailable are unshieldable by default, avoiding accidental omnidirectional blocks.

### Melee query and correction

The melee hit volume is a sphere of the configured radius whose centre sits at `range - radius` in
front of the attacker, so its far edge lands exactly at the configured range. This placement is the
whole rule: nothing behind the attacker is inside it beyond arm's length, and an enemy pressed
against the player still is, which keeps melee usable when something has closed the distance.

The correction cone half-angle and the correction cap are deliberately the same number. A target
inside the cone snaps the swing onto it, a target outside is ignored entirely, and the swing
therefore can never be pulled further than the configured degrees or stolen by an enemy the player
did not aim at. The player is never moved toward a target.

### Projectile travel

A player projectile is a swept query, not a physics body. It carries no rigidbody and no collider;
each step sweeps a sphere of the configured radius from its previous position to its next and
resolves the closest hit against enemies and arena geometry. Tunnelling at speed is therefore
impossible rather than unlikely, and "one projectile resolves one hit" falls out of the design rather
than needing a flag. A projectile that hits nothing recycles when its lifetime runs out.

The pool is fixed size and filled once; sustained fire never allocates. If it is ever exhausted it
recycles the oldest shot in flight rather than dropping the new one, because silently swallowing an
input the player made is the worse failure. The recycle count is on the debug overlay so an undersized
pool is visible rather than invisible.

### Shield blocking

Blocking is a mitigator installed on `Health`, not an interception in each attack. Every hit in the
game keeps calling `IDamageable.ReceiveDamage`; the shield gets first refusal and, if it absorbs,
health is never touched. No attack needs to know a shield exists, which is why melee, projectiles, and
debug damage are all blockable without any of them being changed.

A hit larger than the remaining stability is still blocked in full. The cost of coming up short is the
break, not leaked damage: passing a remainder through would make the moment of breaking impossible to
read.

A break costs exactly the lockout and nothing after it. Stability refills throughout, so when the wait
ends the shield returns with a real charge rather than a token one. The player is punished for one
learnable number of seconds and is then back in the fight, holding a guard they have to spend
carefully — which is more interesting than waiting for a bar to finish filling before they may act.

### Responsiveness rules

The game is arcade and reactive, and these follow from that rather than from any individual system:

- **A cooldown never outlasts its own action.** Melee's cadence equals its authored swing, so one
  swing flows into the next. A cooldown longer than the animation leaves a window where the game is
  visibly idle and still refusing input, which players read as dropped presses.
- **A press made slightly too early is remembered, not discarded.** Melee buffers a press for a short
  window and fires it on the first frame it is legal. Without this, mashing produces fewer attacks
  than metronomic timing, which punishes exactly the player an arcade game should reward. The buffer
  must stay shorter than the cooldown so it forgives an early press without queueing a spare attack.
- **A projectile arrives while the trigger pull still feels connected to it.** A shot that takes
  longer to cross the arena than the gap between shots reads as disconnected from the input.
- **A punishment state is one learnable duration**, not a duration followed by a second condition.

---

## 5. Technical shape

### Runtime ownership

```text
PlayerInputReader
    captures intent only
        ↓
PlayerActionCoordinator
    validates action conflicts and transitions
        ├── PlayerMotor
        ├── PlayerFacing
        ├── MeleeController
        ├── RangedController
        ├── ShieldController
        └── DodgeController

Health ← DamageInfo ← hitboxes / projectiles / enemy attacks
    ↓
PlayerPresentation and HUD listen to state/events
```

Input callbacks must not directly move transforms, find targets, spawn arbitrary objects, or modify health. The coordinator exposes the current allowed action state; individual controllers own their timing and mechanics.

### Recommended player prefab

```text
PF_Vaultbreaker_POC
├── CharacterController
├── PlayerInput
├── Runtime controllers
├── Hurtbox
├── ModelRoot
│   ├── POC body/rig
│   └── Armature (when the Blender rig is introduced)
├── Sockets
│   ├── Socket_RightHand_Melee
│   └── Socket_LeftArm_RangedShield
├── Anchors
│   ├── Muzzle
│   ├── ShieldOrigin
│   ├── MeleeTrail
│   ├── HitVFX
│   └── FeetVFX
└── DebugVisuals
```

Use `CharacterController` for player movement. Keep gameplay movement code-driven and do not use root motion. Resolve dodge displacement through collision-aware movement so the player cannot cross arena walls.

### Enemy composition

Each enemy prefab uses small reusable components:

```text
PF_Enemy_*
├── EnemyBrain
├── EnemyMotor
├── EnemyAttackController
├── Health
├── Hurtbox
├── Collider
├── ModelRoot
└── Telegraph / VFX anchors
```

Use a simple explicit state machine: `Spawn`, `Approach/Reposition`, `Telegraph`, `Attack`, `Recover`, `Hit`, `Dead`. Do not introduce a generic AI framework.

### Data

- `PrototypeBalance`: player, shield, dodge, feedback, and global enemy multipliers.
- `EnemyDefinition`: health, speed, preferred distance, attack damage, timing, stability damage, and prefab/presentation references.
- `WaveDefinition`: ordered spawn entries, spawn point, delay, and count.

ScriptableObjects contain configuration only. Runtime health, timers, cooldowns, and wave state live on scene/prefab instances.

### Layers and collision matrix

Create layers for `Player`, `Enemy`, `PlayerProjectile`, `EnemyProjectile`, `PlayerMeleeHit`, `EnemyAttack`, `Environment`, and optionally `Debug`.

- Player projectiles query/hit enemies and environment only.
- Enemy projectiles query/hit player and environment only.
- Attack triggers do not collide with each other.
- The player and enemies collide with environment.
- Enemies use lightweight separation but do not need perfect crowd physics.
- No tags or string comparisons are used for hot-path faction detection.

### Project layout for the POC

```text
Assets/Vaultbreakers/
├── Art/
│   ├── Characters/{Player,Enemies}/
│   ├── Environments/
│   ├── Materials/
│   ├── VFX/
│   └── Animation/
├── Audio/
├── Data/{Balance,Enemies,Waves}/
├── Input/
├── Prefabs/{Player,Enemies,Projectiles,Zones,UI}/
├── Scenes/{Prototype,Test}/
├── Scripts/
│   ├── Core/
│   ├── Input/
│   ├── Player/
│   ├── Combat/
│   ├── Enemies/
│   ├── Zones/
│   ├── Camera/
│   ├── UI/
│   └── Debug/
├── Settings/
└── Tests/{EditMode,PlayMode}/
```

Begin with one runtime assembly definition and one test assembly definition per test mode. Split further only when compile boundaries produce a clear benefit.

---

## 6. Ordered implementation plan

Every phase ends in a playable or verifiable state. Do not begin the next phase while the current exit gate is red.

### Phase 0 — Preserve and baseline

**Goal:** Know what is ours, what is template content, and whether the editor is healthy before changing it.

Tasks:

1. Record the Unity version and installed package versions in the implementation notes.
2. Inspect the current uncommitted diff; preserve all user-owned changes.
3. Open the project and record all Console errors and warnings.
4. Confirm the project enters and exits Play Mode.
5. Confirm the default input asset and 2D renderer are template assets, not gameplay dependencies.
6. Create a named baseline commit only when the existing changes are understood and the editor is error-free.

Exit gate:

- project opens in Unity `6000.4.4f1`;
- no compile errors;
- user changes are not overwritten;
- baseline state is recoverable in Git.

### Phase 1 — Convert the foundation to 3D URP

**Goal:** Produce a clean 3D test scene before creating gameplay code.

Tasks:

1. Create Universal Renderer Data configured for forward rendering.
2. Assign it as the default renderer in the active URP pipeline asset.
3. Verify depth texture, opaque texture, shadow settings, color space, and HDR choices against a minimal lit cube/sphere test.
4. Create the `Assets/Vaultbreakers` directory structure.
5. Create layers and configure the collision matrix.
6. Create `Prototype_Arena.unity`; do not repurpose `SampleScene`.
7. Build a roughly 20-by-20-unit graybox floor, four walls, player start, three enemy spawn points, and a `ZoneRoot`.
8. Add a fixed orthographic or low-perspective isometric camera near 45-degree yaw and 35-degree pitch. Select one projection through playtesting; orthographic is the initial recommendation for predictable readability.
9. Add one directional light, restrained ambient lighting, and high-contrast graybox materials.
10. Add the prototype scene to Build Settings.

Exit gate:

- opaque 3D meshes, lighting, shadows, depth, and camera render correctly;
- the full walkable arena is visible at 16:9;
- no scene or Console errors;
- a Windows development build launches into the arena.

### Phase 2 — Game-specific input

**Goal:** Convert controller signals into inspectable player intent.

Tasks:

1. Create `VaultbreakersInputActions.inputactions` with `Player` and `UI` maps.
2. Add Move, Aim, Melee, Ranged, Shield, Dodge, Pause, and development Restart actions.
3. Bind the Xbox-style controls in the combat contract.
4. Add keyboard/mouse development bindings.
5. Generate the C# wrapper if the project convention uses generated input classes.
6. Implement `PlayerInputReader` with current vectors and button edge/held state.
7. Add a development overlay that displays connected device, Move, Aim, and held action flags.
8. Test controller disconnect/reconnect without exceptions or stuck held actions.

Exit gate:

- each action changes exactly one visible debug value;
- trigger hold and release are reliable;
- stick dead zones do not cause idle drift;
- keyboard fallback works;
- no combat behavior exists yet.

### Phase 3 — Player locomotion and facing

**Goal:** Make simply moving the character feel good before adding attacks.

Tasks:

1. Create `PF_Vaultbreaker_POC` using a capsule/primitive visual and `CharacterController`.
2. Implement camera-relative movement projected onto XZ.
3. Implement tunable acceleration, deceleration, speed, and collision-aware gravity/grounding as needed.
4. Implement `PlayerFacing` with the locked priority rules.
5. Smooth visual rotation without making gameplay facing lag behind input.
6. Store `LastCombatFacingDirection` and expose it read-only.
7. Add facing, move input, aim input, and velocity gizmos.
8. Clamp play to arena collision; do not silently teleport the player back into bounds.
9. Test cardinal, diagonal, rapid reversal, wall slide, and stick-release behavior.

Exit gate:

- the player starts moving immediately and stops without floatiness;
- movement direction is correct relative to the camera;
- right-stick aim overrides movement without changing travel direction;
- neutral input preserves the last useful facing;
- a new tester can circle the arena comfortably for two minutes.

### Phase 4 — Combat kernel and target dummy

**Goal:** Establish one consistent damage and action-state path before building all four abilities.

Tasks:

1. Implement `DamageInfo`, `IDamageable`/damage receiver, `Health`, and death events.
2. Implement player action-state coordination with explicit conflict rules.
3. Add invulnerability handling and make duplicate death impossible.
4. Create an immobile target dummy with health and reset behavior.
5. Add minimal hit flash, impact marker, and optional damage number.
6. Add a combat event log to the debug overlay.
7. Write EditMode tests for health clamp, invulnerability, and death idempotence.

Exit gate:

- scripted debug damage affects the dummy and player through the same API;
- health never leaves the valid range;
- one lethal hit produces one death event;
- action state is visible and has no contradictory flags.

### Phase 5 — Melee vertical slice

**Goal:** Make the first real attack responsive and readable.

Tasks:

1. Implement a timed melee attack with startup, active, and recovery phases.
2. Use a non-alloc sphere overlap or cast against the enemy layer.
3. Deduplicate targets so one swing damages one enemy once.
4. Apply damage and knockback away from the attacker.
5. Add a small, capped target correction within the configured cone; never move/teleport the player to a target.
6. Add a placeholder weapon at `Socket_RightHand_Melee`.
7. Add a short swing pose/animation, trail, impact flash, and placeholder audio.
8. Add optional short hit-stop that pauses combat presentation consistently without corrupting timers.
9. Add gizmos for query volume and correction cone.
10. Test misses, edge-of-range hits, two targets, cooldown spam, targets behind the player, and attacking against a wall.

Exit gate:

- the hit occurs at the authored active time, not at arbitrary animation completion;
- the same target is hit once per swing;
- input-to-visible-response feels immediate;
- misses are understandable;
- melee remains usable without right-stick aim.

### Phase 6 — Ranged fire and projectile lifecycle

**Goal:** Make hold-to-fire satisfying without creating allocation or aim problems.

Tasks:

1. Implement `RangedController` using held input and a tunable fire cadence.
2. Spawn from the `Muzzle` anchor along combat facing.
3. Create a pooled projectile with speed, lifetime, damage, impact, and environment collision.
4. Ensure a projectile resolves one hit and returns to the pool.
5. Add muzzle flash, readable tracer/projectile shape, impact effect, recoil pose, and placeholder audio.
6. Keep fire cooldown gameplay-authoritative and independent from animation length.
7. Add projectile pool statistics to the debug overlay.
8. Test holding RT, tapping RT, changing aim while firing, firing near walls, missed shots, and pool exhaustion behavior.

Exit gate:

- holding RT produces a stable cadence with no ammo or reload;
- projectiles follow current combat facing;
- projectiles neither tunnel through common targets nor double-hit;
- sustained firing for two minutes does not continually allocate projectiles;
- feedback remains readable across the arena.

### Phase 7 — Directional shield

**Goal:** Prove the defining fire-versus-defense decision.

Tasks:

1. Implement raised, lowered, broken, and recovering shield states.
2. Capture shield-facing behavior on LT press as defined in the combat contract.
3. Implement pure/testable horizontal arc calculation.
4. Route incoming damage through the shield before health.
5. Implement stability damage, lowered/raised regeneration, break lockout, and full recovery.
6. Apply the movement penalty while raised.
7. Stop ranged fire when shield begins and reject fire while shield is raised.
8. Suppress the shield during melee attack/recovery.
9. Add a translucent arc/plane, directional marker, hit ripple, cracks/low-state cue, and break cue. State must be readable by shape and behavior, not only color.
10. Add shield arc and source direction gizmos.
11. Write EditMode tests at center, edge, just outside edge, rear, zero vector, and wraparound angles.

Exit gate:

- frontal attacks block and rear attacks damage health;
- shield facing and movement can differ;
- RT and LT cannot deal damage and block simultaneously;
- melee visibly drops/suppresses the shield;
- shield break is unmistakable and recovery is predictable.

### Phase 8 — Dodge

**Goal:** Add the movement answer to heavy, unblockable, or badly positioned threats.

Tasks:

1. Implement dodge direction from active move input, otherwise last combat facing.
2. Use a short collision-aware movement curve with configurable distance and duration.
3. Apply invulnerability for the configured window.
4. Enforce cooldown and prevent repeated held-button activation.
5. Cancel shield immediately when dodge starts.
6. Define whether dodge interrupts melee recovery after feel testing; default is no cancellation during melee active frames.
7. Add silhouette stretch/pose, ground streak, and placeholder audio.
8. Test into walls, corners, enemies, projectiles, neutral input, held input, and death during dodge.

Exit gate:

- dodge never crosses arena walls;
- invulnerability begins and ends at predictable times;
- LT is no longer active after a dodge starts;
- dodge solves a different problem from shielding;
- repeated button spam cannot bypass cooldown.

### Phase 9 — Enemy teaching set

**Goal:** Validate the player kit against threats designed to teach each response.

#### 9A. Scrap Grunt

- 30 starting health and roughly 3 units/sec movement.
- Approaches directly with lightweight separation.
- Uses a short-range attack with clear windup, active, and recovery phases.
- Tests movement, melee spacing, ranged pressure, ordinary shield blocks, and dodge timing.

#### 9B. Repo Shooter

- Maintains a preferred distance and repositions when crowded.
- Shows a visible aim/fire tell before launching a readable enemy projectile.
- Retreats when the player closes distance.
- Tests shielding while approaching and lateral movement/dodge.

#### 9C. Heavy Bruiser

- Slow approach, large silhouette, long windup, wide heavy strike.
- Deals high stability damage and meaningful health damage.
- Include one clearly signaled attack that is more efficient to dodge than block; whether it is strictly unblockable is a tuning decision, not required initially.
- Tests anticipation, shield resource management, and dodge.

Shared tasks:

1. Implement the small enemy state machine and simple separation.
2. Author each attack with distinct anticipation, active, and recovery timing.
3. Give enemy projectiles a different silhouette/palette from player projectiles.
4. Add death cleanup that cannot leave the wave count stuck.
5. Add spawn/death debug controls.
6. Test each enemy alone before mixed waves.

Exit gate:

- a tester can explain what each enemy is about after one encounter;
- tells are readable without relying only on color;
- no enemy deals damage before its tell;
- each player verb has at least one useful combat situation;
- enemies do not require navigation middleware in the open arena.

### Phase 10 — Arena waves, death, and retry

**Goal:** Turn mechanics into a repeatable five-minute combat test.

Tasks:

1. Implement a minimal `ZoneController` and `WaveSpawner` using wave data.
2. Lock the zone at start, spawn from visible points, and track living enemies by lifecycle events.
3. Build a short progression:
   - Wave 1: three Scrap Grunts;
   - Wave 2: two Scrap Grunts and one Repo Shooter;
   - Wave 3: one Scrap Grunt, one Repo Shooter, and one Heavy Bruiser;
   - optional stress wave: increased counts used only from the debug menu.
4. Add a short delay between waves and a clear banner that does not take control away.
5. On death, reset the active wave, player health, shield, projectiles, transient effects, and enemy state.
6. Do not reload the entire scene unless reset correctness cannot be guaranteed otherwise.
7. Ensure previously completed waves remain completed during the current run, matching `PROJECT.md`.
8. Add manual restart and return-to-wave-one debug commands.

Exit gate:

- Wave 1 clears in under 30 seconds at starting balance;
- death and retry take no more than a few seconds;
- no orphan projectile, enemy, timer, or action survives reset;
- three full runs can be completed without a soft lock;
- camera never loses the player or an active enemy outside the intended stage.

### Phase 11 — HUD, feedback, and accessibility baseline

**Goal:** Make combat state readable without hiding the action.

Tasks:

1. Add health and shield stability bars with numeric/debug alternatives.
2. Add distinct shield-ready, low, broken, and recovering presentation.
3. Add current wave and remaining-enemy display for development; simplify later if unnecessary.
4. Add a minimal pause screen and controller-reconnect prompt.
5. Add toggles for screen shake, hit-stop intensity if exposed, flashes, rumble, and damage numbers.
6. Keep routine VFX short, generally below 0.5 seconds.
7. Validate the arena in grayscale and with bloom disabled.
8. Limit concurrent impact sounds and avoid continuous loud shield loops.
9. Add simple player and enemy ground markers if silhouette tests show they are needed.

Exit gate:

- lethal tells, player position, shield direction/state, enemies, and projectiles remain readable in mixed Wave 3;
- no essential state depends only on red/green or another color pair;
- feedback can be reduced without changing mechanics;
- UI remains readable at 1920x1080 and one lower supported test resolution.

### Phase 12 — Blender validation asset pass

**Goal:** Validate scale, sockets, silhouette, and animation without allowing art production to block combat iteration.

Begin this phase only after Phases 3–8 work with primitives. Blender work may overlap enemy implementation once the mechanical dimensions are stable.

Tasks:

1. Set a shared scale convention: one Unity unit equals one meter; apply transforms before export.
2. Create or adapt one low-poly humanoid placeholder with a simple shared skeleton.
3. Use consistent bone names and a forward-axis/export preset documented beside the `.blend` source.
4. Create short clips for idle, locomotion, melee, ranged recoil, shield hold/hit/break, dodge, hit, and death. These are validation clips, not final animation.
5. Keep motion in-place; gameplay owns translation.
6. Verify the right-hand melee and left-arm ranged/shield attachment points under every clip.
7. Create three low-detail enemy silhouettes whose mass and role read from the game camera.
8. Export FBX files into an import staging folder, then configure Humanoid/Generic rigs consistently in Unity.
9. Keep `.blend` sources outside runtime asset folders or in a clearly named source-art folder; Unity should consume intentional exports.
10. Document source, license, and usage rights for any external model, rig, animation, texture, or audio.

Exit gate:

- player and enemy roles are recognizable at gameplay camera distance;
- attachment points do not drift during any POC animation;
- replacing primitives changes presentation only, not hit timing or movement;
- imported assets use consistent scale, orientation, materials, and rig settings;
- no root-motion dependency has entered player or enemy mechanics.

### Phase 13 — Tests, profiling, build, and playtest gate

**Goal:** Decide with evidence whether combat is strong enough to justify the next systems.

Tasks:

1. Complete the automated test matrix below.
2. Profile the mixed wave in the Editor and a Windows development build.
3. Check for recurring managed allocations during steady-state movement, firing, projectiles, and enemy updates.
4. Run at least five fresh-player sessions with controller as the first input device.
5. Do not explain the controls before the first attempt; observe discoverability.
6. Record metrics, confusion points, favorite/ignored actions, deaths, and requested replays.
7. Tune one variable group at a time in the balance asset.
8. Fix all P0/P1 issues and repeat the playtest.
9. Write a short POC results note with a go/iterate/stop recommendation.

Exit gate:

- development build holds the 60 FPS target on the chosen reference PC during the standard mixed wave;
- no Console errors, soft locks, or recurring hot-loop allocations;
- at least four of five testers complete Wave 1 without instruction;
- at least four of five deliberately use melee, ranged, shield, and dodge by the end of the run;
- at least four of five correctly describe the fire/shield tradeoff;
- median Wave 1 clear time is below 30 seconds;
- at least three of five choose an immediate replay or explicitly ask to try again;
- the team can name no unresolved P0 and no more than an accepted short list of P1 issues.

---

## 7. Test plan

The current automated results and remaining human gate are recorded in `Docs/POC_RESULTS.md`. The tables below retain the original phase-by-phase implementation history.

### EditMode tests

| Area | Required cases | Status |
|---|---|---|
| Shield arc | Front, exact edge, outside edge, rear, 0/360 wrap, missing source | Done — `ShieldSliceTests` |
| Health | Clamp, zero damage, invulnerability, lethal damage, repeated lethal hit | Done — `CombatKernelTests` |
| Action policy | Fire rejected during shield; shield stops fire; melee suppresses shield; dodge cancels shield; death cancels all | Done — `CombatKernelTests`, `RangedSliceTests`, `ShieldSliceTests` |
| Facing | Aim priority, movement fallback, neutral preservation, dead-zone boundaries | Done — `PlayerLocomotionTests` |
| Cooldowns | Exact-ready boundary, held input, reset after death | Done — `MeleeSliceTests`, `RangedSliceTests`, `DodgeSliceTests` |
| Dodge | Distance and curve, frame-rate independence, committed direction, invulnerability window, spam, held button, buffered press, melee active window, death and reset | Done — `DodgeSliceTests` |
| Wave state | Living count, duplicate death event protection, active-wave reset, completed-wave preservation | **Outstanding — needs Phase 10** |

Beyond the required matrix, the suite also pins the melee swing geometry and phase timing, the ranged
cadence, the projectile pool accounting, the generated foundation, the modular avatar, the physics
layer matrix, and the responsiveness rules in section 4.

### PlayMode tests

- Done — Player moves relative to the prototype camera.
- Done — A melee swing hits a target in front and not a target behind.
- Done — Holding ranged input produces repeated shots at the expected cadence.
- Done — Starting shield stops an active firing sequence.
- Done — A front projectile drains shield; a rear projectile drains health.
- Done — Starting melee suppresses an already raised shield.
- Done — Starting dodge cancels shield and grants then removes invulnerability.
- Done — Dodge collision does not pass through a wall, and is contained by a corner.
- **Outstanding (Phase 10)** — A killed enemy decrements the active wave once.
- **Outstanding (Phase 10)** — Player death resets the active wave and all transient combat objects.
- **Outstanding** — Controller disconnect clears held combat input safely. The code path is correct by
  construction and the device-reset behaviour is exercised, but the disconnect itself cannot be
  simulated in batch mode; this one needs physical hardware.

Also covered beyond the required list: a projectile resolves exactly one hit, does not tunnel in a
single long step, is stopped by geometry, expires when it hits nothing, and never grows the pool;
pool exhaustion recycles rather than dropping a shot; a swing hits two targets once each and is
cancelled when a higher-priority action takes the claim; repeated frontal fire breaks a shield and the
next shot then reaches health; a dodge stops held fire and lets it resume by itself, covers its full
distance through the character controller when nothing is in the way, and hands ordinary movement
back when it ends; both generated scenes load and the showcase carries no gameplay components.

### Manual combat scenarios

1. Move only for two minutes; test precision, reversal, edges, and camera-relative direction.
2. Melee only against three grunts; assess contact, range readability, and crowd escape.
3. Ranged only; assess aim, cadence, projectile clarity, and kiting dominance.
4. Shield only against a shooter; test direction, approach, stability, and break recovery.
5. Dodge only against a bruiser; assess tell timing and wall behavior.
6. Mixed tool run; assess whether switching verbs feels natural.
7. Deliberate failure/retry loop repeated three times.
8. Grayscale and reduced-feedback run.
9. Ten-minute soak/stress wave while profiling.

### Issue priorities

- **P0:** Crash, data loss, cannot enter play/build, permanent soft lock.
- **P1:** Incorrect damage, broken input conflict, unavoidable unreadable hit, reset corruption, player leaves arena.
- **P2:** Feel/balance/readability problem that does not invalidate the loop.
- **P3:** Polish, convenience, or content issue outside the combat proof.

---

## 8. Debug and tuning requirements

Create a development-only panel with:

- invulnerability;
- refill health and shield;
- shield break;
- spawn each enemy;
- kill all enemies;
- restart active wave;
- start selected wave;
- player action state;
- move/aim/facing vectors;
- active cooldowns and invulnerability;
- projectile pool active/free counts;
- time scale and optional frame advance;
- screen shake/flash/rumble toggles.

Create Scene-view gizmos for:

- player combat facing;
- shield facing and arc;
- melee query and correction cone;
- enemy detection, preferred range, and attack area;
- spawn points and arena bounds.

Debug code must be disabled or stripped from non-development builds and must never be required for normal play.

---

## 9. Playtest scorecard

Record one row per tester without coaching the first attempt.

| Metric | Record |
|---|---|
| Time to first movement | Seconds |
| Time to first intentional attack | Seconds |
| Time to understand aim/facing | Seconds or not understood |
| Wave 1 clear time | Seconds / failed |
| Melee, ranged, shield, dodge used | Yes/no for each |
| Fire/shield conflict understood | Yes/no and tester explanation |
| Unreadable damage events | Count and description |
| Accidental action conflicts | Count and description |
| Death-to-control retry time | Seconds |
| Camera/player loss | Count |
| Preferred and ignored verb | Names and reason |
| Immediate replay chosen | Yes/no |
| One-sentence feeling | Verbatim note |

After each batch, classify findings as input, timing, balance, readability, camera, feedback, enemy behavior, or reset/reliability. Fix systemic causes before adding content.

---

## 10. Work slices and source-control policy

Use small, reviewable slices. A suggested commit sequence is:

1. `chore: establish 3d urp prototype foundation`
2. `feat: add vaultbreaker input actions`
3. `feat: add player movement and facing`
4. `feat: add combat damage kernel`
5. `feat: add player melee attack`
6. `feat: add pooled ranged attack`
7. `feat: add directional stability shield`
8. `feat: add player dodge`
9. `feat: add prototype enemy roles`
10. `feat: add combat arena waves and retry`
11. `feat: add combat hud and feedback settings`
12. `art: add combat validation models and animation`
13. `test: complete combat poc validation`

For every slice:

1. inspect the current diff and Unity Console;
2. change one coherent behavior;
3. let Unity import and compile;
4. run relevant automated and manual checks;
5. inspect generated `.meta` files and the final diff;
6. commit only an error-free, usable state.

Never commit `Library`, `Temp`, `Logs`, generated builds, or Blender backup files. Never discard existing uncommitted work to make a slice clean.

---

## 11. Risks and controls

| Risk | Early signal | Control |
|---|---|---|
| 2D template leaks into 3D setup | Missing depth/shadows, 2D renderer features | Complete and verify Phase 1 before gameplay |
| Combat feels delayed | Input accepted before visible/gameplay response | Gameplay timing owns attacks; animations follow events |
| Player controller becomes a monolith | Input, damage, visuals, and movement in one script | Maintain responsibility boundaries in Section 5 |
| Shield becomes omnidirectional | Rear attacks block or source direction is ambiguous | Pure arc tests and source-aware damage |
| Twin-stick aim becomes mandatory | Players miss whenever right stick is idle | Movement-facing default and capped soft correction |
| Dodge crosses geometry | Tunneling at walls/corners | Collision-aware displacement and wall tests |
| Ranged kiting dominates | Tester ignores melee and shield | Arena pressure, enemy roles, cadence/damage tuning |
| Effects obscure tells | Player reports unexplained damage | Visual-priority order, short effects, grayscale tests |
| Art blocks iteration | Mechanics wait for a rig or animation | Primitives through Phase 8; animation never owns logic |
| Architecture overbuilds future features | Empty abstractions for cards/co-op/gauges | Implement only the compatibility seams in Section 3 |
| Reset leaves stale state | Orphans, duplicate spawns, stuck wave | Central reset contract and PlayMode coverage |
| Package churn destabilizes project | Import/compiler issues unrelated to combat | No new packages without an explicit need |

---

## 12. Decision checkpoints

### Checkpoint A — Movement

After Phase 3, stop and tune until moving and facing are comfortable. Do not assume attacks will hide bad locomotion.

### Checkpoint B — The combat hook

After Phase 8, use a target dummy and debug damage to answer:

- Is melee immediate and satisfying?
- Is hold-to-fire pleasant for at least a minute?
- Is shield direction obvious while movement remains free?
- Does the fire/shield exclusion create a useful choice rather than frustration?
- Does dodge have a distinct purpose?

If the answer to two or more is no, iterate before enemies.

### Checkpoint C — Enemy readability

After Phase 9, run one-on-one encounters. Each enemy must teach a response without text. If it cannot, change tells and behavior before increasing counts.

### Checkpoint D — POC go/no-go

After Phase 13:

- **Go:** Combat meets the measurable exit gate. Begin the first post-POC slice: gems and persistent gauges.
- **Iterate:** The hook is promising but one or two core verbs fail. Remain in combat scope and retest.
- **Stop/rethink:** Testers consistently fight the controls, cannot read damage, or do not choose a replay after focused iteration.

---

## 13. Post-POC sequence

This is not part of the combat POC backlog. It records the intended order so the team does not jump ahead.

1. Gems, pooling, magnet behavior, caps, and collection window.
2. Persistent liquid gauges and one Relic power.
3. Modular rig/card definitions, three sample cards, and safe-point scanning.
4. One autonomous gem-collector pet.
5. Short `Dock 9 Disaster` vertical slice.
6. Two-player local co-op only after single-player performance and readability remain stable.

The combat POC architecture may support these steps, but no post-POC system should be merged into the combat branch merely because it is listed here.

---

## 14. POC definition of done

The combat POC is done only when all statements are true:

- The project runs as a 3D URP game in Unity and as a Windows development build.
- A controller can move, face, melee, fire, shield, dodge, pause, disconnect, and reconnect safely.
- Player input has an immediate gameplay and visual response.
- Action conflicts match the core combat contract.
- The shield correctly distinguishes frontal and rear attacks.
- The dodge is collision-safe and has reliable invulnerability timing.
- Grunt, shooter, and bruiser roles are mechanically and visually distinct.
- The standard three-wave arena completes, fails, and retries without stale state or soft locks.
- HUD and feedback communicate health, shield, threats, impacts, and wave state without excessive clutter.
- Automated tests cover the deterministic rules and reset-critical flows.
- The mixed wave holds the 60 FPS target on the reference Windows PC with no recurring hot-loop allocation problem.
- The playtest gate in Phase 13 passes.
- All assets have known provenance and any external licenses are recorded.
- The repository has no compile errors and no unintended template/user-file changes.
- A POC results note records what passed, what was tuned, remaining risks, and the recommendation for the next milestone.

Until this definition is met, cards, gauges, pets, and co-op are distractions from the question the POC exists to answer: **is controlling and fighting as a Vaultbreaker fun?**
