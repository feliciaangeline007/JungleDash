using System.Collections.Generic;
using UnityEngine;

namespace JungleDash
{
    public class TrackSpawner : MonoBehaviour
    {
        private const float SegLen = 24f;
        private const int MaxSegs = 8;

        private float nextZ = -12f;
        private readonly List<GameObject> segs = new List<GameObject>();

        private Material pathMat, grassMat, barkMat, leafMat, rockMat, coinMat, gemMat, puMat;
        private Shader litShader;

        private void Awake()
        {
            litShader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                     ?? Shader.Find("Standard");
            BuildMaterials();
        }

        private Material Mat(Color c)
        {
            var m = new Material(litShader);
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            return m;
        }

        private void BuildMaterials()
        {
            pathMat = Mat(new Color(0.62f, 0.52f, 0.36f));
            var tex = Resources.Load<Texture2D>("MudRocky");
            if (tex != null) { pathMat.mainTexture = tex; pathMat.mainTextureScale = new Vector2(2f, 6f); }

            grassMat = Mat(new Color(0.24f, 0.46f, 0.18f));
            barkMat  = Mat(new Color(0.38f, 0.26f, 0.15f));
            leafMat  = Mat(new Color(0.18f, 0.58f, 0.22f));
            rockMat  = Mat(new Color(0.42f, 0.44f, 0.40f));
            coinMat  = Mat(new Color(1f, 0.82f, 0.1f));
            gemMat   = Mat(new Color(0.85f, 0.25f, 1f));
            puMat    = Mat(new Color(0.1f, 0.85f, 0.95f));
        }

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
            while (segs.Count > 0 && segs[0] != null && segs[0].transform.position.z + SegLen < playerZ - 18f)
            {
                Destroy(segs[0]);
                segs.RemoveAt(0);
            }
        }

        private void SpawnSeg(bool withObs)
        {
            var seg = new GameObject("Seg_" + nextZ);
            seg.transform.SetParent(transform, true);
            seg.transform.position = new Vector3(0f, 0f, nextZ);

            // Path floor
            var path = Prim(PrimitiveType.Cube, seg.transform, new Vector3(0f, -0.1f, SegLen * 0.5f),
                new Vector3(8.5f, 0.2f, SegLen), pathMat, false);
            path.tag = "Ground";

            // Wide grass flanks
            var grass = Prim(PrimitiveType.Cube, seg.transform, new Vector3(0f, -0.25f, SegLen * 0.5f),
                new Vector3(70f, 0.25f, SegLen), grassMat, false);
            grass.tag = "Ground";

            // Lane divider dashes
            for (int i = 0; i < 3; i++)
            {
                float mz = 4f + i * 8f;
                DividerDash(seg.transform, -1.3f, mz);
                DividerDash(seg.transform, 1.3f, mz);
            }

            // Flanking jungle trees & bushes
            int trees = Random.Range(4, 7);
            for (int i = 0; i < trees; i++)
            {
                float side = (i % 2 == 0) ? -1f : 1f;
                float x = side * Random.Range(6.2f, 15f);
                float z = Random.Range(2f, SegLen - 2f);
                if (Random.value < 0.55f) PalmTree(seg.transform, new Vector3(x, 0f, z));
                else BroadTree(seg.transform, new Vector3(x, 0f, z));
            }
            for (int b = 0; b < 3; b++)
            {
                float side = (b % 2 == 0) ? -1f : 1f;
                Bush(seg.transform, new Vector3(side * Random.Range(4.8f, 6.2f), 0f, Random.Range(3f, SegLen - 3f)));
            }

            if (withObs) SpawnObstaclesPickups(seg.transform);
            else         SpawnCoins(seg.transform, 1, 8f, 4);

            segs.Add(seg);
            nextZ += SegLen;
        }

        private void DividerDash(Transform p, float x, float z)
        {
            var d = Prim(PrimitiveType.Cube, p, new Vector3(x, 0.01f, z),
                new Vector3(0.08f, 0.02f, 2.6f), leafMat, false);
        }

        private void PalmTree(Transform p, Vector3 pos)
        {
            var t = new GameObject("Palm"); t.transform.SetParent(p, false); t.transform.localPosition = pos;
            float h = Random.Range(5.5f, 7.5f);
            Prim(PrimitiveType.Cylinder, t.transform, new Vector3(0f, h * 0.5f, 0f), new Vector3(0.42f, h * 0.5f, 0.42f), barkMat);
            for (int f = 0; f < 7; f++)
            {
                var fr = Prim(PrimitiveType.Cube, t.transform, new Vector3(0f, h, 0f), new Vector3(0.35f, 0.08f, 2.6f), leafMat);
                fr.transform.localRotation = Quaternion.Euler(22f, f * (360f / 7), 0f);
            }
        }

        private void BroadTree(Transform p, Vector3 pos)
        {
            var t = new GameObject("BroadTree"); t.transform.SetParent(p, false); t.transform.localPosition = pos;
            float h = Random.Range(4.5f, 6.2f);
            Prim(PrimitiveType.Cylinder, t.transform, new Vector3(0f, h * 0.5f, 0f), new Vector3(0.6f, h * 0.5f, 0.6f), barkMat);
            Prim(PrimitiveType.Sphere, t.transform, new Vector3(0f, h * 0.9f, 0f), new Vector3(3.2f, 2.8f, 3.2f), leafMat);
        }

        private void Bush(Transform p, Vector3 pos)
        {
            Prim(PrimitiveType.Sphere, p, pos + Vector3.up * 0.35f, new Vector3(1.4f, 0.9f, 1.4f), leafMat);
        }

        private void SpawnObstaclesPickups(Transform seg)
        {
            int pattern  = Random.Range(0, 3);
            float obsZ   = Random.Range(8f, 14f);
            int safeLane = Random.Range(0, 3);

            if (pattern == 0)
            {
                SpawnObstacle(seg, (safeLane + 1) % 3, obsZ);
                SpawnCoins(seg, safeLane, obsZ - 4f, 4);
            }
            else if (pattern == 1)
            {
                for (int l = 0; l < 3; l++) if (l != safeLane) SpawnObstacle(seg, l, obsZ);
                SpawnCoins(seg, safeLane, obsZ - 3f, 3);
            }
            else
            {
                SpawnLog(seg, safeLane, obsZ);
                SpawnArcCoins(seg, safeLane, obsZ);
            }

            if (Random.value < 0.28f)
            {
                float pz = obsZ + 7f;
                int pl = Random.Range(0, 3);
                if (Random.value < 0.45f) SpawnPowerUp(seg, pl, pz);
                else SpawnGem(seg, pl, pz);
            }
        }

        private void SpawnObstacle(Transform p, int lane, float z)
        {
            float x = PlayerRunner.LaneX[lane];
            int t = Random.Range(0, 3);
            GameObject go;
            if (t == 0)
            {
                go = Prim(PrimitiveType.Sphere, p, new Vector3(x, 0.85f, z), new Vector3(1.8f, 1.7f, 1.8f), rockMat);
                go.GetComponent<Collider>().isTrigger = true;
                go.AddComponent<Obstacle>().Initialize(ObstacleType.Rock, 1.7f);
            }
            else if (t == 1)
            {
                go = Prim(PrimitiveType.Cylinder, p, new Vector3(x, 1.5f, z), new Vector3(1.1f, 1.5f, 1.1f), rockMat);
                go.GetComponent<Collider>().isTrigger = true;
                go.AddComponent<Obstacle>().Initialize(ObstacleType.AncientPillar, 3f);
            }
            else
            {
                go = Prim(PrimitiveType.Cylinder, p, new Vector3(x, 0.4f, z), new Vector3(1.2f, 0.4f, 1.2f), barkMat);
                go.GetComponent<Collider>().isTrigger = true;
                go.AddComponent<Obstacle>().Initialize(ObstacleType.LowBarrier, 0.8f);
            }
        }

        private void SpawnLog(Transform p, int lane, float z)
        {
            float x = PlayerRunner.LaneX[lane];
            var go = Prim(PrimitiveType.Cylinder, p, new Vector3(x, 0.35f, z), new Vector3(0.55f, 1.35f, 0.55f), barkMat);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            go.GetComponent<Collider>().isTrigger = true;
            go.AddComponent<Obstacle>().Initialize(ObstacleType.FallenLog, 0.7f);
        }

        private void SpawnCoins(Transform p, int lane, float startZ, int count)
        {
            float x = PlayerRunner.LaneX[lane];
            for (int i = 0; i < count; i++) AddCoin(p, new Vector3(x, 0.75f, startZ + i * 2.2f));
        }

        private void SpawnArcCoins(Transform p, int lane, float cz)
        {
            float x = PlayerRunner.LaneX[lane];
            float[] heights = { 0.75f, 1.2f, 1.8f, 2.2f, 1.8f, 1.2f, 0.75f };
            for (int i = 0; i < heights.Length; i++)
                AddCoin(p, new Vector3(x, heights[i], cz - 3f + i * 1.0f));
        }

        private void SpawnGem(Transform p, int lane, float z)
        {
            float x = PlayerRunner.LaneX[lane];
            var go = Prim(PrimitiveType.Sphere, p, new Vector3(x, 0.75f, z), Vector3.one * 0.55f, gemMat);
            go.GetComponent<Collider>().isTrigger = true;
            var c = go.AddComponent<Collectible>();
            c.Type = CollectibleType.Gem;
        }

        private void SpawnPowerUp(Transform p, int lane, float z)
        {
            float x = PlayerRunner.LaneX[lane];
            var go = Prim(PrimitiveType.Cube, p, new Vector3(x, 0.75f, z), Vector3.one * 0.7f, puMat);
            go.GetComponent<Collider>().isTrigger = true;
            var c = go.AddComponent<Collectible>();
            c.Type = CollectibleType.PowerUp;
            PowerUpType[] types = { PowerUpType.Magnet, PowerUpType.Shield, PowerUpType.SpeedBoost, PowerUpType.DoubleScore, PowerUpType.Fly };
            c.PowerUpVariant = types[Random.Range(0, types.Length)];
        }

        private void AddCoin(Transform p, Vector3 localPos)
        {
            var go = Prim(PrimitiveType.Cylinder, p, localPos, new Vector3(0.45f, 0.08f, 0.45f), coinMat);
            go.GetComponent<Collider>().isTrigger = true;
            go.AddComponent<Collectible>().Type = CollectibleType.Coin;
        }

        // Helper: create primitive, optionally remove collider
        private GameObject Prim(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Material mat, bool keepCollider = true)
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
