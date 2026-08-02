# Vaultbreakers — Project Status

**Generated:** 2026-08-02
**Unity:** `6000.4.4f1`
**Branch:** `main` (all work below is uncommitted on top of `66927ad Initial check-in`)
**Last full validation:** setup tool, EditMode suite (104 tests), PlayMode suite (34 tests), and a
Windows development build, all green in batch mode on 2026-08-02 after the Phase 6 ranged slice.

This document answers two questions: what actually exists in the repository right now, and what you
can do if you press Play this minute. It deliberately separates *implemented*, *verified by machine*,
and *never checked by a human*.

---

## 1. One-paragraph summary

The project is a tested **foundation with two working verbs**. Phases 0 through 6 of
`Docs/COMBAT_POC_PLAN.md` are implemented: the project is converted from the 2D URP template to a 3D
Universal Renderer, physics layers and the collision matrix are code-owned, a game-specific input
asset feeds a pull-based input reader, the player moves and turns in a graybox arena with a fixed
isometric camera, a damage/health/action-state kernel drives target dummies, **melee** works end to
end, and **hold-to-fire ranged** works end to end — a fixed-size projectile pool that never allocates,
projectiles that sweep their own path so they cannot tunnel or double-hit, and a cadence that is
gameplay-authoritative. Tuning lives in `PrototypeBalance.asset`. **Shield and dodge are still not
implemented**; their inputs are read and their conflict rules are tested, but no controller consumes
them — which also means the fire-versus-defence decision, the point of the whole design, cannot be
felt yet. Phase 7 (directional shield) is the next piece of work and is the one that makes ranged
fire a choice rather than a default.

---

## 2. What you can do right now

Open `Assets/Vaultbreakers/Scenes/Prototype/Prototype_Arena.unity` and press Play, or run
`Builds/Vaultbreakers_Phase6/Vaultbreakers.exe` (a development build, so the debug overlays appear).

**Works:**

| You do | What happens |
|---|---|
| Left stick / WASD | The avatar accelerates and moves camera-relative on the ground plane, decelerates to a stop, and slides along the arena walls without leaving the arena. |
| Right stick / arrow keys | Combat facing snaps to the aim direction above a 0.25 dead zone; the model turns smoothly toward it. Movement direction is unaffected. |
| Release everything | The avatar stops and keeps its last useful facing. It does not drift. |
| **X / left mouse** | **Swings.** The weapon winds up, sweeps, and recovers; an orange arc shows the volume the swing queried; a connecting hit deals 10, flashes the target, pops an impact marker, and freezes the game for 0.05 s. A target within 15 degrees of facing is snapped onto. |
| **Hold or mash X** | At most one swing every 0.4 s. Spam cannot beat the cadence. |
| **Swing at a dummy standing behind you** | Nothing. The volume is in front; misses are visible because the arc is drawn where the query ran. |
| **Swing into the pair of dummies** | Both take damage from one swing, each exactly once. |
| **Hold RT / E** | **Fires.** A cyan tracer leaves the Muzzle anchor every 0.3 s with a flash and a recoil kick, travels at 20 units/sec, deals 8, and pops a mark where it lands. No ammunition, no reload. |
| **Turn while holding fire** | Each shot follows combat facing at the moment it leaves, not the facing of the first shot. |
| **Fire at a wall, or past everything** | The shot stops at the wall and marks it, or expires after 1.5 s. Either way it goes back to the pool. |
| Walk up to the three dummies | One at (0, 4) with a partner beside it, one flanking at (-4.5, 1.5). Each has 50 health, flashes red, and revives 1.5 s after dying. |
| Look at the top-left overlay | Live device name, Move and Aim vectors, and held state for all four action buttons. |
| Look at the second overlay | Player and dummy health, the action-state flags, the live melee phase, cooldown and hit count, the ranged firing state and shot count, and projectile pool active/free/recycled counts. |
| Click the overlay buttons | Damage the dummy 10, kill the dummy, damage the player 10, or reset both. |
| Plug/unplug a controller mid-session | No exceptions and no stuck held actions. |

Open `Assets/Vaultbreakers/Scenes/Test/Avatar_Showcase.unity` and press Play:

| You do | What happens |
|---|---|
| Nothing | The avatar turntables under a three-point light rig. |
| Press `1` | Equips the default Scrapper loadout across all six slots. |
| Press `2` | Equips the alternate Sentinel/Bulwark loadout across all six slots. |

**Does not work yet (by design — later phases):**

- **Shield (LT / right mouse)** and **Dodge (A / Space)** register in the input overlay and nothing
  else. No shield arc, no dodge burst. The conflict rules that make firing and shielding exclusive are
  written, tested, and already enforced by both attack controllers — but with no shield to raise, the
  fire-versus-defence decision cannot actually be experienced yet.
- **Pause (Start / Esc)** and **Restart (R)** are read but not handled.
- There are **no enemies, no waves, no HUD, and no retry loop**. The dummies never fight back and the
  avatar slides without a walk cycle; the swing and recoil poses are procedural, not animated.
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
| 7 — Directional shield | Fire-versus-defense decision | **Not started — next up** |
| 8 — Dodge | Movement answer to threats | Not started |
| 9+ — Enemies, waves, HUD, feel pass | — | Not started |

Three exit gates are open on feel alone: Phase 3's two-minute controller session, Phase 5's
"input-to-visible-response feels immediate", and Phase 6's "hold-to-fire is pleasant for at least a
minute" and "feedback remains readable across the arena". No test can answer any of them. See
`Docs/IMPLEMENTATION_NOTES.md`. Every mechanical check passes — direction, facing priority, wall
collision, swing timing, deduplication, cooldowns, cadence, tunnelling, and pool accounting — but
**do not treat Phase 3, 5, or 6 as signed off.**

---

## 4. What exists in the repository

### Runtime code — `Assets/Vaultbreakers/Scripts/` (`Vaultbreakers.Runtime` assembly)

| File | Responsibility |
|---|---|
| `Core/GameLayers.cs` | Physics layer indices from `PROJECT.md` 5.5 and the query masks built from them. No tag or name comparisons anywhere in gameplay. |
| `Core/PrototypeBalance.cs` | Central tuning ScriptableObject: locomotion, player health, melee, feedback. Configuration only. Sections are added as their phase lands. |
| `Input/PlayerInputReader.cs` | Pull-based intent: Move, Aim, four held actions, four edge actions, current device. Translates nothing. |
| `Player/PlayerMotor.cs` | Camera-relative XZ movement on a `CharacterController` with acceleration, deceleration, and grounding. Runs at execution order −20. |
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
| `Combat/PlaceholderAudio.cs` | Generates the POC's combat cues, seeded by name so they are identical on every run. Shared by both attack presentations. |
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
| `VaultbreakersDataSetup.cs` | Creates `PrototypeBalance.asset` if it is missing. Deliberately never overwrites it: the values in it are playtest results. |
| `VaultbreakersProjectileBuilder.cs` | Builds `PF_PlayerProjectile`. Contributes a component, a layer, and a tracer silhouette — no collider, because the projectile sweeps its own path. |
| `VaultbreakersPhysicsSetup.cs` | Layer names and the collision matrix, declared as an allow-list in code. |
| `VaultbreakersRenderSetup.cs` | 3D Universal Renderer, URP asset, quality levels, colour space, graybox materials. |
| `VaultbreakersAvatarBuilder.cs` | FBX import contract, module grouping from exported names, socket binding, prefab assembly. |
| `VaultbreakersSceneBuilder.cs` | Both scenes and the build-settings list. |
| `VaultbreakersSetupValidation.cs` | Post-build gate: pipeline, layers, input maps, prefab components, module swaps, socket placement, materials. |
| `VaultbreakersPreviewCapture.cs` | Documentation screenshots. Needs a real graphics device and refuses to run without one. |

### Generated assets

- `Settings/VaultbreakersURP.asset` + `VaultbreakersUniversalRenderer.asset` — assigned as the default
  pipeline and on every quality level.
- `Input/VaultbreakersInputActions.inputactions` — `Player` map with all eight actions; `UI` map is an
  empty stub.
- `Data/Balance/PrototypeBalance.asset` — the single tuning asset. `PlayerMotor`, `PlayerFacing`, and
  `MeleeController` copy from it at configure time; their serialized fields are fallbacks for bare
  test scenes, never a second source of truth.
- `Art/Materials/VB_*.mat` — thirteen URP Lit graybox, character, and effect materials.
- `Prefabs/Projectiles/PF_PlayerProjectile.prefab` — the pooled player shot and its cyan tracer.
- `Art/Characters/Player/Vaultbreaker_Modular.fbx` — generated from
  `Tools/Blender/generate_modular_vaultbreaker.py`.
- `Prefabs/Player/PF_Vaultbreaker_POC.prefab` — the playable avatar: input, controller, motor, facing,
  health, action coordinator, melee controller and presentation, hit-stop, projectile pool, ranged
  controller and presentation, modular avatar, socket registry.
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

- **104 EditMode tests** — `Logs/Phase6EditModeResults.xml`
  - combat kernel: clamping, invulnerability, death idempotence, death-before-`Damaged` ordering,
    action conflicts, stop transitions, reset;
  - melee geometry: query placement, edge of range, rear rejection, point-blank acceptance, height
    independence, correction inside/outside the cone, the correction cap swept across ±90°, zero
    vectors, knockback direction;
  - melee timing: startup/active/recovery at the authored times, the attack claim taken and released,
    spam refused, and the swing accepted on the exact frame the cadence allows;
  - ranged cadence: four shots in the first second, no drift across ten seconds, a tap is one shot, a
    released trigger banks nothing, and shield, dodge, and death each stop fire and let it resume;
  - `PrototypeBalance` ships the documented starting values, and both attack controllers are shown to
    read the asset rather than their own fallbacks by tuning the asset away from the defaults;
  - facing and camera-relative movement maths;
  - input asset maps, bindings, keyboard aim composite, single-application of the aim dead zone;
  - physics layers and the full collision matrix, asserted against the design rules rather than the
    setup table;
  - generated foundation: pipeline, import contract, skeleton, sockets, modules, materials, scale,
    handedness, prefab defaults;
  - modular avatar equip/unequip rules and the debug event log.
- **34 PlayMode tests** — `Logs/Phase6PlayModeResults.xml`
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
  - the arena player's own melee and ranged controllers both damage the arena's own practice dummy;
  - the showcase avatar carries no gameplay, melee, ranged, pool, or timescale components;
  - every equipment combination swaps at runtime without losing a socket.
- **Zero compiler warnings** across all four assemblies.
- **Windows development build** produces a running executable — `Builds/Vaultbreakers_Phase6/`.

**Not verified — needs a human, a controller, or a display:**

- Controller feel: acceleration, stopping, reversal, wall sliding, dead zones. This is the deferred
  Phase 3 checkpoint and is the highest-value next validation.
- Melee feel: whether the 0.06 s startup reads as immediate, whether the 0.4 s cadence is satisfying,
  whether the arc and hit-stop make contact legible, and whether misses are understandable. This is
  the Phase 5 exit gate and no test can answer it.
- Ranged feel and readability: whether holding fire is pleasant for a minute, and whether the tracer,
  flash, and impact marks stay readable at the far side of the arena. This is the Phase 6 exit gate.
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
   unlikely at 20 units/sec, and it removes the need for continuous collision detection, trigger
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

## 9. Immediate next steps

1. **Run the deferred Phase 3, 5 and 6 feel checkpoints together.** Two minutes of movement, a few
   minutes of swinging, and a minute of held fire, with a real gamepad on a real display. All three
   gates are about feel and none can be answered by the test suites.
2. **Commit this baseline.** The entire foundation is still uncommitted on top of the initial
   check-in. Suggested slicing: foundation and setup tooling, input, locomotion, combat kernel,
   cleanup pass, balance asset, melee slice, ranged slice.
3. **Start Phase 7 (directional shield)** per `Docs/COMBAT_POC_PLAN.md` section 6. This is the phase
   that makes the other two verbs a choice: the coordinator rules, the shield socket and anchor, and
   the source-aware `DamageInfo` the arc maths needs are all already in place and already exercised.

---

## 10. How to reproduce every check

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
  -buildWindows64Player 'Builds\Vaultbreakers_Phase6\Vaultbreakers.exe' -development `
  -logFile 'Logs\build.log'
```

Rebuilding the avatar from Blender first:

```powershell
& 'D:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background `
  --python 'Tools\Blender\generate_modular_vaultbreaker.py'
```

---

## 11. Package baseline

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
