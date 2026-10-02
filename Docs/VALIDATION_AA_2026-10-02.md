# Stylized graphics overhaul validation — 2026-10-02

## Player orientation and melee revision

The user subsequently reported a forward-tilted player and rejected the circular melee
effect. The hammer is retained at their explicit request. Facing now rotates an upright
`ModelRoot` pivot above `ImportedRig`, keeping the imported Animator's FBX axis conversion
intact. A regression reproduced 90°/95° idle/run tilt when facing replaced the old import
transform; the corrected hierarchy passes pose and runtime checks at 30/60/120 capture rates.

Melee uses a reusable tapered mesh with an ivory leading edge, warm trailing color and a
thin echo. Its vertex-color URP shader fades the ribbon without a filled disc. The old
dungeon-specific circular LineRenderer is removed, and the dungeon explicitly enables the
shared slash. Existing hit sparks, damage, attack timing, lunge and the hammer are retained.

- **177 EditMode + 69 PlayMode tests pass**: [EditMode XML](Validation/Slash_EditMode.xml),
  [PlayMode XML](Validation/Slash_PlayMode.xml).
- Checks include idle/run facing in four directions, recovery from a pitched dodge pose,
  finite slash vertices through attack/recovery, and the real dungeon's slash visibility.
- The initial effect test caught a negative floating-point sine at the tapered tip; clamping
  before the fractional power fixes it. The full suite passes after correction.
- The orientation-only normal-speed mixed-controller route completed and replay reset passed
  (13 melee swings, 26 shots, 3 guard raises, 1 block, 2 dodges, no deaths).
- Local logs: `Logs/Orientation_Reproduction`, `Logs/Orientation_Fix`, `Logs/Slash_Delivery`.
  The existing gate-material edit remains byte-for-byte intact. No packages or bind poses changed.
- Final Windows development build succeeds. The final mixed-controller route completes all
  three rooms and replay resets correctly: 10 melee swings, 25 shots, 3 guard raises,
  2 blocks, 2 dodges, no deaths, no firing through guard. See
  [session](Images/Slash_Delivery_Route/session.json).
- Actual-player [slash screenshot](Images/Slash_Delivery_Video/melee-slash.png),
  [impact screenshot](Images/Slash_Delivery_Video/melee-impact.png),
  [upright movement](Images/Slash_Delivery_Video/upright-running.png), and
  [25-second video](Videos/Slash_Delivery_Video.mp4), H.264 / 720p / 30 fps / silent.
  Reviewed frames include cutting, impact, recovery, movement and the vault reward.
  Video uses the melee-focused policy 0, completes the route, and trims before grayscale
  diagnostics at exclusive frame 721 with a one-second hold/fade. Recording replay remains
  unreliable under capture timing; the separate normal-speed run above verifies it.
- No runtime exceptions, invalid mesh bounds or shader errors remain in either final run.
  The existing shutdown ComputeBuffer-disposal warning persists. Frame-recording timings
  are not performance measurements; sustained performance and physical-controller feel
  remain subject to the previously documented checks and human visual acceptance.

The sections below retain the original graphics-overhaul evidence; revision captures are
listed here separately so the earlier video is not mistaken for the corrected build.

## Implemented

Rebuilt all three Dock 9 environments as industrial salvage, pressure-vessel transfer and
black-glass vault spaces. Added fitted character armor/equipment and enemy role details,
weighted normals, generated surface maps, contact occlusion, local reflection probes,
warm/cool lighting, restrained bloom, ACES, vector HUD icons and tapered impact particles.
Gameplay movement, damage, attack timing, collision, camera tracking and wave counts remain.
The prior gate-material edit is preserved byte-for-byte. No package was added.

The visual review required three iterations: the first was too dark, the second showed
excessive floor grain/reflections, and the delivered version reduces both while retaining
readable silhouettes. Deck labels were corrected for the gameplay camera and separated.

## Build and tests

- Pinned Unity 6000.4.4f1 / URP 17.4.0: setup, compile and Windows development build succeed.
- **175/175 EditMode tests pass** on the delivery assets; [XML](Validation/AA_EditMode.xml).
- **66/66 PlayMode tests pass** on the final runtime code; [XML](Validation/AA_PlayMode.xml).
  Subsequent edits only changed generated surfaces, lighting and a decorative label;
  setup, EditMode, build and actual-player route checks were repeated afterward.
- All model `.meta` identities remain. Schema-1 bones and socket/module checks pass.
- Blender front/back player and individual enemy source renders were inspected.
- Imported art report: [ART_ASSET_REPORT.json](ART_ASSET_REPORT.json).
  Dock scenery has 32 combined meshes and 210,964 imported triangles across all three rooms.
  The editable source retains 1,505 individual parts.
- Runtime logs contain no exceptions, missing-reference failures or shader errors.
  Unity emits a ComputeBuffer disposal warning during player shutdown; this remains unresolved.
  Batch logs also contain package/test-assembly and debugger shutdown warnings.
- The combined stress shell exited 143 after writing the 1080p results; no player remained.
  The 720p stress run was launched separately and exited successfully.

Raw local logs: `Logs/AA_Delivery`, `Logs/AA_Final`, `Logs/AA_Overhaul`, and
`Logs/Dock9Graphics/AA_*`. Test-generated Resources metadata was removed after validation.

## Actual player captures

- [Salvage intake, 1080p](Images/AA_Delivery_Route/01-arena.png)
- [Transfer court, 1080p](Images/AA_Delivery_Route/zone-2.png)
- [Black-glass vault, 1080p](Images/AA_Delivery_Route/zone-3.png)
- [Sustained mixed combat](Images/AA_Delivery_1080p/02-combat.png)
- [720p grayscale / bloom disabled](Images/AA_Delivery_720p/03-grayscale.png)
- [720p pause/settings](Images/AA_Delivery_720p/04-pause.png)
- [24.1-second gameplay video, 720p / 30 fps / silent](Videos/AA_Overhaul.mp4)
- [Player front](Images/Vaultbreaker_Modular_Preview.png) and
  [back](Images/Vaultbreaker_Back_Preview.png) are Blender source renders.

The movie records actual Windows-player frames with a scripted virtual controller. The
recording completes the three-zone route and core recovery. Diagnostic grayscale, pause,
replay and device-teardown frames were trimmed; the final frame holds briefly and fades.
The recorded-run replay probe returned false under capture timing; replay is validated by
the separate normal-timing route below. Recording timings are excluded from performance data.

Reproduce the capture and reviewed trim:

```bash
python3 Tools/Unity/video.py <new-label>
python3 Tools/Unity/video.py AA_Overhaul --encode-only --end-frame 693
```

The frame number is specific to this recording; review another run before choosing its trim.
Raw frames are retained under ignored `Logs/AA_Overhaul/VideoFrames`.

## Combat and performance evidence

The normal-health mixed controller route completed and replay reset passed. It recorded
11 melee swings, 24 shots, 3 guard raises,
2 blocks and 2 dodges, with 0 deaths and
0 shots fired while guard was raised. This uses privileged
world-state navigation and does not measure human understanding or enjoyment.

NVIDIA GeForce RTX 2060, Windows development player, shared workstation. Stress uses seven
immortal enemies and an invulnerable player; its captures happen after the timed interval.
Route maxima include image-capture stalls. No video recording ran during these measurements.

| Workload | Mean frame ms | P95 ms | Max ms | Mean / max GC bytes |
|---|---:|---:|---:|---:|
| 1080p normal-health route | 8.89 | 13.90 | 180.56 | 61 / 58318 |
| 1080p 30-second mixed stress | 11.17 | 20.84 | 27.78 | 2 / 198 |
| 720p 30-second mixed stress | 10.95 | 20.83 | 62.51 | 2 / 198 |

The route P95 is inside 16.67 ms. Sustained stress P95 exceeds the 60 FPS budget at both
resolutions despite mean rates around 90 FPS. A stable 60 FPS claim is **not established**;
frame pacing needs further profiling on an isolated reference machine. See
[AA_Overhaul.json](Validation/AA_Overhaul.json) for machine-readable results.

## Remaining acceptance

Human art-direction acceptance, hands-on controller feel and subjective enjoyment remain.
The existing short in-place animation set is retained; this revision does not establish
production-quality animation or AA production sign-off. The sustained frame-time gate and
shutdown buffer warning remain follow-up work. No post-POC gameplay system was introduced.
