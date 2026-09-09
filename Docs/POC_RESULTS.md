# Dock 9 combat POC — delivery results

Recorded 2026-09-09. **Ready for hands-on combat POC testing under the user's explicit AFK
simulation/reviewer-judgement authorization.** The delivered scope is the combat loop;
gems/gauges, Relic powers, scanning/cards, pets, inventory persistence and co-op remain deferred.
No human playtest findings or replay preference are claimed.

## Playable build and level

Run `Builds/Vaultbreakers_POC/Vaultbreakers.exe`, or use the packaged Windows ZIP.
Keep/extract the entire folder. The package includes 720p/1080p windowed launchers,
controls and licensing. Unity 6000.4.4f1, URP 17.4.0, Windows x64, explicit development build.
The first scene is `Assets/Vaultbreakers/Scenes/Prototype/Dock9_Dungeon.unity`.

Three connected, dressed combat rooms with sealed gates, bridges, checkpoints, three enemy
roles, a final vault-core recovery and replay. Each floor is **24 × 24 metres**, up from
14 × 14 (2.94× area); room centres are 33.6 metres apart. Fixed-angle camera tracking preserves
close character framing. Grunt/Shooter/Bruiser health is **18/14/42**, down from 30/24/75.
Melee, hold-fire, directional guard, dodge, death/retry and run-local score are playable.

Original Blender player, modular equipment, robot enemies and environment assets retain
18 bones, nine sockets and twelve module variant IDs. Ten authored clips remain separate
from gameplay timing. Moving legs continue beneath guard/fire/melee upper-body poses.

## Three polish passes and automated checks

[The three-pass record](POLISH_PASSES.md) documents feedback/accessibility cleanup,
sound-source audit and redesign, larger rooms, faster kills, Blender animation changes,
SMAA, distinct enemy warnings, barrel-parallax correction and the shield normal repair.
Fourteen original cues and a [listen-through sequence](Audio/ReviewSequence.wav) are exported;
[PCM inspection](Audio/SignalReview.json) verifies finite, bounded cues with silent endpoints.

- **175 EditMode tests passed**, zero failed/skipped: [XML](Validation/EditMode.xml).
- **66 PlayMode tests passed**, zero failed/skipped: [XML](Validation/PlayMode.xml).
- Final Windows development build succeeded; no C# errors.
- Tests cover action conflicts, damage, directional guard, dodge collision/invulnerability,
  pooling, enemy tells/attacks, repeated wave runs, death/retry, gates/props, score/core reset,
  virtual device disconnection, moving-guard animation, barrel alignment/cover and shield normals.
- Blender front/back previews and real Windows 1080p/720p, grayscale and effect-stress captures
  were inspected. [Repaired cyan shield](Images/Dock9_Controller_Mixed/zone-1.png),
  [mixed-role effects](Images/Dock9_Polish3/02-combat.png),
  [720p pause controls](Images/Dock9_Final_720p/04-pause.png).
- Package manifests/dependencies were not changed. Original proprietary licensing is retained.

## Controller simulations

Five different programmed policies use virtual Gamepad input with **normal player/enemy
health**, actual game attacks and walking through the level. All claim the core and use Start
to verify a clean replay reset. All have zero deaths and zero shots fired while guard is raised.
The mixed policy deliberately combines both triggers and exercises all four verbs.

| Policy | First wave (s) | Melee | Fire | Guard raises / blocks | Dodge | Damage taken | Core / replay |
|---|---:|---:|---:|---:|---:|---:|---|
| MeleeFirst | 3.10 | 16 | 11 | 0 / 0 | 0 | 0 | Pass / pass |
| RangedPressure | 2.01 | 0 | 46 | 0 / 0 | 0 | 16 | Pass / pass |
| GuardApproach | 3.78 | 14 | 20 | 5 / 4 | 0 | 0 | Pass / pass |
| Evasive | 2.24 | 7 | 34 | 0 / 0 | 6 | 0 | Pass / pass |
| Mixed | 2.97 | 13 | 26 | 3 / 1 | 2 | 30 | Pass / pass |

[Full machine-readable records](Validation/ControllerPolicies.json). Median scripted first-wave
time is **2.97 seconds**. These policies use privileged enemy positions for aiming/navigation;
their speed is not a forecast for new players. Understanding of the fire/shield tradeoff and
voluntary replay choice are **not measurable by simulation**. Guard/fire exclusion is observed
mechanically; replay is a tested reset, not a preference. The first four policies precede the
last presentation-only shield normal repair; Mixed and both final graphics workloads use it.
Gameplay rules, enemy HP, room size and aiming logic are identical across these five runs.

The ranged-pressure baseline wasted 198 shots and needed 10.49 seconds for Wave 1. Correcting
the offset arm barrel onto the already-aimed surface reduces those observations to 46 shots
and 2.01 seconds. A separate physics regression verifies the old miss, corrected hit and cover.

## Performance and measurement limits

Reference machine: RTX 2060 6 GB, i7-10750H, 32 GB RAM, Windows/D3D12. Other Unity games,
editors and Blender were active on this shared desktop; they were not closed for profiling.

| Final-build workload | Mean frame (ms) | P95 (ms) | Maximum (ms) | Mean / max GC bytes |
|---|---:|---:|---:|---:|
| [1080p normal-health mixed route](Images/Dock9_Controller_Mixed/metrics.json) | 10.398 | 13.896 | 215.282 | 69 / 58318 |
| [720p normal-health mixed route](Images/Dock9_Final_720p/metrics.json) | 7.103 | 6.951 | 104.170 | 47 / 58318 |
| [1080p 60-second mixed stress](Images/Dock9_Polish3/metrics.json) | 11.605 | 20.840 | 76.395 | 2 / 410 |
| [720p 60-second mixed stress](Images/Dock9_Polish3_720p/metrics.json) | 7.209 | 6.952 | 41.675 | 1 / 384 |

The **720p steady mixed workload meets the 16.67 ms P95 budget** for a 60 FPS target on this
shared reference machine. The 1080p steady run averages about 86 FPS but its P95 misses that
budget; do not interpret it as uniformly stable 60 FPS. Use the 720p launcher while other
GPU-heavy applications remain active. An idle-desktop 1080p performance certification is
still a follow-up, not a result inferred from these measurements.

The steady workloads keep seven enemies alive with inflated HP and make the player invulnerable;
they exercise repeated fire, guard, dodge and melee for 60 seconds, then stress eight overlapping
flashes. `completed: false` is expected. Normal-health routes are separate. Route maxima include
in-route screenshot readback stalls. GC is whole-frame data including input injection/UI/engine
work, not proof that every engine subsystem allocates zero bytes. Final steady means of 1–2 bytes
per frame show no recurring allocation growth in this measured workload.

The earlier 600-second soak and pre-repair captures remain in `Docs/Images/` and Git history;
they are historical evidence, not substitutes for these final-build measurements.

## Known limits and recommendation

- Human discoverability, enjoyment, physical controller feel/rumble and speaker-output quality
  remain unmeasured. The user explicitly waived human sessions as this delivery's blocker.
- 1080p frame consistency under concurrent game/editor load remains the accepted POC performance
  limitation above; 720p is the tested conservative mode.
- Unity emits a ComputeBuffer disposal warning/native diagnostics at development-player teardown.
  Some capture wrappers ended with status 143 after all capture data was written; only the owned,
  completed review process was closed when it lingered. No gameplay exception or soft lock was
  observed in the completed final runs. This is not a clean native-leak/teardown certification.
- One build attempt exhausted Windows memory in Unity's build-data generator. The retry succeeded;
  subsequent final builds succeed with two Unity workers. No unrelated app was terminated.

Reviewer judgement: deliver this combat POC for hands-on play. Faster kills, the corrected aiming
line, larger navigable rooms and readable cyan guard address concrete arcade-playability issues.
Keep the next iteration in combat scope until the user has tried it; this judgement does not
assert that scripted sessions establish fun or authorize the deferred progression systems.

## Reproduce and recover

`python3 Tools/Unity/validate.py <label> setup EditMode PlayMode build` regenerates and checks
source assets. `Tools/Unity/review.py` performs individual graphics/controller runs;
`Tools/Unity/simulate.py --collect` aggregates the five named policy outputs.
`Tools/Unity/review_audio.py` regenerates the signal report/sequence from exported cues.
`Tools/Unity/package.py` packages the build and validates ZIP CRC/SHA-256.

Source, Blender files, Unity assets/metadata and review evidence are backed up through Git/Git LFS
on GitHub `main`. The packaged Windows build is prepared for a GitHub draft-release backup; its source revision
and checksum are recorded by packaging in `Validation/BuildPackage.json`.
The root `LICENSE` is proprietary; `LICENSES/THIRD_PARTY_ASSETS.md` records exceptions.
