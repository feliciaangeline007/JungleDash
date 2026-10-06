# Jungle Dash - Setup Notes

## Asset Loading Issue Fix

If assets (textures, models) don't appear when you first open the project:

### Solution 1: Reimport Assets
1. Open Unity Editor (6000.6.3f1)
2. Wait for initial import to complete (check bottom right progress bar)
3. If assets still don't show:
   - Select `Assets/JungleDash/Resources` folder
   - Right-click → **Reimport**
   - Wait for reimport to finish

### Solution 2: Force Reimport All
1. Close Unity
2. Delete the `Library` folder in project root
3. Reopen project in Unity
4. Unity will regenerate Library and reimport all assets

### Solution 3: Check Console
1. Open Unity Console: `Window → General → Console`
2. Look for any import errors
3. Fix any shader or material issues

## What Should Appear

When working correctly, you should see:

### In Scene View (Press Play):
- **Green grass floor** with mud trail texture
- **Trees** (palm trees and broadleaf trees) along both sides
- **Player character** (orange/brown capsule with legs)
- **Lane markers** (green dashes on trail)
- **Fog** in the distance

### During Gameplay:
- **Obstacles**: Brown logs, gray rocks, tree stumps
- **Coins**: Gold spinning cylinders
- **Gems**: Purple/cyan spinning cubes
- **Power-ups**: Colored spheres (cyan, orange, purple, gold, blue)

## Resources Used

Assets loaded from `Assets/JungleDash/Resources/`:

1. **MudRocky.png** - Trail/path texture (should show on ground)
2. **Ethan.fbx** - Optional character model (fallback to procedural)
3. **HumanoidRun.fbx** - Running animation (optional)
4. **HumanoidJumpAndFall.fbx** - Jump animation (optional)
5. **HumanoidIdle.fbx** - Idle animation (optional)
6. **PalmBillboard.png** - Palm tree texture (optional)
7. **BroadleafBillboard.png** - Tree texture (optional)
8. **GrassHill.png** - Grass texture (optional)

### Fallback Behavior

The game script (`JungleDashGame.cs`) creates everything procedurally using Unity primitives if assets fail to load:

- **Trail texture missing?** → Solid color material used
- **Character model missing?** → Capsule + cubes created
- **Tree textures missing?** → Solid color trees generated

This means **the game will always work** even without assets!

## First Time Opening

### Expected Behavior:
1. Unity imports all assets (1-2 minutes)
2. Console may show some warnings (ignore if game plays)
3. Press Play → Menu screen appears
4. Click "MULAI BERLARI" or press Enter/Space
5. Game starts with jungle scenery

### If Nothing Appears:
1. Check Scene has `Jungle Dash Runtime` GameObject
2. Check GameObject has `JungleDashGame` script attached
3. Script should be enabled (checkmark in Inspector)
4. Check Console for red errors

## Unity 6 Compatibility

Project is configured for **Unity 6000.6.3f1**:
- Standard render pipeline (not URP/HDRP)
- Legacy shaders for transparency
- OnGUI for UI (no Canvas)
- Procedural mesh generation at runtime

## Troubleshooting

### "Script missing" error
- Right-click script → **Reimport**
- Check GUID in `.meta` file matches scene reference

### Textures appear pink/magenta
- Textures not imported properly
- Select texture → Inspector → Check import settings
- Try changing Texture Type or re-importing

### Game runs but everything is primitive shapes
- This is NORMAL fallback behavior!
- Assets are optional enhancements
- Game is fully playable with procedural graphics

### Performance issues
- Check Quality Settings: `Edit → Project Settings → Quality`
- Reduce shadow quality if needed
- Check target framerate is 60 (set in script)

## Asset File Sizes

Resources folder contents (~6 MB total):
- MudRocky.png: 2.4 MB
- BroadleafBillboard.png: 870 KB
- Ethan.fbx: 724 KB
- GrassHill.png: 553 KB
- PalmBillboard.png: 487 KB
- HumanoidJumpAndFall.fbx: 935 KB
- HumanoidRun.fbx: 950 KB
- HumanoidIdle.fbx: 1.3 MB

## Quick Test

After opening project:
```
1. Press Play ▶️
2. Should see: "JUNGLE DASH" title
3. Press Enter or Space
4. Game starts - you can move and jump
5. If you see green ground + trees = WORKING! ✓
```

## Need Help?

If assets still don't appear after trying solutions above:
1. Check Unity version is **exactly 6000.6.3f1**
2. Verify all `.meta` files exist alongside assets
3. Check git pulled all files correctly
4. Try opening on different machine

**Remember:** Game works without assets! Procedural graphics are built-in fallback.
