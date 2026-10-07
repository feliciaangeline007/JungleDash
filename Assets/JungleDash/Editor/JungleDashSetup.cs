// JungleDashSetup.cs - Project setup tool for Jungle Dash.
// Menu: Jungle Dash > Setup Project  /  Apply Android Settings
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JungleDash.EditorTools
{
    public static class JungleDashSetup
    {
        const string Root = "Assets/JungleDash";
        const string ResDir = Root + "/Resources";
        const string MatDir = ResDir + "/JDMaterials";
        const string SceneDir = "Assets/Scenes";
        const string ScenePath = SceneDir + "/Main.unity";

        [MenuItem("Jungle Dash/Setup Project")]
        public static void SetupMenu() { Run(true); }

        /// <summary>For -executeMethod in batch mode.</summary>
        public static void BatchSetup() { Run(false); }

        static void Run(bool verbose)
        {
            if (!Directory.Exists(MatDir)) Directory.CreateDirectory(MatDir);
            if (!Directory.Exists(SceneDir)) Directory.CreateDirectory(SceneDir);

            CreateMaterials();
            CreateGameAssets();
            CreateScene();
            ApplyAndroid();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[JungleDash] Setup complete! Open Assets/Scenes/Main.unity and press Play.");
            if (verbose) EditorUtility.DisplayDialog("Jungle Dash", "Setup complete!\nOpen Assets/Scenes/Main.unity and press Play.", "OK");
        }

        static void CreateMaterials()
        {
            Shader sh = Mats.FindShader();
            foreach (MatKind k in System.Enum.GetValues(typeof(MatKind)))
            {
                string path = MatDir + "/JD_" + k + ".mat";
                Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null)
                {
                    m = new Material(sh);
                    AssetDatabase.CreateAsset(m, path);
                }
                else
                {
                    m.shader = sh;
                }
                Mats.Configure(m, k);
                if (k == MatKind.Emissive) m.SetColor("_EmissionColor", Color.white);
                EditorUtility.SetDirty(m);
            }
        }

        static void CreateGameAssets()
        {
            string path = ResDir + "/GameAssets.asset";
            GameAssets ga = AssetDatabase.LoadAssetAtPath<GameAssets>(path);
            if (ga == null)
            {
                ga = ScriptableObject.CreateInstance<GameAssets>();
                AssetDatabase.CreateAsset(ga, path);
            }

            // Link local Resources assets (textures from Resources folder, if present)
            ga.ethan     = AssetDatabase.LoadAssetAtPath<GameObject>(ResDir + "/Ethan.fbx");
            ga.mud       = LoadTexture(ResDir + "/MudRocky.png");
            ga.grass     = LoadTexture(ResDir + "/GrassHill.png");
            ga.cliff     = ga.mud;
            ga.palm      = Billboard(ResDir + "/PalmBillboard.png");
            ga.broadleaf = Billboard(ResDir + "/BroadleafBillboard.png");

            // AI-generated textures: generate procedurally if .jpg files are missing
            ga.stonePath   = LoadTexture(ResDir + "/StonePath.jpg")   ?? ProceduralAssets.MakeStonePath_(128, 128);
            ga.carvedStone = LoadTexture(ResDir + "/CarvedStone.jpg") ?? ProceduralAssets.MakeCarvedStone_(128, 128);
            ga.treeBark    = LoadTexture(ResDir + "/TreeBark.jpg")    ?? ProceduralAssets.MakeTreeBark_(64, 128);
            ga.jungleGrass = LoadTexture(ResDir + "/JungleGrass.jpg") ?? ProceduralAssets.MakeJungleGrass_(128, 128);
            ga.goldRelic   = LoadTexture(ResDir + "/GoldRelic.jpg")   ?? ProceduralAssets.MakeGoldRelic_(64, 64);

            // Build or link Animator Controller
            string ctrlPath = ResDir + "/EthanController.controller";
            AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
            if (ctrl == null)
            {
                ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                ctrl.AddParameter("Forward",  AnimatorControllerParameterType.Float);
                ctrl.AddParameter("Turn",     AnimatorControllerParameterType.Float);
                ctrl.AddParameter("OnGround", AnimatorControllerParameterType.Bool);
                ctrl.AddParameter("Crouch",   AnimatorControllerParameterType.Bool);
                ctrl.AddParameter("Jump",     AnimatorControllerParameterType.Float);

                AnimationClip runClip  = FindClip(ResDir + "/HumanoidRun.fbx");
                AnimationClip idleClip = FindClip(ResDir + "/HumanoidIdle.fbx");

                var rootSm    = ctrl.layers[0].stateMachine;
                var idleState = rootSm.AddState("Idle"); idleState.motion = idleClip;
                var runState  = rootSm.AddState("Run");  runState.motion  = runClip;

                var toRun  = idleState.AddTransition(runState);
                toRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Forward");
                toRun.hasExitTime = false; toRun.duration = 0.15f;

                var toIdle = runState.AddTransition(idleState);
                toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Forward");
                toIdle.hasExitTime = false; toIdle.duration = 0.15f;
            }
            ga.animator = ctrl;

            EditorUtility.SetDirty(ga);
        }

        static AnimationClip FindClip(string fbxPath)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    return clip;
            }
            return null;
        }

        static Texture2D LoadTexture(string p)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        }

        static Texture2D Billboard(string p)
        {
            TextureImporter ti = AssetImporter.GetAtPath(p) as TextureImporter;
            if (ti != null && !ti.alphaIsTransparency)
            {
                ti.alphaIsTransparency = true;
                ti.SaveAndReimport();
            }
            return LoadTexture(p);
        }

        [MenuItem("Jungle Dash/Build Scene Hierarchy")]
        public static void BuildSceneHierarchyMenu()
        {
            CreateScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Jungle Dash", "Scene Hierarchy built!\nCheck Assets/Scenes/Main.unity.", "OK");
        }

        static void CreateScene()
        {
            UnityEngine.SceneManagement.Scene scene;
            if (File.Exists(ScenePath))
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            // Clean up legacy runtime objects if any
            var rootObjs = scene.GetRootGameObjects();
            foreach (var go in rootObjs)
            {
                if (go.name == "Jungle Dash Runtime" || go.name == "JungleDash" || go.name == "Player" || go.name == "World")
                {
                    Object.DestroyImmediate(go);
                }
            }

            // Camera
            Camera cam = Object.FindAnyObjectByType<Camera>();
            if (cam == null)
            {
                GameObject camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.62f, 0.82f, 0.74f);
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 150f;
            cam.fieldOfView = 58f;
            cam.transform.position = new Vector3(0f, 3.2f, -6.3f);
            cam.transform.rotation = Quaternion.Euler(14f, 0f, 0f);
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();

            // Sun / Light
            Light sun = Object.FindAnyObjectByType<Light>();
            if (sun == null)
            {
                GameObject sunGo = new GameObject("Sun");
                sun = sunGo.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.name = "Sun";
            sun.color = new Color(1f, 0.93f, 0.76f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;
            sun.transform.position = new Vector3(0f, 3f, 0f);
            sun.transform.rotation = Quaternion.Euler(50f, -28f, 0f);

            // Player
            GameObject playerGo = new GameObject("Player");
            playerGo.AddComponent<Player>();

            // World
            GameObject worldGo = new GameObject("World");
            worldGo.AddComponent<World>();

            // Game Manager
            GameObject gameGo = new GameObject("JungleDash");
            gameGo.AddComponent<GameAudio>();
            gameGo.AddComponent<Game>();

            // RenderSettings
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.52f, 0.62f, 0.58f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.62f, 0.82f, 0.74f);
            RenderSettings.fogStartDistance = 30f;
            RenderSettings.fogEndDistance = 128f;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        [MenuItem("Jungle Dash/Apply Android Settings")]
        public static void ApplyAndroid()
        {
            PlayerSettings.productName = "Jungle Dash";
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
        }
    }
}
#endif
