using UnityEngine;
using JungleDash;

public enum GameState { Menu, Playing, Paused, GameOver }

/// <summary>
/// Master coordinator for Jungle Dash (Unity 6000.6.3f1, Android, URP).
/// Bootstraps all game systems at runtime — no prefabs or manual scene setup needed.
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

    public GameState CurrentState { get; private set; } = GameState.Menu;
    public float CurrentSpeed     { get; private set; } = BaseSpeed;
    public float Distance         { get; private set; }
    public int   Score            { get; private set; }
    public int   Coins            { get; private set; }
    public int   HighScore        { get; private set; }
    public int   TotalCoins       { get; private set; }
    public bool  IsNewHighScore   { get; private set; }

    private float distAcc;
    private int   pickupPts;

    // ─── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Application.targetFrameRate = 60;
        HighScore   = PlayerPrefs.GetInt("JD_Hi", 0);
        TotalCoins  = PlayerPrefs.GetInt("JD_Coins", 0);

        SetupLighting();
        Bootstrap();
    }

    private void Start() => ShowMenu();

    // ─── Setup ─────────────────────────────────────────────────────────────────

    private void SetupLighting()
    {
        RenderSettings.fog          = true;
        RenderSettings.fogMode      = FogMode.Linear;
        RenderSettings.fogColor     = new Color(0.68f, 0.82f, 0.58f);
        RenderSettings.fogStartDistance = 35f;
        RenderSettings.fogEndDistance   = 115f;
        RenderSettings.ambientLight = new Color(0.65f, 0.75f, 0.55f);

        var light = Object.FindFirstObjectByType<Light>();
        if (light != null && light.type == LightType.Directional)
        {
            light.color     = new Color(1f, 0.94f, 0.78f);
            light.intensity = 1.3f;
            light.shadows   = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(46f, -32f, 0f);
        }
    }

    private void Bootstrap()
    {
        if (Object.FindFirstObjectByType<SoundManager>() == null)
            new GameObject("SoundManager").AddComponent<SoundManager>();

        if (Object.FindFirstObjectByType<PowerUpManager>() == null)
            new GameObject("PowerUpManager").AddComponent<PowerUpManager>();

        if (Object.FindFirstObjectByType<UIManager>() == null)
            new GameObject("UIManager").AddComponent<UIManager>();

        var spawnerGO = new GameObject("TrackSpawner");
        Spawner = spawnerGO.AddComponent<TrackSpawner>();

        var playerGO = new GameObject("Player");
        Player = playerGO.AddComponent<PlayerRunner>();
        Player.OnCrash     += OnPlayerCrash;
        Player.OnCollected += OnPlayerCollected;

        Camera cam = Camera.main;
        if (cam != null)
        {
            CameraSystem = cam.gameObject.GetComponent<CameraFollow>();
            if (CameraSystem == null) CameraSystem = cam.gameObject.AddComponent<CameraFollow>();
            CameraSystem.SetTarget(Player.transform);
            cam.backgroundColor = RenderSettings.fogColor;
            cam.clearFlags      = CameraClearFlags.SolidColor;
        }
    }

    // ─── Game State ────────────────────────────────────────────────────────────

    public void ShowMenu()
    {
        CurrentState = GameState.Menu;
        Time.timeScale = 1f;
        ResetRuntimeState();
        Spawner?.ResetTrack();
        Player?.StopRun();
    }

    public void StartGame()
    {
        CurrentState = GameState.Playing;
        Time.timeScale = 1f;
        ResetRuntimeState();
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

    private void ResetRuntimeState()
    {
        CurrentSpeed  = BaseSpeed;
        Distance      = 0f;
        Score         = 0;
        Coins         = 0;
        distAcc       = 0f;
        pickupPts     = 0;
        IsNewHighScore = false;
    }

    // ─── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        HandleKeys();
        if (CurrentState != GameState.Playing) return;

        float dt = Time.deltaTime;
        CurrentSpeed = Mathf.Min(MaxSpeed, CurrentSpeed + Accel * dt);

        float speed = CurrentSpeed;
        if (PowerUpManager.Instance != null && PowerUpManager.Instance.IsActive(PowerUpType.SpeedBoost))
            speed *= 1.45f;

        float dd = speed * dt;
        Distance += dd;

        if (Player != null)
        {
            Player.transform.Translate(Vector3.forward * dd, Space.World);
            Spawner?.UpdateSpawner(Player.transform.position.z);
        }

        float scoreMult = (PowerUpManager.Instance != null && PowerUpManager.Instance.IsActive(PowerUpType.DoubleScore)) ? 2f : 1f;
        distAcc += dd * scoreMult;
        Score    = Mathf.FloorToInt(distAcc) + pickupPts;
    }

    private void HandleKeys()
    {
        if (CurrentState == GameState.Playing)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow)  || Input.GetKeyDown(KeyCode.A)) Player?.MoveLeft();
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) Player?.MoveRight();
            if (Input.GetKeyDown(KeyCode.UpArrow)    || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W)) Player?.Jump();
            if (Input.GetKeyDown(KeyCode.Escape)     || Input.GetKeyDown(KeyCode.P)) PauseGame();
        }
        else if (CurrentState == GameState.Paused)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) ResumeGame();
        }
        else if (CurrentState == GameState.Menu || CurrentState == GameState.GameOver)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) StartGame();
        }
    }

    // ─── Event Handlers ────────────────────────────────────────────────────────

    private void OnPlayerCollected(Collectible c)
    {
        float mult = (PowerUpManager.Instance != null && PowerUpManager.Instance.IsActive(PowerUpType.DoubleScore)) ? 2f : 1f;
        switch (c.Type)
        {
            case CollectibleType.Coin:
                Coins++;  TotalCoins++;
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
