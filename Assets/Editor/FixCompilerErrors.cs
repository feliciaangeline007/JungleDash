using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Fixes compiler errors by regenerating .meta files and setting correct API compatibility
/// </summary>
public class FixCompilerErrors : EditorWindow
{
    [MenuItem("Tools/Fix All Compiler Errors")]
    static void FixAllErrors()
    {
        Debug.Log("=== FIXING COMPILER ERRORS ===");
        
        // Step 1: Fix API Compatibility for all platforms
        Debug.Log("Step 1: Setting API Compatibility...");
        PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Standalone, ApiCompatibilityLevel.NET_Standard_2_1);
        PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Android, ApiCompatibilityLevel.NET_Standard_2_1);
        PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.iOS, ApiCompatibilityLevel.NET_Standard_2_1);
        PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.WebGL, ApiCompatibilityLevel.NET_Standard_2_1);
        Debug.Log("✅ API Compatibility set to .NET Standard 2.1 for all platforms");
        
        // Step 2: Fix scripting backend
        Debug.Log("Step 2: Setting Scripting Backend...");
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        Debug.Log("✅ Android Scripting Backend set to IL2CPP");
        
        // Step 3: Delete corrupted .meta files
        Debug.Log("Step 3: Cleaning corrupted .meta files...");
        string[] corruptedMetas = new string[]
        {
            "Assets/JungleDash/Settings.meta",
            "Assets/ImportPackages/Packages.meta",
            "ProjectSettings/Packages/com.unity.ai.assistant/Settings.json"
        };
        
        foreach (var metaPath in corruptedMetas)
        {
            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
                Debug.Log($"  Deleted: {metaPath}");
            }
        }
        
        // Step 4: Reimport problematic folders
        Debug.Log("Step 4: Reimporting assets...");
        if (Directory.Exists("Assets/Standard Assets"))
        {
            AssetDatabase.ImportAsset("Assets/Standard Assets", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            Debug.Log("✅ Reimported Standard Assets");
        }
        
        if (Directory.Exists("Assets/JungleDash"))
        {
            AssetDatabase.ImportAsset("Assets/JungleDash", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            Debug.Log("✅ Reimported JungleDash folder");
        }
        
        // Step 5: Refresh asset database
        Debug.Log("Step 5: Refreshing Asset Database...");
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        Debug.Log("✅ Asset Database refreshed");
        
        Debug.Log("=== FIX COMPLETE ===");
        Debug.Log("Wait 10-20 seconds for Unity to recompile.");
        Debug.Log("If errors persist, try: Tools → Delete Library and Restart");
    }
    
    [MenuItem("Tools/Delete Library and Restart")]
    static void DeleteLibraryPrompt()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "Delete Library Folder?",
            "This will delete the Library folder and Unity will need to reimport all assets.\n\n" +
            "This usually fixes persistent compiler errors.\n\n" +
            "Unity will close. Reopen project from Unity Hub after deletion.\n\n" +
            "Continue?",
            "Yes, Delete Library",
            "Cancel"
        );
        
        if (confirm)
        {
            Debug.Log("Deleting Library folder...");
            string libraryPath = Path.Combine(Directory.GetCurrentDirectory(), "Library");
            
            if (Directory.Exists(libraryPath))
            {
                // Save this info to a file so user knows what to do
                string infoPath = Path.Combine(Directory.GetCurrentDirectory(), "REOPEN_PROJECT.txt");
                File.WriteAllText(infoPath, 
                    "Library folder has been deleted.\n\n" +
                    "To continue:\n" +
                    "1. This Unity window will close\n" +
                    "2. Open Unity Hub\n" +
                    "3. Open this project again\n" +
                    "4. Wait 5-10 minutes for reimport\n" +
                    "5. Compiler errors should be fixed\n"
                );
                
                EditorApplication.Exit(0);
                
                // Delete library after Unity starts closing
                System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ =>
                {
                    try
                    {
                        Directory.Delete(libraryPath, true);
                    }
                    catch { }
                });
            }
        }
    }
}
