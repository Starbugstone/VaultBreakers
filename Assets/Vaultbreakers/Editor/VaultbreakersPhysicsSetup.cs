using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Vaultbreakers.Core;

namespace Vaultbreakers.Editor
{
    /// <summary>
    /// Idempotent physics layer and collision matrix setup for PROJECT.md section 5.5 and the
    /// layer rules in COMBAT_POC_PLAN.md section 5. This is the executable source of truth:
    /// the collision matrix is stored as an opaque bitfield in DynamicsManager.asset, so the
    /// intent has to live in code where it can be reviewed and tested.
    /// </summary>
    public static class VaultbreakersPhysicsSetup
    {
        private const string TagManagerPath = "ProjectSettings/TagManager.asset";
        private const string DynamicsManagerPath = "ProjectSettings/DynamicsManager.asset";

        /// <summary>
        /// Which gameplay layers are allowed to collide. Listed once per pair; the symmetric
        /// counterpart is derived, so a layer that only ever receives collisions needs no entry
        /// of its own. Every pair absent from this table is ignored by the physics engine.
        /// </summary>
        private static readonly Dictionary<int, int[]> AllowedCollisions = new()
        {
            // The player body stops on geometry, trades hits with enemies, receives enemy
            // attacks, and picks things up. It never collides with another player or with
            // its own attacks.
            {
                GameLayers.Player,
                new[]
                {
                    GameLayers.Enemy, GameLayers.EnemyProjectile, GameLayers.EnemyAttack,
                    GameLayers.Gem, GameLayers.Hazard, GameLayers.Interactable, GameLayers.Environment
                }
            },

            // Enemies separate from each other and take player damage. No friendly fire.
            {
                GameLayers.Enemy,
                new[] { GameLayers.Enemy, GameLayers.PlayerProjectile, GameLayers.PlayerMeleeHit, GameLayers.Environment }
            },

            // Projectiles resolve one hit against the opposing faction or the arena.
            { GameLayers.PlayerProjectile, new[] { GameLayers.Environment } },
            { GameLayers.EnemyProjectile, new[] { GameLayers.Environment } },

            // Gems settle on the floor and are collected by the player or a collector pet.
            // They never block movement or stop a projectile.
            { GameLayers.Gem, new[] { GameLayers.Pet, GameLayers.Environment } },

            // Pets stay inside the arena but never obstruct players, enemies, or combat.
            { GameLayers.Pet, new[] { GameLayers.Environment } },

            { GameLayers.Environment, new[] { GameLayers.Environment } }

            // PlayerMeleeHit, EnemyAttack, Hazard, and Interactable only ever receive collisions,
            // and Debug never participates at all, so they need no entries of their own.
        };

        private static HashSet<long> allowedPairs;

        [MenuItem("Vaultbreakers/Setup/Configure Physics Layers")]
        public static void Apply()
        {
            EnsureLayers();
            ConfigureCollisionMatrix();
            Persist();
            Debug.Log("Vaultbreakers physics layers and collision matrix configured.");
        }

        /// <summary>
        /// True when the two gameplay layers are allowed to interact. Layers outside the
        /// Vaultbreakers range keep Unity's defaults and are not managed here.
        /// </summary>
        public static bool ShouldCollide(int layerA, int layerB)
        {
            allowedPairs ??= BuildAllowedPairs();
            return allowedPairs.Contains(PairKey(layerA, layerB));
        }

        public static void EnsureLayers()
        {
            var asset = AssetDatabase.LoadAllAssetsAtPath(TagManagerPath).FirstOrDefault();
            if (asset == null)
            {
                throw new InvalidOperationException("Could not open " + TagManagerPath + ".");
            }

            var tagManager = new SerializedObject(asset);
            var layers = tagManager.FindProperty("layers");
            if (layers == null || !layers.isArray)
            {
                throw new InvalidOperationException("TagManager.asset has no layer array.");
            }

            foreach (var definition in GameLayers.All)
            {
                if (definition.Index >= layers.arraySize)
                {
                    throw new InvalidOperationException(
                        "Layer index " + definition.Index + " is outside Unity's 32 layer slots.");
                }

                var slot = layers.GetArrayElementAtIndex(definition.Index);
                var current = slot.stringValue;
                if (!string.IsNullOrEmpty(current) && current != definition.Name)
                {
                    throw new InvalidOperationException(
                        "Layer " + definition.Index + " already holds \"" + current + "\"; refusing to overwrite it with \"" +
                        definition.Name + "\". Layer indices are serialized, so this must be resolved by hand.");
                }

                slot.stringValue = definition.Name;
            }

            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void ConfigureCollisionMatrix()
        {
            var managed = GameLayers.All.Select(definition => definition.Index).ToArray();

            for (var i = 0; i < managed.Length; i++)
            {
                for (var j = i; j < managed.Length; j++)
                {
                    Physics.IgnoreLayerCollision(managed[i], managed[j], !ShouldCollide(managed[i], managed[j]));
                }
            }
        }

        private static void Persist()
        {
            foreach (var path in new[] { TagManagerPath, DynamicsManagerPath })
            {
                var asset = AssetDatabase.LoadAllAssetsAtPath(path).FirstOrDefault();
                if (asset != null)
                {
                    EditorUtility.SetDirty(asset);
                }
            }

            AssetDatabase.SaveAssets();
        }

        private static HashSet<long> BuildAllowedPairs()
        {
            var pairs = new HashSet<long>();
            foreach (var entry in AllowedCollisions)
            {
                foreach (var other in entry.Value)
                {
                    pairs.Add(PairKey(entry.Key, other));
                }
            }

            return pairs;
        }

        private static long PairKey(int layerA, int layerB)
        {
            var low = Mathf.Min(layerA, layerB);
            var high = Mathf.Max(layerA, layerB);
            return ((long)low << 32) | (uint)high;
        }
    }
}
