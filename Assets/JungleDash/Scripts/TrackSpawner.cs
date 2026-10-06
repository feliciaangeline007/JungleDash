using System.Collections.Generic;
using UnityEngine;

namespace JungleDash
{
    public class TrackSpawner : MonoBehaviour
    {
        private const float SegLen = 24f;
        private const int MaxSegs = 10;

        private float nextZ = -12f;
        private readonly List<GameObject> segs = new List<GameObject>();

        // Materials
        private Material pathMat, grassMat, barkMat, leafMat, darkLeafMat;
        private Material rockMat, dirtMat, stoneMat;
        private Material coinMat, gemMat, puMat;
        private Material dividerMat, skyMat;

        private void Awake() => BuildMaterials();

        // ── Shader helper ──────────────────────────────────────────────────────
        // Tries every known URP shader name then falls back to Standard/Unlit so
        // we NEVER get a pink material regardless of Unity 6 sub-version.
        private static Shader SafeShader()
        {
            string[] names = {
                "Universal Render Pipeline/Lit",
                "Universal Render Pipeline/Simple Lit",
                "Universal Render Pipeline/Unlit",
                "Unlit/Color",
                "Standard",
                "Diffuse"
            };
            foreach (var n in names)
            {
                var s = Shader.Find(n);
                if (s != null) return s;
            }
            return Shader.Find("Hidden/InternalErrorShader"); // absolute last resort
        }

        private static Material M(Color baseColor, float metallic = 0f, float smooth = 0.3f)
        {
            var shader = SafeShader();
            var mat = new Material(shader);

            // URP property names
            if (mat.HasProperty("_BaseColor"))      mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Color"))          mat.SetColor("_Color", baseColor);
            if (mat.HasProperty("_Metallic"))       mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness"))     mat.SetFloat("_Smoothness", smooth);
            if (mat.HasProperty("_Glossiness"))     mat.SetFloat("_Glossiness", smooth);
            return mat;
        }

        private static Material MEmissive(Color baseColor, Color emitColor, float emitIntensity = 1.5f)
        {
            var mat = M(baseColor, 0.6f, 0.8f);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emitColor * emitIntensity);
            }
            return mat;
        }

        private void BuildMaterials()
        {
            // Ground
            pathMat     = M(new Color(0.55f, 0.44f, 0.28f), 0f, 0.2f);   // sandy brown path
            var mudTex  = Resources.Load<Texture2D>("MudRocky");
            if (mudTex != null)
            {
                if (pathMat.HasProperty("_BaseMap"))      pathMat.SetTexture("_BaseMap", mudTex);
                else                                      pathMat.mainTexture = mudTex;
                pathMat.mainTextureScale = new Vector2(2f, 8f);
            }
            grassMat    = M(new Color(0.20f, 0.48f, 0.15f), 0f, 0.15f);  // vibrant jungle green
            dirtMat     = M(new Color(0.32f, 0.22f, 0.12f), 0f, 0.1f);

            // Trees
            barkMat     = M(new Color(0.30f, 0.18f, 0.08f), 0f, 0.1f);
            leafMat     = M(new Color(0.15f, 0.55f, 0.18f), 0f, 0.15f);
            darkLeafMat = M(new Color(0.10f, 0.38f, 0.12f), 0f, 0.12f);

            // Obstacles
            rockMat     = M(new Color(0.40f, 0.42f, 0.38f), 0.05f, 0.2f);
            stoneMat    = M(new Color(0.50f, 0.47f, 0.42f), 0.05f, 0.25f);

            // Collectibles – emissive so they glow even without perfect lighting
            coinMat     = MEmissive(new Color(1f, 0.80f, 0.05f), new Color(1f, 0.65f, 0f), 1.8f);
            gemMat      = MEmissive(new Color(0.70f, 0.10f, 1.00f), new Color(0.5f, 0f, 1f), 2.0f);
            puMat       = MEmissive(new Color(0.05f, 0.90f, 1.00f), new Color(0f, 0.8f, 1f), 2.2f);

            // Lane divider
            dividerMat  = M(new Color(0.90f, 0.85f, 0.60f), 0f, 0.05f);
        }

        // ── Public API ────────────────────────────────────────────────────────
        public void ResetTrack()
        {
            foreach (var s in segs) if (s) Destroy(s);
            segs.Clear();
            nextZ = -12f;
            for (int i = 0; i < MaxSegs; i++) SpawnSeg(i >= 2);
        }

        public void UpdateSpawner(float playerZ)
        {
            while (nextZ < playerZ + MaxSegs * SegLen) SpawnSeg(true);
            while (segs.Count > 0 && segs[0] != null
                   && segs[0].transform.position.z + SegLen < playerZ - 20f)
            {
                Destroy(segs[0]);
                segs.RemoveAt(0);
            }
        }

        // ── Segment ───────────────────────────────────────────────────────────
        private void SpawnSeg(bool withObs)
        {
            var seg = new GameObject("Seg_" + Mathf.RoundToInt(nextZ));
            seg.transform.SetParent(transform, true);
            seg.transform.position = new Vector3(0f, 0f, nextZ);

            BuildGround(seg.transform);
            BuildLaneDividers(seg.transform);
            BuildFoliage(seg.transform);

            if (withObs) SpawnObstaclesPickups(seg.transform);
            else         SpawnCoins(seg.transform, 1, 8f, 5);

            segs.Add(seg);
            nextZ += SegLen;
        }

        private void BuildGround(Transform seg)
        {
            // Main dirt path (narrower, defined edges)
            var path = Prim(PrimitiveType.Cube, seg,
                new Vector3(0f, -0.05f, SegLen * 0.5f),
                new Vector3(9f, 0.15f, SegLen), pathMat, false);
            path.tag = "Ground";

            // Raised edges / curbs on path sides
            for (int s = -1; s <= 1; s += 2)
            {
                var curb = Prim(PrimitiveType.Cube, seg,
                    new Vector3(s * 4.65f, 0.04f, SegLen * 0.5f),
                    new Vector3(0.3f, 0.18f, SegLen), stoneMat, false);
                curb.tag = "Ground";
            }

            // Wide flanking grass floor
            var grass = Prim(PrimitiveType.Cube, seg,
                new Vector3(0f, -0.2f, SegLen * 0.5f),
                new Vector3(80f, 0.25f, SegLen), grassMat, false);
            grass.tag = "Ground";

            // Dirt strip transitions between path and grass
            for (int s = -1; s <= 1; s += 2)
            {
                Prim(PrimitiveType.Cube, seg,
                    new Vector3(s * 6.5f, -0.12f, SegLen * 0.5f),
                    new Vector3(3f, 0.15f, SegLen), dirtMat, false);
            }
        }

        private void BuildLaneDividers(Transform seg)
        {
            // Dashed lines at x = ±2.6
            int dashCount = 6;
            for (int d = 0; d < dashCount; d++)
            {
                float z = 2f + d * (SegLen / dashCount);
                for (int s = -1; s <= 1; s += 2)
                {
                    Prim(PrimitiveType.Cube, seg,
                        new Vector3(s * 2.6f, 0.02f, z),
                        new Vector3(0.12f, 0.04f, 1.8f), dividerMat, false);
                }
            }
        }

        private void BuildFoliage(Transform seg)
        {
            // Tall trees on both sides
            int treeCount = Random.Range(5, 9);
            for (int i = 0; i < treeCount; i++)
            {
                float side = (i % 2 == 0) ? -1f : 1f;
                float x = side * Random.Range(7f, 18f);
                float z = Random.Range(2f, SegLen - 2f);
                float roll = Random.value;
                if (roll < 0.45f)      PalmTree(seg, new Vector3(x, 0f, z));
                else if (roll < 0.75f) BroadTree(seg, new Vector3(x, 0f, z));
                else                   BambooCluster(seg, new Vector3(x, 0f, z));
            }

            // Dense bushes close to the path
            int bushCount = Random.Range(3, 6);
            for (int b = 0; b < bushCount; b++)
            {
                float side = (b % 2 == 0) ? -1f : 1f;
                float x = side * Random.Range(5.2f, 7.5f);
                float z = Random.Range(3f, SegLen - 3f);
                Bush(seg, new Vector3(x, 0f, z));
            }

            // Decorative mossy rocks on sides
            if (Random.value < 0.6f)
            {
                float side = Random.value < 0.5f ? -1f : 1f;
                float x = side * Random.Range(5.5f, 8f);
                float z = Random.Range(4f, SegLen - 4f);
                DecoRock(seg, new Vector3(x, 0f, z));
            }
        }

        // ── Trees ─────────────────────────────────────────────────────────────
        private void PalmTree(Transform seg, Vector3 pos)
        {
            var t = new GameObject("Palm");
            t.transform.SetParent(seg, false);
            t.transform.localPosition = pos;
            float scale = Random.Range(0.9f, 1.4f);
            float h = Random.Range(6f, 9f) * scale;

            // Slightly curved trunk (stack of cylinders)
            int segments = 4;
            for (int i = 0; i < segments; i++)
            {
                float y0 = i * (h / segments);
                float lean = i * 0.08f * scale;
                var seg2 = Prim(PrimitiveType.Cylinder, t.transform,
                    new Vector3(lean, y0 + h / segments * 0.5f, 0f),
                    new Vector3(0.38f * scale, h / segments * 0.5f, 0.38f * scale), barkMat);
            }

            // Crown of fronds
            int frondCount = 8;
            for (int f = 0; f < frondCount; f++)
            {
                float angle = f * (360f / frondCount);
                float tilt = Random.Range(18f, 30f);
                var frond = Prim(PrimitiveType.Cube, t.transform,
                    new Vector3(0f, h, 0f),
                    new Vector3(0.28f * scale, 0.07f, 2.8f * scale), leafMat);
                frond.transform.localRotation = Quaternion.Euler(tilt, angle, 0f);
            }

            // Small sphere at crown center
            Prim(PrimitiveType.Sphere, t.transform,
                new Vector3(0f, h + 0.15f, 0f),
                Vector3.one * 0.5f * scale, darkLeafMat);
        }

        private void BroadTree(Transform seg, Vector3 pos)
        {
            var t = new GameObject("BroadTree");
            t.transform.SetParent(seg, false);
            t.transform.localPosition = pos;
            float scale = Random.Range(0.85f, 1.3f);
            float h = Random.Range(5f, 8f) * scale;

            Prim(PrimitiveType.Cylinder, t.transform,
                new Vector3(0f, h * 0.5f, 0f),
                new Vector3(0.55f * scale, h * 0.5f, 0.55f * scale), barkMat);

            // Layered canopy (3 spheres at different heights)
            float[] canopyY   = { h * 0.72f, h * 0.90f, h * 1.05f };
            float[] canopyR   = { 3.0f, 2.4f, 1.6f };
            bool[] useDark    = { true, false, true };
            for (int i = 0; i < 3; i++)
            {
                Prim(PrimitiveType.Sphere, t.transform,
                    new Vector3(Random.Range(-0.3f, 0.3f) * scale, canopyY[i], Random.Range(-0.3f, 0.3f) * scale),
                    new Vector3(canopyR[i] * scale, canopyR[i] * 0.85f * scale, canopyR[i] * scale),
                    useDark[i] ? darkLeafMat : leafMat);
            }
        }

        private void BambooCluster(Transform seg, Vector3 pos)
        {
            int count = Random.Range(3, 6);
            for (int i = 0; i < count; i++)
            {
                var t = new GameObject("Bamboo");
                t.transform.SetParent(seg, false);
                float ox = Random.Range(-0.6f, 0.6f);
                float oz = Random.Range(-0.6f, 0.6f);
                t.transform.localPosition = pos + new Vector3(ox, 0f, oz);
                float h = Random.Range(5f, 8f);

                // Bamboo segments
                int nodeCount = Mathf.RoundToInt(h / 1.2f);
                for (int n = 0; n < nodeCount; n++)
                {
                    float y = n * 1.2f + 0.6f;
                    Prim(PrimitiveType.Cylinder, t.transform,
                        new Vector3(0f, y, 0f),
                        new Vector3(0.15f, 0.62f, 0.15f),
                        n % 2 == 0 ? leafMat : darkLeafMat);
                }
                // Small leaves at top
                Prim(PrimitiveType.Sphere, t.transform,
                    new Vector3(0f, h + 0.4f, 0f),
                    new Vector3(0.8f, 0.5f, 0.8f), leafMat);
            }
        }

        private void Bush(Transform seg, Vector3 pos)
        {
            // Multi-sphere bush for fullness
            int spheres = Random.Range(2, 4);
            for (int i = 0; i < spheres; i++)
            {
                float ox = Random.Range(-0.5f, 0.5f);
                float oy = Random.Range(0.25f, 0.55f);
                float s  = Random.Range(0.8f, 1.4f);
                Prim(PrimitiveType.Sphere, seg,
                    pos + new Vector3(ox, oy, Random.Range(-0.4f, 0.4f)),
                    new Vector3(s * 1.3f, s * 0.85f, s * 1.3f),
                    Random.value < 0.5f ? leafMat : darkLeafMat, false);
            }
        }

        private void DecoRock(Transform seg, Vector3 pos)
        {
            int count = Random.Range(1, 4);
            for (int i = 0; i < count; i++)
            {
                float s = Random.Range(0.4f, 1.1f);
                var r = Prim(PrimitiveType.Sphere, seg,
                    pos + new Vector3(Random.Range(-0.6f, 0.6f), s * 0.4f, Random.Range(-0.5f, 0.5f)),
                    new Vector3(s * 1.3f, s * 0.8f, s * 1.1f), rockMat, false);
                r.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }
        }

        // ── Obstacle / pickup spawning ─────────────────────────────────────────
        private void SpawnObstaclesPickups(Transform seg)
        {
            int pattern   = Random.Range(0, 3);
            float obsZ    = Random.Range(8f, 16f);
            int safeLane  = Random.Range(0, 3);

            if (pattern == 0)
            {
                SpawnObstacle(seg, (safeLane + 1) % 3, obsZ);
                SpawnCoins(seg, safeLane, obsZ - 5f, 5);
            }
            else if (pattern == 1)
            {
                for (int l = 0; l < 3; l++) if (l != safeLane) SpawnObstacle(seg, l, obsZ);
                SpawnCoins(seg, safeLane, obsZ - 4f, 4);
            }
            else
            {
                SpawnLog(seg, safeLane, obsZ);
                SpawnArcCoins(seg, safeLane, obsZ);
            }

            if (Random.value < 0.32f)
            {
                float pz = obsZ + 8f;
                int   pl = Random.Range(0, 3);
                if (Random.value < 0.45f) SpawnPowerUp(seg, pl, pz);
                else                      SpawnGem(seg, pl, pz);
            }
        }

        private void SpawnObstacle(Transform seg, int lane, float z)
        {
            float x = PlayerRunner.LaneX[lane];
            int   t = Random.Range(0, 3);

            if (t == 0) // Boulder
            {
                var go = Prim(PrimitiveType.Sphere, seg,
                    new Vector3(x, 0.9f, z), new Vector3(1.9f, 1.8f, 1.9f), rockMat);
                go.GetComponent<Collider>().isTrigger = true;
                go.AddComponent<Obstacle>().Initialize(ObstacleType.Rock, 1.8f);
            }
            else if (t == 1) // Stone pillar
            {
                var go = Prim(PrimitiveType.Cylinder, seg,
                    new Vector3(x, 1.6f, z), new Vector3(1.1f, 1.6f, 1.1f), stoneMat);
                go.GetComponent<Collider>().isTrigger = true;
                go.AddComponent<Obstacle>().Initialize(ObstacleType.AncientPillar, 3.2f);

                // Pillar cap
                Prim(PrimitiveType.Cube, seg,
                    new Vector3(x, 3.3f, z), new Vector3(1.5f, 0.25f, 1.5f), stoneMat, false);
            }
            else // Tree stump
            {
                var go = Prim(PrimitiveType.Cylinder, seg,
                    new Vector3(x, 0.45f, z), new Vector3(1.3f, 0.45f, 1.3f), barkMat);
                go.GetComponent<Collider>().isTrigger = true;
                go.AddComponent<Obstacle>().Initialize(ObstacleType.LowBarrier, 0.9f);

                // Ring on top of stump
                Prim(PrimitiveType.Cylinder, seg,
                    new Vector3(x, 0.92f, z), new Vector3(1.4f, 0.06f, 1.4f), rockMat, false);
            }
        }

        private void SpawnLog(Transform seg, int lane, float z)
        {
            float x = PlayerRunner.LaneX[lane];
            var go = Prim(PrimitiveType.Cylinder, seg,
                new Vector3(x, 0.38f, z), new Vector3(0.6f, 1.4f, 0.6f), barkMat);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            go.GetComponent<Collider>().isTrigger = true;
            go.AddComponent<Obstacle>().Initialize(ObstacleType.FallenLog, 0.76f);

            // Log end caps
            for (int s = -1; s <= 1; s += 2)
            {
                Prim(PrimitiveType.Cylinder, seg,
                    new Vector3(x + s * 1.5f, 0.38f, z),
                    new Vector3(0.64f, 0.08f, 0.64f), rockMat, false);
            }
        }

        private void SpawnCoins(Transform seg, int lane, float startZ, int count)
        {
            float x = PlayerRunner.LaneX[lane];
            for (int i = 0; i < count; i++)
                AddCoin(seg, new Vector3(x, 0.85f, startZ + i * 2.0f));
        }

        private void SpawnArcCoins(Transform seg, int lane, float cz)
        {
            float x = PlayerRunner.LaneX[lane];
            float[] hy = { 0.85f, 1.2f, 1.7f, 2.1f, 2.3f, 2.1f, 1.7f, 1.2f, 0.85f };
            for (int i = 0; i < hy.Length; i++)
                AddCoin(seg, new Vector3(x, hy[i], cz - 4f + i * 1.0f));
        }

        private void SpawnGem(Transform seg, int lane, float z)
        {
            float x = PlayerRunner.LaneX[lane];
            // Diamond shape: sphere + rotated cube
            var go = Prim(PrimitiveType.Sphere, seg,
                new Vector3(x, 0.85f, z), Vector3.one * 0.58f, gemMat);
            go.GetComponent<Collider>().isTrigger = true;
            var c = go.AddComponent<Collectible>();
            c.Type = CollectibleType.Gem;

            // Sparkle ring
            var ring = Prim(PrimitiveType.Cylinder, seg,
                new Vector3(x, 0.85f, z), new Vector3(1.1f, 0.06f, 1.1f), gemMat, false);
        }

        private void SpawnPowerUp(Transform seg, int lane, float z)
        {
            float x = PlayerRunner.LaneX[lane];
            // Star-like shape: cube rotated 45°
            var go = Prim(PrimitiveType.Cube, seg,
                new Vector3(x, 0.85f, z), Vector3.one * 0.72f, puMat);
            go.transform.localRotation = Quaternion.Euler(35f, 45f, 0f);
            go.GetComponent<Collider>().isTrigger = true;
            var c = go.AddComponent<Collectible>();
            c.Type = CollectibleType.PowerUp;

            PowerUpType[] types = {
                PowerUpType.Magnet, PowerUpType.Shield,
                PowerUpType.SpeedBoost, PowerUpType.DoubleScore, PowerUpType.Fly
            };
            c.PowerUpVariant = types[Random.Range(0, types.Length)];

            // Halo ring
            Prim(PrimitiveType.Cylinder, seg,
                new Vector3(x, 0.85f, z), new Vector3(1.3f, 0.07f, 1.3f), puMat, false);
        }

        private void AddCoin(Transform seg, Vector3 localPos)
        {
            // Coin = flat cylinder (like a real coin)
            var go = Prim(PrimitiveType.Cylinder, seg,
                localPos, new Vector3(0.48f, 0.07f, 0.48f), coinMat);
            go.GetComponent<Collider>().isTrigger = true;
            go.AddComponent<Collectible>().Type = CollectibleType.Coin;
        }

        // ── Primitive helper ──────────────────────────────────────────────────
        private static GameObject Prim(
            PrimitiveType type, Transform parent,
            Vector3 localPos, Vector3 scale, Material mat,
            bool keepCollider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!keepCollider) Destroy(go.GetComponent<Collider>());
            return go;
        }
    }
}
