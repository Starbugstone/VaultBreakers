# Dock 9 dungeon POC — current scope, 2026-09-09

The user requested a Minecraft Dungeons-inspired arcade experience **with the lore and gameplay
in the Markdown documents**. Minecraft Dungeons informs visual readability, environmental richness
and encounter pacing; it does not replace Vaultbreakers with fantasy classes or magic mechanics.

## Source of truth

`PROJECT.md`: Shatterbelt science fiction, reckless relic raiders, modular Breaker Rigs,
salvage stations and ancient black-glass vaults. Shield to approach, fire to pressure, melee to
clear, dodge to escape. Same-arm ranged/shield exclusion; melee suppresses guard; dodge cancels
attacks/guard; no ammo; optional precision aim; short code-driven attacks.
`COMBAT_POC_PLAN.md`: combat POC phase gates, reset correctness and tests remain binding.
Gems/gauges, Relic power, scanning/cards, pets and persistence remain subsequent milestones.
The user explicitly confirmed on 2026-09-09: finish and polish this combat POC first.

## Playable redesign

The entry scene is `Dock9_Dungeon`: three connected dressed salvage/vault combat zones,
bridges, sealed exits, room checkpoints, a final recovered vault core and replay. First encounter
contains three Scrap Grunts; later encounters add Repo Shooters and a Heavy Bruiser. Run-local
salvage score provides arcade feedback, without a persistent economy or inventory.

The user subsequently requested substantially larger playing areas and lower enemy health.
Combat floors are now 24 × 24 metres (previously 14 × 14), with 33.6-metre room spacing.
The camera retains fixed isometric orientation and follows within each room, then across bridges;
close framing keeps characters readable without fitting the entire larger floor onscreen.
Grunt/Shooter/Bruiser health is now 18/14/42 (previously 30/24/75).
Hold melee to repeat at the authored cadence. Mouse-ground aiming is an additional PC fallback;
controller movement-facing and optional right-stick precision remain authoritative. A short
collision-aware attack step replaces stationary swiping. Balance is in DungeonBalance.asset;
the regression arena retains PrototypeBalance.asset.

Keyboard: WASD move, mouse or arrows aim, hold LMB melee, E fire, RMB directional guard,
Space dodge, Escape pause, R retry. Controller: left/right sticks move/aim, X melee,
RT fire, LT guard, A dodge, Start pause/replay. Y/B remain reserved for later documented systems.

## Art

Original open-faced rig pilot with clean armor, scanner, gravity hammer / plasma cutter,
left-arm pulse caster and hard-light shield; robotic Scrap Grunt, helmeted Repo Shooter,
broad ancient-metal Heavy Bruiser. Overgrown salvage foundations, conduits, sealed crates,
energy lamps, access bridges and black-glass vault storage. Warm/cool light contrast,
quiet combat floors and distinct emissive accents. No copied Minecraft assets.

`Tools/Blender/dungeon_assets.py` authors the new character/world source and FBX files.
Schema-1 bones, bind transforms, nine sockets, six module pairs and original FBX/meta identities
remain. Historical variant IDs remain stable. All gameplay remains code-driven.

## Exit checks

Compile and regression tests; dungeon traversal, death/retry, gate collision and final reward tests;
source mesh front/back review; actual Windows 1080p and 720p captures; complete scripted input run;
performance measurements with stated workload; inspect logs for runtime exceptions.
On 2026-09-09 the user explicitly authorized simulation and reviewer judgement while AFK.
For this delivery, labelled scripted controller sessions replace the five-human blocking gate.
They must report real gameplay outcomes, never invented understanding or replay preference.
Human discoverability, enjoyment and physical controller feel remain optional follow-up validation.
The user also requested at least three documented polish, sound and graphics passes; see
`POLISH_PASSES.md` for evidence as those passes complete.
