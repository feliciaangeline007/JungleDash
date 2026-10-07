// GameAudio.cs - Rich procedurally synthesised audio: SFX, adaptive jungle drum loop, ambient environment.
using UnityEngine;

namespace JungleDash
{
    public class GameAudio : MonoBehaviour
    {
        const int SR = 44100;
        AudioSource sfx, music, ambient;
        AudioClip coin, gem, jump, slide, hit, power, shieldBreak, over, land, drums;
        AudioClip speedSound, flySound, doubleSound, woodBreak, clickSound, pauseSound, winSound;
        AudioClip doubleJumpClip, comboBreakClip, milestoneClip, reviveClip;
        AudioClip jungleAmbient;

        bool soundOn = true;
        public bool SoundOn
        {
            get { return soundOn; }
            set
            {
                soundOn = value;
                AudioListener.volume = soundOn ? 1f : 0f;
                PlayerPrefs.SetInt("jd_sound", soundOn ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public void Build()
        {
            soundOn = PlayerPrefs.GetInt("jd_sound", 1) == 1;
            AudioListener.volume = soundOn ? 1f : 0f;

            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.spatialBlend = 0f;

            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.volume = 0.34f;

            ambient = gameObject.AddComponent<AudioSource>();
            ambient.playOnAwake = false;
            ambient.loop = true;
            ambient.volume = 0.12f;

            // Core SFX
            coin = Synth("coin", 0.22f, delegate (float t) {
                float f = t < 0.07f ? 988f : 1319f;
                return Mathf.Sin(6.2832f * f * t) * Mathf.Exp(-t * 11f) * 0.4f;
            });
            gem = Synth("gem", 0.4f, delegate (float t) {
                float f = t < 0.08f ? 1175f : (t < 0.16f ? 1568f : 2093f);
                return Mathf.Sin(6.2832f * f * t) * Mathf.Exp(-t * 7f) * 0.38f;
            });
            jump = Synth("jump", 0.22f, delegate (float t) {
                float ph = 6.2832f * (280f * t + 1000f * t * t);
                return Mathf.Sin(ph) * Mathf.Exp(-t * 7.5f) * 0.38f;
            });
            slide = Synth("slide", 0.3f, delegate (float t) {
                return (Random.value * 2f - 1f) * Mathf.Exp(-t * 9f) * 0.22f * Mathf.Min(1f, t * 60f);
            });
            land = Synth("land", 0.16f, delegate (float t) {
                float thud = Mathf.Sin(6.2832f * 65f * t) * Mathf.Exp(-t * 30f) * 0.55f;
                float dust = (Random.value * 2f - 1f) * Mathf.Exp(-t * 20f) * 0.15f;
                return thud + dust;
            });
            hit = Synth("hit", 0.6f, delegate (float t) {
                float n = (Random.value * 2f - 1f) * Mathf.Exp(-t * 9f) * 0.5f;
                float lo = Mathf.Sin(6.2832f * (110f * t - 90f * t * t)) * Mathf.Exp(-t * 6f) * 0.7f;
                return n + lo;
            });
            woodBreak = Synth("woodBreak", 0.35f, delegate (float t) {
                float n = (Random.value * 2f - 1f) * Mathf.Exp(-t * 16f) * 0.55f;
                float snap = Mathf.Sin(6.2832f * 180f * t) * Mathf.Exp(-t * 20f) * 0.45f;
                return n + snap;
            });
            power = Synth("power", 0.5f, delegate (float t) {
                float[] n = { 523f, 659f, 784f, 1047f };
                int i = Mathf.Min(3, (int)(t / 0.09f));
                return Mathf.Sin(6.2832f * n[i] * t) * Mathf.Exp(-t * 4f) * 0.34f;
            });
            speedSound = Synth("speed", 0.45f, delegate (float t) {
                float f = 440f + 950f * t;
                float w = (Random.value * 2f - 1f) * 0.2f;
                return (Mathf.Sin(6.2832f * f * t) * 0.35f + w) * Mathf.Exp(-t * 3.5f);
            });
            flySound = Synth("fly", 0.6f, delegate (float t) {
                float hum = Mathf.Sin(6.2832f * (220f + 30f * Mathf.Sin(t * 35f)) * t) * 0.35f;
                float chime = Mathf.Sin(6.2832f * 1318f * t) * Mathf.Exp(-t * 5f) * 0.25f;
                return (hum + chime) * Mathf.Exp(-t * 2.5f);
            });
            doubleSound = Synth("double", 0.55f, delegate (float t) {
                float[] chords = { 659.25f, 830.6f, 987.77f, 1318.5f };
                int idx = Mathf.Min(3, (int)(t / 0.1f));
                return Mathf.Sin(6.2832f * chords[idx] * t) * Mathf.Exp(-t * 3.5f) * 0.38f;
            });
            shieldBreak = Synth("shieldBreak", 0.45f, delegate (float t) {
                float n = (Random.value * 2f - 1f) * Mathf.Exp(-t * 14f) * 0.4f;
                return n + Mathf.Sin(6.2832f * (900f - 1400f * t) * t) * Mathf.Exp(-t * 8f) * 0.35f;
            });
            clickSound = Synth("click", 0.06f, delegate (float t) {
                return Mathf.Sin(6.2832f * 1400f * t) * Mathf.Exp(-t * 70f) * 0.4f;
            });
            pauseSound = Synth("pause", 0.25f, delegate (float t) {
                float f = 550f - 240f * t;
                return Mathf.Sin(6.2832f * f * t) * Mathf.Exp(-t * 10f) * 0.35f;
            });
            over = Synth("over", 0.9f, delegate (float t) {
                float[] n = { 392f, 330f, 262f, 196f };
                int i = Mathf.Min(3, (int)(t / 0.2f));
                return Mathf.Sin(6.2832f * n[i] * t) * Mathf.Exp(-(t - i * 0.2f) * 5f) * 0.35f * Mathf.Exp(-t * 1.5f);
            });
            winSound = Synth("win", 0.95f, delegate (float t) {
                float[] n = { 523.25f, 659.25f, 783.99f, 1046.5f };
                int i = Mathf.Min(3, (int)(t / 0.16f));
                float chime = Mathf.Sin(6.2832f * n[i] * t) * Mathf.Exp(-t * 2.8f) * 0.42f;
                float sparkle = Mathf.Sin(6.2832f * n[i] * 2f * t) * Mathf.Exp(-t * 4f) * 0.15f;
                return chime + sparkle;
            });

            // New SFX
            doubleJumpClip = Synth("doubleJump", 0.2f, delegate (float t) {
                float ph = 6.2832f * (400f * t + 1200f * t * t);
                return Mathf.Sin(ph) * Mathf.Exp(-t * 9f) * 0.32f;
            });
            comboBreakClip = Synth("comboBreak", 0.3f, delegate (float t) {
                float f = 600f - 400f * t;
                return Mathf.Sin(6.2832f * f * t) * Mathf.Exp(-t * 12f) * 0.25f;
            });
            milestoneClip = Synth("milestone", 0.6f, delegate (float t) {
                float[] tones = { 784f, 988f, 1175f, 1568f };
                int i = Mathf.Min(3, (int)(t / 0.12f));
                float val = Mathf.Sin(6.2832f * tones[i] * t) * Mathf.Exp(-t * 3f) * 0.35f;
                val += Mathf.Sin(6.2832f * tones[i] * 1.5f * t) * Mathf.Exp(-t * 5f) * 0.12f;
                return val;
            });
            reviveClip = Synth("revive", 0.7f, delegate (float t) {
                float[] tones = { 262f, 330f, 392f, 523f, 659f };
                int i = Mathf.Min(4, (int)(t / 0.11f));
                return Mathf.Sin(6.2832f * tones[i] * t) * Mathf.Exp(-t * 2.5f) * 0.38f;
            });

            // Music loop
            drums = BuildDrums();
            
            // Ambient jungle loop
            jungleAmbient = BuildAmbient();
        }

        delegate float Gen(float t);

        static AudioClip Synth(string name, float dur, Gen g)
        {
            int len = (int)(dur * SR);
            float[] d = new float[len];
            for (int i = 0; i < len; i++) d[i] = Mathf.Clamp(g(i / (float)SR), -1f, 1f);
            AudioClip c = AudioClip.Create(name, len, 1, SR, false);
            c.SetData(d, 0);
            return c;
        }

        // 8 beats at ~132 bpm: fuller percussion with layered kick, snare, shaker, bongos, and bass
        static AudioClip BuildDrums()
        {
            float bpm = 132f;
            float beat = 60f / bpm;
            float dur = beat * 16f;
            int len = (int)(dur * SR);
            float[] d = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)SR;
                float v = 0f;

                // Kick drum (every beat)
                float kt = Mathf.Repeat(t, beat);
                float kph = 6.2832f * (42f * kt + 60f * (1f - Mathf.Exp(-25f * kt)) / 25f);
                v += Mathf.Sin(kph) * Mathf.Exp(-kt * 9f) * 0.75f;

                // Snare on beats 2 and 4
                float halfBar = beat * 2f;
                float st = Mathf.Repeat(t + beat, halfBar);
                if (st < 0.15f)
                {
                    v += (Random.value * 2f - 1f) * Mathf.Exp(-st * 28f) * 0.28f;
                    v += Mathf.Sin(6.2832f * 200f * st) * Mathf.Exp(-st * 25f) * 0.2f;
                }

                // Hi-hat shaker (off-beats)
                float ht = Mathf.Repeat(t + beat * 0.5f, beat);
                v += (Random.value * 2f - 1f) * Mathf.Exp(-ht * 45f) * 0.16f;

                // 16th note hi-hat ghost notes
                float ghostT = Mathf.Repeat(t, beat * 0.25f);
                v += (Random.value * 2f - 1f) * Mathf.Exp(-ghostT * 60f) * 0.06f;

                // Bongo syncopation pattern
                float[] bongoAt = { 2.5f, 3.25f, 5.5f, 6.5f, 7.25f, 10.5f, 11.25f, 13.5f, 14.5f, 15.25f };
                for (int b = 0; b < bongoAt.Length; b++)
                {
                    float bt = t - bongoAt[b] * beat;
                    if (bt < 0f || bt > 0.25f) continue;
                    float f = (b & 1) == 0 ? 330f : 247f;
                    v += Mathf.Sin(6.2832f * f * bt) * Mathf.Exp(-bt * 22f) * 0.26f;
                }

                // Sub bass line (octave below kick, sustained)
                float bassT = Mathf.Repeat(t, beat * 4f);
                float bassF = 55f + 10f * Mathf.Sin(bassT * 2f);
                v += Mathf.Sin(6.2832f * bassF * t) * 0.12f * Mathf.Clamp01(1f - bassT * 0.5f);

                d[i] = Mathf.Clamp(v, -1f, 1f);
            }
            AudioClip c = AudioClip.Create("drums", len, 1, SR, false);
            c.SetData(d, 0);
            return c;
        }

        // Ambient jungle: layered bird chirps, wind, and insect hum
        static AudioClip BuildAmbient()
        {
            float dur = 8f;
            int len = (int)(dur * SR);
            float[] d = new float[len];

            System.Random rng = new System.Random(42);
            
            // Pre-generate bird chirp events
            int numChirps = 12;
            float[] chirpTimes = new float[numChirps];
            float[] chirpFreqs = new float[numChirps];
            for (int c = 0; c < numChirps; c++)
            {
                chirpTimes[c] = (float)rng.NextDouble() * dur;
                chirpFreqs[c] = 2800f + (float)rng.NextDouble() * 2500f;
            }

            for (int i = 0; i < len; i++)
            {
                float t = i / (float)SR;
                float v = 0f;

                // Wind (low-pass filtered noise)
                float wind = (float)(rng.NextDouble() * 2.0 - 1.0);
                float windEnv = 0.04f * (0.5f + 0.5f * Mathf.Sin(t * 0.3f));
                v += wind * windEnv;

                // Cricket/insect chirps
                float cricket = Mathf.Sin(6.2832f * 4200f * t) * 0.02f;
                float cricketEnv = Mathf.Max(0f, Mathf.Sin(t * 8f));
                v += cricket * cricketEnv * cricketEnv;

                // Bird chirps
                for (int c = 0; c < numChirps; c++)
                {
                    float ct = t - chirpTimes[c];
                    if (ct >= 0f && ct < 0.12f)
                    {
                        float freq = chirpFreqs[c] + ct * 3000f;
                        v += Mathf.Sin(6.2832f * freq * ct) * Mathf.Exp(-ct * 35f) * 0.08f;
                    }
                }

                d[i] = Mathf.Clamp(v, -1f, 1f);
            }
            AudioClip c2 = AudioClip.Create("jungle_ambient", len, 1, SR, false);
            c2.SetData(d, 0);
            return c2;
        }

        public void PlayMusic(bool on)
        {
            if (on)
            {
                if (!music.isPlaying) { music.clip = drums; music.Play(); }
                if (!ambient.isPlaying && jungleAmbient != null) { ambient.clip = jungleAmbient; ambient.Play(); }
            }
            else
            {
                music.Stop();
                ambient.Stop();
            }
        }

        public void PlayAmbientOnly(bool on)
        {
            if (on)
            {
                if (!ambient.isPlaying && jungleAmbient != null) { ambient.clip = jungleAmbient; ambient.Play(); }
            }
            else
            {
                ambient.Stop();
            }
        }

        public void SetMusicPitch(float pitch)
        {
            if (music != null)
            {
                music.pitch = Mathf.Clamp(pitch, 0.85f, 1.4f);
            }
        }

        void One(AudioClip c, float vol = 1f, float pitch = 1f)
        {
            if (!soundOn || c == null) return;
            sfx.pitch = pitch;
            sfx.PlayOneShot(c, vol);
        }

        public void Coin(int combo) { One(coin, 0.8f, 1f + Mathf.Min(combo, 12) * 0.035f); }
        public void Gem() { One(gem, 0.9f); }
        public void Jump() { One(jump, 0.8f); }
        public void DoubleJump() { One(doubleJumpClip, 0.75f, 1.15f); }
        public void Slide() { One(slide, 0.8f); }
        public void Land() { One(land, 0.65f); }
        public void Hit() { One(hit, 1f); }
        public void WoodBreak() { One(woodBreak, 0.9f); }
        public void Power() { One(power, 0.9f); }
        public void Speed() { One(speedSound, 0.9f); }
        public void Fly() { One(flySound, 0.95f); }
        public void Double() { One(doubleSound, 0.95f); }
        public void ShieldBreak() { One(shieldBreak, 0.9f); }
        public void Click() { One(clickSound, 0.7f); }
        public void Pause() { One(pauseSound, 0.7f); }
        public void Win() { One(winSound, 1f); }
        public void GameOver() { One(over, 0.9f); }
        public void ComboBreak() { One(comboBreakClip, 0.5f); }
        public void Milestone() { One(milestoneClip, 0.85f); }
        public void Revive() { One(reviveClip, 0.9f); }
    }
}
