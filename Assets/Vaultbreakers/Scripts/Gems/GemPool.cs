using System;
using UnityEngine;
using UnityEngine.Rendering;
using Vaultbreakers.Core;

namespace Vaultbreakers.Gems
{
    /// <summary>One update drives a fixed pool. Gems never carry colliders, rigidbodies or lights.</summary>
    public sealed class GemPool : MonoBehaviour
    {
        private sealed class Slot
        {
            public Transform visual;
            public MeshFilter filter;
            public MeshRenderer renderer;
            public bool active, attracted;
            public GemKind kind;
            public int value;
            public float age, remaining, speed;
            public Vector3 position, velocity;
        }
        [SerializeField] private GemBalance balance;
        [SerializeField] private Mesh[] meshes;
        [SerializeField] private Material[] materials;
        private Slot[] slots;
        private int serial;
        private Vector3 roomOrigin;
        public int ActiveCount { get; private set; }
        public int SpawnedUnits { get; private set; }
        public int CollectedUnits { get; private set; }
        public int ExpiredUnits { get; private set; }
        public int MergedUnits { get; private set; }
        public int PeakActive { get; private set; }
        public int ActiveValue
        {
            get { var total = 0; if (slots != null) foreach (var slot in slots) if (slot.active) total += slot.value; return total; }
        }
        public event Action<GemKind, int, Vector3> Collected;
        public GemBalance Balance => balance;
        public void Configure(GemBalance tuning, Mesh[] shapes, Material[] palette)
        { balance = tuning; meshes = shapes; materials = palette; }

        public void Initialize()
        {
            if (slots != null) return;
            slots = new Slot[Mathf.Max(8, balance.capacity)];
            for (var i = 0; i < slots.Length; i++)
            {
                var go = new GameObject("Pooled gem " + i) { layer = GameLayers.Gem };
                go.transform.SetParent(transform, false);
                var slot = slots[i] = new Slot { visual = go.transform, filter = go.AddComponent<MeshFilter>(), renderer = go.AddComponent<MeshRenderer>() };
                slot.renderer.shadowCastingMode = ShadowCastingMode.Off;
                slot.renderer.receiveShadows = false;
                slot.renderer.lightProbeUsage = LightProbeUsage.Off;
                slot.renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                go.SetActive(false);
            }
        }

        public void SetRoom(Vector3 origin) { Clear(); roomOrigin = origin; }
        public void Drop(Vector3 origin, LootSource source)
        {
            for (var i = 0; i < balance.DropCount(source); i++) Spawn(origin, GemRules.KindFor(source, i), 1);
        }
        public void Spawn(Vector3 origin, GemKind kind, int value)
        {
            if (value <= 0) return;
            Initialize();
            Slot free = null, merge = null;
            var nearest = float.MaxValue;
            foreach (var slot in slots)
            {
                if (!slot.active) { free ??= slot; continue; }
                var distance = (slot.position - origin).sqrMagnitude;
                if (slot.kind == kind && distance < nearest) { nearest = distance; merge = slot; }
            }
            SpawnedUnits += value;
            if (GemRules.ShouldMerge(ActiveCount, slots.Length, merge != null) || free == null)
            {
                // Reserving slots above guarantees a matching kind exists when the pool is full.
                if (merge == null) throw new InvalidOperationException("Gem pool lost its reserved type slot.");
                merge.value += value; merge.remaining = balance.lifetime;
                merge.visual.localScale = Vector3.one * Mathf.Min(1.5f, 1f + .1f * (merge.value - 1));
                MergedUnits += value; return;
            }
            var angle = ++serial * 2.399963f;
            var direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            var position = ClampToRoom(origin); position.y = .45f;
            // Avoid depositing a pickup inside a solid prop or gate after a close-quarters kill.
            for (var i = 0; i < 16 && Physics.CheckSphere(position, .18f, GameLayers.Blocking, QueryTriggerInteraction.Ignore); i++)
            {
                var a = angle + i * 2.399963f;
                position = ClampToRoom(origin + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (.5f + i * .14f));
                position.y = .45f;
            }
            free.active = true; free.attracted = false; free.kind = kind; free.value = value;
            free.age = 0; free.remaining = balance.lifetime; free.speed = balance.pullSpeed;
            free.position = position; free.velocity = direction * (1.8f + serial % 4 * .25f) + Vector3.up * 2.5f;
            free.filter.sharedMesh = meshes[(int)kind]; free.renderer.sharedMaterial = materials[(int)kind];
            free.renderer.enabled = true; free.visual.localScale = Vector3.one;
            free.visual.SetPositionAndRotation(position, Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0));
            free.visual.gameObject.SetActive(true);
            ActiveCount++; PeakActive = Mathf.Max(PeakActive, ActiveCount);
        }

        public void Tick(float delta, Vector3 playerPosition, bool canCollect)
        {
            if (delta <= 0 || slots == null) return;
            var target = playerPosition; target.y = .25f;
            foreach (var slot in slots)
            {
                if (!slot.active) continue;
                slot.age += delta; slot.remaining -= delta;
                var offset = target - slot.position; offset.y = 0;
                var clearPath = canCollect && !Physics.Linecast(slot.position, target, GameLayers.Blocking, QueryTriggerInteraction.Ignore);
                if (canCollect && GemRules.CanAttract(slot.age, offset.sqrMagnitude, clearPath, balance)) slot.attracted = true;
                if (slot.attracted && clearPath)
                {
                    slot.speed = Mathf.Min(balance.maximumPullSpeed, slot.speed + balance.pullAcceleration * delta);
                    slot.position = Vector3.MoveTowards(slot.position, target, slot.speed * delta);
                    if ((slot.position - target).sqrMagnitude <= balance.pickupRadius * balance.pickupRadius)
                    {
                        var kind = slot.kind; var value = slot.value; var position = slot.position;
                        Release(slot); CollectedUnits += value; Collected?.Invoke(kind, value, position); continue;
                    }
                }
                else
                {
                    slot.velocity.y -= 14f * delta;
                    var movement = slot.velocity * delta;
                    var horizontal = new Vector3(movement.x, 0, movement.z);
                    if (horizontal.sqrMagnitude > .000001f && Physics.SphereCast(slot.position, .12f, horizontal.normalized, out _, horizontal.magnitude, GameLayers.Blocking, QueryTriggerInteraction.Ignore))
                    { movement.x = movement.z = 0; slot.velocity.x = slot.velocity.z = 0; }
                    slot.position = ClampToRoom(slot.position + movement);
                    if (slot.position.y < .2f)
                    { slot.position.y = .2f; slot.velocity.y = 0; slot.velocity.x = Mathf.MoveTowards(slot.velocity.x, 0, 10 * delta); slot.velocity.z = Mathf.MoveTowards(slot.velocity.z, 0, 10 * delta); }
                }
                if (slot.remaining <= 0 && !slot.attracted) { ExpiredUnits += slot.value; Release(slot); continue; }
                // A blocked chase must still expire; attraction cannot retain inaccessible gems forever.
                if (slot.remaining <= -2) { ExpiredUnits += slot.value; Release(slot); continue; }
                slot.visual.position = slot.position + Vector3.up * (.025f * Mathf.Sin(slot.age * 6));
                slot.visual.Rotate(Vector3.up, 100 * delta, Space.World);
                slot.renderer.enabled = slot.remaining > 1.5f || slot.attracted || (int)(slot.remaining * 8) % 2 == 0;
            }
        }
        public void ExtendCollectionWindow(float seconds)
        { if (slots != null) foreach (var slot in slots) if (slot.active) slot.remaining = Mathf.Max(slot.remaining, seconds); }
        public bool TryNearest(Vector3 position, out Vector3 target)
        {
            target = position; var best = float.MaxValue;
            if (slots == null) return false;
            foreach (var slot in slots) if (slot.active && (slot.position - position).sqrMagnitude < best)
            { best = (slot.position - position).sqrMagnitude; target = slot.position; }
            return best < float.MaxValue;
        }
        private Vector3 ClampToRoom(Vector3 p)
        { p.x = Mathf.Clamp(p.x, roomOrigin.x - 11.2f, roomOrigin.x + 11.2f); p.z = Mathf.Clamp(p.z, roomOrigin.z - 11.2f, roomOrigin.z + 11.2f); return p; }
        private void Release(Slot slot)
        { slot.active = false; slot.visual.gameObject.SetActive(false); ActiveCount--; }
        public void Clear()
        { if (slots != null) foreach (var slot in slots) if (slot.active) Release(slot); }
        public void ResetCounters()
        { SpawnedUnits = CollectedUnits = ExpiredUnits = MergedUnits = PeakActive = 0; }
    }
}
