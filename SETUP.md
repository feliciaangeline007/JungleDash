# Jungle Dash – Complete Setup Guide
> Unity 6 · Built-in Render Pipeline · Android · One-thumb endless runner

---

## 1. Install Unity Hub and Unity 6

1. Download **Unity Hub** from [unity.com/download](https://unity.com/download) and install it.
2. Open Unity Hub → **Installs** → **Install Editor**.
3. Filter by **LTS**. Select **Unity 6 (6000.x LTS)**.
4. In the module list tick:
   - **Android Build Support** (check all three sub-items: Android SDK & NDK Tools, OpenJDK, Android Build Support itself)
5. Click **Install**.

> **If only URP is offered in the template list:**  
> Choose "3D (URP)" anyway, then go to **Edit > Project Settings > Graphics** and change the **Scriptable Render Pipeline Settings** field to **None** (the Built-in RP has no asset). Delete the generated URP asset in Assets/Settings if it exists. The project will switch to the Built-in RP automatically.

---

## 2. Create the Project

1. Unity Hub → **Projects → New Project**.
2. Select template: **3D (Built-In Render Pipeline)**.
3. Name it **Jungle Dash**, pick a location, click **Create project**.

---

## 3. Project Settings

### Active Input Handling
Go to **Edit > Project Settings > Player > Other Settings > Active Input Handling** and set it to **Both**. Unity will ask to restart – click **Apply**.

### Import TMP Essential Resources
Menu: **Window > TextMeshPro > Import TMP Essential Resources**. Click **Import** on the popup. Required before running the builder.

### Mobile Quality Settings
**Edit > Project Settings > Quality**:
| Setting | Value |
|---|---|
| Rendering > Real-Time Shadows | Hard Shadows Only |
| Shadows > Shadow Distance | 25 |
| Pixel Light Count | 2 |
| Texture Quality | Full Res |
| Anisotropic Textures | Per Texture |
| Anti-aliasing | 2x |

---

## 4. Import Standard Assets (Selective Import)

1. Locate **"Standard Assets for Unity 6.unitypackage"** (already in your project's `.conversation/attached_assets/` folder or download separately).
2. **Assets > Import Package > Custom Package…** → select the file.
3. In the Import dialog click **None** to deselect everything, then manually tick ONLY:

| What to tick | Why |
|---|---|
| `Characters/ThirdPersonCharacter/Models/Ethan.fbx` | Player model |
| `Characters/ThirdPersonCharacter/Materials/EthanWhite.mat` | Base skin |
| `Characters/ThirdPersonCharacter/Animator/ThirdPersonAnimatorController.controller` | Walk/run animations |
| `Characters/ThirdPersonCharacter/Animation/` (HumanoidRun, HumanoidJumpAndFall, HumanoidMidAir) | Clip files |
| `Characters/ThirdPersonCharacter/Textures/` | Skin textures |
| `Environment/SpeedTree/Broadleaf/` (the .spm or prefab files) | Trees |
| `Environment/SpeedTree/Palm/` | Palm trees |
| `Environment/TerrainAssets/SurfaceTextures/` (MudRockyAlbedoSpecular.bmp, MudRockyNormals.bmp, GrassHillAlbedo.psd, GrassRockyAlbedo.psd, CliffAlbedoSpecular.psd) | Ground textures |
| `ParticleSystems/Textures/` (ParticleFlare.png, Spark.png, ParticleCloudWhite.png) | Particle textures |
| `Prototyping/` (optional) | Grey-box shapes |
| `Fonts/OpenSans/` (optional) | Better UI font |

**DO NOT import (and why):**
| Folder | Reason |
|---|---|
| Vehicles | Unused, large scripts |
| 2D | Not used in 3D project |
| FirstPersonCharacter | Conflicts with our input setup |
| RollerBall | Unused |
| Cameras | Our CameraFollow.cs replaces it |
| CrossPlatformInput | Conflicts with our swipe code |
| Effects | Tessellation/refraction shaders unsupported on Android |
| Editor | Adds editor-only helpers we don't need |
| Utility | Frame-rate manager conflicts |
| Environment/Water | Heavy shaders, not needed |
| PhysicsMaterials | Not referenced |

> **If a compile error appears after import** (e.g. `ThirdPersonUserControl.cs`):  
> In the Project window, find that script under `Assets/Standard Assets/...`, right-click → **Delete**. The builder's scripts replace all Standard Assets scripts.

### MudRockyNormals — mark as Normal Map
1. In Project window, find `MudRockyNormals.bmp`.
2. Select it → Inspector → **Texture Type: Normal map** → **Apply**.

### Test a SpeedTree
Drag `Broadleaf_Mobile.spm` from Project into the Scene view. If the tree appears **pink**, SpeedTree 7 format is incompatible with this Unity version. Delete the instance and the builder will use primitive capsule trees instead.

---

## 5. Place Scripts

| Script file | Folder |
|---|---|
| PowerUpManager.cs | Assets/JungleDash/Scripts/ |
| GameManager.cs | Assets/JungleDash/Scripts/ |
| PlayerRunner.cs | Assets/JungleDash/Scripts/ |
| CameraFollow.cs | Assets/JungleDash/Scripts/ |
| TrackSegment.cs | Assets/JungleDash/Scripts/ |
| TrackSpawner.cs | Assets/JungleDash/Scripts/ |
| Collectible.cs | Assets/JungleDash/Scripts/ |
| Obstacle.cs | Assets/JungleDash/Scripts/ |
| PowerUpPickup.cs | Assets/JungleDash/Scripts/ |
| PowerUpHUD.cs | Assets/JungleDash/Scripts/ |
| JungleDashBuilder.cs | Assets/JungleDash/Editor/ |

Copy each `.cs` file into its folder via Finder (or drag-drop into the Project window). Wait for Unity to compile (progress bar at the bottom). Resolve any compile errors before running the builder.

---

## 6. Run the Builder

1. **Jungle Dash > 1 - Build Prefabs And Scene** — creates all materials, prefabs, the scene, wires all fields. Watch the Console for warnings (missing assets use fallbacks, not errors).
2. **Jungle Dash > 2 - Apply Android Settings** — sets IL2CPP, ARM64, portrait, package name.
3. The scene is saved at `Assets/JungleDash/Scenes/JungleDash.unity` and added to Build Settings.
4. Double-click `JungleDash.unity` to open it. Press **Play** to test in the Editor.

---

## 7. Manual Wiring Reference (if you need to re-link fields)

The builder wires everything automatically. If you ever need to re-wire by hand, use this table. Select the named GameObject, look for the component, then drag the referenced object into the field.

### GameManager (on GameObject "GameManager")
| Field | Drag in | Value |
|---|---|---|
| Score TMP | Canvas > HUDBar > ScoreText | – |
| Coin TMP | Canvas > HUDBar > CoinText | – |
| Game Over Panel | Canvas > GameOverPanel | – |
| Game Over Score TMP | GameOverPanel > GOScore | – |
| Game Over Best TMP | GameOverPanel > GOBest | – |
| Restart Button | GameOverPanel > RestartButton | – |
| Player Transform | Player | – |

### PlayerRunner (on "Player")
| Field | Value |
|---|---|
| Start Speed | 8 |
| Max Speed | 20 |
| Acceleration | 0.5 |
| Lane Width | 2 |
| Lane Change Time | 0.18 |
| Jump Velocity | 10 |
| Gravity | -22 |
| Jump Buffer | 0.12 |
| Swipe Threshold | 0.06 |
| Character Animator | Player > EthanModel (drag the Animator component) |
| Fly V Speed | 2.5 |

### CameraFollow (on "Main Camera")
| Field | Value |
|---|---|
| Target | Player |
| Offset | (0, 2.8, -5.5) |
| Position Damping | 0.12 |
| Rotation Damping | 0.08 |
| Base FOV | 65 |
| Far Clip | 130 |

### PowerUpManager (on "PowerUpManager")

Five **Effect Settings** blocks (expand each, index matches the enum):

| Index | Name | Duration | Ramp Up | Ramp Down |
|---|---|---|---|---|
| 0 | Magnet | 8 | 1 | 1 |
| 1 | Shield | 10 | 0.5 | 1.5 |
| 2 | SpeedBoost | 7 | 3 | 2 |
| 3 | Fly | 8 | 3 | 2.5 |
| 4 | DoubleScore | 10 | 1 | 1 |

Other fields:
| Field | Value |
|---|---|
| Magnet Radius | 7 |
| Magnet Pull Speed | 12 |
| Speed Boost Fraction | 0.6 |
| Speed Boost FOV | 15 |
| Fly Height | 3 |
| Shield Sphere Renderer | Player > ShieldSphere |
| Fly Glow Particles | Player > FlyGlow |
| Fly Trail Renderer | Player > FlyTrail |
| Speed Lines Particles | Main Camera > SpeedLines |

### TrackSpawner (on "TrackSpawner")
| Field | Value |
|---|---|
| Segment Prefab | Assets/JungleDash/Prefabs/TrackSegment.prefab |
| Segment Count | 6 |
| Empty Start Segments | 2 |
| Row Spacing | 7.5 |
| Obstacle Prefabs (size 3) | ObstacleLog, ObstacleBoulder, ObstacleStump |
| Coin Prefab | Coin.prefab |
| Gem Prefab | Gem.prefab |
| Coins Per Line | 5 |
| Coin Chance | 0.7 |
| Gem Chance | 0.15 |
| Max Obstacles Per Row | 2 |
| Obstacle Chance | 0.55 |
| Power Up Prefabs (size 5) | PU_Magnet, PU_Shield, PU_Speed, PU_Fly, PU_Double |
| Power Up Chance | 0.18 |
| Lane Width | 2 |

### PowerUpPickup — per prefab type
| Prefab | Power Up Type |
|---|---|
| PU_Magnet | Magnet |
| PU_Shield | Shield |
| PU_Speed | SpeedBoost |
| PU_Fly | Fly |
| PU_Double | DoubleScore |

### PowerUpHUD (on "PowerUpHUDBar")
Five entries in order (0=Magnet … 4=DoubleScore).  
Each entry: Root = PU_Entry_N, Icon = Icon child Image, Fill Bar = FillBar child Image (Image Type: Filled), Label = Label TMP.

---

## 8. Power-Up Test Checklist

Run in the Editor and collect each power-up. Expected behaviour at key timestamps:

### Magnet (red horseshoe)
| Time | Expected |
|---|---|
| 0 s | Pickup disappears, MAG icon fades in at bottom of screen |
| 1.5 s | Ramp ≈ 1; coins within 7 m visibly slide toward the player |
| 8 s | Timer ends; coins stop being attracted; MAG icon fades out over 1 s |

### Shield (blue shield)
| Time | Expected |
|---|---|
| 0 s | Blue translucent sphere appears around the player, SHD icon visible |
| Hit obstacle | Obstacle disappears (debris burst plays), sphere fades immediately; run continues |
| 10 s (no hit) | Sphere fades out over 1.5 s; SHD icon disappears |

### Speed Boost (orange arrow)
| Time | Expected |
|---|---|
| 0 s | Player begins accelerating; SPD icon appears |
| 3 s | Full boost: speed is 60% higher, FOV widens by 15°; speed-line particles play |
| 7 s | Boost ends; speed/FOV ramp back over 2 s; particles stop |

### Fly (white wings)
| Time | Expected |
|---|---|
| 0 s | FLY icon appears; player begins rising |
| 3 s | Player at 3 m above ground; glow particles on, trail visible; obstacles pass harmlessly underneath |
| 8 s | Player slowly drifts back down over 2.5 s; landing ends invulnerability |

### Double Score (purple cube)
| Time | Expected |
|---|---|
| 0 s | x2 icon appears; score multiplier starts rising |
| 1 s | Full 2× multiplier; coins and distance score both doubled |
| 10 s | Multiplier eases back to 1; icon fades out |

---

## 9. Tuning Table

| Parameter | Script / Field | Default | Notes |
|---|---|---|---|
| Start speed | PlayerRunner.startSpeed | 8 | Units/sec |
| Max speed | PlayerRunner.maxSpeed | 20 | Without boost |
| Acceleration | PlayerRunner.acceleration | 0.5 | Units/sec² |
| Lane width | PlayerRunner.laneWidth | 2 | Must match TrackSpawner.laneWidth |
| Row spacing | TrackSpawner.rowSpacing | 7.5 | Metres between obstacle rows |
| Obstacle density | TrackSpawner.obstacleChance | 0.55 | 0=none, 1=every row |
| Coin chance | TrackSpawner.coinChance | 0.7 | Per row |
| Gem chance | TrackSpawner.gemChance | 0.15 | Replaces middle coin |
| Power-up chance | TrackSpawner.powerUpChance | 0.18 | Per segment |
| Magnet radius | PowerUpManager.magnetRadius | 7 | Metres |
| Magnet ramp up | effectSettings[0].rampUp | 1 | Seconds |
| Fly height | PowerUpManager.flyHeight | 3 | Metres above ground |
| Fly ramp up | effectSettings[3].rampUp | 3 | Seconds to reach full height |
| Speed boost | PowerUpManager.speedBoostFraction | 0.6 | +60% speed |
| FOV bonus | PowerUpManager.speedBoostFOV | 15 | Extra degrees |
| Shield ramp down | effectSettings[1].rampDown | 1.5 | After hit |
| Double score | Duration: effectSettings[4].duration | 10 | Seconds |
| Jump velocity | PlayerRunner.jumpVelocity | 10 | Units/sec |
| Gravity | PlayerRunner.gravity | -22 | Units/sec² |

---

## 10. Android Export

### Switch Platform
1. **File > Build Settings** → select **Android** → **Switch Platform** (takes a minute).

### Configure Package
Already done by the builder. To verify or change:  
**Edit > Project Settings > Player > Android tab > Other Settings > Package Name**: `com.yourname.jungledash`.

### Orientation
**Edit > Project Settings > Player > Android tab > Resolution and Presentation > Default Orientation**: Portrait.

### Scripting Backend (already applied by builder)
**Edit > Project Settings > Player > Android > Other Settings > Scripting Backend**: IL2CPP.  
**Target Architectures**: tick ARM64 only.

### Minimum API
**Minimum API Level**: Android 7.0 (API 24).

### Keystore (REQUIRED for signed builds)
1. **Edit > Project Settings > Player > Android > Publishing Settings**.
2. Click **Keystore Manager…**.
3. Click **Create New Keystore**.
4. Choose a location **outside** your project folder. Name it e.g. `jungledash.keystore`.
5. Set a password (remember it — you can never recover it).
6. Under **New Key Values**: Alias = `jungledash`, Password = same, fill in Name/Organization, Validity = 25 years. Click **Add Key**.
7. Back in Publishing Settings: **Keystore** = your file, **Alias** = jungledash, enter passwords.
8. **Back up the keystore file and passwords somewhere safe.** Losing them means you can never update your app on the Play Store.

### Enable Developer Mode on your Android phone
1. Go to **Settings > About Phone**, tap **Build Number** 7 times.
2. Return to **Settings > Developer Options**, enable **USB Debugging**.
3. Plug in via USB, accept the ADB prompt on the phone.

### Build and Run (direct to device)
1. **File > Build Settings > Build And Run**. Unity compiles and installs the APK on the connected device.

### Build an AAB for Google Play
1. **File > Build Settings**: tick **Build App Bundle (Google Play)**.
2. **Build** → name it `jungledash.aab`.
3. Set **Bundle Version Code** (integer, increment on every upload, e.g. 1).
4. **Target API Level**: match the latest Google Play requirement (currently API 33+; check the Play Console for the current requirement).
5. Upload `jungledash.aab` to **Google Play Console > Internal Testing > Create new release > Upload**.

---

## 11. Troubleshooting

| Problem | Fix |
|---|---|
| **Pink materials** | Standard shader not found. Project must use Built-in RP (no SRP asset assigned). Check **Edit > Project Settings > Graphics > Scriptable Render Pipeline Settings** = None. |
| **Triggers not firing** | Player GameObject must have tag **"Player"** and a kinematic Rigidbody. Check via Inspector. |
| **Player falls through ground** | CharacterController skinWidth (default 0.08) is larger than the ground's BoxCollider Y. Reduce skinWidth to 0.01 in the CharacterController component. |
| **Camera jitter** | Make sure CameraFollow is in LateUpdate (it is). If still jittery, increase Position Damping slightly (e.g. 0.15). |
| **TMP import prompt** | Run **Window > TextMeshPro > Import TMP Essential Resources** before running the builder. |
| **Swipes not working on device** | Confirm Active Input Handling = Both in Player Settings. |
| **UI not clickable** | Canvas needs a GraphicRaycaster component and an EventSystem in the scene (builder adds both). |
| **Trees pink on device** | SpeedTree 7 format unsupported. Delete tree instances; the builder already placed primitive capsule trees as fallback. |
| **Ethan in T-pose** | Animator Controller not assigned, or Forward parameter not set. Open the Animator window while in Play mode, confirm Forward = 1.0. |
| **Stutter on phone** | In Quality Settings, disable Real-Time Shadows or set Shadow Distance to 15. Reduce TrackSpawner.segmentCount to 5. |
| **Gradle / SDK errors** | In **Edit > Preferences > External Tools**, click **Stop Gradle Daemons**. Make sure Android SDK path is set (Unity Hub usually handles this). |
| **Black screen on device** | Missing main camera or AudioListener warning. Confirm the Main Camera tag and AudioListener component exist. |
| **Compile error from imported SA script** | Find the offending script in the Project window (error shows the filename), right-click → Delete. The JungleDash scripts replace all Standard Assets gameplay scripts. |
