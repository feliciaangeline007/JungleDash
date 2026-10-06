using UnityEngine;

namespace JungleDash
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        private AudioSource sfxSource;
        private AudioClip coinClip, gemClip, powerUpClip, jumpClip, crashClip, clickClip;

        public bool SoundEnabled
        {
            get => sfxSource != null && !sfxSource.mute;
            set
            {
                if (sfxSource != null) sfxSource.mute = !value;
                PlayerPrefs.SetInt("JD_Sound", value ? 1 : 0);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake  = false;
            sfxSource.spatialBlend = 0f;

            bool savedSound = PlayerPrefs.GetInt("JD_Sound", 1) == 1;
            sfxSource.mute = !savedSound;

            BuildClips();
        }

        // ── BUG FIX: crashClip used Random.value inside the loop which
        // modifies global RNG state and produces different noise each
        // frame when re-generated. Pre-bake the noise into the buffer. ──
        private void BuildClips()
        {
            coinClip    = Synth(0.15f, (t) => Mathf.Sin(6200f * t) * Mathf.Exp(-t * 18f) * 0.5f);
            gemClip     = Synth(0.25f, (t) => (Mathf.Sin(8363f * t) + 0.5f * Mathf.Sin(13270f * t)) * Mathf.Exp(-t * 9f) * 0.5f);
            powerUpClip = Synth(0.40f, (t) => Mathf.Sin((2000f + t * 4000f) * t) * Mathf.Exp(-t * 5f) * 0.6f);
            jumpClip    = Synth(0.20f, (t) => Mathf.Sin((800f - t * 600f) * t) * Mathf.Exp(-t * 12f) * 0.55f);
            clickClip   = Synth(0.08f, (t) => Mathf.Sin(3000f * t) * Mathf.Exp(-t * 30f) * 0.4f);
            crashClip   = BuildCrash(); // noise – built separately with seeded noise
        }

        private static AudioClip Synth(float duration, System.Func<float, float> wave)
        {
            const int rate = 22050;
            int   n    = Mathf.RoundToInt(duration * rate);
            float[] d  = new float[n];
            for (int i = 0; i < n; i++) d[i] = Mathf.Clamp(wave((float)i / rate), -1f, 1f);
            var clip = AudioClip.Create("sfx", n, 1, rate, false);
            clip.SetData(d, 0);
            return clip;
        }

        private static AudioClip BuildCrash()
        {
            const int rate = 22050;
            int   n   = Mathf.RoundToInt(0.5f * rate);
            float[] d = new float[n];

            // Use a deterministic LCG so the sound is always the same
            uint rng = 12345u;
            for (int i = 0; i < n; i++)
            {
                rng = rng * 1664525u + 1013904223u;
                float noise = ((rng & 0xFFFFu) / 32767.5f) - 1f; // -1..1
                float t     = (float)i / rate;
                float env   = Mathf.Exp(-t * 6f);
                // Mix noise with a low-freq thud
                float thud  = Mathf.Sin(80f * Mathf.PI * t) * Mathf.Exp(-t * 14f);
                d[i] = Mathf.Clamp((noise * 0.7f + thud * 0.8f) * env, -1f, 1f);
            }
            var clip = AudioClip.Create("crash", n, 1, rate, false);
            clip.SetData(d, 0);
            return clip;
        }

        public void PlayCoin()    => Play(coinClip);
        public void PlayGem()     => Play(gemClip);
        public void PlayPowerUp() => Play(powerUpClip);
        public void PlayJump()    => Play(jumpClip);
        public void PlayCrash()   => Play(crashClip);
        public void PlayClick()   => Play(clickClip);

        private void Play(AudioClip clip)
        {
            if (clip != null && sfxSource != null && !sfxSource.mute)
                sfxSource.PlayOneShot(clip);
        }
    }
}
