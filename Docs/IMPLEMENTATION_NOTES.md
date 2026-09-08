# Implementation notes

## Deferred validation

### Phase 3 — controller feel checkpoint

Deferred on 2026-08-01 at the user's request so Phase 4 could proceed. Automated direction, facing-priority, scene-loading, and arena-wall collision tests pass, but the required two-minute human controller check has not been performed.

When playtesting resumes, verify cardinal/diagonal movement, rapid reversal, stopping, wall sliding, stick release, aim override, neutral-facing preservation, and idle drift. Revise these locations if needed:

- Movement speed, acceleration, deceleration, gravity, visual turn speed, and the aim dead zone: `Assets/Vaultbreakers/Data/Balance/PrototypeBalance.asset`. The matching serialized fields on `PlayerMotor` and `PlayerFacing` are fallbacks for a bare test rig and are overwritten by the asset whenever one is assigned — editing them on the prefab looks like it works and is silently discarded on the next play.
- Move dead zone: the `StickDeadzone` processor on the Move action in `Assets/Vaultbreakers/Input/VaultbreakersInputActions.inputactions`. This is the one movement value that does *not* live in the balance asset, because no gameplay code applies it. The Aim action deliberately has no processor; see below.
- Default `CharacterController` radius, height, skin width, and step offset: `AddGameplayComponents` in `Assets/Vaultbreakers/Editor/VaultbreakersAvatarBuilder.cs`. These are generated, so editing the prefab by hand is undone by the next setup run.

Do not interpret this note as a passed Checkpoint A. Revisit it before combat-feel sign-off and before adding enemies.

### Phase 5 — melee feel checkpoint

Deferred on 2026-08-02. The mechanical half of the exit gate is covered by tests: the hit resolves at the authored active time rather than at animation completion, the same target is hit once per swing, and misses come from a query volume that the on-screen arc matches exactly. The remaining criteria — "input-to-visible-response feels immediate", "misses are understandable", "melee remains usable without right-stick aim" — need a human at a controller.

When playtesting resumes, swing at all three arena dummies from several angles and revise these values in `Assets/Vaultbreakers/Data/Balance/PrototypeBalance.asset`:

- `meleeStartupDuration` if the swing feels delayed. If you change any phase duration, change `meleeCooldown` to match the new total: they are deliberately equal so swings chain, and an EditMode test asserts it.
- `meleeInputBuffer` if mashing still loses inputs, or if swings keep coming after you stop pressing.
- `meleeRange` and `meleeRadius` if contact is hard to judge. The sphere centre is derived from both, so changing either moves where the arc appears; that is intentional, because the arc is the query.
- `meleeTargetCorrection` if the swing either fights the player's aim or fails to connect with a target that was clearly intended.
- `hitStopDuration` if impact reads as mushy or as a stutter. Zero disables it.

Nothing here needs a code change. If a change to swing *behaviour* rather than swing *numbers* turns out to be needed, that belongs in `Assets/Vaultbreakers/Scripts/Combat/MeleeController.cs`.

### Phase 6 — ranged feel and readability checkpoint

Deferred on 2026-08-02. The mechanical half of the exit gate is covered by tests: the cadence is stable and driftless, projectiles follow the facing each shot leaves on, they neither tunnel nor double-hit, and ten seconds of held fire never changes the pool size. What remains is "holding RT is pleasant for at least a minute" and "feedback remains readable across the arena", plus the 60 FPS half of the performance criterion, which needs a profiler on a real display.

When playtesting resumes, hold fire across the arena at all three dummies and revise these values in `Assets/Vaultbreakers/Data/Balance/PrototypeBalance.asset`:

- `fireCooldown` if the rhythm is tiring or too slow, and `projectileDamage` alongside it — they trade off, and changing one alone changes damage per second as well as feel.
- `projectileSpeed` if shots are hard to follow, or if they arrive too late to feel connected to the trigger. Two EditMode tests bracket it: a shot must outlive the arena's corner-to-corner distance, and must cross the arena in comfortably less time than the gap between shots. Lower `projectileLifetime` alongside a large increase, or projectiles will simply stop expiring inside the arena.
- `projectileRadius` if shots visibly clip walls they should have passed, or slip past a target they should have caught.

`projectilePoolSize` should not need tuning. If the debug overlay ever shows a non-zero recycled count during ordinary play, that is a signal that the cadence or lifetime changed, not that the pool is too small.

### Phase 7 — shield readability and break cost checkpoint

Deferred on 2026-08-02. The mechanics are covered by tests: every arc boundary the plan names, frontal blocks versus rear hits through a real projectile, the break and its recovery, the movement penalty, and the exclusion with ranged and melee driven through the real controllers. What remains is "shield break is unmistakable and recovery is predictable", whether the band's direction reads at gameplay camera distance, and plan Checkpoint B — whether the fire-versus-defence exclusion is a useful choice rather than a frustration.

When playtesting resumes, revise these values in `Assets/Vaultbreakers/Data/Balance/PrototypeBalance.asset`:

- `shieldBreakLockout` is the entire cost of a break — 2.5 seconds — and `loweredStabilityRegeneration` decides how much shield comes back with it (`PrototypeBalance.StabilityAfterBreak`, currently 30). Raise the lockout and the punishment bites harder *and* returns more; that coupling is deliberate, but it means the two cannot be tuned independently.
- `shieldArc` if covering a threat feels either fiddly or free. The band always draws the real arc, so widening it is honest — but a wide arc makes the shield omnidirectional in practice, which is the failure the design exists to avoid.
- `shieldStability` and `lightStabilityDamage` set how many hits a guard survives; at the shipped values it is exactly ten light hits.
- `shieldMoveMultiplier` if shielding feels either weightless or like being stuck.
- `raisedStabilityRegeneration` if holding the shield up indefinitely turns out to be the dominant play. It must stay below the lowered rate or lowering the shield has no point; an EditMode test holds that line.

### Phase 8 — dodge feel checkpoint

Deferred on 2026-08-03. The mechanical half of the exit gate is covered by tests: the burst never
crosses a wall or escapes a corner, invulnerability begins on the frame the dodge does and ends with
it, spam and a held button both produce exactly one dodge per cooldown, and a dodge drops a raised
shield and stops held fire through the real controllers. What remains is whether a dodge *reads* as
an escape — whether 3 units is far enough to leave a threat, whether 0.2 seconds is long enough to
see and short enough to trust, and whether the one second cooldown makes it a decision rather than a
second movement speed. The exit gate item "dodge solves a different problem from shielding" cannot be
answered at all until Phase 9 enemies exist to dodge *away from*.

When playtesting resumes, revise these values in `Assets/Vaultbreakers/Data/Balance/PrototypeBalance.asset`:

- `dodgeDistance` and `dodgeDuration` together decide how the burst reads; they are not independent,
  because their ratio is the speed. An EditMode test asserts the average speed stays comfortably
  above walking, since a dodge no faster than running solves nothing running does not.
- `dodgeCooldown` if the dodge is either spammable or feels withheld. It must stay longer than the
  duration, and `dodgeInputBuffer` must stay well under it; both are asserted.
- `dodgeInvulnerability` if hits land during a dodge that visibly cleared them, or if the player
  survives things they should not have. It must cover at least the whole movement — a test holds that
  line, because a dodge that is hittable while visibly mid-dodge reads as a lie.

The one behavioural question the plan leaves open is whether a dodge may interrupt melee; see below.

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

### A break costs the lockout and nothing after it

`COMBAT_POC_PLAN.md` asks for "break lockout, and full recovery" without saying whether the shield is
usable while it refills.

This was first implemented as: locked out, then refilling, and unusable until full — roughly thirteen
seconds. That reading is defensible on paper and wrong in play. It was flagged as the most likely
thing to need tuning and then corrected on 2026-08-02 when the game was confirmed to be arcade and
reactive: **stability now refills during the lockout, and the shield is usable the moment the lockout
ends.** A break therefore costs one number of seconds — currently 2.5 — after which the player is back
in the fight holding about 30 stability, enough for three light hits, which they then have to spend
carefully.

The point of the earlier rule was predictability, and this is *more* predictable, not less: one
duration with one meaning rather than a duration followed by a second condition the player has to
infer. It is also the version that keeps a broken guard interesting instead of turning it into a wait.

### Blocking never leaks partial damage

A hit larger than the remaining stability is blocked in full and breaks the shield. The alternative —
passing the remainder through to health — would make the moment of breaking impossible to read, since
the player would take damage on a hit they visibly blocked.

### The player has no separate Hurtbox

`COMBAT_POC_PLAN.md` section 5 sketches a player prefab containing a `Hurtbox`. There is not one: the
`CharacterController` is a collider on the `Player` layer, so it already answers
`GameLayers.PlayerTargets` queries, and a second collider would only need keeping in sync with it.

This is fine while nothing attacks the player, and it is the thing to revisit first in Phase 9. A
dedicated hurtbox becomes worth adding the moment enemy attacks want a hit volume that differs from
the movement capsule — a taller one for readability, or one that changes during a dodge.

### Explicit null checks, never `?.`, on component references

The null-conditional operator does not run Unity's overloaded lifetime check, so a destroyed
component reads as alive and throws when the call lands. The 2026-08-02 cleanup pass fixed this in
`PlayerFacing`, `TargetDummy`, and `CombatDebugOverlay`; ten fresh instances appeared in the melee,
ranged, and shield controllers and were fixed on 2026-08-02. `?.` on a plain C# object — a delegate,
an `InputAction` — is fine and is still used.

### The setup tool re-serialises the balance asset without changing a value

`VaultbreakersDataSetup.EnsureBalanceAsset` originally returned early whenever the asset existed, to
protect playtest results. That also meant a field added by a later phase never appeared in the file:
after Phase 7 the asset on disk still held only the Phase 3 to 5 values, and the ranged and shield
sections silently fell back to their C# defaults where nobody could find or tune them. The tool now
always marks the asset dirty so Unity rewrites it — values already on disk are preserved exactly, and
new fields materialise at their defaults. It still never changes a value.

### Responsiveness is a project rule, not a per-system choice

`COMBAT_POC_PLAN.md` section 4 now records the rules this follows from: a cooldown never outlasts its
own action, an early press is remembered rather than discarded, a projectile arrives while the pull
still feels connected to it, and a punishment state is one learnable duration. Anything added later —
dodge, enemy attacks, wave transitions — is expected to hold to them, and several are asserted by
EditMode tests against `PrototypeBalance` so a future retune cannot quietly break them.

### The dodge does not interrupt melee active frames, and buffers the press across them

`COMBAT_POC_PLAN.md` Phase 8 task 6 leaves this open — "define whether dodge interrupts melee
recovery after feel testing; default is no cancellation during melee active frames" — so the stated
default is what ships. `DodgeController` refuses to start while `MeleeController.Phase` is `Active`;
startup and recovery are both interruptible, and a swing in either is cancelled outright.

The rule lives in the dodge controller rather than in `PlayerActionCoordinator`, because the
coordinator deliberately owns no timing and asking it about a swing phase would break that. The cost
is one explicit dependency from dodge to melee, declared in `Configure`.

On its own this rule would eat a press, which the project's own responsiveness rules forbid. So the
dodge buffers like melee does: a press made during the active window fires on the first legal frame,
delaying the player by the authored 0.08 seconds rather than dropping the input. Flipping the
decision after feel testing means deleting `IsMeleeCommitted` and its two tests; nothing else depends
on it.

### The dodge owns displacement outright while it runs

`PlayerMotor.SetMovementSuspended` was added for this. During a burst the motor still resolves
`MoveDirection` — the dodge reads it to pick its bearing — but contributes no horizontal motion, and
the dodge moves the `CharacterController` itself along an authored schedule.

The alternative, letting input velocity add to the burst, would make dodge distance depend on whether
the player happened to be running, so "3 units" would only be true from a standstill. The schedule is
a curve of *total distance* rather than a per-frame speed, which is what makes the distance exact at
any frame rate and stops a long frame from overshooting.

Note that `SpeedMultiplier` and `IsMovementSuspended` are separate seams with separate owners — the
shield writes the first, the dodge the second — precisely so neither can stomp the other. They were
briefly one field and the shield's `Lower()` would have reset the dodge's value on the frame after a
dodge cancelled a raised guard.

### Collision safety comes from `CharacterController`, not from a distance check

The dodge never inspects geometry. It asks `CharacterController.Move` for each step of its schedule
and lets the controller resolve walls, corners, and other bodies. "A dodge never crosses an arena
wall" is therefore true by construction rather than by a straight-line check that a corner could
defeat, and `DistanceTravelled` reports what actually happened rather than what was asked for.

### The dodge restores invulnerability rather than clearing it

`Health.SetInvulnerable` is a plain flag with no notion of ownership, and Phase 11's debug panel is
specified to hold it too. `DodgeController` therefore remembers the value it found and puts that back
when the window closes, instead of clearing the flag. Without this, a dodge taken with debug
invulnerability on would silently switch it off. `IsInvulnerableFromDodge` distinguishes the dodge's
own window from the flag itself.

### Combat facing is not forced to the dodge direction

Rolling away from a threat while still aiming at it is the point of having independent aim, so the
dodge does not call `ApplyAttackFacing`. Facing continues to follow the normal priority rules for the
whole burst, which means a backward dodge keeps the player looking at what they retreated from.

## Baseline health

Last full validation run: 2026-08-03, Unity 6000.4.4f1, batch mode, after the Phase 8 dodge slice.

- `Vaultbreakers > Setup > Build 3D Foundation and Modular Avatar` regenerates every generated asset and self-validates.
- EditMode (157) and PlayMode (50) suites both green, zero compiler warnings, Windows development build produced. See `Docs/PROJECT_STATUS.md` for the breakdown and what remains unverified.
