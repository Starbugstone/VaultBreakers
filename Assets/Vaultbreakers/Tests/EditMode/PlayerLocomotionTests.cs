using NUnit.Framework;
using UnityEngine;
using Vaultbreakers.Player;

namespace Vaultbreakers.Tests.EditMode
{
    public sealed class PlayerLocomotionTests
    {
        [Test]
        public void CameraRelativeDirection_ProjectsMovementOntoTheGroundPlane()
        {
            var camera = new GameObject("Camera").transform;
            try
            {
                camera.rotation = Quaternion.Euler(35f, 45f, 0f);
                var direction = PlayerMotor.CameraRelativeDirection(Vector2.up, camera);
                Assert.That(direction.y, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(direction.magnitude, Is.EqualTo(1f).Within(0.0001f));
                Assert.That(Vector3.Dot(direction, Vector3.ProjectOnPlane(camera.forward, Vector3.up).normalized), Is.GreaterThan(0.999f));
            }
            finally
            {
                Object.DestroyImmediate(camera.gameObject);
            }
        }

        [Test]
        public void CameraRelativeDirection_ClampsDiagonalInput()
        {
            var direction = PlayerMotor.CameraRelativeDirection(Vector2.one, null);
            Assert.That(direction.magnitude, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void Facing_AimAboveDeadZoneOverridesMovement()
        {
            var result = PlayerFacing.ResolveFacing(Vector3.back, Vector3.right, 0.5f, Vector3.left, 0.25f);
            Assert.That(result, Is.EqualTo(Vector3.right));
        }

        [Test]
        public void Facing_AimBelowDeadZoneFallsBackToMovement()
        {
            var result = PlayerFacing.ResolveFacing(Vector3.back, Vector3.right, 0.24f, Vector3.left, 0.25f);
            Assert.That(result, Is.EqualTo(Vector3.left));
        }

        [Test]
        public void Facing_UsesAimAtExactDeadZoneBoundary()
        {
            var result = PlayerFacing.ResolveFacing(Vector3.back, Vector3.right, 0.25f, Vector3.left, 0.25f);
            Assert.That(result, Is.EqualTo(Vector3.right));
        }

        [Test]
        public void Facing_NeutralInputPreservesPreviousDirection()
        {
            var previous = new Vector3(1f, 0f, 1f).normalized;
            var result = PlayerFacing.ResolveFacing(previous, Vector3.zero, 0f, Vector3.zero, 0.25f);
            Assert.That(result, Is.EqualTo(previous));
        }
    }
}
