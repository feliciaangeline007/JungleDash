# 🚀 Quick Start - Jungle Dash

Panduan cepat untuk setup dan menjalankan Jungle Dash di Unity 6.

## ⚡ Setup dalam 5 Menit

### 1. Requirements
- ✅ Unity 6000.6.3f1 installed (with Android Build Support)
- ✅ Android SDK & NDK (via Unity Hub)
- ✅ JDK (Java Development Kit)
- ✅ Git installed

### 2. Clone atau Open Project
```bash
# Jika belum clone
git clone <repository-url>
cd "Jungle Dash"

# Atau buka yang sudah ada
cd "/Users/fabrianivan/Jungle Dash"
```

### 3. Download Standard Assets (Optional)
```bash
cd Assets/ImportPackages
./download-standard-assets.sh
```
**Atau skip** jika ingin test game dulu tanpa Standard Assets.

### 4. Open di Unity Hub
1. Open Unity Hub
2. Click **"Add"** → **"Add project from disk"**
3. Select folder: `/Users/fabrianivan/Jungle Dash`
4. Unity version: **6000.6.3f1**
5. Click **"Open"**

### 5. Test di Editor
1. Tunggu Unity load project (1-2 menit)
2. Open scene: `Assets/Scenes/Main.unity`
3. Click **Play** ▶️
4. Test controls:
   - **Arrow keys** / **WASD**: Move left/right
   - **Space** / **Up arrow**: Jump
   - **ESC** / **P**: Pause

✅ **Game berjalan!** Lanjut ke build Android.

---

## 📱 Build untuk Android

### Quick Build (10 menit)

#### 1. Switch Platform
```
File → Build Settings → Android → Switch Platform
```
Tunggu proses (2-5 menit pertama kali)

#### 2. Configure Player Settings
```
File → Build Settings → Player Settings
```

**Minimal configuration**:
- **Company Name**: YourCompany (ganti)
- **Product Name**: Jungle Dash
- **Package Name**: `com.yourcompany.jungledash` (HARUS UNIQUE!)
- **Version**: 1.0
- **Other Settings**:
  - Scripting Backend: **IL2CPP** ✓
  - Target Architectures: **ARM64** ✓

#### 3. Build APK
```
File → Build Settings → Build
```
- Pilih lokasi save APK (Desktop recommended)
- Tunggu build process (5-10 menit pertama kali)
- APK akan tersimpan di lokasi yang dipilih

#### 4. Install ke Device
**Option A: Via USB**
```bash
# Enable USB Debugging di Android device
# Settings → About Phone → Tap "Build Number" 7x
# Settings → Developer Options → USB Debugging ON

# Connect device, then:
cd ~/Desktop  # atau lokasi APK Anda
adb install JungleDash.apk
```

**Option B: Transfer Manual**
- Copy APK ke device (via USB, Bluetooth, atau cloud)
- Buka File Manager di device
- Tap APK file → Install

✅ **Game installed!** Buka dan mainkan!

---

## 🎮 Import Standard Assets (Optional)

Standard Assets menambahkan:
- ✅ Character controller yang lebih smooth
- ✅ Mobile input (swipe gestures)
- ✅ Camera system yang advanced
- ✅ Environment assets (water, skybox, dll)

### Import Steps:

1. **Di Unity Editor**:
   ```
   Assets → Import Package → Custom Package...
   ```

2. **Browse file**:
   ```
   Assets/ImportPackages/StandardAssets.unitypackage
   ```

3. **Import all** (centang semua) → Click **Import**

4. **Tunggu import** (2-5 menit)

5. **Upgrade materials** (jika ada pink materials):
   ```
   Select all Standard Assets materials
   Right click → Upgrade Materials to URP
   ```

### Integrate dengan Game:
Lihat tutorial lengkap di **STANDARD_ASSETS_INTEGRATION.md**

---

## 🐛 Troubleshooting

### Game tidak bisa dibuka di Unity
**Problem**: "Project version mismatch"  
**Solution**: Pastikan Unity version **6000.6.3f1** exact

### Assets tidak tampil (pink objects)
**Problem**: Materials pink/magenta  
**Solution**: 
1. Window → Rendering → Render Pipeline → URP → Upgrade Project Materials
2. Atau reimport: Select Assets folder → Right click → Reimport

### Build error: Android SDK not found
**Solution**: 
1. Unity Hub → Installs → Unity 6000.6.3f1 → Add Modules
2. Check **Android Build Support** (SDK & NDK & OpenJDK)
3. Install

### APK tidak bisa install
**Problem**: "App not installed" error  
**Solution**:
- Check "Install from Unknown Sources" enabled di device
- Uninstall old version jika ada
- Check package name unique (tidak clash dengan app lain)

### Standard Assets download gagal
**Solution**:
```bash
# Manual download via curl
cd Assets/ImportPackages
curl -L -o StandardAssets.unitypackage \
  "https://github.com/marticliment/UnityStandardAssets/releases/download/0.0.0/StandardAssets.unitypackage"
```

---

## 📚 Next Steps

### Untuk Development:
1. ✅ Read **ANDROID_SETUP.md** - Complete Android guide
2. ✅ Read **STANDARD_ASSETS_INTEGRATION.md** - Integration tutorial
3. ✅ Modify scripts di `Assets/JungleDash/Scripts/`
4. ✅ Add more obstacles/collectibles
5. ✅ Customize UI dan graphics

### Untuk Testing:
1. ✅ Test di berbagai Android devices
2. ✅ Check performance (FPS > 30)
3. ✅ Test touch controls
4. ✅ Verify all features work

### Untuk Publishing:
1. ✅ Create proper app icon
2. ✅ Create splash screen
3. ✅ Setup Google Play Console account
4. ✅ Create signed APK/AAB
5. ✅ Upload to Play Store

---

## ⚙️ Configuration Summary

### Project Settings
| Setting | Value |
|---------|-------|
| **Unity Version** | 6000.6.3f1 |
| **Render Pipeline** | URP (Universal) |
| **Target Platform** | Android |
| **Scripting Backend** | IL2CPP |
| **Target Architecture** | ARM64 |
| **Min API Level** | 23 (Android 6.0) |
| **Target API** | 34 (Android 14) |

### Build Settings
| Setting | Value |
|---------|-------|
| **Build System** | Gradle |
| **Compression** | LZ4 |
| **Split APKs** | No (Single APK) |
| **Development Build** | No (Release) |

### Quality Settings
| Setting | Value |
|---------|-------|
| **Target FPS** | 60 |
| **VSync** | Enabled |
| **Anti-Aliasing** | 2x (mobile) |
| **Shadows** | Soft Shadows |
| **Texture Quality** | High |

---

## 📁 Project Structure Quick Reference

```
Jungle Dash/
├── Assets/
│   ├── Scenes/
│   │   └── Main.unity                    # 👈 Start here
│   ├── JungleDash/
│   │   ├── Scripts/                      # 👈 Game logic
│   │   ├── Resources/                    # 👈 Game assets
│   │   └── Settings/                     # 👈 URP config
│   └── ImportPackages/
│       ├── StandardAssets.unitypackage   # 👈 Import this
│       ├── download-standard-assets.sh   # 👈 Or run this
│       └── README.md
├── ProjectSettings/                      # Unity config
├── README.md                             # Main docs
├── ANDROID_SETUP.md                      # 📱 Android guide
├── STANDARD_ASSETS_INTEGRATION.md        # 🎯 Integration guide
├── QUICK_START.md                        # 👈 You are here
└── ANDROID_BUILD_READY.md                # ✅ Checklist

```

---

## ⏱️ Time Estimates

| Task | Time | Notes |
|------|------|-------|
| Download Standard Assets | 2-5 min | Depends on internet speed |
| First Unity load | 1-2 min | Importing assets |
| Import Standard Assets | 2-5 min | If you want it |
| Switch to Android platform | 2-5 min | First time only |
| Configure build settings | 2 min | Quick setup |
| First Android build | 5-10 min | Subsequent builds: 1-3 min |
| Install to device | 1 min | Via USB |
| **Total (minimum)** | **10-15 min** | Without Standard Assets |
| **Total (complete)** | **15-30 min** | With Standard Assets |

---

## ✅ Quick Checklist

### Before Building:
- [ ] Unity 6000.6.3f1 installed
- [ ] Android Build Support installed
- [ ] Project opened in Unity
- [ ] Main.unity scene tested
- [ ] Package name configured
- [ ] Company name configured

### Before Publishing:
- [ ] Game tested on real device
- [ ] Performance acceptable (>30 FPS)
- [ ] All features working
- [ ] App icon created
- [ ] Splash screen created
- [ ] Privacy policy prepared (if needed)
- [ ] Google Play account ready

---

## 🎯 Common Goals

### "Saya mau test game cepat"
```bash
1. Open di Unity Hub
2. Open Main.unity
3. Press Play ▶️
Done! (2 menit)
```

### "Saya mau build APK cepat"
```bash
1. File → Build Settings → Android → Switch Platform
2. Player Settings → Set package name
3. Build Settings → Build
Done! (10 menit)
```

### "Saya mau setup lengkap dengan Standard Assets"
```bash
1. ./Assets/ImportPackages/download-standard-assets.sh
2. Import di Unity
3. Follow STANDARD_ASSETS_INTEGRATION.md
Done! (30 menit)
```

### "Saya mau push ke Git remote"
```bash
# Update remote URL ke repository Anda
git remote set-url origin https://github.com/YOUR_USERNAME/JungleDash.git
git push -f origin main
Done!
```

---

## 💡 Tips

### Performance Tips:
- 🔥 Lower quality settings untuk device lama
- 🔥 Disable shadows untuk FPS lebih tinggi
- 🔥 Reduce obstacle density jika lag

### Development Tips:
- 💻 Test di Editor dulu sebelum build
- 💻 Use "Build and Run" untuk test di device langsung
- 💻 Enable Development Build untuk debugging

### Android Tips:
- 📱 Always test di real device
- 📱 Check battery usage
- 📱 Test dengan berbagai screen sizes
- 📱 Verify touch controls responsive

---

**🎮 Selamat bermain dan developing! 🚀**

Untuk pertanyaan atau masalah, lihat dokumentasi lengkap atau file troubleshooting di ANDROID_SETUP.md
