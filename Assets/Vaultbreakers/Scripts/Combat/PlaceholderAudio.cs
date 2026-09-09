using UnityEngine;

namespace Vaultbreakers.Combat
{
    // Original, reproducible synthesized arcade cues. Historical class name preserves callers.
    public static class PlaceholderAudio
    {
        private static float soundWindow;
        private static int soundsInWindow;
        private const int SampleRate = 22050;

        public static AudioClip CreateBurst(string clipName, float duration, float centreHz, float decay)
        {
            var samples = Synthesize(clipName, duration, centreHz, decay);
            var clip = AudioClip.Create(clipName, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static float[] Synthesize(string name, float duration, float centreHz, float decay)
        {
            var count = Mathf.Max(2, Mathf.RoundToInt(duration * SampleRate));
            var samples = new float[count];
            // FNV-1a, independent of runtime-randomized string.GetHashCode().
            uint seed = 2166136261;
            foreach (var character in name) { seed ^= character; seed = unchecked(seed * 16777619); }
            var random = new System.Random(unchecked((int)seed));
            var swoosh = name.Contains("Swing") || name.Contains("Dodge");
            var energy = name.Contains("Fire") || name.Contains("Block");
            var reward = name.Contains("Clear") || name.Contains("Core");
            var phase = 0f; var filtered = 0f;
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)(count - 1); var seconds = i / (float)SampleRate;
                var attack = Mathf.Min(1, seconds / .003f);
                var release = Mathf.Clamp01((1 - t) / .12f);
                var envelope = attack * release * Mathf.Exp(-t / Mathf.Max(.01f, decay));
                filtered = Mathf.Lerp(filtered, (float)(random.NextDouble() * 2 - 1), swoosh ? .25f : .12f);
                var pitch = reward ? centreHz * (t < .33f ? 1 : t < .66f ? 1.25f : 1.5f)
                    : centreHz * Mathf.Lerp(energy ? 1.7f : 1.1f, swoosh ? .35f : .65f, t);
                phase += 2 * Mathf.PI * pitch / SampleRate;
                var tone = Mathf.Sin(phase) * .7f + Mathf.Sin(phase * (energy ? 2.01f : 1.5f)) * .18f;
                samples[i] = envelope * (tone * (swoosh ? .12f : .65f) + filtered * (swoosh ? 1.1f : .3f));
            }
            return samples;
        }

        /// <summary>Ensures the object has a non-spatial source suitable for one-shot cues.</summary>
        public static AudioSource EnsureSource(GameObject host)
        {
            var source = host.GetComponent<AudioSource>();
            if (source == null)
            {
                source = host.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        public static void Play(AudioSource source, AudioClip clip, float volume)
        {
            if (Time.unscaledTime - soundWindow > 0.1f) { soundWindow = Time.unscaledTime; soundsInWindow = 0; }
            if (soundsInWindow >= 5) return;
            if (source != null && clip != null)
            {
                soundsInWindow++;
                source.PlayOneShot(clip, volume);
            }
        }
    }
}
