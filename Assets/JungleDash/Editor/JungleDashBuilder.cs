// JungleDashBuilder.cs – Builds the complete 2D Jungle Dash scene from scratch.
// Menu: Jungle Dash / 1 - Build Scene
//       Jungle Dash / 2 - Apply Android Settings
// Compatible with Unity 6 + URP. Zero CS errors.
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using TMPro;

public static class JungleDashBuilder
{
    const string SceneDir = "Assets/JungleDash/Scenes";
    const string ScenePath = SceneDir + "/JungleDash.unity";
    const string MatDir  = "Assets/JungleDash/Materials";
    const string PrefDir = "Assets/JungleDash/Prefabs";

    // ── 1. Build Scene ────────────────────────────────────────────────────────
    [MenuItem("Jungle Dash/1 - Build Scene")]
    public static void BuildScene()
    {
        // Guard TMP
        if (!Directory.Exists("Assets/TextMesh Pro"))
        {
            Debug.LogError("[JD] Import TMP Essential Resources first: " +
                           "Window > TextMeshPro > Import TMP Essential Resources");
            return;
        }

        EnsureDirs();

        // ── New scene ─────────────────────────────────────────────────────────
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Camera ────────────────────────────────────────────────────────────
        GameObject camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = new Color(0.08f, 0.15f, 0.08f);
        cam.orthographic     = true;
        cam.orthographicSize = 5f;
        cam.farClipPlane     = 100f;
        camGO.AddComponent<AudioListener>();
        // URP camera component auto-added, just configure defaults
        var urpCamData = camGO.GetComponent<UniversalAdditionalCameraData>();
        if (urpCamData == null) urpCamData = camGO.AddComponent<UniversalAdditionalCameraData>();
        urpCamData.renderPostProcessing = true;
        camGO.transform.position = new Vector3(0f, 0f, -10f);

        // CameraFollow
        var cf = camGO.AddComponent<CameraFollow>();
        cf.offset     = new Vector3(3f, 1f, -10f);
        cf.smoothTime = 0.15f;

        // ── Lighting (2D) ─────────────────────────────────────────────────────
        GameObject lightGO = new GameObject("GlobalLight2D");
        var light2d = lightGO.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        light2d.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Global;
        light2d.intensity = 1f;
        light2d.color     = new Color(0.85f, 0.95f, 0.75f);

        // ── Background layers ─────────────────────────────────────────────────
        BuildBackground();

        // ── Ground platform ───────────────────────────────────────────────────
        Material groundMat = SpriteColorMat("Mat_Ground", new Color(0.35f, 0.55f, 0.2f));
        Material platMat   = SpriteColorMat("Mat_Platform", new Color(0.42f, 0.62f, 0.25f));
        Material stoneMat  = SpriteColorMat("Mat_Stone", new Color(0.55f, 0.5f, 0.4f));

        // Long ground strip
        GameObject ground = CreateSpritePlane("Ground", new Vector2(200f, 2f), groundMat,
                                              new Vector3(0f, -3.5f, 0f));
        ground.tag = "Ground";
        var groundBC = ground.AddComponent<BoxCollider2D>();
        groundBC.size   = new Vector2(200f, 2f);
        ground.layer    = LayerMask.NameToLayer("Default");

        // ── Player ────────────────────────────────────────────────────────────
        Material playerMat = SpriteColorMat("Mat_Player", new Color(0.7f, 0.5f, 0.25f));
        GameObject player = CreateSpritePlane("Player", new Vector2(0.8f, 1.2f), playerMat,
                                              new Vector3(0f, -1.8f, 0f));
        player.tag = "Player";

        var rb = player.AddComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale = 3f;

        var col = player.AddComponent<BoxCollider2D>();
        col.size   = new Vector2(0.6f, 1.1f);
        col.offset = new Vector2(0f, 0f);

        var anim = player.AddComponent<Animator>();

        // Ground check
        GameObject gc = new GameObject("GroundCheck");
        gc.transform.SetParent(player.transform);
        gc.transform.localPosition = new Vector3(0f, -0.65f, 0f);

        var pc = player.AddComponent<PlayerController>();
        pc.groundCheck = gc.transform;
        pc.groundLayer = LayerMask.GetMask("Default");
        pc.runSpeed    = 6f;
        pc.maxSpeed    = 14f;
        pc.jumpForce   = 14f;

        // Wire camera to player
        cf.target = player.transform;
        camGO.transform.position = player.transform.position + cf.offset;

        // ── Platform prefabs ──────────────────────────────────────────────────
        GameObject platPrefab  = MakePlatformPrefab("PlatformGrass", 4f, platMat);
        GameObject platPrefab2 = MakePlatformPrefab("PlatformStone", 3f, stoneMat);
        GameObject platPrefab3 = MakePlatformPrefab("PlatformShort", 2f, platMat);

        // ── Coin prefab ───────────────────────────────────────────────────────
        Material coinMat = SpriteColorMat("Mat_Coin", new Color(1f, 0.82f, 0.1f));
        GameObject coinPrefab = CreateSpritePlane("Coin", new Vector2(0.5f, 0.5f), coinMat, Vector3.zero);
        coinPrefab.tag = "Coin";
        var coinCol = coinPrefab.AddComponent<CircleCollider2D>();
        coinCol.isTrigger = true;
        coinCol.radius    = 0.22f;
        coinPrefab.AddComponent<CoinBob>();
        string coinPrefabPath = PrefDir + "/Coin.prefab";
        coinPrefab = SavePrefab(coinPrefab, coinPrefabPath);

        // ── Enemy prefabs ─────────────────────────────────────────────────────
        Material monkeyMat = SpriteColorMat("Mat_Monkey", new Color(0.55f, 0.35f, 0.15f));
        Material plantMat  = SpriteColorMat("Mat_Plant",  new Color(0.2f,  0.6f,  0.1f));

        GameObject monkeyPrefab = CreateSpritePlane("EnemyMonkey", new Vector2(0.8f, 0.8f), monkeyMat, Vector3.zero);
        monkeyPrefab.tag = "Enemy";
        AddEnemyCollider(monkeyPrefab, new Vector2(0.6f, 0.7f));
        monkeyPrefab.AddComponent<EnemyPatrol>();
        monkeyPrefab = SavePrefab(monkeyPrefab, PrefDir + "/EnemyMonkey.prefab");

        GameObject plantPrefab = CreateSpritePlane("EnemyPlant", new Vector2(0.8f, 1.2f), plantMat, Vector3.zero);
        plantPrefab.tag = "Enemy";
        AddEnemyCollider(plantPrefab, new Vector2(0.5f, 1.0f));
        // Plant doesn't patrol, just sits
        plantPrefab = SavePrefab(plantPrefab, PrefDir + "/EnemyPlant.prefab");

        // ── PlatformSpawner ───────────────────────────────────────────────────
        GameObject spawnerGO = new GameObject("PlatformSpawner");
        var spawner = spawnerGO.AddComponent<PlatformSpawner>();
        spawner.platformPrefabs = new GameObject[] { platPrefab, platPrefab2, platPrefab3 };
        spawner.coinPrefab      = coinPrefab;
        spawner.enemyPrefabs    = new GameObject[] { monkeyPrefab, plantPrefab };
        spawner.playerTransform = player.transform;
        spawner.enemyChance     = 0.3f;
        spawner.coinChance      = 0.75f;
        spawner.coinsPerPlatform = 4;

        // ── Death zone (invisible floor below) ────────────────────────────────
        GameObject dz = new GameObject("DeathZone");
        dz.tag = "DeathZone";
        var dzCol = dz.AddComponent<BoxCollider2D>();
        dzCol.isTrigger = true;
        dzCol.size      = new Vector2(1000f, 1f);
        dz.transform.position = new Vector3(0f, -6f, 0f);

        // ── GameManager ───────────────────────────────────────────────────────
        GameObject gmGO = new GameObject("GameManager");
        var gm = gmGO.AddComponent<GameManager>();

        // ── Canvas ────────────────────────────────────────────────────────────
        BuildCanvas(gm);

        // ── EventSystem ───────────────────────────────────────────────────────
        GameObject esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        // ── Save ──────────────────────────────────────────────────────────────
        Directory.CreateDirectory(SceneDir);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[JungleDash] Scene built → " + ScenePath);
    }

    // ── 2. Android Settings ───────────────────────────────────────────────────
    [MenuItem("Jungle Dash/2 - Apply Android Settings")]
    public static void ApplyAndroid()
    {
        var target = NamedBuildTarget.Android;
        PlayerSettings.SetApplicationIdentifier(target, "com.yourname.jungledash");
        PlayerSettings.productName = "Jungle Dash";
        PlayerSettings.Android.minSdkVersion    = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures      = AndroidArchitecture.ARM64;
        PlayerSettings.defaultInterfaceOrientation      = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToLandscapeLeft  = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait       = false;
        Debug.Log("[JungleDash] Android settings applied.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ═════════════════════════════════════════════════════════════════════════

    static void BuildBackground()
    {
        // Sky gradient (far back)
        Material skyMat  = SpriteColorMat("Mat_BgSky",  new Color(0.30f, 0.55f, 0.22f));
        Material mid1Mat = SpriteColorMat("Mat_BgMid1", new Color(0.22f, 0.42f, 0.15f));
        Material mid2Mat = SpriteColorMat("Mat_BgMid2", new Color(0.15f, 0.32f, 0.10f));

        var sky = CreateSpritePlane("BG_Sky", new Vector2(22f, 12f), skyMat, new Vector3(0f, 0f, 10f));
        sky.AddComponent<BackgroundScroller>().parallaxFactor = 0.05f;

        var mid1 = CreateSpritePlane("BG_Mid1", new Vector2(22f, 8f), mid1Mat, new Vector3(0f, -1f, 5f));
        mid1.AddComponent<BackgroundScroller>().parallaxFactor = 0.2f;

        var mid2 = CreateSpritePlane("BG_Mid2", new Vector2(22f, 6f), mid2Mat, new Vector3(0f, -2f, 2f));
        mid2.AddComponent<BackgroundScroller>().parallaxFactor = 0.5f;
    }

    static GameObject MakePlatformPrefab(string name, float width, Material mat)
    {
        GameObject go = CreateSpritePlane(name, new Vector2(width, 0.6f), mat, Vector3.zero);
        go.tag = "Ground";
        var bc = go.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(width, 0.6f);
        go.layer = LayerMask.NameToLayer("Default");
        return SavePrefab(go, PrefDir + "/" + name + ".prefab");
    }

    static void AddEnemyCollider(GameObject go, Vector2 size)
    {
        var bc = go.AddComponent<BoxCollider2D>();
        bc.size      = size;
        bc.isTrigger = true;
    }

    // Creates a white quad with a SpriteRenderer showing a 1x1 white sprite
    static GameObject CreateSpritePlane(string name, Vector2 size, Material mat, Vector3 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite   = GetWhiteSprite();
        sr.material = mat;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size     = size;
        return go;
    }

    static Sprite _whiteSprite;
    static Sprite GetWhiteSprite()
    {
        if (_whiteSprite != null) return _whiteSprite;
        // 4x4 white texture → sprite
        Texture2D tex = new Texture2D(4, 4);
        Color[] pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return _whiteSprite;
    }

    static Material SpriteColorMat(string name, Color color)
    {
        string path = MatDir + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            // Use the URP Sprite/Lit or Sprite/Unlit shader
            Shader sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            mat = new Material(sh) { name = name };
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.color = color;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void BuildCanvas(GameManager gm)
    {
        GameObject cvGO = new GameObject("Canvas");
        var cv = cvGO.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cvGO.AddComponent<GraphicRaycaster>();
        var cs = cvGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight  = 1f;

        // Top HUD bar
        GameObject hud = Panel(cvGO.transform, "HUD",
            new Vector2(0,1), new Vector2(1,1), new Vector2(0,-50), new Vector2(0,100));
        hud.GetComponent<Image>().color = new Color(0, 0, 0, 0.45f);

        // Score
        TextMeshProUGUI scoreTMP = Label(hud.transform, "ScoreTMP",
            new Vector2(1,0.5f), new Vector2(1,0.5f), new Vector2(-20,0),
            new Vector2(500,60), "Score: 000000", 36f, TextAlignmentOptions.Right);

        // Coin icon area
        TextMeshProUGUI coinTMP = Label(hud.transform, "CoinTMP",
            new Vector2(0,0.5f), new Vector2(0,0.5f), new Vector2(20,0),
            new Vector2(200,60), "x0", 36f, TextAlignmentOptions.Left);

        // Title
        Label(hud.transform, "Title",
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), Vector2.zero,
            new Vector2(400,60), "JUNGLE DASH", 42f, TextAlignmentOptions.Center
        ).color = new Color(0.2f, 0.9f, 0.2f);

        gm.scoreTMP = scoreTMP;
        gm.coinTMP  = coinTMP;

        // Game Over panel
        GameObject goPanel = Panel(cvGO.transform, "GameOverPanel",
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(700,500));
        goPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.82f);
        goPanel.SetActive(false);

        Label(goPanel.transform, "Title",
            new Vector2(0.5f,1f), new Vector2(0.5f,1f), new Vector2(0,-60),
            new Vector2(600,80), "GAME OVER", 64f, TextAlignmentOptions.Center
        ).color = new Color(1f, 0.2f, 0.2f);

        TextMeshProUGUI goScore = Label(goPanel.transform, "Score",
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0,40),
            new Vector2(500,60), "Score: 0", 40f, TextAlignmentOptions.Center);

        TextMeshProUGUI goBest = Label(goPanel.transform, "Best",
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0,-20),
            new Vector2(500,50), "Best: 0", 30f, TextAlignmentOptions.Center);
        goBest.color = new Color(1f, 0.85f, 0.1f);

        // Restart button
        GameObject btnGO = new GameObject("RestartBtn");
        btnGO.transform.SetParent(goPanel.transform, false);
        var btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.5f, 0); btnRT.anchorMax = new Vector2(0.5f, 0);
        btnRT.anchoredPosition = new Vector2(0, 60); btnRT.sizeDelta = new Vector2(280, 72);
        btnGO.AddComponent<Image>().color = new Color(0.1f, 0.75f, 0.2f);
        var btn = btnGO.AddComponent<Button>();
        Label(btnGO.transform, "BtnLbl",
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), Vector2.zero,
            new Vector2(260,60), "PLAY AGAIN", 34f, TextAlignmentOptions.Center);

        gm.gameOverPanel    = goPanel;
        gm.gameOverScoreTMP = goScore;
        gm.gameOverBestTMP  = goBest;
        gm.restartButton    = btn;
    }

    static GameObject Panel(Transform parent, string name,
        Vector2 ancMin, Vector2 ancMax, Vector2 ancPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.anchoredPosition = ancPos; rt.sizeDelta = size;
        go.AddComponent<Image>();
        return go;
    }

    static TextMeshProUGUI Label(Transform parent, string name,
        Vector2 ancMin, Vector2 ancMax, Vector2 ancPos, Vector2 size,
        string text, float fontSize, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.anchoredPosition = ancPos; rt.sizeDelta = size;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.alignment = align;
        tmp.color     = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        return tmp;
    }

    static GameObject SavePrefab(GameObject go, string path)
    {
        var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return saved;
    }

    static void EnsureDirs()
    {
        foreach (var d in new[] { SceneDir, MatDir, PrefDir })
            if (!Directory.Exists(d)) Directory.CreateDirectory(d);
        AssetDatabase.Refresh();
    }

    static void AddToBuildSettings(string path)
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in list) if (s.path == path) return;
        list.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
#endif
