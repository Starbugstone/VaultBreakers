using UnityEngine;
using Vaultbreakers.Combat;

namespace Vaultbreakers.Debugging
{
    /// <summary>
    /// Development view of the combat kernel. This overlay owns the combat event log: Health raises
    /// plain events and only the debug layer turns them into text, so no formatting happens in a
    /// build that never shows this overlay.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatDebugOverlay : MonoBehaviour
    {
        [SerializeField] private Health playerHealth;
        [SerializeField] private Health dummyHealth;
        [SerializeField] private PlayerActionCoordinator actions;
        [SerializeField] private MeleeController melee;
        [SerializeField] private RangedController ranged;

        private readonly HealthLogger playerLogger = new("Player");
        private readonly HealthLogger dummyLogger = new("Dummy");

        public void Configure(
            Health player,
            Health dummy,
            PlayerActionCoordinator coordinator,
            MeleeController meleeController,
            RangedController rangedController)
        {
            playerHealth = player;
            dummyHealth = dummy;
            actions = coordinator;
            melee = meleeController;
            ranged = rangedController;

            if (isActiveAndEnabled)
            {
                Resubscribe();
            }
        }

        private void OnEnable() => Resubscribe();

        private void OnDisable()
        {
            playerLogger.Detach();
            dummyLogger.Detach();
        }

        private void Resubscribe()
        {
            playerLogger.Attach(playerHealth);
            dummyLogger.Attach(dummyHealth);
        }

        private void OnGUI()
        {
            if (!UnityEngine.Debug.isDebugBuild)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(16f, 190f, 430f, 370f), GUI.skin.box);
            GUILayout.Label("COMBAT KERNEL (development)");
            GUILayout.Label($"Player: {Format(playerHealth)}   Dummy: {Format(dummyHealth)}");
            GUILayout.Label("Action state: " + (actions != null ? actions.State.ToString() : "Unavailable"));
            GUILayout.Label("Melee: " + FormatMelee());
            GUILayout.Label("Ranged: " + FormatRanged());
            GUILayout.Label("Projectiles: " + FormatPool());

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Damage dummy 10"))
            {
                Damage(dummyHealth, 10f, playerHealth);
            }

            if (GUILayout.Button("Lethal dummy") && dummyHealth != null)
            {
                Damage(dummyHealth, dummyHealth.MaximumHealth, playerHealth);
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Damage player 10"))
            {
                Damage(playerHealth, 10f, dummyHealth);
            }

            if (GUILayout.Button("Reset both"))
            {
                ResetAll();
            }

            GUILayout.EndHorizontal();

            GUILayout.Label("Recent combat events:");
            var entries = CombatEventLog.Entries;
            for (var index = 0; index < entries.Count; index++)
            {
                GUILayout.Label("• " + entries[index]);
            }

            GUILayout.EndArea();
        }

        private static void Damage(Health target, float amount, Health source)
        {
            if (target == null)
            {
                return;
            }

            target.ReceiveDamage(new DamageInfo(amount, source != null ? source.gameObject : null));
        }

        private void ResetAll()
        {
            if (playerHealth != null)
            {
                playerHealth.ResetHealth();
            }

            if (dummyHealth != null)
            {
                dummyHealth.ResetHealth();
            }

            if (actions != null)
            {
                actions.ResetState();
            }
        }

        private string FormatRanged() =>
            ranged == null
                ? "Unavailable"
                : $"{(ranged.IsFiring ? "firing" : "idle")}   next shot {ranged.CooldownRemaining:0.00}s   shots {ranged.ShotsFired}";

        /// <summary>
        /// Free must never reach zero and recycled must stay at zero. Both are here rather than in a
        /// log so pool pressure is visible while it is happening, not after the fact.
        /// </summary>
        private string FormatPool()
        {
            var pool = ranged != null ? ranged.Pool : null;
            return pool == null
                ? "Unavailable"
                : $"{pool.ActiveCount} active / {pool.FreeCount} free of {pool.Capacity}   recycled {pool.RecycledUnderPressure}";
        }

        private string FormatMelee() =>
            melee == null
                ? "Unavailable"
                : $"{melee.Phase}   cooldown {melee.CooldownRemaining:0.00}s   hits this swing {melee.HitsThisSwing}";

        private static string Format(Health health) =>
            health == null ? "N/A" : $"{health.CurrentHealth:0.#}/{health.MaximumHealth:0.#}";

        /// <summary>
        /// Turns one body's health events into labelled log lines and guarantees the subscription is
        /// released exactly once, so a reconfigured overlay cannot double-report a hit.
        /// </summary>
        private sealed class HealthLogger
        {
            private readonly string label;
            private Health subscribed;

            public HealthLogger(string label) => this.label = label;

            public void Attach(Health health)
            {
                if (ReferenceEquals(subscribed, health))
                {
                    return;
                }

                Detach();
                if (health == null)
                {
                    return;
                }

                subscribed = health;
                subscribed.Damaged += OnDamaged;
                subscribed.Died += OnDied;
                subscribed.ResetPerformed += OnResetPerformed;
            }

            public void Detach()
            {
                if (subscribed != null)
                {
                    subscribed.Damaged -= OnDamaged;
                    subscribed.Died -= OnDied;
                    subscribed.ResetPerformed -= OnResetPerformed;
                }

                subscribed = null;
            }

            private void OnDamaged(DamageInfo damage, DamageResult result) =>
                CombatEventLog.Record(
                    $"{label} took {result.AppliedDamage:0.#} ({result.CurrentHealth:0.#}/{Maximum():0.#})");

            private void OnDied(DamageInfo damage) => CombatEventLog.Record(label + " died");

            private void OnResetPerformed() => CombatEventLog.Record(label + " reset");

            private float Maximum() => subscribed != null ? subscribed.MaximumHealth : 0f;
        }
    }
}
