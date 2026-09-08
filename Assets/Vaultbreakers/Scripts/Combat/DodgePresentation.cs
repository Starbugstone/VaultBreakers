using UnityEngine;
using Vaultbreakers.Core;

namespace Vaultbreakers.Combat
{
    /// <summary>
    /// Placeholder feedback for the dodge: a squash-and-stretch pose on the avatar, a ground streak
    /// laid down along the path the burst actually took, and a generated whoosh. It only ever reads
    /// <see cref="DodgeController"/>, so removing this component changes no distance, no timing, and
    /// no invulnerability window.
    ///
    /// The streak is drawn from the recorded start position to where the player is now rather than
    /// from the authored distance, so a dodge stopped by a wall leaves a short streak and tells the
    /// truth about how far it got.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DodgeController))]
    public sealed class DodgePresentation : MonoBehaviour
    {
        private const float StretchAmount = 0.28f;
        private const float SquashAmount = 0.34f;
        private const float StreakFadeDuration = 0.22f;
        private const float StreakWidth = 0.55f;
        private const float StreakHeight = 0.02f;

        [SerializeField] private DodgeController dodge;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Material streakMaterial;
        [SerializeField, Min(0f)] private float streakHeight = 0.04f;

        private GameObject streak;
        private Vector3 streakOrigin;
        private Vector3 restScale = Vector3.one;
        private AudioSource audioSource;
        private AudioClip dodgeClip;
        private float streakTimer;

        private void Awake()
        {
            if (dodge == null)
            {
                dodge = GetComponent<DodgeController>();
            }

            if (visualRoot != null)
            {
                restScale = visualRoot.localScale;
            }

            audioSource = PlaceholderAudio.EnsureSource(gameObject);
            dodgeClip = PlaceholderAudio.CreateBurst("VB_Dodge", 0.18f, 280f, 0.45f);
            streak = CreateStreak();
        }

        private void OnEnable()
        {
            if (dodge == null)
            {
                return;
            }

            dodge.DodgeStarted += OnDodgeStarted;
            dodge.DodgeEnded += OnDodgeEnded;
        }

        private void OnDisable()
        {
            if (dodge != null)
            {
                dodge.DodgeStarted -= OnDodgeStarted;
                dodge.DodgeEnded -= OnDodgeEnded;
            }

            Show(streak, false);
            ResetPose();
        }

        /// <summary>
        /// The streak object is deliberately unparented so it stays where the dodge happened instead
        /// of riding the player, which means nothing else will ever clean it up.
        /// </summary>
        private void OnDestroy()
        {
            if (streak != null)
            {
                Destroy(streak);
            }
        }

        private void LateUpdate()
        {
            if (dodge == null)
            {
                return;
            }

            PosePlayer();
            PoseStreak();
        }

        public void Configure(DodgeController controller, Transform model, Material streak, float height)
        {
            dodge = controller;
            visualRoot = model;
            streakMaterial = streak;
            streakHeight = height;

            if (visualRoot != null)
            {
                restScale = visualRoot.localScale;
            }
        }

        /// <summary>
        /// Squash on the way out and back to rest by the end, so the pose reads as a roll rather than
        /// as a permanent change of shape. Deliberately not aligned to the dodge direction: the model
        /// turns to combat facing, which during a dodge is frequently not where the player is going.
        /// </summary>
        private void PosePlayer()
        {
            if (visualRoot == null)
            {
                return;
            }

            if (!dodge.IsDodging)
            {
                ResetPose();
                return;
            }

            // Peaks in the middle of the burst and returns to zero at both ends, so no frame is
            // needed to unwind the pose afterwards.
            var amount = Mathf.Sin(dodge.Progress * Mathf.PI);
            visualRoot.localScale = new Vector3(
                restScale.x * (1f + StretchAmount * amount),
                restScale.y * (1f - SquashAmount * amount),
                restScale.z * (1f + StretchAmount * amount));
        }

        private void PoseStreak()
        {
            if (streak == null)
            {
                return;
            }

            if (dodge.IsDodging)
            {
                streakTimer = StreakFadeDuration;
                StretchStreakTo(transform.position);
                return;
            }

            streakTimer = Mathf.Max(0f, streakTimer - Time.deltaTime);
            Show(streak, streakTimer > 0f);
        }

        /// <summary>Spans the ground between where the dodge began and where the player is now.</summary>
        private void StretchStreakTo(Vector3 current)
        {
            var span = current - streakOrigin;
            span.y = 0f;
            var length = span.magnitude;

            if (length <= 0.01f)
            {
                Show(streak, false);
                return;
            }

            Show(streak, true);
            streak.transform.position = streakOrigin + span * 0.5f + Vector3.up * streakHeight;
            streak.transform.rotation = Quaternion.LookRotation(span.normalized, Vector3.up);
            streak.transform.localScale = new Vector3(StreakWidth, StreakHeight, length);
        }

        private void OnDodgeStarted(Vector3 direction)
        {
            streakOrigin = transform.position;
            streakTimer = StreakFadeDuration;
            PlaceholderAudio.Play(audioSource, dodgeClip, 0.5f);
        }

        private void OnDodgeEnded() => ResetPose();

        private void ResetPose()
        {
            if (visualRoot != null)
            {
                visualRoot.localScale = restScale;
            }
        }

        private GameObject CreateStreak()
        {
            var created = GameObject.CreatePrimitive(PrimitiveType.Cube);
            created.name = "DodgeStreak";
            created.layer = GameLayers.Debug;

            var collider = created.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            if (streakMaterial != null)
            {
                created.GetComponent<Renderer>().sharedMaterial = streakMaterial;
            }

            created.SetActive(false);
            return created;
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
