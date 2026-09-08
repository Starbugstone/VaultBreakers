# Dock 9 combat POC results

Recorded 2026-09-09. Recommendation: **iterate with human playtests**. The implemented
combat POC is playable from intake to recovered vault core and replay. The Phase 13
fresh-player acceptance gate is still open; automated completion is not a fun or
controller-feel verdict. Later progression systems remain outside this combat milestone.

## Delivered build and loop

Windows x64 development build: `Builds/Vaultbreakers_POC/Vaultbreakers.exe`.
Keep its entire folder together. Unity 6000.4.4f1, URP 17.4.0, explicit
`BuildOptions.Development`; Dock9_Dungeon is the first enabled build scene.

Three connected Shatterbelt zones, locked combat framing, traversal between cleared
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
The last change after the suites only adjusts the opt-in review bot's doorway waypoints;
it was compiled in the final build and exercised by both successful graphics runs.

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
| 1080p journey | 7.10 | 6.95 | 178.15 | 425 |
| 720p journey | 7.01 | 6.95 | 83.33 | 423 |

[1080p data](Images/Dock9_1080p/metrics.json), [720p data](Images/Dock9_720p/metrics.json).
The 30-second Editor mixed-encounter sample (2560 × 1440 Game view) measured mean 6.31 ms, P95 9.27 ms and maximum 76.82 ms, with 11,301 mean / 129,909 maximum GC bytes per frame. See [Editor data](Images/Dock9_Editor/metrics.json). Editor overhead is included. The sustained Windows sample is still being recorded.

The sustained workload starts the final mixed encounter and gives its seven enemies
1,000,000 health so it remains active. Input cycles movement, firing, shield, melee and
dodge. Two seconds of warm-up are excluded; end screenshots occur after frame sampling.
`durationSeconds` includes roughly four seconds of capture/cleanup after the requested
interval. GC numbers are whole-frame allocations, including input injection, UI and
engine work; they do not isolate gameplay code or prove zero-allocation updates.

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
```

The review switch is disabled in normal play and compiled only for Editor/development
builds. Review input/background settings are restored on exit. No gameplay invulnerability
or inflated enemy health is enabled when launching the build normally.

## Remaining acceptance and known limits

- **Five fresh-player sessions with physical controllers are outstanding.** Record action
  discovery, deliberate use of all four actions, understanding of fire/shield exclusion,
  first-wave time and replay interest against the Phase 13 thresholds.
- Subjective character quality, combat feel, camera comfort, rumble strength and difficulty
  need the user's review. Mouse/controller logic is covered by tests; physical rumble is not.
- Combat sounds are generated placeholders. The ten code-driven animation clips establish
  readable POC actions, not a final animation/audio production pass.
- No persistent loot/inventory, gems/gauges, Relic power, scans/cards, pets or co-op are claimed.
- Unity reports a ComputeBuffer disposal warning and native allocation diagnostics during
  development-player teardown. No gameplay exception was found in the completed route logs;
  the shutdown warning remains recorded rather than represented as a clean leak audit.

The decision is to test and tune this playable combat loop before implementing the next
systems. No claim is made that the documented human playtest gate has passed.
