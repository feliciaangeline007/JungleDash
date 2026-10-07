// Game.cs - Jungle Dash central game manager and loop orchestrator.
using System;
using UnityEngine;

namespace JungleDash
{
    public enum GameState
    {
        Menu,
        Levels,
        Shop,
        Missions,
        DailyReward,
        Run,
        Pause,
        Dead,
        Victory
    }

    [Serializable]
    public struct LevelData
    {
        public int id;
        public string name;
        public string subtitle;
        public float targetDistance;
        public int coinReward;
        public int star2Coins;
        public int star3Coins;
        public Color hazeColor;
        public Color sunColor;
        public Color ambientColor;
        public Color pathColor;
        public Color curbColor;
        public Color foliageColor;
    }

    public enum MissionType
    {
        CollectCoins = 0,
        SlideUnder = 1,
        ReachDistance = 2,
        JumpCount = 3,
        UsePowerUp = 4
    }

    [Serializable]
    public class Mission
    {
        public int type;
        public string title;
        public int target;
        public int current;
        public int reward;
        public bool completed;
        public bool claimed;
    }

    public enum UpgradeType
    {
        Shield = 0,
        Magnet = 1,
        SpeedBoost = 2,
        Flight = 3,
        DoubleScore = 4
    }

    [Serializable]
    public struct SkinData
    {
        public int id;
        public string name;
        public string title;
        public int price;
        public Color primaryColor;
        public Color accentColor;
        public float smoothness;
    }

    public struct FloatingPopup
    {
        public string text;
        public float time;
        public float maxTime;
        public Color color;
        public Vector2 startPos;
        public float scale;
    }

    public static class GameConfig
    {
        // Physics & movement constants
        public const float LaneWidth = 2.3f;
        public const float SegmentLength = 42f;
        public const int SegmentCount = 5;
        public const float BaseSpeed = 12f;
        public const float MaxSpeed = 26f;
        public const float SpeedBoostMultiplier = 1.45f;
        public const float Acceleration = 0.12f;
        public const float Gravity = 38f;
        public const float JumpVelocity = 13.5f;
        public const float SlideDuration = 0.8f;
        public const float CoinMagnetRadius = 8.5f;

        // Upgrade settings
        public static readonly int[] UpgradeCosts = new int[] { 100, 250, 500, 1000 };
        public const int MaxUpgradeLevel = 5;

        // Base durations for power-ups (increases by +1.5s per upgrade level)
        public const float BaseShieldDuration = 10f;
        public const float BaseMagnetDuration = 8f;
        public const float BaseSpeedDuration = 6f;
        public const float BaseFlightDuration = 7.5f;
        public const float BaseDoubleDuration = 10f;

        // Skin definitions
        public static readonly SkinData[] Skins = new SkinData[]
        {
            new SkinData
            {
                id = 0,
                name = "Classic Explorer",
                title = "Legendary Archaeologist",
                price = 0,
                primaryColor = new Color(0.86f, 0.58f, 0.20f),
                accentColor = new Color(0.12f, 0.12f, 0.14f),
                smoothness = 0.25f
            },
            new SkinData
            {
                id = 1,
                name = "Shadow Hunter",
                title = "Master of Stealth",
                price = 500,
                primaryColor = new Color(0.12f, 0.14f, 0.17f),
                accentColor = new Color(0.92f, 0.20f, 0.20f),
                smoothness = 0.5f
            },
            new SkinData
            {
                id = 2,
                name = "Golden Champion",
                title = "Touched by the Sun",
                price = 1500,
                primaryColor = new Color(1f, 0.82f, 0.18f),
                accentColor = new Color(1f, 0.96f, 0.65f),
                smoothness = 0.85f
            },
            new SkinData
            {
                id = 3,
                name = "Cyber Nomad",
                title = "Temporal Traveler",
                price = 3000,
                primaryColor = new Color(0.08f, 0.72f, 0.92f),
                accentColor = new Color(0.90f, 0.18f, 0.95f),
                smoothness = 0.65f
            }
        };

        // Stages with custom biomes & atmospheric lighting
        public static readonly LevelData[] Stages = new LevelData[]
        {
            new LevelData
            {
                id = 1,
                name = "Bamboo Grove",
                subtitle = "Whispering Greens",
                targetDistance = 300f,
                coinReward = 100,
                star2Coins = 15,
                star3Coins = 30,
                hazeColor = new Color(0.62f, 0.82f, 0.74f),
                sunColor = new Color(1f, 0.95f, 0.78f),
                ambientColor = new Color(0.52f, 0.62f, 0.58f),
                pathColor = new Color(0.62f, 0.58f, 0.50f),
                curbColor = new Color(0.44f, 0.40f, 0.35f),
                foliageColor = new Color(0.24f, 0.65f, 0.20f)
            },
            new LevelData
            {
                id = 2,
                name = "Ancient Ruins",
                subtitle = "Forgotten Temples",
                targetDistance = 650f,
                coinReward = 200,
                star2Coins = 25,
                star3Coins = 55,
                hazeColor = new Color(0.72f, 0.78f, 0.68f),
                sunColor = new Color(1f, 0.90f, 0.70f),
                ambientColor = new Color(0.56f, 0.58f, 0.50f),
                pathColor = new Color(0.55f, 0.52f, 0.45f),
                curbColor = new Color(0.38f, 0.35f, 0.30f),
                foliageColor = new Color(0.32f, 0.55f, 0.25f)
            },
            new LevelData
            {
                id = 3,
                name = "River Rapids",
                subtitle = "Misty Waterfalls",
                targetDistance = 1100f,
                coinReward = 350,
                star2Coins = 40,
                star3Coins = 85,
                hazeColor = new Color(0.50f, 0.72f, 0.82f),
                sunColor = new Color(0.92f, 0.96f, 1f),
                ambientColor = new Color(0.42f, 0.55f, 0.65f),
                pathColor = new Color(0.48f, 0.52f, 0.55f),
                curbColor = new Color(0.32f, 0.36f, 0.40f),
                foliageColor = new Color(0.18f, 0.60f, 0.35f)
            },
            new LevelData
            {
                id = 4,
                name = "Canopy Treetop",
                subtitle = "Skyward Vines",
                targetDistance = 1600f,
                coinReward = 500,
                star2Coins = 60,
                star3Coins = 120,
                hazeColor = new Color(0.68f, 0.85f, 0.60f),
                sunColor = new Color(1f, 0.98f, 0.82f),
                ambientColor = new Color(0.48f, 0.65f, 0.45f),
                pathColor = new Color(0.58f, 0.55f, 0.45f),
                curbColor = new Color(0.36f, 0.38f, 0.30f),
                foliageColor = new Color(0.20f, 0.70f, 0.22f)
            },
            new LevelData
            {
                id = 5,
                name = "Shadow Gorge",
                subtitle = "Moonlit Chasm",
                targetDistance = 2200f,
                coinReward = 750,
                star2Coins = 85,
                star3Coins = 170,
                hazeColor = new Color(0.32f, 0.35f, 0.48f),
                sunColor = new Color(0.70f, 0.75f, 0.92f),
                ambientColor = new Color(0.25f, 0.28f, 0.40f),
                pathColor = new Color(0.40f, 0.38f, 0.45f),
                curbColor = new Color(0.25f, 0.24f, 0.30f),
                foliageColor = new Color(0.15f, 0.40f, 0.30f)
            },
            new LevelData
            {
                id = 6,
                name = "Volcano Escape",
                subtitle = "Molten Fury",
                targetDistance = 3000f,
                coinReward = 1200,
                star2Coins = 120,
                star3Coins = 240,
                hazeColor = new Color(0.75f, 0.42f, 0.32f),
                sunColor = new Color(1f, 0.70f, 0.45f),
                ambientColor = new Color(0.60f, 0.35f, 0.30f),
                pathColor = new Color(0.38f, 0.32f, 0.30f),
                curbColor = new Color(0.25f, 0.20f, 0.20f),
                foliageColor = new Color(0.45f, 0.30f, 0.15f)
            }
        };

        // Premium Color Tokens
        public static readonly Color GoldColor = new Color(1f, 0.82f, 0.15f);
        public static readonly Color GemColor = new Color(0.20f, 0.92f, 1f);
        public static readonly Color AccentGreen = new Color(0.25f, 0.85f, 0.45f);
        public static readonly Color AccentPurple = new Color(0.75f, 0.38f, 0.95f);
        public static readonly Color AccentRed = new Color(0.95f, 0.30f, 0.30f);
        public static readonly Color DarkBackdrop = new Color(0.06f, 0.08f, 0.10f, 0.88f);
        public static readonly Color CardBg = new Color(0.12f, 0.15f, 0.20f, 0.92f);
        public static readonly Color CardBorder = new Color(0.25f, 0.32f, 0.42f, 0.6f);
    }

    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (UnityEngine.Object.FindAnyObjectByType<Game>() != null) return;
            new GameObject("JungleDash").AddComponent<Game>();
        }
    }

    public class Game : MonoBehaviour
    {
        public static Game Instance { get; private set; }

        public GameState state = GameState.Menu;

        // Core subsystems
        public ProgressionManager Progression { get; private set; }
        public InputManager InputMgr { get; private set; }
        public CameraController CamCtrl { get; private set; }
        public VFX VisualFX { get; private set; }
        public HUD HudUI { get; private set; }
        public MenuUI MenuUIComp { get; private set; }
        public Player PlayerComp { get; private set; }
        public World WorldComp { get; private set; }
        public GameAudio AudioComp { get; private set; }

        Light sun;

        // Run variables
        float runSpeed;
        float runTime;
        float runDistance;
        int runCoins;
        int combo;
        float comboTimer;
        int lastMilestone;
        bool canRevive = true;
        bool isNewBest = false;
        int currentStageId = 0; // 0 = Endless, 1..6 = Stage

        // Active power-up timers
        float shieldTimer, maxShield;
        float magnetTimer, maxMagnet;
        float speedTimer, maxSpeed;
        float flyTimer, maxFly;
        float doubleTimer, maxDouble;
        float invulnerableTimer;

        void Awake()
        {
            if (Instance == null) Instance = this;

            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Input.multiTouchEnabled = false;

            // 1. Progression & Persistence
            Progression = new ProgressionManager();
            Progression.Load();

            // 2. Subsystems
            InputMgr = gameObject.AddComponent<InputManager>();
            CamCtrl = gameObject.AddComponent<CameraController>();
            VisualFX = gameObject.AddComponent<VFX>();
            HudUI = gameObject.AddComponent<HUD>();
            MenuUIComp = gameObject.AddComponent<MenuUI>();

            AudioComp = GetComponent<GameAudio>();
            if (AudioComp == null) AudioComp = gameObject.AddComponent<GameAudio>();
            AudioComp.Build();

            // 3. Environment & Lighting
            SetupLighting();
            CamCtrl.Setup(Camera.main);

            // 4. Game Assets & World & Player
            GameAssets ga = GameAssets.Load();
            // Fill all textures procedurally – no external image files needed
            if (ga == null) { ga = ScriptableObject.CreateInstance<GameAssets>(); }
            ProceduralAssets.FillAssets(ga);

            WorldComp = UnityEngine.Object.FindAnyObjectByType<World>();
            if (WorldComp == null) WorldComp = new GameObject("World").AddComponent<World>();
            WorldComp.Init(ga);
            WorldComp.OnPickup = HandlePickup;
            WorldComp.OnHit = HandleHit;

            PlayerComp = UnityEngine.Object.FindAnyObjectByType<Player>();
            if (PlayerComp == null) PlayerComp = new GameObject("Player").AddComponent<Player>();
            PlayerComp.Build(ga);
            PlayerComp.SetSkin(Progression.SelectedSkin);
            PlayerComp.OnLand = delegate
            {
                if (state == GameState.Run) AudioComp.Land();
            };

            // 5. Connect UI and Input Callbacks
            WireCallbacks();

            ToMenu();
        }

        void SetupLighting()
        {
            sun = UnityEngine.Object.FindAnyObjectByType<Light>();
            if (sun == null || sun.type != LightType.Directional)
            {
                GameObject l = new GameObject("Sun");
                sun = l.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.name = "Sun";
            // Warm golden dusk light — flatters both the amber stone and cyan glows
            sun.color     = new Color(1.00f, 0.88f, 0.62f);
            sun.intensity = 1.15f;
            sun.shadows   = LightShadows.Soft;
            sun.shadowStrength = 0.80f;
            sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            // Dark deep-jungle atmosphere — lets the emissive glows pop
            RenderSettings.skybox     = null;
            RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.20f, 0.28f, 0.24f);  // very dark green-grey
            RenderSettings.fog          = true;
            RenderSettings.fogMode      = FogMode.ExponentialSquared;
            RenderSettings.fogDensity   = 0.018f;
            RenderSettings.fogColor     = new Color(0.12f, 0.22f, 0.18f);  // deep jungle green fog
        }

        void WireCallbacks()
        {
            // Input
            InputMgr.OnSwipe = HandleSwipe;
            InputMgr.OnTap = delegate { if (state == GameState.Run) PlayerComp.Jump(); };
            InputMgr.OnPauseToggle = delegate
            {
                if (state == GameState.Run) PauseGame();
                else if (state == GameState.Pause) ResumeGame();
            };

            // Player
            PlayerComp.OnDoubleJump = delegate
            {
                if (state == GameState.Run) AudioComp.DoubleJump();
            };

            // HUD
            HudUI.OnPauseRequested = PauseGame;

            // MenuUI
            MenuUIComp.OnPlayEndless = delegate { StartRun(0); };
            MenuUIComp.OnPlayStage = delegate (int stId) { StartRun(stId); };
            MenuUIComp.OnOpenShop = delegate { state = GameState.Shop; AudioComp.Click(); };
            MenuUIComp.OnOpenLevels = delegate { state = GameState.Levels; AudioComp.Click(); };
            MenuUIComp.OnOpenMissions = delegate { state = GameState.Missions; AudioComp.Click(); };
            MenuUIComp.OnOpenDailyRewards = delegate { state = GameState.DailyReward; AudioComp.Click(); };
            MenuUIComp.OnSkinSelected = delegate (int skinId)
            {
                PlayerComp.SetSkin(skinId);
                AudioComp.Click();
            };
            MenuUIComp.OnBackToMenu = ToMenu;
            MenuUIComp.OnResume = ResumeGame;
            MenuUIComp.OnRestart = delegate { StartRun(currentStageId); };
            MenuUIComp.OnRevive = Revive;
            MenuUIComp.OnNextStage = delegate
            {
                if (currentStageId < GameConfig.Stages.Length) StartRun(currentStageId + 1);
                else ToMenu();
            };
            MenuUIComp.OnToggleSound = delegate
            {
                AudioComp.SoundOn = !AudioComp.SoundOn;
            };
        }

        public void ToMenu()
        {
            state = GameState.Menu;
            AudioComp.PlayMusic(false);
            AudioComp.PlayAmbientOnly(true);
            AudioComp.Click();
            Time.timeScale = 1f;

            if (VisualFX != null && PlayerComp != null)
            {
                VisualFX.SetSpeedLines(false, PlayerComp.transform.position);
                VisualFX.SetFlyTrail(false, PlayerComp.transform.position);
            }

            ApplyStageBiome(0);
            WorldComp.Reset();
            PlayerComp.ResetPlayer();
            PlayerComp.SetSkin(Progression.SelectedSkin);
        }

        public void StartRun(int stageId = 0)
        {
            currentStageId = stageId;
            state = GameState.Run;
            Time.timeScale = 1f;

            runSpeed = GameConfig.BaseSpeed;
            runTime = 0f;
            runDistance = 0f;
            runCoins = 0;
            combo = 0;
            comboTimer = 0f;
            lastMilestone = 0;
            canRevive = true;
            isNewBest = false;

            shieldTimer = 0f;
            magnetTimer = 0f;
            speedTimer = 0f;
            flyTimer = 0f;
            doubleTimer = 0f;
            invulnerableTimer = 0f;

            ApplyStageBiome(currentStageId);
            WorldComp.Reset();
            PlayerComp.ResetPlayer();
            PlayerComp.SetSkin(Progression.SelectedSkin);
            PlayerComp.running = true;

            // Apply Daily Login bonuses if active
            if (Progression.BonusShield)
            {
                Progression.BonusShield = false;
                ActivatePowerUp(UpgradeType.Shield);
                HudUI.ShowBanner("BONUS SHIELD ACTIVATED!");
            }
            if (Progression.BonusMagnet)
            {
                Progression.BonusMagnet = false;
                ActivatePowerUp(UpgradeType.Magnet);
                HudUI.ShowBanner("BONUS MAGNET ACTIVATED!");
            }
            if (Progression.BonusFly)
            {
                Progression.BonusFly = false;
                ActivatePowerUp(UpgradeType.Flight);
                HudUI.ShowBanner("BONUS FLIGHT ACTIVATED!");
            }

            AudioComp.PlayMusic(true);
            AudioComp.SetMusicPitch(1f);

            if (currentStageId > 0)
            {
                LevelData stage = GameConfig.Stages[currentStageId - 1];
                HudUI.ShowBanner(stage.name.ToUpper() + " — EXPEDITION START!");
            }
            else
            {
                HudUI.ShowBanner("ENDLESS DASH — GO!");
            }
        }

        public void PauseGame()
        {
            if (state != GameState.Run) return;
            state = GameState.Pause;
            Time.timeScale = 0f;
            AudioComp.Pause();
            AudioComp.PlayMusic(false);
        }

        public void ResumeGame()
        {
            if (state != GameState.Pause) return;
            state = GameState.Run;
            Time.timeScale = 1f;
            AudioComp.Click();
            AudioComp.PlayMusic(true);
        }

        void Revive()
        {
            if (!canRevive || state != GameState.Dead) return;
            canRevive = false;
            state = GameState.Run;
            Time.timeScale = 1f;

            PlayerComp.alive = true;
            PlayerComp.running = true;
            PlayerComp.transform.position = new Vector3((PlayerComp.lane - 1) * GameConfig.LaneWidth, 0f, PlayerComp.Z);

            ActivatePowerUp(UpgradeType.Shield);
            invulnerableTimer = 2.5f;

            HudUI.ShowBanner("REVIVED! EXPEDITION CONTINUES!");
            AudioComp.Revive();
            AudioComp.PlayMusic(true);
        }

        void Die()
        {
            if (state != GameState.Run) return;
            state = GameState.Dead;

            AudioComp.PlayMusic(false);
            AudioComp.Hit();
            AudioComp.GameOver();
            CamCtrl.AddShake(0.85f);
            CamCtrl.TriggerHitFlash();
            if (VisualFX != null)
            {
                VisualFX.SetSpeedLines(false, PlayerComp.transform.position);
                VisualFX.SetFlyTrail(false, PlayerComp.transform.position);
            }
            PlayerComp.Die();

            // Save records
            isNewBest = Progression.UpdateBestDistance(Mathf.FloorToInt(runDistance));
            Progression.AddCoins(runCoins);
            Progression.ReportMissionProgress((int)MissionType.ReachDistance, Mathf.FloorToInt(runDistance), true);
            Progression.Save();
        }

        void TriggerVictory(LevelData stage)
        {
            if (state != GameState.Run) return;
            state = GameState.Victory;

            AudioComp.PlayMusic(false);
            AudioComp.Win();
            if (VisualFX != null)
            {
                VisualFX.SetSpeedLines(false, PlayerComp.transform.position);
                VisualFX.SetFlyTrail(false, PlayerComp.transform.position);
            }
            PlayerComp.running = false;

            Progression.RecordStageClear(stage.id, runCoins);
        }

        void HandleSwipe(int dx, int dy)
        {
            if (state != GameState.Run) return;

            if (dx != 0)
            {
                if (PlayerComp.ChangeLane(dx))
                {
                    AudioComp.Slide();
                }
            }
            else if (dy > 0)
            {
                if (PlayerComp.Jump())
                {
                    AudioComp.Jump();
                    Progression.ReportMissionProgress((int)MissionType.JumpCount, 1);
                }
            }
            else if (dy < 0)
            {
                if (PlayerComp.Slide())
                {
                    AudioComp.Slide();
                    Progression.ReportMissionProgress((int)MissionType.SlideUnder, 1);
                }
            }
        }

        void HandlePickup(World.Item it)
        {
            if (state != GameState.Run) return;

            Vector3 worldPos = new Vector3(it.x, it.y, it.z);
            Vector2 screenPos = CamCtrl.Cam != null ? CamCtrl.Cam.WorldToScreenPoint(worldPos) : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            screenPos.y = Screen.height - screenPos.y; // Convert to GUI coords

            switch (it.kind)
            {
                case World.K.Coin:
                    int val = doubleTimer > 0f ? 2 : 1;
                    runCoins += val;
                    combo++;
                    comboTimer = 2.2f;
                    AudioComp.Coin(combo);
                    VisualFX.PlayCoinPickup(worldPos);
                    HudUI.AddPopup("+" + val, GameConfig.GoldColor, screenPos, 0.9f);
                    Progression.ReportMissionProgress((int)MissionType.CollectCoins, val);
                    break;

                case World.K.Gem:
                    int gVal = (doubleTimer > 0f ? 2 : 1) * 5;
                    runCoins += gVal;
                    AudioComp.Gem();
                    VisualFX.PlayGemPickup(worldPos);
                    HudUI.AddPopup("+★" + gVal, GameConfig.GemColor, screenPos, 1.3f);
                    Progression.ReportMissionProgress((int)MissionType.CollectCoins, gVal);
                    break;

                case World.K.Shield:
                    ActivatePowerUp(UpgradeType.Shield);
                    AudioComp.Power();
                    HudUI.AddPopup("DIVINE SHIELD!", new Color(0.3f, 0.65f, 1f), screenPos, 1.2f);
                    break;

                case World.K.Magnet:
                    ActivatePowerUp(UpgradeType.Magnet);
                    AudioComp.Power();
                    HudUI.AddPopup("COIN MAGNET!", new Color(1f, 0.35f, 0.35f), screenPos, 1.2f);
                    break;

                case World.K.Speed:
                    ActivatePowerUp(UpgradeType.SpeedBoost);
                    AudioComp.Speed();
                    HudUI.AddPopup("DASH SURGE!", new Color(1f, 0.65f, 0.1f), screenPos, 1.2f);
                    break;

                case World.K.Fly:
                    ActivatePowerUp(UpgradeType.Flight);
                    AudioComp.Fly();
                    HudUI.AddPopup("CANOPY FLIGHT!", new Color(0.85f, 0.45f, 1f), screenPos, 1.2f);
                    break;

                case World.K.Double:
                    ActivatePowerUp(UpgradeType.DoubleScore);
                    AudioComp.Double();
                    HudUI.AddPopup("2X BOUNTY!", new Color(1f, 0.9f, 0.2f), screenPos, 1.2f);
                    break;
            }
        }

        void HandleHit(World.Item it)
        {
            if (state != GameState.Run) return;

            Vector3 hitPos = new Vector3(it.x, it.y, it.z);

            // Shield or speed boost protects against obstacles
            if (shieldTimer > 0f || speedTimer > 0f || invulnerableTimer > 0f)
            {
                if (shieldTimer > 0f && speedTimer <= 0f)
                {
                    shieldTimer = 0f;
                    PlayerComp.SetShield(false);
                    VisualFX.PlayShieldBreak(PlayerComp.transform.position + Vector3.up * 0.9f);
                    AudioComp.ShieldBreak();
                    CamCtrl.AddShake(0.35f);
                    CamCtrl.TriggerHitFlash();
                    HudUI.ShowBanner("SHIELD BROKEN!");
                }
                else
                {
                    VisualFX.PlayObstacleHit(hitPos);
                    AudioComp.WoodBreak();
                    CamCtrl.AddShake(0.2f);
                    CamCtrl.TriggerHitFlash();
                }

                invulnerableTimer = 1.0f;
                WorldComp.Remove(it);
                return;
            }

            Die();
        }

        void ActivatePowerUp(UpgradeType type)
        {
            float dur = Progression.GetPowerUpDuration(type);
            switch (type)
            {
                case UpgradeType.Shield:
                    shieldTimer = dur;
                    maxShield = dur;
                    PlayerComp.SetShield(true);
                    break;

                case UpgradeType.Magnet:
                    magnetTimer = dur;
                    maxMagnet = dur;
                    break;

                case UpgradeType.SpeedBoost:
                    speedTimer = dur;
                    maxSpeed = dur;
                    PlayerComp.SetSpeed(true);
                    break;

                case UpgradeType.Flight:
                    flyTimer = dur;
                    maxFly = dur;
                    PlayerComp.SetFlying(true);
                    break;

                case UpgradeType.DoubleScore:
                    doubleTimer = dur;
                    maxDouble = dur;
                    PlayerComp.SetDouble(true);
                    break;
            }
        }

        void ApplyStageBiome(int stageId)
        {
            // Default: dark bioluminescent jungle
            Color haze   = new Color(0.12f, 0.22f, 0.18f);
            Color sunCol = new Color(1.00f, 0.88f, 0.62f);
            Color ambCol = new Color(0.20f, 0.28f, 0.24f);

            if (stageId > 0 && stageId <= GameConfig.Stages.Length)
            {
                LevelData stage = GameConfig.Stages[stageId - 1];
                // Darken all biome colours by 40% to stay in the dark-atmospheric theme
                haze   = stage.hazeColor   * 0.55f;
                sunCol = stage.sunColor;
                ambCol = stage.ambientColor * 0.50f;
            }

            CamCtrl.SetBackgroundColor(haze);
            RenderSettings.fogColor     = haze;
            RenderSettings.ambientLight = ambCol;
            if (sun != null) sun.color  = sunCol;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            InputMgr.Tick();

            if (state == GameState.Run)
            {
                TickRun(dt);
            }

            HudUI.Tick(dt);

            // Camera follow
            bool isRunning = state == GameState.Run;
            bool isFlying = PlayerComp.flying;
            bool isSpeed = speedTimer > 0f;
            bool isDead = state == GameState.Dead;
            CamCtrl.Tick(dt, PlayerComp.transform.position, isRunning, isFlying, isSpeed, isDead);
        }

        void TickRun(float dt)
        {
            runTime += dt;

            // Speed progression
            float targetSpeed = GameConfig.BaseSpeed + (runTime * GameConfig.Acceleration);
            if (speedTimer > 0f) targetSpeed *= GameConfig.SpeedBoostMultiplier;
            runSpeed = Mathf.Min(GameConfig.MaxSpeed * (speedTimer > 0f ? 1.3f : 1f), targetSpeed);

            runDistance += runSpeed * dt;

            // Power-up count-downs
            if (shieldTimer > 0f)
            {
                shieldTimer -= dt;
                if (shieldTimer <= 0f) PlayerComp.SetShield(false);
            }
            if (magnetTimer > 0f) magnetTimer -= dt;
            if (speedTimer > 0f)
            {
                speedTimer -= dt;
                if (speedTimer <= 0f) PlayerComp.SetSpeed(false);
            }
            if (flyTimer > 0f)
            {
                flyTimer -= dt;
                if (flyTimer <= 0f) PlayerComp.SetFlying(false);
            }
            if (doubleTimer > 0f)
            {
                doubleTimer -= dt;
                if (doubleTimer <= 0f) PlayerComp.SetDouble(false);
            }
            if (invulnerableTimer > 0f) invulnerableTimer -= dt;

            // Combo timer
            if (comboTimer > 0f)
            {
                comboTimer -= dt;
                if (comboTimer <= 0f)
                {
                    if (combo >= 5) AudioComp.ComboBreak();
                    combo = 0;
                }
            }

            // Milestone notifications (every 100m)
            int milestone = Mathf.FloorToInt(runDistance / 100f);
            if (milestone > lastMilestone)
            {
                lastMilestone = milestone;
                HudUI.ShowBanner("✦ " + (milestone * 100) + "m DISTANCE REACHED! ✦");
                AudioComp.Milestone();
            }

            // Check Stage Victory
            if (currentStageId > 0 && currentStageId <= GameConfig.Stages.Length)
            {
                LevelData stage = GameConfig.Stages[currentStageId - 1];
                if (runDistance >= stage.targetDistance)
                {
                    TriggerVictory(stage);
                    return;
                }
            }

            // Environmental and power-up VFX
            if (VisualFX != null && PlayerComp != null)
            {
                VisualFX.SetSpeedLines(speedTimer > 0f || runSpeed > 20f, PlayerComp.transform.position);
                VisualFX.SetFlyTrail(flyTimer > 0f, PlayerComp.transform.position);
                VisualFX.UpdateAmbientFireflies(PlayerComp.transform.position);
            }

            // World & Player ticks
            WorldComp.Tick(dt, PlayerComp.Z, runSpeed, true);
            PlayerComp.Tick(dt, runSpeed);
            WorldComp.Collide(PlayerComp, magnetTimer > 0f, invulnerableTimer > 0f || speedTimer > 0f, dt);

            // Dynamic music speed
            AudioComp.SetMusicPitch(1f + (runSpeed - GameConfig.BaseSpeed) * 0.012f);
        }

        void OnGUI()
        {
            // Fullscreen hit flash overlay
            if (CamCtrl != null && CamCtrl.HitFlashAlpha > 0.01f)
            {
                Color flashCol = new Color(1f, 0.25f, 0.25f, CamCtrl.HitFlashAlpha * 0.38f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), UIHelper.GetColorTexture(flashCol));
            }
            switch (state)
            {
                case GameState.Menu:
                    MenuUIComp.DrawMenu(Progression, AudioComp.SoundOn);
                    break;

                case GameState.Levels:
                    MenuUIComp.DrawStageSelect(Progression);
                    break;

                case GameState.Shop:
                    MenuUIComp.DrawShop(Progression);
                    break;

                case GameState.Missions:
                    MenuUIComp.DrawMissions(Progression);
                    break;

                case GameState.DailyReward:
                    MenuUIComp.DrawDailyRewards(Progression);
                    break;

                case GameState.Pause:
                    MenuUIComp.DrawPause();
                    break;

                case GameState.Dead:
                    MenuUIComp.DrawGameOver(runDistance, runCoins, Progression.BestDistance, isNewBest, canRevive);
                    break;

                case GameState.Victory:
                    int stars = Progression.GetStageStars(currentStageId);
                    MenuUIComp.DrawVictory(currentStageId, runCoins, stars);
                    break;

                case GameState.Run:
                    float targetDist = currentStageId > 0 ? GameConfig.Stages[currentStageId - 1].targetDistance : 0f;
                    HudUI.DrawHUD(
                        runDistance,
                        runCoins,
                        combo,
                        comboTimer,
                        currentStageId,
                        targetDist,
                        shieldTimer, maxShield,
                        magnetTimer, maxMagnet,
                        speedTimer, maxSpeed,
                        flyTimer, maxFly,
                        doubleTimer, maxDouble
                    );
                    break;
            }
        }
    }
}
