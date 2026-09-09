using UnityEngine;
using Vaultbreakers.Combat;
using Vaultbreakers.Input;
using Vaultbreakers.Player;

namespace Vaultbreakers.Zones
{
    public enum ZoneState { Preparing, Fighting, BetweenWaves, Retry, Complete, Training }
    [DefaultExecutionOrder(-100)]
    public sealed class ZoneController : MonoBehaviour
    {
        [SerializeField] private bool dungeonJourney;
        public bool IsDungeon => dungeonJourney;
        public Vector3 RoomOrigin => dungeonJourney ? Vector3.forward * Vaultbreakers.Dungeon.DungeonLayout.RoomSpacing * waveIndex : Vector3.zero;
        public void ConfigureDungeon(WaveDefinition[] encounters) { dungeonJourney=true; waves=encounters; }
        [SerializeField] private Health player;
        [SerializeField] private WaveSpawner spawner;
        [SerializeField] private WaveDefinition[] waves;
        [SerializeField] private GameObject[] dummies;
        private PlayerInputReader input;
        private PlayerMotor motor;
        private PlayerFacing facing;
        private MeleeController melee;
        private RangedController ranged;
        private ShieldController shield;
        private DodgeController dodge;
        private HitStop hitStop;
        private float timer;
        private int waveIndex;
        public ZoneState State { get; private set; }
        public bool IsPaused { get; private set; }
        public int WaveNumber => waveIndex + 1;
        public int WaveCount => waves.Length;
        public float TransitionRemaining => timer;
        public Health Player => player;
        public WaveSpawner Spawner => spawner;
        public string WaveTitle => waves[waveIndex].title;
        public void Configure(Health target, WaveSpawner waveSpawner, WaveDefinition[] definitions, GameObject[] practice)
        { player = target; spawner = waveSpawner; waves = definitions; dummies = practice; }
        private void Awake()
        {
            input = player.GetComponent<PlayerInputReader>(); motor = player.GetComponent<PlayerMotor>();
            facing = player.GetComponent<PlayerFacing>(); melee = player.GetComponent<MeleeController>();
            ranged = player.GetComponent<RangedController>(); shield = player.GetComponent<ShieldController>();
            dodge = player.GetComponent<DodgeController>(); hitStop = player.GetComponent<HitStop>();
            player.Died += OnDeath; spawner.Cleared += OnWaveCleared;
        }
        private void Start() { if (State != ZoneState.Training) RestartRun(); }
        private void Update()
        {
            if (input.PausePressedThisFrame) SetPaused(!IsPaused);
            if (input.RestartPressedThisFrame) { if (State == ZoneState.Complete) RestartRun(); else RestartWave(); }
            if (IsPaused) return;
            if (State is ZoneState.Preparing or ZoneState.BetweenWaves or ZoneState.Retry)
            {
                if (dungeonJourney && State == ZoneState.BetweenWaves)
                {
                    if(player.transform.position.z < RoomOrigin.z + Vaultbreakers.Dungeon.DungeonLayout.AdvanceOffset) return;
                    waveIndex++; player.Heal(player.MaximumHealth);shield.ResetShield(); State=ZoneState.Fighting; spawner.Spawn(waves[waveIndex],waveIndex); return;
                }
                timer -= Time.deltaTime;
                if (timer <= 0)
                {
                    if (State == ZoneState.BetweenWaves) waveIndex++;
                    ResetPlayer(); State = ZoneState.Fighting;
                    spawner.Spawn(waves[waveIndex], waveIndex);
                }
            }
        }
        public void RestartRun()
        {
            spawner.Clear(); spawner.Progress.RestartRun(); waveIndex = 0; SetDummies(false);
            RestartWave();
        }
        public void RestartWave()
        {
            SetPaused(false); spawner.Clear(); ResetPlayer(); SetDummies(false);
            State = ZoneState.Preparing; timer = 0.8f;
        }
        public void StartSelectedWave(int index)
        { waveIndex = Mathf.Clamp(index, 0, waves.Length - 1); RestartWave(); }
        public void EnterTraining()
        {
            SetPaused(false); spawner.Clear(); if(dungeonJourney)waveIndex=0; ResetPlayer(); SetDummies(true); State = ZoneState.Training;
        }
        private void SetDummies(bool value) { foreach (var dummy in dummies) if (dummy != null) dummy.SetActive(value); }
        private void OnWaveCleared()
        {
            if (State != ZoneState.Fighting) return;
            spawner.Progress.Complete(); ranged.Pool.ReleaseAll(); spawner.Roster.Projectiles.ReleaseAll();
            if (waveIndex == waves.Length - 1) State = ZoneState.Complete;
            else { State = ZoneState.BetweenWaves; timer = waves[waveIndex].nextWaveDelay; }
        }
        private void OnDeath(DamageInfo damage)
        {
            if (State == ZoneState.Training) { RestartWave(); return; }
            State = ZoneState.Retry; timer = 1.2f; spawner.Clear(); ranged.Pool.ReleaseAll();
            hitStop.Release(); EnableControls(false);
        }
        private void ResetPlayer()
        {
            hitStop.Release(); melee.ResetCombat(); ranged.ResetCombat(); dodge.ResetDodge();
            ranged.Pool.ReleaseAll();
            var controller = player.GetComponent<CharacterController>(); controller.enabled = false;
            player.transform.SetPositionAndRotation(RoomOrigin + (dungeonJourney ? Vector3.back*3.5f : Vector3.zero), Quaternion.identity); controller.enabled = true;
            motor.ResetMovement(); player.ResetHealth(); shield.ResetShield();
            player.GetComponent<PlayerActionCoordinator>().ResetState(); facing.ApplyAttackFacing(Vector3.forward);
            EnableControls(true);
        }
        public void SetPaused(bool value)
        {
            hitStop.Release(); IsPaused = value; Time.timeScale = value ? 0 : 1;

        }
        private void EnableControls(bool value)
        { motor.enabled = value; facing.enabled = value; melee.enabled = value; ranged.enabled = value; shield.enabled = value; dodge.enabled = value; }
        private void OnDestroy()
        {
            if (player != null) player.Died -= OnDeath;
            if (spawner != null) spawner.Cleared -= OnWaveCleared;
            Time.timeScale = 1;
        }
    }
}
