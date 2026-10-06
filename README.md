# Jungle Dash — Unity 6 Endless Runner (Android + Standard Assets)

Complete 3-lane endless runner game built with **Unity 6000.6.3f1** for **Android** platform using **Unity Standard Assets**.

## Features

### Gameplay
- **Three-lane endless runner** with smooth lane switching
- **Jump mechanics** with proper physics and collision detection
- **Procedurally generated** jungle environment
- **Dynamic difficulty** - speed increases over time

### Obstacles & Collectibles
- **Obstacles**: Fallen logs, rocks, tree stumps
- **Collectibles**: Gold coins (+5 points), purple gems (+25 points)
- **5 Power-ups**:
  - 🧲 **Magnet** - Auto-collect nearby coins
  - 🛡️ **Shield** - Protect from one obstacle hit
  - ⚡ **Speed** - Increased running speed
  - ✈️ **Fly** - Fly over obstacles
  - ⭐ **Double Score** - 2x score multiplier

### Visuals
- **Procedural jungle scenery** with palm trees and broadleaf trees
- **Fog and lighting** for atmospheric depth
- **Smooth camera following**
- **Animated character** with running legs

### UI & Controls
- **Indonesian localization** (SKOR, JARAK, LOMPAT, MULAI BERLARI)
- **Score tracking** with persistent high score
- **Distance counter** in meters
- **Pause menu** and **Game Over screen**
- **Touch controls** for mobile (swipe gestures)
- **On-screen buttons** for mobile
- **Keyboard controls** (arrows/WASD/Space)

## How to Open

1. **Install Unity 6000.6.3f1**
   - Download from Unity Hub
   - Or get from [Unity Archive](https://unity.com/releases/editor/archive)

2. **Add Project to Unity Hub**
   - Open Unity Hub
   - Click "Add" → "Add project from disk"
   - Select this folder: `/Users/fabrianivan/Jungle Dash`

3. **Open and Play**
   - Open `Assets/Scenes/Main.unity`
   - Press **Play** ▶️

## Controls

### Keyboard
- **← / A**: Move left lane
- **→ / D**: Move right lane
- **↑ / W / Space**: Jump
- **Esc / P**: Pause/Resume

### Mobile/Touch
- **Swipe Left**: Move left
- **Swipe Right**: Move right
- **Swipe Up**: Jump
- **On-screen buttons**: Left arrow, Right arrow, LOMPAT (Jump)

## Build for Android

### Prerequisites
1. Install **Unity 6000.6.3f1** with **Android Build Support** module
2. Install **Android SDK & NDK** (via Unity Hub or standalone)
3. Install **JDK** (Java Development Kit)

### Quick Build Steps
1. Open **File → Build Settings**
2. Select **Android** platform
3. Click **Switch Platform** (if not already Android)
4. Configure **Player Settings**:
   - **Package Name**: `com.YourCompany.JungleDash`
   - **Minimum API Level**: Android 6.0 (API 23)
   - **Target API Level**: Latest (API 34+)
   - **Scripting Backend**: IL2CPP
   - **Target Architectures**: ARM64 ✓, ARMv7 ✓
5. Click **Build** or **Build and Run**

📱 **Lihat ANDROID_SETUP.md untuk panduan lengkap Android & Standard Assets**

## Standard Assets

Project ini menggunakan **Unity Standard Assets** untuk:
- ✅ Third Person Character Controller
- ✅ Mobile CrossPlatformInput (swipe & virtual buttons)
- ✅ Camera Follow System
- ✅ Environment Assets (water, terrain, skybox)
- ✅ Mobile-optimized shaders

### Import Standard Assets:
1. **File tersedia**: `.conversation/attached_assets/Standard_Assets_for_Unity_20184_1791255762597.unitypackage`
2. Di Unity: `Assets → Import Package → Custom Package`
3. Browse ke file unitypackage
4. Import semua assets

## Build

The `Main` scene is already included in Build Settings.

1. Open **File → Build Settings** (or **Build Profiles** in Unity 6)
2. Select target platform (PC, Mac, Android, iOS, WebGL)
3. Click **Build** or **Build and Run**

## Project Structure

```
Assets/
├── Scenes/
│   └── Main.unity              # Main game scene
├── JungleDash/
│   ├── Scripts/
│   │   └── JungleDashGame.cs  # Single self-contained game script
│   └── Resources/
│       ├── MudRocky.png       # Trail texture
│       ├── Ethan.fbx          # Character model (optional)
│       └── ...                # Other assets
```

## Technical Details

- **Unity Version**: 6000.6.3f1 (Unity 6)
- **Platform**: Android (ARM64 + ARMv7)
- **Rendering**: Universal Render Pipeline (URP)
- **Standard Assets**: Included for character, input, and camera
- **Target FPS**: 60
- **Single Script**: All game logic in `JungleDashGame.cs`
- **Runtime Generation**: No manual scene setup required
- **Object Pooling**: Efficient entity management
- **Persistent Storage**: High score saved via PlayerPrefs
- **Mobile Input**: CrossPlatformInput with swipe gestures

## Game Mechanics

### Scoring
- **Distance**: +1 point per meter traveled
- **Coins**: +5 points each
- **Gems**: +25 points each
- **Speed multiplier**: Increases over time
- **Double Score power-up**: 2x multiplier for 10 seconds

### Power-up Durations
- Magnet: 8 seconds
- Shield: 9 seconds
- Speed Boost: 7 seconds
- Fly: 8 seconds
- Double Score: 10 seconds

### Difficulty Progression
- Starting speed: 11.2 m/s
- Maximum speed: 23 m/s
- Acceleration: +0.24 m/s per second
- Obstacle spawn rate: 67% per row
- Coin line spawn rate: 84%
- Gem spawn rate: 15%
- Power-up spawn rate: 22%

## Credits

**Jungle Dash**
- Built with Unity 6000.6.3f1
- Self-contained endless runner
- Indonesian UI localization
- Touch and keyboard controls
- Procedural jungle generation

## Browser Prototype

The original React/Three.js browser prototype is available in `artifacts/jungle-dash/`.

---

**Ready to play!** Open in Unity Hub and press Play ▶️
