# Vaultbreakers — current project status

Updated 2026-10-02. Unity **6000.4.4f1**, URP 17.4.0, Windows PC.

The playable entry scene is **Dock9_Dungeon**, a three-zone arcade combat POC in the
Shatterbelt. The current art pass rebuilds the route as an industrial orbital dock with richer surfaces,
layered character equipment, contact shadows, reflections and a compact instrument-style HUD.
The fixed-angle camera retains bounded tracking and short transitions. The visual target and
user override are in [ART_DIRECTION_AA.md](ART_DIRECTION_AA.md).

**Alpha 0.0.1:** the user's later request adds multi-gem enemy drops, proximity collection,
and new breakable crates/chests after a fresh baseline test. The authoritative behavior,
reference research and validation are in [ALPHA_0_0_1.md](ALPHA_0_0_1.md).

## Play

Open `Assets/Vaultbreakers/Scenes/Prototype/Dock9_Dungeon.unity` and press Play, or run
`Builds/Vaultbreakers_POC/Vaultbreakers.exe`. The standalone folder must remain intact.

| Action | Controller | Keyboard / mouse |
|---|---|---|
| Move | Left stick | WASD |
| Aim | Right stick, optional | Mouse or arrow keys |
| Melee | Hold X / West | Hold left mouse |
| Repeating fire | RT | E |
| Directional shield | LT | Right mouse |
| Dodge | A / South | Space |
| Pause / resume | Start | Escape |
| Retry checkpoint | Pause menu | R or pause menu |
| Replay after final clear | Start | R |
| Combat lab, development only | — | F1 |

Shielding prevents fire, melee suppresses guard through recovery, and dodge cancels attacks
and guard. Movement remains independent from shield direction. The arm-mounted weapon uses
cooldowns, without ammo or reload. The dungeon uses `DungeonBalance.asset`; the original
regression arena retains `PrototypeBalance.asset`.

## Route and feedback

1. Salvage Intake: three Scrap Grunts, an open and readable first fight.
2. Repo Transfer Court: five Grunts and three Repo Shooters.
3. Black-glass Vault: four Grunts, two Shooters and one Heavy Bruiser, then the vault core.

Each combat floor is 24 × 24 metres, almost three times the previous area. The fixed-angle
camera follows within the room. Enemy health is Grunt 18, Shooter 14 and Bruiser 42.

Exits seal during combat. Clearing a zone gives a three-second gem collection window,
then opens the gate; walking through the bridge starts
the next encounter. Health and guard restore on successful transition. Death clears enemies,
projectiles and active actions, then retries the current checkpoint. The core grants a one-time
run-local score reward. Replay clears score and completion state.

Grunts/Shooters/Bruisers drop 3/4/6 gems. Each room adds two breakable orange crates
and one teal chest with gold diamond markings (6/12 gems). Attack them with the hammer
or caster, then approach the pickups to attract them. Gems use five shapes/colors,
brief bursts and pickup sounds. A capped pool merges values near capacity. The HUD
counts collected gem units; gold also increases salvage score. Successful room transitions
bank gems, checkpoint retries restore the bank, and replay clears it. The final core
remains the approach-to-claim objective. Grey scenery cases remain solid environment props.
Broken caches eject six shaped lid, panel and frame pieces that shrink, fade and disappear
within one second. Pausing freezes the effect; retry restores the containers.

Enemy health bars, committed attack tells, directional attack crescents, impact sparks,
hit flashes, short hit-stop, damage numbers and generated combat audio provide feedback.
The player has a distinct blue accent. The HUD shows vitality, guard, objectives, zone and score.
Pause settings cover shake, flashes, rumble, damage numbers, bloom, grayscale and hit-stop.
A controller disconnected while in use pauses the run and clears held gameplay input.

## Art and tooling

- Clean open-faced modular Breaker Rig, gravity hammer / plasma cutter, pulse caster,
  hard-light shield and alternate equipment; all twelve variant IDs retained.
- Original robotic Grunt, helmeted Shooter and broad ancient-metal Bruiser.
- Salvage containers, recessed service trenches, pressure-vessel transfer machinery and
  black-glass vault monoliths; large quiet deck panels and warm/cool work lighting.
- Original generated surface maps, weighted normals, contact occlusion, local reflections,
  restrained bloom and ACES tone mapping. Vector action icons and compact HUD panels.
- Ten in-place animation clips; eighteen bones and nine unchanged sockets/anchors.
- Editable Blender sources and explicit Unity FBX exports; no Blender dependency to play.
- Scenery exports combined by material and zone; source objects remain independently editable.

Canonical art authoring: `Tools/Blender/dungeon_assets.py`, with `polished_dock9.py` for the
current environment and actor refinements. The original player entry script
now delegates to it. `generate_arena_assets.py` produces the retained test-arena/prop assets.
`export_animation_clips.py` refreshes the shared animation export; `report_source_assets.py`
checks the saved Blender files. Unity setup regenerates imports, materials, prefabs and scenes.

Run `python3 Tools/Unity/validate.py <label> setup EditMode PlayMode build` from WSL.
No Unity package was added. The original `Prototype_Arena`, showcase and isolated test bed
remain available for regression testing and equipment inspection.

## Scope and acceptance

See [DUNGEON_POC.md](DUNGEON_POC.md) for the user's clarified scope and
[POC_RESULTS.md](POC_RESULTS.md) for the actual checks, captures and measured limitations.
Gems and breakable caches are the explicitly authorized first extension. Gauges, Relic powers,
cards/scanning, pets, persistence and co-op remain subsequent milestones. Energy gem totals
do not grant combat bonuses in this slice.
The user authorized scripted controller simulation and reviewer judgement while AFK.
Fresh-player discoverability, physical controller feel and subjective enjoyment remain
unmeasured follow-up checks, not blockers for this delivery.

The current alpha passed **187 EditMode + 76 PlayMode tests**, including the new gem,
container, animated-muzzle and debris lifecycle checks. The pre-alpha combat baseline
passed 177 EditMode + 69 PlayMode tests before implementation. Current standalone evidence
is recorded in the alpha report.
Visual-overhaul validation and captures are in
[VALIDATION_AA_2026-10-02.md](VALIDATION_AA_2026-10-02.md); historical three-pass evidence
is retained in [POLISH_PASSES.md](POLISH_PASSES.md).

The proprietary license is in `LICENSE`; third-party exceptions are in `LICENSES/`.
Source, assets and review evidence use Git/Git LFS; generated caches and raw logs remain local.

Historical five-policy results remain in `POC_RESULTS.md`. Use the new validation report
for measurements of the current art; the old performance figures do not describe this revision.
The requested gameplay recording is generated by `python3 Tools/Unity/video.py <label>`.
It is a silent recording of rendered gameplay, and its timing is excluded from performance claims.
