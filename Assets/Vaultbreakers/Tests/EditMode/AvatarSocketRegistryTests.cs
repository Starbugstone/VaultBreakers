using NUnit.Framework;
using UnityEngine;
using Vaultbreakers.Equipment;

namespace Vaultbreakers.Tests.EditMode
{
    public sealed class AvatarSocketRegistryTests
    {
        private GameObject root;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("SocketOwner");
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TryGet_ReturnsTheBoundTransform()
        {
            var registry = root.AddComponent<AvatarSocketRegistry>();
            var muzzle = new GameObject("ANCHOR_Muzzle").transform;
            muzzle.SetParent(root.transform, false);
            registry.Configure(new[] { new AvatarSocketBinding(AvatarSocketId.Muzzle, muzzle) });

            Assert.IsTrue(registry.TryGet(AvatarSocketId.Muzzle, out var socket));
            Assert.AreSame(muzzle, socket);
        }

        [Test]
        public void TryGet_ReturnsFalseForAnUnboundSocket()
        {
            var registry = root.AddComponent<AvatarSocketRegistry>();
            registry.Configure(new AvatarSocketBinding[0]);

            Assert.IsFalse(registry.TryGet(AvatarSocketId.Shield, out var socket));
            Assert.IsNull(socket);
        }

        [Test]
        public void TryGet_ReturnsFalseWhenTheBoundTransformWasDestroyed()
        {
            var registry = root.AddComponent<AvatarSocketRegistry>();
            var shield = new GameObject("ANCHOR_Shield");
            registry.Configure(new[] { new AvatarSocketBinding(AvatarSocketId.Shield, shield.transform) });
            Object.DestroyImmediate(shield);

            Assert.IsFalse(registry.TryGet(AvatarSocketId.Shield, out var socket));
            Assert.IsNull(socket);
        }

        [Test]
        public void Configure_WithNullClearsTheRegistryWithoutThrowing()
        {
            var registry = root.AddComponent<AvatarSocketRegistry>();
            registry.Configure(null);

            Assert.DoesNotThrow(() => registry.TryGet(AvatarSocketId.Feet, out _));
        }
    }
}
