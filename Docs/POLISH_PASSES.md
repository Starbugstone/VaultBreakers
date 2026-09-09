# Combat POC polish passes — 2026-09-09

The user authorized labelled simulation/reviewer judgement while AFK, requested three polish,
sound and graphics passes, then requested lower enemy health and a substantially larger area.
No human-session data is fabricated. Later progression systems remain deferred.

## Pass 1 — feedback and diagnostics

Reduced-flash settings immediately hide current muzzle/impact effects and suppress subsequent
ones without changing damage or shots. Hidden F1 IMGUI has its own disabled view component;
F1 can still reopen it. Debug enemy spawning follows the current room.

63 PlayMode tests passed; Windows development build succeeded. A real 1080p 30-second mixed-role
stress run retains readable room geometry under eight overlapping impact bursts:
[combat capture](Images/Dock9_Polish1/02-combat.png),
[metrics](Images/Dock9_Polish1/metrics.json). P95 13.896 ms, mean 8.052 ms, max 20.841 ms.
A sound-source audit found identical noise-heavy construction across the cues, no attack/release
taper and a runtime-dependent string hash seed; those findings drive pass 2.

Mean GC 1,699 bytes/frame, max 128,756: this run does **not** establish an allocation improvement.
The workload keeps seven enemies alive with synthetic HP and makes the review player invulnerable;
it is a graphics/performance check, not a difficulty or playtest result.

## Pass 2 — larger rooms, faster kills, sound and animation

Rooms expand to 24 × 24 metres (2.94× previous area),
with coherent scenery/collision/gates/bridges and bounded fixed-angle camera tracking.
Grunt/Shooter/Bruiser HP changes 30/24/75 → 18/14/42. New original tapered sound synthesis
separates energy shots, metal impacts, swishes, shield and progression cues.
Blender-authored movement adds arm/torso motion; dodge tucks both legs; melee returns to rest.
A generic upper-body mask preserves locomotion beneath guard/fire/melee without root motion.

175 EditMode + 63 PlayMode tests passed; the Windows build succeeds. The actual 1080p
scripted route clears all enlarged rooms and claims the core:
[second-room capture](Images/Dock9_Polish2/zone-2.png),
[metrics](Images/Dock9_Polish2/metrics.json). Duration 25.955 seconds including capture cleanup;
P95 6.951 ms, mean 7.196 ms, maximum 199.821 ms (in-route screenshot stall included),
mean GC 54 bytes/frame. This route still uses player invulnerability, so it verifies traversal
and presentation, not difficulty. The new normal-health policies belong to pass 3.

Eleven original WAV cues exported for inspection; every cue starts/ends at zero, maximum
single-cue PCM peak 0.627. [Sequence preview](Audio/ReviewSequence.wav) and
[signal review](Audio/SignalReview.json). This measures source signals, not physical speakers.
Blender front/back renders were re-inspected after export; the ten clip/socket tests pass.

## Pass 3 — final integration and controller policies

175 EditMode + 66 PlayMode tests pass, including a live Animator assertion that the leg
rotation changes while the upper body remains in the raised-shield pose. High-quality SMAA
smooths silhouettes and floor edges. Grunt, shooter and bruiser windups now have distinct,
quiet original cues, still subject to the bounded combat one-shot mix.

The first final-build attempt failed inside Unity's build-data generator with
`System.OutOfMemoryException`, not a C# diagnostic. Other unrelated Unity projects were running
on the shared Windows machine. A finished pass-1 review process had lingered after writing its
captures/metrics; only that owned process was closed. The retry uses two Unity workers and
process-local .NET memory conservation. No unrelated application was closed. The retry succeeded. The failed build had left Unity's
temporary input-action preload entry in ProjectSettings; only that injected entry was restored
to its original empty list after the successful retry.

The first normal-health ranged-pressure run completed with 198 shots (10.486-second first wave),
compared with the melee-first policy's 17 swings / 12 shots. Inspection found the offset arm
barrel firing parallel to the centre aiming ray. Pass 3 now adds up to 15° of convergence onto
the surface already under that ray. A physics regression explicitly demonstrates the old
parallel miss, the corrected hit and unchanged cover blocking. Baseline runs are retained in
`Images/Dock9_Baseline_*`; final policy results must use the corrected build.

A final actual-graphics inspection exposed a black shield band: front/back triangles shared
vertices, so their opposite normals cancelled. The band now gives each side separate vertices;
a regression checks unit-length, opposing normals. Blocking arc, damage and stability are
unchanged. The first four corrected-aim controller policies predate this presentation-only
repair; the mixed policy and final graphics/stress captures use the repaired build.

All fourteen exported sound cues pass the PCM signal inspection (maximum single-cue peak
0.627; silent endpoints). All five normal-health controller policies finish all three rooms, claim the core and
reset successfully using Start. There are no deaths or shots fired while guard is raised
in these runs. Strategy-specific action counts follow; zero means that policy did not use
the action, not that a human failed to discover it.

## Controller session record — simulations only

| Policy | First wave (s) | Melee swings | Shots | Guard raises / blocks | Dodges | Damage taken | Core / replay reset |
|---|---:|---:|---:|---:|---:|---:|---|
| MeleeFirst | 3.10 | 16 | 11 | 0 / 0 | 0 | 0 | Pass / pass |
| RangedPressure | 2.01 | 0 | 46 | 0 / 0 | 0 | 16 | Pass / pass |
| GuardApproach | 3.78 | 14 | 20 | 5 / 4 | 0 | 0 | Pass / pass |
| Evasive | 2.24 | 7 | 34 | 0 / 0 | 6 | 0 | Pass / pass |
| Mixed | 2.97 | 13 | 26 | 3 / 1 | 2 | 30 | Pass / pass |

[Machine-readable results](Validation/ControllerPolicies.json). Median first-wave time is
**2.97 seconds** for these programmed policies. Their aiming/navigation uses privileged enemy
positions, so this is not a prediction of new-player clear times. Shield/fire exclusion is
mechanically observed (zero simultaneous shots); understanding and replay preference are
**not measurable by simulation**. Replay here means an actual scripted Start-button reset.

The repaired shield is visibly cyan in the [final first-room capture](Images/Dock9_Controller_Mixed/zone-1.png).
The ranged-pressure policy drops from 198 shots / 10.49-second first wave to 46 shots /
2.01 seconds after barrel convergence. Runs were not a controlled human or statistical study.

## Final graphics and frame measurements

| Final-build workload | Mean frame (ms) | P95 (ms) | Maximum (ms) | Mean / max GC bytes |
|---|---:|---:|---:|---:|
| [1080p normal-health mixed route](Images/Dock9_Controller_Mixed/metrics.json) | 10.398 | 13.896 | 215.282 | 69 / 58318 |
| [720p normal-health mixed route](Images/Dock9_Final_720p/metrics.json) | 7.103 | 6.951 | 104.170 | 47 / 58318 |
| [1080p 60-second mixed stress](Images/Dock9_Polish3/metrics.json) | 11.605 | 20.840 | 76.395 | 2 / 410 |
| [720p 60-second mixed stress](Images/Dock9_Polish3_720p/metrics.json) | 7.209 | 6.952 | 41.675 | 1 / 384 |

720p steady-state P95 meets the 60 FPS budget; 1080p steady-state P95 does not under this shared
workload. Other Unity games/editors and Blender were active. Route maxima include capture
stalls; stress captures happen after the measured interval. Steady stress uses seven immortal
enemies and an invulnerable player, whereas controller routes use normal health. The 720p
stress wrapper ended with status 143 after its metrics/captures were written; a check found
no lingering player from that run. No human-playtest or uniform-1080p-performance claim is made.

All three requested polish/sound/graphics passes are complete. See `POC_RESULTS.md` for the
current delivery decision, known limits and recovery instructions.
