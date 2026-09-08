using UnityEditor;
using UnityEngine;
using Vaultbreakers.Combat;
using Vaultbreakers.Core;

namespace Vaultbreakers.Editor
{
    /// <summary>
    /// Builds the pooled player projectile. It carries no collider and no rigidbody on purpose: the
    /// projectile sweeps its own path each step, so all this prefab contributes is the component, the
    /// layer, and a tracer silhouette that reads across the arena.
    /// </summary>
    internal static class VaultbreakersProjectileBuilder
    {
        public static GameObject BuildPlayerProjectile(PrototypeBalance balance)
        {
            var root = new GameObject("PF_PlayerProjectile") { layer = GameLayers.PlayerProjectile };
            try
            {
                root.AddComponent<Projectile>()
                    .Configure(balance != null ? balance.ProjectileRadius : 0.12f);

                VaultbreakersArtBuilder.Model("Assets/Vaultbreakers/Art/VFX/Player_Bolt.fbx", root.transform, "Tracer");

                return PrefabUtility.SaveAsPrefabAsset(root, VaultbreakersSetupPaths.ProjectilePrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
