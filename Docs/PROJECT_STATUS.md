# Vaultbreakers — Project Status

**Generated:** 2026-08-03
**Unity:** `6000.4.4f1`
**Branch:** `main` (Phases 0–8 preserved in the September 2026 source-control checkpoint)
**Last full validation:** setup tool, EditMode suite (157 tests), PlayMode suite (50 tests), and a
Windows development build, all green in batch mode on 2026-08-03 after the Phase 8 dodge slice.

**Source-control checkpoint, 2026-09-08:** the existing Phase 8 work was preserved alongside a
proprietary root license, README, and asset/dependency license register. EditMode (157/157),
PlayMode (50/50), and a fresh Windows development build passed again with Unity 6000.4.4f1.
No gameplay changes or setup regeneration were made for this checkpoint. See
`Docs/VALIDATION_2026-09-08.md` for evidence and remaining human validation.

This document answers two questions: what actually exists in the repository right now, and what you
can do if you press Play this minute. It deliberately separates *implemented*, *verified by machine*,
and *never checked by a human*.

---

## 1. One-paragraph summary

The project is a tested **foundation with the complete player kit**. Phases 0 through 8 of
`Docs/COMBAT_POC_PLAN.md` are implemented: the project is converted from the 2D URP template to a 3D
Universal Renderer, physics layers and the collision matrix are code-owned, a game-specific input
asset feeds a pull-based input reader, the player moves and turns in a graybox arena with a fixed
isometric camera, a damage/health/action-state kernel drives target dummies, **melee** and
**hold-to-fire ranged** work end to end, the **directional shield** blocks a 120-degree frontal arc
through stability, breaks and recovers, and the **dodge** now bursts three units in a fifth of a
second, invulnerable throughout and stopped by walls rather than passing through them. All four verbs
are wired into one set of conflict rules: raising the shield stops fire on the same frame, melee drops
the shield, and a dodge outranks everything except a swing that is already dealing damage. Tuning
lives in `PrototypeBalance.asset`. **Every verb the POC needs now exists.** What does not exist is
anything to use them against: Phase 9 (the three-enemy teaching set) is the next piece of work, and it
is the largest phase left.

---

## 2. What you can do right now

Open `Assets/Vaultbreakers/Scenes/Prototype/Prototype_Arena.unity` and press Play, or run
`Builds/Vaultbreakers_Phase8/Vaultbreakers.exe` (a development build, so the debug overlays appear).

**Works:**

| You do | What happens |
|---|---|
| Left stick / WASD | The avatar accelerates and moves camera-relative on the ground plane, decelerates to a stop, and slides along the arena walls without leaving the arena. |
| Right stick / arrow keys | Combat facing snaps to the aim direction above a 0.25 dead zone; the model turns smoothly toward it. Movement direction is unaffected. |
| Release everything | The avatar stops and keeps its last useful facing. It does not drift. |
| **X / left mouse** | **Swings.** The weapon winds up, sweeps, and recovers; an orange arc shows the volume the swing queried; a connecting hit deals 10, flashes the target, pops an impact marker, and freezes the game for 0.05 s. A target within 15 degrees of facing is snapped onto. |
| **Mash X** | Four swings a second, chaining with no dead window. A press made during recovery is remembered and fires the moment the next swing is legal, so mashing works instead of eating inputs. Spam still cannot beat the cadence. |
| **Swing at a dummy standing behind you** | Nothing. The volume is in front; misses are visible because the arc is drawn where the query ran. |
| **Swing into the pair of dummies** | Both take damage from one swing, each exactly once. |
| **Hold RT / E** | **Fires.** A cyan tracer leaves the Muzzle anchor every 0.3 s with a flash and a recoil kick, travels at 32 units/sec — across the arena in well under a second — deals 8, and pops a mark where it lands. No ammunition, no reload. |
| **Turn while holding fire** | Each shot follows combat facing at the moment it leaves, not the facing of the first shot. |
| **Fire at a wall, or past everything** | The shot stops at the wall and marks it, or expires after 1.5 s. Either way it goes back to the pool. |
| **Hold LT / right mouse** | **Raises the shield.** A translucent band appears across the 120-degree arc it actually blocks, with a pip on its centre line. Movement drops to 85% but stays free — you can walk anywhere while covering one direction. |
| **Aim while shielding** | The band turns. Movement does not turn it: only deliberate right-stick aim does, which is what lets you back away from the thing you are guarding against. |
| **Hold RT and LT together** | Fire stops the instant the shield goes up and resumes by itself when it comes down. They can never both be active. |
| **Press melee while shielding** | The band drops for the swing and comes back after the recovery, without releasing the button. |
| **Overlay: "Hit player: front" / "rear" / "Heavy: front"** | Front hits cost stability and no health; rear hits cost health and no stability; the heavy button drains 30. The band gets visibly shorter as stability falls, and stutters below a third. |
| **Drain stability to zero, or press "Break shield"** | The band bursts and is gone. A ring at your feet fills across the 2.5 s lockout; when it completes, the guard is back — with about 30 stability, enough for three light hits, refilled while you waited. |
| **Press A / Space** | **Dodges.** A three-unit burst in 0.2 s, fastest on the frame you press it, in whatever direction you are holding — or the way you are facing if the stick is at rest. The avatar squashes and leaves a ground streak showing exactly where it went. You are invulnerable for the whole burst. |
| **Dodge into a wall, or into a corner** | You stop at it. The burst runs its full duration and simply covers less ground; the streak is short, and the overlay's "travelled" figure says how short. Nothing crosses the arena boundary. |
| **Dodge while the overlay hits you** | Nothing lands. "INVULNERABLE" appears on the dodge line for exactly the burst, and no damage event is logged. The frame it ends, hits connect again. |
| **Mash A** | One dodge per second, no more. A press made slightly too early is remembered for 0.15 s and fires the moment the dodge is legal; holding the button down is one dodge, not a stream. |
| **Dodge while shielding or firing** | The shield drops and fire stops on the frame the dodge starts. A held trigger picks fire back up by itself when the burst ends; the shield needs no re-press either. |
| **Dodge in the middle of a swing** | During windup or recovery, the swing is cancelled and the dodge goes. During the 0.08 s the swing is actually dealing damage, the dodge waits — and then fires by itself the moment the window closes, rather than eating the press. |
| **Dodge while running** | Exactly three units, the same as from standing. Ordinary movement is suspended for the burst so your speed cannot add to it, and comes back the frame it ends. |
| Walk up to the three dummies | One at (0, 4) with a partner beside it, one flanking at (-4.5, 1.5). Each has 50 health, flashes red, and revives 1.5 s after dying. |
| Look at the top-left overlay | Live device name, Move and Aim vectors, and held state for all four action buttons. |
| Look at the second overlay | Player and dummy health, the action-state flags, the live melee phase, cooldown and hit count, the ranged firing state and shot count, projectile pool active/free/recycled counts, the shield state with its stability, and the dodge state with its cooldown, distance travelled, and invulnerability. Blocks and shield state changes are written to the event log, which is the only place a blocked hit shows up — it never reaches `Health`, so it raises no damage event. |
| Click the overlay buttons | Damage or kill the dummy, hit the player from the front, the rear, or with a heavy blow, break the shield outright, or reset player, dummy, action state and shield together. |
| Plug/unplug a controller mid-session | No exceptions and no stuck held actions — by construction; see section 5, this has not been exercised with real hardware. |

Open `Assets/Vaultbreakers/Scenes/Test/Avatar_Showcase.unity` and press Play:

| You do | What happens |
|---|---|
| Nothing | The avatar turntables under a three-point light rig. |
| Press `1` | Equips the default Scrapper loadout across all six slots. |
| Press `2` | Equips the alternate Sentinel/Bulwark loadout across all six slots. |

**Does not work yet (by design — later phases):**

- **Pause (Start / Esc)** and **Restart (R)** are read but not handled.
- There are **no enemies, no waves, no HUD, and no retry loop**. The dummies never fight back, so the
  shield and the dodge can only be exercised against the overlay's directed-hit buttons. This is now
  the binding constraint on the design rather than on the code: the Phase 8 gate item "dodge solves a
  different problem from shielding" is unanswerable until there is something worth dodging. The avatar
  slides without a walk cycle; the swing, recoil, and dodge poses are procedural, not animated.
- **Nothing declares heavy stability damage in play.** `DamageInfo.StabilityDamage` carries it and the
  overlay's heavy button uses it, but the 30-point value is a reference for the Phase 9 bruiser.
- **Knockback is computed and delivered** through `DamageInfo.Knockback`, but nothing in the arena can
  be pushed: the dummies are immobile by design. It becomes visible with the Phase 9 enemies.

---

## 3. Implementation status by plan phase

| Phase | Goal | Status |
|---|---|---|
| 0 — Preserve and baseline | Known-good starting point | **Complete** |
| 1 — Convert foundation to 3D URP | Clean 3D test scene | **Complete** |
| 2 — Game-specific input | Controller signals to inspectable intent | **Complete** |
| 3 — Locomotion and facing | Movement that feels good | **Code complete, human feel checkpoint deferred** |
| 4 — Combat kernel and dummy | One consistent damage path | **Complete** |
| 5 — Melee vertical slice | First real attack | **Code complete, human feel checkpoint deferred** |
| 6 — Ranged and projectiles | Hold-to-fire | **Code complete, human feel checkpoint deferred** |
| 7 — Directional shield | Fire-versus-defense decision | **Code complete, human feel checkpoint deferred** |
| 8 — Dodge | Movement answer to threats | **Code complete, human feel checkpoint deferred** |
| 9 — Enemy teaching set | Grunt, Shooter, Bruiser | **Not started — next up** |
| 10 — Arena waves, death, retry | A repeatable five-minute test | Not started |
| 11 — HUD, feedback, accessibility | Readable combat state | Not started |
| 12 — Blender validation asset pass | Animation and enemy silhouettes | Partly satisfied — see below |
| 13 — Tests, profiling, build, playtest gate | Go / iterate / stop | Not started |

Five exit gates are open on feel alone: Phase 3's two-minute controller session, Phase 5's
"input-to-visible-response feels immediate", Phase 6's "hold-to-fire is pleasant for at least a
minute", Phase 7's "shield break is unmistakable and recovery is predictable", and Phase 8's "dodge
solves a different problem from shielding". No test can answer any of them. See
`Docs/IMPLEMENTATION_NOTES.md`. Every mechanical check passes — direction, facing priority, wall
collision, swing timing, deduplication, cooldowns, cadence, tunnelling, pool accounting, every arc
boundary, and the dodge's distance, curve, invulnerability window and wall behaviour — but **do not
treat Phase 3, 5, 6, 7, or 8 as signed off.**

Phase 8's gate is the one that cannot be closed by a controller session alone. Four of its five items
are mechanical and tested; "dodge solves a different problem from shielding" is a comparison between
two answers to a threat, and there are no threats yet. It carries over to Phase 9.

Checkpoint B in the plan asks whether the fire/shield exclusion creates a useful choice rather than
frustration. All the verbs it needs now exist, so Checkpoint B is answerable for the first time — but
only by a human with a controller.

### What is left to reach a functional POC

Two different finish lines, and it is worth keeping them apart.

**A playable loop** — fight waves, die, retry — needs Phases 9 and 10:

| Phase | What is missing | Rough size | What already exists to make it cheaper |
|---|---|---|---|
| 9 — Enemies | Grunt, Shooter, Bruiser: state machine, separation, telegraphs, enemy projectiles | **Largest — roughly Phases 5, 6 and 7 combined** | The damage kernel, layers and query masks are done, and enemy attacks will be blockable by the shield and dodgeable without changes to any of them. Needs `EnemyDefinition`, an enemy projectile pool, and three distinct attack authorings. |
| 10 — Waves and retry | `ZoneController`, `WaveSpawner`, wave data, banner, death and retry | Medium | `ZoneRoot` and three spawn points exist. Much of the reset contract is already built and tested: `Health.ResetHealth`, `ResetPerformed`, `PlayerActionCoordinator.ResetState`, `ShieldController.ResetShield`, `DodgeController.ResetDodge`, `ProjectilePool.ReleaseAll`. The Restart input is already read. |

**The plan's definition of done** (section 14) additionally needs:

| Phase | What is missing | Rough size | Notes |
|---|---|---|---|
| 11 — HUD and accessibility | Health and stability bars, wave display, pause screen, feedback toggles, grayscale pass | Medium | Nothing built; only development-only IMGUI overlays exist. uGUI is installed. The Pause input is read but unhandled. |
| 12 — Art validation | Animation clips, three enemy silhouettes | Medium, art-gated | **Partly satisfied already**: the scale convention, shared skeleton, sockets and player silhouette exist and are validated. What is missing is animation — the avatar slides, and the swing and recoil poses are procedural — and the enemy models. |
| 13 — Playtest gate | Profiling, five fresh-player sessions, tuning, results note | Needs people | This phase can legitimately fail; "iterate" and "stop" are stated outcomes. |

**Blocked on a human, and on the critical path:** the five deferred feel gates plus Checkpoint B, all
answerable in one controller session except Phase 8's "different problem from shielding", which needs
enemies first; Checkpoint C once enemies exist; and Phase 13's five fresh-player sessions. No amount
of implementation gets past these.

**One decision worth taking before Phase 9:** the projectile pool is player-only. Enemy projectiles
need either a second pool or a small generalisation, and that is cheaper to choose deliberately now
than to discover halfway through the phase.

---

## 4. What exists in the repository

### Runtime code — `Assets/Vaultbreakers/Scripts/` (`Vaultbreakers.Runtime` assembly)

| File | Responsibility |
|---|---|
| `Core/GameLayers.cs` | Physics layer indices from `PROJECT.md` 5.5 and the query masks built from them. No tag or name comparisons anywhere in gameplay. |
| `Core/PrototypeBalance.cs` | Central tuning ScriptableObject: locomotion, player health, melee, ranged, shield, feedback. Configuration only. Sections are added as their phase lands. |
| `Input/PlayerInputReader.cs` | Pull-based intent: Move, Aim, four held actions, four edge actions, current device. Translates nothing. |
| `Player/PlayerMotor.cs` | Camera-relative XZ movement on a `CharacterController` with acceleration, deceleration, and grounding. Runs at execution order −20. Exposes two independent seams: a speed multiplier the raised shield uses to slow the player without editing the tuned speed, and a movement suspension the dodge uses to take horizontal displacement over outright. Separate owners, so neither can stomp the other. |
| `Player/PlayerFacing.cs` | Facing priority (aim → movement → last valid). Gameplay facing in `Update` at order −19; visual smoothing in `LateUpdate`. |
| `Combat/DamageInfo.cs` | `DamageInfo` and `DamageResult`. Captures the source position at construction so shield maths later works without a live attacker. |
| `Combat/IDamageable.cs` | The single entry point every hit in the game passes through. |
| `Combat/Health.cs` | Clamped health, invulnerability, exactly-once death, reset. Raises events; records no diagnostics itself. |
| `Combat/PlayerActionCoordinator.cs` | Action-conflict rules and state flags. Owns no timing or mechanics. |
| `Combat/MeleeSwing.cs` | Pure swing geometry: query placement, hit test, capped target correction, knockback direction. No scene, no colliders. |
| `Combat/MeleeController.cs` | The swing state machine. Gameplay-authoritative timing, one non-allocating overlap, per-swing deduplication, and it cancels itself the moment the coordinator revokes its claim. |
| `Combat/MeleePresentation.cs` | Placeholder feedback only: procedural swing pose on the equipped weapon, an arc drawn where the query actually ran, generated audio. Removing it changes nothing about what a swing hits. |
| `Combat/HitStop.cs` | Short freeze on a connecting hit, on an unscaled timer, restoring the exact timescale it captured. |
| `Combat/Projectile.cs` | One pooled shot. No rigidbody and no collider: each step sweeps a sphere along its own path, which is what makes tunnelling and double-hits impossible rather than unlikely. |
| `Combat/ProjectilePool.cs` | Fixed-size pool, filled once. Drives every projectile from one Update, and steals the oldest shot rather than dropping a new one if it is ever exhausted. |
| `Combat/RangedController.cs` | Hold-to-fire. The cadence carries its own remainder so it cannot drift, and it drops fire the frame the coordinator revokes its claim. |
| `Combat/RangedPresentation.cs` | Muzzle flash, recoil kick, impact marks, generated audio. Downstream of the controller and the pool; removing it changes no cadence, aim, or damage. |
| `Combat/PlaceholderAudio.cs` | Generates the POC's combat cues, seeded by name so they are identical on every run. Shared by all three presentations. |
| `Combat/IDamageMitigator.cs` | First refusal on a hit before it reaches `Health`. The seam the shield uses, so no attack in the game needs to know shields exist. |
| `Combat/ShieldArc.cs` | Pure horizontal arc test. A hit with no known source is unshieldable by design, which is what stops the shield becoming omnidirectional by accident. |
| `Combat/ShieldController.cs` | Lowered, raised, broken, recovering. Owns stability, regeneration, the break lockout, the movement penalty, and shield facing — which starts from combat facing and afterwards answers only to aim. |
| `Combat/ShieldPresentation.cs` | Procedural band spanning the true arc, height driven by stability, a burst on break, and a ring that fills across the lockout. Shape and behaviour, not colour. |
| `Combat/DodgeController.cs` | The dodge burst. Displacement is a schedule of total distance on a quadratic ease-out, moved through `CharacterController` so walls and corners stop it without this code knowing they exist. Owns the invulnerability window, the cooldown, the input buffer, and the one rule that outranks it — a melee swing already dealing damage. |
| `Combat/DodgePresentation.cs` | Squash pose on the avatar, a ground streak drawn along the path the burst actually took, and generated audio. Downstream of the controller; removing it changes no distance, timing, or invulnerability. |
| `Combat/TargetDummy.cs` | Practice target: hit flash, pooled impact marker, automatic revive. |
| `Equipment/EquipmentSlot.cs`, `EquipmentModule.cs`, `ModularAvatar.cs` | Visual equipment swapping by stable data ID, with the default loadout baked into the prefab. |
| `Equipment/AvatarSocketRegistry.cs` | Serialized socket lookup by `AvatarSocketId`. Nothing searches the hierarchy at runtime. |
| `Debug/InputDebugOverlay.cs`, `CombatDebugOverlay.cs`, `CombatEventLog.cs` | Development-only overlays. The combat overlay owns the event log and subscribes to `Health`. |
| `Debug/ModularAvatarShowcase.cs` | Turntable and loadout hotkeys for the showcase bench. |

### Editor tooling — `Assets/Vaultbreakers/Editor/` (`Vaultbreakers.Editor` assembly)

Everything generated is reproducible from one menu item:
`Vaultbreakers > Setup > Build 3D Foundation and Modular Avatar`.

| File | Responsibility |
|---|---|
| `VaultbreakersProjectSetup.cs` | Menu/batch entry points, step order, folder creation. |
| `VaultbreakersSetupPaths.cs` | Every generated asset path, the default loadout, and the required socket list. |
| `VaultbreakersDataSetup.cs` | Creates `PrototypeBalance.asset` if it is missing, and re-serialises it otherwise so fields added by a later phase actually appear in the file. It never changes a value: the values in it are playtest results. |
| `VaultbreakersProjectileBuilder.cs` | Builds `PF_PlayerProjectile`. Contributes a component, a layer, and a tracer silhouette — no collider, because the projectile sweeps its own path. |
| `VaultbreakersPhysicsSetup.cs` | Layer names and the collision matrix, declared as an allow-list in code. |
| `VaultbreakersRenderSetup.cs` | 3D Universal Renderer, URP asset, quality levels, colour space, and every material. Opaque or transparent follows from the base colour's alpha, because setting an alpha on an opaque URP material silently does nothing. |
| `VaultbreakersAvatarBuilder.cs` | FBX import contract, module grouping from exported names, socket binding, prefab assembly. |
| `VaultbreakersSceneBuilder.cs` | All three scenes and the build-settings list. |
| `VaultbreakersSetupValidation.cs` | Post-build gate: pipeline, layers, input maps, balance asset, projectile prefab, player prefab components, tuning actually taken from the balance asset, module swaps, socket placement, materials. |
| `VaultbreakersPreviewCapture.cs` | Documentation screenshots. Needs a real graphics device and refuses to run without one. |

### Generated assets

- `Settings/VaultbreakersURP.asset` + `VaultbreakersUniversalRenderer.asset` — assigned as the default
  pipeline and on every quality level.
- `Input/VaultbreakersInputActions.inputactions` — `Player` map with all eight actions; `UI` map is an
  empty stub.
- `Data/Balance/PrototypeBalance.asset` — the single tuning asset. `PlayerMotor`, `PlayerFacing`, and
  `MeleeController` copy from it at configure time; their serialized fields are fallbacks for bare
  test scenes, never a second source of truth.
- `Art/Materials/VB_*.mat` — sixteen URP Lit graybox, character, and effect materials. All opaque
  except `VB_ShieldArc` and `VB_DodgeStreak`, which are translucent so neither hides the threat
  behind it.
- `Prefabs/Projectiles/PF_PlayerProjectile.prefab` — the pooled player shot and its cyan tracer.
- `Art/Characters/Player/Vaultbreaker_Modular.fbx` — generated from
  `Tools/Blender/generate_modular_vaultbreaker.py`.
- `Prefabs/Player/PF_Vaultbreaker_POC.prefab` — the playable avatar: input, controller, motor, facing,
  health, action coordinator, melee controller and presentation, hit-stop, projectile pool, ranged
  controller and presentation, shield controller and presentation, dodge controller and presentation,
  modular avatar, socket registry.
- `Scenes/Prototype/Prototype_Arena.unity` — 20×20 graybox arena, four walls, player start, three spawn
  points, `ZoneRoot`, three target dummies, fixed orthographic isometric camera, lighting.
- `Scenes/Test/Avatar_Showcase.unity` — turntable bench with the gameplay components stripped from the
  avatar instance.
- `Scenes/Test/Empty_TestBed.unity` — a camera and a light and nothing else. PlayMode tests that build
  their own rig load this first so no leftover scene answers their physics queries.

All three scenes are enabled in Build Settings.

---

## 5. Verification — what is actually proven

**Automated, re-runnable, currently green:**

- **157 EditMode tests** — `Logs/EditModeResults.xml`
  - combat kernel: clamping, invulnerability, death idempotence, death-before-`Damaged` ordering,
    action conflicts, stop transitions, reset;
  - melee geometry: query placement, edge of range, rear rejection, point-blank acceptance, height
    independence, correction inside/outside the cone, the correction cap swept across ±90°, zero
    vectors, knockback direction;
  - melee timing: startup/active/recovery at the authored times, the attack claim taken and released,
    spam refused, the next swing allowed on the frame the last one ends, a press queued during
    recovery firing on the first legal frame, and a stale press expiring instead of firing late;
  - the responsiveness rules themselves, asserted against `PrototypeBalance`: the melee cadence equals
    the swing, the input buffer is shorter than the cadence, a break costs one wait and returns a
    shield worth more than a single hit, and a projectile crosses the arena in comfortably less time
    than the gap between shots;
  - ranged cadence: four shots in the first second, no drift across ten seconds, a tap is one shot, a
    released trigger banks nothing, and shield, dodge, and death each stop fire and let it resume;
  - shield arc at every boundary the plan names: dead centre, the exact edge, half a degree past it,
    directly behind, the wrap through north, a purely vertical offset, a source standing on the
    defender, a shield with no facing, and a hit with no known source;
  - shield state: raise and lower claim and release the action, a frontal hit costs stability and no
    health while a rear hit does the reverse, a hit larger than the remaining stability is still
    blocked in full, the break locks out for five seconds without regenerating and then refills before
    it can be raised again, recovery never appears to go backwards, regeneration is faster lowered
    than raised, raising applies the movement penalty without touching the authored speed, melee and
    dodge both drop it, death drops a raised shield but does not repair a broken one, reviving
    restores a whole one, and an invulnerable player spends no stability;
  - dodge: the burst covers the authored distance exactly and covers the same distance in one huge
    frame as in twelve small ones, the curve front-loads and is clamped outside its range, the
    direction is committed at input and does not bend when the body turns, invulnerability begins on
    the frame the dodge does and ends with it, a dodge takes no damage during its window and is
    hittable again immediately after, invulnerability the dodge did not own is restored rather than
    cleared, the cooldown is refused a frame early and allowed on the exact frame it ends, three
    seconds of mashing gives exactly three dodges and a held button gives one, an early press is
    buffered and a stale one expires, the claim is taken and released, a raised shield drops on the
    same frame, a swing in startup is cancelled but a swing in its active window makes the dodge wait
    and then fire, a dead player cannot dodge, a cancelled dodge still owes its cooldown, and both
    death and a health reset leave nothing invulnerable or stuck;
  - `PrototypeBalance` ships the documented starting values, and all three tuned controllers are shown
    to read the asset rather than their own fallbacks by tuning the asset away from the defaults;
  - facing and camera-relative movement maths;
  - input asset maps, bindings, keyboard aim composite, single-application of the aim dead zone;
  - physics layers and the full collision matrix, asserted against the design rules rather than the
    setup table;
  - generated foundation: pipeline, import contract, skeleton, sockets, modules, materials, scale,
    handedness, prefab defaults;
  - modular avatar equip/unequip rules and the debug event log.
- **50 PlayMode tests** — `Logs/PlayModeResults.xml`
  - both scenes load; arena geometry, colliders, layers, and markers exist;
  - the camera frames the whole arena at 16:9 within the isometric pitch range;
  - the player cannot be pushed through the east wall;
  - player and dummy take damage through the same API;
  - a swing damages a target in front and not one behind; each target exactly once per swing; two
    targets in one volume; a target past the range is missed; the swing snaps onto a target inside the
    cone; knockback points away and stays horizontal; only the enemy layer is queried; spam produces
    exactly three swings per second; death and a higher-priority action both cancel a running swing;
    hit-stop always restores the timescale;
  - a projectile damages a target and returns to the pool; it resolves one hit and does not carry on
    into the body behind it; a single 10-unit step does not tunnel past a target; a wall stops it; a
    shot that hits nothing expires; ten seconds of held fire never changes the pool size or recycles
    under pressure; exhaustion steals the oldest shot rather than dropping a new one; each shot
    follows the facing at the moment it left;
  - a real projectile arriving at a raised shield from the front drains stability and leaves health
    whole; the same shot from behind does the reverse; a lowered shield blocks nothing; ten frontal
    blocks break it and the eleventh reaches health;
  - raising the shield stops an active firing sequence through the real controllers and lowering it
    lets held fire resume by itself; starting melee drops a raised shield and it returns after the
    swing; a raised shield slows movement without changing the authored speed;
  - a dodge is stopped by a wall instead of crossing it, is contained by a corner from the inside,
    covers its full distance through the character controller when nothing is in the way, drops a
    raised shield with the button still held and gives back the movement penalty, stops an active
    firing sequence and lets a held trigger resume by itself afterwards, suspends ordinary movement
    for its duration and hands it back, and ignores damage for the whole burst and not after it;
  - the arena player's own melee and ranged controllers both damage the arena's own practice dummy,
    and its own shield blocks from the front but not from behind;
  - the showcase avatar carries no gameplay, melee, ranged, pool, shield, dodge, or timescale
    components;
  - every equipment combination swaps at runtime without losing a socket.
- **Zero compiler warnings** across all four assemblies.
- **Windows development build** produces a running executable — `Builds/Vaultbreakers_Phase8/`.

**Not verified — needs a human, a controller, or a display:**

- Controller feel: acceleration, stopping, reversal, wall sliding, dead zones. This is the deferred
  Phase 3 checkpoint and is the highest-value next validation.
- Melee feel: whether the 0.06 s startup reads as immediate, whether the 0.3 s cadence is satisfying,
  whether the arc and hit-stop make contact legible, and whether misses are understandable. This is
  the Phase 5 exit gate and no test can answer it.
- Ranged feel and readability: whether holding fire is pleasant for a minute, and whether the tracer,
  flash, and impact marks stay readable at the far side of the arena. This is the Phase 6 exit gate.
- Shield readability and the cost of a break: whether the band's direction is obvious at gameplay
  camera distance, whether a break is unmistakable, and whether 2.5 seconds is the right punish. This
  is the Phase 7 exit gate.
- Dodge feel: whether three units is far enough to escape, whether 0.2 seconds reads as a burst, and
  whether a one second cooldown makes it a decision. This is most of the Phase 7 gate's sibling for
  Phase 8; the mechanical half is fully tested.
- Whether the dodge solves a different problem from the shield. This is the one Phase 8 gate item that
  a controller session cannot close either, because it is a comparison between two answers to a threat
  and there are no threats yet. It carries into Phase 9.
- Whether the fire-versus-defence exclusion is a useful choice rather than a frustration. This is
  plan Checkpoint B, and Phase 7 is the first point at which it can be asked at all.
- Frame-rate behaviour under sustained fire. The pool is proven not to grow, which is the allocation
  half of the Phase 6 gate; the 60 FPS half needs a profiler on a real display.
- Placeholder audio. Every cue is generated at runtime; every automated run so far used
  `-batchmode -nographics`, where nothing was audible.
- Anything visual: lighting, shadows, material readability at the fixed camera distance, silhouette
  legibility. Every automated run so far used `-nographics`.
- Physical controller disconnect/reconnect. The code path is correct by construction (the device is
  reset rather than the action maps being disabled) but has not been exercised with real hardware.
- Frame rate against the 60 FPS target.

---

## 6. Cleanup pass of 2026-08-02

The following bugs and incoherences were found and fixed in this pass. Recorded here so the same
ground is not re-covered.

### Bugs fixed

1. **The aim dead zone was applied twice.** The input asset carried `StickDeadzone(min=0.25)` on Aim
   *and* `PlayerFacing` applied a 0.25 threshold. `StickDeadzone` rescales the range above its
   minimum, so the real threshold sat near 0.44 raw deflection while both places read as "0.25".
   The asset processor was removed; `PlayerFacing` is now the single, testable owner.
2. **Keyboard aim was bound to `<Pointer>/delta`.** Pointer delta is relative motion, so facing only
   updated while the mouse was moving and its magnitude depended on how fast it moved. Replaced with a
   four-way arrow-key composite. Mouse world aim is deferred; it needs a camera-to-ground projection,
   which does not belong in the input reader.
3. **`Health` committed death after announcing the hit.** A `Damaged` listener on the lethal hit saw a
   body at zero health that still reported `IsDead == false`. `IsDead` is now final before `Damaged`
   is raised, and this ordering is covered by a test.
4. **`PlayerInputReader` disabled and re-enabled the entire input asset** on device change and on
   `OnDisable`. That re-enabled the `UI` map alongside `Player` and fought `PlayerInput` for lifecycle
   ownership. Replaced with `InputSystem.ResetDevice`, which is what actually prevents a latched
   trigger, and the map lifecycle was handed back to `PlayerInput`.
5. **`PlayerFacing` could consume the previous frame's movement direction.** Both it and `PlayerMotor`
   ran in `Update` with undefined relative order. Explicit execution orders now guarantee motor first,
   and visual smoothing moved to `LateUpdate` so presentation cannot delay gameplay facing.
6. **The showcase scene was running the full gameplay stack.** The avatar there kept `PlayerInput`,
   `PlayerMotor`, `PlayerFacing` and a `CharacterController`, so WASD drove it off the turntable and
   facing fought the turntable rotation. The setup tool now strips the gameplay components from that
   instance, and a PlayMode test holds the line.
7. **`TargetDummy` allocated a primitive per hit** (`CreatePrimitive` plus `Destroy`) and started an
   overlapping flash coroutine per hit, so rapid hits cut each other's flash short. Replaced with one
   pooled marker and plain timers — this matters before Phase 6 starts firing three shots a second.
8. **`CombatEventLog` kept its entries across play sessions** when domain reloading is disabled. It now
   clears itself on load.
9. **Unity fake-null was mishandled.** `??=` and `?.` on `UnityEngine.Object` fields bypass Unity's
   lifetime check, so a destroyed reference would be treated as alive. Replaced with explicit
   `== null` checks in `PlayerFacing`, `TargetDummy`, and `CombatDebugOverlay`.

### Separation and cleanup

- **`VaultbreakersProjectSetup.cs` was 841 lines** doing folders, pipeline, materials, import, prefab
  assembly, two scenes, validation, and screenshots. Split into seven focused files; the largest is
  now 289 lines and the orchestrator is 73. The batch entry points (`BuildAll`,
  `CaptureShowcasePreview`) are unchanged, so existing scripts keep working.
- **`Health` depended on the debug namespace** and formatted a log string on every hit. Combat no
  longer references `Vaultbreakers.Debugging`; `CombatDebugOverlay` subscribes to health events and
  does the formatting, so a build without the overlay does none of it.
- **`PF_Vaultbreaker_Visual.prefab` was renamed to `PF_Vaultbreaker_POC.prefab`.** It carries a
  `CharacterController`, `PlayerInput`, `Health`, and the action coordinator — calling it "Visual" was
  actively misleading, and the plan already named it `PF_Vaultbreaker_POC`. The arena instance is now
  called `Player` instead of `PlayerVisual_POC`.
- **`PlayerActionState.Interacting`** was removed. It was never set, and interaction is post-POC scope.
  `Stunned` and `ShieldBroken` were kept and documented — both belong to phases inside POC scope and
  are already read by the conflict rules.
- **A misleading test name** — `PlayerPrefab_TranslatesInputWithoutGameplayComponents` asserted the
  presence of gameplay components — was corrected.
- **Deprecated `FindObjectsSortMode` API** removed from the PlayMode suite; the build is now
  warning-free.
- **Stale `Assembly-CSharp.csproj` / `Assembly-CSharp-Editor.csproj`** deleted. Every script lives
  under an assembly definition, so neither is referenced by `VaultBreakers.slnx`.
- **Documentation reconciled**: the plan's keyboard binding table now matches the asset, the pipeline
  doc points at the renamed prefab and records the showcase stripping, and the implementation notes
  record both deliberate deviations with their reasoning.

### Deliberately left alone

- `GameLayers.PlayerTargets` and `Blocking` are unused today but are the query masks Phases 6 to 9
  will need immediately, and they exist so no hot-path code resorts to tag comparisons. `EnemyTargets`
  is now in use by the melee query.
- `PlayerInputReader`'s unused edge-state properties are the stated Phase 2 deliverable.
- The empty `UI` action map is a required stub for the input contract.
- `Docs/PROJECT.md` (the full design) was not touched; it describes the whole game, not the POC.

---

## 7. Phase 5 pass of 2026-08-02

### What was built

- `PrototypeBalance.asset` and its ScriptableObject, holding the locomotion, player-health, melee, and
  feedback values from the plan's tuning table. `PlayerMotor`, `PlayerFacing`, and `MeleeController`
  copy from it when configured; their serialized fields remain only as fallbacks so a bare test scene
  still behaves sensibly. The setup tool creates the asset when it is missing and **never overwrites
  it**, because its contents are playtest results rather than generated data.
- `MeleeSwing`, the pure swing geometry, and `MeleeController`, the state machine that uses it.
- `MeleePresentation` and `HitStop` for feedback, both strictly downstream of gameplay timing.
- Two extra target dummies in the arena, so one swing hitting two bodies and a target that has to be
  turned toward are both reachable by hand and not only by a test.

### Decisions worth knowing

1. **The swing direction is committed when the input is accepted, not when the active window opens.**
   Correcting at input time is what makes the strike feel like it went where the player pointed. The
   *hit* still resolves at the authored active time, which is what the exit gate asks for.
2. **The correction cone and the correction cap are the same number.** A target outside the cone is
   ignored entirely rather than dragging the swing 15° toward it, which is what stops an enemy the
   player never aimed at from stealing a strike.
3. **Hit-stop uses `Time.timeScale`, and melee timers use scaled time.** The swing therefore pauses
   with the rest of the simulation instead of drifting past it. `HitStop` restores the exact value it
   captured and releases on `OnDisable`, so it can never leave the game frozen. A pause screen will
   have to coordinate with it in Phase 11.
4. **Knockback is computed and delivered but nothing consumes it yet.** `DamageInfo.Knockback` carries
   an away-facing horizontal impulse; the practice dummies are immobile by design, so it becomes
   visible with the Phase 9 enemies. It is tested at the `DamageInfo` boundary rather than faked.
5. **Placeholder audio is generated at runtime** rather than imported, so the slice has swing and
   impact cues without adding assets of unknown provenance to the repository.

### Bug fixed while integrating

- **A running swing did not notice when the action coordinator revoked its claim.** The coordinator is
  the arbiter of action conflicts, but `MeleeController` only asked it for permission at the start.
  A higher-priority action taking the attack mid-swing — dodge in Phase 8, or a stun — would have left
  the swing to finish its active window and deal damage during an action that had already cancelled
  it. The controller now cancels itself on the same frame its claim disappears, and a PlayMode test
  holds the line before Phase 8 arrives.

---

## 8. Phase 6 pass of 2026-08-02

### What was built

- `Projectile`, `ProjectilePool`, `RangedController`, and `RangedPresentation`, plus the generated
  `PF_PlayerProjectile` prefab and the `VB_ProjectileCore` material.
- `Empty_TestBed.unity`, a deliberately empty scene the self-contained PlayMode tests load first.
- `PlaceholderAudio`, extracted from `MeleePresentation` so both attack presentations share one
  generator instead of keeping two copies of the same synthesis code.

### Decisions worth knowing

1. **A projectile has no rigidbody and no collider.** Each step sweeps a sphere from where it was to
   where it is going. That is what makes tunnelling and double-hits *impossible* rather than merely
   unlikely at any speed the game can reach, and it removes the need for continuous collision
   detection, trigger
   callbacks, or a physics body per shot. The collision matrix still describes the projectile layers
   correctly for anything later that does want to collide with them.
2. **The pool drives every projectile from one Update.** Thirty-two shots in flight cost one call, not
   thirty-two, and the step is exposed so travel and impact can be tested at an exact delta.
3. **Exhaustion steals the oldest shot in flight rather than dropping the new one**, and counts it.
   Silently swallowing an input the player made is the worse failure; a visible counter makes an
   undersized pool obvious instead of invisible. At the shipped cadence and lifetime it cannot happen.
4. **The cadence carries its own remainder forward** so it does not drift when the cooldown is not a
   whole number of frames, but it is clamped while the trigger is up so a long pause cannot bank
   shots and empty a burst on the next tap.
5. **Position comes from the Muzzle anchor, direction from gameplay facing.** The anchor rides the
   smoothed visual model, so taking direction from it as well would let a presentation-only turn bend
   a shot.

### Bug fixed

- **The PlayMode suite was not isolated, and one test was passing for the wrong reason.** PlayMode
  tests share one scene manager and one physics world, and the tests that load a generated scene leave
  it loaded. The arena's own practice dummy therefore stood in the flight path of the self-built
  ranged rig and absorbed its shots: the anti-tunnelling test failed outright, and the
  "expires with nothing to hit" test had been passing because the projectile was quietly hitting an
  arena dummy instead of expiring. Both rig-building test classes now load `Empty_TestBed` first. This
  would have gone on producing confident, meaningless green results as more scene-loading tests were
  added.

---

## 9. Phase 7 pass of 2026-08-02

### What was built

- `IDamageMitigator`, `ShieldArc`, `ShieldController`, and `ShieldPresentation`, plus the
  `VB_ShieldArc` and `VB_ShieldMarker` materials and transparency support in the material generator.
- `PlayerMotor.SpeedMultiplier`, so the raised shield slows the player without editing the tuned speed.
- `DamageInfo.StabilityDamage` and `DamageResult.Blocked`.
- Directed-hit buttons on the debug overlay — front, rear, heavy, and break — because nothing in the
  arena attacks the player, so without them the exit gate could not be checked by hand at all.

### Decisions worth knowing

1. **Blocking is a mitigator on `Health`, not an interception.** Every attack in the game still calls
   `ReceiveDamage` and none of them know shields exist. That is why the shield already works against
   melee, against projectiles, and against the debug buttons without any of them being told about it —
   and why enemy attacks in Phase 9 will be blockable the day they are written.
2. **A hit with no known source is unshieldable.** Guessing a direction would hand the player an
   accidental omnidirectional block, which the plan names as a risk for this phase.
3. **A hit larger than the remaining stability is still blocked in full.** The cost of coming up short
   is the break, not leaked damage; letting part of a hit through would make the moment of breaking
   impossible to read.
4. ~~**A broken shield cannot be raised until it is whole again**~~ — **superseded the same day by
   the arcade feel pass in section 10.** It was implemented as five seconds locked out and then
   refilling before it could be used, roughly thirteen seconds in total, on the reasoning that a
   shield raisable at three stability would be unpredictable. It was flagged in this section as the
   most likely thing to need tuning, and it was: the wait now *is* the lockout, the shield refills
   during it, and a break costs 2.5 seconds.
5. **The band never lies about coverage.** It always spans the true arc; stability changes its
   *height*, not its width, and it stutters below a third. State is readable in grayscale.

### Bugs fixed

1. **A hit landing exactly on the edge of the arc did not block.** Rotating a direction and measuring
   the angle back out lands a hair either side of the authored value, so a hit placed at exactly 60
   degrees off a 120-degree arc blocked or not depending on float representation. `ShieldArc` now
   carries a hundredth of a degree of tolerance — under a millimetre at melee range. Found by the
   boundary test the plan asks for, which is the reason it asks for it.
2. **Ten uses of `?.` on `UnityEngine.Object` fields**, across the melee, ranged, and shield
   controllers. The null-conditional operator skips Unity's lifetime check, so a destroyed
   coordinator, motor, or pool reads as alive and throws on the call. This is the same bug class the
   2026-08-02 cleanup pass fixed in `PlayerFacing`, `TargetDummy`, and `CombatDebugOverlay`, and it had
   crept straight back in. All replaced with explicit `!= null` checks.
3. **The shield band was built from an Awake-order-dependent value.** `ShieldPresentation` read the
   controller's arc in `Awake`, which is only correct because the controller happens to be added to
   the prefab first. Moved to `Start`, where every `Awake` has run. It would have drawn an arc of the
   wrong width the moment component order changed.
4. **The generated band mesh leaked.** Runtime-created meshes are not collected with the object that
   holds them; it is now destroyed with the component.

---

## 10. Arcade feel pass of 2026-08-02

Run after the project was confirmed to be **reactive and arcade**, against the items Phase 5 to 7 had
flagged as needing a human. Every change here is tuning or responsiveness; no system was added.

### Changes

1. **A break now costs the lockout and nothing after it.** Stability refills during the lockout, so
   the shield returns usable at about 30 — three light hits — instead of being unusable until full.
   The lockout dropped from 5 s to 2.5 s. Total downtime went from roughly thirteen seconds to two and
   a half. This reverses a decision recorded in `IMPLEMENTATION_NOTES.md`, which is updated with why.
2. **Melee buffers an early press.** A press during recovery or the tail of the cooldown is remembered
   for 0.15 s and fires on the first legal frame. Without it, mashing produced *fewer* swings than
   metronomic timing — punishing exactly the player an arcade game should reward.
3. **The melee cadence now equals the authored swing** (0.4 s to 0.3 s). The old cadence left a 0.1 s
   window where the swing was over, nothing was happening, and input was still refused: the classic
   shape of "it ate my press".
4. **Projectiles travel at 32 units/sec instead of 20.** A shot used to take half a second to reach
   something ten units away, which is longer than the gap between shots and reads as disconnected from
   the trigger.

The rules these follow from are now written into `COMBAT_POC_PLAN.md` section 4 as project-wide
responsiveness rules, and four of them are asserted by EditMode tests against `PrototypeBalance`, so a
later retune cannot quietly undo them.

### Bug fixed

- **The balance asset was silently incomplete.** `EnsureBalanceAsset` returned early whenever the file
  existed, to protect playtest results — which also meant fields added by later phases were never
  written to it. After Phase 7 the asset on disk still held only the Phase 3 to 5 values, and every
  ranged and shield value was falling back to a C# default where nobody could find or tune it in the
  Inspector. The tool now always re-serialises the asset: existing values are preserved exactly and
  new fields appear at their defaults. It still never changes a value.

---

## 11. Phase 8 pass of 2026-08-03

### What was built

- `DodgeController` and `DodgePresentation`, plus the `VB_DodgeStreak` material and the dodge section
  of `PrototypeBalance`.
- `PlayerMotor.SetMovementSuspended`, so the dodge can own horizontal displacement outright for the
  length of the burst without touching the tuned speed or the shield's separate multiplier.
- A dodge line on the combat overlay carrying the cooldown, the distance actually travelled, and the
  invulnerability window — the last of which nothing else could show.

### Decisions worth knowing

1. **Collision safety is `CharacterController`, not a distance check.** The dodge never inspects
   geometry; it asks the controller to move each step of its schedule and lets it resolve walls,
   corners, and bodies. "Never crosses an arena wall" is therefore true by construction rather than by
   a straight-line test a corner could defeat, and the reported distance is what happened rather than
   what was asked for.
2. **The displacement curve is a schedule of total distance, not a speed.** That is what makes three
   units exact at any frame rate and stops a long frame from overshooting. It is a quadratic ease-out,
   so the burst is fastest on the frame the button is pressed.
3. **The dodge does not interrupt melee active frames**, which is the plan's own stated default for
   task 6. Startup and recovery are both interruptible. Because refusing a press outright would break
   the project's responsiveness rules, the press is buffered across the 0.08 second window and fires
   the moment it closes. The rule lives in the dodge controller rather than in the coordinator, which
   deliberately owns no timing.
4. **Invulnerability is restored, not cleared.** `Health.SetInvulnerable` is a flag with no notion of
   ownership and Phase 11's debug panel is specified to hold it too, so the dodge puts back whatever
   it found. Without this a dodge taken with debug invulnerability on would silently switch it off.
5. **Combat facing is not forced to the dodge direction.** Rolling away from a threat while still
   aiming at it is the point of having independent aim.
6. **`SpeedMultiplier` and `IsMovementSuspended` are separate seams with separate owners** — the
   shield writes one, the dodge the other. Briefly they were the same field, and the shield's `Lower()`
   would have reset the dodge's value on the frame after a dodge cancelled a raised guard.

### Bug fixed while integrating

- **`ModularAvatar` used `?.` on an `EquipmentModule`.** The null-conditional operator skips Unity's
  lifetime check, so a destroyed module reads as alive and throws when the call lands. This is the
  same bug class the 2026-08-02 cleanup pass fixed in `PlayerFacing`, `TargetDummy` and
  `CombatDebugOverlay`, and the Phase 7 pass fixed ten more of in the three combat controllers — this
  one instance had been missed both times. The other three loops in the same file already used
  explicit checks, so it was inconsistent with its own neighbours as well as with the project rule.

## 12. Immediate next steps

1. **Run the deferred feel checkpoints, and Checkpoint B with them.** Movement, swinging, held fire,
   the shield, and now the dodge, with a real gamepad on a real display. Five exit gates and one
   decision checkpoint are waiting on the same session, and none of them can be answered by the test
   suites. The whole player kit is complete for the first time, so this session can now be run once
   rather than repeated per verb — which makes it, by some distance, the highest-value thing left.
2. **Commit this baseline.** The entire foundation is still uncommitted on top of the initial
   check-in. Suggested slicing: foundation and setup tooling, input, locomotion, combat kernel,
   cleanup pass, balance asset, melee slice, ranged slice, shield slice, dodge slice.
3. **Decide the enemy projectile question before starting Phase 9.** The pool is player-only today.
   Enemy projectiles need either a second pool or a small generalisation, and that is much cheaper to
   choose deliberately now than to discover halfway through the largest phase in the plan.
4. **Start Phase 9 (enemy teaching set)** per `Docs/COMBAT_POC_PLAN.md` section 6. It is the largest
   remaining phase — roughly Phases 5, 6 and 7 combined — but the kit it has to be designed against is
   now finished and tested, and enemy attacks will be blockable and dodgeable the day they are
   written, because neither the shield nor the dodge needs to know what hit the player.

---

## 13. How to reproduce every check

```powershell
# Regenerate every generated asset and self-validate
& 'D:\Unity\Hub\6000.4.4f1\Editor\Unity.exe' -batchmode -nographics -quit `
  -projectPath 'D:\Unity\Projects\VaultBreakers' `
  -executeMethod Vaultbreakers.Editor.VaultbreakersProjectSetup.BuildAll `
  -logFile 'Logs\setup.log'

# EditMode suite
& 'D:\Unity\Hub\6000.4.4f1\Editor\Unity.exe' -batchmode -nographics -runTests `
  -projectPath 'D:\Unity\Projects\VaultBreakers' -testPlatform EditMode `
  -testResults 'Logs\EditModeResults.xml' -logFile 'Logs\editmode.log'

# PlayMode suite
& 'D:\Unity\Hub\6000.4.4f1\Editor\Unity.exe' -batchmode -nographics -runTests `
  -projectPath 'D:\Unity\Projects\VaultBreakers' -testPlatform PlayMode `
  -testResults 'Logs\PlayModeResults.xml' -logFile 'Logs\playmode.log'

# Windows development build
& 'D:\Unity\Hub\6000.4.4f1\Editor\Unity.exe' -batchmode -nographics -quit `
  -projectPath 'D:\Unity\Projects\VaultBreakers' `
  -buildWindows64Player 'Builds\Vaultbreakers_Phase8\Vaultbreakers.exe' -development `
  -logFile 'Logs\build.log'
```

Rebuilding the avatar from Blender first:

```powershell
& 'D:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background `
  --python 'Tools\Blender\generate_modular_vaultbreaker.py'
```

---

## 14. Package baseline

| Package | Version |
|---|---|
| `com.unity.render-pipelines.universal` | 17.4.0 |
| `com.unity.inputsystem` | 1.19.0 |
| `com.unity.test-framework` | 1.6.0 |
| `com.unity.ugui` | 2.0.0 |
| `com.unity.timeline` | 1.8.12 |
| `com.unity.visualscripting` | 1.9.11 |
| `com.unity.ai.assistant` | 2.6.0-pre.1 (tooling only, never a runtime dependency) |

No package has been added for the combat POC, and none is required for Phases 5 through 8.
