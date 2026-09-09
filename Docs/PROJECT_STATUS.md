# Vaultbreakers — current project status

Updated 2026-09-09. Unity **6000.4.4f1**, URP 17.4.0, Windows PC.

The playable entry scene is **Dock9_Dungeon**, a three-zone arcade combat POC in the
Shatterbelt. It replaces the rejected empty arena presentation with a dressed route,
rebuilt Blender characters, fixed-angle cameras with bounded tracking and short transitions. Minecraft
Dungeons informs presentation and pacing; the lore and combat remain Vaultbreakers.

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

Exits seal during combat. Clearing a zone opens the gate; walking through the bridge starts
the next encounter. Health and guard restore on successful transition. Death clears enemies,
projectiles and active actions, then retries the current checkpoint. The core grants a one-time
run-local score reward. Replay clears score and completion state.

Enemy health bars, committed attack tells, directional attack crescents, impact sparks,
hit flashes, short hit-stop, damage numbers and generated combat audio provide feedback.
The player has a distinct blue accent. The HUD shows vitality, guard, objectives, zone and score.
Pause settings cover shake, flashes, rumble, damage numbers, bloom, grayscale and hit-stop.
A controller disconnected while in use pauses the run and clears held gameplay input.

## Art and tooling

- Clean open-faced modular Breaker Rig, gravity hammer / plasma cutter, pulse caster,
  hard-light shield and alternate equipment; all twelve variant IDs retained.
- Original robotic Grunt, helmeted Shooter and broad ancient-metal Bruiser.
- Overgrown intake, industrial decking and black-glass vault, with conduits, crates,
  energy lamps, arches, bridges and surrounding scenery.
- Ten in-place animation clips; eighteen bones and nine unchanged sockets/anchors.
- Editable Blender sources and explicit Unity FBX exports; no Blender dependency to play.
- Scenery exports combined by material and zone; source objects remain independently editable.

Canonical art authoring: `Tools/Blender/dungeon_assets.py`. The original player entry script
now delegates to it. `generate_arena_assets.py` produces the retained test-arena/prop assets.
`export_animation_clips.py` refreshes the shared animation export; `report_source_assets.py`
checks the saved Blender files. Unity setup regenerates imports, materials, prefabs and scenes.

Run `python3 Tools/Unity/validate.py <label> setup EditMode PlayMode build` from WSL.
No Unity package was added. The original `Prototype_Arena`, showcase and isolated test bed
remain available for regression testing and equipment inspection.

## Scope and acceptance

See [DUNGEON_POC.md](DUNGEON_POC.md) for the user's clarified scope and
[POC_RESULTS.md](POC_RESULTS.md) for the actual checks, captures and measured limitations.
Gems/gauges, Relic powers, cards/scanning, pets, persistence and co-op remain the subsequent
milestones defined by the combat POC plan. They are not implied by the score/core presentation.
The user authorized scripted controller simulation and reviewer judgement while AFK.
Fresh-player discoverability, physical controller feel and subjective enjoyment remain
unmeasured follow-up checks, not blockers for this delivery.

The latest automated suite passes **175 EditMode + 64 PlayMode tests**, including moving-leg
animation beneath raised guard. Three-pass evidence is in [POLISH_PASSES.md](POLISH_PASSES.md).

The proprietary license is in `LICENSE`; third-party exceptions are in `LICENSES/`.
Source, assets and review evidence use Git/Git LFS; generated caches and raw logs remain local.
