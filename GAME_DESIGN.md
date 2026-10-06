# 🎮 Jungle Dash - Game Design Document

**Platform:** Android (Unity 6000.6.3f1)  
**Genre:** Endless Runner 3D  
**Style:** Jungle/Forest Theme  
**Target:** Mobile Casual Players  
**Reference:** Web prototype di `artifacts/jungle-dash/`

---

## 📋 Table of Contents
1. [Game Overview](#game-overview)
2. [Core Mechanics](#core-mechanics)
3. [Visual Design](#visual-design)
4. [Game Features](#game-features)
5. [Technical Implementation](#technical-implementation)
6. [Standard Assets Integration](#standard-assets-integration)
7. [Android Optimization](#android-optimization)

---

## 🎯 Game Overview

### Concept
**Jungle Dash** adalah endless runner 3D dimana player berlari melalui hutan jungle sambil menghindari rintangan, mengumpulkan koin/gems, dan menggunakan power-ups.

### Core Loop
```
Start Run → Run Forward → Avoid Obstacles → Collect Items 
    ↓                                              ↓
Game Over ← Collision           Score Increase ←┘
    ↓
View Score → Restart
```

### Win Condition
- **None** (endless runner)
- Goal: Beat personal high score
- Secondary goal: Collect as many coins as possible

### Lose Condition
- Collision dengan obstacle
- Shield power-up melindungi dari 1 collision

---

## 🕹️ Core Mechanics

### 1. **Movement System**

#### Lane-Based Movement
```
Lane 0 (Left)    x = -3.0
Lane 1 (Center)  x =  0.0  ← Starting position
Lane 2 (Right)   x = +3.0
```

**Controls:**
- **Swipe Left** / **Left Arrow** / **A** → Move to left lane
- **Swipe Right** / **Right Arrow** / **D** → Move to right lane
- **Swipe Up** / **Space** / **Up Arrow** → Jump
- **Tap Pause Icon** / **ESC** / **P** → Pause game

**Movement Parameters:**
- Lane distance: `3.0` units
- Lane change speed: `10.0` (lerp speed)
- Lane change duration: ~0.3 seconds
- Smooth lerp interpolation

#### Jump Mechanics
```csharp
// Jump parameters
jumpPower = 12.0f;
gravity = -25.0f;
jumpDuration = ~1.0 second;
jumpHeight = ~2.5 units;
```

**Jump States:**
- Grounded → Can jump
- Jumping → Cannot jump again (no double jump)
- Falling → Player falls back to ground
- Landed → Return to grounded state

#### Auto-Forward Movement
```csharp
// Player always moves forward
baseSpeed = 11.2f;          // Starting speed
maxSpeed = 23.0f;           // Maximum speed
acceleration = 0.24f;       // Speed increase per second
currentSpeed = baseSpeed + (time * acceleration);
```

**Speed Formula:**
```
currentSpeed = min(maxSpeed, baseSpeed + (elapsedTime * acceleration))
```

### 2. **Procedural Track Generation**

#### Track Structure
```
┌─────────────────────┐
│   Lane 0  │  1  │ 2 │  ← 3 lanes
├─────────────────────┤
│     Ground Mesh     │  ← Visual track
├─────────────────────┤
│    Track Segments   │  ← Spawned ahead
└─────────────────────┘
      ↑ Player moves forward
```

**Segment Spawning:**
```csharp
segmentLength = 10.0f;           // Each segment 10 units
visibleSegments = 20;            // 20 segments visible
spawnDistance = 100.0f;          // Spawn when player reaches
despawnDistance = -20.0f;        // Remove when behind player
```

**Spawn Logic:**
```csharp
// Spawn new segment every 10 units
if (playerZ > lastSpawnZ - 10.0f)
{
    SpawnSegment(lastSpawnZ + 10.0f);
    lastSpawnZ += 10.0f;
}
```

#### Row Generation
Each segment has 1 row with 3 lanes:
```
Row Structure:
┌─────┬─────┬─────┐
│ L0  │ L1  │ L2  │  ← Each lane can have:
└─────┴─────┴─────┘    - Obstacle
                        - Coin
                        - Gem
                        - Power-up
                        - Empty
```

**Spawn Probabilities:**
```csharp
obstacleSpawnChance = 0.67f;     // 67% per lane
coinLineSpawnChance = 0.84f;     // 84% for coin line
gemSpawnChance = 0.15f;          // 15% for gem
powerupSpawnChance = 0.22f;      // 22% for power-up
```

**Spawn Rules:**
- Minimum 1 lane must be clear (player can pass)
- Obstacles distributed across lanes
- Coin lines span multiple segments
- Power-ups are rare and valuable

### 3. **Collision System**

#### Collision Types
```csharp
// Player collider
playerCollider: CapsuleCollider
    height = 2.0f
    radius = 0.4f
    center = (0, 1, 0)

// Obstacle colliders
obstacleCollider: BoxCollider
    size = (1.5, 2.0, 1.5)
    isTrigger = false

// Pickup colliders
pickupCollider: SphereCollider
    radius = 0.8f
    isTrigger = true
```

**Collision Detection:**
```csharp
void OnCollisionEnter(Collision collision)
{
    if (collision.gameObject.CompareTag("Obstacle"))
    {
        if (hasShield)
        {
            DeactivateShield();
            PlaySound("shield");
        }
        else
        {
            GameOver();
            PlaySound("crash");
        }
    }
}

void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("Coin"))
    {
        CollectCoin(5);
        PlaySound("coin");
        Destroy(other.gameObject);
    }
    else if (other.CompareTag("Gem"))
    {
        CollectGem(25);
        PlaySound("gem");
        Destroy(other.gameObject);
    }
    else if (other.CompareTag("Powerup"))
    {
        ActivatePowerup(other.GetComponent<PowerupPickup>().type);
        PlaySound("powerup");
        Destroy(other.gameObject);
    }
}
```

### 4. **Power-Up System**

#### Available Power-ups
```
1. 🧲 MAGNET   - Auto-collect nearby coins
2. 🛡️ SHIELD   - Protect from ONE obstacle hit
3. ⚡ SPEED    - Increased running speed
4. ✈️ FLY      - Fly over obstacles
5. ⭐ DOUBLE   - 2x score multiplier
```

#### Power-up Properties
```csharp
// Magnet
magnetDuration = 8.0f;          // 8 seconds
magnetRange = 5.0f;             // 5 units radius
magnetPullSpeed = 15.0f;        // Pull speed

// Shield
shieldDuration = 9.0f;          // 9 seconds
shieldHits = 1;                 // Blocks 1 collision

// Speed Boost
speedBoostDuration = 7.0f;      // 7 seconds
speedBoostMultiplier = 1.5f;    // 50% faster

// Fly
flyDuration = 8.0f;             // 8 seconds
flyHeight = 4.0f;               // Hover height
ignoreObstacles = true;         // No collision

// Double Score
doubleDuration = 10.0f;         // 10 seconds
scoreMultiplier = 2.0f;         // 2x points
```

#### Power-up Stacking
```csharp
// Multiple power-ups can be active simultaneously
activePowerups = List<Powerup>
{
    { type, remainingTime, ... }
}

// Refresh duration if same type collected
if (HasActivePowerup(type))
    RefreshDuration(type);
else
    AddPowerup(type);
```

---

## 🎨 Visual Design

### Art Style
- **Theme:** Tropical Jungle/Forest
- **Color Palette:**
  - Foliage: Vibrant greens (#2d8541, #4a9b5e)
  - Ground: Muddy browns (#6b4423, #8b5a2b)
  - Sky: Clear blue (#87ceeb) with fog
  - Coins: Gold (#ffd700)
  - Gems: Purple (#9b59b6)

### Environment

#### Ground/Track
```
Material: Mud/Rocky texture
Texture: MudRocky.png (from Resources)
Size: Width 9 units (3 lanes × 3 units)
Length: Infinitely tiled
Color: Brown tones
Shader: URP/Lit or Mobile/Diffuse
```

#### Trees/Foliage
```
Palm Trees:
- Billboard sprites or 3D models
- Placement: Sides of track (x = ±6)
- Height variation: 4-8 units
- Density: Every 5-10 units

Broadleaf Trees:
- Similar to palms, different texture
- Mixed with palms for variety
```

#### Lighting
```
Directional Light (Sun):
- Rotation: (50, -30, 0)
- Color: Warm white (#fff5e1)
- Intensity: 1.0
- Shadow: Soft shadows

Ambient Light:
- Mode: Skybox or Flat
- Color: Light blue-grey
- Intensity: 0.3

Fog:
- Enabled: Yes
- Mode: Exponential
- Color: Light blue-white (#b8d4e8)
- Density: 0.015
- Start: 20 units
- End: 100 units
```

### Camera Setup
```
Position: (0, 5, -8)      // Behind and above player
Rotation: (30, 0, 0)      // Looking down at player
FOV: 60 degrees
Near Clip: 0.3
Far Clip: 150

Follow behavior:
- Smooth follow player Z position
- Fixed X offset (centered)
- Fixed Y height
- Look slightly ahead of player
```

### Models

#### Player Character
```
Option A: Ethan from Standard Assets
- Humanoid rig
- Third person character
- Running/jumping animations

Option B: Ethan.fbx from Resources
- Custom character model
- Requires animation setup
```

#### Obstacles
```
Types:
1. Fallen Log
   - Cylinder mesh
   - Wood texture
   - Size: (1.5, 0.6, 1.5)

2. Rock
   - Sphere or custom mesh
   - Stone texture
   - Size: (1.5, 1.5, 1.5)

3. Tree Stump
   - Cylinder mesh
   - Wood texture
   - Size: (1.2, 1.0, 1.2)
```

#### Collectibles
```
Coin:
- Mesh: Cylinder (flat) or Sphere
- Color: Gold (#ffd700)
- Size: 0.5 units
- Animation: Rotate on Y axis
- Emission: Slight glow

Gem:
- Mesh: Diamond shape or Sphere
- Color: Purple (#9b59b6)
- Size: 0.6 units
- Animation: Rotate + Bob up/down
- Emission: Glow effect

Power-up:
- Mesh: Cube or custom icon
- Color: By type (magnet=red, shield=blue, etc.)
- Size: 0.8 units
- Animation: Rotate + Floating
- VFX: Particle effect
```

---

## ⚙️ Game Features

### UI System (Indonesian Localization)

#### Main Menu
```
┌──────────────────────────┐
│   JUNGLE DASH           │
│                          │
│   [MULAI BERLARI]       │  ← Start button
│                          │
│   High Score: 1234      │
│   Coins: 567            │
└──────────────────────────┘
```

#### In-Game HUD
```
Top Left:
┌──────────────┐
│ SKOR: 1234   │  ← Score
│ JARAK: 56m   │  ← Distance
│              │
│ 🧲 05.2s     │  ← Active power-ups
│ ⭐ 08.1s     │    with timers
└──────────────┘

Top Right:
┌──────────┐
│ [PAUSE]  │  ← Pause button
└──────────┘

Bottom Center:
┌─────────────────────┐
│  [ ← ]  [LOMPAT] [ → ] │  ← Touch controls
└─────────────────────┘
```

#### Game Over Screen
```
┌──────────────────────────┐
│   GAME OVER             │
│                          │
│   SKOR: 1234            │
│   JARAK: 56 meter       │
│   KOIN: 12              │
│                          │
│   SKOR TERTINGGI: 2000  │
│                          │
│   [MAIN LAGI]           │  ← Restart
└──────────────────────────┘
```

#### Pause Menu
```
┌──────────────────────────┐
│   DIJEDA                │
│                          │
│   [LANJUTKAN]           │  ← Resume
│   [MAIN LAGI]           │  ← Restart
│   [KELUAR]              │  ← Exit
│                          │
│   [🔊 SUARA ON/OFF]     │  ← Sound toggle
└──────────────────────────┘
```

### Scoring System

#### Points
```csharp
// Distance-based score
scoreFromDistance = (int)distance;   // 1 point per meter

// Collectibles
coinValue = 5;                       // +5 per coin
gemValue = 25;                       // +25 per gem

// Score multiplier from power-ups
if (hasDoubleScore)
    earnedPoints *= 2;

// Total score
totalScore = scoreFromDistance + coinScore * multiplier;
```

#### High Score
```csharp
// Persistent storage
PlayerPrefs.SetInt("HighScore", score);
highScore = PlayerPrefs.GetInt("HighScore", 0);

// Update on game over
if (currentScore > highScore)
{
    highScore = currentScore;
    SaveHighScore();
    ShowNewHighScoreEffect();
}
```

### Sound System

#### Sound Effects
```
coin.wav       - Coin collect (ding)
gem.wav        - Gem collect (sparkle)
powerup.wav    - Power-up collected
jump.wav       - Jump action
shield.wav     - Shield block hit
crash.wav      - Game over collision
```

#### Audio Mixer
```csharp
// Master volume
masterVolume = 1.0f;

// SFX volume
sfxVolume = 0.8f;

// Play sound
AudioSource.PlayOneShot(clip, volume);
```

#### Sound Toggle
```csharp
// UI button to toggle sound
bool soundEnabled = true;

void ToggleSound()
{
    soundEnabled = !soundEnabled;
    AudioListener.volume = soundEnabled ? 1.0f : 0.0f;
    PlayerPrefs.SetInt("SoundEnabled", soundEnabled ? 1 : 0);
}
```

### Animation System

#### Player Animations
```
Idle:
- State: Standing still
- Used: Menu screen
- Loop: Yes

Run:
- State: Running forward
- Used: Normal gameplay
- Speed: Sync with game speed
- Loop: Yes

Jump:
- State: In air
- Used: When jumping
- Duration: ~0.5 seconds
- Transition: Run → Jump → Run
- Loop: No

Fall/Landing:
- State: Falling down
- Used: After jump peak
- Transition: Jump → Fall → Run
- Loop: No

Crash:
- State: Hit obstacle
- Used: Game over
- Duration: 1.0 second
- Loop: No
```

#### Animator Controller
```
States:
┌────────┐
│  Idle  │ ──StartRun──> ┌──────┐
└────────┘                │  Run │ <──Land─┐
                          └──────┘         │
                            │              │
                          Jump           Falling
                            │              │
                            ↓              │
                          ┌──────┐        │
                          │ Jump │ ───────┘
                          └──────┘

Parameters:
- isRunning (bool)
- isJumping (bool)
- isGrounded (bool)
- speed (float)
```

### Particle Effects

#### Coin Collect Effect
```csharp
// Burst particles when collecting coin
ParticleSystem coinBurst = Instantiate(coinParticlePrefab, position);
coinBurst.Play();
Destroy(coinBurst.gameObject, 1.0f);

// Particle properties
startLifetime = 0.5f;
startSize = 0.2f;
startColor = Gold
emissionRate = 10 particles
shape = Sphere (radius 0.5)
```

#### Power-up Aura
```csharp
// Continuous aura around player
ParticleSystem aura = player.GetComponentInChildren<ParticleSystem>();

// Shield aura
aura.startColor = Blue;
aura.emissionRate = 20;

// Magnet aura
aura.startColor = Red;
aura.emissionRate = 15;
```

---

## 🔧 Technical Implementation

### Project Structure
```
Assets/
├── Scenes/
│   └── Main.unity                  # Main game scene
├── JungleDash/
│   ├── Scripts/
│   │   ├── JungleDashGame.cs      # Main game controller
│   │   ├── PlayerRunner.cs         # Player movement
│   │   ├── TrackSpawner.cs         # Procedural generation
│   │   ├── CameraFollow.cs         # Camera system
│   │   ├── PowerUpManager.cs       # Power-up logic
│   │   ├── Obstacle.cs             # Obstacle behavior
│   │   ├── Collectible.cs          # Coin/gem behavior
│   │   ├── PowerUpPickup.cs        # Power-up behavior
│   │   ├── UIManager.cs            # UI controller
│   │   └── GameManager.cs          # Game state
│   ├── Prefabs/
│   │   ├── Obstacles/
│   │   │   ├── Log.prefab
│   │   │   ├── Rock.prefab
│   │   │   └── Stump.prefab
│   │   ├── Collectibles/
│   │   │   ├── Coin.prefab
│   │   │   ├── Gem.prefab
│   │   │   └── PowerUp.prefab
│   │   ├── Player.prefab
│   │   └── TrackSegment.prefab
│   ├── Materials/
│   │   ├── Ground.mat
│   │   ├── Coin.mat
│   │   ├── Gem.mat
│   │   └── Obstacle.mat
│   └── Resources/
│       ├── MudRocky.png
│       ├── Ethan.fbx
│       ├── Animations/
│       └── Sounds/
└── Standard Assets/              # Imported package
    ├── Characters/
    ├── CrossPlatformInput/
    └── Cameras/
```

### Core Classes Architecture

#### JungleDashGame.cs (Main Controller)
```csharp
public class JungleDashGame : MonoBehaviour
{
    // Singleton
    public static JungleDashGame Instance { get; private set; }
    
    // References
    public PlayerRunner player;
    public TrackSpawner trackSpawner;
    public CameraFollow cameraFollow;
    public UIManager uiManager;
    public PowerUpManager powerUpManager;
    
    // Game state
    public GameState currentState;
    public float currentSpeed;
    public int score;
    public int coins;
    public float distance;
    
    // Methods
    void Awake() { /* Singleton setup */ }
    void Start() { /* Initialize game */ }
    void Update() { /* Game loop */ }
    public void StartGame() { /* Begin run */ }
    public void PauseGame() { /* Pause */ }
    public void ResumeGame() { /* Resume */ }
    public void GameOver() { /* End run */ }
    public void RestartGame() { /* Restart */ }
    private void UpdateScore() { /* Calculate score */ }
}

public enum GameState { Menu, Playing, Paused, GameOver }
```

#### PlayerRunner.cs
```csharp
public class PlayerRunner : MonoBehaviour
{
    // Movement
    public float laneDistance = 3f;
    public float laneChangeSpeed = 10f;
    private int currentLane = 1;
    private float targetX;
    
    // Jump
    public float jumpPower = 12f;
    private bool isGrounded = true;
    private Rigidbody rb;
    
    // Components
    private Animator animator;
    private CapsuleCollider collider;
    
    void Start() { /* Setup */ }
    void Update() { /* Handle input */ }
    void FixedUpdate() { /* Physics movement */ }
    
    public void MoveLeft() { /* Switch lane left */ }
    public void MoveRight() { /* Switch lane right */ }
    public void Jump() { /* Jump */ }
    
    void OnCollisionEnter(Collision col) { /* Collision */ }
    void OnTriggerEnter(Collider col) { /* Pickup */ }
}
```

#### TrackSpawner.cs
```csharp
public class TrackSpawner : MonoBehaviour
{
    // Spawning
    public float segmentLength = 10f;
    public int visibleSegments = 20;
    private float spawnZ = 0f;
    private Queue<GameObject> activeSegments;
    
    // Prefabs
    public GameObject[] obstaclePrefabs;
    public GameObject coinPrefab;
    public GameObject gemPrefab;
    public GameObject[] powerUpPrefabs;
    
    void Start() { /* Initialize */ }
    void Update() { /* Spawn/despawn segments */ }
    
    private void SpawnSegment(float z) { /* Create segment */ }
    private void DespawnSegment() { /* Remove old segment */ }
    private void SpawnObstacle(int lane, float z) { /* Place obstacle */ }
    private void SpawnCollectible(int lane, float z) { /* Place pickup */ }
}
```

#### PowerUpManager.cs
```csharp
public class PowerUpManager : MonoBehaviour
{
    // Active power-ups
    private Dictionary<PowerUpType, float> activePowerUps;
    
    // Power-up effects
    public GameObject magnetAura;
    public GameObject shieldAura;
    
    void Update() { /* Tick power-up timers */ }
    
    public void ActivatePowerUp(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.Magnet:
                StartMagnet();
                break;
            // etc...
        }
    }
    
    public bool HasPowerUp(PowerUpType type) { /* Check active */ }
    private void DeactivatePowerUp(PowerUpType type) { /* Remove */ }
}

public enum PowerUpType { Magnet, Shield, Speed, Fly, Double }
```

---

## 🎮 Standard Assets Integration

### Using Third Person Character

```csharp
// Replace PlayerRunner with ThirdPersonCharacter
using UnityStandardAssets.Characters.ThirdPerson;

public class JungleDashPlayer : ThirdPersonCharacter
{
    private int currentLane = 1;
    private float laneDistance = 3f;
    
    protected override void Update()
    {
        base.Update();
        
        // Custom lane movement
        float targetX = (currentLane - 1) * laneDistance;
        Vector3 targetPos = new Vector3(targetX, transform.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 10f);
    }
    
    public void MoveLeft()
    {
        if (currentLane > 0) currentLane--;
    }
    
    public void MoveRight()
    {
        if (currentLane < 2) currentLane++;
    }
}
```

### Mobile Input with CrossPlatformInput

```csharp
using UnityStandardAssets.CrossPlatformInput;

void Update()
{
    // Get input from virtual joystick or keyboard
    if (CrossPlatformInputManager.GetButtonDown("Jump"))
    {
        Jump();
    }
    
    float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
    if (horizontal < -0.5f)
        MoveLeft();
    else if (horizontal > 0.5f)
        MoveRight();
}
```

### Camera Setup with FreeLookCam

```csharp
// Use FreeLookCam from Standard Assets
// In scene hierarchy:
// - FreeLookCameraRig (prefab from Standard Assets)
//   - Set target to Player transform
//   - Configure follow distance
//   - Set rotation speed

// Script to customize:
public class JungleDashCamera : FreeLookCam
{
    protected override void FollowTarget(float deltaTime)
    {
        base.FollowTarget(deltaTime);
        
        // Add custom camera behavior
        // e.g., look ahead of player
        Vector3 lookAhead = m_Target.position + m_Target.forward * 5f;
        transform.LookAt(lookAhead);
    }
}
```

---

## 📱 Android Optimization

### Performance Targets
```
Target FPS: 60 (minimum 30)
Max Draw Calls: 100
Max Triangles: 50,000
Texture Memory: < 100 MB
APK Size: < 150 MB
```

### Optimization Strategies

#### 1. Object Pooling
```csharp
public class ObjectPool : MonoBehaviour
{
    public GameObject prefab;
    public int poolSize = 20;
    private Queue<GameObject> pool;
    
    void Start()
    {
        pool = new Queue<GameObject>();
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }
    
    public GameObject Get()
    {
        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        return Instantiate(prefab);
    }
    
    public void Return(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}
```

#### 2. LOD (Level of Detail)
```csharp
// Use LOD Group for trees and obstacles
LODGroup lodGroup = tree.AddComponent<LODGroup>();
LOD[] lods = new LOD[3];
lods[0] = new LOD(0.6f, highDetailRenderers);  // Close
lods[1] = new LOD(0.3f, mediumDetailRenderers); // Medium
lods[2] = new LOD(0.1f, lowDetailRenderers);   // Far
lodGroup.SetLODs(lods);
```

#### 3. Occlusion Culling
```
Window → Rendering → Occlusion Culling
- Bake occlusion data
- Only render visible objects
```

#### 4. Texture Compression
```
Import Settings for textures:
- Max Size: 1024 or 512 for mobile
- Compression: ASTC (Android)
- Generate Mipmaps: Yes
- Filter Mode: Trilinear
```

#### 5. Mobile Shaders
```csharp
// Use mobile-optimized shaders
Material.shader = Shader.Find("Mobile/Diffuse");

// From Standard Assets:
"Mobile/Bumped Specular"
"Mobile/Particles/Additive"
"Mobile/Unlit (Supports Lightmap)"
```

#### 6. Batching
```
Edit → Project Settings → Player → Other Settings
- Static Batching: ✓ Enable
- Dynamic Batching: ✓ Enable
- GPU Skinning: ✓ Enable (for characters)
```

#### 7. Reduce Shadow Quality
```
Edit → Project Settings → Quality → Android
- Shadows: Hard Shadows Only
- Shadow Resolution: Medium Resolution
- Shadow Distance: 20
- Shadow Cascades: No Cascades
```

### Build Settings for Android
```
File → Build Settings → Android
Player Settings → Other Settings:

Graphics:
- Graphics API: OpenGLES3
- Multithreaded Rendering: ✓

Optimization:
- Managed Stripping Level: Medium
- Strip Engine Code: ✓
- Vertex Compression: Everything
- Optimize Mesh Data: ✓

Configuration:
- Scripting Backend: IL2CPP
- Target Architectures: ARM64
- API Compatibility: .NET Standard 2.1
```

### Mobile Input UI

#### Virtual Buttons
```csharp
// Add from Standard Assets/CrossPlatformInput/Prefabs
// - MobileSingleStickControl.prefab
// - Or create custom buttons:

public class VirtualButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public string buttonName;
    
    public void OnPointerDown(PointerEventData data)
    {
        CrossPlatformInputManager.SetButtonDown(buttonName);
    }
    
    public void OnPointerUp(PointerEventData data)
    {
        CrossPlatformInputManager.SetButtonUp(buttonName);
    }
}
```

#### Swipe Detection
```csharp
public class SwipeDetector : MonoBehaviour
{
    private Vector2 startTouch;
    private Vector2 swipeDelta;
    private float minSwipeDistance = 50f;
    
    void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                startTouch = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                swipeDelta = touch.position - startTouch;
                
                if (swipeDelta.magnitude > minSwipeDistance)
                {
                    float x = swipeDelta.x;
                    float y = swipeDelta.y;
                    
                    if (Mathf.Abs(x) > Mathf.Abs(y))
                    {
                        // Horizontal swipe
                        if (x > 0)
                            OnSwipeRight();
                        else
                            OnSwipeLeft();
                    }
                    else
                    {
                        // Vertical swipe
                        if (y > 0)
                            OnSwipeUp();
                    }
                }
            }
        }
    }
    
    void OnSwipeLeft() { player.MoveLeft(); }
    void OnSwipeRight() { player.MoveRight(); }
    void OnSwipeUp() { player.Jump(); }
}
```

---

## ✅ Implementation Checklist

### Phase 1: Core Gameplay (Week 1)
- [ ] Scene setup dengan Camera, Light, Ground
- [ ] Player movement (lane switching)
- [ ] Player jump mechanic
- [ ] Auto-forward movement
- [ ] Basic collision detection
- [ ] Game state management (Menu, Playing, GameOver)

### Phase 2: Track Generation (Week 1)
- [ ] Procedural track spawning
- [ ] Obstacle placement
- [ ] Object pooling system
- [ ] Despawn old segments
- [ ] Ground texture tiling

### Phase 3: Collectibles (Week 2)
- [ ] Coin implementation
- [ ] Gem implementation
- [ ] Collect trigger detection
- [ ] Score calculation
- [ ] Visual effects on collect

### Phase 4: Power-ups (Week 2)
- [ ] Magnet power-up
- [ ] Shield power-up
- [ ] Speed boost power-up
- [ ] Fly power-up
- [ ] Double score power-up
- [ ] Power-up UI timer display

### Phase 5: Visuals & Polish (Week 3)
- [ ] Scenery (trees, foliage)
- [ ] Lighting and fog
- [ ] Particle effects
- [ ] Player animations
- [ ] Obstacle variety
- [ ] Camera follow smooth

### Phase 6: UI & UX (Week 3)
- [ ] Main menu UI
- [ ] In-game HUD (score, distance, power-ups)
- [ ] Game Over screen
- [ ] Pause menu
- [ ] Indonesian localization
- [ ] Sound effects
- [ ] Sound toggle

### Phase 7: Standard Assets Integration (Week 4)
- [ ] Import Standard Assets package
- [ ] Setup ThirdPersonCharacter
- [ ] Configure animations
- [ ] Setup CrossPlatformInput
- [ ] Add mobile virtual buttons
- [ ] Implement swipe controls
- [ ] Setup FreeLookCam

### Phase 8: Android Optimization (Week 4)
- [ ] Object pooling for all entities
- [ ] LOD for trees/obstacles
- [ ] Texture compression
- [ ] Mobile shaders
- [ ] Static batching
- [ ] Shadow quality reduction
- [ ] Build settings optimization

### Phase 9: Testing & Polish (Week 5)
- [ ] Test on Android devices
- [ ] Performance profiling
- [ ] Fix bugs
- [ ] Balance difficulty
- [ ] Tune spawn rates
- [ ] Final polish

### Phase 10: Release Preparation (Week 5)
- [ ] App icon
- [ ] Splash screen
- [ ] Build signed APK
- [ ] Test installation
- [ ] Google Play store assets
- [ ] Privacy policy
- [ ] Submit to Play Store

---

## 📚 References

- **Web Prototype:** `artifacts/jungle-dash/` (React/Three.js version)
- **Unity Documentation:** [Unity 6 Manual](https://docs.unity3d.com/6000.0/Documentation/Manual/)
- **Standard Assets Guide:** [STANDARD_ASSETS_INTEGRATION.md](./STANDARD_ASSETS_INTEGRATION.md)
- **Android Setup:** [ANDROID_SETUP.md](./ANDROID_SETUP.md)
- **Quick Start:** [QUICK_START.md](./QUICK_START.md)

---

**Game Design Status:** ✅ Complete  
**Ready for Implementation:** ✅ Yes  
**Target Platform:** Android via Unity 6  
**Estimated Development Time:** 5 weeks

---

**Last Updated:** October 6, 2026  
**Version:** 1.0  
**Author:** Jungle Dash Team
