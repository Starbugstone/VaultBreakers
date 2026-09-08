# Vaultbreakers in the Shatterbelt

## Complete Unity Project Plan

**Status:** Prototype implementation baseline  
**Engine:** Unity 6, 3D URP  
**Initial platform:** Windows PC  
**Primary input:** Xbox-style controller  
**Core mantra:** **Your class is what you scanned.**

---

# 1. Purpose

This document is the implementation contract for creating **Vaultbreakers in the Shatterbelt** in Unity.

It consolidates the current design decisions for:

- arcade combat;
- controller input;
- fixed combat zones;
- gems and persistent liquid-energy gauges;
- the modular Breaker Rig;
- card-driven equipment;
- autonomous pets;
- enemies and waves;
- visual readability;
- accessibility;
- Unity architecture;
- AI-agent collaboration;
- prototype milestones;
- later QR, NFC, Rig Profile, and multiplayer systems.

It is intended for human developers and AI coding agents working through VS Code, Cursor, Claude Code, Codex, Unity MCP, Blender, or related tools.

The first goal is not to build every system. The first goal is to prove that the core combat loop is fast, readable, satisfying, and worth replaying.

---

# 2. Design compliance verification

The current design remains compliant with the project's main goals.

## 2.1 Main goals

Vaultbreakers must remain:

- arcade-first;
- controller-first;
- fast and immediately responsive;
- readable during one-to-four-player combat;
- based on short fixed or semi-fixed combat zones;
- fun without requiring physical cards;
- defined by loadouts rather than traditional classes;
- visually modular;
- easy to start but capable of deeper mastery;
- low-clutter despite enemies, pets, projectiles, gems, and effects;
- accessible without relying on color alone for critical information;
- humorous, scrappy, collectible, and toy-like.

## 2.2 Confirmed combat hook

The core interaction is coherent:

- Hold **RT** for repeated ranged fire.
- Hold **LT** to raise the shield.
- The ranged weapon and shield use the same arm.
- The player must choose between firing and shielding.
- Melee remains available, but temporarily drops the shield.
- Dodge cancels the shield.
- The right stick adds precision without becoming mandatory.

This creates the intended rhythm:

> Shield to approach.  
> Fire to pressure.  
> Melee to clear.  
> Dodge to escape.  
> Relic power to spike.  
> Scan to redefine the build.

## 2.3 Confirmed card identity

There are no fixed classes.

Every player is a **Vaultbreaker** using a modular **Breaker Rig**. Combat identity comes from the current Relic Loadout:

- Melee;
- Range;
- Armor;
- Pet;
- Skin/Rig;
- optional Hero or Rig Profile.

Tank, assassin, engineer, ranged, support, or hybrid styles emerge from cards rather than class selection.

## 2.4 Confirmed gem and gauge direction

Enemies drop controlled bursts of colored and shape-coded gems.

Gems:

- stay in the arena;
- settle quickly;
- slide or magnet slightly toward nearby players;
- do not fly to HUD slots;
- feed liquid-energy gauges;
- remain subordinate to combat information;
- are pooled and capped to prevent clutter.

Gauge fill visibly rises and sloshes.

Gauge progress **persists between successfully completed zones**. Death resets current gauges.

## 2.5 Confirmed modular character requirement

The player model must support modular visuals from the beginning:

- shared humanoid skeleton;
- right-hand melee socket;
- left-arm ranged/shield socket;
- armor regions;
- helmet and rig swaps;
- pet spawning;
- VFX anchors;
- future card-driven reconfiguration.

This must not be retrofitted after combat is complete.

## 2.6 Confirmed pets

Each player equips one autonomous pet.

Each pet has:

- simple AI;
- a preferred position relative to the player;
- a distinct movement pattern;
- a support, offensive, defensive, or utility role;
- no required extra prototype input.

Initial roles:

- gem collector;
- shooter drone;
- shield charger.

## 2.7 Arcade-fun guardrails

Hard rules:

- player input must produce immediate visible response;
- gameplay must not wait for decorative animation;
- combat animations stay short;
- camera remains stable during combat;
- standard ranged weapons use cooldowns, not ammo;
- no routine combat action requires a menu;
- VFX cannot hide danger;
- gem rewards cannot flood the screen;
- physical-card systems cannot delay combat development;
- external cards cannot be required for completion;
- Unique means special identity, not strictly greater power.

**Conclusion:** The design is ready for a Unity combat prototype.

---

# 3. High concept

**Vaultbreakers in the Shatterbelt** is a fixed-camera, isometric science-fiction arcade brawler.

Players are reckless relic raiders called Vaultbreakers. They enter ancient vaults, salvage stations, broken moons, corporate recovery sites, and alien megastructures.

Combat occurs in fixed or semi-fixed zones:

1. players enter;
2. camera locks;
3. exits seal;
4. enemy waves spawn;
5. enemies drop gems;
6. liquid rig gauges charge;
7. Relic powers activate;
8. zone clears;
9. players collect remaining gems;
10. scan points or exits become available;
11. the mission proceeds toward a boss.

Relic Cards are encoded machine-patterns. Scanning one causes the Breaker Rig to reconfigure visibly and mechanically.

The long-term game supports one-to-four-player local co-op. The initial prototype is single-player.

---

# 4. Core pillars

## 4.1 Arcade combat

The player can quickly understand and use:

- movement;
- facing;
- melee;
- repeated ranged fire;
- directional shield;
- dodge;
- gems;
- gauges;
- Relic power;
- safe-point scanning.

## 4.2 Fixed-zone stage design

The camera acts as the stage.

Rooms prioritize:

- visible boundaries;
- readable spawn points;
- short enemy waves;
- clear hazards;
- quick transitions;
- bosses and mini-bosses;
- controlled visual density.

## 4.3 Loadout-defined identity

Cards are:

- the build system;
- the class replacement;
- the visible equipment system;
- the collectible system;
- the future physical-card bridge.

## 4.4 Virtual-first design

The complete game must work through virtual cards.

Later, the same card system may accept:

- printable QR cards;
- event QR cards;
- NFC miniatures;
- Rig Profiles;
- local portable loadouts;
- Entangled Equipment.

## 4.5 Neon Rust Relicpunk

Visual language:

- salvage armor;
- patched technology;
- black-glass vaults;
- ancient metal;
- hard-light shields;
- scanner beams;
- restrained neon energy;
- chunky low-poly silhouettes;
- brief arcade effects.

---

# 5. Technology baseline

## 5.1 Unity project

Use:

- Unity 6 or later;
- 3D URP template;
- Windows as first target;
- 60 FPS target;
- Git from the beginning.

Do not hard-pin package versions in this document. Install versions officially compatible with the chosen Unity editor.

## 5.2 Unity packages

Install through **Window > Package Manager**:

Required:

- Input System;
- Universal Render Pipeline;
- Test Framework;
- TextMeshPro if absent.

Recommended:

- Cinemachine;
- ProBuilder;
- AI Assistant for Unity MCP.

Later if required:

- Addressables;
- AI Navigation;
- Localization.

## 5.3 Rendering

Use URP with:

- restrained bloom;
- limited realtime lights;
- controlled transparency;
- crisp materials;
- optional subtle rim lighting;
- no cinematic post-processing that reduces visibility.

## 5.4 Input System

Create:

`Assets/Vaultbreakers/Input/VaultbreakersInputActions.inputactions`

Action map: `Player`

| Action | Type | Binding |
|---|---|---|
| Move | Vector2 | Left stick |
| Aim | Vector2 | Right stick |
| Melee | Button | X / West |
| Ranged | Button | RT |
| Shield | Button | LT |
| Dodge | Button | A / South |
| RelicPower | Button | Y / North |
| Interact | Button | B / East |
| Locate | Button | D-pad Up, later |
| Pause | Button | Start |
| MissionInfo | Button | View/Back |

Use `PlayerInput` first. Introduce `PlayerInputManager` with local co-op.

## 5.5 Physics layers

Create:

- Player;
- Enemy;
- PlayerProjectile;
- EnemyProjectile;
- PlayerMeleeHit;
- EnemyAttack;
- Gem;
- Pet;
- Hazard;
- Environment;
- Interactable.

Rules:

- no friendly fire;
- no player-player collision later;
- players and enemies use capsule colliders;
- melee uses overlap or cast queries;
- projectiles are pooled triggers;
- pets do not obstruct players;
- gems do not block movement.

Layer indices and the collision matrix are applied by `Vaultbreakers > Setup > Configure Physics Layers` (also run by the full setup). Unity stores the matrix as an opaque bitfield in `DynamicsManager.asset`, so the intent lives in `Assets/Vaultbreakers/Editor/VaultbreakersPhysicsSetup.cs` and the indices in `Vaultbreakers.Core.GameLayers`. Layers 0–7 keep Unity's defaults. Runtime code uses `GameLayers` constants rather than name or tag lookups.

---

# 6. Repository structure

```text
Vaultbreakers/
├── Assets/
├── Packages/
├── ProjectSettings/
├── README.md
├── PROJECT.md
├── CHANGELOG.md
├── LICENSES/
└── Docs/
```

```text
Assets/
└── Vaultbreakers/
    ├── Art/
    │   ├── Characters/
    │   │   ├── Player/
    │   │   ├── Enemies/
    │   │   └── Pets/
    │   ├── Environments/
    │   ├── Materials/
    │   ├── Textures/
    │   ├── VFX/
    │   ├── UI/
    │   └── Animation/
    ├── Audio/
    ├── Data/
    │   ├── Cards/
    │   ├── Enemies/
    │   ├── Missions/
    │   ├── Zones/
    │   ├── Waves/
    │   ├── Pets/
    │   └── Balance/
    ├── Input/
    ├── Prefabs/
    │   ├── Player/
    │   ├── Enemies/
    │   ├── Pets/
    │   ├── Projectiles/
    │   ├── Gems/
    │   ├── Zones/
    │   ├── Interactables/
    │   └── UI/
    ├── Scenes/
    │   ├── Bootstrap/
    │   ├── Prototype/
    │   ├── Test/
    │   └── Missions/
    ├── Scripts/
    │   ├── Core/
    │   ├── Input/
    │   ├── Player/
    │   ├── Combat/
    │   ├── Enemies/
    │   ├── Pets/
    │   ├── Gems/
    │   ├── Gauges/
    │   ├── Cards/
    │   ├── Equipment/
    │   ├── Zones/
    │   ├── Camera/
    │   ├── UI/
    │   ├── Audio/
    │   ├── Pooling/
    │   ├── Save/
    │   └── Debug/
    ├── Settings/
    ├── Shaders/
    └── Tests/
        ├── EditMode/
        └── PlayMode/
```

Use assembly definitions once the structure stabilizes. Do not create unnecessary micro-assemblies during early prototyping.

---

# 7. Scenes

## 7.1 Bootstrap

`Scenes/Bootstrap/Bootstrap.unity`

Later responsibilities:

- persistent services;
- settings;
- audio;
- card registry;
- scene loading;
- session management.

The first prototype may open directly into the arena.

## 7.2 Prototype arena

`Scenes/Prototype/Prototype_Arena.unity`

Contains:

- rectangular arena;
- visible boundaries;
- PlayerStart;
- enemy spawn points;
- fixed isometric camera;
- ZoneController;
- WaveSpawner;
- exit;
- prototype HUD;
- debug panel;
- pools;
- later, a safe scan terminal.

## 7.3 Visual combat test room

Later create:

`Scenes/Test/VisualCombatTest.unity`

Eventually include:

- modular Vaultbreaker;
- melee weapon;
- ranged weapon;
- shield;
- one pet;
- three enemy roles;
- hazard;
- vault door;
- scan point;
- player label;
- card scan effect;
- shield break;
- representative lighting.

---

# 8. Camera

## 8.1 Prototype target

Start near:

- 45° yaw;
- 35° pitch;
- 10–12 units distance;
- fixed orientation;
- fixed zone framing.

Tune against actual player scale and arena size.

## 8.2 Camera modes

```csharp
public enum CameraMode
{
    LockedToZone,
    TransitionToZone,
    BossArena,
    VictoryFocus
}
```

Prototype requires `LockedToZone`.

## 8.3 Guardrails

The camera must not:

- rotate during combat;
- violently shake;
- hide a player;
- zoom unpredictably;
- perform cinematic cuts during combat;
- lose players outside visible bounds.

---

# 9. Modular Breaker Rig

## 9.1 Prefab hierarchy

```text
PF_Vaultbreaker
├── PlayerInput
├── PlayerMotor
├── PlayerFacing
├── PlayerCombat
├── PlayerHealth
├── ShieldController
├── DodgeController
├── GaugeController
├── PlayerLoadout
├── EquipmentVisualController
├── PlayerAnimationController
├── ModelRoot
│   ├── Armature
│   ├── BaseRigBody
│   ├── ArmorRoot
│   └── RigVisualRoot
├── Sockets
│   ├── Socket_RightHand_Melee
│   ├── Socket_LeftArm_RangedShield
│   ├── Socket_Back
│   ├── Socket_Shoulder_L
│   ├── Socket_Shoulder_R
│   ├── Socket_PetAnchor
│   └── Socket_Nameplate
├── VFXAnchors
│   ├── Anchor_MeleeTrail
│   ├── Anchor_Muzzle
│   ├── Anchor_Shield
│   ├── Anchor_ScanWeave
│   ├── Anchor_Hit
│   └── Anchor_Feet
└── Runtime
    ├── ActiveMelee
    ├── ActiveRanged
    ├── ActiveArmor
    └── ActivePetReference
```

## 9.2 Shared skeleton

Production-compatible humanoid parts must use one skeleton and bone naming convention.

Prototype:

- one rigged humanoid;
- sockets added on prefab;
- test animation retargeting;
- normalize scale and axes;
- prove socket alignment during movement and attacks.

## 9.3 Slot visual responsibilities

### Melee

- right-hand weapon;
- optional holster;
- trail;
- attack profile;
- hit VFX.

### Range

- left-arm module;
- muzzle;
- projectile;
- firing VFX;
- fire cooldown.

### Shield

- same left-arm module;
- hard-light arc;
- impact ripple;
- crack state;
- break effect.

### Armor

- torso, shoulders, arms, and legs;
- silhouette;
- shield and mobility modifiers;
- later shared-skeleton meshes.

### Pet

- independent prefab;
- autonomous AI;
- role-specific position.

### Skin/Rig

- base body;
- helmet;
- materials;
- accent geometry;
- unchanged universal sockets.

## 9.4 Avoiding combinatorial art problems

Hard rules:

- one melee attachment standard;
- one ranged/shield attachment standard;
- one shield origin;
- one shared humanoid skeleton;
- documented armor volume zones;
- skin changes do not move sockets;
- cards do not require custom versions for every combination.

## 9.5 Equipment transition

Prototype:

- instant replacement;
- short flash;
- no long input lock.

Later:

1. old part dissolves;
2. hologram appears;
3. panels assemble;
4. materials resolve;
5. slot pulses.

Timing:

- one slot: 0.4–0.6 sec;
- full Rig Profile: 0.8–1.0 sec.

---

# 10. Movement and facing

## 10.1 Movement

Starting target:

- 5 units/sec;
- movement on XZ;
- fast acceleration;
- fast deceleration;
- camera-relative input;
- no root-motion-controlled movement.

A `CharacterController` is recommended initially.

## 10.2 Facing priority

Maintain `LastCombatFacingDirection`.

Priority:

1. active right stick;
2. otherwise active movement;
3. otherwise subtle target correction when attacking;
4. otherwise preserve previous facing.

Starting dead zones:

- movement: 0.15–0.2;
- aim: 0.25.

## 10.3 Shield facing

On LT press:

- capture facing;
- lock direction;
- let movement continue independently;
- right stick rotates shield;
- release restores normal facing behaviour.

`PlayerFacing` owns direction logic only, not damage or attacks.

---

# 11. Combat

> **Numbers in this section are the design's starting points, not the live values.** Combat tuning
> now lives in `Assets/Vaultbreakers/Data/Balance/PrototypeBalance.asset`, which is authoritative for
> anything implemented. Several values have since been tuned for arcade responsiveness — melee
> cooldown, projectile speed, and the shield break lockout among them. See `COMBAT_POC_PLAN.md`
> section 4 for the current table and the reasoning, and `PROJECT_STATUS.md` for what is implemented.
> This section is kept as written because it records the intended shape of each verb, which has not
> changed.

## 11.1 Suggested state flags

```csharp
[Flags]
public enum PlayerActionState
{
    None = 0,
    Attacking = 1 << 0,
    Dodging = 1 << 1,
    Shielding = 1 << 2,
    ShieldBroken = 1 << 3,
    Stunned = 1 << 4,
    Interacting = 1 << 5,
    Dead = 1 << 6
}
```

## 11.2 Melee

Input: X.

Starting values:

- damage: 10;
- cooldown: 0.4 sec;
- range: 2.5;
- radius: 1.5;
- target correction: 15°;
- no player teleport;
- knockback away from attacker.

Use a sphere cast or overlap query with an enemy layer mask.

Later:

- three-hit combo;
- hold X for heavy;
- attack queue;
- card-defined patterns;
- hit pause;
- armor break.

## 11.3 Ranged

Input: hold RT.

Rules:

- repeated fire;
- no ammo;
- no reload;
- card defines cooldown;
- disabled while shield is active;
- current combat facing;
- pooled projectile.

Starting values:

- speed: 20;
- cooldown: 0.3 sec;
- damage: 8.

Later cards may define spread, burst, beam, ricochet, pierce, charge, or heat. Standard weapons remain cooldown-based.

## 11.4 Shield

Input: hold LT.

| Parameter | Start value |
|---|---:|
| Stability | 100 |
| Arc | 120° |
| Light drain | 10 |
| Heavy drain | 30 |
| Lowered regen | 12/sec |
| Raised idle regen | 4/sec |
| Break cooldown | 5 sec |
| Movement penalty | 15% |
| Prototype frontal block | 100% |

Block process:

1. damage reaches player;
2. shield active and available;
3. compare source direction with shield facing;
4. if within arc, block;
5. drain stability;
6. play impact;
7. break at zero.

Melee suppresses shield through attack recovery.  
Dodge cancels shield.

## 11.5 Dodge

Input: A.

| Parameter | Start value |
|---|---:|
| Distance | 3 |
| Duration | 0.2 sec |
| Cooldown | 1 sec |
| Direction | Move, otherwise last facing |
| Prototype invulnerability | Dash duration |

Prevent passing through walls.

---

# 12. Health, death, and retry

On death:

- restart current wave;
- keep previously cleared waves cleared;
- respawn at zone checkpoint;
- restore health;
- restore shield;
- keep equipped cards;
- reset gauges;
- end active temporary powers;
- reset active-wave enemies.

Between successful zones:

- shield fully restores;
- health restoration is configurable;
- gauges persist.

Start with full health restoration while tuning combat, then test partial restoration later.

---

# 13. Enemies

## 13.1 Base prefab

```text
PF_EnemyBase
├── EnemyBrain
├── EnemyMotor
├── EnemyCombat
├── Health
├── Hurtbox
├── GemDropper
├── Animator
├── AudioSource
├── ModelRoot
└── VFXAnchors
```

## 13.2 Scrap Grunt

Purpose:

- basic melee test;
- movement and spacing;
- damage feedback.

Starting values:

- health: 30;
- speed: 3;
- attack: 5;
- visible melee windup;
- one or two melee/gold-biased gems.

## 13.3 Repo Shooter

Purpose:

- teaches shield approach;
- creates ranged pressure.

Behaviour:

- maintain range;
- visible firing tell;
- clear projectile;
- retreat when too close;
- ranged/gold drops.

## 13.4 Heavy Bruiser

Purpose:

- tests dodge;
- heavy shield drain;
- long telegraph.

Behaviour:

- slow;
- wide windup;
- heavy hit;
- shield/melee/gold drops.

## 13.5 Prototype AI limits

Start with:

- direct steering;
- simple separation;
- obstacle-light arena;
- no behaviour-tree framework;
- NavMesh only once room geometry requires it.

---

# 14. Zones and waves

## 14.1 Definitions

```csharp
[CreateAssetMenu(menuName = "Vaultbreakers/Zones/Zone Definition")]
public sealed class ZoneDefinition : ScriptableObject
{
    public string zoneId;
    public WaveDefinition[] waves;
    public bool hasSafeScanPoint;
    public bool restoreShieldOnClear = true;
    [Range(0f, 1f)] public float healthRestoreFraction = 1f;
}
```

```csharp
[CreateAssetMenu(menuName = "Vaultbreakers/Zones/Wave Definition")]
public sealed class WaveDefinition : ScriptableObject
{
    public EnemySpawnEntry[] spawns;
    public float startDelay;
    public float nextWaveDelay;
}
```

## 14.2 Runtime flow

```text
Enter zone
→ lock camera and exits
→ start wave
→ spawn enemies
→ defeat enemies
→ next wave or clear
→ 3–5 sec gem collection
→ optional scan point
→ open exit
→ next zone
```

## 14.3 First wave

Three Scrap Grunts.

Acceptance:

- cleared in under 30 seconds;
- controls understandable quickly;
- camera never loses player.

---

# 15. Gems

## 15.1 Types

| Type | Color | Shape | Feeds |
|---|---|---|---|
| Melee | Orange/red | Sharp shard | Melee |
| Ranged | Blue/cyan | Prism/hex | Ranged |
| Shield | White/teal | Flat plate | Shield |
| Relic | Purple/violet | Round core | Relic |
| Gold | Yellow/gold | Nugget | Score |

No embedded icon is required on the tiny world object.

## 15.2 Pickup flow

1. request pooled gem;
2. small burst;
3. settle;
4. 0.2 sec delay;
5. find nearby player;
6. slide toward player;
7. collect at radius;
8. update gauge;
9. play brief sound;
10. return to pool.

Starting values:

| Parameter | Value |
|---|---:|
| Lifetime | 6–8 sec |
| Pickup radius | 1.0 |
| Magnet radius | 1.75–2.0 |
| Magnet delay | 0.2 sec |
| Max active | ~35 |

## 15.3 Drop policy

- melee grunt: 1–2, melee/gold biased;
- ranged enemy: 1–2, ranged/gold biased;
- heavy: 2–4, shield/melee/gold;
- elite: 4–6 mixed;
- boss: controlled mixed burst during safe time.

## 15.4 Clutter prevention

Near cap:

- merge same-type value;
- upgrade an existing gem;
- skip low-value spawn;
- reduce particles.

Gems must never obscure danger.

---

# 16. Liquid-energy gauges

## 16.1 Gauges

- Melee;
- Ranged;
- Shield;
- Relic;
- Gold score.

Prototype maximum: 100.  
Matching gem: +10.

## 16.2 Persistence

Successful zone transition:

- retain values;
- retain readiness;
- retain equipped cards.

Death:

- reset values;
- end active power;
- retain equipment.

## 16.3 Prototype effects

### Melee full

- arm next melee;
- next successful melee deals +100%;
- consume gauge.

### Ranged full

- arm next volley;
- configured shots are empowered;
- consume gauge.

### Shield full

First test:

- restore 50 stability when below maximum;
- consume gauge.

Alternative later:

- empower next block.

### Relic full

Y activates:

- +50% damage;
- -50% incoming damage;
- 10 seconds;
- consume full gauge.

These are temporary test values.

## 16.4 HUD animation

On pickup:

- fill rises;
- liquid surface sloshes briefly;
- one flash;
- short sound;
- optional subtle rumble.

At full:

- gentle ready pulse;
- one sound;
- clear Y prompt for Relic;
- no repeated aggressive flashing.

Start with functional bars. Add the liquid shader only after gauge logic works.

---

# 17. Card architecture

## 17.1 Prototype scope

Only after combat, gems, and gauges work:

- three to five virtual prototype cards;
- no ownership;
- no save;
- no QR;
- no NFC;
- no rarity drops.

## 17.2 Enums

```csharp
public enum CardSlot
{
    Melee,
    Range,
    Armor,
    Pet,
    Skin,
    HeroProfile
}
```

```csharp
public enum CardRarity
{
    Common,
    Rare,
    Epic,
    Legendary,
    Mythic,
    Unique,
    Custom
}
```

```csharp
public enum CardSource
{
    Virtual,
    PrintedQr,
    EventQr,
    NfcMiniature,
    Debug
}
```

## 17.3 Definition

```csharp
[CreateAssetMenu(menuName = "Vaultbreakers/Cards/Card Definition")]
public sealed class CardDefinition : ScriptableObject
{
    public string cardId;
    public string displayName;
    public CardSlot slot;
    public CardRarity rarity;

    public GameObject equipmentPrefab;
    public Sprite cardArt;

    public float meleeDamageMultiplier = 1f;
    public float meleeCooldownMultiplier = 1f;
    public float rangedDamageMultiplier = 1f;
    public float rangedCooldownMultiplier = 1f;
    public float shieldStabilityBonus;
    public float shieldArcBonus;
    public float moveSpeedMultiplier = 1f;
}
```

Avoid premature inheritance trees.

## 17.4 First cards

### Gravity Hammer

- +50% melee damage;
- slower;
- wider;
- stronger knockback;
- right-hand visual.

### Pulse Pistol

- RT repeated fire;
- medium cooldown;
- clean projectile;
- left-arm visual.

### Bulwark Plate

- +50 stability;
- heavier shield identity;
- visible torso/shoulder plates;
- small movement or regeneration tradeoff.

Then:

- Scrap Katana;
- Scatter Blaster;
- Blink Rig.

## 17.5 Equip flow

1. validate slot;
2. remove previous modifiers;
3. update loadout;
4. apply modifiers;
5. replace visible module;
6. refresh combat profile;
7. play short scan effect.

## 17.6 Future unified token flow

```csharp
public readonly struct CardToken
{
    public readonly string CardId;
    public readonly string Serial;
    public readonly string Signature;
    public readonly CardSource Source;
}
```

```text
Virtual / QR / NFC / Debug
→ CardToken
→ CardInputService
→ CardRegistry
→ CardDefinition
→ PlayerLoadout
```

Tokens never provide trusted stats.

## 17.7 Scan timing

Base mode:

- before mission;
- safe room;
- reward room;
- before boss;
- pause binder in casual/home mode.

No normal mid-combat swapping.  
Future Chaos Mode may allow it.

---

# 18. Pets

## 18.1 Rules

- one pet per player;
- autonomous;
- near owner;
- no required input;
- invincible in prototype;
- no player collision;
- no blocking attacks;
- return or teleport after leash violation.

## 18.2 Architecture

```text
PetController
├── PetBrain
├── PetMovementPattern
├── PetTargeting
├── PetAction
└── PetVisual
```

Priority:

1. return if too far;
2. perform high-priority task;
3. act on valid target;
4. maintain preferred position;
5. idle.

Distances:

- preferred: 1.5–3.5;
- max leash: 6–8.

## 18.3 Gem collector

Possible identity: Scrap Ferret or Magnet Slime.

Behaviour:

- orbit or wander;
- find gems;
- prioritize expiring gems;
- dart to collect;
- credit owner;
- return.

Movement:

- playful orbit;
- quick dart;
- zig-zag or bounce.

## 18.4 Shooter drone

Possible identity: Pulse Drone.

Behaviour:

- shoulder position;
- find nearby enemy;
- maintain range;
- weak cooldown fire;
- never replace player damage.

Movement:

- shoulder hover;
- small strafing arcs;
- recoil motion.

## 18.5 Shield charger

Possible identity: Aegis Mite.

Behaviour:

- stay shield-side;
- monitor stability;
- approach below threshold;
- pulse recharge;
- return.

Start:

- every 8 seconds restore 15 stability.

Movement:

- tight shield-side orbit;
- brief dock.

## 18.6 Visual priority

Pets remain below hazards, enemies, projectiles, and gauge readiness.

No permanent bright tether.

---

# 19. Animation

## 19.1 Player list

- idle;
- locomotion;
- melee;
- ranged recoil;
- shield hold;
- shield hit;
- shield break;
- dodge;
- hit;
- death.

## 19.2 Enemy list

- idle;
- move;
- attack;
- hit;
- death.

## 19.3 Principles

- gameplay movement authoritative;
- no root-motion dependency for basic movement;
- immediate response;
- startup, active, recovery;
- controlled cancellation;
- no long cinematic attacks;
- animation does not secretly delay hit logic.

## 19.4 Imported assets

- use legitimate licensed assets;
- configure Humanoid rig where applicable;
- normalize scale;
- check forward axis;
- disable root motion unless deliberately required;
- preserve sockets;
- record license in `LICENSES/THIRD_PARTY_ASSETS.md`.

---

# 20. UI and accessibility

## 20.1 Prototype HUD

- health;
- shield stability;
- four gauges;
- gold;
- objective;
- wave clear.

Later:

- loadout slots;
- player number/name;
- pet;
- scan prompt;
- rarity;
- Entangled marker.

## 20.2 Rules

- minimal;
- large enough to read;
- high contrast;
- scalable;
- no long combat text;
- no essential critical information by color alone;
- optional damage numbers.

## 20.3 Planned settings

- UI scale;
- text size;
- player labels;
- outlines;
- screen shake;
- flash;
- bloom;
- rumble;
- damage numbers;
- hazard patterns;
- enemy role icons.

---

# 21. Visual readability

Priority:

1. player;
2. lethal enemy attacks;
3. shield direction and state;
4. enemies;
5. player attacks and projectiles;
6. hazards;
7. gauge readiness;
8. pets;
9. gems;
10. decorative VFX;
11. background.

Use:

- short impacts;
- brief trails;
- readable projectiles;
- shield ripple;
- shield cracks;
- controlled gem bursts.

Avoid:

- long smoke;
- giant bloom;
- persistent corpses;
- damage clouds;
- constant shake;
- effects that obscure gameplay.

Most routine VFX should last under roughly 0.5 sec.

---

# 22. Audio

Prototype events:

- melee swing/hit;
- ranged fire/impact;
- shield raise/hit/break;
- dodge;
- gem pickup;
- gauge full;
- Relic ready/activate;
- enemy telegraph;
- enemy death;
- wave clear;
- scan/equip.

AudioMixer groups:

- Master;
- Music;
- SFX;
- UI;
- Voice;
- Ambience.

Limit pickup sound concurrency.

---

# 23. Pooling and performance

Pool:

- projectiles;
- gems;
- common VFX;
- damage numbers;
- common enemies where useful.

Avoid high-frequency `Instantiate` and `Destroy` during combat.

Targets:

- stable 60 FPS;
- low input latency;
- no recurring hot-loop allocations;
- no per-frame scene searches;
- controlled physics queries;
- limited materials and realtime lights.

Profile after every major layer.

---

# 24. AI-agent workflow

## 24.1 Unity MCP

Unity MCP can allow an approved AI client to:

- inspect scenes;
- create objects;
- inspect assets;
- edit scripts;
- read console output;
- automate editor work.

Requirements:

- Unity 6+;
- `com.unity.ai.assistant`;
- MCP bridge running;
- approved client.

Check:

`Edit > Project Settings > AI > Unity MCP Server`

The exact label can vary by Assistant package version.

## 24.2 Agent rules

Agents must:

- inspect before editing;
- work in small tasks;
- compile frequently;
- read console output;
- avoid new packages without approval;
- preserve structure;
- report failures;
- avoid future-scope systems;
- create tests for deterministic logic;
- never rewrite working architecture casually.

## 24.3 Good request

```text
Goal:
Implement movement and facing.

Scope:
- Use existing Input Actions.
- Camera-relative XZ movement.
- Left stick moves and faces.
- Right stick overrides above dead zone.
- Preserve last facing while neutral.

Constraints:
- No combat.
- No packages.
- Use Assets/Vaultbreakers/Scripts/Player.
- Compile and inspect console.

Acceptance:
- No errors.
- Configurable speed.
- Correct facing priority.
```

## 24.4 Git safety

- commit before agent work;
- task branch;
- inspect diff;
- test;
- keep `.meta` files;
- do not commit Library/Temp/Logs;
- commit only working states.

---

# 25. Milestones

# Milestone 0 — Foundation

Tasks:

- Unity 6 URP project;
- Git;
- folder structure;
- Input System;
- AI Assistant and MCP;
- ProBuilder/Cinemachine if useful;
- Input Actions;
- Prototype_Arena;
- licensed player/enemy model;
- physics layers.

Exit:

- no errors;
- controller detected;
- MCP reads console;
- scene loads;
- clean repository.

# Milestone 1 — Naked combat

Build:

- movement;
- hybrid facing;
- melee;
- RT fire;
- LT shield;
- dodge;
- one grunt;
- health/death;
- one wave;
- arena lock.

Exit:

- three grunts under 30 sec;
- shield blocks firing;
- movement independent from shield facing;
- melee drops shield;
- dodge cancels shield;
- camera retains player;
- stable performance.

# Milestone 2 — Combat feel

Build:

- stability;
- shield break;
- telegraphs;
- shooter;
- bruiser;
- soft aim;
- feedback;
- wave retry.

Exit:

- four combat tools distinct;
- enemy roles teach counters;
- no hidden animation delay;
- no mandatory twin-stick play.

# Milestone 3 — Gems and gauges

Build:

- pools;
- drops;
- magnet;
- cap;
- HUD;
- persistent values;
- collection window;
- empowered attacks;
- Y Relic.

Exit:

- satisfying, not cluttered;
- drop bias works;
- gems slide to player;
- gauges persist;
- death resets;
- full state readable.

# Milestone 4 — Modular rig and cards

Build:

- socket hierarchy;
- card data;
- loadout;
- effect applier;
- visual controller;
- Hammer, Pistol, Plate;
- safe scan point;
- short transition.

Exit:

- stats and visuals change;
- slots coexist;
- sockets stay aligned;
- loadout changes feel meaningful;
- no full binder/QR/NFC/save.

# Milestone 5 — First pet

Build:

- owner;
- leash;
- pattern;
- gem targeting;
- collector pet;
- replacement.

Exit:

- remains near player;
- collects gems;
- no obstruction;
- no clutter;
- uses normal gauge path.

Then add shooter and shield charger.

# Milestone 6 — Vertical slice

Mission: **Dock 9 Disaster**

Zones:

1. basic combat;
2. ranged pressure;
3. bruiser/hazard;
4. safe scan;
5. mini-boss;
6. reward.

Include:

- environment kit;
- three enemies;
- three-to-six cards;
- one pet;
- scanning;
- score;
- completion.

Exit:

- tester learns quickly;
- understands fire/shield choice;
- enjoys gems;
- notices persistent gauges;
- sees equipment change;
- wants another run.

# Milestone 7 — Two-player co-op

Only after single-player stability.

Build:

- PlayerInputManager;
- two controllers;
- separate HUD;
- group camera;
- labels;
- no player collision;
- pet ownership;
- scaling;
- revive/retry.

Exit:

- readable;
- stable camera;
- manageable clutter;
- correct input assignment.

---

# 26. Testing

## 26.1 Automated

EditMode:

- shield-angle calculation;
- gauge clamp;
- card modifiers;
- gem merge;
- zone progression.

PlayMode:

- no firing while shielding;
- melee suppresses shield;
- dodge cancels shield;
- gauge persists after zone;
- gauge resets on death;
- active wave restarts;
- pet returns after leash break.

## 26.2 Manual metrics

Record:

- control-understanding time;
- wave clear time;
- melee misses;
- shield use;
- accidental input conflicts;
- dodge use;
- camera loss;
- gem clutter complaints;
- gauge persistence awareness;
- card impact;
- pet usefulness.

## 26.3 Arcade-fun checklist

- interesting action within five seconds?
- immediate button response?
- satisfying hold-to-fire?
- meaningful shield choice?
- melee remains central?
- dodge solves different threats?
- readable enemies?
- gems exciting, not chores?
- gauges satisfying?
- persistence creates anticipation?
- cards visibly change play?
- screen readable during chaos?
- tester wants to retry?

If several answers are no, tune before adding features.

---

# 27. Balance asset

Create:

`Data/Balance/PrototypeBalance.asset`

Fields:

- movement speed;
- dodge values;
- melee values;
- ranged values;
- shield values;
- gem values;
- gauge maximums;
- collection window;
- enemy modifiers;
- hit-stop;
- shake.

Avoid scattered magic numbers.

---

# 28. Debug tools

Development panel:

- invulnerability;
- refill shield;
- fill gauges;
- spawn enemy;
- spawn gems;
- equip card;
- spawn pet;
- facing vector;
- shield arc;
- melee query;
- magnet radius;
- pet desired position;
- restart wave;
- slow motion.

Gizmos:

- facing;
- melee;
- shield;
- detection;
- leash;
- zone;
- spawn points.

Disable for release.

---

# 29. Asset pipeline

## 29.1 Prototype priorities

- readable humanoid player;
- melee robot;
- ranged soldier/drone;
- heavy enemy;
- simple pet;
- sci-fi arena;
- compatible animations.

## 29.2 Import standards

Characters:

- consistent scale;
- correct forward axis;
- Humanoid where applicable;
- organized materials;
- compressed animations;
- root motion disabled by default.

Textures:

- reasonable resolution;
- correct import types;
- restrained emissive;
- low noise.

Materials:

- shared faction kits;
- few slots;
- controlled palette;
- grayscale checks.

## 29.3 AI-generated art

Review every generated asset for:

- usage terms;
- geometry;
- UVs;
- topology;
- rig compatibility;
- consistency;
- readability;
- accidental copyrighted imagery.

Do not let AI invent unrelated palettes per asset.

---

# 30. Deferred long-term systems

Do not implement before the vertical slice proves fun.

## Virtual binder

- ownership;
- filtering;
- details;
- scanning;
- presets;
- duplicates;
- rewards;
- print eligibility.

## Save data

Later:

- version;
- cards;
- profiles;
- scores;
- progress;
- settings.

## QR

Contains:

- game ID;
- card/profile ID;
- version;
- source/serial;
- signature/checksum.

Never trusted stats.

## NFC

Optional Unique/event layer.

## Rig Profiles

Player-created Custom full-loadout presets.

## Entangled Equipment

Valid portable guest equipment becomes session-only:

- shared locally;
- never permanently unlocked;
- not exportable;
- no reward-table influence;
- disabled in competitive validation;
- cleared at session end.

## Unique cards

Special origin, visuals, memories, and sidegrades.

Never required and not automatically strongest.

---

# 31. Prototype non-goals

Do not build:

- online multiplayer;
- accounts;
- cloud sync;
- QR;
- NFC;
- full save/load;
- procedural generation;
- crafting;
- upgrades;
- trading;
- complex economy;
- four-player balance;
- production shaders;
- cinematics;
- pet commands;
- ranked play;
- leaderboards.

---

# 32. Definition of prototype success

A tester can:

1. move naturally;
2. face without fighting controls;
3. melee immediately;
4. hold RT for repeated fire;
5. hold LT and understand firing stops;
6. move independently from shield direction;
7. dodge appropriate attacks;
8. clear a readable wave;
9. collect sliding gems;
10. see liquid gauges fill;
11. carry gauges between zones;
12. use Y Relic power;
13. see card equipment change;
14. later use an autonomous pet;
15. want another run.

Feature count does not compensate for sluggish, confusing, or cluttered combat.

---

# 33. First Unity-agent tasks

## Task 1 — Inspect

```text
Inspect and report:
- Unity version
- render pipeline
- packages
- scenes
- compile errors
- input setup
- folder structure

Do not modify anything.
```

## Task 2 — Foundation

```text
Create the PROJECT.md folder structure.
Create Prototype_Arena with:
- 20x20 floor
- four walls
- PlayerStart
- SpawnPoint_A/B/C
- ZoneRoot
- fixed isometric camera near 35° pitch and 45° yaw

No gameplay scripts.
Compile and report errors.
```

## Task 3 — Input

```text
Create VaultbreakersInputActions with the Player actions in PROJECT.md.
Add PlayerInput to PF_Vaultbreaker.
Do not implement combat.
Verify an Xbox-style controller.
```

## Task 4 — Movement

```text
Implement PlayerMotor and PlayerFacing.
Camera-relative XZ movement.
Left stick moves/faces.
Right stick overrides.
Preserve last facing.
Add facing gizmo.
Compile and test.
```

## Task 5 — Combat skeleton

```text
Add:
- one melee attack
- hold-RT fire
- LT shield
- A dodge

Shield blocks firing.
Melee suppresses shield.
Dodge cancels shield.
Use configurable values.
No cards, gems, gauges, or pets yet.
```

---

# 34. Decision log

Locked:

- Unity 6 3D URP;
- controller-first;
- fixed isometric zones;
- no classes;
- cards define build;
- left-stick default facing;
- right-stick precision;
- RT repeated fire;
- no standard ammo;
- shield/fire mutual exclusion;
- melee drops shield;
- dodge cancels shield;
- persistent gauges;
- gems magnet to player;
- no gem-to-HUD flight;
- liquid gauge feedback;
- modular rig from day one;
- one autonomous pet;
- pet-specific positioning;
- safe-zone scans;
- virtual-first cards;
- Unique is special, not stronger.

Tuning variables:

- damage;
- cooldowns;
- health restoration;
- shield regeneration;
- gem count;
- gauge bonuses;
- camera distance;
- aim correction;
- dodge invulnerability;
- enemy stats;
- Relic values.

Deferred:

- QR;
- NFC;
- Rig Profiles;
- Entangled Equipment;
- multiplayer until single-player works;
- online systems;
- procedural generation.

---

# 35. Official references

Use official Unity documentation for current setup:

- Unity AI Assistant and MCP:  
  https://docs.unity3d.com/Packages/com.unity.ai.assistant@latest/
- Input System:  
  https://docs.unity3d.com/Packages/com.unity.inputsystem@latest/
- Cinemachine:  
  https://docs.unity3d.com/Packages/com.unity.cinemachine@latest/
- ProBuilder:  
  https://docs.unity3d.com/Packages/com.unity.probuilder@latest/
- URP:  
  https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/
- Test Framework:  
  https://docs.unity3d.com/Packages/com.unity.test-framework@latest/

---

# 36. Final implementation statement

Build the smallest real version of Vaultbreakers that proves this loop:

> Enter a fixed combat room.  
> Move and face naturally.  
> Melee quickly.  
> Hold RT to fire.  
> Hold LT to exchange firepower for defense.  
> Dodge danger.  
> Defeat readable enemies.  
> Collect controlled gem chaos.  
> Fill persistent liquid-energy gauges.  
> Trigger a Relic power.  
> Scan at a safe point.  
> Watch the Breaker Rig become a different build.  
> Bring an autonomous pet that changes how the room is played.

Everything else comes after this is fun.
