using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;

namespace AdventureGame.Editor
{
    [InitializeOnLoad]
    public static class FullMigrationRunner
    {
        private const string RunKey = "Unity6_Steps4and5_Completed_v1";

        static FullMigrationRunner()
        {
            EditorApplication.delayCall += CheckAndRun;
        }

        private static void CheckAndRun()
        {
            if (!SessionState.GetBool(RunKey, false))
            {
                SessionState.SetBool(RunKey, true);
                RunAllRemainingSteps();
            }
        }

        [MenuItem("Migration/Execute Steps 4 & 5 (Prefabs, Materials, Lighting)", false, 20)]
        public static void RunAllRemainingSteps()
        {
            Debug.Log("<color=cyan><b>=== STARTING MIGRATION MATRIX: STEPS 4 & 5 ===</b></color>");

            // ---------------------------------------------------------
            // STEP 4: PREFAB & SCENE SALVAGING
            // ---------------------------------------------------------
            int prefabsChecked = 0;
            int missingScriptsRemoved = 0;
            int prefabsRepaired = 0;

            string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            foreach (string guid in allPrefabs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                prefabsChecked++;
                GameObject prefabRoot = null;
                try
                {
                    prefabRoot = PrefabUtility.LoadPrefabContents(path);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Could not load prefab at {path}: {ex.Message}");
                    continue;
                }

                if (prefabRoot == null) continue;

                bool modified = false;
                Transform[] allChildren = prefabRoot.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in allChildren)
                {
                    int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
                    if (missingCount > 0)
                    {
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
                        missingScriptsRemoved += missingCount;
                        modified = true;
                    }
                }

                if (modified)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                    prefabsRepaired++;
                }
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            Debug.Log($"<color=green><b>[Step 4 Complete]</b> Audited {prefabsChecked} Prefabs. Cleaned {missingScriptsRemoved} missing script hooks across {prefabsRepaired} prefabs.</color>");

            // ---------------------------------------------------------
            // STEP 5: VISUALS, MATERIALS, AND LIGHTING RE-AUTHORING
            // ---------------------------------------------------------
            int materialsChecked = 0;
            int materialsUpgraded = 0;

            string[] allMaterials = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
            Shader standardShader = Shader.Find("Standard");
            Shader mobileDiffuse = Shader.Find("Mobile/Diffuse");
            Shader particleUnlit = Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            Shader skyboxProcedural = Shader.Find("Skybox/Procedural");

            foreach (string guid in allMaterials)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                materialsChecked++;
                bool matModified = false;

                // Check for broken / error shader
                if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader" || mat.shader.name == "")
                {
                    if (path.ToLowerInvariant().Contains("particle"))
                    {
                        mat.shader = particleUnlit;
                    }
                    else if (path.ToLowerInvariant().Contains("skybox"))
                    {
                        mat.shader = skyboxProcedural;
                    }
                    else
                    {
                        mat.shader = standardShader ?? mobileDiffuse;
                    }
                    matModified = true;
                    materialsUpgraded++;
                }

                // If material is Transparent or Water, ensure proper surface blending & queue
                if (mat.name.ToLowerInvariant().Contains("water") || mat.name.ToLowerInvariant().Contains("transparent"))
                {
                    if (mat.HasProperty("_Mode") && mat.GetFloat("_Mode") != 3f)
                    {
                        mat.SetFloat("_Mode", 3f); // Transparent
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.SetInt("_ZWrite", 0);
                        mat.DisableKeyword("_ALPHATEST_ON");
                        mat.EnableKeyword("_ALPHABLEND_ON");
                        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        mat.renderQueue = 3000;
                        matModified = true;
                    }
                }

                // Enforce smoothness/specular validity
                if (mat.HasProperty("_Glossiness") && mat.GetFloat("_Glossiness") > 1.0f)
                {
                    mat.SetFloat("_Glossiness", 0.5f);
                    matModified = true;
                }

                if (matModified)
                {
                    EditorUtility.SetDirty(mat);
                }
            }

            Debug.Log($"<color=green><b>[Step 5 Materials Complete]</b> Audited {materialsChecked} Materials. Re-authored {materialsUpgraded} shaders/materials.</color>");

            // Re-author Scene Lighting for all active scenes
            ReauthorSceneLighting();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green><b>=== FULL MIGRATION MATRIX SUCCESSFULLY EXECUTED ===</b></color>");
        }

        private static void ReauthorSceneLighting()
        {
            string scenePath = "Assets/AdventureGame/Scenes/AdventureIsland.unity";
            if (!File.Exists(scenePath)) return;

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) return;

            // Find or create procedural skybox
            Material skyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/SampleScenes/Materials/SkyboxProcedural.mat");
            if (skyMat == null)
            {
                skyMat = new Material(Shader.Find("Skybox/Procedural"));
                skyMat.SetFloat("_SunSize", 0.04f);
                skyMat.SetFloat("_AtmosphereThickness", 1.0f);
                skyMat.SetColor("_SkyTint", new Color(0.5f, 0.7f, 1.0f));
                skyMat.SetColor("_GroundColor", new Color(0.35f, 0.38f, 0.42f));
                AssetDatabase.CreateAsset(skyMat, "Assets/AdventureGame/Materials/Skybox_Procedural.mat");
            }

            RenderSettings.skybox = skyMat;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1.15f;
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
            RenderSettings.defaultReflectionResolution = 256;
            RenderSettings.reflectionIntensity = 1.0f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.72f, 0.85f, 0.95f);
            RenderSettings.fogDensity = 0.004f;

            // Directional Light Settings
            Light[] lights = Object.FindObjectsByType<Light>();
            foreach (Light l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    l.shadows = LightShadows.Soft;
                    l.shadowNormalBias = 0.4f;
                    l.shadowBias = 0.05f;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"<color=green><b>[Step 5 Lighting Complete]</b> Re-authored Linear PBR Lighting, Soft Shadows, and Procedural Skybox for {scenePath}.</color>");
        }
    }
}
