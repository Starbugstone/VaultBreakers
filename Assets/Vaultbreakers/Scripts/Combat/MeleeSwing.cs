using UnityEngine;

namespace Vaultbreakers.Combat
{
    /// <summary>
    /// The geometry of one melee swing, kept pure so the locked rules in COMBAT_POC_PLAN.md
    /// section 6 can be tested without a scene, a collider, or a frame of play.
    /// </summary>
    public static class MeleeSwing
    {
        private const float MinimumSquareLength = 0.0001f;

        /// <summary>
        /// Centre of the overlap sphere. Placing it at (range - radius) in front of the attacker puts
        /// the far edge of the sphere exactly at the configured range, which is what makes the two
        /// tuning numbers mean what they say.
        /// </summary>
        public static Vector3 QueryCentre(Vector3 origin, Vector3 swingDirection, float range, float radius)
        {
            var flattened = Flatten(swingDirection);
            if (flattened == Vector3.zero)
            {
                return origin;
            }

            return origin + flattened * Mathf.Max(0f, range - radius);
        }

        /// <summary>
        /// Whether a point is inside the swing volume. This mirrors the physics query the controller
        /// runs: horizontal distance from the sphere centre, so a target hugging the attacker is hit
        /// while a target well behind is not.
        /// </summary>
        public static bool IsWithinSwing(
            Vector3 origin,
            Vector3 swingDirection,
            Vector3 targetPosition,
            float range,
            float radius)
        {
            var centre = QueryCentre(origin, swingDirection, range, radius);
            var offset = targetPosition - centre;
            offset.y = 0f;
            return offset.sqrMagnitude <= radius * radius;
        }

        /// <summary>
        /// Snaps the swing toward a target that is already inside the correction cone and leaves it
        /// untouched otherwise. The cone half-angle doubles as the cap, so the swing can never be
        /// pulled further than the configured number of degrees and a target the player did not aim
        /// at can never steal the strike. The player is never moved: only the direction changes.
        /// </summary>
        public static Vector3 ApplyTargetCorrection(
            Vector3 swingDirection,
            Vector3 origin,
            Vector3 targetPosition,
            float maximumCorrectionDegrees)
        {
            var swing = Flatten(swingDirection);
            if (swing == Vector3.zero)
            {
                return swingDirection;
            }

            var toTarget = Flatten(targetPosition - origin);
            if (toTarget == Vector3.zero)
            {
                return swing;
            }

            return Vector3.Angle(swing, toTarget) <= maximumCorrectionDegrees ? toTarget : swing;
        }

        /// <summary>
        /// True when a candidate is a legal correction target: inside the cone and inside the reach.
        /// Reach is measured from the attacker rather than from the sphere centre, because correction
        /// is chosen before the swing direction is final.
        /// </summary>
        public static bool IsCorrectionCandidate(
            Vector3 swingDirection,
            Vector3 origin,
            Vector3 targetPosition,
            float range,
            float maximumCorrectionDegrees)
        {
            var swing = Flatten(swingDirection);
            var toTarget = Flatten(targetPosition - origin);
            if (swing == Vector3.zero || toTarget == Vector3.zero)
            {
                return false;
            }

            var horizontalOffset = targetPosition - origin;
            horizontalOffset.y = 0f;
            return horizontalOffset.magnitude <= range &&
                   Vector3.Angle(swing, toTarget) <= maximumCorrectionDegrees;
        }

        /// <summary>Horizontal knockback direction, away from the attacker.</summary>
        public static Vector3 KnockbackDirection(Vector3 origin, Vector3 targetPosition, Vector3 swingDirection)
        {
            var away = Flatten(targetPosition - origin);
            return away != Vector3.zero ? away : Flatten(swingDirection);
        }

        private static Vector3 Flatten(Vector3 value)
        {
            value.y = 0f;
            return value.sqrMagnitude > MinimumSquareLength ? value.normalized : Vector3.zero;
        }
    }
}
