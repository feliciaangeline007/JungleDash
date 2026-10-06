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
            set { if (sfxSource != null) sfxSource.mute = !value; }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            BuildClips();
        }

        private void BuildClips()
        {
            coinClip    = SynthClip(0.15f, (t) => Mathf.Sin(6200f * t) * Mathf.Exp(-t * 18f) * 0.5f);
            gemClip     = SynthClip(0.25f, (t) => (Mathf.Sin(8363f * t) + 0.5f * Mathf.Sin(13270f * t)) * Mathf.Exp(-t * 9f) * 0.5f);
            powerUpClip = SynthClip(0.40f, (t) => Mathf.Sin((2000f + t * 4000f) * t) * Mathf.Exp(-t * 5f) * 0.6f);
            jumpClip    = SynthClip(0.20f, (t) => Mathf.Sin((800f - t * 600f) * t) * Mathf.Exp(-t * 12f) * 0.55f);
            crashClip   = SynthClip(0.50f, (t) => (Random.value * 2f - 1f) * Mathf.Exp(-t * 6f) * 0.7f);
            clickClip   = SynthClip(0.08f, (t) => Mathf.Sin(3000f * t) * Mathf.Exp(-t * 30f) * 0.4f);
        }

        private AudioClip SynthClip(float duration, System.Func<float, float> wave)
        {
            int rate = 22050;
            int samples = Mathf.RoundToInt(duration * rate);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++) data[i] = Mathf.Clamp(wave((float)i / rate), -1f, 1f);
            AudioClip clip = AudioClip.Create("sfx", samples, 1, rate, false);
            clip.SetData(data, 0);
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
