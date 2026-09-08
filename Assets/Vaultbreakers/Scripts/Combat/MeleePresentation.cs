using UnityEngine;
using Vaultbreakers.Core;
using Vaultbreakers.Equipment;

namespace Vaultbreakers.Combat
{
    /// <summary>
    /// Placeholder feedback for the melee slice: a procedural swing pose on the equipped weapon, an
    /// arc that shows the volume the swing actually queried, and generated placeholder audio. It only
    /// ever reads <see cref="MeleeController"/>; presentation never owns or delays combat timing, so
    /// removing this component changes nothing about what a swing hits.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeleeController))]
    public sealed class MeleePresentation : MonoBehaviour
    {
        private const float WindUpAngle = 65f;
        private const float FollowThroughAngle = -80f;

        [SerializeField] private MeleeController melee;
        [SerializeField] private AvatarSocketRegistry sockets;
        [SerializeField] private Material arcMaterial;
        [SerializeField, Min(0f)] private float arcHeight = 1f;

        [SerializeField] private bool drawArc=true;
        public void SetArcVisible(bool visible)=>drawArc=visible;
        private Transform weaponSocket;
        private Quaternion weaponRest = Quaternion.identity;
        private GameObject arc;
        private AudioSource audioSource;
        private AudioClip swingClip;
        private AudioClip impactClip;

        private void Awake()
        {
            if (melee == null)
            {
                melee = GetComponent<MeleeController>();
            }

            if (sockets == null)
            {
                sockets = GetComponent<AvatarSocketRegistry>();
            }

            if (sockets != null && sockets.TryGet(AvatarSocketId.RightHandMelee, out var socket))
            {
                weaponSocket = socket;
                weaponRest = socket.localRotation;
            }

            arc = CreateArc();
            SetUpAudio();
        }

        private void OnEnable()
        {
            if (melee == null)
            {
                return;
            }

            melee.SwingStarted += OnSwingStarted;
            melee.PhaseChanged += OnPhaseChanged;
            melee.Hit += OnHit;
        }

        private void OnDisable()
        {
            if (melee != null)
            {
                melee.SwingStarted -= OnSwingStarted;
                melee.PhaseChanged -= OnPhaseChanged;
                melee.Hit -= OnHit;
            }

            ShowArc(false);
            ResetPose();
        }

        private void LateUpdate()
        {
            if (melee == null)
            {
                return;
            }

            PoseWeapon(melee.Phase, melee.PhaseProgress);

            if (arc != null && arc.activeSelf)
            {
                var origin = transform.position + Vector3.up * arcHeight;
                arc.transform.position = MeleeSwing.QueryCentre(
                    origin, melee.SwingDirection, melee.Range, melee.Radius);
                var diameter = melee.Radius * 2f;
                arc.transform.localScale = new Vector3(diameter, 0.02f, diameter);
            }
        }

        public void Configure(
            MeleeController controller,
            AvatarSocketRegistry registry,
            Material swingArcMaterial,
            float height)
        {
            melee = controller;
            sockets = registry;
            arcMaterial = swingArcMaterial;
            arcHeight = height;
        }

        /// <summary>
        /// Wind up through startup, sweep across the active window, settle back through recovery.
        /// Driven entirely by the controller's phase progress so the visual can never disagree with
        /// the frame the damage landed on.
        /// </summary>
        private void PoseWeapon(MeleePhase phase, float progress)
        {
            if (weaponSocket == null)
            {
                return;
            }

            var angle = phase switch
            {
                MeleePhase.Startup => Mathf.Lerp(0f, WindUpAngle, progress),
                MeleePhase.Active => Mathf.Lerp(WindUpAngle, FollowThroughAngle, progress),
                MeleePhase.Recovery => Mathf.Lerp(FollowThroughAngle, 0f, progress),
                _ => 0f
            };

            weaponSocket.localRotation = weaponRest * Quaternion.Euler(0f, angle, 0f);
        }

        private void ResetPose()
        {
            if (weaponSocket != null)
            {
                weaponSocket.localRotation = weaponRest;
            }
        }

        private void OnSwingStarted(Vector3 direction) => Play(swingClip, 0.35f);

        private void OnPhaseChanged(MeleePhase phase) => ShowArc(phase == MeleePhase.Active);

        private void OnHit(Health target, DamageResult result) => Play(impactClip, 0.6f);

        private void ShowArc(bool visible)
        {
            if (arc != null && arc.activeSelf != visible)
            {
                arc.SetActive(visible && drawArc);
            }
        }

        private void Play(AudioClip clip, float volume) => PlaceholderAudio.Play(audioSource, clip, volume);

        /// <summary>
        /// One reused disc, created here rather than per swing so a sustained fight never allocates.
        /// It matches the query volume exactly, which is what makes a miss readable.
        /// </summary>
        private GameObject CreateArc()
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "MeleeArc";
            disc.layer = GameLayers.Debug;
            disc.transform.SetParent(transform, true);

            var collider = disc.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            if (arcMaterial != null)
            {
                disc.GetComponent<Renderer>().sharedMaterial = arcMaterial;
            }

            disc.SetActive(false);
            return disc;
        }

        private void SetUpAudio()
        {
            audioSource = PlaceholderAudio.EnsureSource(gameObject);
            swingClip = PlaceholderAudio.CreateBurst("VB_MeleeSwing", 0.16f, 1400f, 0.35f);
            impactClip = PlaceholderAudio.CreateBurst("VB_MeleeImpact", 0.09f, 180f, 0.9f);
        }
    }
}
