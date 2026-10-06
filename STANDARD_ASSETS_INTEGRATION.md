# 🎯 Integrasi Standard Assets ke Jungle Dash

Panduan langkah demi langkah untuk mengintegrasikan Unity Standard Assets ke dalam game Jungle Dash yang sudah ada.

## 📦 Step 1: Import Standard Assets

1. Buka Unity Editor dengan project Jungle Dash
2. Pilih menu `Assets → Import Package → Custom Package...`
3. Browse ke file: `.conversation/attached_assets/Standard_Assets_for_Unity_20184_1791255762597.unitypackage`
4. Pada dialog import, centang semua item:
   - ✅ Standard Assets/Characters
   - ✅ Standard Assets/CrossPlatformInput
   - ✅ Standard Assets/Cameras
   - ✅ Standard Assets/Environment
   - ✅ Standard Assets/Utility
5. Klik `Import`
6. Tunggu hingga proses import selesai

## 🏃 Step 2: Upgrade Player dengan Third Person Character

### Option A: Ganti Player dengan Prefab Standard Assets

1. **Hapus atau disable PlayerRunner.cs** dari GameObject Player di scene
2. **Tambahkan Third Person Character**:
   - Drag `Standard Assets/Characters/ThirdPersonCharacter/Prefabs/ThirdPersonController` ke scene
   - Rename menjadi "Player"
   - Posisikan di lane tengah (0, 1, 0)

3. **Update PlayerRunner.cs** untuk bekerja dengan ThirdPersonCharacter:

```csharp
using UnityEngine;
using UnityStandardAssets.Characters.ThirdPerson;

[RequireComponent(typeof(ThirdPersonCharacter))]
public class PlayerRunner : MonoBehaviour
{
    private ThirdPersonCharacter character;
    private Rigidbody rb;
    
    [Header("Lane Settings")]
    public float laneDistance = 3f;
    public float laneChangeSpeed = 10f;
    private int currentLane = 1; // 0=left, 1=middle, 2=right
    
    [Header("Jump Settings")]
    public float jumpPower = 12f;
    private bool isGrounded = true;
    
    void Start()
    {
        character = GetComponent<ThirdPersonCharacter>();
        rb = GetComponent<Rigidbody>();
    }
    
    void Update()
    {
        HandleLaneSwitch();
        HandleJump();
    }
    
    void FixedUpdate()
    {
        // Auto-move forward
        Vector3 move = transform.forward * GameManager.Instance.CurrentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);
        
        // Smooth lane transition
        float targetX = (currentLane - 1) * laneDistance;
        Vector3 targetPos = new Vector3(targetX, transform.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.fixedDeltaTime * laneChangeSpeed);
    }
    
    void HandleLaneSwitch()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            if (currentLane > 0) currentLane--;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            if (currentLane < 2) currentLane++;
        }
    }
    
    void HandleJump()
    {
        if (isGrounded && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow)))
        {
            rb.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
            isGrounded = false;
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
        else if (collision.gameObject.CompareTag("Obstacle"))
        {
            GameManager.Instance.GameOver();
        }
    }
}
```

### Option B: Tambahkan Komponen Standard Assets ke Player Existing

Jika ingin tetap menggunakan GameObject Player yang ada:

1. **Add Component** ke Player:
   - `Third Person Character` (dari Standard Assets)
   - `Animator` (jika belum ada)
   
2. **Setup Animator**:
   - Animator Controller: `Standard Assets/Characters/ThirdPersonCharacter/ThirdPersonAnimatorController`
   - Avatar: Ethan Avatar atau custom

3. **Sesuaikan PlayerRunner.cs** untuk memanggil animasi:

```csharp
private Animator animator;

void Start()
{
    animator = GetComponent<Animator>();
}

void Update()
{
    // Set animation parameters
    animator.SetFloat("Forward", GameManager.Instance.CurrentSpeed);
    animator.SetBool("OnGround", isGrounded);
}
```

## 📱 Step 3: Setup Mobile Input dengan CrossPlatformInput

### 3.1 Tambahkan Mobile Control Prefab

1. Buka scene `Main.unity`
2. Drag prefab `Standard Assets/CrossPlatformInput/Prefabs/MobileSingleStickControl` ke Canvas
3. Atau buat custom control UI:

```
Canvas
├── MobileControls (GameObject)
│   ├── ButtonLeft (Button)
│   ├── ButtonRight (Button)
│   └── ButtonJump (Button)
```

### 3.2 Update Input Script

Buat script baru `MobileInputHandler.cs`:

```csharp
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

public class MobileInputHandler : MonoBehaviour
{
    private PlayerRunner player;
    
    void Start()
    {
        player = FindObjectOfType<PlayerRunner>();
    }
    
    void Update()
    {
        // Detect swipe gestures
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Ended)
            {
                Vector2 swipeDelta = touch.position - touch.position;
                
                if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
                {
                    // Horizontal swipe
                    if (swipeDelta.x > 0)
                        player.MoveRight();
                    else
                        player.MoveLeft();
                }
                else if (swipeDelta.y > 0)
                {
                    // Swipe up = jump
                    player.Jump();
                }
            }
        }
        
        // Use CrossPlatformInput for buttons
        if (CrossPlatformInputManager.GetButtonDown("Jump"))
        {
            player.Jump();
        }
        
        float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
        if (horizontal < -0.5f)
            player.MoveLeft();
        else if (horizontal > 0.5f)
            player.MoveRight();
    }
}
```

### 3.3 Setup Input Manager

Di `Edit → Project Settings → Input Manager`, pastikan ada axis:
- **Horizontal**: Left/Right arrow keys, A/D keys
- **Vertical**: Up/Down arrow keys, W/S keys
- **Jump**: Space bar

## 🎥 Step 4: Setup Camera dengan Standard Assets

### Option A: Gunakan FreeLookCam

1. **Delete kamera existing** atau disable CameraFollow.cs
2. **Drag prefab** `Standard Assets/Cameras/Prefabs/FreeLookCameraRig` ke scene
3. **Setup target**:
   - Select FreeLookCameraRig
   - Di Inspector, set `Target` ke Player GameObject
   - Set `Move Speed`: 5
   - Set `Turn Speed`: 2
   - Set `Tilt Max/Min`: untuk sudut kamera

### Option B: Update CameraFollow dengan Smooth Follow

Modifikasi `CameraFollow.cs`:

```csharp
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 5, -8);
    public float smoothSpeed = 0.125f;
    public float lookAheadFactor = 3f;
    
    private Vector3 velocity = Vector3.zero;
    
    void LateUpdate()
    {
        if (target == null) return;
        
        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothSpeed);
        transform.position = smoothedPosition;
        
        Vector3 lookAtPos = target.position + Vector3.forward * lookAheadFactor;
        transform.LookAt(lookAtPos);
    }
}
```

## 🌳 Step 5: Gunakan Environment Assets

### Water (Opsional untuk Efek Visual)

1. Drag `Standard Assets/Environment/Water/Water/Prefabs/WaterProDaytime` ke scene
2. Scale sesuai kebutuhan
3. Posisikan di samping track untuk efek sungai jungle

### Terrain Textures

Jika menggunakan Unity Terrain:
1. Buat Terrain GameObject
2. Terrain Settings → Paint Texture → Edit Textures
3. Import textures dari `Standard Assets/Environment/TerrainAssets/SurfaceTextures`

### Skybox

1. `Window → Rendering → Lighting`
2. Environment → Skybox Material
3. Pilih skybox dari `Standard Assets/Environment/Sky`

## 🔧 Step 6: Optimasi untuk Mobile

### 6.1 Shader Mobile

Standard Assets sudah include mobile shaders. Ganti material shader:
- `Mobile/Diffuse` - untuk objek solid
- `Mobile/Bumped Specular` - untuk objek dengan bump map
- `Mobile/Unlit (Supports Lightmap)` - untuk objek static

### 6.2 Quality Settings

`Edit → Project Settings → Quality → Android`
- Texture Quality: Medium
- Anti Aliasing: Disabled atau 2x
- Shadows: Hard Shadows Only
- Shadow Distance: 20
- Pixel Light Count: 1

### 6.3 FPS Counter (Optional)

Add `Standard Assets/Utility/FPSCounter` script ke Camera untuk monitoring:

```csharp
using UnityEngine;
using UnityStandardAssets.Utility;

public class PerformanceMonitor : MonoBehaviour
{
    void Start()
    {
        gameObject.AddComponent<FPSCounter>();
    }
}
```

## 🎮 Step 7: Testing

### Di Unity Editor
1. Set Game view ke aspect ratio mobile (9:16 atau 16:9)
2. Enable `Simulate Touch` di Input settings
3. Test swipe controls dan buttons

### Di Android Device
1. Connect device via USB
2. Enable Developer Mode + USB Debugging
3. `File → Build Settings → Build and Run`
4. Test performa dan controls

## ✅ Checklist Integrasi

- [ ] Standard Assets diimport tanpa error
- [ ] Player menggunakan ThirdPersonCharacter atau komponen animasinya
- [ ] Mobile input (swipe + buttons) berfungsi
- [ ] Camera smooth follow berfungsi
- [ ] Environment assets (water, skybox) terpasang
- [ ] Mobile shaders digunakan untuk optimasi
- [ ] Quality settings disesuaikan untuk Android
- [ ] Testing di real device berhasil
- [ ] FPS stabil di target device (>30fps)

## 🐛 Troubleshooting

### Error: "ThirdPersonCharacter namespace not found"
**Solusi**: Pastikan Standard Assets sudah diimport lengkap. Check folder `Assets/Standard Assets/Characters/`

### Animasi tidak jalan
**Solusi**: 
1. Check Animator Controller terpasang
2. Pastikan parameter animasi cocok (Forward, Turn, Crouch, OnGround, Jump)
3. Avatar harus humanoid

### CrossPlatformInput tidak work
**Solusi**:
1. Check Input Manager settings
2. Pastikan `Standard Assets/CrossPlatformInput` diimport
3. Add using statement: `using UnityStandardAssets.CrossPlatformInput;`

### Mobile buttons tidak muncul
**Solusi**:
1. Check Canvas Scaler settings (Scale with Screen Size)
2. Pastikan MobileSingleStickControl prefab di child Canvas
3. Check EventSystem ada di scene

### Performa rendah di Android
**Solusi**:
1. Gunakan mobile shaders
2. Reduce shadow distance
3. Disable post-processing
4. Lower texture quality
5. Enable static batching

---

**Selamat mengintegrasikan! 🎉**

Jika ada pertanyaan atau error, check dokumentasi Unity Standard Assets atau ANDROID_SETUP.md
