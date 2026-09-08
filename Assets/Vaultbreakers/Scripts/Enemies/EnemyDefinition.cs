using UnityEngine;

namespace Vaultbreakers.Enemies
{
    public enum EnemyRole { Grunt, Shooter, Bruiser }
    [CreateAssetMenu(menuName = "Vaultbreakers/Enemy")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        public EnemyRole role;
        public string displayName = "Scrap Grunt";
        [Min(1)] public float health = 30;
        [Min(0)] public float speed = 3;
        [Min(0.1f)] public float range = 1.7f;
        [Min(0.1f)] public float windup = 0.65f;
        [Min(0.01f)] public float active = 0.12f;
        [Min(0.1f)] public float recovery = 0.8f;
        [Min(0)] public float damage = 5;
        [Min(0)] public float stabilityDamage = 10;
        [Range(1, 360)] public float arc = 85;
        [Min(0)] public float projectileSpeed = 9;
        [Min(1)] public float preferredRange = 6;
        public Color accent = new(1f, 0.55f, 0.18f);
    }
}
