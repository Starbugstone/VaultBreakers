using UnityEngine;
using Vaultbreakers.Combat;
using Vaultbreakers.Core;

namespace Vaultbreakers.Gems
{
    [RequireComponent(typeof(Health), typeof(BoxCollider))]
    public sealed class BreakableLoot : MonoBehaviour, IDamageMitigator
    {
        [SerializeField] private LootSource source;
        [SerializeField] private int room;
        [SerializeField] private GameObject intact, broken;
        [SerializeField] private DungeonLoot loot;
        private Health health;
        private BoxCollider body;
        private float hitTime, breakTime = -10;
        private const float DebrisLifetime = 1f;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private sealed class Fragment
        {
            public Renderer renderer;
            public Vector3 position, scale, velocity, spin;
            public Quaternion rotation;
            public Color[] colors;
        }
        private Fragment[] fragments;
        private MaterialPropertyBlock tint;
        public LootSource Source => source;
        public int Room => room;
        public bool IsBroken => health != null && health.IsDead;
        public Health Health => health;

        public void Configure(DungeonLoot owner, LootSource kind, int roomIndex, GameObject whole, GameObject fragments)
        { loot = owner; source = kind; room = roomIndex; intact = whole; broken = fragments; }
        private void Awake()
        {
            health = GetComponent<Health>(); body = GetComponent<BoxCollider>();
            health.SetMitigator(this); health.Damaged += OnHit; health.Died += OnBroken;
            InitializeDebris();
        }
        public bool TryAbsorb(in DamageInfo damage)
            => damage.Source == null || damage.Source.layer != GameLayers.Player;
        public void ResetLoot(bool active)
        {
            if (health == null) { health = GetComponent<Health>(); body = GetComponent<BoxCollider>(); }
            health.Configure(source == LootSource.Chest ? loot.Balance.chestHealth : loot.Balance.crateHealth);
            body.enabled = true; intact.SetActive(true); broken.SetActive(false);
            InitializeDebris(); RestoreDebris();
            intact.transform.localPosition = Vector3.zero;
            hitTime = 0; breakTime = -10; gameObject.SetActive(active);
        }
        private void OnHit(DamageInfo damage, DamageResult result)
        { hitTime = .14f; loot.HitContainer(transform.position, result.Killed); }
        private void OnBroken(DamageInfo damage)
        {
            body.enabled = false; intact.SetActive(false); broken.SetActive(true); breakTime = Time.time;
            AnimateDebris(0);
            Physics.SyncTransforms(); // The burst must see the now-open container volume.
            loot.ContainerBroken(this);
        }
        private void Update()
        {
            if (hitTime > 0 && !IsBroken)
            { hitTime -= Time.deltaTime; intact.transform.localPosition = Vector3.right * (Mathf.Sin(hitTime * 100) * hitTime * .25f); }
            else if (!IsBroken) intact.transform.localPosition = Vector3.zero;
            if (IsBroken && broken.activeSelf) AnimateDebris(Time.time - breakTime);
        }
        private void InitializeDebris()
        {
            if (fragments != null) return;
            var renderers = broken.GetComponentsInChildren<Renderer>(true);
            fragments = new Fragment[renderers.Length]; tint = new MaterialPropertyBlock();
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i]; var piece = renderer.transform;
                var position = transform.InverseTransformPoint(piece.position);
                var radial = new Vector3(position.x, 0, position.z).normalized;
                if (radial.sqrMagnitude < .01f) radial = Quaternion.Euler(0, i * 60, 0) * Vector3.forward;
                var materials = renderer.sharedMaterials; var colors = new Color[materials.Length];
                for (var m = 0; m < colors.Length; m++) colors[m] = materials[m].GetColor(BaseColor);
                fragments[i] = new Fragment { renderer = renderer, position = position,
                    rotation = Quaternion.Inverse(transform.rotation) * piece.rotation, scale = piece.localScale,
                    velocity = radial * (1.3f + i * .13f) + Vector3.up * (2.1f + i % 3 * .28f),
                    spin = new Vector3(90 + i * 19, (i % 2 == 0 ? 1 : -1) * 145, 75 - i * 27), colors = colors };
            }
        }
        private void RestoreDebris()
        {
            foreach (var fragment in fragments)
            {
                var piece = fragment.renderer.transform;
                piece.SetPositionAndRotation(transform.TransformPoint(fragment.position), transform.rotation * fragment.rotation);
                piece.localScale = fragment.scale;
                for (var m = 0; m < fragment.colors.Length; m++) fragment.renderer.SetPropertyBlock(null, m);
            }
        }
        private void AnimateDebris(float age)
        {
            if (age >= DebrisLifetime) { broken.SetActive(false); return; }
            var shrink = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.35f, DebrisLifetime, age));
            var alpha = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.4f, .95f, age));
            foreach (var fragment in fragments)
            {
                // Authored pivots keep split lids, jagged plates and bent rails rotating individually.
                var origin = new Vector3(fragment.position.x * .4f, .6f + fragment.position.y, fragment.position.z * .4f);
                var position = origin + fragment.velocity * age + Vector3.down * (6 * age * age);
                position.y = Mathf.Max(.1f, position.y);
                var piece = fragment.renderer.transform;
                piece.SetPositionAndRotation(transform.TransformPoint(position), transform.rotation * Quaternion.Euler(fragment.spin * age) * fragment.rotation);
                piece.localScale = fragment.scale * shrink;
                for (var m = 0; m < fragment.colors.Length; m++)
                {
                    tint.Clear(); var color = fragment.colors[m]; color.a *= alpha;
                    tint.SetColor(BaseColor, color); fragment.renderer.SetPropertyBlock(tint, m);
                }
            }
        }
        private void OnDestroy()
        { if (health != null) { health.Damaged -= OnHit; health.Died -= OnBroken; } }
    }
}
