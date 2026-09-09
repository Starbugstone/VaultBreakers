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

Pending verification; this section must be completed with measured results before delivery.
