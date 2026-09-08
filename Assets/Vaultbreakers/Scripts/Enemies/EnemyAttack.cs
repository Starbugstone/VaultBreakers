using UnityEngine;

namespace Vaultbreakers.Enemies
{
    public enum EnemyState { Approach, Windup, Active, Recovery, Dead }

    // Timing is gameplay owned. Animation is a consumer of this clock.
    public sealed class EnemyAttack
    {
        public EnemyState State { get; private set; } = EnemyState.Approach;
        public float Remaining { get; private set; }
        public void Begin(float windup) { State = EnemyState.Windup; Remaining = Mathf.Max(0.1f, windup); }
        public void Reset(bool dead = false) { State = dead ? EnemyState.Dead : EnemyState.Approach; Remaining = 0; }
        public bool Tick(float delta, float active, float recovery)
        {
            if (State is EnemyState.Approach or EnemyState.Dead || delta <= 0) return false;
            Remaining -= delta;
            if (Remaining > 0.00001f) return false;
            // At most one phase per frame: a hitch must not skip the visible active pose.
            if (State == EnemyState.Windup) { State = EnemyState.Active; Remaining = active; return true; }
            if (State == EnemyState.Active) { State = EnemyState.Recovery; Remaining = recovery; }
            else Reset();
            return false;
        }
        public static bool Contains(Vector3 origin, Vector3 facing, Vector3 target, float range, float arc)
        {
            var offset = target - origin; offset.y = 0;
            return offset.sqrMagnitude <= range * range &&
                (offset.sqrMagnitude < 0.0001f || Vector3.Angle(facing, offset) <= arc * 0.5f + 0.01f);
        }
    }
}
