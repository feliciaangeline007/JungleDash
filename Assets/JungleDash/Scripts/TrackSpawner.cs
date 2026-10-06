using System.Collections.Generic;
using UnityEngine;

namespace JungleDash
{
    public class TrackSpawner : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────────
        private const float SegLen  = 24f;
        private const int   MaxSegs = 10;

        // ── State ─────────────────────────────────────────────────────────────
        private float nextZ = -12f;
        private readonly List<GameObject> segs = new List<GameObject>();

        // ── Materials ─────────────────────────────────────────────────────────
        private Material pathMat, grassMat, barkMat, leafMat, darkLeafMat;
        private Material rockMat, dirtMat, stoneMat;
        private Material coinMat, gemMat, puMat, dividerMat;

        private void Awake() => BuildMaterials();

        // ── Shader helper ─────────────────────────────────────────────────────
        private static Shader SafeShader()
        {
            string[] n = {
                "Universal Render Pipeline/Lit",
                "Universal Render Pipeline/Simple Lit",
                "Universal Render Pipeline/Unlit",
                "Unlit/Color",
                "Standard",
                "Diffuse"
            };
            foreach (var name in n) { var s = Shader.Find(name); if (s != null) return s; }
            return null;
        }

        private static Material M(Color c, float metallic = 0f, float smooth = 0.3f)
        {
            var sh  = SafeShader() ?? Shader.Find("Standard");
            var mat = new Material(sh);
            mat.color = c;
            if (mat.HasProperty("_BaseColor"))  mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color"))      mat.SetColor("_Color",     c);
            if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic",  metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness",smooth);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness",smooth);
            return mat;
        }

        private static Material MEmit(Color c, Color emit, float intensity = 1.6f)
        {
            var mat = M(c, 0.5f, 0.75f);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emit * intensity);
            }
            return mat;
        }

        private void BuildMaterials()
        {
            pathMat     = M(new Color(0.56f, 0.45f, 0.28f), 0f, 0.18f);
            var mudTex  = Resources.Load<Texture2D>("MudRocky");
            if (mudTex != null)
            {
                if (pathMat.HasProperty("_BaseMap")) pathMat.SetTexture("_BaseMap", mudTex);
                else                                 pathMat.mainTexture = mudTex;
                pathMat.mainTextureScale = new Vector2(2f, 8f);
            }

            grassMat    = M(new Color(0.20f, 0.50f, 0.14f), 0f, 0.12f);
            dirtMat     = M(new Color(0.32f, 0.22f, 0.12f), 0f, 0.08f);
            barkMat     = M(new Color(0.30f, 0.19f, 0.08f), 0f, 0.08f);
            leafMat     = M(new Color(0.14f, 0.54f, 0.17f), 0f, 0.12f);
            darkLeafMat = M(new Color(0.09f, 0.36f, 0.11f), 0f, 0.10f);
            rockMat     = M(new Color(0.40f, 0.42f, 0.38f), 0.05f, 0.20f);
            stoneMat    = M(new Color(0.52f, 0.48f, 0.43f), 0.05f, 0.22f);

            // Collectibles glow so they're visible even without perfect lighting
            coinMat     = MEmit(new Color(1.00f, 0.82f, 0.05f), new Color(1f, 0.60f, 0f), 1.8f);
            gemMat      = MEmit(new Color(0.72f, 0.10f, 1.00f), new Color(0.5f, 0f,  1f), 2.0f);
            puMat       = MEmit(new Color(0.05f, 0.92f, 1.00f), new Color(0f,  0.8f, 1f), 2.2f);
            dividerMat  = M(new Color(0.92f, 0.88f, 0.62f), 0f, 0.05f);
        }

        // ── Public API ────────────────────────────────────────────────────────
        public void ResetTrack()
        {
            foreach (var s in segs) if (s) Destroy(s);
            segs.Clear();
            nextZ = -12f;
            // First 2 segments are obstacle-free so the player isn't hit immediately
            for (int i = 0; i < MaxSegs; i++) SpawnSeg(i >= 2);
        }

        public void UpdateSpawner(float playerZ)
        {
            while (nextZ < playerZ + MaxSegs * SegLen) SpawnSeg(true);

            // BUG FIX: destroy old segments based on world position, not count,
            // so the sliding window stays correct even after a game restart.
            while (segs.Count > 0 && segs[0] != null
                && segs[0].transform.position.z + SegLen < playerZ - 24f)
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

            Ground(seg.transform);
            Dividers(seg.transform);
            Foliage(seg.transform);

            if (withObs) SpawnObstaclesPickups(seg.transform);
            else         SpawnCoinLine(seg.transform, 1, 6f, 5);

            segs.Add(seg);
            nextZ += SegLen;
        }

        // ── Ground ────────────────────────────────────────────────────────────
        private void Ground(Transform seg)
        {
            var path = Prim(PrimitiveType.Cube, seg,
                new Vector3(0f, -0.05f, SegLen * .5f),
                new Vector3(9f, 0.15f, SegLen), pathMat, false);
            path.tag = "Ground";

            // Stone curbs
            foreach (int s in new[] { -1, 1 })
            {
                var c = Prim(PrimitiveType.Cube, seg,
                    new Vector3(s * 4.6f, 0.04f, SegLen * .5f),
                    new Vector3(0.28f, 0.18f, SegLen), stoneMat, false);
                c.tag = "Ground";
            }

            // Wide grass
            var grass = Prim(PrimitiveType.Cube, seg,
                new Vector3(0f, -0.22f, SegLen * .5f),
                new Vector3(80f, 0.26f, SegLen), grassMat, false);
            grass.tag = "Ground";

            // Dirt shoulders
            foreach (int s in new[] { -1, 1 })
                Prim(PrimitiveType.Cube, seg,
                    new Vector3(s * 6.5f, -0.12f, SegLen * .5f),
                    new Vector3(3f, 0.15f, SegLen), dirtMat, false);
        }

        // ── Lane dividers ─────────────────────────────────────────────────────
        private void Dividers(Transform seg)
        {
            int dashes = 6;
            for (int d = 0; d < dashes; d++)
            {
                float z = 2f + d * (SegLen / dashes);
                foreach (int s in new[] { -1, 1 })
                    Prim(PrimitiveType.Cube, seg,
                        new Vector3(s * 2.6f, 0.02f, z),
                        new Vector3(0.12f, 0.04f, 1.8f), dividerMat, false);
            }
        }

        // ── Foliage ───────────────────────────────────────────────────────────
        private void Foliage(Transform seg)
        {
            int trees = Random.Range(5, 9);
            for (int i = 0; i < trees; i++)
            {
                float side = (i % 2 == 0) ? -1f : 1f;
                float x    = side * Random.Range(7f, 18f);
                float z    = Random.Range(2f, SegLen - 2f);
                float roll = Random.value;
                if      (roll < 0.45f) PalmTree(seg, new Vector3(x, 0f, z));
                else if (roll < 0.75f) BroadTree(seg, new Vector3(x, 0f, z));
                else                   BambooCluster(seg, new Vector3(x, 0f, z));
            }
            for (int b = 0; b < Random.Range(3, 6); b++)
            {
                float side = (b % 2 == 0) ? -1f : 1f;
                Bush(seg, new Vector3(side * Random.Range(5.2f, 7.5f), 0f, Random.Range(3f, SegLen - 3f)));
            }
            if (Random.value < 0.6f)
            {
                float side = Random.value < 0.5f ? -1f : 1f;
                DecoRock(seg, new Vector3(side * Random.Range(5.5f, 8f), 0f, Random.Range(4f, SegLen - 4f)));
            }
        }

        private void PalmTree(Transform seg, Vector3 pos)
        {
            var t   = Child(seg, "Palm", pos);
            float sc = Random.Range(0.9f, 1.4f);
            float h  = Random.Range(6f, 9f) * sc;

            for (int i = 0; i < 4; i++)
            {
                float y = i * (h / 4f) + h / 8f;
                float lean = i * 0.08f * sc;
                Prim(PrimitiveType.Cylinder, t,
                    new Vector3(lean, y, 0f),
                    new Vector3(0.38f * sc, h / 8f, 0.38f * sc), barkMat);
            }
            for (int f = 0; f < 8; f++)
            {
                var fr = Prim(PrimitiveType.Cube, t,
                    new Vector3(0f, h, 0f),
                    new Vector3(0.28f * sc, 0.07f, 2.8f * sc), leafMat);
                fr.transform.localRotation = Quaternion.Euler(Random.Range(18f, 30f), f * 45f, 0f);
            }
            Prim(PrimitiveType.Sphere, t, new Vector3(0f, h + 0.15f, 0f), Vector3.one * 0.5f * sc, darkLeafMat);
        }

        private void BroadTree(Transform seg, Vector3 pos)
        {
            var t  = Child(seg, "BroadTree", pos);
            float sc = Random.Range(0.85f, 1.3f);
            float h  = Random.Range(5f, 8f) * sc;

            Prim(PrimitiveType.Cylinder, t, new Vector3(0f, h * .5f, 0f),
                new Vector3(0.55f * sc, h * .5f, 0.55f * sc), barkMat);

            float[] ys = { h * .72f, h * .90f, h * 1.05f };
            float[] rs = { 3.0f, 2.4f, 1.6f };
            for (int i = 0; i < 3; i++)
                Prim(PrimitiveType.Sphere, t,
                    new Vector3(Random.Range(-.3f, .3f) * sc, ys[i], Random.Range(-.3f, .3f) * sc),
                    new Vector3(rs[i]*sc, rs[i]*.85f*sc, rs[i]*sc),
                    i % 2 == 0 ? darkLeafMat : leafMat);
        }

        private void BambooCluster(Transform seg, Vector3 pos)
        {
            int count = Random.Range(3, 6);
            for (int i = 0; i < count; i++)
            {
                var t = Child(seg, "Bamboo",
                    pos + new Vector3(Random.Range(-.6f, .6f), 0f, Random.Range(-.6f, .6f)));
                float h = Random.Range(5f, 8f);
                int nodes = Mathf.RoundToInt(h / 1.2f);
                for (int n = 0; n < nodes; n++)
                    Prim(PrimitiveType.Cylinder, t, new Vector3(0f, n * 1.2f + .6f, 0f),
                        new Vector3(.15f, .62f, .15f), n % 2 == 0 ? leafMat : darkLeafMat);
                Prim(PrimitiveType.Sphere, t, new Vector3(0f, h + .4f, 0f),
                    new Vector3(.8f, .5f, .8f), leafMat);
            }
        }

        private void Bush(Transform seg, Vector3 pos)
        {
            for (int i = 0; i < Random.Range(2, 4); i++)
            {
                float s = Random.Range(.8f, 1.4f);
                Prim(PrimitiveType.Sphere, seg,
                    pos + new Vector3(Random.Range(-.5f, .5f), Random.Range(.25f, .55f), Random.Range(-.4f, .4f)),
                    new Vector3(s * 1.3f, s * .85f, s * 1.3f),
                    Random.value < .5f ? leafMat : darkLeafMat, false);
            }
        }

        private void DecoRock(Transform seg, Vector3 pos)
        {
            for (int i = 0; i < Random.Range(1, 4); i++)
            {
                float s = Random.Range(.4f, 1.1f);
                var r = Prim(PrimitiveType.Sphere, seg,
                    pos + new Vector3(Random.Range(-.6f, .6f), s * .4f, Random.Range(-.5f, .5f)),
                    new Vector3(s * 1.3f, s * .8f, s * 1.1f), rockMat, false);
                r.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }
        }

        // ── Obstacles & pickups ───────────────────────────────────────────────
        private void SpawnObstaclesPickups(Transform seg)
        {
            // BUG FIX: guarantee at least 1 safe lane. Previously pattern==1
            // could theoretically use safeLane==0 and block lanes 1 and 2,
            // but if the player happened to be in lane 0 approaching very fast
            // there was not enough visual lead time. Now obsZ is pushed further.
            int pattern  = Random.Range(0, 3);
            float obsZ   = Random.Range(10f, 16f);  // give player more lead time
            int safeLane = Random.Range(0, 3);

            if (pattern == 0)
            {
                SpawnObstacle(seg, (safeLane + 1) % 3, obsZ);
                SpawnCoinLine(seg, safeLane, obsZ - 5f, 4);
            }
            else if (pattern == 1)
            {
                // BUG FIX: use ((safeLane+1)%3) and ((safeLane+2)%3) to block
                // exactly the two non-safe lanes (old code used l != safeLane
                // which is correct but confusing; keeping that form but explicit)
                for (int l = 0; l < 3; l++) if (l != safeLane) SpawnObstacle(seg, l, obsZ);
                SpawnCoinLine(seg, safeLane, obsZ - 4f, 4);
            }
            else // fallen log
            {
                SpawnLog(seg, (safeLane + 1) % 3, obsZ);
                SpawnArcCoins(seg, safeLane, obsZ);
            }

            // Rare gem or power-up after the obstacle cluster
            if (Random.value < 0.30f)
            {
                float pz = obsZ + Random.Range(6f, 10f);
                int   pl = Random.Range(0, 3);
                if (Random.value < 0.45f) SpawnPowerUp(seg, pl, pz);
                else                      SpawnGem(seg, pl, pz);
            }
        }

        private void SpawnObstacle(Transform seg, int lane, float z)
        {
            float x = PlayerRunner.LaneX[lane];
            int   t = Random.Range(0, 3);

            if (t == 0)      // Boulder
            {
                var go = Prim(PrimitiveType.Sphere, seg, new Vector3(x, .9f, z), new Vector3(1.9f, 1.8f, 1.9f), rockMat);
                go.GetComponent<Collider>().isTrigger = true;
                go.AddComponent<Obstacle>().Initialize(ObstacleType.Rock, 1.8f);
            }
            else if (t == 1) // Ancient pillar
            {
                var go = Prim(PrimitiveType.Cylinder, seg, new Vector3(x, 1.6f, z), new Vector3(1.1f, 1.6f, 1.1f), stoneMat);
                go.GetComponent<Collider>().isTrigger = true;
                go.AddComponent<Obstacle>().Initialize(ObstacleType.AncientPillar, 3.2f);
                Prim(PrimitiveType.Cube, seg, new Vector3(x, 3.3f, z), new Vector3(1.5f, .25f, 1.5f), stoneMat, false);
            }
            else             // Tree stump
            {
                var go = Prim(PrimitiveType.Cylinder, seg, new Vector3(x, .45f, z), new Vector3(1.3f, .45f, 1.3f), barkMat);
                go.GetComponent<Collider>().isTrigger = true;
                go.AddComponent<Obstacle>().Initialize(ObstacleType.LowBarrier, .9f);
                Prim(PrimitiveType.Cylinder, seg, new Vector3(x, .92f, z), new Vector3(1.4f, .06f, 1.4f), rockMat, false);
            }
        }

        private void SpawnLog(Transform seg, int lane, float z)
        {
            float x  = PlayerRunner.LaneX[lane];
            var   go = Prim(PrimitiveType.Cylinder, seg, new Vector3(x, .38f, z), new Vector3(.6f, 1.4f, .6f), barkMat);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            go.GetComponent<Collider>().isTrigger = true;
            go.AddComponent<Obstacle>().Initialize(ObstacleType.FallenLog, .76f);
            foreach (int s in new[] { -1, 1 })
                Prim(PrimitiveType.Cylinder, seg, new Vector3(x + s * 1.5f, .38f, z), new Vector3(.64f, .08f, .64f), rockMat, false);
        }

        private void SpawnCoinLine(Transform seg, int lane, float startZ, int count)
        {
            float x = PlayerRunner.LaneX[lane];
            for (int i = 0; i < count; i++) AddCoin(seg, new Vector3(x, .85f, startZ + i * 2.0f));
        }

        private void SpawnArcCoins(Transform seg, int lane, float cz)
        {
            float x  = PlayerRunner.LaneX[lane];
            float[] h = { .85f, 1.2f, 1.7f, 2.1f, 2.3f, 2.1f, 1.7f, 1.2f, .85f };
            for (int i = 0; i < h.Length; i++) AddCoin(seg, new Vector3(x, h[i], cz - 4f + i * 1.0f));
        }

        private void SpawnGem(Transform seg, int lane, float z)
        {
            float x  = PlayerRunner.LaneX[lane];
            var   go = Prim(PrimitiveType.Sphere, seg, new Vector3(x, .85f, z), Vector3.one * .58f, gemMat);
            go.GetComponent<Collider>().isTrigger = true;
            go.AddComponent<Collectible>().Type    = CollectibleType.Gem;
            Prim(PrimitiveType.Cylinder, seg, new Vector3(x, .85f, z), new Vector3(1.1f, .06f, 1.1f), gemMat, false);
        }

        private void SpawnPowerUp(Transform seg, int lane, float z)
        {
            float x  = PlayerRunner.LaneX[lane];
            var   go = Prim(PrimitiveType.Cube, seg, new Vector3(x, .85f, z), Vector3.one * .72f, puMat);
            go.transform.localRotation = Quaternion.Euler(35f, 45f, 0f);
            go.GetComponent<Collider>().isTrigger = true;
            var c = go.AddComponent<Collectible>();
            c.Type = CollectibleType.PowerUp;
            PowerUpType[] types = { PowerUpType.Magnet, PowerUpType.Shield, PowerUpType.SpeedBoost, PowerUpType.DoubleScore, PowerUpType.Fly };
            c.PowerUpVariant = types[Random.Range(0, types.Length)];
            Prim(PrimitiveType.Cylinder, seg, new Vector3(x, .85f, z), new Vector3(1.3f, .07f, 1.3f), puMat, false);
        }

        private void AddCoin(Transform seg, Vector3 pos)
        {
            var go = Prim(PrimitiveType.Cylinder, seg, pos, new Vector3(.48f, .07f, .48f), coinMat);
            go.GetComponent<Collider>().isTrigger = true;
            go.AddComponent<Collectible>().Type   = CollectibleType.Coin;
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static Transform Child(Transform parent, string name, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go.transform;
        }

        private static GameObject Prim(
            PrimitiveType type, Transform parent,
            Vector3 localPos, Vector3 scale, Material mat,
            bool keepCollider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale    = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!keepCollider) Destroy(go.GetComponent<Collider>());
            return go;
        }
    }
}
