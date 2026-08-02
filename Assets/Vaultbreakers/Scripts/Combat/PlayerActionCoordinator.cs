using System;
using UnityEngine;

namespace Vaultbreakers.Combat
{
    /// <summary>
    /// Current player action state. Values are combined, so append new flags instead of reordering.
    /// <see cref="Stunned"/> and <see cref="ShieldBroken"/> are read by the conflict rules here but
    /// are only ever raised by the hit-reaction and shield phases that own them.
    /// </summary>
    [Flags]
    public enum PlayerActionState
    {
        None = 0,
        Attacking = 1 << 0,
        Dodging = 1 << 1,
        Shielding = 1 << 2,
        ShieldBroken = 1 << 3,
        Stunned = 1 << 4,
        Dead = 1 << 5
    }

    public enum PlayerAction { Melee, Ranged, Shield, Dodge }

    /// <summary>
    /// Validates the action conflict rules in COMBAT_POC_PLAN.md section 4. It owns no timing and no
    /// mechanics: individual controllers ask whether an action may start and report when it ends.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerActionCoordinator : MonoBehaviour
    {
        private const PlayerActionState BlockingStates = PlayerActionState.Dead | PlayerActionState.Stunned;

        [SerializeField] private Health health;

        private Health subscribedHealth;
        private bool meleeActive;
        private bool rangedActive;

        public PlayerActionState State { get; private set; }
        public bool IsMeleeActive => meleeActive;
        public bool IsRangedActive => rangedActive;

        private void Awake()
        {
            Configure(health != null ? health : GetComponent<Health>());
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        public bool TryStart(PlayerAction action)
        {
            if ((State & BlockingStates) != 0)
            {
                return false;
            }

            switch (action)
            {
                case PlayerAction.Dodge:
                    // Dodge outranks everything: it cancels attacks and drops the shield immediately.
                    meleeActive = false;
                    rangedActive = false;
                    State &= ~(PlayerActionState.Attacking | PlayerActionState.Shielding);
                    State |= PlayerActionState.Dodging;
                    return true;

                case PlayerAction.Melee:
                    if ((State & PlayerActionState.Dodging) != 0)
                    {
                        return false;
                    }

                    rangedActive = false;
                    meleeActive = true;
                    State &= ~PlayerActionState.Shielding;
                    State |= PlayerActionState.Attacking;
                    return true;

                case PlayerAction.Ranged:
                    if ((State & (PlayerActionState.Dodging | PlayerActionState.Shielding)) != 0)
                    {
                        return false;
                    }

                    rangedActive = true;
                    meleeActive = false;
                    State |= PlayerActionState.Attacking;
                    return true;

                case PlayerAction.Shield:
                    if ((State & (PlayerActionState.Dodging | PlayerActionState.ShieldBroken)) != 0 || meleeActive)
                    {
                        return false;
                    }

                    rangedActive = false;
                    State &= ~PlayerActionState.Attacking;
                    State |= PlayerActionState.Shielding;
                    return true;

                default:
                    return false;
            }
        }

        public void Stop(PlayerAction action)
        {
            switch (action)
            {
                case PlayerAction.Melee:
                    meleeActive = false;
                    break;
                case PlayerAction.Ranged:
                    rangedActive = false;
                    break;
                case PlayerAction.Shield:
                    State &= ~PlayerActionState.Shielding;
                    break;
                case PlayerAction.Dodge:
                    State &= ~PlayerActionState.Dodging;
                    break;
            }

            if (!meleeActive && !rangedActive)
            {
                State &= ~PlayerActionState.Attacking;
            }
        }

        public void ResetState()
        {
            meleeActive = false;
            rangedActive = false;
            State = PlayerActionState.None;
        }

        public void Configure(Health newHealth)
        {
            Unsubscribe();
            health = newHealth;
            subscribedHealth = health;

            if (subscribedHealth != null)
            {
                subscribedHealth.Died += OnDied;
            }
        }

        private void Unsubscribe()
        {
            if (subscribedHealth != null)
            {
                subscribedHealth.Died -= OnDied;
            }

            subscribedHealth = null;
        }

        /// <summary>Death cancels every active action and blocks new ones until the state is reset.</summary>
        private void OnDied(DamageInfo damage)
        {
            meleeActive = false;
            rangedActive = false;
            State = PlayerActionState.Dead;
        }
    }
}
