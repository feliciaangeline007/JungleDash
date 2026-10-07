// JungleDashBuilder.cs – Builds 3D endless runner scene. Unity 6 Built-in RP.
// Menu: Jungle Dash / 1 - Build Scene
//       Jungle Dash / 2 - Apply Android Settings
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
    const string SceneDir = "Assets/JungleDash/Scenes";
    const string ScenePath = SceneDir + "/JungleDash.unity";
    const string MatDir   = "Assets/JungleDash/Materials";
    const string PrefDir  = "Assets/JungleDash/Prefabs";

    // SA paths (fall back to primitives if missing)
    const string EthanPath  = "Assets/Standard Assets/Characters/ThirdPersonCharacter/Models/Ethan.fbx";
    const string EthanMat   = "Assets/Standard Assets/Characters/ThirdPersonCharacter/Materials/EthanWhite.mat";
    const string AnimCtrl   = "Assets/Standard Assets/Characters/ThirdPersonCharacter/Animator/ThirdPersonAnimatorController.controller";
    const string MudTex     = "Assets/Standard Assets/Environment/TerrainAssets/SurfaceTextures/MudRockyAlbedoSpecular.bmp";
    const string GrassTex   = "Assets/Standard Assets/Environment/TerrainAssets/SurfaceTextures/GrassHillAlbedo.psd";
    const string CliffTex   = "Assets/Standard Assets/Environment/TerrainAssets/SurfaceTextures/CliffAlbedoSpecular.psd";
    const string MudNorm    = "Assets/Standard Assets/Environment/TerrainAssets/SurfaceTextures/MudRockyNormals.bmp";

    // ── 1 ────────────────────────────────────────────────────────────────────
    [MenuItem("Jungle Dash/1 - Build Scene")]
    public static void BuildScene()
    {
        if (!Directory.Exists("Assets/TextMesh Pro"))
        {
            Debug.LogError("[JD] Import TMP Essential Resources first (Window > TextMeshPro > Import TMP Essential Resources).");
            return;
        }
        if (Shader.Find("Standard") == null)
        {
            Debug.LogError("[JD] Standard shader not found. Use Built-in Render Pipeline.");
            return;
        }

        EnsureDirs();

        // Materials
        Material matTrail  = StdMat("Mat_Trail",  "#7A5C14", MudTex,  MudNorm);
        Material matGrass  = StdMat("Mat_Grass",  "#3A6A1A", GrassTex, null);
        Material matRock   = StdMat("Mat_Rock",   "#555050", CliffTex, null);
        Material matBark   = StdMat("Mat_Bark",   "#4A2A0A", null, null);
        Material matGold   = EmissiveMat("Mat_Gold",  "#FFD700", "#FFD700", 0.5f);
        Material matCyan   = EmissiveMat("Mat_Gem",   "#00EEFF", "#00EEFF", 0.9f);
        Material matShield = TransMat("Mat_Shield",   "#4488FF", 0.28f);

        // Prefabs
        GameObject coinPfb   = MakeCoin(matGold);
        GameObject gemPfb    = MakeGem(matCyan);
        GameObject logPfb    = MakeLog(matBark, StdMat("Mat_Cut","#C09050",null,null));
        GameObject boulderPfb= MakeBoulder(matRock, matGrass);
        GameObject stumpPfb  = MakeStump(matBark, matGrass);
        GameObject segPfb    = MakeSegment(matTrail, matGrass, matRock);

        // Scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Lighting + environment
        BuildLighting();

        // Player
        GameObject player = BuildPlayer(matShield);

        // Camera
        GameObject camGO = BuildCamera(player.transform);
        GameObject speedLines = BuildSpeedLines(camGO.transform);

        // Spawner
        GameObject spawnGO = new GameObject("TrackSpawner");
        TrackSpawner sp = spawnGO.AddComponent<TrackSpawner>();
        sp.segmentPrefab      = segPfb.GetComponent<TrackSegment>();
        sp.coinPrefab         = coinPfb;
        sp.gemPrefab          = gemPfb;
        sp.obstaclePrefabs    = new GameObject[]{ logPfb, boulderPfb, stumpPfb };
        sp.laneWidth          = 2f;
        sp.rowSpacing         = 7.5f;
        sp.segmentCount       = 6;
        sp.emptyStartSegments = 2;

        // GameManager
        GameObject gmGO = new GameObject("GameManager");
        GameManager gm  = gmGO.AddComponent<GameManager>();
        gm.PlayerTransform = player.transform;

        // Canvas / UI
        BuildCanvas(gm);

        // Save
        Directory.CreateDirectory(SceneDir);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuild(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[JungleDash] Scene built → " + ScenePath);
    }

    // ── 2 ────────────────────────────────────────────────────────────────────
    [MenuItem("Jungle Dash/2 - Apply Android Settings")]
    public static void ApplyAndroid()
    {
        var tgt = NamedBuildTarget.Android;
        PlayerSettings.SetApplicationIdentifier(tgt, "com.yourname.jungledash");
        PlayerSettings.productName   = "Jungle Dash";
        PlayerSettings.Android.minSdkVersion    = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.SetScriptingBackend(tgt, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures        = AndroidArchitecture.ARM64;
        PlayerSettings.defaultInterfaceOrientation        = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait        = true;
        PlayerSettings.allowedAutorotateToLandscapeLeft   = false;
        PlayerSettings.allowedAutorotateToLandscapeRight  = false;
        Debug.Log("[JungleDash] Android settings applied.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  MATERIAL HELPERS
    // ═════════════════════════════════════════════════════════════════════════
    static Material StdMat(string name, string hex, string texPath, string normPath)
    {
        string p = MatDir + "/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null) { m = new Material(Shader.Find("Standard")){ name=name }; AssetDatabase.CreateAsset(m, p); }
        ColorUtility.TryParseHtmlString(hex, out Color c); m.color = c; m.enableInstancing = true;
        Texture2D tx = texPath  != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(texPath)  : null;
        Texture2D nm = normPath != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(normPath) : null;
        if (tx != null) m.mainTexture = tx;
        if (nm != null) { m.SetTexture("_BumpMap", nm); m.EnableKeyword("_NORMALMAP"); }
        EditorUtility.SetDirty(m); return m;
    }
    static Material EmissiveMat(string name, string hex, string hexE, float i)
    {
        string p = MatDir + "/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null) { m = new Material(Shader.Find("Standard")){ name=name }; AssetDatabase.CreateAsset(m, p); }
        ColorUtility.TryParseHtmlString(hex,  out Color c);
        ColorUtility.TryParseHtmlString(hexE, out Color e);
        m.color = c; m.SetColor("_EmissionColor", e*i); m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        m.SetFloat("_Metallic",0.7f); m.SetFloat("_Glossiness",0.85f);
        m.enableInstancing = true; EditorUtility.SetDirty(m); return m;
    }
    static Material TransMat(string name, string hex, float alpha)
    {
        string p = MatDir + "/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null) { m = new Material(Shader.Find("Standard")){ name=name }; AssetDatabase.CreateAsset(m, p); }
        ColorUtility.TryParseHtmlString(hex, out Color c); c.a = alpha;
        m.SetFloat("_Mode",2);
        m.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite",0);
        m.DisableKeyword("_ALPHATEST_ON"); m.EnableKeyword("_ALPHABLEND_ON"); m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = 3000; m.color = c; m.enableInstancing = true; EditorUtility.SetDirty(m); return m;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  PREFAB BUILDERS
    // ═════════════════════════════════════════════════════════════════════════
    static GameObject MakeCoin(Material mat)
    {
        GameObject go = new GameObject("Coin");
        go.tag = "Coin";
        var disk = P(PrimitiveType.Cylinder, go, Vector3.zero, new Vector3(0.45f,0.06f,0.45f), mat);
        Object.DestroyImmediate(disk.GetComponent<CapsuleCollider>());
        var sc = go.AddComponent<SphereCollider>(); sc.radius = 0.3f; sc.isTrigger = true;
        go.AddComponent<Collectible>();
        return Save(go, PrefDir+"/Coin.prefab");
    }
    static GameObject MakeGem(Material mat)
    {
        GameObject go = new GameObject("Gem");
        go.tag = "Gem";
        var b = P(PrimitiveType.Sphere, go, Vector3.zero, new Vector3(0.5f,0.7f,0.5f), mat);
        Object.DestroyImmediate(b.GetComponent<SphereCollider>());
        var sc = go.AddComponent<SphereCollider>(); sc.radius = 0.35f; sc.isTrigger = true;
        go.AddComponent<Collectible>();
        return Save(go, PrefDir+"/Gem.prefab");
    }
    static GameObject MakeLog(Material bark, Material cut)
    {
        GameObject go = new GameObject("ObstacleLog");
        go.tag = "Obstacle";
        var body = P(PrimitiveType.Cylinder, go, new Vector3(0,0.45f,0), new Vector3(0.5f,0.85f,0.5f), bark);
        body.transform.localRotation = Quaternion.Euler(0,0,90);
        Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());
        for(int s=-1;s<=1;s+=2){
            var cap=P(PrimitiveType.Cylinder,go,new Vector3(s*0.85f,0.45f,0),new Vector3(0.5f,0.05f,0.5f),cut);
            cap.transform.localRotation=Quaternion.Euler(0,0,90); Object.DestroyImmediate(cap.GetComponent<CapsuleCollider>());
        }
        var bc=go.AddComponent<BoxCollider>(); bc.center=new Vector3(0,0.45f,0); bc.size=new Vector3(1.7f,0.9f,0.9f); bc.isTrigger=true;
        Obstacle obs=go.AddComponent<Obstacle>(); obs.obstacleRenderer=body.GetComponent<MeshRenderer>();
        AddDebris(go);
        return Save(go, PrefDir+"/ObstacleLog.prefab");
    }
    static GameObject MakeBoulder(Material rock, Material moss)
    {
        GameObject go = new GameObject("ObstacleBoulder"); go.tag="Obstacle";
        var b=P(PrimitiveType.Sphere,go,new Vector3(0,0.5f,0),new Vector3(1.1f,0.9f,1.0f),rock);
        Object.DestroyImmediate(b.GetComponent<SphereCollider>());
        var t=P(PrimitiveType.Sphere,go,new Vector3(0,0.85f,0),new Vector3(0.8f,0.35f,0.8f),moss);
        Object.DestroyImmediate(t.GetComponent<SphereCollider>());
        var bc=go.AddComponent<BoxCollider>(); bc.center=new Vector3(0,0.5f,0); bc.size=new Vector3(1.1f,1.0f,1.0f); bc.isTrigger=true;
        Obstacle obs=go.AddComponent<Obstacle>(); obs.obstacleRenderer=b.GetComponent<MeshRenderer>();
        AddDebris(go);
        return Save(go, PrefDir+"/ObstacleBoulder.prefab");
    }
    static GameObject MakeStump(Material wood, Material moss)
    {
        GameObject go = new GameObject("ObstacleStump"); go.tag="Obstacle";
        var tr=P(PrimitiveType.Cylinder,go,new Vector3(0,0.5f,0),new Vector3(0.7f,0.5f,0.7f),wood);
        Object.DestroyImmediate(tr.GetComponent<CapsuleCollider>());
        var tp=P(PrimitiveType.Cylinder,go,new Vector3(0,1.06f,0),new Vector3(0.72f,0.06f,0.72f),moss);
        Object.DestroyImmediate(tp.GetComponent<CapsuleCollider>());
        var bc=go.AddComponent<BoxCollider>(); bc.center=new Vector3(0,0.5f,0); bc.size=new Vector3(0.75f,1.0f,0.75f); bc.isTrigger=true;
        Obstacle obs=go.AddComponent<Obstacle>(); obs.obstacleRenderer=tr.GetComponent<MeshRenderer>();
        AddDebris(go);
        return Save(go, PrefDir+"/ObstacleStump.prefab");
    }
    static void AddDebris(GameObject parent)
    {
        var d=new GameObject("Debris"); d.transform.SetParent(parent.transform); d.transform.localPosition=new Vector3(0,0.5f,0);
        var ps=d.AddComponent<ParticleSystem>();
        var mn=ps.main; mn.playOnAwake=false; mn.loop=false; mn.duration=0.5f; mn.startLifetime=0.7f; mn.startSpeed=5f; mn.startSize=0.15f; mn.maxParticles=30;
        var em=ps.emission; em.enabled=true; em.SetBurst(0,new ParticleSystem.Burst(0f,20));
        var sh=ps.shape; sh.enabled=true; sh.shapeType=ParticleSystemShapeType.Sphere; sh.radius=0.3f;
        ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
    }
    static GameObject MakeSegment(Material trail, Material grass, Material rock)
    {
        GameObject root = new GameObject("TrackSegment");
        var ground = P(PrimitiveType.Cube, root, Vector3.zero, Vector3.one, trail);
        ground.name = "Ground";
        // Grass banks
        for(int s=-1;s<=1;s+=2){
            var bk=P(PrimitiveType.Cube,root,new Vector3(s*5f,-0.3f,15f),new Vector3(4f,0.4f,30f),grass);
            bk.name="Bank"; Object.DestroyImmediate(bk.GetComponent<BoxCollider>());
        }
        // Rock accents
        for(int s=-1;s<=1;s+=2)
            for(int r=0;r<3;r++){
                var rk=P(PrimitiveType.Cube,root,new Vector3(s*3.2f,0.1f,5f+r*9f),new Vector3(0.4f,0.35f,0.55f),rock);
                rk.name="Rock"; rk.transform.localRotation=Quaternion.Euler(0,r*15f,0); Object.DestroyImmediate(rk.GetComponent<BoxCollider>());
            }
        // Bushes
        for(int s=-1;s<=1;s+=2)
            for(int b=0;b<4;b++){
                var bush=P(PrimitiveType.Sphere,root,new Vector3(s*3.8f,0.15f,3f+b*7f),new Vector3(0.7f,0.35f,0.7f),grass);
                bush.name="Bush"; Object.DestroyImmediate(bush.GetComponent<SphereCollider>());
            }
        // Trees (primitive fallback)
        for(int s=-1;s<=1;s+=2)
            for(int t=0;t<4;t++){
                var tree=new GameObject("Tree"); tree.transform.SetParent(root.transform); tree.transform.localPosition=new Vector3(s*6.5f,0,4f+t*7f);
                var trunk=P(PrimitiveType.Cylinder,tree,new Vector3(0,1.5f,0),new Vector3(0.22f,1.5f,0.22f),rock);
                Object.DestroyImmediate(trunk.GetComponent<CapsuleCollider>());
                var crown=P(PrimitiveType.Sphere,tree,new Vector3(0,3.5f,0),new Vector3(1.5f,1.8f,1.5f),grass);
                Object.DestroyImmediate(crown.GetComponent<SphereCollider>());
            }
        var seg=root.AddComponent<TrackSegment>();
        seg.segmentLength=30f; seg.groundWidth=6f; seg.groundThickness=0.5f; seg.groundChild=ground.transform;
        seg.ApplyGroundScale();
        return Save(root, PrefDir+"/TrackSegment.prefab");
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  SCENE OBJECTS
    // ═════════════════════════════════════════════════════════════════════════
    static void BuildLighting()
    {
        var sunGO=new GameObject("Sun"); var sun=sunGO.AddComponent<Light>();
        sun.type=LightType.Directional; sun.color=new Color(1f,0.92f,0.7f);
        sun.intensity=1.2f; sun.shadows=LightShadows.Hard;
        sunGO.transform.rotation=Quaternion.Euler(52f,-30f,0f);
        QualitySettings.shadowDistance=25f;

        Material sky=new Material(Shader.Find("Skybox/Procedural"));
        if(sky!=null){ sky.name="ProcSkybox"; AssetDatabase.CreateAsset(sky,MatDir+"/ProcSkybox.mat"); sky.SetFloat("_AtmosphereThickness",1f); RenderSettings.skybox=sky; }

        RenderSettings.fog=true; RenderSettings.fogMode=FogMode.Linear;
        RenderSettings.fogStartDistance=35f; RenderSettings.fogEndDistance=120f;
        RenderSettings.fogColor=new Color(0.62f,0.72f,0.58f);
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(0.5f,0.65f,0.45f);
        RenderSettings.ambientEquatorColor=new Color(0.45f,0.5f,0.38f);
        RenderSettings.ambientGroundColor=new Color(0.25f,0.22f,0.15f);
    }

    static GameObject BuildPlayer(Material shieldMat)
    {
        GameObject player=new GameObject("Player"); player.tag="Player";
        var cc=player.AddComponent<CharacterController>(); cc.height=1.8f; cc.radius=0.3f; cc.center=new Vector3(0,0.9f,0); cc.slopeLimit=45f; cc.stepOffset=0.3f;
        var rb=player.AddComponent<Rigidbody>(); rb.isKinematic=true; rb.useGravity=false;
        var runner=player.AddComponent<PlayerRunner>();

        // Try Ethan, else capsule
        var animCtrl=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimCtrl);
        var ethanFBX=AssetDatabase.LoadAssetAtPath<GameObject>(EthanPath);
        if(ethanFBX!=null)
        {
            var ethan=(GameObject)PrefabUtility.InstantiatePrefab(ethanFBX,player.transform);
            ethan.name="EthanModel"; ethan.transform.localPosition=Vector3.zero; ethan.transform.localRotation=Quaternion.identity;
            var baseMat=AssetDatabase.LoadAssetAtPath<Material>(EthanMat);
            if(baseMat!=null){
                var expl=new Material(baseMat){name="Mat_Explorer",color=new Color(0.72f,0.65f,0.35f)};
                expl.enableInstancing=true;
                string ep=MatDir+"/Mat_Explorer.mat";
                if(AssetDatabase.LoadAssetAtPath<Material>(ep)==null) AssetDatabase.CreateAsset(expl,ep);
                foreach(var r in ethan.GetComponentsInChildren<SkinnedMeshRenderer>()) r.sharedMaterial=expl;
            }
            var anim=ethan.GetComponent<Animator>()??ethan.AddComponent<Animator>();
            if(animCtrl!=null) anim.runtimeAnimatorController=animCtrl;
            anim.applyRootMotion=false;
            runner.characterAnimator=anim;
        }
        else
        {
            Debug.LogWarning("[JD] Ethan.fbx not found – using capsule.");
            var cap=P(PrimitiveType.Capsule,player,new Vector3(0,0.9f,0),new Vector3(0.5f,0.9f,0.5f),null);
            cap.name="Body"; Object.DestroyImmediate(cap.GetComponent<CapsuleCollider>());
        }

        // Shield sphere
        var sph=P(PrimitiveType.Sphere,player,new Vector3(0,0.9f,0),new Vector3(1.3f,1.3f,1.3f),shieldMat);
        sph.name="ShieldSphere";
        var sr=sph.GetComponent<MeshRenderer>(); sr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; sr.receiveShadows=false;
        Object.DestroyImmediate(sph.GetComponent<SphereCollider>()); sph.SetActive(false);

        return player;
    }

    static GameObject BuildCamera(Transform playerTf)
    {
        var camGO=new GameObject("Main Camera"); camGO.tag="MainCamera";
        var cam=camGO.AddComponent<Camera>(); cam.farClipPlane=130f; cam.fieldOfView=65f; cam.clearFlags=CameraClearFlags.Skybox;
        camGO.AddComponent<AudioListener>();
        var cf=camGO.AddComponent<CameraFollow>(); cf.target=playerTf; cf.baseFOV=65f; cf.farClip=130f; cf.offset=new Vector3(0,2.8f,-5.5f);
        camGO.transform.position=playerTf.position+new Vector3(0,2.8f,-5.5f);
        return camGO;
    }

    static GameObject BuildSpeedLines(Transform camTf)
    {
        var go=new GameObject("SpeedLines"); go.transform.SetParent(camTf); go.transform.localPosition=new Vector3(0,0,2f);
        var ps=go.AddComponent<ParticleSystem>();
        var mn=ps.main; mn.loop=true; mn.playOnAwake=false; mn.startLifetime=0.15f; mn.startSpeed=15f; mn.startSize=new ParticleSystem.MinMaxCurve(0.01f,0.04f); mn.startColor=new Color(1,1,1,0.3f); mn.maxParticles=80;
        var em=ps.emission; em.rateOverTime=0f;
        var sh=ps.shape; sh.shapeType=ParticleSystemShapeType.Cone; sh.angle=25f;
        ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        return go;
    }

    static void BuildCanvas(GameManager gm)
    {
        var esGO=new GameObject("EventSystem"); esGO.AddComponent<EventSystem>(); esGO.AddComponent<StandaloneInputModule>();

        var cvGO=new GameObject("Canvas"); var cv=cvGO.AddComponent<Canvas>(); cv.renderMode=RenderMode.ScreenSpaceOverlay;
        cvGO.AddComponent<GraphicRaycaster>();
        var cs=cvGO.AddComponent<CanvasScaler>(); cs.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; cs.referenceResolution=new Vector2(1080,1920); cs.matchWidthOrHeight=0.5f;

        // HUD top bar
        var hud=UIPanel(cvGO.transform,"HUD",new Vector2(0,1),new Vector2(1,1),new Vector2(0,-40),new Vector2(0,80));
        hud.GetComponent<Image>().color=new Color(0,0,0,0.5f);

        var scoreTMP=UILabel(hud.transform,"ScoreTMP",new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(500,60),"Score: 000000",36f);
        var coinTMP =UILabel(hud.transform,"CoinTMP", new Vector2(1,0.5f),   new Vector2(1,0.5f),   new Vector2(-90,0),new Vector2(160,60),"x0",28f);

        // Title label top-left
        var title=UILabel(hud.transform,"Title",new Vector2(0,0.5f),new Vector2(0,0.5f),new Vector2(10,0),new Vector2(300,60),"JUNGLE DASH",30f);
        title.color=new Color(0.2f,1f,0.3f);

        gm.scoreTMP=scoreTMP; gm.coinTMP=coinTMP;

        // Game Over panel
        var gop=UIPanel(cvGO.transform,"GameOverPanel",new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(700,600));
        gop.GetComponent<Image>().color=new Color(0,0,0,0.8f); gop.SetActive(false);

        UILabel(gop.transform,"GOTitle",new Vector2(0.5f,1f),new Vector2(0.5f,1f),new Vector2(0,-60),new Vector2(600,80),"GAME OVER",64f).color=new Color(1f,0.2f,0.2f);
        var goScore=UILabel(gop.transform,"GOScore",new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),new Vector2(0,50),new Vector2(500,60),"Score: 0",42f);
        var goBest= UILabel(gop.transform,"GOBest", new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),new Vector2(0,-10),new Vector2(500,50),"Best: 0",30f);
        goBest.color=new Color(1f,0.85f,0.1f);

        var btnGO=new GameObject("RestartBtn"); btnGO.transform.SetParent(gop.transform,false);
        var btnRT=btnGO.AddComponent<RectTransform>(); btnRT.anchorMin=new Vector2(0.5f,0); btnRT.anchorMax=new Vector2(0.5f,0); btnRT.anchoredPosition=new Vector2(0,60); btnRT.sizeDelta=new Vector2(280,70);
        btnGO.AddComponent<Image>().color=new Color(0.1f,0.75f,0.2f);
        var btn=btnGO.AddComponent<Button>();
        UILabel(btnGO.transform,"BtnLbl",new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(260,60),"PLAY AGAIN",34f);

        gm.gameOverPanel=gop; gm.gameOverScoreTMP=goScore; gm.gameOverBestTMP=goBest; gm.restartButton=btn;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  TINY HELPERS
    // ═════════════════════════════════════════════════════════════════════════
    static GameObject P(PrimitiveType type,GameObject parent,Vector3 pos,Vector3 scale,Material mat)
    {
        var go=GameObject.CreatePrimitive(type);
        if(parent!=null) go.transform.SetParent(parent.transform,false);
        go.transform.localPosition=pos; go.transform.localScale=scale;
        if(mat!=null) go.GetComponent<MeshRenderer>().sharedMaterial=mat;
        return go;
    }
    static GameObject Save(GameObject go,string path)
    {
        var saved=PrefabUtility.SaveAsPrefabAsset(go,path);
        Object.DestroyImmediate(go); return saved;
    }
    static GameObject UIPanel(Transform parent,string name,Vector2 aMin,Vector2 aMax,Vector2 aPos,Vector2 size)
    {
        var go=new GameObject(name); go.transform.SetParent(parent,false);
        var rt=go.AddComponent<RectTransform>(); rt.anchorMin=aMin; rt.anchorMax=aMax; rt.anchoredPosition=aPos; rt.sizeDelta=size;
        go.AddComponent<Image>(); return go;
    }
    static TextMeshProUGUI UILabel(Transform parent,string name,Vector2 aMin,Vector2 aMax,Vector2 aPos,Vector2 size,string text,float fs)
    {
        var go=new GameObject(name); go.transform.SetParent(parent,false);
        var rt=go.AddComponent<RectTransform>(); rt.anchorMin=aMin; rt.anchorMax=aMax; rt.anchoredPosition=aPos; rt.sizeDelta=size;
        var tmp=go.AddComponent<TextMeshProUGUI>(); tmp.text=text; tmp.fontSize=fs; tmp.color=Color.white; tmp.alignment=TextAlignmentOptions.Center; tmp.fontStyle=FontStyles.Bold;
        return tmp;
    }
    static void EnsureDirs()
    {
        foreach(var d in new[]{SceneDir,MatDir,PrefDir}) if(!Directory.Exists(d)) Directory.CreateDirectory(d);
        AssetDatabase.Refresh();
    }
    static void AddToBuild(string path)
    {
        var list=new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach(var s in list) if(s.path==path) return;
        list.Insert(0,new EditorBuildSettingsScene(path,true));
        EditorBuildSettings.scenes=list.ToArray();
    }
}
#endif
