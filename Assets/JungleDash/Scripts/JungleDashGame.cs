using UnityEngine;
using JungleDash;

public enum GameState { Menu, Playing, Paused, GameOver }

/// <summary>
/// Master coordinator for Jungle Dash (Unity 6000.6.3f1 · Android · URP).
/// Bootstraps all game systems at runtime – no prefabs or manual scene setup needed.
/// </summary>
public class JungleDashGame : MonoBehaviour
{
    public static JungleDashGame Instance { get; private set; }

    public const float BaseSpeed = 11.2f;
    public const float MaxSpeed  = 23.0f;
    public const float Accel     = 0.24f;

    public PlayerRunner Player       { get; private set; }
    public TrackSpawner Spawner      { get; private set; }
    public CameraFollow CameraSystem { get; private set; }

    public GameState CurrentState  { get; private set; } = GameState.Menu;
    public float     CurrentSpeed  { get; private set; } = BaseSpeed;
    public float     Distance      { get; private set; }
    public int       Score         { get; private set; }
    public int       Coins         { get; private set; }
    public int       HighScore     { get; private set; }
    public int       TotalCoins    { get; private set; }
    public bool      IsNewHighScore{ get; private set; }

    private float distAcc;
    private int   pickupPts;

    // ── Lifecycle ──────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Application.targetFrameRate = 60;
        HighScore  = PlayerPrefs.GetInt("JD_Hi",    0);
        TotalCoins = PlayerPrefs.GetInt("JD_Coins", 0);
        SetupLighting();
        Bootstrap();
    }

    private void Start() => ShowMenu();

    // ── Scene setup ───────────────────────────────────────────────────────────
    private void SetupLighting()
    {
        // Lush jungle atmosphere
        RenderSettings.fog              = true;
        RenderSettings.fogMode          = FogMode.Linear;
        RenderSettings.fogColor         = new Color(0.55f, 0.75f, 0.48f);
        RenderSettings.fogStartDistance = 40f;
        RenderSettings.fogEndDistance   = 120f;
        RenderSettings.ambientLight     = new Color(0.60f, 0.72f, 0.50f);
        RenderSettings.ambientIntensity = 1.1f;

        // Configure the Directional Light already in the scene
        var light = Object.FindFirstObjectByType<Light>();
        if (light != null && light.type == LightType.Directional)
        {
            light.color     = new Color(1.0f, 0.92f, 0.72f);   // warm sunlight
            light.intensity = 1.4f;
            light.shadows   = LightShadows.Soft;
            light.shadowStrength = 0.55f;
            light.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        }
    }

    private void Bootstrap()
    {
        // Ensure singletons exist
        if (Object.FindFirstObjectByType<SoundManager>()   == null) new GameObject("SoundManager").AddComponent<SoundManager>();
        if (Object.FindFirstObjectByType<PowerUpManager>() == null) new GameObject("PowerUpManager").AddComponent<PowerUpManager>();
        if (Object.FindFirstObjectByType<UIManager>()      == null) new GameObject("UIManager").AddComponent<UIManager>();

        // Track
        var spawnerGO = new GameObject("TrackSpawner");
        Spawner = spawnerGO.AddComponent<TrackSpawner>();

        // Player
        var playerGO = new GameObject("Player");
        Player = playerGO.AddComponent<PlayerRunner>();
        Player.OnCrash     += OnPlayerCrash;
        Player.OnCollected += OnPlayerCollected;

        // Camera
        Camera cam = Camera.main;
        if (cam != null)
        {
            CameraSystem = cam.GetComponent<CameraFollow>() ?? cam.gameObject.AddComponent<CameraFollow>();
            CameraSystem.SetTarget(Player.transform);

            // Sky colour matches fog
            cam.backgroundColor = new Color(0.45f, 0.68f, 0.88f);
            cam.clearFlags      = CameraClearFlags.SolidColor;
        }
    }

    // ── State machine ─────────────────────────────────────────────────────────
    public void ShowMenu()
    {
        CurrentState = GameState.Menu;
        Time.timeScale = 1f;
        ResetState();
        Spawner?.ResetTrack();
        Player?.StopRun();
    }

    public void StartGame()
    {
        CurrentState = GameState.Playing;
        Time.timeScale = 1f;
        ResetState();
        PowerUpManager.Instance?.ResetAll();
        Spawner?.ResetTrack();
        Player?.StartRun();
        if (CameraSystem != null && Player != null) CameraSystem.SetTarget(Player.transform);
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

    private void ResetState()
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

        float dt = Time.deltaTime;
        CurrentSpeed = Mathf.Min(MaxSpeed, CurrentSpeed + Accel * dt);

        float spd = CurrentSpeed *
            ((PowerUpManager.Instance != null && PowerUpManager.Instance.IsActive(PowerUpType.SpeedBoost)) ? 1.45f : 1f);

        float dd = spd * dt;
        Distance += dd;

        if (Player != null)
        {
            Player.transform.Translate(Vector3.forward * dd, Space.World);
            Spawner?.UpdateSpawner(Player.transform.position.z);
        }

        float mult = (PowerUpManager.Instance != null && PowerUpManager.Instance.IsActive(PowerUpType.DoubleScore)) ? 2f : 1f;
        distAcc += dd * mult;
        Score    = Mathf.FloorToInt(distAcc) + pickupPts;
    }

    private void HandleKeys()
    {
        if (CurrentState == GameState.Playing)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow)  || Input.GetKeyDown(KeyCode.A))     Player?.MoveLeft();
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))     Player?.MoveRight();
            if (Input.GetKeyDown(KeyCode.UpArrow)    || Input.GetKeyDown(KeyCode.Space)
                                                     || Input.GetKeyDown(KeyCode.W))     Player?.Jump();
            if (Input.GetKeyDown(KeyCode.Escape)     || Input.GetKeyDown(KeyCode.P))     PauseGame();
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

    // ── Events ────────────────────────────────────────────────────────────────
    private void OnPlayerCollected(Collectible c)
    {
        float mult = (PowerUpManager.Instance != null && PowerUpManager.Instance.IsActive(PowerUpType.DoubleScore)) ? 2f : 1f;
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
                CameraSystem?.Shake(0.2f, 0.2f);
                break;
        }
    }

    private void OnPlayerCrash()
    {
        CurrentState = GameState.GameOver;
        if (Score > HighScore) { HighScore = Score; IsNewHighScore = true; PlayerPrefs.SetInt("JD_Hi", HighScore); }
        PlayerPrefs.SetInt("JD_Coins", TotalCoins);
        PlayerPrefs.Save();
    }
}
