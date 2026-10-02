using UnityEngine;
using Vaultbreakers.Combat;
using Vaultbreakers.Dungeon;
using Vaultbreakers.Enemies;
using Vaultbreakers.UI;
using Vaultbreakers.Zones;

namespace Vaultbreakers.Gems
{
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(ZoneController), typeof(GemPool))]
    public sealed class DungeonLoot : MonoBehaviour
    {
        [SerializeField] private GemBalance balance;
        [SerializeField] private BreakableLoot[] containers;
        private ZoneController zone;
        private DungeonJourney journey;
        private ArcadeFeedback feedback;
        private AudioSource audioSource;
        private AudioClip pickup, impact, shatter;
        private float nextPickupSound, chainUntil;
        private int chain, currentRoom = -1;
        private bool subscribed;
        public GemPool Pool { get; private set; }
        public GemWallet Wallet { get; } = new();
        public GemBalance Balance => balance;
        public BreakableLoot[] Containers => containers;
        public int CratesBroken { get; private set; }
        public int ChestsBroken { get; private set; }
        public int EnemyDrops { get; private set; }
        public void Configure(GemBalance tuning, BreakableLoot[] objects) { balance = tuning; containers = objects; }
        private void Awake()
        {
            zone = GetComponent<ZoneController>(); Pool = GetComponent<GemPool>();
            journey = GetComponent<DungeonJourney>(); feedback = GetComponent<ArcadeFeedback>();
            zone.Resetting += OnReset; zone.RoomStarted += OnRoomStarted; zone.RoomCleared += OnRoomCleared;
            Pool.Collected += OnCollected;
        }
        private void Start()
        {
            Pool.Initialize(); zone.Spawner.Roster.Initialize();
            foreach (var enemy in zone.Spawner.Roster.Instances) enemy.HitReceived += OnEnemyHit;
            subscribed = true;
            audioSource = gameObject.AddComponent<AudioSource>(); audioSource.playOnAwake = false;
            pickup = PlaceholderAudio.CreateBurst("Gem_Pickup", .075f, 1047, .12f);
            impact = PlaceholderAudio.CreateBurst("Cache_Hit", .10f, 180, .35f);
            shatter = PlaceholderAudio.CreateBurst("Cache_Break", .22f, 110, .65f);
        }
        private void Update()
        {
            if (zone.IsPaused || zone.State is ZoneState.Preparing or ZoneState.Retry or ZoneState.Training) return;
            Pool.Tick(Time.deltaTime, zone.Player.transform.position, !zone.Player.IsDead);
        }
        private void OnEnemyHit(EnemyBrain enemy, DamageInfo damage, DamageResult result)
        {
            if (!result.Killed || zone.State != ZoneState.Fighting) return;
            var kind = enemy.Definition.role switch
            { EnemyRole.Shooter => LootSource.Shooter, EnemyRole.Bruiser => LootSource.Bruiser, _ => LootSource.Grunt };
            Pool.Drop(enemy.transform.position, kind); EnemyDrops++;
        }
        public void ContainerBroken(BreakableLoot container)
        {
            Pool.Drop(container.transform.position, container.Source);
            if (container.Source == LootSource.Chest) ChestsBroken++; else CratesBroken++;
        }
        public void HitContainer(Vector3 position, bool destroyed)
        {
            if (feedback != null) feedback.Burst(position + Vector3.up * .5f, new Color(1, .72f, .3f), 0);
            if (audioSource != null) { audioSource.pitch = 1; audioSource.PlayOneShot(destroyed ? shatter : impact, destroyed ? .35f : .22f); }
        }
        private void OnCollected(GemKind kind, int value, Vector3 position)
        {
            Wallet.Add(kind, value);
            if (kind == GemKind.Gold) journey.AddGemScore(value * balance.goldScore);
            if (Time.time < nextPickupSound || audioSource == null) return;
            chain = Time.time < chainUntil ? Mathf.Min(chain + 1, 7) : 0;
            chainUntil = Time.time + .5f; nextPickupSound = Time.time + .045f;
            audioSource.pitch = 1 + chain * .065f; audioSource.PlayOneShot(pickup, .23f);
        }
        private void OnReset(ZoneResetReason reason)
        {
            Pool.Clear(); chain = 0; chainUntil = nextPickupSound = 0;
            if (reason == ZoneResetReason.Run)
            { Wallet.Reset(); Pool.ResetCounters(); currentRoom = -1; CratesBroken = ChestsBroken = EnemyDrops = 0; }
            else Wallet.Retry();
            if (containers != null) foreach (var container in containers)
                container.ResetLoot(reason != ZoneResetReason.Training && container.Room == zone.WaveNumber - 1);
            Pool.SetRoom(zone.RoomOrigin);
        }
        private void OnRoomStarted(int room)
        {
            if (room != currentRoom) { Wallet.Bank(); currentRoom = room; Pool.SetRoom(zone.RoomOrigin); }
            foreach (var container in containers) if (container.Room != room) container.gameObject.SetActive(false);
            // A successful transition enables the new room's authored containers, with fresh health.
            foreach (var container in containers) if (container.Room == room && !container.gameObject.activeSelf) container.ResetLoot(true);
        }
        private void OnRoomCleared(int room) => Pool.ExtendCollectionWindow(balance.collectionWindow + 4);
        private void OnDestroy()
        {
            if (zone != null)
            {
                zone.Resetting -= OnReset; zone.RoomStarted -= OnRoomStarted; zone.RoomCleared -= OnRoomCleared;
                if (subscribed && zone.Spawner != null && zone.Spawner.Roster != null)
                    foreach (var enemy in zone.Spawner.Roster.Instances) if (enemy != null) enemy.HitReceived -= OnEnemyHit;
            }
            if (Pool != null) Pool.Collected -= OnCollected;
            Destroy(pickup); Destroy(impact); Destroy(shatter);
        }
    }
}
