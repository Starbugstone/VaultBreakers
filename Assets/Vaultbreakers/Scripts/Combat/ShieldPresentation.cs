using UnityEngine;
using Vaultbreakers.Core;

namespace Vaultbreakers.Combat
{
    /// <summary>
    /// Shield feedback for the Phase 7 exit gate, which requires state to be readable by shape and
    /// behaviour rather than by colour alone. So:
    ///
    /// <list type="bullet">
    /// <item>the band always spans the true arc, and never lies about coverage;</item>
    /// <item>stability drives its <em>height</em>, so a failing guard is visibly thinner;</item>
    /// <item>a block makes it pulse outward;</item>
    /// <item>a break replaces it with a burst, and the band is simply gone;</item>
    /// <item>a ring at the player's feet fills across the lockout, so the wait has one clear end.</item>
    /// </list>
    ///
    /// Nothing here is read by the controller; deleting this component changes no blocking rule.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ShieldController))]
    public sealed class ShieldPresentation : MonoBehaviour
    {
        private const int ArcSegments = 24;
        private const float ArcRadius = 1.15f;
        private const float ArcHeight = 1.5f;
        private const float ArcBaseHeight = 0.25f;
        private const float BlockPulseDuration = 0.16f;
        private const float BreakBurstDuration = 0.35f;
        private const float FlickerThreshold = 0.3f;
        private const float FlickerHz = 12f;

        [SerializeField] private ShieldController shield;
        [SerializeField] private Material arcMaterial;
        [SerializeField] private Material markerMaterial;
        [SerializeField, Min(0f)] private float originHeight = 0.15f;

        private Transform pivot;
        private Transform band;
        private Mesh bandMesh;
        private GameObject directionPip;
        private GameObject breakBurst;
        private GameObject recoveryRing;
        private AudioSource audioSource;
        private AudioClip blockClip;
        private AudioClip breakClip;
        private float pulseTimer;
        private float burstTimer;

        private void Awake()
        {
            if (shield == null)
            {
                shield = GetComponent<ShieldController>();
            }

            audioSource = PlaceholderAudio.EnsureSource(gameObject);
            blockClip = PlaceholderAudio.CreateBurst("VB_ShieldBlock", 0.1f, 520f, 0.7f);
            breakClip = PlaceholderAudio.CreateBurst("VB_ShieldBreak", 0.3f, 140f, 0.5f);
        }

        /// <summary>
        /// The band is built here rather than in Awake because its width comes from the controller's
        /// arc, and that arc is only final once the controller's own Awake has applied the balance
        /// asset. Building a frame earlier would silently draw an arc of the wrong width.
        /// </summary>
        private void Start() => BuildVisuals();

        private void OnEnable()
        {
            if (shield == null)
            {
                return;
            }

            shield.Blocked += OnBlocked;
            shield.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            if (shield != null)
            {
                shield.Blocked -= OnBlocked;
                shield.StateChanged -= OnStateChanged;
            }

            Show(pivot, false);
            Show(recoveryRing, false);
            Show(breakBurst, false);
        }

        private void LateUpdate()
        {
            if (shield == null || pivot == null)
            {
                return;
            }

            var deltaTime = Time.deltaTime;
            pulseTimer = Mathf.Max(0f, pulseTimer - deltaTime);
            burstTimer = Mathf.Max(0f, burstTimer - deltaTime);

            PoseBand();
            PoseRecoveryRing();

            if (burstTimer <= 0f)
            {
                Show(breakBurst, false);
            }
            else if (breakBurst != null)
            {
                var growth = 1f - burstTimer / BreakBurstDuration;
                breakBurst.transform.position = transform.position + Vector3.up * originHeight;
                breakBurst.transform.localScale = Vector3.one * Mathf.Lerp(0.4f, 3.2f, growth);
            }
        }

        public void Configure(ShieldController controller, Material arc, Material marker, float height)
        {
            shield = controller;
            arcMaterial = arc;
            markerMaterial = marker;
            originHeight = height;
        }

        /// <summary>
        /// The band spans the arc exactly and only ever changes height, because an arc that narrowed
        /// as stability fell would be telling the player they are covered where they are not.
        /// </summary>
        private void PoseBand()
        {
            var raised = shield.IsRaised;
            Show(pivot, raised);
            if (!raised)
            {
                return;
            }

            pivot.SetPositionAndRotation(
                transform.position + Vector3.up * originHeight,
                Quaternion.LookRotation(shield.ShieldFacing, Vector3.up));

            var pulse = pulseTimer > 0f ? 1f + 0.35f * (pulseTimer / BlockPulseDuration) : 1f;
            var height = Mathf.Lerp(ArcBaseHeight, 1f, shield.StabilityFraction);

            // Below a third, the band stutters. Behaviour, so it survives a grayscale pass.
            if (shield.StabilityFraction < FlickerThreshold &&
                Mathf.Sin(Time.unscaledTime * FlickerHz * Mathf.PI * 2f) < 0f)
            {
                height *= 0.55f;
            }

            band.localScale = new Vector3(pulse, height, pulse);
        }

        /// <summary>
        /// One ring, one meaning: how long until the shield is usable again. It only exists while the
        /// shield is broken, so a full ring is the moment the player gets their guard back.
        /// </summary>
        private void PoseRecoveryRing()
        {
            var broken = shield.IsUnavailable;
            Show(recoveryRing, broken);
            if (!broken || recoveryRing == null)
            {
                return;
            }

            var diameter = Mathf.Lerp(0.2f, 2.6f, shield.RecoveryProgress);
            recoveryRing.transform.position = transform.position + Vector3.up * 0.03f;
            recoveryRing.transform.localScale = new Vector3(diameter, 0.02f, diameter);
        }

        private void OnBlocked(DamageInfo damage, float stabilityRemaining)
        {
            pulseTimer = BlockPulseDuration;
            PlaceholderAudio.Play(audioSource, blockClip, 0.45f);
        }

        private void OnStateChanged(ShieldState state)
        {
            if (state != ShieldState.Broken)
            {
                return;
            }

            burstTimer = BreakBurstDuration;
            Show(breakBurst, true);
            PlaceholderAudio.Play(audioSource, breakClip, 0.7f);
        }

        private void BuildVisuals()
        {
            pivot = new GameObject("ShieldArc").transform;
            pivot.SetParent(transform, false);

            var bandObject = new GameObject("Band", typeof(MeshFilter), typeof(MeshRenderer));
            bandObject.layer = GameLayers.Debug;
            band = bandObject.transform;
            band.SetParent(pivot, false);
            bandMesh = BuildArcBand(shield != null ? shield.ArcDegrees : 120f, ArcRadius, ArcHeight);
            bandObject.GetComponent<MeshFilter>().sharedMesh = bandMesh;
            if (arcMaterial != null)
            {
                bandObject.GetComponent<MeshRenderer>().sharedMaterial = arcMaterial;
            }

            // A pip on the centre line, so shield facing is legible even when the band is thin.
            directionPip = CreatePrimitive("DirectionPip", PrimitiveType.Sphere, markerMaterial);
            directionPip.transform.SetParent(pivot, false);
            directionPip.transform.localPosition = new Vector3(0f, 0f, ArcRadius + 0.12f);
            directionPip.transform.localScale = Vector3.one * 0.16f;
            directionPip.SetActive(true);

            breakBurst = CreatePrimitive("ShieldBreakBurst", PrimitiveType.Sphere, markerMaterial);
            recoveryRing = CreatePrimitive("ShieldRecoveryRing", PrimitiveType.Cylinder, markerMaterial);

            Show(pivot, false);
        }

        /// <summary>
        /// A curved vertical band spanning the arc, built double sided so it stays visible from the
        /// fixed camera whichever way the player turns. Generated rather than authored so it can never
        /// drift out of step with the arc the controller actually blocks.
        /// </summary>
        private static Mesh BuildArcBand(float arcDegrees, float radius, float height)
        {
            var frontCount = (ArcSegments + 1) * 2;
            // Separate vertices prevent opposite faces from cancelling their lighting normals.
            var vertices = new Vector3[frontCount * 2];
            var half = arcDegrees * 0.5f;

            for (var index = 0; index <= ArcSegments; index++)
            {
                var angle = Mathf.Lerp(-half, half, index / (float)ArcSegments) * Mathf.Deg2Rad;
                var offset = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;
                vertices[index * 2] = offset;
                vertices[index * 2 + 1] = offset + Vector3.up * height;
            }

            System.Array.Copy(vertices, 0, vertices, frontCount, frontCount);
            var triangles = new int[ArcSegments * 12];
            for (var index = 0; index < ArcSegments; index++)
            {
                var v = index * 2;
                var t = index * 12;

                triangles[t] = v;
                triangles[t + 1] = v + 1;
                triangles[t + 2] = v + 2;
                triangles[t + 3] = v + 2;
                triangles[t + 4] = v + 1;
                triangles[t + 5] = v + 3;

                // Reversed winding for the inside face.
                triangles[t + 6] = frontCount + v + 2;
                triangles[t + 7] = frontCount + v + 1;
                triangles[t + 8] = frontCount + v;
                triangles[t + 9] = frontCount + v + 3;
                triangles[t + 10] = frontCount + v + 1;
                triangles[t + 11] = frontCount + v + 2;
            }

            var mesh = new Mesh { name = "ShieldArcBand" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static GameObject CreatePrimitive(string primitiveName, PrimitiveType type, Material material)
        {
            var created = GameObject.CreatePrimitive(type);
            created.name = primitiveName;
            created.layer = GameLayers.Debug;

            var collider = created.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            if (material != null)
            {
                created.GetComponent<Renderer>().sharedMaterial = material;
            }

            created.SetActive(false);
            return created;
        }

        /// <summary>
        /// The band mesh is generated at runtime, so nothing else will ever clean it up, and the two
        /// world-space markers are deliberately unparented and would outlive this component.
        /// </summary>
        private void OnDestroy()
        {
            DestroyDetached(breakBurst);
            DestroyDetached(recoveryRing);

            if (bandMesh != null)
            {
                Destroy(bandMesh);
            }
        }

        private static void DestroyDetached(GameObject target)
        {
            if (target != null)
            {
                Destroy(target);
            }
        }

        private static void Show(Transform target, bool visible)
        {
            if (target != null && target.gameObject.activeSelf != visible)
            {
                target.gameObject.SetActive(visible);
            }
        }

        private static void Show(GameObject target, bool visible)
        {
            if (target != null && target.activeSelf != visible)
            {
                target.SetActive(visible);
            }
        }
    }
}
