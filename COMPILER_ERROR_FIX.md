# 🔧 Compiler Error Fix Guide

Jika ada compiler errors saat membuka project di Unity, ikuti panduan ini.

---

## 🔍 Step 1: Identify Error

**Di Unity Editor:**
1. Open **Console** window (`Window → General → Console` atau Ctrl/Cmd+Shift+C)
2. Look for **RED** error messages
3. Copy error message text

**Common Error Patterns:**
```
CS0246: The type or namespace 'X' could not be found
CS0104: 'X' is an ambiguous reference
CS1061: 'X' does not contain a definition for 'Y'
error CS8370: Feature 'X' is not available in C# 7.3
```

---

## 🛠️ Common Fixes

### Error 1: "Namespace 'UnityStandardAssets' could not be found"

**Cause:** Standard Assets not imported yet

**Fix:**
```bash
# Import Standard Assets
Assets → Import Package → Custom Package
Select: Assets/ImportPackages/StandardAssets.unitypackage
Import all
```

### Error 2: "Feature not available in C# 7.3"

**Cause:** Script uses newer C# features

**Fix:**
```
Edit → Project Settings → Player → Other Settings
→ API Compatibility Level: .NET Standard 2.1
→ C# Compiler: Default
```

### Error 3: "Assembly has reference to non-existent assembly"

**Cause:** Missing package or module

**Fix:**
```bash
# Check Packages/manifest.json includes:
{
  "dependencies": {
    "com.unity.render-pipelines.universal": "17.0.3",
    "com.unity.textmeshpro": "3.2.0-pre.7"
  }
}

# Then: Window → Package Manager → Refresh
```

### Error 4: "Script class cannot be found"

**Cause:** Script .meta file out of sync

**Fix:**
```bash
# In Unity:
Assets → Reimport All

# Or delete Library folder and reopen:
# Close Unity
# Delete: <Project>/Library/
# Reopen project in Unity Hub
```

### Error 5: "Type 'X' is defined in an assembly that is not referenced"

**Cause:** Missing assembly reference

**Fix:**
```
# For Standard Assets scripts:
1. Right-click script in Project window
2. Properties → Assembly Definition
3. Add reference to required assembly
```

---

## 🚀 Quick Fix Sequence

Try these steps in order:

### 1. Reimport All Assets
```
Assets → Reimport All
Wait for completion (may take 2-5 min)
```

### 2. Refresh Packages
```
Window → Package Manager
Click refresh icon (top left)
Wait for package refresh
```

### 3. Check API Compatibility
```
Edit → Project Settings → Player
Android tab → Other Settings
→ API Compatibility Level: .NET Standard 2.1
→ Scripting Backend: IL2CPP
```

### 4. Clean and Rebuild
```
# Close Unity
# Delete these folders:
Library/
Temp/
obj/

# Reopen project
# Wait for reimport (5-10 min)
```

### 5. Import Standard Assets (if not done)
```
Assets → Import Package → Custom Package
Browse: Assets/ImportPackages/StandardAssets.unitypackage
Select all → Import
Wait 2-5 minutes
```

---

## 🔧 Script-Specific Fixes

### JungleDashGame.cs Errors

**Error:** "MonoBehaviour could not be found"
```
Fix: Ensure using UnityEngine; at top of script
```

**Error:** "PlayerPrefs does not exist"
```
Fix: Already in UnityEngine, check if Unity Editor loaded properly
```

### Standard Assets Errors

**Error:** "UnityStandardAssets namespace not found"
```bash
Fix 1: Import Standard Assets package
Fix 2: Check Assets/Standard Assets/ folder exists
Fix 3: Reimport: Right-click folder → Reimport
```

**Error:** "CrossPlatformInputManager not found"
```bash
Fix: Ensure Standard Assets/CrossPlatformInput/ imported
Verify: Assets/Standard Assets/CrossPlatformInput/Scripts/ exists
```

---

## 🔍 Diagnostic Script

Create this script to check project state:

**File:** `Assets/Editor/DiagnosticTool.cs`

```csharp
using UnityEditor;
using UnityEngine;
using System.IO;

public class DiagnosticTool : EditorWindow
{
    [MenuItem("Tools/Project Diagnostic")]
    static void ShowWindow()
    {
        GetWindow<DiagnosticTool>("Diagnostic");
    }

    void OnGUI()
    {
        GUILayout.Label("Project Diagnostic", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Check Standard Assets"))
        {
            CheckStandardAssets();
        }
        
        if (GUILayout.Button("Check Scripts"))
        {
            CheckScripts();
        }
        
        if (GUILayout.Button("Check Packages"))
        {
            CheckPackages();
        }
        
        if (GUILayout.Button("Fix API Compatibility"))
        {
            FixAPICompatibility();
        }
    }
    
    void CheckStandardAssets()
    {
        string path = "Assets/Standard Assets";
        if (Directory.Exists(path))
        {
            Debug.Log("✅ Standard Assets found at: " + path);
            string[] folders = Directory.GetDirectories(path);
            Debug.Log($"  Contains {folders.Length} folders:");
            foreach (var folder in folders)
            {
                Debug.Log($"    - {Path.GetFileName(folder)}");
            }
        }
        else
        {
            Debug.LogError("❌ Standard Assets not found!");
            Debug.Log("Import via: Assets → Import Package → Custom Package");
        }
    }
    
    void CheckScripts()
    {
        string[] scripts = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories);
        Debug.Log($"✅ Found {scripts.Length} C# scripts");
        
        // Check main game script
        string mainScript = "Assets/JungleDash/Scripts/JungleDashGame.cs";
        if (File.Exists(mainScript))
            Debug.Log("✅ JungleDashGame.cs found");
        else
            Debug.LogError("❌ JungleDashGame.cs missing!");
    }
    
    void CheckPackages()
    {
        string manifestPath = "Packages/manifest.json";
        if (File.Exists(manifestPath))
        {
            string manifest = File.ReadAllText(manifestPath);
            Debug.Log("✅ Packages manifest found");
            
            if (manifest.Contains("com.unity.render-pipelines.universal"))
                Debug.Log("  ✅ URP package referenced");
            else
                Debug.LogWarning("  ⚠️  URP package not in manifest");
                
            if (manifest.Contains("com.unity.textmeshpro"))
                Debug.Log("  ✅ TextMeshPro package referenced");
            else
                Debug.LogWarning("  ⚠️  TextMeshPro not in manifest");
        }
    }
    
    void FixAPICompatibility()
    {
        PlayerSettings.SetApiCompatibilityLevel(
            BuildTargetGroup.Android,
            ApiCompatibilityLevel.NET_Standard_2_1
        );
        
        PlayerSettings.SetScriptingBackend(
            BuildTargetGroup.Android,
            ScriptingImplementation.IL2CPP
        );
        
        Debug.Log("✅ API Compatibility set to .NET Standard 2.1");
        Debug.Log("✅ Scripting Backend set to IL2CPP");
    }
}
```

**Usage:**
1. Create `Assets/Editor/` folder
2. Save script as `DiagnosticTool.cs`
3. Unity menu: `Tools → Project Diagnostic`
4. Click buttons to run checks

---

## 📝 Collect Error Information

If errors persist, collect this info:

```bash
# 1. Unity version
Help → About Unity

# 2. Console errors
Window → General → Console
Right-click error → Copy

# 3. Script causing error
Double-click error to see which script

# 4. Package versions
Window → Package Manager
Check versions of installed packages

# 5. Project settings
Edit → Project Settings → Player
Screenshot or note settings
```

---

## 🆘 Emergency Reset

If project won't compile at all:

### Option 1: Fresh Scene
```
1. File → New Scene
2. Save as: Assets/Scenes/Fresh.unity
3. Add GameObject → Create Empty
4. Add Component → JungleDashGame
5. File → Save
6. Test Play
```

### Option 2: Remove Problematic Scripts
```
1. Identify error script from Console
2. Move script to Desktop (temporary)
3. Let Unity recompile
4. Fix script on Desktop
5. Move back when fixed
```

### Option 3: Clean Reinstall
```
1. Export project (without Library/Temp/):
   Assets → Export Package → All → Export
   
2. Create new Unity 6000.6.3f1 project

3. Import package:
   Assets → Import Package → Custom Package
   
4. Setup scene manually
```

---

## ✅ Verification Steps

After fixes, verify:

```
1. Console shows 0 errors (may have warnings, OK)
2. Click Play ▶️ - game starts
3. Scene view shows objects
4. Hierarchy shows GameObjects
5. Scripts have no red icons in Project window
```

---

## 📞 Getting Help

If you need specific error fix:

1. Copy FULL error message from Console
2. Note which script is erroring
3. Screenshot Console window
4. Share error details

Example error format:
```
Assets/Path/To/Script.cs(42,15): error CS0246: 
The type or namespace name 'SomeThing' could not be found 
(are you missing a using directive or an assembly reference?)
```

---

## 🎯 Most Common Solution

**90% of compiler errors are fixed by:**

```bash
1. Close Unity
2. Delete Library/ folder
3. Reopen project
4. Wait for reimport (5-10 min)
5. Import Standard Assets if not done
6. Check Console again
```

---

**Last Updated:** October 6, 2026  
**For:** Unity 6000.6.3f1 + Android + Standard Assets
