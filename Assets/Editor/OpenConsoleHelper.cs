using UnityEditor;
using UnityEngine;

/// <summary>
/// Helper to open Console and show errors
/// </summary>
public class OpenConsoleHelper : EditorWindow
{
    [MenuItem("Tools/Show Errors in Console")]
    static void ShowErrors()
    {
        // Open Console window
        EditorWindow.GetWindow(System.Type.GetType("UnityEditor.ConsoleWindow,UnityEditor"));
        
        Debug.Log("=== CHECKING FOR ERRORS ===");
        Debug.Log("If you see errors above this message, those need to be fixed.");
        Debug.Log("Common fixes:");
        Debug.Log("1. Import Standard Assets if not done yet");
        Debug.Log("2. Edit → Project Settings → Player → API Compatibility: .NET Standard 2.1");
        Debug.Log("3. Assets → Reimport All");
    }
    
    [MenuItem("Tools/Quick Fix Common Issues")]
    static void QuickFix()
    {
        Debug.Log("=== RUNNING QUICK FIX ===");
        
        // Fix API compatibility
        PlayerSettings.SetApiCompatibilityLevel(
            BuildTargetGroup.Android,
            ApiCompatibilityLevel.NET_Standard_2_1
        );
        Debug.Log("✅ Set API Compatibility to .NET Standard 2.1");
        
        PlayerSettings.SetApiCompatibilityLevel(
            BuildTargetGroup.Standalone,
            ApiCompatibilityLevel.NET_Standard_2_1
        );
        Debug.Log("✅ Set Standalone API Compatibility to .NET Standard 2.1");
        
        // Check Standard Assets
        if (System.IO.Directory.Exists("Assets/Standard Assets"))
        {
            Debug.Log("✅ Standard Assets folder found");
        }
        else
        {
            Debug.LogWarning("⚠️ Standard Assets not found. Import via:");
            Debug.LogWarning("   Assets → Import Package → Custom Package");
            Debug.LogWarning("   Select: Assets/ImportPackages/StandardAssets.unitypackage");
        }
        
        // Refresh
        AssetDatabase.Refresh();
        Debug.Log("✅ Asset database refreshed");
        
        Debug.Log("=== QUICK FIX COMPLETE ===");
        Debug.Log("Check Console above for any remaining errors.");
    }
}
