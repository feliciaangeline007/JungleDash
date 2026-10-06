// PowerUpManager.cs
// Singleton that owns all five power-up effects.
// Each effect has an independent 0..1 ramp (SmoothStep), timer and visual references.
// No per-frame allocations: no new, no string concat, no LINQ.
using System.Collections.Generic;
using UnityEngine;

public enum PowerUpType { Magnet, Shield, SpeedBoost, Fly, DoubleScore }

public class PowerUpManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Singleton
    // ─────────────────────────────────────────────
    public static PowerUpManager Instance { get; private set; }

    // ─────────────────────────────────────────────
    //  Per-effect settings (Inspector)
    // ─────────────────────────────────────────────
    [System.Serializable]
    public class EffectSettings
    {
        [Tooltip("Total active duration in seconds.")]
        public float duration = 8f;
        [Tooltip("Seconds for ramp to reach 1.")]
        public float rampUp   = 1f;
        [Tooltip("Seconds for ramp to fall to 0 after expiry.")]
        public float rampDown = 1f;
    }

    [Header("Per-Effect Settings")]
    [Tooltip("Settings for each power-up type in enum order.")]
    public EffectSettings[] effectSettings = new EffectSettings[5]
    {
        new EffectSettings { duration = 8f,  rampUp = 1f,  rampDown = 1f  }, // Magnet
        new EffectSettings { duration = 10f, rampUp = 0.5f, rampDown = 1.5f }, // Shield
        new EffectSettings { duration = 7f,  rampUp = 3f,  rampDown = 2f  }, // SpeedBoost
        new EffectSettings { duration = 8f,  rampUp = 3f,  rampDown = 2.5f }, // Fly
        new EffectSettings { duration = 10f, rampUp = 1f,  rampDown = 1f  }, // DoubleScore
    };

    // ─────────────────────────────────────────────
    //  Magnet
    // ─────────────────────────────────────────────
    [Header("Magnet")]
    [Tooltip("Maximum pull radius at full ramp.")]
    public float magnetRadius    = 7f;
    [Tooltip("Pull speed multiplier (added to player speed so coins always catch up).")]
    public float magnetPullSpeed = 12f;

    // ─────────────────────────────────────────────
    //  Speed Boost
    // ─────────────────────────────────────────────
    [Header("Speed Boost")]
    [Tooltip("Extra speed fraction at full ramp (0.6 = +60%).")]
    public float speedBoostFraction = 0.60f;
    [Tooltip("Extra FOV degrees at full ramp.")]
    public float speedBoostFOV      = 15f;

    // ─────────────────────────────────────────────
    //  Fly
    // ─────────────────────────────────────────────
    [Header("Fly")]
    [Tooltip("Height above ground at full ramp.")]
    public float flyHeight = 3f;

    // ─────────────────────────────────────────────
    //  Visual references (wired by builder / Inspector)
    // ─────────────────────────────────────────────
    [Header("Visual References")]
    [Tooltip("Shield sphere renderer (null = no visual).")]
    public Renderer shieldSphereRenderer;
    [Tooltip("Fly glow particle system (null = no visual).")]
    public ParticleSystem flyGlowParticles;
    [Tooltip("Fly trail renderer (null = no visual).")]
    public TrailRenderer flyTrailRenderer;
    [Tooltip("Speed-line particle system child of camera (null = no visual).")]
    public ParticleSystem speedLinesParticles;

    // ─────────────────────────────────────────────
    //  Runtime state (private, no allocations)
    // ─────────────────────────────────────────────
    private struct PowerUpState
    {
        public float timer;     // time remaining while active
        public float ramp;      // current 0..1 smoothed value
        public bool  active;    // currently counting down
        public bool  consumed;  // Shield: already hit
    }

    private PowerUpState[] _states = new PowerUpState[5];

    // Shield: has it absorbed a hit?
    private bool _shieldConsumed;

    // Shield MaterialPropertyBlock (avoids material instance allocation)
    private MaterialPropertyBlock _shieldMPB;
    private static readonly int _alphaPropID = Shader.PropertyToID("_Color");

    // Cached player speed reference (set by PlayerRunner each frame)
    private float _currentPlayerSpeed;

    // Collectibles in scene that the Magnet can see — filled by Collectible.Register/Unregister
    private readonly List<Collectible> _collectibles = new List<Collectible>(64);

    // ─────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _shieldMPB = new MaterialPropertyBlock();
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsRunning) return;

        float dt = Time.deltaTime;

        for (int i = 0; i < 5; i++)
        {
            ref PowerUpState s = ref _states[i];
            EffectSettings   e = effectSettings[i];

            if (s.active)
            {
                s.timer -= dt;
                if (s.timer <= 0f) s.active = false;

                // Ramp toward 1 while active
                float target = s.active ? 1f : 0f;
                // During active phase ramp up; after expiry ramp down
                float rate   = s.active ? (1f / Mathf.Max(e.rampUp, 0.001f))
                                        : (1f / Mathf.Max(e.rampDown, 0.001f));
                s.ramp = Mathf.MoveTowards(s.ramp, target, rate * dt);
            }
            else
            {
                // Ramp down to 0
                float rate = 1f / Mathf.Max(e.rampDown, 0.001f);
                s.ramp = Mathf.MoveTowards(s.ramp, 0f, rate * dt);
            }
        }

        // Shield visual alpha
        UpdateShieldVisual();
        // Fly trail alpha
        UpdateFlyVisual();
        // Speed lines
        UpdateSpeedLines();
        // Magnet pull
        if (MagnetRamp > 0.001f) TickMagnet(dt);
    }

    // ─────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────

    /// <summary>Activate or refresh a power-up.</summary>
    public void Activate(PowerUpType type)
    {
        int idx = (int)type;
        ref PowerUpState s = ref _states[idx];
        s.active   = true;
        s.timer    = effectSettings[idx].duration;
        // Shield: reset consumed on re-pickup
        if (type == PowerUpType.Shield) _shieldConsumed = false;
    }

    /// <summary>Called by PlayerRunner each frame to give Magnet the player speed.</summary>
    public void SetPlayerSpeed(float speed) => _currentPlayerSpeed = speed;

    /// <summary>Ramp values (0..1 smoothed) for each type.</summary>
    public float MagnetRamp      => Smoothed(0);
    public float ShieldRamp      => Smoothed(1);
    public float SpeedBoostRamp  => Smoothed(2);
    public float FlyRamp         => Smoothed(3);
    public float DoubleScoreRamp => Smoothed(4);

    /// <summary>True when Shield is active and has not yet been consumed.</summary>
    public bool ShieldActive => _states[1].active && !_shieldConsumed;

    /// <summary>
    /// Called when a shielded player hits an obstacle.
    /// Returns true if the shield absorbed the hit (obstacle should be hidden).
    /// </summary>
    public bool TryConsumeShield()
    {
        if (!ShieldActive) return false;
        _shieldConsumed        = true;
        _states[1].active      = false; // begin ramp-down
        return true;
    }

    /// <summary>Score multiplier (1→2) from DoubleScore ramp.</summary>
    public float ScoreMultiplier => Mathf.Lerp(1f, 2f, DoubleScoreRamp);

    /// <summary>Speed bonus from SpeedBoost ramp (fraction of base speed).</summary>
    public float SpeedBonusFraction => speedBoostFraction * SpeedBoostRamp;

    /// <summary>FOV bonus in degrees from SpeedBoost ramp.</summary>
    public float FOVBonus => speedBoostFOV * SpeedBoostRamp;

    /// <summary>Current fly height offset (0..flyHeight).</summary>
    public float FlyHeightOffset => flyHeight * FlyRamp;

    /// <summary>True if any fly ramp is above zero (invulnerable window).</summary>
    public bool FlyActive => _states[3].ramp > 0.001f || _states[3].active;

    /// <summary>Effective magnet radius scaled by ramp.</summary>
    public float MagnetEffectiveRadius => magnetRadius * MagnetRamp;

    // Called by Collectible.OnEnable / OnDisable to register in the pull list
    public void RegisterCollectible(Collectible c)
    {
        if (!_collectibles.Contains(c)) _collectibles.Add(c);
    }
    public void UnregisterCollectible(Collectible c) => _collectibles.Remove(c);

    // ─────────────────────────────────────────────
    //  Internal helpers
    // ─────────────────────────────────────────────

    // SmoothStep the raw ramp value
    private float Smoothed(int idx) => Mathf.SmoothStep(0f, 1f, _states[idx].ramp);

    private void TickMagnet(float dt)
    {
        if (GameManager.Instance == null) return;
        Transform playerTf = GameManager.Instance.PlayerTransform;
        if (playerTf == null) return;

        Vector3 playerPos = playerTf.position;
        float   radius    = MagnetEffectiveRadius;
        float   pullSpd   = (magnetPullSpeed + _currentPlayerSpeed) * MagnetRamp;

        for (int i = _collectibles.Count - 1; i >= 0; i--)
        {
            Collectible c = _collectibles[i];
            if (c == null || !c.gameObject.activeInHierarchy) continue;
            if (Vector3.Distance(c.transform.position, playerPos) <= radius)
            {
                c.transform.position = Vector3.MoveTowards(
                    c.transform.position, playerPos, pullSpd * dt);
            }
        }
    }

    private void UpdateShieldVisual()
    {
        if (shieldSphereRenderer == null) return;
        float a = ShieldRamp;
        bool show = a > 0.001f;
        if (shieldSphereRenderer.gameObject.activeSelf != show)
            shieldSphereRenderer.gameObject.SetActive(show);

        if (show)
        {
            shieldSphereRenderer.GetPropertyBlock(_shieldMPB);
            Color c = new Color(0.2f, 0.5f, 1f, 0.35f * a);
            _shieldMPB.SetColor(_alphaPropID, c);
            shieldSphereRenderer.SetPropertyBlock(_shieldMPB);

            float s = 0.5f + 0.5f * a;
            shieldSphereRenderer.transform.localScale = new Vector3(s, s, s);
        }
    }

    private void UpdateFlyVisual()
    {
        float r = FlyRamp;
        if (flyTrailRenderer != null)
        {
            flyTrailRenderer.enabled = r > 0.01f;
            flyTrailRenderer.time    = Mathf.Lerp(0f, 0.5f, r);
        }
        if (flyGlowParticles != null)
        {
            bool wantPlay = r > 0.01f;
            bool isPlaying = flyGlowParticles.isPlaying;
            if (wantPlay && !isPlaying) flyGlowParticles.Play();
            if (!wantPlay && isPlaying)  flyGlowParticles.Stop();
        }
    }

    private void UpdateSpeedLines()
    {
        if (speedLinesParticles == null) return;
        float r = SpeedBoostRamp;
        bool wantPlay = r > 0.01f;
        if (wantPlay && !speedLinesParticles.isPlaying) speedLinesParticles.Play();
        if (!wantPlay && speedLinesParticles.isPlaying) speedLinesParticles.Stop();
        if (wantPlay)
        {
            var main = speedLinesParticles.main;
            main.startSpeedMultiplier = Mathf.Lerp(1f, 4f, r);
        }
    }
}
