using UnityEngine;
using Vaultbreakers.Core;
using Vaultbreakers.Combat;

namespace Vaultbreakers.Enemies
{
    // Ground geometry represents the committed attack area, never a decorative approximation.
    [RequireComponent(typeof(EnemyBrain))]
    public sealed class EnemyPresentation : MonoBehaviour
    {
        [SerializeField] private Material tellTemplate;
        public void Configure(Material material) => tellTemplate=material;
        private EnemyBrain brain;
        private LineRenderer tell;
        private Transform model;
        private Vector3 restScale;
        private float deathTime;
        private AudioSource audioSource;
        private AudioClip warning;
        private EnemyState previous;

        private void Start()
        {
            brain = GetComponent<EnemyBrain>();
            audioSource=PlaceholderAudio.EnsureSource(gameObject);
            warning=PlaceholderAudio.CreateBurst("EnemyTell_"+brain.Definition.role,
                brain.Definition.role==EnemyRole.Bruiser?.38f:.12f,
                brain.Definition.role==EnemyRole.Bruiser?110:brain.Definition.role==EnemyRole.Shooter?1100:220,.7f);
            model = transform.Find("ModelRoot"); if (model != null) restScale = model.localScale;
            var host = new GameObject("AttackTell") { layer = GameLayers.Debug };
            host.transform.SetParent(transform, false);
            tell = host.AddComponent<LineRenderer>(); tell.useWorldSpace = true;
            tell.sharedMaterial = new Material(tellTemplate);
            tell.widthMultiplier = 0.055f; tell.positionCount = 26; tell.enabled = false;
        }
        private void OnEnable() { previous=EnemyState.Approach; deathTime = 0; if (model != null) model.localScale = restScale; }
        private void LateUpdate()
        {
            if (brain == null || brain.Definition == null) return;
            var data = brain.Definition; var state = brain.Attack.State;
            if(state==EnemyState.Windup && previous!=state && Time.timeScale>0)
                PlaceholderAudio.Play(audioSource,warning,brain.Definition.role==EnemyRole.Bruiser?.28f:.12f);
            previous=state;
            tell.enabled = state is EnemyState.Windup or EnemyState.Active;
            if (tell.enabled)
            {
                var origin = transform.position + Vector3.up * 0.055f;
                var radius = data.role == EnemyRole.Shooter ? 10 : data.range;
                var arc = data.role == EnemyRole.Shooter ? 3 : data.arc;
                tell.SetPosition(0, origin);
                for (var i = 1; i < 25; i++)
                    tell.SetPosition(i, origin + Quaternion.Euler(0, Mathf.Lerp(-arc / 2, arc / 2, (i - 1) / 23f), 0) * brain.AttackFacing * radius);
                tell.SetPosition(25, origin);
                tell.sharedMaterial.color = state == EnemyState.Active ? Color.white : data.accent;
                tell.widthMultiplier = state == EnemyState.Active ? 0.13f : Mathf.Lerp(0.045f, 0.1f, 1 - brain.Attack.Remaining / data.windup);
            }
            if (model == null) return;
            if (state == EnemyState.Dead)
            {
                deathTime += Time.deltaTime;
                model.localScale = restScale * Mathf.Max(0, 1 - deathTime / 0.35f);
                if (deathTime >= 0.4f) gameObject.SetActive(false);
            }
        }
        private void OnDestroy() { Destroy(warning); if (tell != null) Destroy(tell.sharedMaterial); }
    }
}
