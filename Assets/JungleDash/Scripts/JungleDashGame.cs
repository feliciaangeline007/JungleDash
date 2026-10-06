// JungleDashGame.cs – Master coordinator for Jungle Dash
// Temple Run-style endless runner on a raised jungle bridge
// Unity 6000.6.3f1 · Android · URP 17
using UnityEngine;
using JungleDash;

public enum GameState { Menu, Playing, Paused, GameOver }

public class JungleDashGame : MonoBehaviour
{
    public static JungleDashGame Instance { get; private set; }

    // Speed ramp
    public const float BaseSpeed =  9.5f;
    public const float MaxSpeed  = 22.0f;
    public const float Accel     =  0.20f;

    // Runtime references
    public PlayerRunner Player       { get; private set; }
    public TrackSpawner Spawner      { get; private set; }
    public CameraFollow CameraSystem { get; private set; }

    // Game state
    public GameState CurrentState   { get; private set; } = GameState.Menu;
    public float     CurrentSpeed   { get; private set; } = BaseSpeed;
    public float     Distance       { get; private set; }
    public int       Score          { get; private set; }
    public int       Coins          { get; private set; }
    public int       HighScore      { get; private set; }
    public int       TotalCoins     { get; private set; }
    public bool      IsNewHighScore { get; private set; }

    private float distAcc;
    private int   pickupPts;

    // ── Lifecycle ──────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount  = 0;

        HighScore  = PlayerPrefs.GetInt("JD_Hi",    0);
        TotalCoins = PlayerPrefs.GetInt("JD_Coins", 0);

        SetupEnvironment();
        Bootstrap();
    }

    private void Start() => ShowMenu();

    // ── Environment ───────────────────────────────────────────────────────────
    private void SetupEnvironment()
    {
        // Temple atmosphere: dark, dense jungle, torch-lit
        RenderSettings.fog              = true;
        RenderSettings.fogMode          = FogMode.Linear;
        RenderSettings.fogColor         = new Color(0.10f, 0.14f, 0.10f); // near-black jungle fog
        RenderSettings.fogStartDistance = 30f;
        RenderSettings.fogEndDistance   = 90f;
        RenderSettings.ambientLight     = new Color(0.18f, 0.20f, 0.14f); // dim jungle ambient
        RenderSettings.ambientIntensity = 0.7f;

        var light = Object.FindFirstObjectByType<Light>();
        if (light != null && light.type == LightType.Directional)
        {
            light.color          = new Color(0.90f, 0.82f, 0.60f); // late-afternoon gold
            light.intensity      = 1.10f;
            light.shadows        = LightShadows.Soft;
            light.shadowStrength = 0.65f;
            light.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
        }
    }

    // ── Bootstrap ─────────────────────────────────────────────────────────────
    private void Bootstrap()
    {
        if (Object.FindFirstObjectByType<SoundManager>()   == null)
            new GameObject("SoundManager").AddComponent<SoundManager>();
        if (Object.FindFirstObjectByType<PowerUpManager>() == null)
            new GameObject("PowerUpManager").AddComponent<PowerUpManager>();
        if (Object.FindFirstObjectByType<UIManager>()      == null)
            new GameObject("UIManager").AddComponent<UIManager>();

        if (Spawner == null)
        {
            var go = new GameObject("TrackSpawner");
            Spawner = go.AddComponent<TrackSpawner>();
        }

        if (Player == null)
        {
            var go = new GameObject("Player");
            Player             = go.AddComponent<PlayerRunner>();
            Player.OnCrash    += HandleCrash;
            Player.OnCollected += HandleCollected;
        }

        var cam = Camera.main;
        if (cam != null)
        {
            CameraSystem = cam.GetComponent<CameraFollow>()
                        ?? cam.gameObject.AddComponent<CameraFollow>();
            CameraSystem.SetTarget(Player.transform);

            // Dark sky matching jungle fog
            cam.backgroundColor = new Color(0.06f, 0.08f, 0.06f);
            cam.clearFlags      = CameraClearFlags.SolidColor;
        }
    }

    // ── State machine ─────────────────────────────────────────────────────────
    public void ShowMenu()
    {
        CurrentState   = GameState.Menu;
        Time.timeScale = 1f;
        ResetStats();
        Spawner?.ResetTrack();
        Player?.StopRun();
    }

    public void StartGame()
    {
        CurrentState   = GameState.Playing;
        Time.timeScale = 1f;
        ResetStats();
        PowerUpManager.Instance?.ResetAll();
        Spawner?.ResetTrack();
        Player?.StartRun();
        if (CameraSystem != null && Player != null)
            CameraSystem.SetTarget(Player.transform);
    }

    public void PauseGame()
    {
        if (CurrentState != GameState.Playing) return;
        CurrentState   = GameState.Paused;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (CurrentState != GameState.Paused) return;
        CurrentState   = GameState.Playing;
        Time.timeScale = 1f;
    }

    private void ResetStats()
    {
        CurrentSpeed   = BaseSpeed;
        Distance       = 0f;
        Score          = 0;
        Coins          = 0;
        distAcc        = 0f;
        pickupPts      = 0;
        IsNewHighScore = false;
    }

    // ── Update ────────────────────────────────────────────────────────────────
    private void Update()
    {
        HandleKeys();
        if (CurrentState != GameState.Playing) return;

        float dt = Mathf.Min(Time.deltaTime, 0.05f);

        CurrentSpeed = Mathf.Min(MaxSpeed, CurrentSpeed + Accel * dt);

        bool  boosting = PowerUpManager.Instance != null
                      && PowerUpManager.Instance.IsActive(PowerUpType.SpeedBoost);
        float spd      = CurrentSpeed * (boosting ? 1.45f : 1f);
        float dd       = spd * dt;
        Distance      += dd;

        if (Player != null)
        {
            Player.transform.Translate(Vector3.forward * dd, Space.World);
            Spawner?.UpdateSpawner(Player.transform.position.z);
        }

        bool  doubling = PowerUpManager.Instance != null
                      && PowerUpManager.Instance.IsActive(PowerUpType.DoubleScore);
        distAcc += dd * (doubling ? 2f : 1f);
        Score    = Mathf.FloorToInt(distAcc) + pickupPts;
    }

    private void HandleKeys()
    {
        if (CurrentState == GameState.Playing)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow)  || Input.GetKeyDown(KeyCode.A))  Player?.MoveLeft();
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))  Player?.MoveRight();
            if (Input.GetKeyDown(KeyCode.UpArrow)    || Input.GetKeyDown(KeyCode.Space)
                                                     || Input.GetKeyDown(KeyCode.W))  Player?.Jump();
            if (Input.GetKeyDown(KeyCode.DownArrow)  || Input.GetKeyDown(KeyCode.S))  Player?.Slide();
            if (Input.GetKeyDown(KeyCode.Escape)     || Input.GetKeyDown(KeyCode.P))  PauseGame();
        }
        else if (CurrentState == GameState.Paused)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) ResumeGame();
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) StartGame();
        }
    }

    // ── Event handlers ────────────────────────────────────────────────────────
    private void HandleCollected(Collectible c)
    {
        bool  dbl  = PowerUpManager.Instance != null
                  && PowerUpManager.Instance.IsActive(PowerUpType.DoubleScore);
        float mult = dbl ? 2f : 1f;

        switch (c.Type)
        {
            case CollectibleType.Coin:
                Coins++; TotalCoins++;
                pickupPts += Mathf.RoundToInt(10 * mult);
                SoundManager.Instance?.PlayCoin();
                break;
            case CollectibleType.Gem:
                Coins += 5; TotalCoins += 5;
                pickupPts += Mathf.RoundToInt(50 * mult);
                SoundManager.Instance?.PlayGem();
                break;
            case CollectibleType.PowerUp:
                PowerUpManager.Instance?.Activate(c.PowerUpVariant);
                pickupPts += Mathf.RoundToInt(25 * mult);
                SoundManager.Instance?.PlayPowerUp();
                CameraSystem?.Shake(0.22f, 0.22f);
                break;
        }
    }

    private void HandleCrash()
    {
        CurrentState = GameState.GameOver;
        if (Score > HighScore)
        {
            HighScore      = Score;
            IsNewHighScore = true;
            PlayerPrefs.SetInt("JD_Hi", HighScore);
        }
        PlayerPrefs.SetInt("JD_Coins", TotalCoins);
        PlayerPrefs.Save();
    }
}
