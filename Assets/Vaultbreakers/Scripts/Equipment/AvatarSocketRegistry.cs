using System;
using UnityEngine;

namespace Vaultbreakers.Equipment
{
    public enum AvatarSocketId
    {
        RightHandMelee = 0,
        LeftArmRangedShield = 1,
        Back = 2,
        PetAnchor = 3,
        Muzzle = 4,
        Shield = 5,
        MeleeTrail = 6,
        Hit = 7,
        Feet = 8
    }

    [Serializable]
    public struct AvatarSocketBinding
    {
        [SerializeField] private AvatarSocketId id;
        [SerializeField] private Transform transform;

        public AvatarSocketId Id => id;
        public Transform Transform => transform;

        public AvatarSocketBinding(AvatarSocketId id, Transform transform)
        {
            this.id = id;
            this.transform = transform;
        }
    }

    /// <summary>
    /// Serialized socket lookup. Callers never search the hierarchy by name at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AvatarSocketRegistry : MonoBehaviour
    {
        [SerializeField] private AvatarSocketBinding[] sockets = Array.Empty<AvatarSocketBinding>();

        public bool TryGet(AvatarSocketId id, out Transform socket)
        {
            for (var index = 0; index < sockets.Length; index++)
            {
                if (sockets[index].Id == id && sockets[index].Transform != null)
                {
                    socket = sockets[index].Transform;
                    return true;
                }
            }

            socket = null;
            return false;
        }

        public void Configure(AvatarSocketBinding[] bindings)
        {
            sockets = bindings ?? Array.Empty<AvatarSocketBinding>();
        }
    }
}
