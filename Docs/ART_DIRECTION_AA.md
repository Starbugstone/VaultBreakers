# Dock 9 visual overhaul — 2026-10-02

The user rejected the previous visual quality and requested a complete graphics overhaul
toward a polished AA presentation while preserving fluid arcade combat. They selected
**stylized AA: strong silhouettes, rich materials, cinematic lighting, readable combat**
and delegated the creative direction. This supersedes the earlier placeholder-art ceiling
in the combat POC plan. It authorizes presentation work, not additional gameplay systems.

## Direction

An orbital salvage dock built over a deep industrial void. Warm work lights, painted cargo
cases and articulated steel foundations lead through a pressure-vessel transfer court into
a cold black-glass vault. Large structural forms establish each room's identity; smaller
hardware belongs around the perimeter. The walkable floor remains visually quiet.

- Intake: oxidized orange cargo containers, broad muted deck panels, amber work lights.
- Transfer: blue steel decking, pressure vessels, manifolds, visible service conduits.
- Vault: dark reflective monoliths, recessed cyan cores, pale structural trim.
- Player: open-faced pilot, blue undersuit, layered armor, distinct hammer and arm caster.
- Enemies: compact orange cutter, narrow hooded gunner, broad armored vault guardian.
- HUD: compact dark instrument panels, restrained cyan rules, original vector action icons.
- Lighting: warm grazing key, cool fill, contact occlusion, restrained bloom and ACES.

## Arcade constraints

Keep the existing 24 × 24 metre rooms, collision, fixed camera orientation, movement,
attack timings, damage, tells, enemy counts and reset behavior. No animation may delay an
accepted action. No decorative prop introduces an unrepresented collision obstacle.
Screen shake, flashes, bloom, numbers, grayscale and hit-stop remain configurable.
Validate readability at 1080p and 720p, with grayscale and bloom disabled.

The melee revision retains the hammer at the user's explicit request. Replace the circular
line/disc placeholders with sword-like arcade slashes: an open tapered crescent, an ivory cutting edge,
warm trailing color and a thinner echo. Existing impact sparks remain hit-triggered.
The slash follows the committed attack direction and fades through early recovery; it
never owns damage or collision. Its reusable mesh and vertex-color shader are generated
without textures or additional packages. The player uses a separate upright facing pivot
above the imported animation rig so turning cannot overwrite the FBX axis conversion.

## Reproducibility

`Tools/Blender/dungeon_assets.py` remains the canonical entry point. It calls
`polished_dock9.py` for industrial scenery and character refinements. Source meshes stay
separate in the saved `.blend`; environment FBX meshes are combined by material and room.
Schema-1 bones, bind transforms, sockets and twelve equipment variant IDs remain unchanged.
Weighted normals preserve broad hard-surface planes. Environment UVs use a consistent
world scale. Unity setup generates original machining-grain maps and explicit URP materials.

No packages or third-party artwork are added. The rendering features use installed URP.
The pre-existing gate material is loaded without resetting its authored properties.

## Review evidence

Use actual Windows player captures for artistic review. Blender renders verify source meshes
but do not establish gameplay appearance. The requested gameplay video must show actual
rendered combat; it must not be a concept-art animation or slideshow.

`--poc-video <folder>` writes a development-only 30 fps PNG sequence. Encode with FFmpeg;
keep raw frames under ignored `Logs/`. Recording alters frame timing, so profile a separate
run without that argument. Scripted controller runs do not establish human enjoyment or
physical controller feel. Final screenshots, recording, test results and measured performance
are recorded in `Docs/VALIDATION_AA_2026-10-02.md` after execution.
