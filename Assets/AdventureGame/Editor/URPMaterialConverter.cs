using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;

namespace AdventureGame.Editor
{
    [InitializeOnLoad]
    public static class URPMaterialConverter
    {
        private const string ConvertedKey = "Unity6_URPMigration_Done_v1";

        static URPMaterialConverter()
        {
            EditorApplication.delayCall += () =>
            {
                EnforceURPPipelineSettings();
                CheckAndConvert();
            };
        }

        [MenuItem("Migration/Enforce Active URP Pipeline in Settings", false, 25)]
        public static void EnforceURPPipelineSettings()
        {
            RenderPipelineAsset urpAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            if (urpAsset == null)
            {
                urpAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            }

            if (urpAsset != null)
            {
                bool changed = false;
                if (GraphicsSettings.defaultRenderPipeline != urpAsset)
                {
                    GraphicsSettings.defaultRenderPipeline = urpAsset;
                    changed = true;
                    Debug.Log($"<color=green><b>[URP Pipeline Setup] GraphicsSettings.defaultRenderPipeline assigned to: {urpAsset.name}</b></color>");
                }

                int qualityLevels = QualitySettings.names.Length;
                for (int i = 0; i < qualityLevels; i++)
                {
                    QualitySettings.SetQualityLevel(i, false);
                    if (QualitySettings.renderPipeline != urpAsset)
                    {
                        QualitySettings.renderPipeline = urpAsset;
                        changed = true;
                    }
                }

                if (changed)
                {
                    AssetDatabase.SaveAssets();
                    Debug.Log($"<color=green><b>[URP Pipeline Setup] Successfully applied URP across all {qualityLevels} Quality Settings levels and GraphicsSettings. Built-in Render Pipeline is disabled!</b></color>");
                }
            }
            else
            {
                Debug.LogWarning("[URP Pipeline Setup] Could not find Mobile_RPAsset.asset or PC_RPAsset.asset in Assets/Settings.");
            }
        }

        private static void CheckAndConvert()
        {
            EnforceURPPipelineSettings();
            if (!SessionState.GetBool(ConvertedKey, false))
            {
                SessionState.SetBool(ConvertedKey, true);
                ConvertAllToURP();
            }
        }

        [MenuItem("Migration/Upgrade All Materials & Lighting to URP", false, 30)]
        public static void ConvertAllToURP()
        {
            EnforceURPPipelineSettings();
            Debug.Log("<color=cyan><b>=== CONVERTING PROJECT MATERIALS & LIGHTING TO URP ===</b></color>");

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            Shader urpSimpleLit = Shader.Find("Universal Render Pipeline/Simple Lit");
            Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
            Shader urpParticleUnlit = Shader.Find("Universal Render Pipeline/Particles/Unlit");

            if (urpLit == null)
            {
                Debug.LogError("URP Lit Shader not found. Ensure com.unity.render-pipelines.universal package is installed.");
                return;
            }

            int upgradedCount = 0;
            string[] allMaterialGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string guid in allMaterialGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(path)) continue;

                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null || mat.shader == null) continue;

                    string shaderName = mat.shader.name;

                    // If already URP, skip
                    if (shaderName.StartsWith("Universal Render Pipeline/")) continue;
                    // Keep procedural skybox
                    if (shaderName.Contains("Skybox")) continue;

                    // Cache existing properties
                    Color baseColor = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                    Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                    Vector2 texScale = mat.HasProperty("_MainTex") ? mat.GetTextureScale("_MainTex") : Vector2.one;
                    Vector2 texOffset = mat.HasProperty("_MainTex") ? mat.GetTextureOffset("_MainTex") : Vector2.zero;

                    Texture bumpMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
                    float gloss = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.5f;
                    float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;

                    bool isTransparent = false;
                    if (mat.HasProperty("_Mode") && mat.GetFloat("_Mode") >= 2f) isTransparent = true;
                    if (mat.renderQueue >= 3000) isTransparent = true;
                    if (mat.name.ToLowerInvariant().Contains("water") || mat.name.ToLowerInvariant().Contains("glass") || mat.name.ToLowerInvariant().Contains("transparent")) isTransparent = true;

                    // Determine target URP shader
                    if (shaderName.Contains("Particle"))
                    {
                        mat.shader = urpParticleUnlit ?? urpUnlit;
                    }
                    else if (shaderName.Contains("Unlit"))
                    {
                        mat.shader = urpUnlit;
                    }
                    else
                    {
                        mat.shader = urpLit;
                    }

                    // Re-apply properties into URP equivalents
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
                    if (mat.HasProperty("_BaseMap") && mainTex != null)
                    {
                        mat.SetTexture("_BaseMap", mainTex);
                        mat.SetTextureScale("_BaseMap", texScale);
                        mat.SetTextureOffset("_BaseMap", texOffset);
                    }

                    if (mat.HasProperty("_BumpMap") && bumpMap != null)
                    {
                        mat.SetTexture("_BumpMap", bumpMap);
                        mat.EnableKeyword("_NORMALMAP");
                    }

                    if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", gloss);
                    if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

                    // Transparent setup in URP
                    if (isTransparent)
                    {
                        mat.SetFloat("_Surface", 1f); // Transparent
                        mat.SetFloat("_Blend", 0f);   // Alpha
                        mat.SetFloat("_ZWrite", 0f);
                        mat.renderQueue = (int)RenderQueue.Transparent;
                        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    }

                    EditorUtility.SetDirty(mat);
                    upgradedCount++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"<color=green><b>[URP Migration] Successfully upgraded {upgradedCount} materials to Universal Render Pipeline!</b></color>");

            // Upgrade Scene Cameras and Lighting in both scenes
            SetupURPInScene("Assets/AdventureGame/Scenes/AdventureIsland.unity");
            SetupURPInScene("Assets/AdventureGame/Scenes/MainMenu.unity");

            // Apply fine-tuned fixes for SpeedTree 7, transparent ocean, character normals, and vehicles
            StandardAssetRenderFixer.FixAllRenderIssues();
        }

        private static void SetupURPInScene(string scenePath)
        {
            if (!File.Exists(scenePath)) return;

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) return;

            // 1. Setup Camera for URP
            Camera cam = Camera.main;
            if (cam != null)
            {
                var camData = cam.GetComponent<UniversalAdditionalCameraData>();
                if (camData == null)
                {
                    camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                }
                camData.renderPostProcessing = true;
                camData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            }

            // 2. Setup Sun Light for URP
            Light[] lights = Object.FindObjectsByType<Light>();
            foreach (Light l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    var lightData = l.GetComponent<UniversalAdditionalLightData>();
                    if (lightData == null)
                    {
                        lightData = l.gameObject.AddComponent<UniversalAdditionalLightData>();
                    }
                    l.shadows = LightShadows.Soft;
                }
            }

            // 3. Add / Configure Global Volume with ACES Tonemapping, Bloom, and Vignette
            Volume volume = Object.FindAnyObjectByType<Volume>();
            if (volume == null)
            {
                GameObject volObj = new GameObject("Global Volume (URP)");
                volume = volObj.AddComponent<Volume>();
                volume.isGlobal = true;
            }

            if (volume.profile == null)
            {
                VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "URP_Adventure_Profile";

                // Tonemapping
                Tonemapping tonemap = profile.Add<Tonemapping>(true);
                tonemap.mode.value = TonemappingMode.ACES;

                // Bloom
                Bloom bloom = profile.Add<Bloom>(true);
                bloom.threshold.value = 1.05f;
                bloom.intensity.value = 0.45f;
                bloom.scatter.value = 0.7f;

                // Vignette
                Vignette vignette = profile.Add<Vignette>(true);
                vignette.intensity.value = 0.25f;
                vignette.smoothness.value = 0.35f;

                string profilePath = "Assets/AdventureGame/Materials/URP_PostProfile_" + scene.name + ".asset";
                AssetDatabase.CreateAsset(profile, profilePath);
                volume.profile = profile;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"<color=green><b>[URP Scene Setup] Configured URP Camera Post-Processing, Soft Light Data, and Global Volume for {scenePath}</b></color>");
        }
    }
}
