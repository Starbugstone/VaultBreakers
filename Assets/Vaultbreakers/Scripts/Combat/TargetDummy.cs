using UnityEngine;
using Vaultbreakers.Core;

namespace Vaultbreakers.Combat
{
    /// <summary>
    /// Immobile practice target: hit flash, impact marker, and automatic recovery after death.
    /// Feedback runs on plain timers and a single reused marker so repeated fire in later phases
    /// cannot allocate per hit or leave overlapping flash timers fighting each other.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class TargetDummy : MonoBehaviour
    {
        private const float FlashDuration = 0.1f;
        private const float MarkerDuration = 0.18f;
        private const float MarkerHeight = 1.25f;
        private const float MarkerScale = 0.15f;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly Color HitColor = new(1f, 0.2f, 0.05f);

        [SerializeField, Min(0f)] private float resetDelay = 1.5f;
        [SerializeField] private Renderer targetRenderer;

        private Health health;
        private MaterialPropertyBlock properties;
        private GameObject impactMarker;
        private Color normalColor = Color.white;
        private float flashTimer;
        private float markerTimer;
        private float resetTimer;

        private void Awake()
        {
            health = GetComponent<Health>();
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }

            properties = new MaterialPropertyBlock();
            if (targetRenderer != null && targetRenderer.sharedMaterial != null &&
                targetRenderer.sharedMaterial.HasProperty(BaseColor))
            {
                normalColor = targetRenderer.sharedMaterial.GetColor(BaseColor);
            }

            impactMarker = CreateImpactMarker();

            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        private void OnDestroy()
        {
            if (health == null)
            {
                return;
            }

            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        private void Update()
        {
            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                if (flashTimer <= 0f)
                {
                    SetTint(normalColor);
                }
            }

            if (markerTimer > 0f)
            {
                markerTimer -= Time.deltaTime;
                if (markerTimer <= 0f && impactMarker != null)
                {
                    impactMarker.SetActive(false);
                }
            }

            if (resetTimer > 0f)
            {
                resetTimer -= Time.deltaTime;
                if (resetTimer <= 0f)
                {
                    ResetDummy();
                }
            }
        }

        /// <summary>Returns the dummy to a clean, alive, untinted state and cancels pending feedback.</summary>
        public void ResetDummy()
        {
            flashTimer = 0f;
            markerTimer = 0f;
            resetTimer = 0f;

            if (impactMarker != null)
            {
                impactMarker.SetActive(false);
            }

            SetTint(normalColor);
            health.ResetHealth();
        }

        private void OnDamaged(DamageInfo damage, DamageResult result)
        {
            SetTint(HitColor);
            flashTimer = FlashDuration;

            if (impactMarker == null)
            {
                return;
            }

            impactMarker.SetActive(true);
            markerTimer = MarkerDuration;
        }

        private void OnDied(DamageInfo damage) => resetTimer = Mathf.Max(0.01f, resetDelay);

        private GameObject CreateImpactMarker()
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "ImpactMarker";
            marker.layer = GameLayers.Debug;
            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = Vector3.up * MarkerHeight;
            marker.transform.localScale = Vector3.one * MarkerScale;

            var collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            var markerRenderer = marker.GetComponent<Renderer>();
            if (markerRenderer != null && targetRenderer != null)
            {
                markerRenderer.sharedMaterial = targetRenderer.sharedMaterial;
            }

            marker.SetActive(false);
            return marker;
        }

        private void SetTint(Color color)
        {
            if (targetRenderer == null)
            {
                return;
            }

            targetRenderer.GetPropertyBlock(properties);
            properties.SetColor(BaseColor, color);
            targetRenderer.SetPropertyBlock(properties);
        }
    }
}
