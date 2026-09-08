# Modular Vaultbreaker Avatar Pipeline

**Canonical source:** `ArtSource/Blender/Characters/Player/Vaultbreaker_Modular.blend`  
**Generator:** `Tools/Blender/generate_modular_vaultbreaker.py`  
**Unity export:** `Assets/Vaultbreakers/Art/Characters/Player/Vaultbreaker_Modular.fbx`  
**Unity prefab:** `Assets/Vaultbreakers/Prefabs/Player/PF_Vaultbreaker_POC.prefab`  
**Schema version:** 1

## Purpose

This pipeline keeps the player avatar visually modular without multiplying skeletons, breaking animation compatibility, or allowing equipment to move gameplay anchors.

The current avatar is a detailed validation asset for the POC. Its chunky rigid construction is intentional: armor and machinery follow bones without fragile deformation weights while proportions and combat animation timing are still changing. A later deforming organic undersuit can be added to the same skeleton without changing the equipment contract.

## Non-negotiable rules

1. There is one canonical skeleton for every player body, skin, armor set, and equipment combination.
2. Bone names, bone parent relationships, scale, bind pose, and socket names are versioned API. Do not rename or move them casually.
3. One Blender unit equals one meter. Apply mesh transforms before export.
4. Model front is Blender `-Y`; the FBX preset converts this to Unity `+Z`.
5. Gameplay movement is code-driven. Player meshes and animations do not use root motion.
6. Equipment never adds a second armature to the player prefab.
7. Equipment meshes may change silhouette but may not move a universal socket.
8. The range and shield visuals both use the left-arm standard and must be tested together.
9. Runtime code uses serialized module and socket references. It does not search by name every frame.
10. The Blender file is the source. Do not make irreplaceable mesh edits inside the imported FBX or generated Unity prefab.

## Canonical skeleton

```text
Root
└── Hips
    ├── Spine
    │   └── Chest
    │       ├── Neck
    │       │   └── Head
    │       ├── UpperArm_L
    │       │   └── LowerArm_L
    │       │       └── Hand_L
    │       └── UpperArm_R
    │           └── LowerArm_R
    │               └── Hand_R
    ├── UpperLeg_L
    │   └── LowerLeg_L
    │       └── Foot_L
    └── UpperLeg_R
        └── LowerLeg_R
            └── Foot_R
```

New twist/helper bones may be appended after an animation migration test. Existing bones may not be reordered, renamed, reparented, or have their bind transforms changed without incrementing the schema and rebuilding every equipment asset.

## Stable sockets and anchors

| Name | Owner | Purpose |
|---|---|---|
| `SOCKET_RightHand_Melee` | `Hand_R` | Universal melee attachment |
| `SOCKET_LeftArm_RangedShield` | `LowerArm_L` | Universal ranged/shield module attachment |
| `SOCKET_Back` | `Chest` | Back rig and later armor accessories |
| `SOCKET_PetAnchor` | `Chest` | Pet reference/orbit origin |
| `ANCHOR_Muzzle` | `LowerArm_L` | Projectile and muzzle VFX origin |
| `ANCHOR_Shield` | `LowerArm_L` | Hard-light shield origin |
| `ANCHOR_MeleeTrail` | `Hand_R` | Melee trail reference |
| `ANCHOR_Hit` | `Chest` | General player hit VFX |
| `ANCHOR_Feet` | `Root` | Ground, dust, selection ring, and shadow reference |

`AvatarSocketRegistry` serializes these references on the generated prefab. Gameplay and VFX code should request a socket by `AvatarSocketId` and handle a missing optional socket explicitly.

Sockets are authored so that Unity receives an **identity rotation**: local `+Z` is the gameplay facing direction and local `+Y` is up. Because Blender `-Y` maps to Unity `+Z`, the generator pre-rotates each socket a quarter turn around X. An empty left at Blender identity arrives in Unity pointing at the sky.

Socket empties are created with `bpy.data.objects.new()`, whose `matrix_world` stays at identity until the dependency graph is evaluated. The generator therefore builds each socket's world matrix explicitly and calls `view_layer.update()` on both sides of the bone-parenting step. Skipping this collapses every socket onto the model origin — the FBX still imports, modules still swap, and the avatar still renders correctly, so the failure is invisible without an explicit placement check. `Build 3D Foundation and Modular Avatar` and the EditMode tests both assert socket placement and orientation for this reason.

## Module naming contract

Exported equipment geometry uses:

```text
VAR_<Slot>_<VariantId>_<PartName>
```

Examples:

```text
VAR_Armor_Scrapper_ChestPlate
VAR_Armor_Scrapper_Shoulder_L
VAR_Melee_ScrapHammer_Head
VAR_Ranged_PulseCaster_Barrel
```

The Unity setup tool groups every object with the same slot and variant ID into one `EquipmentModule`. The variant ID cannot contain underscores in schema version 1. Part names may contain underscores.

Permanent body geometry begins with `BASE_` and is never toggled. Sockets use `SOCKET_`; VFX origins use `ANCHOR_`.

## Current equipment matrix

| Slot | Default | Alternate |
|---|---|---|
| Helmet | Scrapper | Sentinel |
| Armor | Scrapper | Bulwark |
| Melee | ScrapHammer | PlasmaCutter |
| Ranged | PulseCaster | ArcBlaster |
| Shield | AegisEmitter | PrismEmitter |
| Rig | Reclaimer | Capacitor |

The showcase scene rotates the avatar. Press `1` for the default Scrapper set and `2` for the alternate Bulwark/Sentinel set.

The showcase instance is the same prefab with its gameplay half removed by the setup tool: no
`PlayerInput`, `PlayerInputReader`, `CharacterController`, `PlayerMotor`, `PlayerFacing`, `Health`,
`PlayerActionCoordinator`, `MeleeController`, `MeleePresentation`, `HitStop`, `ProjectilePool`,
`RangedController`, `RangedPresentation`, `ShieldController`, `ShieldPresentation`, `DodgeController`,
or `DodgePresentation`. Left in place, movement input would drive the avatar off the turntable, facing
would fight the turntable rotation, the swing and recoil poses would fight the weapon's rest pose, a
hit would freeze the global timescale, a projectile pool would appear in a scene that exists only to
be looked at, a shield band would sit in front of the model on show, and a dodge would squash the
model and leave a streak across the bench.

The dodge presentation is the one piece of feedback that poses the model root rather than a socket: it
squashes `ModelRoot` for the length of the burst and restores the scale it found. Anything else that
wants to scale the model has to coordinate with it.

Three sockets are consumed at runtime by gameplay, which is why their authored transforms are
versioned API rather than a convenience:

| Socket | Consumer | Use |
|---|---|---|
| `SOCKET_RightHand_Melee` | `MeleePresentation` | Rest rotation cached on `Awake`, then rotated about its local up axis through the swing. |
| `SOCKET_LeftArm_RangedShield` | `RangedPresentation` | Rest rotation cached on `Awake`, then kicked back on each shot. |
| `ANCHOR_Muzzle` | `RangedController` | Every shot's spawn position. Its *direction* is deliberately ignored: shots travel along gameplay facing, so the smoothed visual turn can never bend one. |

Moving any of these changes how combat reads or where shots come from, even though nothing about the
mechanics changes.

`ANCHOR_Shield` is deliberately **not** used by `ShieldPresentation`. The shield arc is centred on the
body it protects, not on the arm that projects it: an arc drawn from the left forearm would sit off
the axis the block is actually tested against, and would misrepresent coverage every time the player
turned. The anchor stays in the contract for hard-light emitter VFX, which do belong on the arm.

## Adding an equipment variant

1. Open the canonical `.blend` or extend the generator.
2. Model at final player scale while the armature remains unchanged.
3. Split pieces by the bone they must follow.
4. Parent each rigid piece to the correct canonical bone. Deforming pieces must use an Armature modifier and only canonical bones.
5. Name every part with the module naming contract.
6. Check all nearby combinations, especially helmet/collar, shoulder/weapon, left ranged/shield, chest/rig, and thigh/dodge silhouettes.
7. Export using the generator's FBX settings.
8. In Unity run `Vaultbreakers > Setup > Build 3D Foundation and Modular Avatar`.
9. Open `Avatar_Showcase.unity` and switch every variant.
10. Check the avatar from the fixed gameplay camera, not only close-up.
11. Run attachment, material, animation, shadow, and bounds validation before committing.

## Blender-to-Unity rebuild

From the repository root:

```powershell
& 'D:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python 'Tools\Blender\generate_modular_vaultbreaker.py'
```

Then let Unity import and run:

```text
Vaultbreakers > Setup > Build 3D Foundation and Modular Avatar
```

The Unity setup is idempotent. It rebuilds materials, import settings, prefab module mappings, scenes, and validation without requiring manual FBX edits.

## Import contract

- Rig type: Generic for the POC canonical mechanical skeleton.
- Global scale: 1.
- File scale: enabled.
- Cameras/lights: not imported.
- Animations: not imported from the modular geometry FBX. Animation clips will use a dedicated export later.
- Blend shapes: disabled until a real use case exists.
- Read/write mesh data: disabled.
- Mesh compression: off during POC silhouette validation.
- Materials: remapped by Blender material name to explicit URP Lit `.mat` assets.
- Colliders: not generated from visual geometry.

Separating animation clips from the modular geometry export prevents an equipment re-export from silently replacing clip timing or avatar state-machine references.

## Required graphics validation

For every body or equipment change:

- no pink/missing material in URP;
- all materials use `Universal Render Pipeline/Lit` unless deliberately documented;
- normals and smoothing look correct under a rotating key light;
- no negative scale in exported model hierarchy;
- no duplicate armature;
- no missing or renamed required socket;
- no visible equipment intersection in idle, locomotion, melee, ranged, shield, dodge, hit, and death poses;
- ranged and shield variants coexist without z-fighting;
- weapon and muzzle direction agree with Unity `+Z` gameplay facing;
- renderer bounds contain each module in its widest pose;
- shadows do not detach or vanish at the fixed camera distance;
- inactive variants do not render, cast shadows, or contribute VFX;
- equipping an invalid ID fails safely and leaves the current module unchanged;
- replacing visuals does not change player collider, hitboxes, damage timing, or movement.

## Future production migration

The current rigid modular avatar is not a dead end. Production art should migrate in this order:

1. Lock gameplay proportions and socket locations after the naked-combat POC.
2. Add dedicated in-place animation exports on the existing skeleton.
3. Replace the rigid undersuit with a clean deforming mesh if required.
4. Author armor pieces against the same bind pose and documented body-volume zones.
5. Add LODs per module only after the fixed-camera size and target hardware are known.
6. Add texture atlases/material families after the visual palette is locked.
7. Introduce an equipment compatibility test scene containing every extreme silhouette.

The production migration must preserve variant IDs used by card data. A visual revision may replace the objects inside a variant, but save/load and loadout systems should continue referring to stable data IDs rather than prefab names.
