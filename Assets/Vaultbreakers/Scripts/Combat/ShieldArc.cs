using UnityEngine;

namespace Vaultbreakers.Combat
{
    /// <summary>
    /// The horizontal arc test from COMBAT_POC_PLAN.md section 4, kept pure so every boundary case —
    /// dead centre, the exact edge, a hair outside it, directly behind, and the wrap through zero —
    /// can be tested without a scene. A shield that quietly becomes omnidirectional is one of the
    /// named risks for this phase, and this is where that is prevented.
    /// </summary>
    public static class ShieldArc
    {
        private const float MinimumSquareLength = 0.0001f;

        /// <summary>
        /// A hundredth of a degree of slack on the edge. Rotating a direction and measuring the angle
        /// back out lands a hair either side of the authored value, so an attack placed exactly on the
        /// edge of a 120 degree arc would otherwise block or not depending on float representation.
        /// At melee range this is under a millimetre: it settles the boundary without widening the arc.
        /// </summary>
        private const float EdgeTolerance = 0.01f;

        /// <summary>
        /// True when a hit arriving from <paramref name="sourcePosition"/> lands inside the arc. Only
        /// the horizontal direction matters: an attack from above or below is judged by where it came
        /// from on the ground plane, never by its height.
        /// </summary>
        public static bool IsWithinArc(
            Vector3 shieldFacing,
            Vector3 defenderPosition,
            Vector3 sourcePosition,
            float arcDegrees)
        {
            var facing = Flatten(shieldFacing);
            var toSource = Flatten(sourcePosition - defenderPosition);

            // A source standing exactly on the defender has no direction to judge, and a shield with
            // no facing has no arc. Both are unshieldable rather than universally blocking.
            if (facing == Vector3.zero || toSource == Vector3.zero)
            {
                return false;
            }

            return Vector3.Angle(facing, toSource) <= Mathf.Max(0f, arcDegrees) * 0.5f + EdgeTolerance;
        }

        /// <summary>
        /// Whether a shield may block this particular hit. A hit with no known source is unshieldable
        /// by design: guessing a direction would hand the player an accidental omnidirectional block.
        /// </summary>
        public static bool CanBlock(
            in DamageInfo damage,
            Vector3 shieldFacing,
            Vector3 defenderPosition,
            float arcDegrees)
        {
            return damage.HasSourcePosition &&
                   IsWithinArc(shieldFacing, defenderPosition, damage.SourcePosition, arcDegrees);
        }

        private static Vector3 Flatten(Vector3 value)
        {
            value.y = 0f;
            return value.sqrMagnitude > MinimumSquareLength ? value.normalized : Vector3.zero;
        }
    }
}
