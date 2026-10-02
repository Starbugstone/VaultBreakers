# Alpha 0.0.1 — gems and breakable caches

## Authorized scope

On 2026-10-02 the user requested a first test run, then enemy gem drops and collection,
inspired by Vampire Hunters, plus new breakable crates and chests. This explicitly advances
the first post-combat slice after the functional baseline checks. It supersedes the earlier
combat-only exclusion for these features. Gauges, powers, cards, scanning, pets, saves and
co-op remain deferred. Combat tuning, character contracts and existing user art changes remain.

## Baseline check, before implementation

The current Windows development player completed all three rooms and replay with a scripted
virtual controller using normal health: 14 melee swings, 25 shots, 5 guard raises, 1 block,
3 dodges, 8 damage taken, no deaths, no firing through guard. Captures were inspected.
All **177 EditMode and 69 PlayMode** tests passed again, including death/retry, gates,
controller handling and reset behavior. No C# errors or gameplay exceptions were found.

Evidence: [controller session](Images/Alpha001_Baseline/session.json),
[frame measurements](Images/Alpha001_Baseline/metrics.json),
[EditMode](Validation/Alpha001_Baseline_EditMode.xml),
[PlayMode](Validation/Alpha001_Baseline_PlayMode.xml).

The route measured 14.05 ms mean / 27.73 ms P95 on the RTX 2060 while another Unity project
was building. Screenshot stalls affect the maximum. This establishes functional readiness
for the requested loot slice, **not stable 60 FPS**. The existing ComputeBuffer-disposal
warning at shutdown remains a separate follow-up. No human understanding, enjoyment or
physical controller feel is inferred from the scripted run.

## Reference and adaptation

Vampire Hunters uses collectible souls and collection-distance upgrades. Its developer's
[May 2024 update](https://store.steampowered.com/news/posts/?enddate=1717175277&feed=steam_community_announcements)
describes a magnet upgrade that increases collection distance and lets attracted souls
damage enemies. Its [August 2025 update](https://steamcommunity.com/app/2206270/allnews/?l=english)
also describes an item that collects soul drops immediately on a kill.

The adaptation here is nearby attraction into the character: visible scattered rewards,
an accelerating pickup stream and quick, rising pickup notes. Numeric tuning and cache
rewards are Vaultbreakers design choices, not claims about Vampire Hunters' exact settings.
No automatic map-wide pickup, XP/level-up system or damaging gems are added.

## Playable behavior

| Source | Gems | Composition |
|---|---:|---|
| Scrap Grunt | 3 | Melee and gold |
| Repo Shooter | 4 | Ranged and gold |
| Heavy Bruiser | 6 | Shield, melee and gold |
| Breakable orange crate | 6 | Gold and energy gems |
| Breakable teal chest | 12 | All five kinds |

Two orange crates and one teal chest are placed in each room, away from the central
doorway lane. Gold braces and a raised diamond distinguish them from the permanent grey
industrial scenery. Hammer attacks and arm fire break them. Crates have 14 HP, chests 28 HP.
They show broken panels and lose collision immediately, pay once per attempt, and do not
count as hostiles. Enemy attacks cannot farm cache rewards. The final vault core remains
the existing approach-to-claim objective, separate from these breakable loot chests.

The user's follow-up requests better debris with a one-second shrink and fade. Each cache
now breaks into six individually pivoted pieces: split lids, jagged side plates with retained
latches, and bent corner rails. They tumble outward, begin shrinking after 0.35 seconds,
fade from 0.4 to 0.95 seconds, and deactivate at one second of game time. Pause freezes the
effect; retry restores full-size pieces. Shared transparent materials and property blocks
provide fading without per-break material creation or physics debris.

Gems have five distinct shapes/colors and no physical collider. They burst, settle, then
attract within 2.5 m after a 0.2-second delay. Collection happens within 0.65 m; attraction
accelerates from 5 to 18 m/s. Solid walls/caches occlude attraction. Pickups stay in the
room, blink near expiry, and normally last 8 seconds. A cleared fight gives 3 seconds of
collection before its exit unlocks and extends surviving pickups to at least 7 seconds.
The collection timer pauses with the game. Collection remains possible after the gate opens.

The fixed pool holds at most 40 visible pickups. Near capacity, same-kind pickups combine
their values; reserved slots preserve kinds not already on the floor. This avoids losing
reward value at capacity. Pooling and a single update avoid per-drop object creation.
Pickup audio is rate-limited. No extra lights, rigidbodies, packages or per-gem Update run.

The HUD counts collected gem units. Gold adds 25 salvage score per unit; energy gems are
counted without combat bonuses in this slice. Collected amounts bank on entry to the next
room. Death or manual checkpoint retry restores the banked amounts, resets that room's
caches, and clears loose gems. Replay clears everything. Uncollected room pickups are
discarded when crossing into the next room. There is no between-run persistence.

## Rebuild and validation

- Art: `Tools/Blender/generate_loot_assets.py`; editable source
  `ArtSource/Blender/Props/Dock9_Loot.blend`, explicit FBXs under `Art/Loot`.
- Unity generation: `VaultbreakersLootBuilder`, called by the existing project setup.
- Tuning: `Assets/Vaultbreakers/Data/Balance/GemBalance.asset`, preserved across setup.
- Rules: `GemRules`, `GemWallet`; lifecycle: `DungeonLoot`, `GemPool`, `BreakableLoot`.
- `python3 Tools/Unity/validate.py Alpha001_Debris setup EditMode PlayMode build`
- `python3 Tools/Unity/review.py journey 1920 1080 Alpha001_Final_1080p 120 --poc-policy 4 --poc-loot`
- `python3 Tools/Unity/review.py journey 1280 720 Alpha001_Final_720p 120 --poc-policy 4 --poc-loot`
- `python3 Tools/Unity/review.py stress 1920 1080 Alpha001_Isolated_Stress 30 --poc-gem-stress`
- `python3 Tools/Unity/package.py --alpha`

The loot controller policy attacks containers through the existing gamepad input, walks to
pickups, completes the route and verifies gem reset after replay. It uses privileged world
state for navigation; this is not a human playtest or physical controller test.

The final gameplay suite passed **187/187 EditMode + 76/76 PlayMode** tests with no skips.
These cover value conservation at pool capacity, magnet delay and occlusion, expiry,
single-payout enemy/container destruction, real melee/projectile damage, animated muzzle
height, non-blocking pickups, pause, death/retry, collection gates, banking and replay.
The debris test checks both cache types: six pieces, decreasing scale and alpha, pause
freezing the effect, disappearance within one second, and full-size restoration on retry.
Evidence: [EditMode XML](Validation/Alpha001_EditMode.xml),
[PlayMode XML](Validation/Alpha001_PlayMode.xml).

The final 1080p controller route completed all three rooms and replay: **18 enemy drops,
6 crates, 3 chests, 134 gems spawned and collected, zero expired, peak 25 visible pickups**.
It took 22 damage with no deaths and no shots through guard. Both normal and loot replay
resets passed. Evidence: [session](Images/Alpha001_Final_1080p/session.json),
[metrics](Images/Alpha001_Final_1080p/metrics.json),
[chest burst](Images/Alpha001_Final_1080p/chest-1-burst.png),
[partial shrink/fade](Images/Alpha001_Final_1080p/chest-1-collection.png),
[cleared floor after one second](Images/Alpha001_Final_1080p/chest-1-cleared.png).

The 720p route also completed and replayed with all 134 gems collected from the 18 enemies
and nine containers, no expired gems and no deaths. Its crate/chest burst, partial fade,
cleared-floor, grayscale and pause captures were checked at native resolution.
[720p session](Images/Alpha001_Final_720p/session.json),
[720p metrics](Images/Alpha001_Final_720p/metrics.json).
The subsequent [720p verification run](Images/Alpha001_Verified_720p/session.json) tested
the packaged executable after review-telemetry changes: all 134 gems collected, both replay
checks passed, no deaths and zero shots under either the authoritative guard flag or the
shield controller's cached raised state. Its gameplay/replay results were written successfully,
but the automated player then stalled during teardown and required termination of that exact
task-owned process. This is not a clean-exit pass. The earlier final 1080p/720p players and
`Alpha001_Final_Stress` player exited normally. An earlier failed diagnostic route also exhibited
this intermittent review-teardown issue; it remains a tooling follow-up.

A separate 30-second synthetic stress run kept all seven final-room enemies alive while
spawning a 12-gem burst every 0.4 seconds: **900 gem units, peak 36 visible pickups,
738 units merged**, with the pool remaining below its 40-object cap. Some uncollected gems
expired as designed. On the RTX 2060, this run measured **7.39 ms mean / 13.89 ms P95**;
maximum 20.85 ms, mean 2 / maximum 198 allocated bytes per sampled frame. This is one
development-build sample, not a stable-60-FPS certification. The route's screenshot and
debug-log captures are unsuitable for a clean performance claim. Evidence:
[stress metrics](Images/Alpha001_Isolated_Stress/metrics.json),
[pool metrics](Images/Alpha001_Isolated_Stress/loot-metrics.json),
[stress capture](Images/Alpha001_Isolated_Stress/02-combat.png).
This repeated the earlier stress workload on the final build with no other review player
running; the preceding `Alpha001_Final_Stress` run overlapped a player stalled in teardown
and is retained only as supplementary evidence.
The isolated stress player's results were saved, but automatic teardown again stalled;
closing that exact task-owned window completed shutdown with exit code 0. This required
intervention and is not counted as an automatic clean-exit pass.

The generated art was inspected in Blender-source and in-game captures. See
[source preview](Images/Alpha001_Loot_Source.png) and
[source report](Validation/Alpha001_Loot_Source.json). The
[Unity import report](ART_ASSET_REPORT.json) records five meshes/material slots for each
intact cache, and six meshes / 17 material sections / 2,156 triangles for each broken cache.
These transient chunks render for at most one second; no debris casts shadows.
No Unity packages were added.
The pre-existing gate material matches its pre-task SHA-256 exactly; the user's character,
environment and other art changes were retained.

## Issues found during implementation review

The first extended route exposed unreliable firing at the new low containers. A dedicated
test reproduced the issue only after allowing the firing animation to play: the muzzle
was at 1.36 m while the crate's top was 1.12 m. Its first low shot could hit, but subsequent
shots passed above the cache. The source models were raised along with their collision
heights (crate collider 1.42 m, chest collider 1.52 m). The regression now fires through the actual animated
caster in four cardinal directions for both cache types. The player's firing contract and
avatar anchors are unchanged. [Failing reproduction](Validation/Alpha001_Muzzle_Reproduction.xml).

The scripted route also now re-centres after loot detours before approaching gate posts.
This changes the review driver's navigation, not player movement. Intact container export
combines surfaces by material (five meshes/material slots). Following the debris request,
broken exports combine by physical chunk (six meshes with retained material sections),
preserving individual pivots for the one-second animation. Editable Blender source parts
remain separate. No art from Vampire Hunters is used.

The first 720p run gathered all 33 first-room gems but timed out before the next room.
Navigation diagnostics reproduced a control-scheme switch from the virtual Gamepad to
Keyboard: movement became zero with no blocking collider, movement suspension or pause.
Desktop activity could take control of the review. The opt-in driver now pins its gamepad
control scheme and restores the previous setting at teardown. Normal player input remains
unchanged. The diagnostic run was closed after confirming this, so it has no completed
session. Evidence: [failed route](Images/Alpha001_Delivery_720p/session.json),
[navigation trace](Validation/Alpha001_ReviewInput_Reproduction.json).

One earlier 720p session recorded a shot while `ShieldController.IsRaised` was still true.
Code inspection found that melee can revoke the guard action before `Ranged.Update`, while
`Shield.Update` reconciles its cached state afterwards. This makes that callback-time metric
ambiguous on a guard-release frame. The review now records the authoritative `Shielding`
action flag and the raw controller state separately. Neither observation occurred in the
verification rerun; the original event was not reproduced, so its guard-release explanation
remains an inference. No combat rules were changed for this telemetry adjustment.
The [original observation](Validation/Alpha001_GuardTelemetry_Observation.json) is retained
for follow-up if a human playtest shows an actual firing/guard overlap.

## Playable package

Extract `Builds/Vaultbreakers_Alpha_0.0.1_Windows.zip`, then run `Vaultbreakers.exe` or one
of the included 720p/1080p launchers. The existing local development folder is
`Builds/Vaultbreakers_POC/`. Keep that folder intact. The historical combat-only ZIP is
retained separately and does not contain this alpha.

The alpha archive contains controls, third-party notices and the tested Windows player.
ZIP CRC verification passed, and both the packaged executable and runtime assembly match
the tested build byte for byte. The 74,457,356-byte archive's SHA-256 is
`8dc77f8aa7f2e6fcd92fab02fb581853f7717054bcf26559fde1963853424586`.
[Package verification](Validation/Alpha001_BuildPackage.json). This is a working-tree build;
the report's Git commit identifies its base, not a claim that all delivered changes are committed.

Remaining acceptance work: human feel/readability/balance feedback and a physical controller
session. Stable 60 FPS is not certified across machines or sessions. The pre-existing
ComputeBuffer shutdown warning, intermittent automated-review teardown, and the single
unreproduced guard-transition telemetry observation above are retained as follow-ups.
