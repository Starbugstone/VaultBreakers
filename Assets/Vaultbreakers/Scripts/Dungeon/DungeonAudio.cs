using UnityEngine;
using Vaultbreakers.Combat;
using Vaultbreakers.Zones;

namespace Vaultbreakers.Dungeon
{
    // Quiet, distinct progression cues complement the transient combat mix.
    public sealed class DungeonAudio : MonoBehaviour
    {
        private ZoneController zone;
        private DungeonJourney journey;
        private AudioSource source;
        private AudioClip clear, core, retry, enter;
        private ZoneState previous;
        private bool claimed;
        private void Start()
        {
            zone = GetComponent<ZoneController>(); journey = GetComponent<DungeonJourney>();
            source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false;
            clear = PlaceholderAudio.CreateBurst("Dock9_Clear", .5f, 440, .8f);
            core = PlaceholderAudio.CreateBurst("Dock9_Core", .9f, 660, 1.2f);
            retry = PlaceholderAudio.CreateBurst("Dock9_Retry", .4f, 165, .8f);
            enter = PlaceholderAudio.CreateBurst("Dock9_Enter", .24f, 330, .6f);
            previous = zone.State;
        }
        private void Update()
        {
            if (zone.IsPaused) return;
            if (zone.State != previous)
            {
                if (zone.State == ZoneState.BetweenWaves || zone.State == ZoneState.Complete) source.PlayOneShot(clear, .38f);
                if (zone.State == ZoneState.Retry) source.PlayOneShot(retry, .4f);
                if (zone.State == ZoneState.Fighting) source.PlayOneShot(enter, .22f);
                previous = zone.State;
            }
            if (journey.TreasureClaimed && !claimed) source.PlayOneShot(core, .5f);
            claimed = journey.TreasureClaimed;
        }
        private void OnDestroy()
        {
            Destroy(clear); Destroy(core); Destroy(retry); Destroy(enter);
        }
    }
}
