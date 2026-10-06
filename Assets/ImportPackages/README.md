# 📦 Import Packages

## Standard Assets for Unity

**File**: `StandardAssets.unitypackage`  
**Size**: 181 MB  
**Source**: [marticliment/UnityStandardAssets](https://github.com/marticliment/UnityStandardAssets)

### 🔽 Download Package

File ini **tidak di-commit ke Git** karena ukurannya besar (181 MB).

#### Option 1: Automatic Download (Recommended)
```bash
# Dari root project
cd Assets/ImportPackages
./download-standard-assets.sh
```

#### Option 2: Manual Download
```bash
# Download via curl
cd Assets/ImportPackages
curl -L -o StandardAssets.unitypackage \
  "https://github.com/marticliment/UnityStandardAssets/releases/download/0.0.0/StandardAssets.unitypackage"
```

#### Option 3: Browser Download
1. Buka: https://github.com/marticliment/UnityStandardAssets/releases
2. Download `StandardAssets.unitypackage`
3. Pindahkan ke folder `Assets/ImportPackages/`

### Cara Import:

1. **Di Unity Editor**, pilih menu:
   ```
   Assets → Import Package → Custom Package...
   ```

2. **Browse** ke file ini:
   ```
   Assets/ImportPackages/StandardAssets.unitypackage
   ```

3. **Pada dialog import**, centang semua items yang ingin diimport:
   - ✅ Characters (Third Person, First Person, RollerBall)
   - ✅ CrossPlatformInput (Mobile controls)
   - ✅ Cameras (Follow, FreeLook, Pivot)
   - ✅ Environment (Water, Terrain, Skybox)
   - ✅ Utility (FPS Counter, Object Destructor, dll)

4. **Klik Import** dan tunggu proses selesai (2-5 menit)

### Isi Package:

```
Standard Assets/
├── Characters/
│   ├── FirstPersonCharacter/
│   │   ├── Prefabs/
│   │   ├── Scripts/
│   │   └── ...
│   ├── ThirdPersonCharacter/
│   │   ├── Prefabs/
│   │   │   └── ThirdPersonController.prefab
│   │   ├── Scripts/
│   │   │   ├── ThirdPersonCharacter.cs
│   │   │   └── ThirdPersonUserControl.cs
│   │   ├── Animation/
│   │   └── ...
│   └── RollerBall/
├── CrossPlatformInput/
│   ├── Prefabs/
│   │   ├── MobileSingleStickControl.prefab
│   │   └── ...
│   └── Scripts/
│       ├── CrossPlatformInputManager.cs
│       └── ...
├── Cameras/
│   ├── Prefabs/
│   │   └── FreeLookCameraRig.prefab
│   └── Scripts/
│       ├── FreeLookCam.cs
│       └── ...
├── Environment/
│   ├── Water/
│   │   └── Prefabs/
│   │       └── WaterProDaytime.prefab
│   ├── TerrainAssets/
│   │   └── SurfaceTextures/
│   └── SpeedTree/
└── Utility/
    └── Scripts/
        ├── FPSCounter.cs
        ├── SimpleActivatorMenu.cs
        ├── TimedObjectDestructor.cs
        └── ...
```

### Setelah Import:

Lihat file **STANDARD_ASSETS_INTEGRATION.md** untuk tutorial lengkap mengintegrasikan Standard Assets ke Jungle Dash game.

### Features yang Didapat:

✅ **Third Person Character Controller**  
- Ready-to-use humanoid character dengan animasi
- Controller untuk movement dan rotation
- Physics-based collision

✅ **Mobile CrossPlatformInput**  
- Virtual joystick untuk mobile
- Virtual buttons (jump, fire, dll)
- Auto-detect platform (mobile vs desktop)
- Swipe gesture support

✅ **Camera Systems**  
- FreeLookCam - Camera yang smooth follow player
- PivotCam - Camera dengan pivot point
- Auto camera collision avoidance

✅ **Environment Assets**  
- Water prefabs dengan refleksi
- Terrain textures (grass, rock, mud, dll)
- Skybox materials
- SpeedTree models

✅ **Utility Scripts**  
- FPSCounter - Display FPS di screen
- TimedObjectDestructor - Auto destroy setelah waktu tertentu
- SimpleActivatorMenu - Toggle objects on/off
- AutoMobileShaderSwitch - Otomatis pakai mobile shader

### Mobile Optimization:

Standard Assets include mobile-optimized shaders:
- `Mobile/Diffuse`
- `Mobile/Bumped Specular`
- `Mobile/Particles/Additive`
- `Mobile/Unlit (Supports Lightmap)`

### Compatibility:

✅ Unity 2018.4 - Unity 6.x  
✅ Works dengan URP setelah material upgrade  
✅ Compatible dengan IL2CPP  
✅ Android, iOS, WebGL, PC, Mac, Linux

### Troubleshooting:

**Q: Import error "scripting defines"**  
A: Normal, abaikan saja. Scripts tetap work.

**Q: Pink materials setelah import**  
A: Karena URP. Pilih materials → Right click → Upgrade Materials to URP

**Q: Animation not playing**  
A: Pastikan Animator Controller terpasang dan Avatar set ke Humanoid

**Q: CrossPlatformInput tidak work**  
A: Add using statement: `using UnityStandardAssets.CrossPlatformInput;`

---

**Ready to import! 🚀**
