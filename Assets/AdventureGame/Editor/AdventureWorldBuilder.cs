using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;

namespace AdventureGame.Editor
{
    [InitializeOnLoad]
    public static class AdventureWorldBuilder
    {
        private const string MainMenuScenePath = "Assets/AdventureGame/Scenes/MainMenu.unity";
        private const string GameScenePath = "Assets/AdventureGame/Scenes/AdventureIsland.unity";

        // Asset Paths from legacy Standard Assets and SampleScenes
        private const string SkyboxPath = "Assets/SampleScenes/Materials/SkyboxProcedural.mat";
        private const string SeaPrefabPath = "Assets/SampleScenes/Prefabs/Sea.prefab";
        private const string GroundArenaPath = "Assets/SampleScenes/Prefabs/GroundArena.prefab";
        private const string GroundRunwayPath = "Assets/SampleScenes/Prefabs/GroundRunway.prefab";
        private const string GroundTrackPath = "Assets/SampleScenes/Prefabs/GroundTrack.prefab";
        private const string GroundObstaclesPath = "Assets/SampleScenes/Prefabs/GroundObstacles.prefab";
        private const string HousePrefabPath = "Assets/SampleScenes/Prefabs/House.prefab";
        private const string PillarPrefabPath = "Assets/SampleScenes/Prefabs/Pillar.prefab";
        private const string WallPrefabPath = "Assets/SampleScenes/Prefabs/Wall.prefab";
        private const string SmashBoxesPath = "Assets/SampleScenes/Prefabs/SmashBoxes.prefab";
        private const string BoxPilePath = "Assets/SampleScenes/Prefabs/BoxPile.prefab";
        private const string RingPrefabPath = "Assets/SampleScenes/Prefabs/Ring.prefab";
        private const string PickupPrefabPath = "Assets/SampleScenes/Prefabs/Pickup.prefab";
        
        // SpeedTree Assets
        private const string BroadleafTreePath = "Assets/Standard Assets/Environment/SpeedTree/Broadleaf/Broadleaf_Mobile.spm";
        private const string ConiferTreePath = "Assets/Standard Assets/Environment/SpeedTree/Conifer/Conifer_Desktop.spm";
        private const string PalmTreePath = "Assets/Standard Assets/Environment/SpeedTree/Palm/Palm_Desktop.spm";

        // Prototyping Assets
        private const string StepsPrefabPath = "Assets/Standard Assets/Prototyping/Prefabs/StepsPrototype04x02x02.prefab";
        private const string RampPrefabPath = "Assets/Standard Assets/Prototyping/Prefabs/RampPrototype04x02x02.prefab";
        private const string PillarTallPrefabPath = "Assets/Standard Assets/Prototyping/Prefabs/PillarPrototype02x08x02.prefab";
        private const string FloorLargePrefabPath = "Assets/Standard Assets/Prototyping/Prefabs/FloorPrototype64x01x64.prefab";

        // Characters & Vehicles
        private const string PlayerPrefabPath = "Assets/Standard Assets/Characters/ThirdPersonCharacter/Prefabs/ThirdPersonController.prefab";
        private const string AICharacterPrefabPath = "Assets/Standard Assets/Characters/ThirdPersonCharacter/Prefabs/AIThirdPersonController.prefab";
        private const string CarPrefabPath = "Assets/Standard Assets/Vehicles/Car/Prefabs/Car.prefab";
        private const string CameraRigPrefabPath = "Assets/Standard Assets/Cameras/Prefabs/MultipurposeCameraRig.prefab";
        private const string MobileControlsPrefabPath = "Assets/Standard Assets/CrossPlatformInput/Prefabs/MobileSingleStickControl.prefab";

        // Particles
        private const string FirePrefabPath = "Assets/Standard Assets/ParticleSystems/Prefabs/FireMobile.prefab";
        private const string FlarePrefabPath = "Assets/Standard Assets/ParticleSystems/Prefabs/FlareMobile.prefab";
        private const string DustPrefabPath = "Assets/Standard Assets/ParticleSystems/Prefabs/DustStormMobile.prefab";
        private const string FireworksPrefabPath = "Assets/Standard Assets/ParticleSystems/Prefabs/Fireworks.prefab";

        static AdventureWorldBuilder()
        {
            EditorApplication.delayCall += CheckAndBuildAll;
        }

        private static void CheckAndBuildAll()
        {
            if (!File.Exists(MainMenuScenePath) || !File.Exists(GameScenePath))
            {
                BuildCompleteGame();
            }
        }

        [MenuItem("Adventure Game/Recreate Complete Game With Raw Assets", false, 1)]
        public static void BuildCompleteGame()
        {
            Debug.Log("<color=cyan><b>=== RECREATING COMPLETE GAME WITH STANDARD ASSETS & SAMPLE SCENES ===</b></color>");

            BuildMainMenuScene();
            BuildIslandScene();

            // Set up EditorBuildSettings with both scenes
            EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true)
            };
            EditorBuildSettings.scenes = scenes;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green><b>[AdventureWorldBuilder] GAME SUCCESSFULLY RECREATED WITH RICH 3D ASSETS! MainMenu (Scene 0) & AdventureIsland (Scene 1) are active.</b></color>");
        }

        // ====================================================================
        // 1. MAIN MENU SCENE
        // ====================================================================
        public static void BuildMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Lighting & Atmosphere
            GameObject sunObj = new GameObject("Sun_Light");
            Light sun = sunObj.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1.0f, 0.95f, 0.85f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sunObj.transform.rotation = Quaternion.Euler(35f, -40f, 0f);
            var sunData = sunObj.AddComponent<UniversalAdditionalLightData>();

            Material skyMat = AssetDatabase.LoadAssetAtPath<Material>(SkyboxPath);
            if (skyMat != null) RenderSettings.skybox = skyMat;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.5f, 0.6f);

            // Background Altar with rotating Ethan character
            GameObject bgRoot = new GameObject("--- MENU BACKGROUND ---");

            // Scenic Pedestal
            GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestal.name = "Showcase_Pedestal";
            pedestal.transform.parent = bgRoot.transform;
            pedestal.transform.position = new Vector3(0, -0.6f, 4.5f);
            pedestal.transform.localScale = new Vector3(3.5f, 0.6f, 3.5f);
            Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/SampleScenes/Materials/NavyDarkSmooth.mat");
            if (stoneMat != null) pedestal.GetComponent<Renderer>().sharedMaterial = stoneMat;

            // Showcase Ethan Character
            GameObject ethanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (ethanPrefab != null)
            {
                GameObject ethan = (GameObject)PrefabUtility.InstantiatePrefab(ethanPrefab);
                ethan.name = "Ethan_Showcase";
                ethan.transform.parent = bgRoot.transform;
                ethan.transform.position = new Vector3(0, 0f, 4.5f);
                ethan.transform.rotation = Quaternion.Euler(0, 165f, 0);

                // Disable player control scripts on menu dummy
                var userControl = ethan.GetComponent<UnityStandardAssets.Characters.ThirdPerson.ThirdPersonUserControl>();
                if (userControl != null) Object.DestroyImmediate(userControl);
                var rb = ethan.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;

                // Add gentle rotation
                var rotator = ethan.AddComponent<MenuCharacterRotator>();
                rotator.turnSpeed = 15f;
            }

            // SpeedTree Palms flanking the altar
            SpawnAsset(PalmTreePath, new Vector3(-3.2f, -0.6f, 5.5f), Quaternion.Euler(0, 30, 0), bgRoot.transform, new Vector3(0.8f, 0.8f, 0.8f));
            SpawnAsset(PalmTreePath, new Vector3(3.2f, -0.6f, 5.5f), Quaternion.Euler(0, -45, 0), bgRoot.transform, new Vector3(0.8f, 0.8f, 0.8f));

            // Flaming Torches
            SpawnAsset(FirePrefabPath, new Vector3(-1.8f, 0f, 4.2f), Quaternion.identity, bgRoot.transform);
            SpawnAsset(FirePrefabPath, new Vector3(1.8f, 0f, 4.2f), Quaternion.identity, bgRoot.transform);

            // Floating Spinning 3D Pickup Diamond
            GameObject diamond = SpawnAsset(PickupPrefabPath, new Vector3(1.2f, 1.4f, 4.0f), Quaternion.identity, bgRoot.transform);
            if (diamond != null)
            {
                var gem = diamond.AddComponent<CollectibleGem>();
                gem.rotateSpeed = 50f;
                gem.bobHeight = 0.15f;
            }

            // Camera
            GameObject camObj = new GameObject("Menu_Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
            camObj.transform.position = new Vector3(0f, 1.3f, 0.5f);
            camObj.transform.rotation = Quaternion.Euler(6f, 0f, 0f);

            var camData = camObj.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;

            // Global Volume with ACES
            SetupPostProcessing(bgRoot.transform, "Menu_URP_PostProfile");

            // UI Canvas
            BuildMainMenuCanvas();

            // Save Scene
            Directory.CreateDirectory(Path.GetDirectoryName(MainMenuScenePath));
            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        }

        // ====================================================================
        // 2. ADVENTURE ISLAND SCENE (Rebuilt with real 3D assets)
        // ====================================================================
        public static void BuildIslandScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Lighting & Atmosphere
            GameObject sunObj = new GameObject("Directional Light - Sun");
            Light sun = sunObj.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1.0f, 0.96f, 0.88f);
            sun.intensity = 1.3f;
            sun.shadows = LightShadows.Soft;
            sun.shadowNormalBias = 0.4f;
            sunObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var sunData = sunObj.AddComponent<UniversalAdditionalLightData>();

            Material skyMat = AssetDatabase.LoadAssetAtPath<Material>(SkyboxPath);
            if (skyMat != null) RenderSettings.skybox = skyMat;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1.15f;
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.70f, 0.85f, 0.98f);
            RenderSettings.fogDensity = 0.0035f;

            // World Root
            GameObject worldRoot = new GameObject("--- 3D ADVENTURE WORLD ---");

            // 2. Vast 3D Ocean Surface
            GameObject sea = SpawnAsset(SeaPrefabPath, new Vector3(0, -1.0f, 0), Quaternion.identity, worldRoot.transform);
            if (sea != null) sea.name = "3D_Ocean_Surface";

            // Ocean Fall Hazard Trigger
            GameObject hazardObj = new GameObject("Ocean_Hazard_Trigger");
            hazardObj.transform.parent = worldRoot.transform;
            hazardObj.transform.position = new Vector3(0, -2.5f, 0);
            BoxCollider hazardCol = hazardObj.AddComponent<BoxCollider>();
            hazardCol.size = new Vector3(600f, 3f, 600f);
            hazardCol.isTrigger = true;
            hazardObj.AddComponent<WaterHazard>();

            // 3. Central Temple Arena (GroundArena.prefab)
            GameObject arena = SpawnAsset(GroundArenaPath, new Vector3(0, 0, 0), Quaternion.identity, worldRoot.transform);
            if (arena != null) arena.name = "Central_Temple_Arena";

            // 4. Elevated Coastal Runway (GroundRunway.prefab)
            GameObject runway = SpawnAsset(GroundRunwayPath, new Vector3(55f, 0f, 0f), Quaternion.Euler(0, 90f, 0), worldRoot.transform);
            if (runway != null) runway.name = "Coastal_Runway_Bridge";

            // 5. Grand Exploration Circuit (GroundTrack.prefab)
            GameObject track = SpawnAsset(GroundTrackPath, new Vector3(0, 0, -65f), Quaternion.identity, worldRoot.transform);
            if (track != null) track.name = "Valley_Exploration_Track";

            // 6. Platforming Obstacles & Ramps (GroundObstacles.prefab)
            GameObject obstacles = SpawnAsset(GroundObstaclesPath, new Vector3(-35f, 0f, 15f), Quaternion.Euler(0, 45f, 0), worldRoot.transform);
            if (obstacles != null) obstacles.name = "Platforming_Obstacles_Area";

            // 7. Explorer Outpost Village (House.prefab & Crates)
            GameObject villageRoot = new GameObject("--- EXPLORER OUTPOST VILLAGE ---");
            villageRoot.transform.parent = worldRoot.transform;

            SpawnAsset(HousePrefabPath, new Vector3(75f, 0f, 25f), Quaternion.Euler(0, -30f, 0), villageRoot.transform);
            SpawnAsset(HousePrefabPath, new Vector3(92f, 0f, 5f), Quaternion.Euler(0, 180f, 0), villageRoot.transform);
            SpawnAsset(HousePrefabPath, new Vector3(82f, 0f, -22f), Quaternion.Euler(0, 45f, 0), villageRoot.transform);

            // Destructible Physics Crate Piles (SmashBoxes.prefab & BoxPile.prefab)
            SpawnAsset(SmashBoxesPath, new Vector3(65f, 0f, 15f), Quaternion.identity, villageRoot.transform);
            SpawnAsset(SmashBoxesPath, new Vector3(50f, 0f, -12f), Quaternion.identity, villageRoot.transform);
            SpawnAsset(BoxPilePath, new Vector3(30f, 0f, -45f), Quaternion.identity, villageRoot.transform);
            SpawnAsset(BoxPilePath, new Vector3(-20f, 0f, -50f), Quaternion.identity, villageRoot.transform);

            // 8. Ancient Ruin Temple (Pillars & Walls)
            GameObject ruinsRoot = new GameObject("--- ANCIENT RUINS SANCTUARY ---");
            ruinsRoot.transform.parent = worldRoot.transform;

            Vector3 ruinsCenter = new Vector3(-5f, 0f, 30f);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                Vector3 pPos = ruinsCenter + new Vector3(Mathf.Cos(angle) * 12f, 0, Mathf.Sin(angle) * 12f);
                GameObject pillar = SpawnAsset(PillarPrefabPath, pPos, Quaternion.identity, ruinsRoot.transform);

                // Add torch atop every alternate pillar
                if (i % 2 == 0)
                {
                    SpawnAsset(FirePrefabPath, pPos + Vector3.up * 4.2f, Quaternion.identity, ruinsRoot.transform);
                }
            }

            SpawnAsset(WallPrefabPath, ruinsCenter + new Vector3(-12f, 0, 0), Quaternion.Euler(0, 90f, 0), ruinsRoot.transform);
            SpawnAsset(WallPrefabPath, ruinsCenter + new Vector3(12f, 0, 0), Quaternion.Euler(0, 90f, 0), ruinsRoot.transform);

            // Altar with Ruby Relic Key
            GameObject altar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            altar.name = "Altar_Ruby";
            altar.transform.parent = ruinsRoot.transform;
            altar.transform.position = ruinsCenter;
            altar.transform.localScale = new Vector3(2.5f, 0.8f, 2.5f);
            Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/SampleScenes/Materials/NavyDarkSmooth.mat");
            if (stoneMat != null) altar.GetComponent<Renderer>().sharedMaterial = stoneMat;

            CreateRelicKey(ruinsRoot.transform, ruinsCenter + Vector3.up * 1.5f, KeyType.Ruby, Color.red, "Ancient Ruby Key");

            // 9. Grand Mountain Summit & Sacred Shrine
            GameObject summitRoot = new GameObject("--- SACRED SUMMIT SHINE ---");
            summitRoot.transform.parent = worldRoot.transform;

            Vector3 summitBase = new Vector3(0f, 0f, 65f);

            // Grand Stepped Staircase to Summit
            SpawnAsset(StepsPrefabPath, new Vector3(0f, 0f, 48f), Quaternion.identity, summitRoot.transform, new Vector3(3f, 2f, 2f));
            SpawnAsset(StepsPrefabPath, new Vector3(0f, 2.0f, 54f), Quaternion.identity, summitRoot.transform, new Vector3(3f, 2f, 2f));
            SpawnAsset(StepsPrefabPath, new Vector3(0f, 4.0f, 60f), Quaternion.identity, summitRoot.transform, new Vector3(3f, 2f, 2f));

            // Raised Summit Platform
            GameObject summitPlat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            summitPlat.name = "Summit_Sanctuary_Floor";
            summitPlat.transform.parent = summitRoot.transform;
            summitPlat.transform.position = new Vector3(0, 5.0f, 72f);
            summitPlat.transform.localScale = new Vector3(26f, 2f, 26f);
            if (stoneMat != null) summitPlat.GetComponent<Renderer>().sharedMaterial = stoneMat;

            // Grand Monolithic Columns
            SpawnAsset(PillarTallPrefabPath, new Vector3(-8f, 6.0f, 68f), Quaternion.identity, summitRoot.transform);
            SpawnAsset(PillarTallPrefabPath, new Vector3(8f, 6.0f, 68f), Quaternion.identity, summitRoot.transform);
            SpawnAsset(PillarTallPrefabPath, new Vector3(-8f, 6.0f, 78f), Quaternion.identity, summitRoot.transform);
            SpawnAsset(PillarTallPrefabPath, new Vector3(8f, 6.0f, 78f), Quaternion.identity, summitRoot.transform);

            // Torches on columns
            SpawnAsset(FirePrefabPath, new Vector3(-8f, 10.2f, 68f), Quaternion.identity, summitRoot.transform);
            SpawnAsset(FirePrefabPath, new Vector3(8f, 10.2f, 68f), Quaternion.identity, summitRoot.transform);

            // Rotating Sacred Ring
            SpawnAsset(RingPrefabPath, new Vector3(0f, 9.5f, 75f), Quaternion.Euler(0, 0, 90f), summitRoot.transform);

            // Summit Shrine Portal
            BuildShrinePortal(summitRoot.transform, new Vector3(0f, 6.0f, 75f));

            // 10. Sky Island Platform & Emerald Key
            GameObject skyRoot = new GameObject("--- SKY ISLAND PLATFORM ---");
            skyRoot.transform.parent = worldRoot.transform;

            GameObject skyPlat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            skyPlat.name = "Sky_Island_Base";
            skyPlat.transform.parent = skyRoot.transform;
            skyPlat.transform.position = new Vector3(-35f, 9f, 40f);
            skyPlat.transform.localScale = new Vector3(14f, 1.2f, 14f);
            if (stoneMat != null) skyPlat.GetComponent<Renderer>().sharedMaterial = stoneMat;

            // Moving Platform Elevator connecting Obstacles area to Sky Island
            GameObject movingPlat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            movingPlat.name = "Moving_Platform_Elevator";
            movingPlat.transform.parent = skyRoot.transform;
            movingPlat.transform.position = new Vector3(-35f, 1.5f, 22f);
            movingPlat.transform.localScale = new Vector3(5f, 0.6f, 5f);
            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/SampleScenes/Materials/OrangeSmooth.mat");
            if (woodMat != null) movingPlat.GetComponent<Renderer>().sharedMaterial = woodMat;
            var mp = movingPlat.AddComponent<MovingPlatform>();
            mp.moveOffset = new Vector3(0f, 7.5f, 15f);
            mp.speed = 3.5f;

            CreateRelicKey(skyRoot.transform, new Vector3(-35f, 10.5f, 40f), KeyType.Emerald, Color.green, "Ancient Emerald Key");

            // 11. Coastal Pier & Sapphire Key
            GameObject pierRoot = new GameObject("--- COASTAL PIER ---");
            pierRoot.transform.parent = worldRoot.transform;

            GameObject pierFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pierFloor.name = "Village_Pier";
            pierFloor.transform.parent = pierRoot.transform;
            pierFloor.transform.position = new Vector3(105f, 0.4f, 5f);
            pierFloor.transform.localScale = new Vector3(16f, 0.8f, 6f);
            if (woodMat != null) pierFloor.GetComponent<Renderer>().sharedMaterial = woodMat;

            CreateRelicKey(pierRoot.transform, new Vector3(110f, 1.8f, 5f), KeyType.Sapphire, Color.cyan, "Ancient Sapphire Key");

            // 12. SpeedTree Vegetation (Broadleaf, Conifer, Palm)
            GameObject foliageRoot = new GameObject("--- SPEEDTREE VEGETATION ---");
            foliageRoot.transform.parent = worldRoot.transform;

            // Broadleaf trees around village & arena borders
            Vector3[] broadleafSpots = new Vector3[]
            {
                new Vector3(65f, 0f, 35f),
                new Vector3(88f, 0f, 32f),
                new Vector3(72f, 0f, -8f),
                new Vector3(92f, 0f, -15f),
                new Vector3(20f, 0f, 20f),
                new Vector3(-18f, 0f, -25f)
            };
            foreach (var pos in broadleafSpots)
            {
                SpawnAsset(BroadleafTreePath, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), foliageRoot.transform, Vector3.one * Random.Range(0.85f, 1.15f));
            }

            // Conifer pine trees on mountain slopes & peaks
            Vector3[] coniferSpots = new Vector3[]
            {
                new Vector3(-16f, 3.5f, 60f),
                new Vector3(16f, 3.5f, 60f),
                new Vector3(-14f, 5.5f, 72f),
                new Vector3(14f, 5.5f, 72f),
                new Vector3(-25f, 0f, 50f),
                new Vector3(25f, 0f, 50f)
            };
            foreach (var pos in coniferSpots)
            {
                SpawnAsset(ConiferTreePath, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), foliageRoot.transform, Vector3.one * Random.Range(0.9f, 1.25f));
            }

            // Coastal Palm trees along waterways & runways
            Vector3[] palmSpots = new Vector3[]
            {
                new Vector3(45f, 0f, 12f),
                new Vector3(45f, 0f, -12f),
                new Vector3(60f, 0f, -30f),
                new Vector3(98f, 0f, 15f),
                new Vector3(112f, 0f, -5f),
                new Vector3(-35f, 0f, -15f),
                new Vector3(-45f, 0f, 0f),
                new Vector3(-45f, 0f, 30f)
            };
            foreach (var pos in palmSpots)
            {
                SpawnAsset(PalmTreePath, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), foliageRoot.transform, Vector3.one * Random.Range(0.8f, 1.1f));
            }

            // 13. Drivable Vehicle (Car.prefab)
            GameObject car = SpawnAsset(CarPrefabPath, new Vector3(40f, 0.4f, 0f), Quaternion.Euler(0, 90f, 0), worldRoot.transform);
            if (car != null)
            {
                car.name = "Drivable_SkyCar";
                var mount = car.AddComponent<VehicleMount>();
                mount.carObject = car;
            }

            // 14. 3 Animated Humanoid Guardians (AIThirdPersonController.prefab)
            GameObject enemiesRoot = new GameObject("--- ANCIENT GUARDIANS (AI ETHANS) ---");
            enemiesRoot.transform.parent = worldRoot.transform;

            // Guardian 1: Ruins Sanctuary Guardian (defending Ruby Key)
            SpawnAnimatedGuardian(enemiesRoot.transform, new Vector3(-5f, 0.2f, 30f), new Vector3[]
            {
                new Vector3(-10f, 0.2f, 30f),
                new Vector3(-5f, 0.2f, 35f),
                new Vector3(0f, 0.2f, 30f),
                new Vector3(-5f, 0.2f, 25f)
            }, "Guardian_Ruins");

            // Guardian 2: Outpost Sentinel (defending Sapphire Key)
            SpawnAnimatedGuardian(enemiesRoot.transform, new Vector3(95f, 0.2f, 5f), new Vector3[]
            {
                new Vector3(90f, 0.2f, 15f),
                new Vector3(105f, 0.2f, 5f),
                new Vector3(90f, 0.2f, -10f)
            }, "Guardian_Outpost");

            // Guardian 3: Sky Platform Guardian (defending Emerald Key)
            SpawnAnimatedGuardian(enemiesRoot.transform, new Vector3(-35f, 9.2f, 40f), new Vector3[]
            {
                new Vector3(-39f, 9.2f, 43f),
                new Vector3(-31f, 9.2f, 43f),
                new Vector3(-31f, 9.2f, 37f),
                new Vector3(-39f, 9.2f, 37f)
            }, "Guardian_SkyPlatform");

            // 15. 15 Authentic 3D Spinning Diamond Crystals (Pickup.prefab)
            BuildDiamonds(worldRoot.transform);

            // 16. Player Character (ThirdPersonController.prefab)
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            GameObject playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            playerInstance.name = "ThirdPersonPlayer";
            playerInstance.tag = "Player";
            playerInstance.transform.position = new Vector3(0f, 0.15f, -10f);

            var health = playerInstance.AddComponent<PlayerHealth>();
            health.maxHealth = 3;
            health.currentHealth = 3;

            var stamina = playerInstance.AddComponent<PlayerStamina>();
            stamina.maxStamina = 100f;

            var pAudio = playerInstance.AddComponent<PlayerAudioEffects>();
            pAudio.footstepClips = new AudioClip[]
            {
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Standard Assets/Characters/FirstPersonCharacter/Audio/Footstep01.wav"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Standard Assets/Characters/FirstPersonCharacter/Audio/Footstep02.wav"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Standard Assets/Characters/FirstPersonCharacter/Audio/Footstep03.wav"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Standard Assets/Characters/FirstPersonCharacter/Audio/Footstep04.wav")
            };
            pAudio.jumpClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Standard Assets/Characters/FirstPersonCharacter/Audio/Jump.wav");
            pAudio.landClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Standard Assets/Characters/FirstPersonCharacter/Audio/Land.wav");

            // 17. Smooth Follow Camera (MultipurposeCameraRig.prefab)
            GameObject camPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CameraRigPrefabPath);
            if (camPrefab != null)
            {
                GameObject camInstance = (GameObject)PrefabUtility.InstantiatePrefab(camPrefab);
                camInstance.name = "MultipurposeCameraRig";
                var follower = camInstance.GetComponent<UnityStandardAssets.Cameras.AbstractTargetFollower>();
                if (follower != null) follower.SetTarget(playerInstance.transform);

                Camera mainCam = camInstance.GetComponentInChildren<Camera>();
                if (mainCam != null)
                {
                    var camData = mainCam.GetComponent<UniversalAdditionalCameraData>();
                    if (camData == null) camData = mainCam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                    camData.renderPostProcessing = true;
                    camData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                }
            }

            // 18. Atmospheric Particles
            SpawnAsset(DustPrefabPath, new Vector3(0, 1.5f, 0), Quaternion.identity, worldRoot.transform);

            // 19. Post-Processing Global Volume
            SetupPostProcessing(worldRoot.transform, "Game_URP_PostProfile");

            // 20. HUD & Controls
            BuildCompleteHUD(playerInstance);

            // 21. Game Manager
            GameObject gmObj = new GameObject("GameManager");
            GameManager gm = gmObj.AddComponent<GameManager>();
            gm.player = playerInstance.transform;
            gm.initialSpawnPoint = new Vector3(0f, 0.15f, -10f);

            // Ensure EventSystem
            EnsureEventSystem();

            // Save Scene
            Directory.CreateDirectory(Path.GetDirectoryName(GameScenePath));
            EditorSceneManager.SaveScene(scene, GameScenePath);
        }

        // ====================================================================
        // HELPERS
        // ====================================================================
        private static GameObject SpawnAsset(string path, Vector3 pos, Quaternion rot, Transform parent = null, Vector3? scale = null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                if (parent != null) inst.transform.parent = parent;
                inst.transform.position = pos;
                inst.transform.rotation = rot;
                if (scale.HasValue) inst.transform.localScale = scale.Value;
                return inst;
            }
            return null;
        }

        private static void SpawnAnimatedGuardian(Transform parent, Vector3 spawnPos, Vector3[] waypointsList, string guardianName)
        {
            GameObject aiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AICharacterPrefabPath);
            if (aiPrefab == null) return;

            GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(aiPrefab);
            enemy.name = guardianName;
            enemy.transform.parent = parent;
            enemy.transform.position = spawnPos;

            // Remove or disable legacy AICharacterControl to let PatrolEnemy direct movement
            var legacyAI = enemy.GetComponent<UnityStandardAssets.Characters.ThirdPerson.AICharacterControl>();
            if (legacyAI != null) Object.DestroyImmediate(legacyAI);

            var agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            // Add Rigidbody & Collider for stomp physics
            var rb = enemy.GetComponent<Rigidbody>();
            if (rb == null) rb = enemy.AddComponent<Rigidbody>();
            rb.mass = 3f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            var col = enemy.GetComponent<CapsuleCollider>();
            if (col == null)
            {
                col = enemy.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0, 1f, 0);
                col.height = 2f;
                col.radius = 0.5f;
            }

            // Create waypoints
            Transform[] wps = new Transform[waypointsList.Length];
            for (int i = 0; i < waypointsList.Length; i++)
            {
                GameObject wp = new GameObject($"{guardianName}_WP_{i}");
                wp.transform.parent = parent;
                wp.transform.position = waypointsList[i];
                wps[i] = wp.transform;
            }

            var patrol = enemy.AddComponent<PatrolEnemy>();
            patrol.waypoints = wps;
            patrol.patrolSpeed = 2.4f;
            patrol.chaseSpeed = 3.8f;
            patrol.detectionRadius = 10f;
            patrol.attackRadius = 1.4f;
            patrol.defeatEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FireworksPrefabPath);

            // Tint materials to red/guardian armor
            var renderers = enemy.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (r.material != null)
                {
                    r.material.color = new Color(0.85f, 0.25f, 0.25f);
                }
            }
        }

        private static void CreateRelicKey(Transform parent, Vector3 pos, KeyType type, Color color, string keyName)
        {
            GameObject keyRoot = new GameObject(keyName);
            keyRoot.transform.parent = parent;
            keyRoot.transform.position = pos;

            // Key Gem Visual
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "Key_Visual";
            visual.transform.parent = keyRoot.transform;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.5f, 0.8f, 0.5f);
            visual.transform.localRotation = Quaternion.Euler(45, 0, 45);

            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", 0.8f);
            mat.SetFloat("_Smoothness", 0.9f);
            visual.GetComponent<Renderer>().sharedMaterial = mat;

            // Pillar of Light Beam
            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beam.name = "Light_Beam";
            beam.transform.parent = keyRoot.transform;
            beam.transform.localPosition = new Vector3(0, 3f, 0);
            beam.transform.localScale = new Vector3(0.2f, 3f, 0.2f);
            var beamMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            beamMat.SetColor("_BaseColor", new Color(color.r, color.g, color.b, 0.4f));
            beam.GetComponent<Renderer>().sharedMaterial = beamMat;
            Object.DestroyImmediate(beam.GetComponent<Collider>());

            // Trigger & Key Component
            var col = keyRoot.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 1.2f;

            var keyComp = keyRoot.AddComponent<CollectibleKey>();
            keyComp.keyType = type;
            keyComp.keyColor = color;
            keyComp.keyName = keyName;
            keyComp.pickupSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Standard Assets/Characters/FirstPersonCharacter/Audio/Jump.wav");
        }

        private static void BuildShrinePortal(Transform parent, Vector3 pos)
        {
            GameObject shrine = new GameObject("Ancient_Shrine_Portal");
            shrine.transform.parent = parent;
            shrine.transform.position = pos;

            // Flare & Light Beacon
            GameObject flare = SpawnAsset(FlarePrefabPath, pos + Vector3.up * 3.5f, Quaternion.identity, shrine.transform);

            GameObject beacon = new GameObject("PortalBeaconLight");
            beacon.transform.parent = shrine.transform;
            beacon.transform.localPosition = new Vector3(0, 3.5f, 0);
            Light beaconLight = beacon.AddComponent<Light>();
            beaconLight.type = LightType.Point;
            beaconLight.color = new Color(0.4f, 0.85f, 1f);
            beaconLight.range = 15f;
            beaconLight.intensity = 3.5f;

            // Portal Arch Visual
            GameObject arch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arch.name = "Portal_Archway";
            arch.transform.parent = shrine.transform;
            arch.transform.localPosition = new Vector3(0, 3.5f, 0);
            arch.transform.localScale = new Vector3(4f, 6f, 0.8f);
            Material portalMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            portalMat.SetColor("_BaseColor", new Color(0.3f, 0.75f, 1f, 0.4f));
            arch.GetComponent<Renderer>().sharedMaterial = portalMat;
            var boxCol = arch.GetComponent<BoxCollider>();
            boxCol.isTrigger = true;

            var portal = arch.AddComponent<ShrinePortal>();
            portal.portalBeaconLight = beaconLight;
            portal.portalVFX = flare;
            portal.unlockFanfare = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Standard Assets/Characters/FirstPersonCharacter/Audio/Land.wav");
        }

        private static void BuildDiamonds(Transform parent)
        {
            GameObject diamondsRoot = new GameObject("--- 3D PICKUP DIAMONDS (15) ---");
            diamondsRoot.transform.parent = parent;

            Vector3[] diamondPositions = new Vector3[]
            {
                // Along Central Arena & Runway
                new Vector3(0f, 1.0f, -5f),
                new Vector3(5f, 1.0f, 0f),
                new Vector3(-5f, 1.0f, 0f),
                new Vector3(25f, 1.0f, 0f),
                new Vector3(45f, 1.0f, 0f),
                new Vector3(65f, 1.0f, 0f),
                // Village & Pier
                new Vector3(85f, 1.0f, 20f),
                new Vector3(90f, 1.0f, -10f),
                new Vector3(100f, 1.2f, 5f),
                // Exploration Track
                new Vector3(15f, 1.0f, -55f),
                new Vector3(-15f, 1.0f, -55f),
                // Obstacles Area & Ruins
                new Vector3(-35f, 2.0f, 15f),
                new Vector3(-10f, 1.2f, 25f),
                new Vector3(0f, 1.2f, 35f),
                // Staircase to Summit
                new Vector3(0f, 3.2f, 55f)
            };

            for (int i = 0; i < diamondPositions.Length; i++)
            {
                GameObject diamond = SpawnAsset(PickupPrefabPath, diamondPositions[i], Quaternion.identity, diamondsRoot.transform);
                if (diamond != null)
                {
                    diamond.name = $"3D_Crystal_{i + 1}";
                    var gem = diamond.AddComponent<CollectibleGem>();
                    gem.pointValue = 100;
                    gem.rotateSpeed = 75f;
                    gem.bobHeight = 0.2f;
                    gem.pickupSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Standard Assets/Characters/FirstPersonCharacter/Audio/Jump.wav");
                    gem.pickupEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FireworksPrefabPath);
                }
            }
        }

        private static void SetupPostProcessing(Transform parent, string profileName)
        {
            Volume volume = Object.FindAnyObjectByType<Volume>();
            if (volume == null)
            {
                GameObject volObj = new GameObject("Global Volume (URP)");
                volObj.transform.parent = parent;
                volume = volObj.AddComponent<Volume>();
                volume.isGlobal = true;
            }

            if (volume.profile == null)
            {
                VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = profileName;

                // ACES Tonemapping
                Tonemapping tonemap = profile.Add<Tonemapping>(true);
                tonemap.mode.value = TonemappingMode.ACES;

                // Bloom
                Bloom bloom = profile.Add<Bloom>(true);
                bloom.threshold.value = 1.05f;
                bloom.intensity.value = 0.5f;
                bloom.scatter.value = 0.7f;

                // Vignette
                Vignette vignette = profile.Add<Vignette>(true);
                vignette.intensity.value = 0.28f;
                vignette.smoothness.value = 0.35f;

                string path = "Assets/AdventureGame/Materials/" + profileName + ".asset";
                Directory.CreateDirectory("Assets/AdventureGame/Materials");
                AssetDatabase.CreateAsset(profile, path);
                volume.profile = profile;
            }
        }

        private static void BuildMainMenuCanvas()
        {
            GameObject canvasObj = new GameObject("MainMenu_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            var menuController = canvasObj.AddComponent<MainMenuController>();

            // Title Banner
            GameObject titleObj = new GameObject("Title_Text");
            titleObj.transform.SetParent(canvasObj.transform, false);
            Text titleText = titleObj.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 58;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(1f, 0.85f, 0.25f);
            titleText.text = "ETHAN'S 3D ADVENTURE";
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.5f, 0.82f);
            tRt.anchorMax = new Vector2(0.5f, 0.95f);
            tRt.anchoredPosition = Vector2.zero;
            tRt.sizeDelta = new Vector2(900, 100);

            // Subtitle
            GameObject subObj = new GameObject("Subtitle_Text");
            subObj.transform.SetParent(canvasObj.transform, false);
            Text subText = subObj.AddComponent<Text>();
            subText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subText.fontSize = 24;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.color = new Color(0.85f, 0.92f, 1f, 0.9f);
            subText.text = "Standard Assets Modernized for Unity 6 (URP)";
            RectTransform sRt = subObj.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.5f, 0.77f);
            sRt.anchorMax = new Vector2(0.5f, 0.83f);
            sRt.anchoredPosition = Vector2.zero;
            sRt.sizeDelta = new Vector2(800, 50);

            // Main Buttons Container
            GameObject btnContainer = new GameObject("Menu_Buttons");
            btnContainer.transform.SetParent(canvasObj.transform, false);
            RectTransform bRt = btnContainer.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.1f, 0.25f);
            bRt.anchorMax = new Vector2(0.4f, 0.65f);
            bRt.anchoredPosition = Vector2.zero;

            Button playBtn = CreateMenuButton(btnContainer.transform, "PLAY ADVENTURE", new Vector2(0, 100), new Color(0.2f, 0.75f, 0.35f));
            Button tutBtn = CreateMenuButton(btnContainer.transform, "HOW TO PLAY", new Vector2(0, 0), new Color(0.25f, 0.55f, 0.85f));
            Button setBtn = CreateMenuButton(btnContainer.transform, "SETTINGS", new Vector2(0, -100), new Color(0.5f, 0.35f, 0.75f));

            menuController.playButton = playBtn;
            menuController.howToPlayButton = tutBtn;
            menuController.settingsButton = setBtn;

            // Create Panels
            menuController.howToPlayPanel = CreateModalPanel(canvasObj.transform, "HOW TO PLAY", 
                "• WASD / Virtual Joystick: Move Ethan\n" +
                "• SPACE / Jump Button: Jump & Platforming\n" +
                "• SHIFT / Sprint: Fast Running (consumes stamina)\n" +
                "• Press [E] near SkyCar: Drive the vehicle around the circuit!\n" +
                "• Jump on Guardian heads to defeat them!\n" +
                "• Collect all 3 Ancient Relic Keys to open the Summit Shrine Portal!", out Button tutClose);
            menuController.closeHowToPlayBtn = tutClose;

            menuController.settingsPanel = CreateModalPanel(canvasObj.transform, "SETTINGS", 
                "Audio & Performance Options\n\nTarget Frame Rate: 60 FPS (Optimized for Android)\nUniversal Render Pipeline: Active (URP 17.5.0)", out Button setClose);
            menuController.closeSettingsBtn = setClose;

            EnsureEventSystem();
        }

        private static Button CreateMenuButton(Transform parent, string text, Vector2 pos, Color color)
        {
            GameObject btnObj = new GameObject("Button_" + text.Replace(" ", ""));
            btnObj.transform.SetParent(parent, false);
            Image img = btnObj.AddComponent<Image>();
            img.color = color;
            Button btn = btnObj.AddComponent<Button>();

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(320, 65);
            rt.anchoredPosition = pos;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            Text t = textObj.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 26;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.text = text;
            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;

            return btn;
        }

        private static GameObject CreateModalPanel(Transform parent, string header, string content, out Button closeBtn)
        {
            GameObject modal = new GameObject("Modal_" + header.Replace(" ", ""));
            modal.transform.SetParent(parent, false);
            Image bg = modal.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.12f, 0.18f, 0.95f);
            RectTransform rt = modal.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.2f, 0.15f);
            rt.anchorMax = new Vector2(0.8f, 0.85f);
            rt.sizeDelta = Vector2.zero;

            // Header Text
            GameObject hObj = new GameObject("Header");
            hObj.transform.SetParent(modal.transform, false);
            Text hText = hObj.AddComponent<Text>();
            hText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hText.fontSize = 38;
            hText.fontStyle = FontStyle.Bold;
            hText.alignment = TextAnchor.MiddleCenter;
            hText.color = new Color(1f, 0.85f, 0.25f);
            hText.text = header;
            RectTransform hrt = hObj.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0, 0.82f);
            hrt.anchorMax = new Vector2(1, 0.98f);
            hrt.sizeDelta = Vector2.zero;

            // Content Text
            GameObject cObj = new GameObject("Content");
            cObj.transform.SetParent(modal.transform, false);
            Text cText = cObj.AddComponent<Text>();
            cText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cText.fontSize = 24;
            cText.lineSpacing = 1.3f;
            cText.alignment = TextAnchor.UpperLeft;
            cText.color = Color.white;
            cText.text = content;
            RectTransform crt = cObj.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.08f, 0.22f);
            crt.anchorMax = new Vector2(0.92f, 0.8f);
            crt.sizeDelta = Vector2.zero;

            // Close Button
            closeBtn = CreateMenuButton(modal.transform, "CLOSE", new Vector2(0, -180), new Color(0.75f, 0.25f, 0.25f));
            modal.SetActive(false);
            return modal;
        }

        private static void BuildCompleteHUD(GameObject player)
        {
            // Mobile single stick rig
            GameObject mobileControlsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MobileControlsPrefabPath);
            if (mobileControlsPrefab != null)
            {
                GameObject mobileRig = (GameObject)PrefabUtility.InstantiatePrefab(mobileControlsPrefab);
                mobileRig.name = "MobileSingleStickControl";
            }

            // HUD Canvas
            GameObject canvasObj = new GameObject("CompleteGameHUD_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            CompleteGameHUD hud = canvasObj.AddComponent<CompleteGameHUD>();

            // Hearts (Top Left)
            GameObject heartsObj = new GameObject("HealthHearts");
            heartsObj.transform.SetParent(canvasObj.transform, false);
            Text hText = heartsObj.AddComponent<Text>();
            hText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hText.fontSize = 42;
            hText.color = Color.red;
            hText.text = "❤️ ❤️ ❤️";
            RectTransform hRt = heartsObj.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0, 1);
            hRt.anchorMax = new Vector2(0.2f, 1);
            hRt.pivot = new Vector2(0, 1);
            hRt.anchoredPosition = new Vector2(30, -30);
            hud.healthText = hText;

            // Stamina Slider
            GameObject stamObj = new GameObject("StaminaSlider");
            stamObj.transform.SetParent(canvasObj.transform, false);
            Slider sSlider = stamObj.AddComponent<Slider>();
            RectTransform sRt = stamObj.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0, 1);
            sRt.anchorMax = new Vector2(0, 1);
            sRt.pivot = new Vector2(0, 1);
            sRt.anchoredPosition = new Vector2(30, -85);
            sRt.sizeDelta = new Vector2(220, 16);

            GameObject sFill = new GameObject("Fill");
            sFill.transform.SetParent(stamObj.transform, false);
            Image fillImg = sFill.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.8f, 0.4f);
            sSlider.fillRect = sFill.GetComponent<RectTransform>();
            hud.staminaSlider = sSlider;

            // Gems & Score (Top Right)
            GameObject gemObj = new GameObject("GemsCounter");
            gemObj.transform.SetParent(canvasObj.transform, false);
            Text gText = gemObj.AddComponent<Text>();
            gText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            gText.fontSize = 32;
            gText.color = new Color(1f, 0.85f, 0.2f);
            gText.alignment = TextAnchor.UpperRight;
            gText.text = "💎 0 / 15";
            RectTransform gRt = gemObj.GetComponent<RectTransform>();
            gRt.anchorMin = new Vector2(0.8f, 1);
            gRt.anchorMax = new Vector2(1, 1);
            gRt.pivot = new Vector2(1, 1);
            gRt.anchoredPosition = new Vector2(-30, -30);
            hud.gemsText = gText;

            // Relic Keys Inventory UI
            GameObject keysRoot = new GameObject("KeysInventoryUI");
            keysRoot.transform.SetParent(canvasObj.transform, false);
            RectTransform kRt = keysRoot.AddComponent<RectTransform>();
            kRt.anchorMin = new Vector2(0.5f, 1);
            kRt.anchorMax = new Vector2(0.5f, 1);
            kRt.pivot = new Vector2(0.5f, 1);
            kRt.anchoredPosition = new Vector2(0, -30);
            kRt.sizeDelta = new Vector2(240, 50);

            hud.rubyKeyIcon = CreateKeySlot(keysRoot.transform, -70, Color.red, "R");
            hud.sapphireKeyIcon = CreateKeySlot(keysRoot.transform, 0, Color.cyan, "S");
            hud.emeraldKeyIcon = CreateKeySlot(keysRoot.transform, 70, Color.green, "E");

            // Notification Banner
            GameObject notifObj = new GameObject("NotificationBanner");
            notifObj.transform.SetParent(canvasObj.transform, false);
            Text nText = notifObj.AddComponent<Text>();
            nText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nText.fontSize = 28;
            nText.fontStyle = FontStyle.Bold;
            nText.alignment = TextAnchor.MiddleCenter;
            nText.color = Color.yellow;
            nText.text = "";
            RectTransform nRt = notifObj.GetComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0.2f, 0.75f);
            nRt.anchorMax = new Vector2(0.8f, 0.85f);
            nRt.sizeDelta = Vector2.zero;
            hud.notificationText = nText;

            // Mobile Sprint & Jump buttons
            hud.sprintButton = CreateActionButton(canvasObj.transform, "SPRINT", new Vector2(-160, 90), new Color(0.2f, 0.6f, 0.9f));

            // Modals
            hud.pausePanel = CreateModalPanel(canvasObj.transform, "GAME PAUSED", "Adventure Paused.\n\nTake a breath and continue when ready!", out Button pResume);
            hud.resumeButton = pResume;

            hud.gameOverPanel = CreateModalPanel(canvasObj.transform, "QUEST FAILED", "You ran out of health!\n\nRespawn and try again!", out Button gRetry);
            hud.gameOverRetryButton = gRetry;

            hud.victoryPanel = CreateModalPanel(canvasObj.transform, "QUEST COMPLETE!", "Congratulations!\nYou unlocked the Ancient Portal and saved the Island!", out Button vPlayAgain);
            hud.victoryPlayAgainButton = vPlayAgain;
        }

        private static Image CreateKeySlot(Transform parent, float posX, Color col, string label)
        {
            GameObject slot = new GameObject("Slot_" + label);
            slot.transform.SetParent(parent, false);
            Image img = slot.AddComponent<Image>();
            img.color = new Color(col.r, col.g, col.b, 0.25f);
            RectTransform rt = slot.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(48, 48);
            rt.anchoredPosition = new Vector2(posX, 0);

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(slot.transform, false);
            Text t = textObj.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 22;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.text = label;
            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;

            return img;
        }

        private static Button CreateActionButton(Transform parent, string label, Vector2 anchoredPos, Color color)
        {
            GameObject btnObj = new GameObject("Btn_" + label);
            btnObj.transform.SetParent(parent, false);
            Image img = btnObj.AddComponent<Image>();
            img.color = color;
            Button btn = btnObj.AddComponent<Button>();

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(110, 110);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            Text t = textObj.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 20;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.text = label;
            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;

            return btn;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }
    }

    public class MenuCharacterRotator : MonoBehaviour
    {
        public float turnSpeed = 20f;
        private void Update()
        {
            transform.Rotate(Vector3.up, turnSpeed * Time.deltaTime, Space.World);
        }
    }
}
