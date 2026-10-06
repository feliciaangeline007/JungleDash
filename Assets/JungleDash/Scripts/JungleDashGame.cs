using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A self-contained Unity version of Jungle Dash. The scene and runner world are
/// generated at runtime so the project opens and plays without manual setup.
/// </summary>
public sealed class JungleDashGame : MonoBehaviour
{
    private enum GameMode { Menu, Playing, Paused, GameOver }
    private enum EntityKind { Coin, Gem, Powerup, Obstacle }

    private sealed class Entity
    {
        public GameObject root;
        public EntityKind kind;
        public int lane;
        public float z;
        public float obstacleHeight;
        public string powerup;
        public bool spent;
        public float phase;
    }

    private const float LaneWidth = 2.5f;
    private const float PlayerZ = 0.7f;
    private const float StartSpeed = 11.2f;
    private const float MaxSpeed = 23f;
    private const float RowSpacing = 14f;
    private static readonly float[] LaneX = { -LaneWidth, 0f, LaneWidth };
    private static readonly string[] PowerupIds = { "magnet", "shield", "speed", "fly", "double" };

    private readonly List<Entity> entities = new List<Entity>();
    private readonly List<Transform> trees = new List<Transform>();
    private readonly List<Transform> laneMarkers = new List<Transform>();
    private readonly Dictionary<string, float> powerups = new Dictionary<string, float>();
    private readonly List<Transform> runnerLegs = new List<Transform>();

    private GameMode mode = GameMode.Menu;
    private Transform runner;
    private Transform runnerVisual;
    private Transform shield;
    private Camera gameCamera;
    private Material coinMaterial;
    private Material gemMaterial;
    private Material powerupMaterial;
    private Material obstacleMaterial;
    private Material rockMaterial;
    private Material barkMaterial;
    private Material leafMaterial;
    private Material lightLeafMaterial;

    private int laneIndex = 1;
    private float runnerX;
    private float jumpHeight;
    private float verticalSpeed;
    private bool grounded = true;
    private float speed = StartSpeed;
    private float distance;
    private float coinCount;
    private float score;
    private float pickupPoints;
    private float spawnHeadZ = -82f;
    private float scoreAccumulator;
    private int best;
    private int previousFreeLane = 1;
    private bool previousRowWasSingle;
    private bool soundEnabled = true;
    private float runClock;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        best = PlayerPrefs.GetInt("jungle-dash-best-v1", 0);
        CreateMaterials();
        CreateWorld();
        CreateRunner();
        PopulatePreview();
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, 0.04f);
        ReadInput();
        if (mode != GameMode.Playing)
            return;

        runClock += dt;
        speed = Mathf.Min(MaxSpeed, speed + 0.24f * dt);
        float currentSpeed = speed * (HasPowerup("speed") ? 1.44f : 1f);
        distance += currentSpeed * dt;
        scoreAccumulator += currentSpeed * dt * (HasPowerup("double") ? 2f : 1f);
        score = Mathf.Floor(scoreAccumulator) + pickupPoints;
        if (score > best)
        {
            best = Mathf.FloorToInt(score);
            PlayerPrefs.SetInt("jungle-dash-best-v1", best);
        }

        spawnHeadZ += currentSpeed * dt;
        while (spawnHeadZ > -82f)
        {
            SpawnRow(spawnHeadZ);
            spawnHeadZ -= RowSpacing;
        }

        runnerX = Mathf.Lerp(runnerX, LaneX[laneIndex], Mathf.Min(1f, dt * 13f));
        AnimateRunner();
        runner.position = new Vector3(runnerX, jumpHeight + (HasPowerup("fly") ? 2.35f : 0f), PlayerZ);
        UpdateWorld(currentSpeed, dt);
        UpdatePowerups(dt);
        UpdateEntities(currentSpeed, dt);
    }

    private void ReadInput()
    {
        if (mode == GameMode.Playing)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) MoveLeft();
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) MoveRight();
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W)) Jump();
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) mode = GameMode.Paused;

            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
            {
                Touch touch = Input.GetTouch(0);
                Vector2 delta = touch.position - touch.rawPosition;
                if (Mathf.Abs(delta.x) > 45f && Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                {
                    if (delta.x < 0) MoveLeft(); else MoveRight();
                }
                else if (delta.y > 45f)
                {
                    Jump();
                }
            }
        }
        else if (mode == GameMode.Paused && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)))
        {
            mode = GameMode.Playing;
        }
        else if ((mode == GameMode.Menu || mode == GameMode.GameOver) &&
                 (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)))
        {
            StartRun();
        }
    }

    private void CreateMaterials()
    {
        coinMaterial = MakeMaterial(new Color(1f, 0.72f, 0.18f), 0.55f, 0.25f);
        gemMaterial = MakeMaterial(new Color(0.85f, 0.35f, 1f), 0.2f, 0.18f);
        powerupMaterial = MakeMaterial(new Color(0.33f, 0.88f, 0.92f), 0.2f, 0.22f);
        obstacleMaterial = MakeMaterial(new Color(0.37f, 0.23f, 0.13f), 0f, 0.88f);
        rockMaterial = MakeMaterial(new Color(0.43f, 0.44f, 0.39f), 0f, 0.95f);
        barkMaterial = MakeMaterial(new Color(0.35f, 0.24f, 0.14f), 0f, 0.95f);
        leafMaterial = MakeMaterial(new Color(0.15f, 0.39f, 0.23f), 0f, 0.92f);
        lightLeafMaterial = MakeMaterial(new Color(0.35f, 0.56f, 0.23f), 0f, 0.9f);
    }

    private Material MakeMaterial(Color color, float metallic, float roughness)
    {
        Material material = new Material(Shader.Find("Standard"));
        material.color = color;
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Glossiness", 1f - roughness);
        return material;
    }

    private void CreateWorld()
    {
        RenderSettings.ambientLight = new Color(0.69f, 0.78f, 0.58f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.66f, 0.82f, 0.54f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 38f;
        RenderSettings.fogEndDistance = 128f;

        GameObject cameraObject = new GameObject("Jungle Dash Camera");
        gameCamera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        gameCamera.fieldOfView = 58f;
        gameCamera.backgroundColor = RenderSettings.fogColor;
        gameCamera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.transform.position = new Vector3(0f, 5.1f, 9.8f);
        cameraObject.transform.LookAt(new Vector3(0f, 1.3f, -10f));

        GameObject sunObject = new GameObject("Jungle Sun");
        Light sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.91f, 0.69f);
        sun.intensity = 1.35f;
        sun.shadows = LightShadows.Soft;
        sunObject.transform.rotation = Quaternion.Euler(45f, -28f, 0f);

        Material grass = MakeMaterial(new Color(0.38f, 0.53f, 0.27f), 0f, 1f);
        Material path = MakeMaterial(new Color(0.64f, 0.55f, 0.38f), 0f, 0.98f);
        Texture2D pathTexture = Resources.Load<Texture2D>("MudRocky");
        if (pathTexture != null)
        {
            path.mainTexture = pathTexture;
            path.mainTextureScale = new Vector2(2f, 22f);
        }

        CreatePrimitive(PrimitiveType.Cube, "Forest floor", null, new Vector3(0f, -0.32f, -70f),
            new Vector3(100f, 0.25f, 200f), grass);
        CreatePrimitive(PrimitiveType.Cube, "Running trail", null, new Vector3(0f, -0.16f, -70f),
            new Vector3(8.5f, 0.2f, 180f), path);

        for (int i = 0; i < 14; i++)
        {
            GameObject marker = new GameObject("Trail markers");
            marker.transform.position = new Vector3(0f, -0.02f, -102f + i * 9f);
            CreatePrimitive(PrimitiveType.Cube, "Left lane dash", marker.transform,
                new Vector3(-1.36f, 0f, 0f), new Vector3(0.07f, 0.025f, 3.1f), lightLeafMaterial);
            CreatePrimitive(PrimitiveType.Cube, "Right lane dash", marker.transform,
                new Vector3(1.36f, 0f, 0f), new Vector3(0.07f, 0.025f, 3.1f), lightLeafMaterial);
            laneMarkers.Add(marker.transform);
        }

        for (int i = 0; i < 42; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            int column = i / 2;
            float x = side * Random.Range(7.2f, 14.8f);
            float z = -114f + column * 5.7f + Random.Range(-2.6f, 2.6f);
            bool palm = Random.value < 0.3f;
            GameObject tree = new GameObject(palm ? "Palm tree" : "Jungle tree");
            tree.transform.position = new Vector3(x, 0f, z);
            tree.transform.localScale = Vector3.one * Random.Range(0.72f, 1.35f);
            float trunkHeight = palm ? Random.Range(6.1f, 8.2f) : Random.Range(4.4f, 6.2f);
            CreatePrimitive(PrimitiveType.Cylinder, "Trunk", tree.transform,
                new Vector3(0f, trunkHeight * 0.5f, 0f),
                new Vector3(palm ? 0.45f : 0.75f, trunkHeight * 0.5f, palm ? 0.45f : 0.75f), barkMaterial);

            if (palm)
            {
                for (int frond = 0; frond < 7; frond++)
                {
                    float angle = frond / 7f * Mathf.PI * 2f;
                    GameObject leaf = CreatePrimitive(PrimitiveType.Cube, "Palm frond", tree.transform,
                        new Vector3(Mathf.Cos(angle) * 1.15f, trunkHeight + 0.35f, Mathf.Sin(angle) * 1.15f),
                        new Vector3(0.28f, 0.16f, Random.Range(2.2f, 3.1f)),
                        frond % 2 == 0 ? lightLeafMaterial : leafMaterial);
                    leaf.transform.rotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg, -28f);
                }
            }
            else
            {
                for (int leaf = 0; leaf < 5; leaf++)
                {
                    GameObject crown = CreatePrimitive(PrimitiveType.Sphere, "Tree canopy", tree.transform,
                        new Vector3(Random.Range(-1.3f, 1.3f), trunkHeight * 0.78f + Random.Range(-0.3f, 0.75f),
                            Random.Range(-0.8f, 0.8f)),
                        new Vector3(Random.Range(2f, 3.3f), Random.Range(1.7f, 2.8f), Random.Range(1.7f, 2.8f)),
                        leaf % 2 == 0 ? lightLeafMaterial : leafMaterial);
                    crown.transform.rotation = Random.rotation;
                }
            }
            trees.Add(tree.transform);
        }

        for (int i = 0; i < 25; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            GameObject bush = new GameObject("Jungle undergrowth");
            bush.transform.position = new Vector3(side * Random.Range(5.3f, 7.1f), -0.04f, -112f + i * 8.7f);
            for (int leaf = 0; leaf < 4; leaf++)
            {
                CreatePrimitive(PrimitiveType.Sphere, "Leaf tuft", bush.transform,
                    new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.18f, 0.55f), Random.Range(-0.35f, 0.35f)),
                    Vector3.one * Random.Range(0.65f, 1.15f), leaf % 2 == 0 ? lightLeafMaterial : leafMaterial);
            }
            trees.Add(bush.transform);
        }
    }

    private void CreateRunner()
    {
        runner = new GameObject("Runner").transform;
        runner.position = new Vector3(0f, 0f, PlayerZ);
        runnerVisual = new GameObject("Runner visual").transform;
        runnerVisual.SetParent(runner, false);

        Material suit = MakeMaterial(new Color(0.91f, 0.42f, 0.19f), 0f, 0.72f);
        Material darkSuit = MakeMaterial(new Color(0.14f, 0.2f, 0.17f), 0f, 0.82f);
        Material skin = MakeMaterial(new Color(0.78f, 0.51f, 0.34f), 0f, 0.82f);
        Material boot = MakeMaterial(new Color(0.24f, 0.19f, 0.14f), 0f, 0.9f);

        CreatePrimitive(PrimitiveType.Capsule, "Explorer jacket", runnerVisual,
            new Vector3(0f, 1.16f, 0f), new Vector3(0.66f, 1.12f, 0.54f), suit);
        CreatePrimitive(PrimitiveType.Cube, "Backpack", runnerVisual,
            new Vector3(0f, 1.22f, 0.31f), new Vector3(0.48f, 0.62f, 0.22f), darkSuit);
        CreatePrimitive(PrimitiveType.Sphere, "Head", runnerVisual,
            new Vector3(0f, 1.92f, 0f), new Vector3(0.52f, 0.56f, 0.5f), skin);
        CreatePrimitive(PrimitiveType.Sphere, "Explorer cap", runnerVisual,
            new Vector3(0f, 2.13f, 0f), new Vector3(0.6f, 0.22f, 0.58f), darkSuit);

        for (int side = -1; side <= 1; side += 2)
        {
            Transform leg = new GameObject("Runner leg").transform;
            leg.SetParent(runnerVisual, false);
            leg.localPosition = new Vector3(side * 0.2f, 0.73f, 0f);
            CreatePrimitive(PrimitiveType.Capsule, "Leg", leg,
                new Vector3(0f, -0.25f, 0f), new Vector3(0.24f, 0.58f, 0.24f), darkSuit);
            CreatePrimitive(PrimitiveType.Cube, "Boot", leg,
                new Vector3(0f, -0.52f, 0.08f), new Vector3(0.27f, 0.15f, 0.4f), boot);
            runnerLegs.Add(leg);

            Transform arm = new GameObject("Runner arm").transform;
            arm.SetParent(runnerVisual, false);
            arm.localPosition = new Vector3(side * 0.43f, 1.44f, 0f);
            CreatePrimitive(PrimitiveType.Capsule, "Sleeve", arm,
                new Vector3(0f, -0.23f, 0f), new Vector3(0.2f, 0.55f, 0.2f), suit);
        }

        shield = CreatePrimitive(PrimitiveType.Sphere, "Shield", runner,
            new Vector3(0f, 1.05f, 0f), new Vector3(2.6f, 2.8f, 2.6f), MakeShieldMaterial());
        shield.SetParent(runner, false);
        shield.gameObject.SetActive(false);
    }

    private Material MakeShieldMaterial()
    {
        Material material = new Material(Shader.Find("Legacy Shaders/Transparent/Diffuse"));
        material.color = new Color(0.43f, 0.72f, 1f, 0.22f);
        return material;
    }

    private GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 position,
        Vector3 scale, Material material)
    {
        GameObject item = GameObject.CreatePrimitive(type);
        item.name = name;
        item.transform.SetParent(parent, false);
        item.transform.localPosition = position;
        item.transform.localScale = scale;
        Renderer renderer = item.GetComponent<Renderer>();
        if (renderer != null && material != null)
            renderer.sharedMaterial = material;
        Collider collider = item.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
        return item;
    }

    private void PopulatePreview()
    {
        SpawnRow(-36f);
        SpawnRow(-51f);
        SpawnRow(-66f);
        SpawnRow(-81f);
    }

    private void StartRun()
    {
        ClearEntities();
        powerups.Clear();
        distance = 0f;
        coinCount = 0f;
        score = 0f;
        pickupPoints = 0f;
        scoreAccumulator = 0f;
        speed = StartSpeed;
        laneIndex = 1;
        runnerX = 0f;
        jumpHeight = 0f;
        verticalSpeed = 0f;
        grounded = true;
        previousFreeLane = 1;
        previousRowWasSingle = false;
        spawnHeadZ = -91f;
        runner.position = new Vector3(0f, 0f, PlayerZ);
        for (int row = 0; row < 4; row++)
        {
            SpawnRow(-35f - row * RowSpacing);
        }
        mode = GameMode.Playing;
    }

    private void MoveLeft()
    {
        laneIndex = Mathf.Max(0, laneIndex - 1);
    }

    private void MoveRight()
    {
        laneIndex = Mathf.Min(2, laneIndex + 1);
    }

    private void Jump()
    {
        if (!grounded || HasPowerup("fly"))
            return;
        grounded = false;
        verticalSpeed = 9.3f;
    }

    private void AnimateRunner()
    {
        if (!HasPowerup("fly") && !grounded)
        {
            verticalSpeed -= 24f * Time.deltaTime;
            jumpHeight = Mathf.Max(0f, jumpHeight + verticalSpeed * Time.deltaTime);
            if (jumpHeight <= 0f)
            {
                grounded = true;
                verticalSpeed = 0f;
            }
        }
        else if (HasPowerup("fly"))
        {
            grounded = true;
            jumpHeight = 0f;
            verticalSpeed = 0f;
        }

        float swing = grounded ? Mathf.Sin(runClock * 13f) * 28f : -15f;
        for (int i = 0; i < runnerLegs.Count; i++)
            runnerLegs[i].localRotation = Quaternion.Euler(i == 0 ? swing : -swing, 0f, 0f);
        if (shield != null)
        {
            shield.gameObject.SetActive(HasPowerup("shield"));
            shield.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 5f) * 0.035f);
        }
    }

    private void UpdateWorld(float currentSpeed, float dt)
    {
        for (int i = 0; i < trees.Count; i++)
        {
            Vector3 position = trees[i].position;
            position.z += currentSpeed * dt * 0.46f;
            if (position.z > 22f) position.z -= 142f;
            trees[i].position = position;
        }
        for (int i = 0; i < laneMarkers.Count; i++)
        {
            Vector3 position = laneMarkers[i].position;
            position.z += currentSpeed * dt;
            if (position.z > 14f) position.z -= 126f;
            laneMarkers[i].position = position;
        }
    }

    private void SpawnRow(float z)
    {
        bool[] blocked = new bool[3];
        if (Random.value < 0.67f)
        {
            int first = Random.Range(0, 3);
            blocked[first] = true;
            if (Random.value < 0.34f)
                blocked[(first + (Random.value < 0.5f ? 1 : 2)) % 3] = true;
        }

        List<int> freeLanes = new List<int>();
        for (int lane = 0; lane < 3; lane++)
            if (!blocked[lane]) freeLanes.Add(lane);
        if (freeLanes.Count == 0)
        {
            blocked[Random.Range(0, 3)] = false;
            for (int lane = 0; lane < 3; lane++)
                if (!blocked[lane]) freeLanes.Add(lane);
        }

        if (previousRowWasSingle && freeLanes.Count == 1 &&
            Mathf.Abs(freeLanes[0] - previousFreeLane) > 1)
        {
            int safeLane = Mathf.Clamp(previousFreeLane + (freeLanes[0] > previousFreeLane ? 1 : -1), 0, 2);
            blocked[0] = blocked[1] = blocked[2] = true;
            blocked[safeLane] = false;
            freeLanes.Clear();
            freeLanes.Add(safeLane);
        }

        int freeLane = freeLanes[Random.Range(0, freeLanes.Count)];
        for (int lane = 0; lane < 3; lane++)
            if (blocked[lane]) SpawnObstacle(lane, z + Random.Range(-0.35f, 0.35f));

        if (Random.value < 0.84f)
            for (int coin = 0; coin < 5; coin++)
                SpawnPickup(EntityKind.Coin, freeLane, z - 2.8f + coin * 1.35f, 1.35f);
        if (Random.value < 0.15f)
            SpawnPickup(EntityKind.Gem, freeLane, z - Random.Range(5f, 8f), 1.72f);
        if (Random.value < 0.22f)
            SpawnPickup(EntityKind.Powerup, freeLanes[Random.Range(0, freeLanes.Count)],
                z - Random.Range(8f, 10f), 1.8f, PowerupIds[Random.Range(0, PowerupIds.Length)]);

        previousFreeLane = freeLane;
        previousRowWasSingle = freeLanes.Count == 1;
    }

    private void SpawnObstacle(int lane, float z)
    {
        int kind = Random.Range(0, 3);
        GameObject root = new GameObject(kind == 0 ? "Fallen log" : kind == 1 ? "Trail rock" : "Tree stump");
        root.transform.position = new Vector3(LaneX[lane], 0f, z);
        float height = 1.05f;
        if (kind == 0)
        {
            CreatePrimitive(PrimitiveType.Cylinder, "Log", root.transform,
                new Vector3(0f, 0.48f, 0f), new Vector3(2.35f, 0.48f, 0.48f), obstacleMaterial);
        }
        else if (kind == 1)
        {
            height = 1.18f;
            GameObject rock = CreatePrimitive(PrimitiveType.Sphere, "Rock", root.transform,
                new Vector3(0f, 0.58f, 0f), new Vector3(1.65f, 1.18f, 1.45f), rockMaterial);
            rock.transform.rotation = Random.rotation;
        }
        else
        {
            height = 1.36f;
            CreatePrimitive(PrimitiveType.Cylinder, "Stump", root.transform,
                new Vector3(0f, 0.66f, 0f), new Vector3(1.05f, 0.66f, 1.05f), obstacleMaterial);
        }
        entities.Add(new Entity { root = root, kind = EntityKind.Obstacle, lane = lane, z = z, obstacleHeight = height });
    }

    private void SpawnPickup(EntityKind kind, int lane, float z, float y, string powerup = null)
    {
        GameObject root = new GameObject(kind.ToString());
        root.transform.position = new Vector3(LaneX[lane], y, z);
        if (kind == EntityKind.Coin)
        {
            CreatePrimitive(PrimitiveType.Cylinder, "Gold coin", root.transform, Vector3.zero,
                new Vector3(0.62f, 0.12f, 0.62f), coinMaterial).transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
        else if (kind == EntityKind.Gem)
        {
            GameObject gem = CreatePrimitive(PrimitiveType.Sphere, "Jungle gem", root.transform,
                Vector3.zero, new Vector3(0.62f, 0.9f, 0.62f), gemMaterial);
            gem.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
        }
        else
        {
            Color color = powerup == "shield" ? new Color(0.5f, 0.75f, 1f) :
                powerup == "speed" ? new Color(1f, 0.55f, 0.24f) :
                powerup == "fly" ? new Color(0.76f, 0.52f, 1f) :
                powerup == "double" ? new Color(1f, 0.87f, 0.3f) : new Color(0.35f, 0.87f, 0.94f);
            powerupMaterial.color = color;
            CreatePrimitive(PrimitiveType.Sphere, "Power-up", root.transform, Vector3.zero,
                Vector3.one * 0.78f, powerupMaterial);
        }
        entities.Add(new Entity
        {
            root = root, kind = kind, lane = lane, z = z, powerup = powerup,
            phase = Random.Range(0f, Mathf.PI * 2f)
        });
    }

    private void UpdateEntities(float currentSpeed, float dt)
    {
        for (int i = entities.Count - 1; i >= 0; i--)
        {
            Entity entity = entities[i];
            if (entity.root == null)
            {
                entities.RemoveAt(i);
                continue;
            }
            entity.z += currentSpeed * dt;
            entity.root.transform.position = new Vector3(LaneX[entity.lane],
                entity.kind == EntityKind.Obstacle ? 0f : 1.35f + Mathf.Sin(Time.time * 4f + entity.phase) * 0.14f,
                entity.z);
            entity.root.transform.Rotate(0f, 90f * dt, 0f);

            if (!entity.spent && Mathf.Abs(entity.z - PlayerZ) < 0.95f &&
                (entity.lane == laneIndex || (entity.kind == EntityKind.Coin && HasPowerup("magnet"))))
            {
                if (entity.kind == EntityKind.Obstacle)
                {
                    if (jumpHeight < entity.obstacleHeight && !HasPowerup("fly"))
                    {
                        if (HasPowerup("shield")) powerups.Remove("shield");
                        else
                        {
                            mode = GameMode.GameOver;
                            continue;
                        }
                    }
                }
                else
                {
                    entity.spent = true;
                    if (entity.kind == EntityKind.Coin)
                    {
                        coinCount += 1f;
                        pickupPoints += 5f;
                    }
                    else if (entity.kind == EntityKind.Gem)
                    {
                        pickupPoints += 25f;
                    }
                    else
                    {
                        float duration = entity.powerup == "double" ? 10f :
                            entity.powerup == "speed" ? 7f : entity.powerup == "shield" ? 9f : 8f;
                        powerups[entity.powerup] = duration;
                    }
                    Destroy(entity.root);
                }
            }

            if (entity.z > 14f || entity.spent)
            {
                if (entity.root != null) Destroy(entity.root);
                entities.RemoveAt(i);
            }
        }
    }

    private void UpdatePowerups(float dt)
    {
        List<string> expired = null;
        List<string> activeIds = new List<string>(powerups.Keys);
        for (int i = 0; i < activeIds.Count; i++)
        {
            string id = activeIds[i];
            float remaining = powerups[id] - dt;
            if (remaining <= 0f)
            {
                if (expired == null) expired = new List<string>();
                expired.Add(id);
            }
            else
            {
                powerups[id] = remaining;
            }
        }
        if (expired != null)
            for (int i = 0; i < expired.Count; i++) powerups.Remove(expired[i]);
    }

    private bool HasPowerup(string id)
    {
        return powerups.ContainsKey(id);
    }

    private void ClearEntities()
    {
        for (int i = 0; i < entities.Count; i++)
            if (entities[i].root != null) Destroy(entities[i].root);
        entities.Clear();
    }

    private void OnGUI()
    {
        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1280f, Screen.height / 720f, 1f));
        GUI.depth = 0;

        if (mode == GameMode.Playing)
        {
            DrawPlayingHud();
        }
        else
        {
            DrawModal();
        }
        GUI.matrix = oldMatrix;
    }

    private void DrawPlayingHud()
    {
        GUI.color = new Color(0.08f, 0.14f, 0.1f, 0.88f);
        GUI.Box(new Rect(28f, 24f, 455f, 94f), GUIContent.none);
        GUI.color = Color.white;
        GUI.Label(new Rect(48f, 38f, 155f, 30f), "SKOR", MakeLabel(17, new Color(0.78f, 0.86f, 0.72f)));
        GUI.Label(new Rect(48f, 61f, 155f, 43f), Mathf.FloorToInt(score).ToString("N0"), MakeLabel(30, Color.white, true));
        GUI.Label(new Rect(220f, 40f, 105f, 48f), "◉  " + Mathf.FloorToInt(coinCount).ToString("N0"),
            MakeLabel(23, new Color(1f, 0.79f, 0.3f), true));
        GUI.Label(new Rect(345f, 38f, 120f, 30f), "JARAK", MakeLabel(17, new Color(0.78f, 0.86f, 0.72f)));
        GUI.Label(new Rect(345f, 64f, 120f, 38f), Mathf.FloorToInt(distance) + " m", MakeLabel(23, Color.white, true));

        GUI.color = new Color(0.12f, 0.24f, 0.15f, 0.92f);
        if (GUI.Button(new Rect(1165f, 27f, 88f, 62f), "Ⅱ", MakeButton(30)))
            mode = GameMode.Paused;
        GUI.color = Color.white;

        if (powerups.Count > 0)
        {
            string status = "";
            foreach (KeyValuePair<string, float> powerup in powerups)
                status += powerup.Key.ToUpper() + "  " + Mathf.CeilToInt(powerup.Value) + "s     ";
            GUI.Label(new Rect(465f, 34f, 620f, 38f), status, MakeLabel(16, Color.white, true));
        }

        GUI.Label(new Rect(400f, 659f, 480f, 34f), "←  → PINDAH JALUR      ↑ / SPASI LOMPAT      ESC JEDA",
            MakeLabel(16, new Color(1f, 1f, 1f, 0.86f), true));
        GUI.color = new Color(0.09f, 0.19f, 0.14f, 0.85f);
        if (GUI.Button(new Rect(30f, 594f, 105f, 90f), "◀", MakeButton(34))) MoveLeft();
        if (GUI.Button(new Rect(1145f, 594f, 105f, 90f), "▶", MakeButton(34))) MoveRight();
        GUI.color = new Color(0.68f, 0.39f, 0.16f, 0.94f);
        if (GUI.Button(new Rect(1010f, 594f, 115f, 90f), "LOMPAT", MakeButton(19))) Jump();
        GUI.color = Color.white;
    }

    private void DrawModal()
    {
        Rect panel = new Rect(410f, 118f, 460f, 484f);
        GUI.color = new Color(0.055f, 0.105f, 0.075f, 0.94f);
        GUI.Box(panel, GUIContent.none);
        GUI.color = Color.white;

        if (mode == GameMode.Menu)
        {
            GUI.Label(new Rect(450f, 153f, 380f, 32f), "WILDLANDS RUN", MakeLabel(19, new Color(0.73f, 0.88f, 0.57f), true, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(440f, 207f, 400f, 112f), "JUNGLE\nDASH", MakeLabel(54, new Color(1f, 0.91f, 0.69f), true, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(455f, 326f, 370f, 56f), "Lari sejauh mungkin. Kumpulkan koin.\nHindari rintangan di hutan.",
                MakeLabel(19, Color.white, false, TextAnchor.MiddleCenter));
            GUI.color = new Color(0.65f, 0.36f, 0.14f, 1f);
            if (GUI.Button(new Rect(505f, 405f, 270f, 66f), "MULAI BERLARI", MakeButton(23))) StartRun();
            GUI.color = Color.white;
            GUI.Label(new Rect(475f, 500f, 330f, 35f), "REKOR TERBAIK     " + best.ToString("N0"),
                MakeLabel(17, new Color(1f, 0.82f, 0.37f), true, TextAnchor.MiddleCenter));
        }
        else if (mode == GameMode.Paused)
        {
            GUI.Label(new Rect(450f, 195f, 380f, 80f), "LARI DIJEDA", MakeLabel(42, new Color(1f, 0.91f, 0.69f), true, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(465f, 285f, 350f, 42f), "Skor  " + Mathf.FloorToInt(score).ToString("N0") +
                "      Koin  " + Mathf.FloorToInt(coinCount).ToString("N0"),
                MakeLabel(20, Color.white, true, TextAnchor.MiddleCenter));
            GUI.color = new Color(0.65f, 0.36f, 0.14f, 1f);
            if (GUI.Button(new Rect(505f, 365f, 270f, 60f), "LANJUT", MakeButton(22))) mode = GameMode.Playing;
            GUI.color = new Color(0.14f, 0.28f, 0.19f, 1f);
            if (GUI.Button(new Rect(505f, 440f, 270f, 56f), "MULAI ULANG", MakeButton(19))) StartRun();
            GUI.color = Color.white;
        }
        else
        {
            GUI.Label(new Rect(450f, 165f, 380f, 40f), "PETUALANGAN SELESAI", MakeLabel(18, new Color(0.73f, 0.88f, 0.57f), true, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(440f, 218f, 400f, 82f), "KENA JEBAKAN!", MakeLabel(39, new Color(1f, 0.76f, 0.48f), true, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(475f, 310f, 330f, 30f), "SKOR AKHIR", MakeLabel(17, new Color(0.78f, 0.86f, 0.72f), false, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(475f, 339f, 330f, 54f), Mathf.FloorToInt(score).ToString("N0"), MakeLabel(34, Color.white, true, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(475f, 397f, 330f, 35f), "Koin " + Mathf.FloorToInt(coinCount).ToString("N0") + "     Rekor " + best.ToString("N0"),
                MakeLabel(17, new Color(1f, 0.82f, 0.37f), true, TextAnchor.MiddleCenter));
            GUI.color = new Color(0.65f, 0.36f, 0.14f, 1f);
            if (GUI.Button(new Rect(505f, 465f, 270f, 60f), "LARI LAGI", MakeButton(22))) StartRun();
            GUI.color = Color.white;
        }
    }

    private GUIStyle MakeLabel(int size, Color color, bool bold = false, TextAnchor alignment = TextAnchor.UpperLeft)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = size;
        style.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        style.normal.textColor = color;
        style.alignment = alignment;
        style.wordWrap = true;
        return style;
    }

    private GUIStyle MakeButton(int size)
    {
        GUIStyle style = new GUIStyle(GUI.skin.button);
        style.fontSize = size;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;
        style.hover.textColor = new Color(1f, 0.92f, 0.72f);
        style.alignment = TextAnchor.MiddleCenter;
        return style;
    }

    private void OnDestroy()
    {
        ClearEntities();
        DestroyRuntimeMaterials();
    }

    private void DestroyRuntimeMaterials()
    {
        Destroy(coinMaterial);
        Destroy(gemMaterial);
        Destroy(powerupMaterial);
        Destroy(obstacleMaterial);
        Destroy(rockMaterial);
        Destroy(barkMaterial);
        Destroy(leafMaterial);
        Destroy(lightLeafMaterial);
    }
}
