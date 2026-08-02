using UnityEngine;

namespace Vaultbreakers.Combat
{
    /// <summary>
    /// Generates the POC's placeholder combat cues rather than importing them. Clips made here have
    /// known provenance and cost the repository nothing, which matters while every real sound is still
    /// unlicensed and unwritten. All of this is replaced wholesale once real audio exists.
    /// </summary>
    public static class PlaceholderAudio
    {
        private const int SampleRate = 22050;

        /// <summary>
        /// A decaying burst of noise mixed with a tone. Seeded from the clip name, so the same cue
        /// sounds identical on every run and across every machine.
        /// </summary>
        public static AudioClip CreateBurst(string clipName, float duration, float centreHz, float decay)
        {
            var sampleCount = Mathf.Max(1, Mathf.RoundToInt(duration * SampleRate));
            var samples = new float[sampleCount];
            var random = new System.Random(clipName.GetHashCode());

            for (var index = 0; index < sampleCount; index++)
            {
                var time = index / (float)SampleRate;
                var envelope = Mathf.Exp(-time / Mathf.Max(0.001f, duration * decay));
                var noise = (float)(random.NextDouble() * 2.0 - 1.0);
                var tone = Mathf.Sin(2f * Mathf.PI * centreHz * time);
                samples[index] = (noise * 0.6f + tone * 0.4f) * envelope;
            }

            var clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
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
            if (source != null && clip != null)
            {
                source.PlayOneShot(clip, volume);
            }
        }
    }
}
