using UnityEngine;
using Vaultbreakers.Core;
using Vaultbreakers.Equipment;

namespace Vaultbreakers.Combat
{
    /// <summary>
    /// Directional melee feedback: a tapered cutting arc, a thin trailing echo and swing audio. It only
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
        private const int Segments = 40;
        private const int Bands = 4;
        private Mesh slashMesh;
        private readonly Vector3[] vertices = new Vector3[(Segments + 1) * Bands * 2];
        private readonly Color[] colors = new Color[(Segments + 1) * Bands * 2];
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

            var visible = melee.Phase == MeleePhase.Active ||
                          melee.Phase == MeleePhase.Recovery && melee.PhaseProgress < .65f;
            ShowArc(visible);
            if (arc != null && arc.activeSelf)
            {
                arc.transform.position = transform.position + Vector3.up * arcHeight;
                arc.transform.rotation = Quaternion.LookRotation(melee.SwingDirection, Vector3.up);
                UpdateSlash();
            }
        }

        private void UpdateSlash()
        {
            var recovery = melee.Phase == MeleePhase.Recovery;
            var progress = recovery ? 1f : melee.PhaseProgress;
            var fade = recovery ? 1f - Mathf.Clamp01(melee.PhaseProgress / .65f) : 1f;
            // A moving, open crescent: its leading edge crosses the committed attack direction.
            // The pointed ends and radial alpha falloff avoid a filled hit-volume disc.
            var head = Mathf.Lerp(0f, -72f, progress);
            var span = Mathf.Lerp(100f, 120f, progress);
            var radius = melee.Range * .92f;
            for (var ribbon = 0; ribbon < 2; ribbon++)
                for (var segment = 0; segment <= Segments; segment++)
                {
                    var t = segment / (float)Segments;
                    var angle = (head + span * (1f - t) - ribbon * 9f) * Mathf.Deg2Rad;
                    var taper = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * Mathf.PI)), .7f);
                    var width = (ribbon == 0 ? .42f : .075f) * taper;
                    var edge = radius - ribbon * .30f;
                    for (var band = 0; band < Bands; band++)
                    {
                        var across = band == 0 ? 0f : band == 1 ? .56f : band == 2 ? .87f : 1f;
                        var r = edge - width * (1f - across);
                        var index = ribbon * (Segments + 1) * Bands + segment * Bands + band;
                        vertices[index] = new Vector3(Mathf.Sin(angle) * r,
                            Mathf.Sin(angle) * .18f + ribbon * .06f, Mathf.Cos(angle) * r);
                        var color = band == 2 ? new Color(1f, .94f, .73f) : new Color(1f, .42f, .07f);
                        color.a = band == 0 || band == 3 ? 0f : fade * taper * (ribbon == 0 ? .85f : .45f);
                        colors[index] = color;
                    }
                }
            slashMesh.vertices = vertices;
            slashMesh.colors = colors;
            slashMesh.RecalculateBounds();
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
            if (arc != null && arc.activeSelf != (visible && drawArc))
            {
                arc.SetActive(visible && drawArc);
            }
        }

        private void Play(AudioClip clip, float volume) => PlaceholderAudio.Play(audioSource, clip, volume);

        // Reuse one small mesh across attacks. It is presentation only: no collider or shadow.
        private GameObject CreateArc()
        {
            var slash = new GameObject("MeleeSlash", typeof(MeshFilter), typeof(MeshRenderer));
            slash.layer = GameLayers.Debug;
            slash.transform.SetParent(transform, false);
            slashMesh = new Mesh { name = "Directional melee crescents" };
            slashMesh.MarkDynamic();
            slashMesh.vertices = vertices;
            var triangles = new int[Segments * (Bands - 1) * 6 * 2];
            var next = 0;
            for (var ribbon = 0; ribbon < 2; ribbon++)
                for (var segment = 0; segment < Segments; segment++)
                    for (var band = 0; band < Bands - 1; band++)
                    {
                        var a = ribbon * (Segments + 1) * Bands + segment * Bands + band;
                        var b = a + Bands;
                        triangles[next++] = a; triangles[next++] = b; triangles[next++] = a + 1;
                        triangles[next++] = a + 1; triangles[next++] = b; triangles[next++] = b + 1;
                    }
            slashMesh.triangles = triangles;
            slash.GetComponent<MeshFilter>().sharedMesh = slashMesh;
            var renderer = slash.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = arcMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            slash.SetActive(false);
            return slash;
        }

        private void OnDestroy()
        {
            if (slashMesh != null) Destroy(slashMesh);
        }

        private void SetUpAudio()
        {
            audioSource = PlaceholderAudio.EnsureSource(gameObject);
            swingClip = PlaceholderAudio.CreateBurst("VB_MeleeSwing", 0.16f, 1400f, 0.35f);
            impactClip = PlaceholderAudio.CreateBurst("VB_MeleeImpact", 0.09f, 180f, 0.9f);
        }
    }
}
