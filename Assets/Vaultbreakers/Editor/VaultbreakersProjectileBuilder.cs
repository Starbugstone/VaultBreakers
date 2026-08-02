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

                var tracer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                tracer.name = "Tracer";
                tracer.layer = GameLayers.PlayerProjectile;
                tracer.transform.SetParent(root.transform, false);

                // The capsule's long axis is Y, so it is laid down to point along the travel axis.
                tracer.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                tracer.transform.localScale = new Vector3(0.16f, 0.35f, 0.16f);
                tracer.GetComponent<Renderer>().sharedMaterial =
                    VaultbreakersRenderSetup.LoadMaterial("VB_ProjectileCore");

                var collider = tracer.GetComponent<Collider>();
                if (collider != null)
                {
                    Object.DestroyImmediate(collider);
                }

                return PrefabUtility.SaveAsPrefabAsset(root, VaultbreakersSetupPaths.ProjectilePrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
