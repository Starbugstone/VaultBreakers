using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Vaultbreakers.Core;
using Vaultbreakers.Combat;
using Vaultbreakers.Equipment;
using Vaultbreakers.Input;
using Vaultbreakers.Player;

namespace Vaultbreakers.Tests.PlayMode
{
    /// <summary>
    /// Runtime checks against the generated scenes. These prove the foundation actually plays,
    /// not just that the assets exist on disk.
    /// </summary>
    public sealed class GeneratedScenePlayModeTests
    {
        private const string PrototypeArena = "Prototype_Arena";
        private const string AvatarShowcase = "Avatar_Showcase";

        private static readonly (EquipmentSlot Slot, string Variant)[] DefaultLoadout =
        {
            (EquipmentSlot.Helmet, "Scrapper"),
            (EquipmentSlot.Armor, "Scrapper"),
            (EquipmentSlot.Melee, "ScrapHammer"),
            (EquipmentSlot.Ranged, "PulseCaster"),
            (EquipmentSlot.Shield, "AegisEmitter"),
            (EquipmentSlot.Rig, "Reclaimer")
        };

        private static readonly (EquipmentSlot Slot, string Variant)[] AlternateLoadout =
        {
            (EquipmentSlot.Helmet, "Sentinel"),
            (EquipmentSlot.Armor, "Bulwark"),
            (EquipmentSlot.Melee, "PlasmaCutter"),
            (EquipmentSlot.Ranged, "ArcBlaster"),
            (EquipmentSlot.Shield, "PrismEmitter"),
            (EquipmentSlot.Rig, "Capacitor")
        };

        [UnityTest]
        public IEnumerator PrototypeArena_LoadsWithoutErrors()
        {
            yield return LoadScene(PrototypeArena);

            Assert.AreEqual(PrototypeArena, SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator PrototypeArena_HasEnclosedGrayboxGeometry()
        {
            yield return LoadScene(PrototypeArena);

            var floor = GameObject.Find("Floor");
            Assert.IsNotNull(floor, "The arena has no floor.");
            Assert.IsNotNull(floor.GetComponent<Collider>(), "The floor has no collider.");
            Assert.AreEqual(GameLayers.Environment, floor.layer, "The floor is not on the Environment layer.");

            foreach (var wallName in new[] { "Wall_North", "Wall_South", "Wall_East", "Wall_West" })
            {
                var wall = GameObject.Find(wallName);
                Assert.IsNotNull(wall, "Missing arena wall: " + wallName + ".");
                var collider = wall.GetComponent<Collider>();
                Assert.IsNotNull(collider, wallName + " has no collider, so the player could leave the arena.");
                Assert.IsTrue(collider.enabled, wallName + " has a disabled collider.");
                Assert.AreEqual(GameLayers.Environment, wall.layer,
                    wallName + " is not on the Environment layer, so it will not block projectiles or dodges.");
            }

            foreach (var markerName in new[] { "PlayerStart", "SpawnPoint_A", "SpawnPoint_B", "SpawnPoint_C", "ZoneRoot" })
            {
                Assert.IsNotNull(GameObject.Find(markerName), "Missing gameplay marker: " + markerName + ".");
            }
        }

        [UnityTest]
        public IEnumerator PrototypeArena_FixedCameraFramesTheWholeArena()
        {
            yield return LoadScene(PrototypeArena);

            var camera = Camera.main;
            Assert.IsNotNull(camera, "The arena has no MainCamera-tagged camera.");
            Assert.IsTrue(camera.orthographic, "The prototype camera should be orthographic for predictable readability.");

            camera.aspect = 16f / 9f;
            var corners = new[]
            {
                new Vector3(-10f, 0f, -10f), new Vector3(10f, 0f, -10f),
                new Vector3(-10f, 0f, 10f), new Vector3(10f, 0f, 10f)
            };

            foreach (var corner in corners)
            {
                var viewport = camera.WorldToViewportPoint(corner);
                Assert.IsTrue(viewport.z > 0f, "Arena corner " + corner + " is behind the camera.");
                Assert.IsTrue(viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f,
                    "Arena corner " + corner + " is outside the 16:9 view at " + viewport + ".");
            }

            var pitch = camera.transform.eulerAngles.x;
            Assert.IsTrue(pitch > 25f && pitch < 55f, "Camera pitch " + pitch + " is outside the isometric range.");
        }

        [UnityTest]
        public IEnumerator PrototypeArena_SpawnsThePlayerVisualWithTheDefaultLoadout()
        {
            yield return LoadScene(PrototypeArena);

            var avatar = UnityEngine.Object.FindAnyObjectByType<ModularAvatar>();
            Assert.IsNotNull(avatar, "The arena contains no player visual.");
            AssertLoadout(avatar, DefaultLoadout);
        }

        [UnityTest]
        public IEnumerator PrototypeArena_PlayerHasPhaseThreeLocomotionAndCannotCrossAWall()
        {
            yield return LoadScene(PrototypeArena);

            var motor = UnityEngine.Object.FindAnyObjectByType<PlayerMotor>();
            Assert.IsNotNull(motor, "The arena player has no PlayerMotor.");
            Assert.IsNotNull(motor.GetComponent<PlayerInputReader>(), "The motor has no input reader.");
            Assert.IsNotNull(motor.GetComponent<PlayerFacing>(), "The player has no facing controller.");
            var controller = motor.GetComponent<CharacterController>();
            Assert.IsNotNull(controller, "The player has no CharacterController.");
            Assert.AreEqual(GameLayers.Player, motor.gameObject.layer);

            motor.enabled = false;
            motor.transform.position = Vector3.zero;
            Physics.SyncTransforms();
            for (var step = 0; step < 60; step++)
            {
                controller.Move(Vector3.right * 0.5f);
            }

            Assert.Less(motor.transform.position.x, 10f,
                "CharacterController movement crossed the east arena wall.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator PrototypeArena_PlayerAndDummyShareTheDamageApi()
        {
            yield return LoadScene(PrototypeArena);

            var bodies = UnityEngine.Object.FindObjectsByType<Health>(FindObjectsInactive.Exclude);
            var player = bodies.Single(candidate => candidate.gameObject.layer == GameLayers.Player);
            var dummy = bodies.Single(candidate => candidate.name == "TargetDummy");
            Assert.IsNotNull(player.GetComponent<PlayerActionCoordinator>());
            Assert.IsNotNull(dummy.GetComponent<TargetDummy>());

            var playerResult = ((IDamageable)player).ReceiveDamage(new DamageInfo(10f, dummy.gameObject));
            var dummyResult = ((IDamageable)dummy).ReceiveDamage(new DamageInfo(10f, player.gameObject));
            Assert.AreEqual(10f, playerResult.AppliedDamage);
            Assert.AreEqual(10f, dummyResult.AppliedDamage);
            Assert.AreEqual(90f, player.CurrentHealth);
            Assert.AreEqual(40f, dummy.CurrentHealth);
        }

        /// <summary>
        /// The melee slice has to work in the scene that ships, not only on a rig built by a test.
        /// This drives the arena player's own controller against the arena's own practice target.
        /// </summary>
        [UnityTest]
        public IEnumerator PrototypeArena_MeleeSwingDamagesThePracticeDummy()
        {
            yield return LoadScene(PrototypeArena);

            var melee = UnityEngine.Object.FindAnyObjectByType<MeleeController>();
            Assert.IsNotNull(melee, "The arena player has no melee controller.");
            Assert.IsNotNull(melee.GetComponent<HitStop>(), "The arena player has no hit-stop feedback.");

            var dummy = UnityEngine.Object.FindObjectsByType<Health>(FindObjectsInactive.Exclude)
                .Single(candidate => candidate.name == "TargetDummy");
            var startingHealth = dummy.CurrentHealth;

            melee.enabled = false;
            melee.transform.position = dummy.transform.position - new Vector3(0f, 1f, 2f);
            Physics.SyncTransforms();
            yield return null;

            // Neutral sticks preserve the last combat facing, so this survives until the swing starts.
            melee.GetComponent<PlayerFacing>().ApplyAttackFacing(Vector3.forward);
            Assert.IsTrue(melee.TryStartSwing(), "The arena player refused to swing.");
            melee.Tick(0.06f);

            Assert.AreEqual(MeleePhase.Active, melee.Phase);
            Assert.Less(dummy.CurrentHealth, startingHealth, "The swing did not damage the practice dummy.");
        }

        /// <summary>
        /// As with melee, ranged fire has to work in the scene that ships. This drives the arena
        /// player's own controller and pool against the arena's own practice target.
        /// </summary>
        [UnityTest]
        public IEnumerator PrototypeArena_RangedFireDamagesThePracticeDummy()
        {
            yield return LoadScene(PrototypeArena);

            var ranged = UnityEngine.Object.FindAnyObjectByType<RangedController>();
            Assert.IsNotNull(ranged, "The arena player has no ranged controller.");
            Assert.IsNotNull(ranged.Pool, "The arena player's ranged controller has no projectile pool.");
            Assert.Greater(ranged.Pool.Capacity, 0, "The projectile pool was never filled.");

            var dummy = UnityEngine.Object.FindObjectsByType<Health>(FindObjectsInactive.Exclude)
                .Single(candidate => candidate.name == "TargetDummy");
            var startingHealth = dummy.CurrentHealth;

            ranged.enabled = false;
            ranged.Pool.enabled = false;

            // Shots leave the Muzzle anchor, which sits off the body's centre line. Line that anchor
            // up with the dummy rather than the player's origin, or the test would be measuring where
            // the avatar happens to hold its weapon.
            var registry = ranged.GetComponent<AvatarSocketRegistry>();
            Assert.IsTrue(registry.TryGet(AvatarSocketId.Muzzle, out var muzzle), "The avatar has no Muzzle anchor.");
            var muzzleOffset = muzzle.position - ranged.transform.position;

            ranged.transform.position = new Vector3(
                dummy.transform.position.x - muzzleOffset.x,
                ranged.transform.position.y,
                dummy.transform.position.z - 6f);
            Physics.SyncTransforms();
            yield return null;

            ranged.GetComponent<PlayerFacing>().ApplyAttackFacing(Vector3.forward);
            ranged.Tick(1f / 60f, true);
            Assert.AreEqual(1, ranged.Pool.ActiveCount, "No projectile left the muzzle.");

            for (var step = 0; step < 240 && ranged.Pool.ActiveCount > 0; step++)
            {
                ranged.Pool.Tick(1f / 60f);
            }

            Assert.Less(dummy.CurrentHealth, startingHealth, "The projectile did not damage the practice dummy.");
            Assert.AreEqual(0, ranged.Pool.ActiveCount, "The projectile never returned to the pool.");
        }

        /// <summary>
        /// The arena player's own shield, blocking a hit from the front and letting the same hit
        /// through from behind. Nothing in the arena attacks yet, so the hits are directed by hand —
        /// which is exactly what the debug overlay's front and rear buttons do.
        /// </summary>
        [UnityTest]
        public IEnumerator PrototypeArena_ShieldBlocksFromTheFrontAndNotFromBehind()
        {
            yield return LoadScene(PrototypeArena);

            var shield = UnityEngine.Object.FindAnyObjectByType<ShieldController>();
            Assert.IsNotNull(shield, "The arena player has no shield controller.");

            var health = shield.GetComponent<Health>();
            shield.enabled = false;
            shield.Tick(1f / 60f, true, Vector2.zero);
            Assert.IsTrue(shield.IsRaised, "The arena player refused to raise its shield.");

            var startingStability = shield.Stability;
            var front = shield.transform.position + shield.ShieldFacing * 4f;
            health.ReceiveDamage(new DamageInfo(10f, front));

            Assert.AreEqual(100f, health.CurrentHealth, "A frontal hit reached health through a raised shield.");
            Assert.Less(shield.Stability, startingStability, "A frontal hit cost no stability.");

            var behind = shield.transform.position - shield.ShieldFacing * 4f;
            var stabilityBeforeRearHit = shield.Stability;
            health.ReceiveDamage(new DamageInfo(10f, behind));

            Assert.AreEqual(90f, health.CurrentHealth, "A hit from behind must reach health.");
            Assert.AreEqual(stabilityBeforeRearHit, shield.Stability, "A hit from behind must not cost stability.");
        }

        [UnityTest]
        public IEnumerator AvatarShowcase_LoadsWithAnAvatarAndShowcaseDriver()
        {
            yield return LoadScene(AvatarShowcase);

            var avatar = UnityEngine.Object.FindAnyObjectByType<ModularAvatar>();
            Assert.IsNotNull(avatar, "The showcase scene contains no avatar.");
            Assert.IsNotNull(avatar.GetComponent<Vaultbreakers.Debugging.ModularAvatarShowcase>(),
                "The showcase driver is not attached to the avatar.");
            AssertLoadout(avatar, DefaultLoadout);
        }

        /// <summary>
        /// The showcase is a visual bench. If the gameplay half of the player prefab survives here,
        /// WASD drives the avatar off the turntable and PlayerFacing fights the turntable rotation.
        /// </summary>
        [UnityTest]
        public IEnumerator AvatarShowcase_HasNoGameplayComponentsDrivingTheTurntable()
        {
            yield return LoadScene(AvatarShowcase);

            var avatar = UnityEngine.Object.FindAnyObjectByType<ModularAvatar>();
            Assert.IsNull(avatar.GetComponent<PlayerMotor>(), "Locomotion is still active in the showcase scene.");
            Assert.IsNull(avatar.GetComponent<PlayerFacing>(), "Facing would fight the turntable rotation.");
            Assert.IsNull(avatar.GetComponent<CharacterController>(), "The showcase avatar should not be a moving body.");
            Assert.IsNull(avatar.GetComponent<PlayerInputReader>(), "The showcase avatar should not read player intent.");
            Assert.IsNull(avatar.GetComponent<Health>(), "The showcase avatar is not a combat target.");
            Assert.IsNull(avatar.GetComponent<MeleeController>(), "The showcase avatar should not be able to attack.");
            Assert.IsNull(avatar.GetComponent<MeleePresentation>(), "Swing presentation would fight the turntable pose.");
            Assert.IsNull(avatar.GetComponent<HitStop>(), "A visual bench must never touch the global timescale.");
            Assert.IsNull(avatar.GetComponent<RangedController>(), "The showcase avatar should not be able to shoot.");
            Assert.IsNull(avatar.GetComponent<RangedPresentation>(), "Recoil would fight the turntable pose.");
            Assert.IsNull(avatar.GetComponent<ProjectilePool>(), "A visual bench must not spawn a projectile pool.");
            Assert.IsNull(avatar.GetComponent<ShieldController>(), "The showcase avatar should not be able to block.");
            Assert.IsNull(avatar.GetComponent<ShieldPresentation>(), "A shield arc would obscure the model on show.");
            Assert.IsNull(avatar.GetComponent<DodgeController>(), "The showcase avatar should not be able to dodge.");
            Assert.IsNull(avatar.GetComponent<DodgePresentation>(),
                "A dodge squash would fight the turntable pose and leave a streak on the bench.");
        }

        [UnityTest]
        public IEnumerator AvatarShowcase_SwapsTheCompleteAlternateLoadoutAtRuntime()
        {
            yield return LoadScene(AvatarShowcase);

            var avatar = UnityEngine.Object.FindAnyObjectByType<ModularAvatar>();
            foreach (var entry in AlternateLoadout)
            {
                Assert.IsTrue(avatar.Equip(entry.Slot, entry.Variant), "Equip rejected " + entry.Slot + "/" + entry.Variant + ".");
            }

            yield return null;
            AssertLoadout(avatar, AlternateLoadout);
        }

        [UnityTest]
        public IEnumerator AvatarShowcase_SocketsSurviveEveryEquipmentCombination()
        {
            yield return LoadScene(AvatarShowcase);

            var avatar = UnityEngine.Object.FindAnyObjectByType<ModularAvatar>();
            var registry = avatar.GetComponent<AvatarSocketRegistry>();
            Assert.IsNotNull(registry, "The avatar has no socket registry.");

            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                foreach (var module in avatar.Modules.Where(candidate => candidate.Slot == slot).ToArray())
                {
                    Assert.IsTrue(avatar.Equip(slot, module.VariantId));
                    yield return null;

                    foreach (AvatarSocketId id in Enum.GetValues(typeof(AvatarSocketId)))
                    {
                        Assert.IsTrue(registry.TryGet(id, out var socket), "Socket " + id + " vanished after equipping " + module.VariantId + ".");
                        Assert.IsTrue(socket.gameObject.activeInHierarchy,
                            "Socket " + id + " was deactivated by equipping " + slot + "/" + module.VariantId + ".");
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator Avatar_RejectsAnUnknownVariantWithoutChangingVisuals()
        {
            yield return LoadScene(AvatarShowcase);

            var avatar = UnityEngine.Object.FindAnyObjectByType<ModularAvatar>();
            Assert.IsFalse(avatar.Equip(EquipmentSlot.Melee, "NoSuchWeapon"));
            AssertLoadout(avatar, DefaultLoadout);
        }

        private static IEnumerator LoadScene(string sceneName)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            Assert.IsNotNull(operation, "Scene " + sceneName + " is not in the build settings.");
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
        }

        private static void AssertLoadout(ModularAvatar avatar, (EquipmentSlot Slot, string Variant)[] expected)
        {
            foreach (var entry in expected)
            {
                Assert.IsTrue(avatar.TryGetEquipped(entry.Slot, out var variant), "Nothing equipped in slot " + entry.Slot + ".");
                Assert.AreEqual(entry.Variant, variant, "Unexpected variant in slot " + entry.Slot + ".");

                foreach (var module in avatar.Modules.Where(candidate => candidate.Slot == entry.Slot))
                {
                    var shouldBeVisible = module.VariantId == entry.Variant;
                    foreach (var part in module.Parts)
                    {
                        Assert.IsNotNull(part, "Module " + entry.Slot + "/" + module.VariantId + " lost a part.");
                        Assert.AreEqual(shouldBeVisible, part.activeSelf,
                            "Part " + part.name + " visibility does not match the equipped loadout.");
                    }
                }
            }
        }
    }
}
