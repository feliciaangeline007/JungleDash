# 🤖 Setup Android untuk Jungle Dash

## 📱 Konfigurasi Android

Project ini sudah dikonfigurasi untuk build Android dengan pengaturan berikut:

### Build Settings
- **Build Target**: Android
- **Build System**: Gradle
- **API Level**: Automatic (Latest)
- **Scripting Backend**: IL2CPP
- **Architecture**: ARM64 + ARMv7

### Player Settings
Buka `Edit > Project Settings > Player > Android` untuk mengatur:

1. **Company Name**: Ganti dengan nama perusahaan Anda
2. **Product Name**: Jungle Dash
3. **Package Name**: com.YourCompany.JungleDash (format: com.namacompany.namagame)
4. **Version**: 1.0
5. **Bundle Version Code**: 1

### Minimum Requirements
- **Minimum API Level**: Android 6.0 (API 23)
- **Target API Level**: Android 14 (API 34) atau terbaru

## 🎮 Standard Assets

Project ini menggunakan **Unity Standard Assets** yang sudah diimport. Standard Assets menyediakan:

### Characters
- **Third Person Character**: Karakter humanoid dengan controller siap pakai
- **First Person Character**: Controller FPS
- **Ethan Character**: Model karakter dengan animasi

### CrossPlatformInput
- **Mobile Input**: Virtual joystick dan tombol untuk Android
- **Standalone Input**: Input keyboard/mouse untuk testing di editor
- **Auto-detect**: Otomatis beralih antara mobile dan desktop input

### Cameras
- **Follow Camera**: Kamera yang mengikuti player (third person)
- **Freelook Camera**: Kamera bebas
- **Pivot Camera**: Kamera dengan pivot point

### Environment
- **Water**: Prefab air dengan efek refleksi
- **Terrain Textures**: Tekstur untuk terrain
- **Skyboxes**: Skybox siap pakai

### Utility
- **SimpleActivatorMenu**: Menu aktivasi objek
- **AutoMobileShaderSwitch**: Otomatis ganti shader untuk mobile
- **FPSCounter**: Penghitung FPS
- **TimedObjectDestructor**: Hapus objek setelah waktu tertentu

## 📦 Import Standard Assets

File Standard Assets sudah tersedia di:
```
Assets/ImportPackages/StandardAssets.unitypackage
```
**Size**: 181 MB  
**Source**: GitHub marticliment/UnityStandardAssets

### Cara Import:
1. Buka Unity Editor
2. Pilih `Assets > Import Package > Custom Package...`
3. Browse ke file: `Assets/ImportPackages/StandardAssets.unitypackage`
4. Klik `Open`
5. Pada dialog import, pastikan semua item tercentang
6. Klik `Import`

### Folder yang Akan Diimport:
```
Assets/
└── Standard Assets/
    ├── Characters/
    │   ├── FirstPersonCharacter/
    │   ├── ThirdPersonCharacter/
    │   └── RollerBall/
    ├── CrossPlatformInput/
    │   ├── Prefabs/
    │   └── Scripts/
    ├── Cameras/
    │   └── Scripts/
    ├── Environment/
    │   ├── Water/
    │   └── TerrainAssets/
    └── Utility/
        └── Scripts/
```

## 🎯 Menggunakan Standard Assets di Jungle Dash

### 1. Setup Player dengan Third Person Character

```csharp
// Ganti PlayerRunner.cs untuk menggunakan Standard Assets Character Controller
using UnityStandardAssets.Characters.ThirdPerson;

public class PlayerRunner : MonoBehaviour 
{
    private ThirdPersonUserControl thirdPersonControl;
    private ThirdPersonCharacter thirdPersonCharacter;
    
    void Start()
    {
        thirdPersonControl = GetComponent<ThirdPersonUserControl>();
        thirdPersonCharacter = GetComponent<ThirdPersonCharacter>();
    }
}
```

### 2. Setup Mobile Input (CrossPlatformInput)

```csharp
using UnityStandardAssets.CrossPlatformInput;

void Update()
{
    // Gunakan CrossPlatformInput untuk input universal
    float h = CrossPlatformInputManager.GetAxis("Horizontal");
    float v = CrossPlatformInputManager.GetAxis("Vertical");
    bool jump = CrossPlatformInputManager.GetButtonDown("Jump");
}
```

### 3. Setup Camera Follow

```csharp
using UnityStandardAssets.Cameras;

// Di CameraFollow.cs, ganti dengan FreeLookCam dari Standard Assets
// Atau gunakan langsung FreeLookCam prefab dari Standard Assets/Cameras/Prefabs
```

## 🔧 Build untuk Android

### Persiapan:
1. Install **Android SDK** dan **NDK** melalui Unity Hub
2. Install **JDK** (Java Development Kit)
3. Setting path SDK di `Edit > Preferences > External Tools`

### Build Steps:
1. Buka `File > Build Settings`
2. Pastikan **Android** sudah terpilih (jika belum, klik Android lalu "Switch Platform")
3. Klik `Add Open Scenes` untuk menambahkan scene aktif
4. Klik `Player Settings` dan atur:
   - Package Name: `com.YourCompany.JungleDash`
   - Minimum API Level: 23
   - Target API Level: 34
   - Scripting Backend: IL2CPP
   - Target Architectures: ARM64 ✓, ARMv7 ✓
5. Kembali ke Build Settings
6. Klik `Build` atau `Build and Run`
7. Pilih lokasi output APK

### Optimasi untuk Android:
```csharp
// Di Quality Settings (Edit > Project Settings > Quality)
// Gunakan preset "Medium" atau "Low" untuk Android
// Disable shadows untuk performa lebih baik di mobile
```

### Testing di Device:
1. Aktifkan **Developer Mode** di Android device
2. Aktifkan **USB Debugging**
3. Hubungkan device ke komputer via USB
4. Di Unity, klik `Build and Run`
5. APK akan diinstall dan run otomatis di device

## 📱 Mobile Controls dengan CrossPlatformInput

### Setup UI Controls:
1. Tambahkan `MobileSingleStickControl` prefab ke scene
2. Atau buat custom dengan `CrossPlatformInputManager`

```csharp
// Example: Swipe controls untuk lane switching
using UnityStandardAssets.CrossPlatformInput;

public class SwipeDetector : MonoBehaviour
{
    private Vector2 startTouchPosition;
    private Vector2 currentTouchPosition;
    
    void Update()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            startTouchPosition = Input.GetTouch(0).position;
        }
        
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
        {
            currentTouchPosition = Input.GetTouch(0).position;
            Vector2 swipe = currentTouchPosition - startTouchPosition;
            
            if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y))
            {
                // Horizontal swipe
                if (swipe.x > 0) OnSwipeRight();
                else OnSwipeLeft();
            }
            else
            {
                // Vertical swipe
                if (swipe.y > 0) OnSwipeUp();
                else OnSwipeDown();
            }
        }
    }
    
    void OnSwipeLeft() { /* Move to left lane */ }
    void OnSwipeRight() { /* Move to right lane */ }
    void OnSwipeUp() { /* Jump */ }
    void OnSwipeDown() { /* Slide */ }
}
```

## 🎨 Graphics Settings untuk Android

### Render Pipeline:
- Gunakan **URP (Universal Render Pipeline)** untuk performa terbaik di mobile
- Sudah dikonfigurasi di `Assets/JungleDash/Settings/UniversalRenderPipelineAsset.asset`

### Texture Settings:
- Max Size: 1024 atau 2048 untuk mobile
- Compression: ASTC, ETC2, atau Auto
- Mipmaps: Enabled untuk mengurangi aliasing

### Shader Settings:
- Gunakan shader mobile-optimized dari Standard Assets
- `Mobile/Diffuse`, `Mobile/Bumped Specular`, dll.

## 🐛 Troubleshooting

### Build Error: "Unable to find Android SDK"
**Solusi**: Install Android SDK via Unity Hub > Installs > Add Modules > Android Build Support

### Build Error: "NDK not found"
**Solusi**: Install Android NDK via Unity Hub atau download manual

### APK terlalu besar
**Solusi**: 
- Build dengan IL2CPP + ARM64 only (drop ARMv7)
- Enable "Split APKs by target architecture"
- Compress textures dengan ASTC

### Performa rendah di device
**Solusi**:
- Turunkan Quality Setting ke "Low" atau "Medium"
- Disable shadows dan post-processing
- Reduce draw calls dengan batching
- Gunakan LOD (Level of Detail) untuk model

### Touch input tidak bekerja
**Solusi**:
- Pastikan CrossPlatformInput sudah diimport
- Check Input Manager settings (`Edit > Project Settings > Input`)
- Test dengan `Input.touchCount` di script

## 📚 Resources

- [Unity Android Build Documentation](https://docs.unity3d.com/Manual/android-BuildProcess.html)
- [Unity Standard Assets Documentation](https://docs.unity3d.com/Manual/HOWTO-InstallStandardAssets.html)
- [CrossPlatformInput Tutorial](https://learn.unity.com/tutorial/using-cross-platform-input)
- [Mobile Optimization Guide](https://docs.unity3d.com/Manual/MobileOptimizationPracticalGuide.html)

## ✅ Checklist Build Android

- [ ] Standard Assets sudah diimport
- [ ] Build target sudah Android
- [ ] Package name sudah diset
- [ ] Minimum API level 23+
- [ ] IL2CPP + ARM64 enabled
- [ ] Mobile input controls sudah diimplementasi
- [ ] Quality settings sudah dioptimasi untuk mobile
- [ ] Testing di real device
- [ ] APK size < 150MB
- [ ] FPS > 30 di target device

---

**Selamat Building! 🚀**
