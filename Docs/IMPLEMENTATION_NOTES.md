# Implementation notes

## Deferred validation

### Phase 3 — controller feel checkpoint

Deferred on 2026-08-01 at the user's request so Phase 4 could proceed. Automated direction, facing-priority, scene-loading, and arena-wall collision tests pass, but the required two-minute human controller check has not been performed.

When playtesting resumes, verify cardinal/diagonal movement, rapid reversal, stopping, wall sliding, stick release, aim override, neutral-facing preservation, and idle drift. Revise these locations if needed:

- Movement speed, acceleration, deceleration, and gravity: serialized fields in `Assets/Vaultbreakers/Scripts/Player/PlayerMotor.cs`.
- Visual turn speed and aim dead zone: serialized fields in `Assets/Vaultbreakers/Scripts/Player/PlayerFacing.cs`.
- Move dead zone: the `StickDeadzone` processor on the Move action in `Assets/Vaultbreakers/Input/VaultbreakersInputActions.inputactions`. The Aim action deliberately has no processor; see below.
- Default `CharacterController` radius, height, skin width, and step offset: `AddGameplayComponents` in `Assets/Vaultbreakers/Editor/VaultbreakersAvatarBuilder.cs`.

Do not interpret this note as a passed Checkpoint A. Revisit it before combat-feel sign-off and before adding enemies.

### Phase 5 — melee feel checkpoint

Deferred on 2026-08-02. The mechanical half of the exit gate is covered by tests: the hit resolves at the authored active time rather than at animation completion, the same target is hit once per swing, and misses come from a query volume that the on-screen arc matches exactly. The remaining criteria — "input-to-visible-response feels immediate", "misses are understandable", "melee remains usable without right-stick aim" — need a human at a controller.

When playtesting resumes, swing at all three arena dummies from several angles and revise these values in `Assets/Vaultbreakers/Data/Balance/PrototypeBalance.asset`:

- `meleeStartupDuration` if the swing feels delayed, and `meleeCooldown` if the cadence feels sluggish or spammy. These are independent: the cooldown runs from swing start, so shortening the phases alone will not speed up the cadence.
- `meleeRange` and `meleeRadius` if contact is hard to judge. The sphere centre is derived from both, so changing either moves where the arc appears; that is intentional, because the arc is the query.
- `meleeTargetCorrection` if the swing either fights the player's aim or fails to connect with a target that was clearly intended.
- `hitStopDuration` if impact reads as mushy or as a stutter. Zero disables it.

Nothing here needs a code change. If a change to swing *behaviour* rather than swing *numbers* turns out to be needed, that belongs in `Assets/Vaultbreakers/Scripts/Combat/MeleeController.cs`.

### Phase 6 — ranged feel and readability checkpoint

Deferred on 2026-08-02. The mechanical half of the exit gate is covered by tests: the cadence is stable and driftless, projectiles follow the facing each shot leaves on, they neither tunnel nor double-hit, and ten seconds of held fire never changes the pool size. What remains is "holding RT is pleasant for at least a minute" and "feedback remains readable across the arena", plus the 60 FPS half of the performance criterion, which needs a profiler on a real display.

When playtesting resumes, hold fire across the arena at all three dummies and revise these values in `Assets/Vaultbreakers/Data/Balance/PrototypeBalance.asset`:

- `fireCooldown` if the rhythm is tiring or too slow, and `projectileDamage` alongside it — they trade off, and changing one alone changes damage per second as well as feel.
- `projectileSpeed` if shots are hard to follow or arrive too late to feel connected to the trigger. Raise `projectileLifetime` with it, or shots will start expiring inside the arena; the balance test asserts they outlive the corner-to-corner distance.
- `projectileRadius` if shots visibly clip walls they should have passed, or slip past a target they should have caught.

`projectilePoolSize` should not need tuning. If the debug overlay ever shows a non-zero recycled count during ordinary play, that is a signal that the cadence or lifetime changed, not that the pool is too small.

## Deliberate deviations from the plan

### Keyboard aim uses arrow keys, not mouse world aim

`COMBAT_POC_PLAN.md` section 4 allows "mouse world aim or arrow keys". The asset binds arrow keys.
The previous `<Pointer>/delta` binding was removed on 2026-08-02: pointer delta is a relative motion
value, so facing only updated while the mouse was moving and its magnitude scaled with movement speed
rather than expressing a direction. Mouse world aim requires projecting the cursor onto the ground
plane through the camera, which is gameplay translation and does not belong in `PlayerInputReader`.
Revisit when a camera service exists; the natural owner is `PlayerFacing`.

### The aim dead zone lives in one place

Both the input asset and `PlayerFacing` used to apply a 0.25 aim dead zone. `StickDeadzone` rescales
the range above its minimum, so the two stacked into an effective threshold near 0.44 raw stick
deflection while both places still read as "0.25". The asset processor was removed on 2026-08-02 and
`PlayerFacing.aimDeadZone` is now the single, testable threshold. Move keeps its asset processor
because no gameplay code applies a move dead zone.

### The melee swing direction is chosen at input, not at the active frame

`COMBAT_POC_PLAN.md` section 4 lists target correction as happening "during attack execution". The
implementation resolves it once, when the input is accepted, and holds it for the whole swing. A
direction that keeps re-snapping through the active window makes the strike feel like it is chasing
the target rather than going where the player pointed, and it is far harder to reason about. The
requirement the exit gate actually states — that the *hit* lands at the authored active time — is
unchanged and tested.

### `PrototypeBalance.asset` is generated once and never regenerated

Every other generated asset is rebuilt from `Vaultbreakers > Setup > Build 3D Foundation and Modular
Avatar` and is safe to delete. The balance asset is not: its contents are playtest results. The setup
tool creates it when missing and leaves it alone otherwise. If it is ever deleted, it comes back with
the documented starting values, which an EditMode test pins.

### Hit-stop owns the global timescale

`HitStop` drives `Time.timeScale` to zero on a connecting hit and restores the exact value it
captured, releasing on `OnDisable` so a disabled or destroyed component can never leave the game
frozen. Melee timers deliberately run on scaled time, so the swing pauses with the simulation instead
of drifting past it. Phase 11's pause screen will be the second owner of the timescale and must
coordinate with this rather than setting it independently.

### Projectiles are swept queries, not physics bodies

`COMBAT_POC_PLAN.md` asks for "a pooled projectile with speed, lifetime, damage, impact, and
environment collision" and for projectiles that neither tunnel nor double-hit. The implementation
carries no `Rigidbody` and no `Collider`: each step sweeps a sphere from the previous position to the
next and resolves the closest hit against `GameLayers.PlayerProjectileHits`.

This is a stronger guarantee than continuous collision detection, not a shortcut. Tunnelling becomes
impossible rather than unlikely, "exactly one hit" falls out of the design instead of needing a
`hasHit` flag, and no physics body is simulated per shot. The collision matrix still describes the
projectile layers correctly, so anything later that genuinely wants to collide with a projectile —
a shield, a deflector — still can.

A consequence worth knowing: nothing can collide *with* a player projectile today, because there is
no collider to hit. Phase 7 must resolve shield interception by asking the projectile, not by waiting
for a trigger.

### PlayMode tests that build their own rig must load `Empty_TestBed` first

PlayMode tests share one scene manager and one physics world, and a test that loads a generated scene
leaves it loaded for everything that runs after it. Colliders from the arena will answer another
test's queries and produce confident, meaningless results — this cost one real failure and one test
that had been passing for the wrong reason. Any new PlayMode test that constructs its own objects
should start with `yield return IsolatedTestBed.Load();`.

## Baseline health

Last full validation run: 2026-08-02, Unity 6000.4.4f1, batch mode, after the Phase 6 ranged slice.

- `Vaultbreakers > Setup > Build 3D Foundation and Modular Avatar` regenerates every generated asset and self-validates.
- EditMode (104) and PlayMode (34) suites both green, zero compiler warnings, Windows development build produced. See `Docs/PROJECT_STATUS.md` for the breakdown and what remains unverified.
