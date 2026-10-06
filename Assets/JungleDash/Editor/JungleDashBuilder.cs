// JungleDashBuilder.cs  –  Unity 6 compatible, zero CS errors
// Menu: "Jungle Dash/1 - Build Prefabs And Scene"
//       "Jungle Dash/2 - Apply Android Settings"
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public static class JungleDashBuilder
{
    // ── Asset paths ───────────────────────────────────────────────────────────
    private const string PrefabDir   = "Assets/JungleDash/Prefabs";
    private const string MaterialDir = "Assets/JungleDash/Materials";
    private const string SceneDir    = "Assets/JungleDash/Scenes";
    private const string ScenePath   = SceneDir + "/JungleDash.unity";

    private const string EthanFBX       = "Assets/Standard Assets/Characters/ThirdPersonCharacter/Models/Ethan.fbx";
    private const string EthanMat       = "Assets/Standard Assets/Characters/ThirdPersonCharacter/Materials/EthanWhite.mat";
    private const string AnimController = "Assets/Standard Assets/Characters/ThirdPersonCharacter/Animator/ThirdPersonAnimatorController.controller";
    private const string MudTex         = "Assets/Standard Assets/Environment/TerrainAssets/SurfaceTextures/MudRockyAlbedoSpecular.bmp";
    private const string MudNorm        = "Assets/Standard Assets/Environment/TerrainAssets/SurfaceTextures/MudRockyNormals.bmp";
    private const string GrassTex       = "Assets/Standard Assets/Environment/TerrainAssets/SurfaceTextures/GrassHillAlbedo.psd";
    private const string CliffTex       = "Assets/Standard Assets/Environment/TerrainAssets/SurfaceTextures/CliffAlbedoSpecular.psd";

    // ─────────────────────────────────────────────────────────────────────────
    //  1. Build Prefabs And Scene
    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("Jungle Dash/1 - Build Prefabs And Scene")]
    public static void BuildAll()
    {
        if (!Directory.Exists("Assets/TextMesh Pro"))
        {
            Debug.LogError("[JungleDash] Import TMP Essential Resources first: " +
                           "Window > TextMeshPro > Import TMP Essential Resources");
            return;
        }
        if (Shader.Find("Standard") == null)
        {
            Debug.LogError("[JungleDash] Standard shader not found. Use Built-in Render Pipeline.");
            return;
        }

        EnsureDirectories();

        // Materials
        Material matMud      = MatStd("Mat_Trail",    "#8B6914", MudTex, MudNorm);
        Material matGrass    = MatStd("Mat_Grass",    "#4A7A2A", GrassTex, null);
        Material matCliff    = MatStd("Mat_Rock",     "#5A5550", CliffTex, null);
        Material matBark     = MatStd("Mat_Bark",     "#5C3D1A", null, null);
        Material matCut      = MatStd("Mat_BarkCut",  "#C0A060", null, null);
        Material matGold     = MatEmissive("Mat_Gold",     "#FFD700", "#FFD700", 0.4f);
        Material matGem      = MatEmissive("Mat_Gem",      "#00E8FF", "#00E8FF", 0.8f);
        Material matMagnetPU = MatEmissive("Mat_PU_Magnet","#FF2222", "#FF4444", 0.6f);
        Material matShieldPU = MatEmissive("Mat_PU_Shield","#2255FF", "#4477FF", 0.6f);
        Material matSpeedPU  = MatEmissive("Mat_PU_Speed", "#FF8800", "#FFAA00", 0.6f);
        Material matFlyPU    = MatEmissive("Mat_PU_Fly",   "#FFFFFF", "#CCFFFF", 0.6f);
        Material matDoublePU = MatEmissive("Mat_PU_Double","#AA00FF", "#CC44FF", 0.6f);
        Material matHalo         = MatTransparent("Mat_Halo",        "#FFFFFF", 0.12f);
        Material matShieldSphere = MatTransparent("Mat_ShieldSphere","#4488FF", 0.30f);
        Material matBoulder  = MatStd("Mat_Boulder",  "#606060", CliffTex, null);
        Material matStump    = MatStd("Mat_Stump",    "#3D2200", null, null);
        Material matMoss     = MatStd("Mat_Moss",     "#3A5A1A", GrassTex, null);

        // Prefabs
        GameObject coinPrefab    = PrefabCoin(matGold);
        GameObject gemPrefab     = PrefabGem(matGem);
        GameObject logPrefab     = PrefabLog(matBark, matCut);
        GameObject boulderPrefab = PrefabBoulder(matBoulder, matMoss);
        GameObject stumpPrefab   = PrefabStump(matStump, matMoss);
        GameObject[] puPrefabs   = PrefabPowerUps(matMagnetPU, matShieldPU, matSpeedPU,
                                                   matFlyPU, matDoublePU, matHalo);
        GameObject segPrefab     = PrefabTrackSegment(matMud, matGrass, matCliff);

        // New scene — store the returned Scene so SaveScene doesn't accidentally
        // pick the internal DontDestroyOnLoad scene.
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Lighting
        BuildLighting();

        // Player
        GameObject player = BuildPlayer(matShieldSphere);

        // Camera
        GameObject cam = BuildCamera(player.transform);
        GameObject speedLinesGO = BuildSpeedLines(cam.transform);

        // PowerUpManager
        GameObject puMgrGO = new GameObject("PowerUpManager");
        PowerUpManager puMgr = puMgrGO.AddComponent<PowerUpManager>();
        puMgr.shieldSphereRenderer = player.transform.Find("ShieldSphere")?.GetComponent<Renderer>();
        puMgr.flyGlowParticles     = player.transform.Find("FlyGlow")?.GetComponent<ParticleSystem>();
        puMgr.flyTrailRenderer     = player.transform.Find("FlyTrail")?.GetComponent<TrailRenderer>();
        puMgr.speedLinesParticles  = speedLinesGO?.GetComponent<ParticleSystem>();

        // TrackSpawner
        GameObject spawnerGO = new GameObject("TrackSpawner");
        TrackSpawner spawner = spawnerGO.AddComponent<TrackSpawner>();
        spawner.segmentPrefab      = segPrefab.GetComponent<TrackSegment>();
        spawner.coinPrefab         = coinPrefab;
        spawner.gemPrefab          = gemPrefab;
        spawner.obstaclePrefabs    = new GameObject[] { logPrefab, boulderPrefab, stumpPrefab };
        spawner.powerUpPrefabs     = puPrefabs;
        spawner.laneWidth          = 2f;
        spawner.rowSpacing         = 7.5f;
        spawner.segmentCount       = 6;
        spawner.emptyStartSegments = 2;

        // GameManager
        GameObject gmGO = new GameObject("GameManager");
        GameManager gm  = gmGO.AddComponent<GameManager>();
        gm.PlayerTransform = player.transform;

        // Canvas / HUD
        BuildCanvas(gm, puMgr);

        // Save — use the scene handle returned by NewScene, not GetActiveScene()
        Directory.CreateDirectory(SceneDir);
        EditorSceneManager.SaveScene(newScene, ScenePath);
        AddToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[JungleDash] Build complete → " + ScenePath);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  2. Apply Android Settings
    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("Jungle Dash/2 - Apply Android Settings")]
    public static void ApplyAndroidSettings()
    {
        NamedBuildTarget android = NamedBuildTarget.Android;
        PlayerSettings.SetApplicationIdentifier(android, "com.yourname.jungledash");
        PlayerSettings.productName   = "Jungle Dash";
        PlayerSettings.companyName   = "YourStudio";
        PlayerSettings.Android.minSdkVersion      = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion   = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures         = AndroidArchitecture.ARM64;
        PlayerSettings.defaultInterfaceOrientation         = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToLandscapeLeft    = false;
        PlayerSettings.allowedAutorotateToLandscapeRight   = false;
        PlayerSettings.allowedAutorotateToPortrait         = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        Debug.Log("[JungleDash] Android settings applied.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  MATERIAL FACTORIES
    // ═════════════════════════════════════════════════════════════════════════

    static Material MatStd(string name, string hex, string texPath, string normPath)
    {
        string assetPath = MaterialDir + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard")) { name = name };
            AssetDatabase.CreateAsset(mat, assetPath);
        }
        ColorUtility.TryParseHtmlString(hex, out Color c);
        mat.color            = c;
        mat.enableInstancing = true;
        Texture2D tex  = texPath  != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(texPath)  : null;
        Texture2D norm = normPath != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(normPath) : null;
        if (tex  != null) mat.mainTexture = tex;
        if (norm != null) { mat.SetTexture("_BumpMap", norm); mat.EnableKeyword("_NORMALMAP"); }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material MatEmissive(string name, string hex, string hexEmit, float intensity)
    {
        string assetPath = MaterialDir + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard")) { name = name };
            AssetDatabase.CreateAsset(mat, assetPath);
        }
        ColorUtility.TryParseHtmlString(hex,     out Color c);
        ColorUtility.TryParseHtmlString(hexEmit, out Color e);
        mat.color                   = c;
        mat.SetColor("_EmissionColor", e * intensity);
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        mat.SetFloat("_Metallic",    0.7f);
        mat.SetFloat("_Glossiness",  0.8f);
        mat.enableInstancing        = true;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material MatTransparent(string name, string hex, float alpha)
    {
        string assetPath = MaterialDir + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard")) { name = name };
            AssetDatabase.CreateAsset(mat, assetPath);
        }
        ColorUtility.TryParseHtmlString(hex, out Color c);
        c.a = alpha;
        mat.SetFloat("_Mode",    2);   // Fade
        mat.SetInt("_SrcBlend",  (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend",  (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite",    0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue      = 3000;
        mat.color            = c;
        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  PREFAB BUILDERS
    // ═════════════════════════════════════════════════════════════════════════

    static GameObject PrefabCoin(Material mat)
    {
        GameObject go = new GameObject("Coin");
        GameObject disk = Prim(PrimitiveType.Cylinder, go, Vector3.zero,
                               new Vector3(0.5f, 0.06f, 0.5f), mat);
        Object.DestroyImmediate(disk.GetComponent<CapsuleCollider>());
        SphereCollider sc = go.AddComponent<SphereCollider>();
        sc.radius    = 0.35f;
        sc.isTrigger = true;
        Collectible col = go.AddComponent<Collectible>();
        col.points    = 10;
        col.coinValue = 1;
        return Save(go, PrefabDir + "/Coin.prefab");
    }

    static GameObject PrefabGem(Material mat)
    {
        GameObject go   = new GameObject("Gem");
        GameObject body = Prim(PrimitiveType.Sphere, go, Vector3.zero,
                               new Vector3(0.55f, 0.75f, 0.55f), mat);
        Object.DestroyImmediate(body.GetComponent<SphereCollider>());
        SphereCollider sc = go.AddComponent<SphereCollider>();
        sc.radius    = 0.45f;
        sc.isTrigger = true;
        Collectible col = go.AddComponent<Collectible>();
        col.points    = 50;
        col.coinValue = 5;
        return Save(go, PrefabDir + "/Gem.prefab");
    }

    static GameObject PrefabLog(Material bark, Material cut)
    {
        GameObject go   = new GameObject("ObstacleLog");
        GameObject body = Prim(PrimitiveType.Cylinder, go,
                               new Vector3(0f, 0.45f, 0f), new Vector3(0.5f, 0.85f, 0.5f), bark);
        body.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());
        for (int s = -1; s <= 1; s += 2)
        {
            GameObject cap = Prim(PrimitiveType.Cylinder, go,
                                  new Vector3(s * 0.85f, 0.45f, 0f),
                                  new Vector3(0.5f, 0.05f, 0.5f), cut);
            cap.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Object.DestroyImmediate(cap.GetComponent<CapsuleCollider>());
        }
        BoxCollider bc = go.AddComponent<BoxCollider>();
        bc.center    = new Vector3(0f, 0.45f, 0f);
        bc.size      = new Vector3(1.7f, 0.9f, 0.9f);
        bc.isTrigger = true;
        AddDebris(go);
        Obstacle obs = go.AddComponent<Obstacle>();
        obs.obstacleRenderer = body.GetComponent<MeshRenderer>();
        obs.debrisParticles  = go.transform.Find("Debris")?.GetComponent<ParticleSystem>();
        return Save(go, PrefabDir + "/ObstacleLog.prefab");
    }

    static GameObject PrefabBoulder(Material rock, Material moss)
    {
        GameObject go   = new GameObject("ObstacleBoulder");
        GameObject body = Prim(PrimitiveType.Sphere, go,
                               new Vector3(0f, 0.45f, 0f), new Vector3(1.1f, 0.9f, 1.0f), rock);
        Object.DestroyImmediate(body.GetComponent<SphereCollider>());
        GameObject top = Prim(PrimitiveType.Sphere, go,
                               new Vector3(0f, 0.8f, 0f), new Vector3(0.85f, 0.4f, 0.85f), moss);
        Object.DestroyImmediate(top.GetComponent<SphereCollider>());
        BoxCollider bc = go.AddComponent<BoxCollider>();
        bc.center    = new Vector3(0f, 0.45f, 0f);
        bc.size      = new Vector3(1.1f, 0.9f, 1.0f);
        bc.isTrigger = true;
        AddDebris(go);
        Obstacle obs = go.AddComponent<Obstacle>();
        obs.obstacleRenderer = body.GetComponent<MeshRenderer>();
        obs.debrisParticles  = go.transform.Find("Debris")?.GetComponent<ParticleSystem>();
        return Save(go, PrefabDir + "/ObstacleBoulder.prefab");
    }

    static GameObject PrefabStump(Material wood, Material moss)
    {
        GameObject go    = new GameObject("ObstacleStump");
        GameObject trunk = Prim(PrimitiveType.Cylinder, go,
                                new Vector3(0f, 0.5f, 0f), new Vector3(0.7f, 0.5f, 0.7f), wood);
        Object.DestroyImmediate(trunk.GetComponent<CapsuleCollider>());
        GameObject top = Prim(PrimitiveType.Cylinder, go,
                              new Vector3(0f, 1.06f, 0f), new Vector3(0.72f, 0.06f, 0.72f), moss);
        Object.DestroyImmediate(top.GetComponent<CapsuleCollider>());
        for (int r = 0; r < 2; r++)
        {
            GameObject root = Prim(PrimitiveType.Cube, go,
                                   new Vector3(r == 0 ? -0.35f : 0.35f, 0.12f, 0.1f),
                                   new Vector3(0.15f, 0.25f, 0.55f), wood);
            root.transform.localRotation = Quaternion.Euler(0f, r == 0 ? -25f : 25f, 0f);
            Object.DestroyImmediate(root.GetComponent<BoxCollider>());
        }
        BoxCollider bc = go.AddComponent<BoxCollider>();
        bc.center    = new Vector3(0f, 0.5f, 0f);
        bc.size      = new Vector3(0.75f, 1.0f, 0.75f);
        bc.isTrigger = true;
        AddDebris(go);
        Obstacle obs = go.AddComponent<Obstacle>();
        obs.obstacleRenderer = trunk.GetComponent<MeshRenderer>();
        obs.debrisParticles  = go.transform.Find("Debris")?.GetComponent<ParticleSystem>();
        return Save(go, PrefabDir + "/ObstacleStump.prefab");
    }

    static void AddDebris(GameObject parent)
    {
        GameObject d = new GameObject("Debris");
        d.transform.SetParent(parent.transform);
        d.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        ParticleSystem ps  = d.AddComponent<ParticleSystem>();
        var main           = ps.main;
        main.playOnAwake   = false;
        main.loop          = false;
        main.duration      = 0.5f;
        main.startLifetime = 0.7f;
        main.startSpeed    = 5f;
        main.startSize     = 0.15f;
        main.maxParticles  = 30;
        var em = ps.emission;
        em.enabled = true;
        em.SetBurst(0, new ParticleSystem.Burst(0f, 20));
        var sh       = ps.shape;
        sh.enabled   = true;
        sh.shapeType = ParticleSystemShapeType.Sphere;
        sh.radius    = 0.3f;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    // ── Power-up prefabs ─────────────────────────────────────────────────────
    static GameObject[] PrefabPowerUps(Material mag, Material shd, Material spd,
                                       Material fly, Material dbl, Material halo)
    {
        Material[] mats  = { mag, shd, spd, fly, dbl };
        string[]   names = { "PU_Magnet","PU_Shield","PU_Speed","PU_Fly","PU_Double" };
        PowerUpType[] types = { PowerUpType.Magnet, PowerUpType.Shield,
                                PowerUpType.SpeedBoost, PowerUpType.Fly, PowerUpType.DoubleScore };
        GameObject[] result = new GameObject[5];

        for (int i = 0; i < 5; i++)
        {
            GameObject go     = new GameObject(names[i]);
            GameObject visual = PUVisual(i, mats[i]);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform);
            visual.transform.localPosition = Vector3.zero;

            // Translucent halo sphere (Renderer, no collider)
            GameObject haloGO = Prim(PrimitiveType.Sphere, go, Vector3.zero,
                                     new Vector3(1.6f, 1.6f, 1.6f), halo);
            MeshRenderer haloRend        = haloGO.GetComponent<MeshRenderer>();
            haloRend.shadowCastingMode   = UnityEngine.Rendering.ShadowCastingMode.Off;
            haloRend.receiveShadows      = false;
            Object.DestroyImmediate(haloGO.GetComponent<SphereCollider>());

            // Trigger
            SphereCollider sc = go.AddComponent<SphereCollider>();
            sc.radius    = 0.7f;
            sc.isTrigger = true;

            // "x2" label for DoubleScore only
            TextMeshPro lbl = null;
            if (i == 4)
            {
                GameObject lblGO = new GameObject("Label");
                lblGO.transform.SetParent(go.transform);
                lblGO.transform.localPosition = new Vector3(0f, 1.1f, 0f);
                lbl           = lblGO.AddComponent<TextMeshPro>();
                lbl.text      = "x2";
                lbl.fontSize  = 5f;
                lbl.alignment = TextAlignmentOptions.Center;
                lbl.color     = Color.white;
            }

            PowerUpPickup pup = go.AddComponent<PowerUpPickup>();
            pup.powerUpType  = types[i];
            pup.visualRoot   = visual.transform;
            pup.haloRenderer = haloRend;
            pup.label        = lbl;

            result[i] = Save(go, PrefabDir + "/" + names[i] + ".prefab");
        }
        return result;
    }

    static GameObject PUVisual(int idx, Material mat)
    {
        GameObject vis;
        switch (idx)
        {
            case 0: // Magnet – U shape
                vis = new GameObject("MagVis");
                CubePart(vis, mat, new Vector3(-0.25f, 0f, 0f),    new Vector3(0.15f, 0.6f,  0.15f));
                CubePart(vis, mat, new Vector3( 0.25f, 0f, 0f),    new Vector3(0.15f, 0.6f,  0.15f));
                CubePart(vis, mat, new Vector3(0f, -0.3f, 0f),     new Vector3(0.65f, 0.15f, 0.15f));
                Material tipMat = MatEmissive("Mat_MagTips","#FFFFFF","#FFFFFF",0.5f);
                CubePart(vis, tipMat, new Vector3(-0.25f, 0.32f, 0f), new Vector3(0.15f, 0.08f, 0.18f));
                CubePart(vis, tipMat, new Vector3( 0.25f, 0.32f, 0f), new Vector3(0.15f, 0.08f, 0.18f));
                break;
            case 1: // Shield – flat plate
                vis = new GameObject("ShdVis");
                CubePart(vis, mat, Vector3.zero, new Vector3(0.7f, 0.85f, 0.12f));
                Material bossMat = MatEmissive("Mat_ShdBoss","#FFFFFF","#FFFFFF",0.4f);
                CubePart(vis, bossMat, new Vector3(0f, 0f, -0.08f), new Vector3(0.2f, 0.2f, 0.12f));
                break;
            case 2: // Speed – chevron
                vis = new GameObject("SpdVis");
                CubePart(vis, mat, new Vector3(0f,     0.3f, 0f), new Vector3(0.25f, 0.25f, 0.25f));
                CubePart(vis, mat, new Vector3(-0.25f, 0f,   0f), new Vector3(0.25f, 0.25f, 0.25f));
                CubePart(vis, mat, new Vector3( 0.25f, 0f,   0f), new Vector3(0.25f, 0.25f, 0.25f));
                break;
            case 3: // Fly – wings
                vis = new GameObject("FlyVis");
                CubePart(vis, mat, new Vector3(-0.45f, 0f, 0f), new Vector3(0.55f, 0.1f, 0.35f));
                CubePart(vis, mat, new Vector3( 0.45f, 0f, 0f), new Vector3(0.55f, 0.1f, 0.35f));
                break;
            default: // DoubleScore – cube
                vis = GameObject.CreatePrimitive(PrimitiveType.Cube);
                vis.name = "DblVis";
                vis.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
                vis.GetComponent<MeshRenderer>().sharedMaterial = mat;
                Object.DestroyImmediate(vis.GetComponent<BoxCollider>());
                break;
        }
        return vis;
    }

    static void CubePart(GameObject parent, Material mat, Vector3 pos, Vector3 scale)
    {
        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.transform.SetParent(parent.transform);
        c.transform.localPosition = pos;
        c.transform.localScale    = scale;
        c.GetComponent<MeshRenderer>().sharedMaterial = mat;
        Object.DestroyImmediate(c.GetComponent<BoxCollider>());
    }

    // ── Track segment ─────────────────────────────────────────────────────────
    static GameObject PrefabTrackSegment(Material mud, Material grass, Material rock)
    {
        GameObject root   = new GameObject("TrackSegment");

        // Ground cube – scale set by ApplyGroundScale(), BoxCollider kept for walking
        GameObject ground = Prim(PrimitiveType.Cube, root, Vector3.zero, Vector3.one, mud);
        ground.name = "Ground";

        // Grass banks
        for (int s = -1; s <= 1; s += 2)
        {
            GameObject bank = Prim(PrimitiveType.Cube, root,
                                   new Vector3(s * 5f, -0.35f, 15f),
                                   new Vector3(4f, 0.4f, 30f), grass);
            bank.name = "Bank";
            Object.DestroyImmediate(bank.GetComponent<BoxCollider>());
        }

        // Rock accents along edges
        for (int s = -1; s <= 1; s += 2)
        {
            for (int r = 0; r < 3; r++)
            {
                GameObject rk = Prim(PrimitiveType.Cube, root,
                                     new Vector3(s * 3.2f, 0.1f, 5f + r * 9f),
                                     new Vector3(0.4f, 0.35f, 0.55f), rock);
                rk.name = "Rock";
                rk.transform.localRotation = Quaternion.Euler(0f, r * 15f, 0f);
                Object.DestroyImmediate(rk.GetComponent<BoxCollider>());
            }
        }

        // Bushes
        for (int s = -1; s <= 1; s += 2)
        {
            for (int b = 0; b < 4; b++)
            {
                GameObject bush = Prim(PrimitiveType.Sphere, root,
                                       new Vector3(s * 3.8f, 0.15f, 3f + b * 7f),
                                       new Vector3(0.7f, 0.35f, 0.7f), grass);
                bush.name = "Bush";
                Object.DestroyImmediate(bush.GetComponent<SphereCollider>());
            }
        }

        // Primitive trees (trunk + crown)
        Material trunkMat = rock;
        Material crownMat = grass;
        for (int s = -1; s <= 1; s += 2)
        {
            for (int t = 0; t < 4; t++)
            {
                float z = 4f + t * 7f;
                GameObject tree   = new GameObject("Tree");
                tree.transform.SetParent(root.transform);
                tree.transform.localPosition = new Vector3(s * 6.5f, 0f, z);

                GameObject trunk = Prim(PrimitiveType.Cylinder, tree,
                                        new Vector3(0f, 1.5f, 0f),
                                        new Vector3(0.2f, 1.5f, 0.2f), trunkMat);
                Object.DestroyImmediate(trunk.GetComponent<CapsuleCollider>());

                GameObject crown = Prim(PrimitiveType.Sphere, tree,
                                        new Vector3(0f, 3.5f, 0f),
                                        new Vector3(1.5f, 1.8f, 1.5f), crownMat);
                Object.DestroyImmediate(crown.GetComponent<SphereCollider>());
            }
        }

        TrackSegment seg  = root.AddComponent<TrackSegment>();
        seg.segmentLength  = 30f;
        seg.groundWidth    = 6f;
        seg.groundThickness = 0.5f;
        seg.groundChild    = ground.transform;
        seg.ApplyGroundScale();

        return Save(root, PrefabDir + "/TrackSegment.prefab");
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  SCENE OBJECTS
    // ═════════════════════════════════════════════════════════════════════════

    static void BuildLighting()
    {
        GameObject sunGO = new GameObject("Sun");
        Light sun        = sunGO.AddComponent<Light>();
        sun.type         = LightType.Directional;
        sun.color        = new Color(1f, 0.92f, 0.7f);
        sun.intensity    = 1.2f;
        sun.shadows      = LightShadows.Hard;
        sunGO.transform.rotation = Quaternion.Euler(52f, -30f, 0f);
        // Shadow distance lives on QualitySettings in Unity 6, not on Light
        QualitySettings.shadowDistance = 25f;

        // Procedural skybox
        Material sky = new Material(Shader.Find("Skybox/Procedural"));
        if (sky != null)
        {
            sky.name = "ProcSkybox";
            AssetDatabase.CreateAsset(sky, MaterialDir + "/ProcSkybox.mat");
            sky.SetFloat("_AtmosphereThickness", 1f);
            RenderSettings.skybox = sky;
        }

        RenderSettings.fog              = true;
        RenderSettings.fogMode          = FogMode.Linear;
        RenderSettings.fogStartDistance = 35f;
        RenderSettings.fogEndDistance   = 120f;
        RenderSettings.fogColor         = new Color(0.62f, 0.72f, 0.58f);

        RenderSettings.ambientMode        = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor    = new Color(0.5f,  0.65f, 0.45f);
        RenderSettings.ambientEquatorColor = new Color(0.45f, 0.5f, 0.38f);
        RenderSettings.ambientGroundColor  = new Color(0.25f, 0.22f, 0.15f);
    }

    static GameObject BuildPlayer(Material shieldMat)
    {
        GameObject player = new GameObject("Player");
        player.tag = "Player";

        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height    = 1.8f;
        cc.radius    = 0.3f;
        cc.center    = new Vector3(0f, 0.9f, 0f);
        cc.slopeLimit = 45f;
        cc.stepOffset = 0.3f;

        Rigidbody rb  = player.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        PlayerRunner runner = player.AddComponent<PlayerRunner>();

        // Ethan or capsule fallback
        RuntimeAnimatorController animCtrl =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimController);
        GameObject charChild = BuildCharacter(player.transform, animCtrl);
        if (charChild != null)
            runner.characterAnimator = charChild.GetComponentInChildren<Animator>();

        // Shield sphere (Renderer, not Material)
        GameObject sphGO = Prim(PrimitiveType.Sphere, player,
                                new Vector3(0f, 0.9f, 0f), new Vector3(1.3f, 1.3f, 1.3f), shieldMat);
        sphGO.name = "ShieldSphere";
        MeshRenderer sphRend     = sphGO.GetComponent<MeshRenderer>();
        sphRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        sphRend.receiveShadows    = false;
        Object.DestroyImmediate(sphGO.GetComponent<SphereCollider>());
        sphGO.SetActive(false);

        // Fly glow particles
        GameObject glowGO = new GameObject("FlyGlow");
        glowGO.transform.SetParent(player.transform);
        glowGO.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        ParticleSystem glow = glowGO.AddComponent<ParticleSystem>();
        var gm = glow.main;
        gm.loop          = true;
        gm.playOnAwake   = false;
        gm.startLifetime = 0.6f;
        gm.startSpeed    = 1.5f;
        gm.startSize     = 0.15f;
        gm.startColor    = new Color(0.5f, 1f, 1f, 0.8f);
        gm.maxParticles  = 40;
        var ge = glow.emission; ge.rateOverTime = 20f;
        var gs = glow.shape;    gs.shapeType = ParticleSystemShapeType.Sphere; gs.radius = 0.4f;
        glow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Fly trail
        GameObject trailGO = new GameObject("FlyTrail");
        trailGO.transform.SetParent(player.transform);
        trailGO.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        TrailRenderer tr = trailGO.AddComponent<TrailRenderer>();
        tr.startWidth = 0.3f;
        tr.endWidth   = 0f;
        tr.time       = 0.4f;
        tr.enabled    = false;
        tr.material   = new Material(Shader.Find("Sprites/Default"));

        return player;
    }

    static GameObject BuildCharacter(Transform parent, RuntimeAnimatorController animCtrl)
    {
        GameObject ethanFBX = AssetDatabase.LoadAssetAtPath<GameObject>(EthanFBX);
        if (ethanFBX == null)
        {
            Debug.LogWarning("[JungleDash] Ethan.fbx not found – capsule fallback.");
            GameObject cap = Prim(PrimitiveType.Capsule, null,
                                  Vector3.zero, new Vector3(0.5f, 0.9f, 0.5f), null);
            cap.name = "RunnerCapsule";
            cap.transform.SetParent(parent);
            cap.transform.localPosition = Vector3.zero;
            Object.DestroyImmediate(cap.GetComponent<CapsuleCollider>());
            return cap;
        }

        GameObject ethan = (GameObject)PrefabUtility.InstantiatePrefab(ethanFBX, parent);
        ethan.name = "EthanModel";
        ethan.transform.localPosition = Vector3.zero;
        ethan.transform.localRotation = Quaternion.identity;
        ethan.transform.localScale    = Vector3.one;

        Material baseMat = AssetDatabase.LoadAssetAtPath<Material>(EthanMat);
        if (baseMat != null)
        {
            Material explorerMat  = new Material(baseMat) { name = "Mat_Explorer" };
            explorerMat.color     = new Color(0.72f, 0.65f, 0.35f);
            explorerMat.enableInstancing = true;
            string ePath = MaterialDir + "/Mat_Explorer.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(ePath) == null)
                AssetDatabase.CreateAsset(explorerMat, ePath);
            foreach (var r in ethan.GetComponentsInChildren<SkinnedMeshRenderer>())
                r.sharedMaterial = explorerMat;
        }

        // Safari hat primitives
        Material hatMat = MatStd("Mat_Hat", "#8B6914", null, null);
        GameObject brim = Prim(PrimitiveType.Cube, ethan,
                               new Vector3(0f, 1.7f, 0f), new Vector3(0.35f, 0.04f, 0.35f), hatMat);
        brim.name = "HatBrim";
        Object.DestroyImmediate(brim.GetComponent<BoxCollider>());
        GameObject crown = Prim(PrimitiveType.Cube, ethan,
                                new Vector3(0f, 1.8f, 0f), new Vector3(0.22f, 0.15f, 0.22f), hatMat);
        crown.name = "HatCrown";
        Object.DestroyImmediate(crown.GetComponent<BoxCollider>());

        Animator anim = ethan.GetComponent<Animator>() ?? ethan.AddComponent<Animator>();
        if (animCtrl != null) anim.runtimeAnimatorController = animCtrl;
        anim.applyRootMotion = false;

        return ethan;
    }

    static GameObject BuildCamera(Transform playerTf)
    {
        GameObject camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        Camera cam = camGO.AddComponent<Camera>();
        cam.farClipPlane = 130f;
        cam.fieldOfView  = 65f;
        cam.clearFlags   = CameraClearFlags.Skybox;
        camGO.AddComponent<AudioListener>();
        CameraFollow cf = camGO.AddComponent<CameraFollow>();
        cf.target   = playerTf;
        cf.baseFOV  = 65f;
        cf.farClip  = 130f;
        cf.offset   = new Vector3(0f, 2.8f, -5.5f);
        camGO.transform.position = playerTf.position + new Vector3(0f, 2.8f, -5.5f);
        return camGO;
    }

    static GameObject BuildSpeedLines(Transform camTf)
    {
        GameObject go = new GameObject("SpeedLines");
        go.transform.SetParent(camTf);
        go.transform.localPosition = new Vector3(0f, 0f, 2f);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var m = ps.main;
        m.loop          = true;
        m.playOnAwake   = false;
        m.startLifetime = 0.15f;
        m.startSpeed    = 15f;
        m.startSize     = new ParticleSystem.MinMaxCurve(0.01f, 0.04f);
        m.startColor    = new Color(1f, 1f, 1f, 0.3f);
        m.maxParticles  = 80;
        var e = ps.emission; e.rateOverTime = 0f;
        var s = ps.shape;    s.shapeType = ParticleSystemShapeType.Cone; s.angle = 25f;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return go;
    }

    static void BuildCanvas(GameManager gm, PowerUpManager puMgr)
    {
        // EventSystem
        GameObject esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        // Canvas
        GameObject canvasGO = new GameObject("Canvas");
        Canvas cv = canvasGO.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<GraphicRaycaster>();
        CanvasScaler cs = canvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1080f, 1920f);
        cs.matchWidthOrHeight  = 0.5f;

        // HUD bar (top)
        GameObject hudBar = UIPanel(canvasGO.transform, "HUDBar",
            new Vector2(0f,1f), new Vector2(1f,1f), Vector2.zero, new Vector2(0f, 80f));
        hudBar.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        TextMeshProUGUI scoreTMP = UILabel(hudBar.transform, "ScoreText",
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            Vector2.zero, new Vector2(400f,60f), "0", 36f);
        TextMeshProUGUI coinTMP = UILabel(hudBar.transform, "CoinText",
            new Vector2(1f,0.5f), new Vector2(1f,0.5f),
            new Vector2(-100f,0f), new Vector2(180f,60f), "0", 30f);

        gm.scoreTMP = scoreTMP;
        gm.coinTMP  = coinTMP;

        // PowerUp HUD (bottom)
        GameObject puBar = UIPanel(canvasGO.transform, "PowerUpHUDBar",
            new Vector2(0f,0f), new Vector2(1f,0f), new Vector2(0f,55f), new Vector2(0f,110f));
        puBar.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);
        PowerUpHUD puHUD = puBar.AddComponent<PowerUpHUD>();

        string[] lblTxt = { "MAG","SHD","SPD","FLY","x2" };
        Color[]  cols   = {
            new Color(1f,0.2f,0.2f), new Color(0.2f,0.4f,1f),
            new Color(1f,0.55f,0f),  new Color(0.9f,0.9f,0.9f),
            new Color(0.7f,0.1f,1f)
        };
        puHUD.entries = new PowerUpHUD.HUDEntry[5];
        for (int i = 0; i < 5; i++)
        {
            GameObject entry = UIPanel(puBar.transform, "PU_Entry_" + i,
                new Vector2(0f,0.5f), new Vector2(0f,0.5f),
                new Vector2(20f + i * 205f, 0f), new Vector2(190f, 90f));
            Object.DestroyImmediate(entry.GetComponent<Image>());

            Image icon = UIImg(entry.transform, "Icon",
                new Vector2(0f,0.5f), new Vector2(0f,0.5f),
                Vector2.zero, new Vector2(60f,60f));
            icon.color = cols[i];

            Image fill = UIImg(entry.transform, "FillBar",
                new Vector2(0f,0f), new Vector2(0f,0f),
                new Vector2(65f,5f), new Vector2(120f,12f));
            fill.color       = cols[i];
            fill.type        = Image.Type.Filled;
            fill.fillMethod  = Image.FillMethod.Horizontal;
            fill.fillAmount  = 1f;

            TextMeshProUGUI lbl = UILabel(entry.transform, "Label",
                new Vector2(0f,1f), new Vector2(0f,1f),
                new Vector2(65f,-5f), new Vector2(120f,30f), lblTxt[i], 18f);

            puHUD.entries[i] = new PowerUpHUD.HUDEntry
                { root = entry, icon = icon, fillBar = fill, label = lbl };
            entry.SetActive(false);
        }

        // Game Over panel
        GameObject goPanel = UIPanel(canvasGO.transform, "GameOverPanel",
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            Vector2.zero, new Vector2(700f,700f));
        goPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);
        goPanel.SetActive(false);

        UILabel(goPanel.transform, "Title",
            new Vector2(0.5f,1f), new Vector2(0.5f,1f),
            new Vector2(0f,-60f), new Vector2(600f,80f), "GAME OVER", 64f
        ).color = new Color(1f, 0.2f, 0.2f);

        TextMeshProUGUI goScore = UILabel(goPanel.transform, "GOScore",
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            new Vector2(0f,60f), new Vector2(500f,60f), "0", 44f);

        TextMeshProUGUI goBest = UILabel(goPanel.transform, "GOBest",
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            new Vector2(0f,-10f), new Vector2(500f,50f), "BEST: 0", 32f);
        goBest.color = new Color(1f, 0.85f, 0.1f);

        // Restart button
        GameObject btnGO = new GameObject("RestartButton");
        btnGO.transform.SetParent(goPanel.transform, false);
        RectTransform btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin        = new Vector2(0.5f, 0f);
        btnRT.anchorMax        = new Vector2(0.5f, 0f);
        btnRT.anchoredPosition = new Vector2(0f, 80f);
        btnRT.sizeDelta        = new Vector2(300f, 80f);
        btnGO.AddComponent<Image>().color = new Color(0.1f, 0.75f, 0.2f);
        Button btn = btnGO.AddComponent<Button>();
        UILabel(btnGO.transform, "BtnLbl",
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            Vector2.zero, new Vector2(260f,60f), "RESTART", 36f);

        gm.gameOverPanel    = goPanel;
        gm.gameOverScoreTMP = goScore;
        gm.gameOverBestTMP  = goBest;
        gm.restartButton    = btn;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  UI HELPERS
    // ═════════════════════════════════════════════════════════════════════════

    static GameObject UIPanel(Transform parent, string name,
        Vector2 ancMin, Vector2 ancMax, Vector2 ancPos, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = ancMin;
        rt.anchorMax        = ancMax;
        rt.anchoredPosition = ancPos;
        rt.sizeDelta        = sizeDelta;
        go.AddComponent<Image>();
        return go;
    }

    static Image UIImg(Transform parent, string name,
        Vector2 ancMin, Vector2 ancMax, Vector2 ancPos, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = ancMin;
        rt.anchorMax        = ancMax;
        rt.anchoredPosition = ancPos;
        rt.sizeDelta        = sizeDelta;
        return go.AddComponent<Image>();
    }

    static TextMeshProUGUI UILabel(Transform parent, string name,
        Vector2 ancMin, Vector2 ancMax, Vector2 ancPos, Vector2 sizeDelta,
        string text, float fontSize)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = ancMin;
        rt.anchorMax        = ancMax;
        rt.anchoredPosition = ancPos;
        rt.sizeDelta        = sizeDelta;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        return tmp;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  UTILITIES
    // ═════════════════════════════════════════════════════════════════════════

    // Create a primitive, parent it, set position+scale+material, return it
    static GameObject Prim(PrimitiveType type, GameObject parent,
                            Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        if (parent != null) go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = localScale;
        if (mat != null) go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    static GameObject Save(GameObject go, string path)
    {
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return saved;
    }

    static void EnsureDirectories()
    {
        foreach (string d in new[] { PrefabDir, MaterialDir, SceneDir })
            if (!Directory.Exists(d)) Directory.CreateDirectory(d);
        AssetDatabase.Refresh();
    }

    static void AddToBuildSettings(string scenePath)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in scenes)
            if (s.path == scenePath) return;
        scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
