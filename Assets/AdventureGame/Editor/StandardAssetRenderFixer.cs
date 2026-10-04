using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using System.IO;

namespace AdventureGame.Editor
{
    [InitializeOnLoad]
    public static class StandardAssetRenderFixer
    {
        private const string FixKey = "Unity6_StandardAsset_RenderFix_v2";

        static StandardAssetRenderFixer()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool(FixKey, false))
                {
                    SessionState.SetBool(FixKey, true);
                    FixAllRenderIssues();
                }
            };
        }

        [MenuItem("Migration/Fix All Standard Asset Render Issues (SpeedTree, Water, Shaders)", false, 35)]
        public static void FixAllRenderIssues()
        {
            Debug.Log("<color=cyan><b>=== FIXING UNITY STANDARD ASSETS URP RENDER ISSUES ===</b></color>");

            FixSpeedTreeMaterials();
            FixWaterMaterials();
            FixCharacterMaterials();
            FixVehicleMaterials();
            FixEffectsAndProjectorMaterials();
            FixSceneRenderersAndCameras("Assets/AdventureGame/Scenes/AdventureIsland.unity");
            FixSceneRenderersAndCameras("Assets/AdventureGame/Scenes/MainMenu.unity");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green><b>=== ALL STANDARD ASSET RENDER ISSUES SUCCESSFULLY FIXED! ===</b></color>");
        }

        // ====================================================================
        // 1. SPEEDTREE SHADER & KEYWORD CORRECTION
        // ====================================================================
        private static void FixSpeedTreeMaterials()
        {
            Shader speedTree7 = Shader.Find("Universal Render Pipeline/Nature/SpeedTree7");
            if (speedTree7 == null)
            {
                string p = AssetDatabase.GUIDToAssetPath("0f4122b9a743b744abe2fb6a0a88868b");
                if (!string.IsNullOrEmpty(p)) speedTree7 = AssetDatabase.LoadAssetAtPath<Shader>(p);
            }

            Shader speedTreeBillboard = Shader.Find("Universal Render Pipeline/Nature/SpeedTree7 Billboard");
            if (speedTreeBillboard == null)
            {
                string p = AssetDatabase.GUIDToAssetPath("5ec81c81908db34429b4f6ddecadd3bd");
                if (!string.IsNullOrEmpty(p)) speedTreeBillboard = AssetDatabase.LoadAssetAtPath<Shader>(p);
            }

            if (speedTreeBillboard == null)
            {
                speedTreeBillboard = speedTree7;
            }

            if (speedTree7 == null)
            {
                Debug.LogWarning("[RenderFixer] URP SpeedTree7 shader could not be loaded!");
                return;
            }

            string[] speedTreeMatGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Standard Assets/Environment/SpeedTree" });
            int fixedCount = 0;

            foreach (string guid in speedTreeMatGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                string name = mat.name.ToLowerInvariant();

                if (name.Contains("billboard"))
                {
                    mat.shader = speedTreeBillboard;
                    mat.SetFloat("_Cutoff", 0.25f);
                    mat.EnableKeyword("_ALPHATEST_ON");
                    mat.SetOverrideTag("RenderType", "TransparentCutout");
                    mat.renderQueue = 2450;
                    EditorUtility.SetDirty(mat);
                    fixedCount++;
                }
                else if (name.Contains("leaves") || name.Contains("fronds") || name.Contains("needles"))
                {
                    mat.shader = speedTree7;
                    mat.SetFloat("_Cutoff", 0.33f);
                    mat.SetFloat("_TwoSided", 1f);
                    mat.EnableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("EFFECT_BUMP");

                    if (name.Contains("fronds"))
                    {
                        mat.EnableKeyword("GEOM_TYPE_FROND");
                        mat.DisableKeyword("GEOM_TYPE_LEAF");
                    }
                    else
                    {
                        mat.EnableKeyword("GEOM_TYPE_LEAF");
                        mat.DisableKeyword("GEOM_TYPE_FROND");
                    }

                    mat.DisableKeyword("GEOM_TYPE_BRANCH");
                    mat.SetOverrideTag("RenderType", "TransparentCutout");
                    mat.renderQueue = 2450;
                    EditorUtility.SetDirty(mat);
                    fixedCount++;
                }
                else if (name.Contains("branches") || name.Contains("bark"))
                {
                    mat.shader = speedTree7;
                    mat.EnableKeyword("GEOM_TYPE_BRANCH");
                    mat.DisableKeyword("GEOM_TYPE_LEAF");
                    mat.DisableKeyword("GEOM_TYPE_FROND");
                    mat.EnableKeyword("EFFECT_BUMP");
                    mat.SetOverrideTag("RenderType", "Opaque");
                    mat.renderQueue = 2000;
                    EditorUtility.SetDirty(mat);
                    fixedCount++;
                }
            }

            Debug.Log($"<color=green><b>[RenderFixer] Fixed {fixedCount} SpeedTree 7 materials with URP cutout shaders & geometry keywords.</b></color>");
        }

        // ====================================================================
        // 2. WATER SHADERS & OCEAN RENDERING
        // ====================================================================
        private static void FixWaterMaterials()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) return;

            Texture waveNormal = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Standard Assets/Environment/Water/Water4/Textures/SmallWaves.png");

            // Create or configure primary URP Ocean Water Material
            string oceanMatPath = "Assets/AdventureGame/Materials/Mat_URP_OceanWater.mat";
            Material oceanMat = AssetDatabase.LoadAssetAtPath<Material>(oceanMatPath);
            if (oceanMat == null)
            {
                oceanMat = new Material(urpLit);
                AssetDatabase.CreateAsset(oceanMat, oceanMatPath);
            }

            ConfigureWaterMaterial(oceanMat, urpLit, waveNormal, new Color(0.12f, 0.52f, 0.82f, 0.82f));

            // Also configure all standard assets water materials
            string[] waterMatPaths = new string[]
            {
                "Assets/Standard Assets/Environment/Water/Water/Materials/WaterProDaytime.mat",
                "Assets/Standard Assets/Environment/Water/Water/Materials/WaterProNighttime.mat",
                "Assets/Standard Assets/Environment/Water/Water/Materials/WaterPlaneMaterial.mat",
                "Assets/Standard Assets/Environment/Water/Water4/Materials/OceanPlaneMaterial.mat",
                "Assets/Standard Assets/Environment/Water/Water4/Materials/Water4Simple.mat",
                "Assets/Standard Assets/Environment/Water/Water4/Materials/Water4Advanced.mat",
                "Assets/Standard Assets/Environment/Water (Basic)/Materials/WaterDefault.mat",
                "Assets/Standard Assets/Environment/Water (Basic)/Materials/WaterBasicDaytime.mat",
                "Assets/Standard Assets/Environment/Water (Basic)/Materials/WaterBasicNighttime.mat"
            };

            foreach (var path in waterMatPaths)
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m != null)
                {
                    ConfigureWaterMaterial(m, urpLit, waveNormal, new Color(0.12f, 0.50f, 0.78f, 0.80f));
                }
            }

            Debug.Log("<color=green><b>[RenderFixer] Configured URP transparent ocean water materials with wave normal maps.</b></color>");
        }

        private static void ConfigureWaterMaterial(Material mat, Shader shader, Texture normalTex, Color color)
        {
            mat.shader = shader;
            mat.SetFloat("_Surface", 1f); // Transparent
            mat.SetFloat("_Blend", 0f);   // Alpha
            mat.SetFloat("_ZWrite", 0f);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", 0.94f);
            mat.SetFloat("_Metallic", 0.05f);

            if (normalTex != null)
            {
                mat.SetTexture("_BumpMap", normalTex);
                mat.EnableKeyword("_NORMALMAP");
            }

            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 3000;
            EditorUtility.SetDirty(mat);
        }

        // ====================================================================
        // 3. ETHAN CHARACTER SHADER & METALLIC FIX
        // ====================================================================
        private static void FixCharacterMaterials()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) return;

            Texture normalMap = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Standard Assets/Characters/ThirdPersonCharacter/Textures/EthanNormals.png");
            Texture occMap = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Standard Assets/Characters/ThirdPersonCharacter/Textures/EthanOcclusion.png");

            Material ethanWhite = AssetDatabase.LoadAssetAtPath<Material>("Assets/Standard Assets/Characters/ThirdPersonCharacter/Materials/EthanWhite.mat");
            if (ethanWhite != null)
            {
                ethanWhite.shader = urpLit;
                ethanWhite.SetColor("_BaseColor", new Color(0.92f, 0.92f, 0.94f, 1f));
                ethanWhite.SetFloat("_Smoothness", 0.45f);
                ethanWhite.SetFloat("_Metallic", 0.1f);
                if (normalMap != null)
                {
                    ethanWhite.SetTexture("_BumpMap", normalMap);
                    ethanWhite.EnableKeyword("_NORMALMAP");
                }
                if (occMap != null)
                {
                    ethanWhite.SetTexture("_OcclusionMap", occMap);
                    ethanWhite.EnableKeyword("_OCCLUSIONMAP");
                }
                EditorUtility.SetDirty(ethanWhite);
            }

            Material ethanGrey = AssetDatabase.LoadAssetAtPath<Material>("Assets/Standard Assets/Characters/ThirdPersonCharacter/Materials/EthanGrey.mat");
            if (ethanGrey != null)
            {
                ethanGrey.shader = urpLit;
                ethanGrey.SetColor("_BaseColor", new Color(0.24f, 0.25f, 0.28f, 1f));
                ethanGrey.SetFloat("_Smoothness", 0.5f);
                ethanGrey.SetFloat("_Metallic", 0.15f);
                if (normalMap != null)
                {
                    ethanGrey.SetTexture("_BumpMap", normalMap);
                    ethanGrey.EnableKeyword("_NORMALMAP");
                }
                if (occMap != null)
                {
                    ethanGrey.SetTexture("_OcclusionMap", occMap);
                    ethanGrey.EnableKeyword("_OCCLUSIONMAP");
                }
                EditorUtility.SetDirty(ethanGrey);
            }

            Debug.Log("<color=green><b>[RenderFixer] Fixed Ethan character skin, normal maps, and specular settings.</b></color>");
        }

        // ====================================================================
        // 4. SKYCAR MATERIALS FIX
        // ====================================================================
        private static void FixVehicleMaterials()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) return;

            // Car Paint Gloss
            Material bodyWhite = AssetDatabase.LoadAssetAtPath<Material>("Assets/Standard Assets/Vehicles/Car/Materials/SkyCarBodyWhite.mat");
            if (bodyWhite != null)
            {
                bodyWhite.shader = urpLit;
                bodyWhite.SetColor("_BaseColor", Color.white);
                bodyWhite.SetFloat("_Smoothness", 0.9f);
                bodyWhite.SetFloat("_Metallic", 0.35f);
                EditorUtility.SetDirty(bodyWhite);
            }

            Material bodyGrey = AssetDatabase.LoadAssetAtPath<Material>("Assets/Standard Assets/Vehicles/Car/Materials/SkyCarBodyGrey.mat");
            if (bodyGrey != null)
            {
                bodyGrey.shader = urpLit;
                bodyGrey.SetColor("_BaseColor", new Color(0.26f, 0.27f, 0.3f, 1f));
                bodyGrey.SetFloat("_Smoothness", 0.88f);
                bodyGrey.SetFloat("_Metallic", 0.65f);
                EditorUtility.SetDirty(bodyGrey);
            }

            // Glass Light Covers
            Material lightCover = AssetDatabase.LoadAssetAtPath<Material>("Assets/Standard Assets/Vehicles/Car/Materials/SkyCarLightCoversWhite.mat");
            if (lightCover != null)
            {
                lightCover.shader = urpLit;
                lightCover.SetFloat("_Surface", 1f);
                lightCover.SetFloat("_Blend", 0f);
                lightCover.SetFloat("_ZWrite", 0f);
                lightCover.SetColor("_BaseColor", new Color(0.92f, 0.96f, 1f, 0.35f));
                lightCover.SetFloat("_Smoothness", 0.98f);
                lightCover.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                lightCover.SetOverrideTag("RenderType", "Transparent");
                lightCover.renderQueue = 3000;
                EditorUtility.SetDirty(lightCover);
            }

            Debug.Log("<color=green><b>[RenderFixer] Fixed SkyCar automotive clearcoat and transparent glass materials.</b></color>");
        }

        // ====================================================================
        // 5. EFFECTS, PROJECTORS, GLASS & TOON MATERIALS FIX
        // ====================================================================
        private static void FixEffectsAndProjectorMaterials()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) return;

            // 1. Glass Refractive Material
            Material glass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Standard Assets/Effects/GlassRefraction/Materials/GlassRefractive.mat");
            if (glass != null)
            {
                glass.shader = urpLit;
                glass.SetFloat("_Surface", 1f); // Transparent
                glass.SetFloat("_Blend", 0f);   // Alpha
                glass.SetFloat("_ZWrite", 0f);
                glass.SetColor("_BaseColor", new Color(0.85f, 0.94f, 1f, 0.35f));
                glass.SetFloat("_Smoothness", 0.96f);
                glass.SetFloat("_Metallic", 0.05f);
                glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                glass.SetOverrideTag("RenderType", "Transparent");
                glass.renderQueue = 3000;
                EditorUtility.SetDirty(glass);
            }

            // 2. Projectors (Grid, Light, Shadow)
            string[] projPaths = new string[]
            {
                "Assets/Standard Assets/Effects/Projectors/Materials/GridProjector.mat",
                "Assets/Standard Assets/Effects/Projectors/Materials/LightProjector.mat",
                "Assets/Standard Assets/Effects/Projectors/Materials/ShadowProjector.mat"
            };

            foreach (var path in projPaths)
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m != null)
                {
                    m.shader = urpLit;
                    // If _BaseMap is missing but _ShadowTex is present, map it
                    if (m.GetTexture("_BaseMap") == null && m.HasProperty("_ShadowTex"))
                    {
                        Texture shadowTex = m.GetTexture("_ShadowTex");
                        if (shadowTex != null)
                        {
                            m.SetTexture("_BaseMap", shadowTex);
                        }
                    }

                    m.SetFloat("_Surface", 1f);
                    m.SetFloat("_Blend", 0f);
                    m.SetFloat("_ZWrite", 0f);
                    m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    m.SetOverrideTag("RenderType", "Transparent");
                    m.renderQueue = 3000;
                    EditorUtility.SetDirty(m);
                }
            }

            // 3. Toon Materials
            string[] toonPaths = new string[]
            {
                "Assets/Standard Assets/Effects/ToonShading/Materials/ToonLit.mat",
                "Assets/Standard Assets/Effects/ToonShading/Materials/ToonBasic.mat",
                "Assets/Standard Assets/Effects/ToonShading/Materials/ToonLitOutline.mat",
                "Assets/Standard Assets/Effects/ToonShading/Materials/ToonBasicOutline.mat"
            };

            foreach (var path in toonPaths)
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m != null)
                {
                    m.shader = urpLit;
                    m.SetFloat("_Smoothness", 0.15f);
                    m.SetFloat("_Metallic", 0f);
                    EditorUtility.SetDirty(m);
                }
            }

            Debug.Log("<color=green><b>[RenderFixer] Configured URP GlassRefraction, Projectors, and Toon materials.</b></color>");
        }

        // ====================================================================
        // 6. SCENE RENDERERS, OCEAN, AND CAMERAS
        // ====================================================================
        private static void FixSceneRenderersAndCameras(string scenePath)
        {
            if (!File.Exists(scenePath)) return;

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) return;

            // Remove obsolete FlareLayer or GUILayer on any camera
            Camera[] cameras = Object.FindObjectsByType<Camera>();
            foreach (Camera cam in cameras)
            {
                var camData = cam.GetComponent<UniversalAdditionalCameraData>();
                if (camData == null) camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                camData.renderPostProcessing = true;
                camData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            }

            // Ensure Directional Lights have soft URP shadows
            Light[] lights = Object.FindObjectsByType<Light>();
            foreach (Light l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    l.shadows = LightShadows.Soft;
                    var lightData = l.GetComponent<UniversalAdditionalLightData>();
                    if (lightData == null) l.gameObject.AddComponent<UniversalAdditionalLightData>();
                }
            }

            // In AdventureIsland, ensure ocean has MeshRenderer with water material
            if (scenePath.Contains("AdventureIsland"))
            {
                Material waterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/AdventureGame/Materials/Mat_URP_OceanWater.mat");
                GameObject sea = GameObject.Find("3D_Ocean_Surface");
                if (sea == null) sea = GameObject.Find("Sea");
                if (sea == null) sea = GameObject.Find("Ocean_Water");

                if (sea != null)
                {
                    var mf = sea.GetComponent<MeshFilter>();
                    if (mf == null) mf = sea.AddComponent<MeshFilter>();

                    var mr = sea.GetComponent<MeshRenderer>();
                    if (mr == null) mr = sea.AddComponent<MeshRenderer>();

                    if (waterMat != null)
                    {
                        mr.sharedMaterial = waterMat;
                    }

                    // Attach WaterScroll
                    var scroll = sea.GetComponent<WaterScroll>();
                    if (scroll == null) sea.AddComponent<WaterScroll>();
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"<color=green><b>[RenderFixer] Cleaned cameras and renderers for scene: {scenePath}</b></color>");
        }

        // ====================================================================
        // 7. PACKAGE EXPORT
        // ====================================================================
        [MenuItem("Migration/Export Standard Assets for Unity 6 UnityPackage", false, 40)]
        public static void ExportStandardAssetsPackage()
        {
            FixAllRenderIssues();

            string packagePath = "/Users/fabrianivan/Downloads/Standard Assets for Unity 6.unitypackage";
            string[] exportPaths = new string[]
            {
                "Assets/Standard Assets",
                "Assets/SampleScenes"
            };

            Debug.Log($"<color=cyan><b>[PackageExporter] Exporting clean URP Standard Assets to: {packagePath}</b></color>");
            AssetDatabase.ExportPackage(exportPaths, packagePath, ExportPackageOptions.Recurse);
            Debug.Log($"<color=green><b>[PackageExporter] Successfully generated {packagePath}!</b></color>");
        }
    }
}
