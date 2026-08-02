using UnityEngine;
using Vaultbreakers.Core;
using Vaultbreakers.Equipment;

namespace Vaultbreakers.Combat
{
    /// <summary>
    /// Placeholder feedback for ranged fire: a muzzle flash on the Muzzle anchor, a recoil kick on the
    /// weapon arm, a short mark at each impact, and generated audio. Everything here is downstream of
    /// <see cref="RangedController"/> and <see cref="ProjectilePool"/>; removing this component changes
    /// cadence, aim, and damage not at all.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RangedController))]
    public sealed class RangedPresentation : MonoBehaviour
    {
        private const float FlashDuration = 0.05f;
        private const float RecoilDuration = 0.12f;
        private const float RecoilAngle = -18f;
        private const float ImpactDuration = 0.1f;

        /// <summary>
        /// Four marks in rotation. One would be cut short whenever two projectiles land close
        /// together, which is exactly what happens when the player sweeps fire across two targets.
        /// </summary>
        private const int ImpactMarkCount = 4;

        [SerializeField] private RangedController ranged;
        [SerializeField] private AvatarSocketRegistry sockets;
        [SerializeField] private Material effectMaterial;
        [SerializeField, Min(0f)] private float flashScale = 0.22f;
        [SerializeField, Min(0f)] private float impactScale = 0.28f;

        private readonly GameObject[] impactMarks = new GameObject[ImpactMarkCount];
        private readonly float[] impactTimers = new float[ImpactMarkCount];

        private Transform muzzle;
        private Transform weaponArm;
        private Quaternion weaponArmRest = Quaternion.identity;
        private GameObject flash;
        private AudioSource audioSource;
        private AudioClip fireClip;
        private AudioClip impactClip;
        private float flashTimer;
        private float recoilTimer;
        private int nextImpactMark;

        private void Awake()
        {
            if (ranged == null)
            {
                ranged = GetComponent<RangedController>();
            }

            if (sockets == null)
            {
                sockets = GetComponent<AvatarSocketRegistry>();
            }

            if (sockets != null)
            {
                sockets.TryGet(AvatarSocketId.Muzzle, out muzzle);
                if (sockets.TryGet(AvatarSocketId.LeftArmRangedShield, out weaponArm))
                {
                    weaponArmRest = weaponArm.localRotation;
                }
            }

            flash = CreateEffect("MuzzleFlash", flashScale);
            for (var index = 0; index < impactMarks.Length; index++)
            {
                impactMarks[index] = CreateEffect("ImpactMark_" + index, impactScale);
            }

            audioSource = PlaceholderAudio.EnsureSource(gameObject);
            fireClip = PlaceholderAudio.CreateBurst("VB_RangedFire", 0.11f, 900f, 0.4f);
            impactClip = PlaceholderAudio.CreateBurst("VB_RangedImpact", 0.07f, 260f, 0.8f);
        }

        private void OnEnable()
        {
            if (ranged != null)
            {
                ranged.Fired += OnFired;
                if (ranged.Pool != null)
                {
                    ranged.Pool.Impacted += OnImpacted;
                }
            }
        }

        private void OnDisable()
        {
            if (ranged != null)
            {
                ranged.Fired -= OnFired;
                if (ranged.Pool != null)
                {
                    ranged.Pool.Impacted -= OnImpacted;
                }
            }

            HideAll();
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;

            if (flashTimer > 0f)
            {
                flashTimer -= deltaTime;
                if (flashTimer <= 0f)
                {
                    Show(flash, false);
                }
            }

            for (var index = 0; index < impactMarks.Length; index++)
            {
                if (impactTimers[index] <= 0f)
                {
                    continue;
                }

                impactTimers[index] -= deltaTime;
                if (impactTimers[index] <= 0f)
                {
                    Show(impactMarks[index], false);
                }
            }

            if (recoilTimer > 0f)
            {
                recoilTimer -= deltaTime;
            }

            PoseWeaponArm();
        }

        private void LateUpdate()
        {
            if (flash != null && flash.activeSelf && muzzle != null)
            {
                flash.transform.position = muzzle.position;
            }
        }

        public void Configure(
            RangedController controller,
            AvatarSocketRegistry registry,
            Material material,
            float muzzleFlashScale,
            float impactMarkScale)
        {
            ranged = controller;
            sockets = registry;
            effectMaterial = material;
            flashScale = muzzleFlashScale;
            impactScale = impactMarkScale;
        }

        /// <summary>Recoil settles back to rest over its own short window; it never gates a shot.</summary>
        private void PoseWeaponArm()
        {
            if (weaponArm == null)
            {
                return;
            }

            var progress = recoilTimer > 0f ? recoilTimer / RecoilDuration : 0f;
            weaponArm.localRotation = weaponArmRest * Quaternion.Euler(RecoilAngle * progress, 0f, 0f);
        }

        private void OnFired(Vector3 origin, Vector3 direction)
        {
            if (flash != null)
            {
                flash.transform.position = origin;
                Show(flash, true);
                flashTimer = FlashDuration;
            }

            recoilTimer = RecoilDuration;
            PlaceholderAudio.Play(audioSource, fireClip, 0.3f);
        }

        private void OnImpacted(Vector3 point, Vector3 normal)
        {
            var mark = impactMarks[nextImpactMark];
            if (mark != null)
            {
                mark.transform.position = point;
                Show(mark, true);
                impactTimers[nextImpactMark] = ImpactDuration;
            }

            nextImpactMark = (nextImpactMark + 1) % impactMarks.Length;
            PlaceholderAudio.Play(audioSource, impactClip, 0.4f);
        }

        /// <summary>
        /// Effects are created once and reused. They are unparented so a moving player cannot drag an
        /// impact mark away from the wall it landed on.
        /// </summary>
        private GameObject CreateEffect(string effectName, float scale)
        {
            var effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            effect.name = effectName;
            effect.layer = GameLayers.Debug;
            effect.transform.localScale = Vector3.one * scale;

            var collider = effect.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            if (effectMaterial != null)
            {
                effect.GetComponent<Renderer>().sharedMaterial = effectMaterial;
            }

            effect.SetActive(false);
            return effect;
        }

        private void OnDestroy()
        {
            DestroyEffect(flash);
            for (var index = 0; index < impactMarks.Length; index++)
            {
                DestroyEffect(impactMarks[index]);
            }
        }

        private static void DestroyEffect(GameObject effect)
        {
            if (effect != null)
            {
                Destroy(effect);
            }
        }

        private void HideAll()
        {
            Show(flash, false);
            for (var index = 0; index < impactMarks.Length; index++)
            {
                Show(impactMarks[index], false);
                impactTimers[index] = 0f;
            }

            flashTimer = 0f;
            recoilTimer = 0f;
            PoseWeaponArm();
        }

        private static void Show(GameObject effect, bool visible)
        {
            if (effect != null && effect.activeSelf != visible)
            {
                effect.SetActive(visible);
            }
        }
    }
}
