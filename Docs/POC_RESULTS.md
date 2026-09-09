# Dock 9 combat POC results

Recorded 2026-09-09. The user authorized scripted controller simulation/reviewer judgement
while AFK and requested three further polish passes, larger rooms and lower enemy health.
The current iteration is recorded in [polish passes](POLISH_PASSES.md); measurements below
are the earlier baseline unless explicitly updated. Human understanding and replay preference
are not claimed. Later progression systems remain outside this combat milestone.

## Delivered build and loop

Windows x64 development build: `Builds/Vaultbreakers_POC/Vaultbreakers.exe`.
Keep its entire folder together. Unity 6000.4.4f1, URP 17.4.0, explicit
`BuildOptions.Development`; Dock9_Dungeon is the first enabled build scene.

Three connected Shatterbelt zones, fixed-angle bounded camera tracking, traversal between cleared
encounters, Grunt/Shooter/Bruiser roles, melee/fire/directional guard/dodge, room retry,
full-kit checkpoint resets, run-local salvage score, one-time core reward and replay.
The clean Blender pilot, robot enemies, equipment, scenery and animation exports replace
the earlier arena presentation. See [current scope](DUNGEON_POC.md) and
[controls/status](PROJECT_STATUS.md).

## Automated and graphics checks

| Check | Result / evidence |
|---|---|
| EditMode | **170 passed, 0 failed, 0 skipped** — [XML](Validation/EditMode.xml) |
| PlayMode | **61 passed, 0 failed, 0 skipped** — [XML](Validation/PlayMode.xml) |
| Windows build | Succeeded; explicit development build, no C# compiler errors |
| 1920 × 1080 scripted route | Completed three encounters, physical gate traversal and core claim |
| 1280 × 720 scripted route | Completed the same route |
| Rendering | Actual GPU captures inspected: character/material imports, room framing, health bars, effects, grayscale and pause UI |
| Asset contract | 18 bones, 9 sockets, all 12 modular variant IDs retained; imported normals checked |
| Source control hygiene | Unity metadata present; package manifests unchanged; no new package dependency |

The 231 tests cover the deterministic combat rules, action priority/cancellation,
shield aiming, projectile sweep/pooling, enemy attacks and removal, wave counting,
death/retry resets, repeated runs, pause, controller disconnection, equipment imports,
dungeon gates/props, camera locking, score reset and one-time reward behavior.
Later changes adjust the opt-in review bot's doorway waypoints and flash stress capture,
and cap bloom input brightness in the profile and its setup builder. They are validated
by compilation and the subsequent real-graphics checks; deterministic combat is unchanged.

Actual Windows screenshots:

- [Intake](Images/Dock9_1080p/zone-1.png), [transfer court](Images/Dock9_1080p/zone-2.png), [vault fight](Images/Dock9_1080p/zone-3.png).
- [Core recovered](Images/Dock9_1080p/02-combat.png), [grayscale world](Images/Dock9_1080p/03-grayscale.png), [pause settings](Images/Dock9_1080p/04-pause.png).
- [720p fight](Images/Dock9_720p/zone-3.png), [720p pause](Images/Dock9_720p/04-pause.png).
- [Imported default rig](Images/Vaultbreaker_Unity_Preview.png), [alternate modules](Images/Vaultbreaker_Unity_Alternate_Preview.png).

The gameplay captures come from the Windows executable. The gallery comes from Unity's
real graphics renderer. [Front](Images/Vaultbreaker_Modular_Preview.png) and
[back](Images/Vaultbreaker_Back_Preview.png) images render the actual Blender source.
`Images/POC_1080p` records the earlier arena and is historical evidence, not this delivery.

## Performance method and measurements

Reference machine: Intel Core i7-10750H, approximately 32 GB RAM, NVIDIA GeForce RTX 2060
(6 GB), Direct3D 12. These measurements describe this machine and workload only.

The opt-in review harness uses a virtual Gamepad and an invulnerable player. Journey
runs use normal enemy health and walk through gates; no teleporting or direct enemy
kills are used. Their short clear times are **not player-balance measurements**.
Screenshots taken at room transitions are inside the journey interval and can stall frames.

| Workload | Mean frame ms | P95 ms | Maximum ms | Mean GC bytes/frame |
|---|---:|---:|---:|---:|
| 1080p journey | 7.15 | 6.95 | 173.62 | 425 |
| 720p journey | 7.05 | 6.95 | 97.22 | 425 |

[1080p data](Images/Dock9_1080p/metrics.json), [720p data](Images/Dock9_720p/metrics.json).
The 30-second Editor mixed-encounter sample (2560 × 1440 Game view) measured mean 6.31 ms, P95 9.27 ms and maximum 76.82 ms, with 11,301 mean / 129,909 maximum GC bytes per frame. See [Editor data](Images/Dock9_Editor/metrics.json). Editor overhead is included; this Editor sample also preceded the final bloom cap. The **600-second 1080p Windows mixed encounter before the final bloom cap** completed with 69,636 sampled frames: mean **8.59 ms**, P95 **13.90 ms**, maximum **97.23 ms**; mean **409 bytes/frame** and maximum **888 bytes/frame** of managed allocation. See [sustained data](Images/Dock9_Soak/metrics.json) and [end-of-run capture](Images/Dock9_Soak/02-combat.png). All seven enemies remained active. No gameplay exception appeared during the run. The final image exposed a large bloom flare: these endurance captures document that defect, not the corrected visual state. `completed: false` is expected in this endurance mode: the encounter is deliberately kept alive.

The sustained workload starts the final mixed encounter and gives its seven enemies
1,000,000 health so it remains active. Input cycles movement, firing, shield, melee and
dodge. Two seconds of warm-up are excluded; end screenshots occur after frame sampling.
`durationSeconds` includes roughly four seconds of capture/cleanup after the requested
interval. GC numbers are whole-frame allocations, including input injection, UI and
engine work; they do not isolate gameplay code or prove zero-allocation updates.

The sustained P95 is below the 16.67 ms frame budget for 60 FPS. The 97.23 ms maximum
means occasional hitches remain; this is not a claim that every frame met the target.
Allocations stayed small in this synthetic workload, but an allocation-free gameplay
claim and memory-leak sign-off would require profiling without injected review input.

The corrected build was measured for **60 seconds at 1080p**: mean **7.37 ms**, P95 **13.89 ms**, maximum **20.84 ms**, with **409 mean / 888 maximum GC bytes/frame**. The capture then triggers all eight pooled impact flashes together. The [overlap capture](Images/Dock9_FlashStress/02-combat.png) keeps the room readable; [raw data](Images/Dock9_FlashStress/metrics.json) records the preceding timed interval. Both journey runs and their screenshots above were repeated with the capped bloom profile. The 600-second run was not repeated after this rendering-only cap; its earlier measurements remain identified separately.

## Asset costs and reproducibility

The saved-source and imported counts are recorded separately in
[Blender source report](BLENDER_SOURCE_REPORT.json) and [Unity import report](ART_ASSET_REPORT.json).
Export-only joining combines the environment by material and zone; the `.blend` retains
1,131 independently editable mesh objects. Source counts include helper geometry that
is excluded from the explicit FBX export.

| Imported asset | Meshes / material slots | Triangles | Bones |
|---|---:|---:|---:|
| Complete Dock 9 scenery | 43 / 43 | 107,020 | 0 |
| Modular player, all variants | 68 / 68 | 21,920 | 18 |
| Scrap Grunt | 28 / 28 | 9,224 | 18 |
| Repo Shooter | 23 / 23 | 8,164 | 18 |
| Heavy Bruiser | 23 / 23 | 8,340 | 18 |

Player counts include mutually exclusive modules; the whole FBX is not displayed at once.
These are POC assets with multiple rigid skinned parts; further batching and animation
polish remain possible. All sources/exports are original project assets under the root
proprietary license. Dependency exceptions remain in `LICENSES/`.

Reproduce on the configured WSL/Windows workstation:

```sh
python3 Tools/Unity/validate.py Review setup EditMode PlayMode build
python3 Tools/Unity/review.py journey 1920 1080 Dock9_1080p
python3 Tools/Unity/review.py journey 1280 720 Dock9_720p
python3 Tools/Unity/review.py benchmark 1920 1080 Dock9_Soak 600
python3 Tools/Unity/review.py benchmark 1920 1080 Dock9_FlashStress 60 --poc-stress-flashes
```

The review switch is disabled in normal play and compiled only for Editor/development
builds. Review input/background settings are restored on exit. No gameplay invulnerability
or inflated enemy health is enabled when launching the build normally.

## Backup verification

Source/art checkpoint `ffdb6aaa93af48e392081ca7f9059e13e56456c7` was pushed to
GitHub `main`. A separate clone restored **547 tracked files and 50 real Git LFS files**;
`git lfs pull` and `git lfs fsck` passed. No Blender/FBX/image remained an unresolved
pointer. The evidence commit adds the sustained-run captures and report; the complete final tree is also fetched and LFS-checked after pushing.
Generated Unity caches, raw logs and the reproducible Windows build remain local.
The proprietary root license and third-party notices are included in the restored tree.

## Human follow-up and known limits

- **Five human sessions remain unmeasured; the user waived them as a delivery blocker.** Future research can record action
  discovery, deliberate use of all four actions, understanding of fire/shield exclusion,
  first-wave time and replay interest against the Phase 13 thresholds.
- Subjective character quality, combat feel, camera comfort, rumble strength and difficulty
  need the user's review. Mouse/controller logic is covered by tests; physical rumble is not.
- Combat and progression sounds use original tapered synthesis, with explicit enemy windup cues.
  Ten authored animation clips remain presentation-only; gameplay owns all timing.
- No persistent loot/inventory, gems/gauges, Relic power, scans/cards, pets or co-op are claimed.
- Unity reports a ComputeBuffer disposal warning and native allocation diagnostics during
  development-player teardown. No gameplay exception was found in the completed route logs;
  the shutdown warning remains recorded rather than represented as a clean leak audit.

The decision is to test and tune this playable combat loop before implementing the next
systems. No claim is made that the documented human playtest gate has passed.

## Historical audit before the AFK simulation override

This historical audit predates the user's explicit simulation authorization and larger-room
request. The current delivery decision and measurements are in `POLISH_PASSES.md`.

Audit baseline: `a35e8d2b4d968a7a445b633f0276de78aa4556c3`, inspected after delivery.
The local tree was clean and matched the delivered source. The Windows executable,
final build-success log, enabled entry scene, test XML and graphics JSON were re-read;
tests were not rerun for this documentation correction. The source tests were inspected
to distinguish their actual assertions from broader human acceptance claims.

| Plan requirement | Evidence and remaining acceptance |
|---|---|
| 3D URP in Editor and Windows development build | Established: pinned 6000.4.4f1; Dock9_Dungeon first in build settings; final development build succeeded and both graphics routes completed. |
| Controller movement, aim, four actions, pause, disconnect/reconnect | Input/action and virtual disconnection behavior tested. **Physical controller and reconnection acceptance open.** |
| Immediate gameplay and visual response | Timing, cadence, buffering and cancellation assertions pass. **Perceived responsiveness and movement comfort open.** |
| Core action conflicts | Established by combat policy and melee/ranged/shield/dodge tests. |
| Front/rear shield behavior | Established by arc boundary tests and actual projectile-to-shield/health PlayMode assertions. |
| Collision-safe dodge and invulnerability | Established by distance/timing/cooldown tests and wall/corner PlayMode cases. |
| Distinct enemy mechanics and appearance | Three authored roles, committed attack tests and actual gameplay captures. **Fresh-player recognition and tell comprehension open.** |
| Waves complete, fail and retry without stale state | `ThreeCompleteRunsDoNotSoftLockOrCarryMembership`, lethal projectile reset, current-room retry, core/replay tests and both scripted graphics routes pass. Direct-damage tests prove state flow; the graphics bot separately uses game inputs. |
| Readable HUD, threats and feedback | 1080p/720p, grayscale and eight-flash overlap captures inspected. **Human mixed-encounter readability and reduced-feedback playtest open.** |
| Deterministic and reset-critical tests | Established: 170 EditMode and 61 PlayMode passed, zero skipped. Source assertions inspected for the wave/reset/device cases above. |
| Reference-machine performance without recurring allocation problems | Measured with the workload limits recorded above. Fixed projectile-pool growth is tested; whole-frame GC includes diagnostics/input/UI. P95 meets the 60 FPS budget, but maximum-frame hitches and strict allocation-free behavior are not represented as proven. |
| Phase 13 fresh-player gate | **Not passed: no five-player session results have been supplied.** Automated/invulnerable runs do not count as fresh-player sessions. |
| Asset provenance and licensing | Original Blender sources, reproducible exporters and import reports present; proprietary LICENSE and third-party register retained. |
| Compile and repository hygiene | Final build succeeded without C# errors; clean source tree at audit baseline; final fresh restore verified 557 tracked files and 58 real LFS files. |
| Results note and milestone recommendation | This note records tests, measurements, tuning, remaining risks and **iterate with human playtests**. It does not authorize the next systems or mark the human gate passed. |

Optional future human research should include the manual scenarios in
[the combat plan, sections 7 and 9](COMBAT_POC_PLAN.md): movement comfort, melee/ranged/shield/dodge
situations, enemy comprehension, failure/retry, grayscale/reduced feedback and mixed-tool play.
For the five fresh players, record first-wave completion/time, deliberate use of all four
actions, fire/shield explanation, deaths/confusion and replay choice without coaching their
first attempt. Apply the exact four-of-five, median-under-30-seconds and three-of-five
replay thresholds from Phase 13. No tester identities or results have been invented.
