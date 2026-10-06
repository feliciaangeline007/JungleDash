// TrackSpawner.cs – Temple Run-style jungle temple track
// Narrow raised stone bridge, deep abyss on sides, ancient ruins, torches,
// hanging vines, and dense jungle canopy overhead.
using System.Collections.Generic;
using UnityEngine;

namespace JungleDash
{
    public class TrackSpawner : MonoBehaviour
    {
        // ── Layout constants ──────────────────────────────────────────────────
        // Path is a narrow 3-lane stone bridge.  Each lane is 1.6 u wide.
        // Lane centres: -1.6, 0, +1.6 (tight, like Temple Run)
        public  const float LaneW    = 1.6f;
        private const float PathW    = 5.0f;   // total bridge width
        private const float BridgeH  = 0.35f;  // bridge deck height
        private const float SegLen   = 28f;
        private const int   MaxSegs  = 9;
        private const float AbyssDepth = 18f;  // how deep the sides look

        // ── State ─────────────────────────────────────────────────────────────
        private float nextZ = 0f;
        private readonly List<GameObject> segs = new List<GameObject>();

        // ── Materials ─────────────────────────────────────────────────────────
        private Material stoneMat;       // aged dark stone bridge
        private Material stoneRailMat;   // lighter stone railings
        private Material mossMat;        // green moss patches
        private Material abyssMat;       // deep void below
        private Material voidSkyMat;     // dark foggy sides
        private Material barkMat;        // tree trunks
        private Material leafMat;        // jungle canopy green
        private Material darkLeafMat;    // deep canopy shadow
        private Material vineMat;        // hanging vines
        private Material torchBaseMat;   // stone torch holder
        private Material fireMat;        // glowing fire
        private Material coinMat;        // gold coins
        private Material gemMat;         // purple gems
        private Material puMat;          // cyan power-up
        private Material ruinMat;        // broken stone ruins
        private Material goldRuinMat;    // gilded temple gold

        private Texture2D mudTex;
        private Texture2D palmTex;
        private Texture2D leafTex;

        private void Awake()
        {
            mudTex  = Resources.Load<Texture2D>("MudRocky");
            palmTex = Resources.Load<Texture2D>("PalmBillboard");
            leafTex = Resources.Load<Texture2D>("BroadleafBillboard");
            BuildMaterials();
        }

        private void BuildMaterials()
        {
            // Stone bridge – dark, slightly rough temple stone
            stoneMat      = MatFactory.Opaque(new Color(0.28f, 0.25f, 0.22f), 0f, 0.18f);
            if (mudTex != null)
            {
                if (stoneMat.HasProperty("_BaseMap")) stoneMat.SetTexture("_BaseMap", mudTex);
                else stoneMat.mainTexture = mudTex;
                stoneMat.mainTextureScale = new Vector2(3f, 10f);
            }

            stoneRailMat  = MatFactory.Opaque(new Color(0.35f, 0.32f, 0.28f), 0f, 0.20f);
            mossMat       = MatFactory.Opaque(new Color(0.18f, 0.42f, 0.12f), 0f, 0.10f);
            ruinMat       = MatFactory.Opaque(new Color(0.32f, 0.28f, 0.22f), 0.05f, 0.25f);
            goldRuinMat   = MatFactory.Opaque(new Color(0.72f, 0.55f, 0.12f), 0.8f, 0.7f);
            abyssMat      = MatFactory.Opaque(new Color(0.04f, 0.04f, 0.06f), 0f, 0f);
            voidSkyMat    = MatFactory.Opaque(new Color(0.06f, 0.08f, 0.06f), 0f, 0f);

            barkMat       = MatFactory.Opaque(new Color(0.22f, 0.14f, 0.06f), 0f, 0.08f);
            leafMat       = MatFactory.Opaque(new Color(0.12f, 0.48f, 0.14f), 0f, 0.10f);
            darkLeafMat   = MatFactory.Opaque(new Color(0.07f, 0.28f, 0.08f), 0f, 0.08f);
            vineMat       = MatFactory.Opaque(new Color(0.14f, 0.36f, 0.10f), 0f, 0.12f);
            torchBaseMat  = MatFactory.Opaque(new Color(0.30f, 0.27f, 0.22f), 0.1f, 0.3f);
            fireMat       = MatFactory.Glow(new Color(1f, 0.45f, 0.05f), 2.5f);

            coinMat  = MatFactory.Glow(new Color(1.0f, 0.80f, 0.05f), 2.2f);
            gemMat   = MatFactory.Glow(new Color(0.6f, 0.05f, 1.00f), 2.5f);
            puMat    = MatFactory.Glow(new Color(0.0f, 0.90f, 1.00f), 2.5f);
        }

        // ── Public API ────────────────────────────────────────────────────────
        // FIX: static readonly field instead of property returning new array each call
        public static readonly float[] LaneX = { -LaneW, 0f, LaneW };

        public void ResetTrack()
        {
            foreach (var s in segs) if (s) Destroy(s);
            segs.Clear();
            nextZ = 0f;
            for (int i = 0; i < MaxSegs; i++) SpawnSeg(i >= 3);
        }

        public void UpdateSpawner(float playerZ)
        {
            while (nextZ < playerZ + MaxSegs * SegLen) SpawnSeg(true);
            while (segs.Count > 0 && segs[0] != null
                   && segs[0].transform.position.z + SegLen < playerZ - 30f)
            {
                Destroy(segs[0]);
                segs.RemoveAt(0);
            }
        }

        // ── Segment ───────────────────────────────────────────────────────────
        private void SpawnSeg(bool withHazards)
        {
            var seg = new GameObject("Seg_" + Mathf.RoundToInt(nextZ));
            seg.transform.SetParent(transform, true);
            seg.transform.position = new Vector3(0f, 0f, nextZ);

            BuildBridge(seg.transform);
            BuildAbyss(seg.transform);
            BuildJungleWalls(seg.transform);
            BuildRuins(seg.transform);
            PlaceTorches(seg.transform);

            if (withHazards)  SpawnHazardsAndPickups(seg.transform);
            else              SpawnCoinLine(seg.transform, 1, 8f, 5);

            segs.Add(seg);
            nextZ += SegLen;
        }

        // ── Bridge deck ───────────────────────────────────────────────────────
        private void BuildBridge(Transform seg)
        {
            // Central stone deck
            var deck = Prim(PrimitiveType.Cube, seg,
                new Vector3(0f, 0f, SegLen * .5f),
                new Vector3(PathW, BridgeH, SegLen), stoneMat, false);
            deck.tag = "Ground";

            // Moss patches along the deck
            for (int m = 0; m < 4; m++)
            {
                float mz = Random.Range(2f, SegLen - 2f);
                float mx = Random.Range(-PathW * .4f, PathW * .4f);
                Prim(PrimitiveType.Cube, seg,
                    new Vector3(mx, BridgeH * .5f + 0.01f, mz),
                    new Vector3(Random.Range(.5f, 1.4f), 0.04f, Random.Range(.4f, 1.2f)), mossMat, false);
            }

            // Stone railing pillars on each side (like Temple Run bridge walls)
            float railH = 1.1f;
            float railX = PathW * .5f - 0.18f;
            int pillars = Mathf.RoundToInt(SegLen / 3.5f);
            for (int p = 0; p < pillars; p++)
            {
                float pz = 1.5f + p * (SegLen / pillars);
                foreach (int s in new[] { -1, 1 })
                {
                    // Vertical pillar
                    Prim(PrimitiveType.Cube, seg,
                        new Vector3(s * railX, BridgeH * .5f + railH * .5f, pz),
                        new Vector3(0.28f, railH, 0.28f), stoneRailMat, false);
                    // Cap stone
                    Prim(PrimitiveType.Cube, seg,
                        new Vector3(s * railX, BridgeH * .5f + railH + 0.1f, pz),
                        new Vector3(0.38f, 0.2f, 0.38f), goldRuinMat, false);
                }
                // Horizontal rail between pillars (every other segment)
                if (p < pillars - 1)
                {
                    float midZ = pz + (SegLen / pillars) * .5f;
                    float spanLen = SegLen / pillars - 0.2f;
                    foreach (int s in new[] { -1, 1 })
                    {
                        Prim(PrimitiveType.Cube, seg,
                            new Vector3(s * railX, BridgeH * .5f + railH - 0.15f, midZ),
                            new Vector3(0.18f, 0.18f, spanLen), stoneRailMat, false);
                    }
                }
            }

            // Bridge underside supports (arches visible in abyss)
            int arches = 4;
            for (int a = 0; a < arches; a++)
            {
                float az = (a + .5f) * (SegLen / arches);
                // Two diagonal struts
                foreach (int s in new[] { -1, 1 })
                {
                    Prim(PrimitiveType.Cylinder, seg,
                        new Vector3(s * PathW * .3f, -AbyssDepth * .3f, az),
                        new Vector3(0.4f, AbyssDepth * .35f, 0.4f), stoneMat, false);
                }
            }
        }

        // ── Abyss ─────────────────────────────────────────────────────────────
        private void BuildAbyss(Transform seg)
        {
            float w = 30f;
            // Dark floor far below
            Prim(PrimitiveType.Cube, seg,
                new Vector3(0f, -AbyssDepth, SegLen * .5f),
                new Vector3(w * 2f, 0.5f, SegLen), abyssMat, false);

            // Side walls (the cliffs/void)
            foreach (int s in new[] { -1, 1 })
            {
                Prim(PrimitiveType.Cube, seg,
                    new Vector3(s * (PathW * .5f + w * .5f), -AbyssDepth * .5f, SegLen * .5f),
                    new Vector3(w, AbyssDepth, SegLen), voidSkyMat, false);
            }

            // Floating jungle debris / ancient platform fragments in abyss
            for (int d = 0; d < 3; d++)
            {
                float dx = Random.Range(-8f, 8f);
                float dy = Random.Range(-AbyssDepth + 2f, -3f);
                float dz = Random.Range(3f, SegLen - 3f);
                float ds = Random.Range(1.2f, 3f);
                Prim(PrimitiveType.Cube, seg,
                    new Vector3(dx, dy, dz), new Vector3(ds, .3f, ds), ruinMat, false);
            }
        }

        // ── Jungle walls flanking the bridge ─────────────────────────────────
        private void BuildJungleWalls(Transform seg)
        {
            // Dense canopy trees on both sides (x = ±8..±22)
            int treeCount = Random.Range(6, 10);
            for (int i = 0; i < treeCount; i++)
            {
                float side = (i % 2 == 0) ? -1f : 1f;
                float x    = side * Random.Range(7f, 22f);
                float z    = Random.Range(1f, SegLen - 1f);
                float roll = Random.value;
                if (roll < 0.5f) TempleTree(seg, new Vector3(x, -1f, z));
                else             JunglePalm(seg, new Vector3(x, -1f, z));
            }

            // Hanging vines from above (they drape down from canopy)
            int vines = Random.Range(3, 7);
            for (int v = 0; v < vines; v++)
            {
                float side = Random.value < .5f ? -1f : 1f;
                float vx   = side * Random.Range(6f, 12f);
                float vz   = Random.Range(3f, SegLen - 3f);
                BuildVine(seg, new Vector3(vx, 0f, vz));
            }

            // Overhang canopy over the bridge (adds enclosed temple feel)
            for (int c = 0; c < 2; c++)
            {
                float cz = 6f + c * (SegLen * .5f);
                Prim(PrimitiveType.Sphere, seg,
                    new Vector3(0f, 14f, cz),
                    new Vector3(18f, 8f, 12f), darkLeafMat, false);
            }
        }

        private void TempleTree(Transform seg, Vector3 pos)
        {
            float sc = Random.Range(1f, 1.6f);
            float h  = Random.Range(8f, 14f) * sc;

            // Thick trunk
            Prim(PrimitiveType.Cylinder, seg,
                pos + new Vector3(0f, h * .5f, 0f),
                new Vector3(0.7f * sc, h * .5f, 0.7f * sc), barkMat, false);

            // Roots (3-4 angled props)
            for (int r = 0; r < 3; r++)
            {
                float angle = r * 120f;
                float rx    = Mathf.Cos(angle * Mathf.Deg2Rad) * 1.2f * sc;
                float rz    = Mathf.Sin(angle * Mathf.Deg2Rad) * 1.2f * sc;
                var root    = Prim(PrimitiveType.Cylinder, seg,
                    pos + new Vector3(rx, 0.8f * sc, rz),
                    new Vector3(0.22f * sc, 0.85f * sc, 0.22f * sc), barkMat, false);
                root.transform.localRotation = Quaternion.Euler(20f, angle, 0f);
            }

            // 3-layer canopy
            for (int l = 0; l < 3; l++)
            {
                float ly = h * (.65f + l * .18f);
                float lr = (3f - l * .7f) * sc;
                Prim(PrimitiveType.Sphere, seg,
                    pos + new Vector3(0f, ly, 0f),
                    new Vector3(lr * 1.2f, lr * .85f, lr * 1.2f),
                    l % 2 == 0 ? darkLeafMat : leafMat, false);
            }
        }

        private void JunglePalm(Transform seg, Vector3 pos)
        {
            float sc = Random.Range(0.9f, 1.5f);
            float h  = Random.Range(7f, 11f) * sc;

            // Curved 3-part trunk
            for (int i = 0; i < 3; i++)
            {
                float lean = i * 0.5f * sc;
                Prim(PrimitiveType.Cylinder, seg,
                    pos + new Vector3(lean, (i + .5f) * (h / 3f), 0f),
                    new Vector3(0.4f * sc, h / 6f, 0.4f * sc), barkMat, false);
            }

            // Palm fronds
            for (int f = 0; f < 9; f++)
            {
                float angle = f * 40f;
                var frond = Prim(PrimitiveType.Cube, seg,
                    pos + new Vector3(1.5f * sc, h, 0f),
                    new Vector3(0.24f * sc, 0.06f, 3.2f * sc), leafMat, false);
                frond.transform.RotateAround(pos + new Vector3(0f, h, 0f), Vector3.up, angle);
                frond.transform.localRotation *= Quaternion.Euler(Random.Range(15f, 30f), 0f, 0f);
            }
        }

        private void BuildVine(Transform seg, Vector3 pos)
        {
            float topY  = Random.Range(6f, 12f);
            float len   = Random.Range(3f, topY * .8f);
            int   nodes = Mathf.RoundToInt(len / 0.6f);
            for (int n = 0; n < nodes; n++)
            {
                float sway = Mathf.Sin(n * .8f) * 0.3f;
                Prim(PrimitiveType.Cylinder, seg,
                    pos + new Vector3(sway, topY - n * (len / nodes), 0f),
                    new Vector3(0.06f, len / nodes * .55f, 0.06f), vineMat, false);
            }
            // Leaf cluster at bottom
            Prim(PrimitiveType.Sphere, seg,
                pos + new Vector3(0f, topY - len + 0.3f, 0f),
                new Vector3(0.55f, 0.4f, 0.55f), leafMat, false);
        }

        // ── Temple ruins ──────────────────────────────────────────────────────
        private void BuildRuins(Transform seg)
        {
            // Large broken pillar stumps on bridge sides (outside railings)
            if (Random.value < 0.6f)
            {
                float side = Random.value < .5f ? -1f : 1f;
                float rz   = Random.Range(5f, SegLen - 5f);
                float rx   = side * (PathW * .5f + 1.5f);

                // Toppled pillar pieces
                float pieceSc = Random.Range(.7f, 1.2f);
                Prim(PrimitiveType.Cylinder, seg,
                    new Vector3(rx, -1f, rz),
                    new Vector3(.9f * pieceSc, .6f, .9f * pieceSc), ruinMat, false);
                // Toppled section (rotated on its side)
                var fallen = Prim(PrimitiveType.Cylinder, seg,
                    new Vector3(rx + side * 1.2f, -.4f, rz + .5f),
                    new Vector3(.7f * pieceSc, 1.8f * pieceSc, .7f * pieceSc), ruinMat, false);
                fallen.transform.localRotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);

                // Golden temple carving block
                Prim(PrimitiveType.Cube, seg,
                    new Vector3(rx, .2f, rz + 1.5f),
                    new Vector3(1f * pieceSc, .5f, .5f * pieceSc), goldRuinMat, false);
            }

            // Background arch/doorway ruins (distant, atmospheric)
            if (Random.value < 0.4f)
            {
                float archZ = Random.Range(8f, SegLen - 8f);
                float archX = (Random.value < .5f ? -1f : 1f) * Random.Range(5f, 10f);
                BuildArch(seg, new Vector3(archX, 0f, archZ));
            }
        }

        private void BuildArch(Transform seg, Vector3 pos)
        {
            float w = Random.Range(3f, 5f);
            float h = Random.Range(5f, 8f);
            // Left pillar
            Prim(PrimitiveType.Cylinder, seg,
                pos + new Vector3(-w * .5f, h * .5f, 0f),
                new Vector3(.6f, h * .5f, .6f), ruinMat, false);
            // Right pillar
            Prim(PrimitiveType.Cylinder, seg,
                pos + new Vector3(w * .5f, h * .5f, 0f),
                new Vector3(.6f, h * .5f, .6f), ruinMat, false);
            // Arch top
            Prim(PrimitiveType.Cube, seg,
                pos + new Vector3(0f, h, 0f),
                new Vector3(w + .6f, .5f, .5f), goldRuinMat, false);
            // Gold ornament
            Prim(PrimitiveType.Sphere, seg,
                pos + new Vector3(0f, h + .55f, 0f),
                Vector3.one * .6f, goldRuinMat, false);
        }

        // ── Torches ───────────────────────────────────────────────────────────
        private void PlaceTorches(Transform seg)
        {
            int torchCount = Random.Range(2, 5);
            for (int t = 0; t < torchCount; t++)
            {
                float side = (t % 2 == 0) ? -1f : 1f;
                float tz   = 4f + t * (SegLen / torchCount);
                BuildTorch(seg, new Vector3(side * (PathW * .5f - 0.1f), BridgeH, tz));
            }
        }

        private void BuildTorch(Transform seg, Vector3 pos)
        {
            float h = 1.4f;
            // Pole
            Prim(PrimitiveType.Cylinder, seg,
                pos + new Vector3(0f, h * .5f, 0f),
                new Vector3(0.12f, h * .5f, 0.12f), torchBaseMat, false);
            // Bracket
            Prim(PrimitiveType.Cube, seg,
                pos + new Vector3(0f, h, 0f),
                new Vector3(0.3f, 0.24f, 0.3f), torchBaseMat, false);
            // Fire glow (emissive sphere)
            Prim(PrimitiveType.Sphere, seg,
                pos + new Vector3(0f, h + .28f, 0f),
                new Vector3(.22f, .32f, .22f), fireMat, false);
            // Outer glow corona
            Prim(PrimitiveType.Sphere, seg,
                pos + new Vector3(0f, h + .32f, 0f),
                Vector3.one * .45f, MatFactory.Glow(new Color(1f, .3f, .05f), 1.2f), false);
        }

        // ── Obstacles & Pickups ───────────────────────────────────────────────
        private void SpawnHazardsAndPickups(Transform seg)
        {
            int   pattern  = Random.Range(0, 4);
            float obsZ     = Random.Range(12f, 18f);
            int   safeLane = Random.Range(0, 3);
            float[] lanes  = LaneX;

            switch (pattern)
            {
                case 0: // One boulder / crumbled pillar
                    SpawnObstacle(seg, (safeLane + 1) % 3, obsZ);
                    SpawnCoinLine(seg, safeLane, obsZ - 5f, 5);
                    break;

                case 1: // Two obstacles, one safe lane
                    for (int l = 0; l < 3; l++) if (l != safeLane) SpawnObstacle(seg, l, obsZ);
                    SpawnCoinLine(seg, safeLane, obsZ - 4f, 4);
                    break;

                case 2: // Low barrier (jump over)
                    SpawnLowBarrier(seg, safeLane, obsZ);
                    SpawnArcCoins(seg, safeLane, obsZ);
                    break;

                case 3: // Swinging rope / dangling statue (slide under clearance)
                    SpawnHangingObstacle(seg, lanes[safeLane], obsZ);
                    SpawnCoinLine(seg, safeLane, obsZ + 2f, 4);
                    break;
            }

            // Rare gem or power-up
            if (Random.value < 0.28f)
            {
                float pz = obsZ + Random.Range(7f, 12f);
                int   pl = Random.Range(0, 3);
                if (Random.value < .4f) SpawnPowerUp(seg, pl, pz);
                else                   SpawnGem(seg, pl, pz);
            }
        }

        private void SpawnObstacle(Transform seg, int lane, float z)
        {
            float x    = LaneX[lane];
            int   type = Random.Range(0, 3);

            if (type == 0) // Ancient stone idol / boulder
            {
                var go = Prim(PrimitiveType.Sphere, seg,
                    new Vector3(x, BridgeH + .9f, z), new Vector3(1.6f, 1.5f, 1.6f), ruinMat);
                go.GetComponent<Collider>().isTrigger = true;
                go.AddComponent<Obstacle>().Initialize(ObstacleType.Rock, 1.5f);

                // Gold markings on idol
                Prim(PrimitiveType.Sphere, seg,
                    new Vector3(x, BridgeH + 1.6f, z), Vector3.one * .55f, goldRuinMat, false);
            }
            else if (type == 1) // Broken pillar column
            {
                var go = Prim(PrimitiveType.Cylinder, seg,
                    new Vector3(x, BridgeH + 1.5f, z), new Vector3(.95f, 1.5f, .95f), ruinMat);
                go.GetComponent<Collider>().isTrigger = true;
                go.AddComponent<Obstacle>().Initialize(ObstacleType.AncientPillar, 3f);
                // Toppled cap
                Prim(PrimitiveType.Cube, seg,
                    new Vector3(x, BridgeH + 3.1f, z), new Vector3(1.4f, .25f, 1.4f), goldRuinMat, false);
            }
            else // Crumbled stone wall chunk
            {
                var go = Prim(PrimitiveType.Cube, seg,
                    new Vector3(x, BridgeH + .55f, z), new Vector3(1.4f, 1.1f, 0.9f), stoneMat);
                go.GetComponent<Collider>().isTrigger = true;
                go.AddComponent<Obstacle>().Initialize(ObstacleType.LowBarrier, 1.1f);
                // Moss on top
                Prim(PrimitiveType.Cube, seg,
                    new Vector3(x, BridgeH + 1.1f, z), new Vector3(1.3f, .08f, .8f), mossMat, false);
            }
        }

        private void SpawnLowBarrier(Transform seg, int lane, float z)
        {
            float x  = LaneX[lane];
            var   go = Prim(PrimitiveType.Cube, seg,
                new Vector3(x, BridgeH + .36f, z), new Vector3(1.5f, .72f, .55f), stoneMat);
            go.GetComponent<Collider>().isTrigger = true;
            go.AddComponent<Obstacle>().Initialize(ObstacleType.FallenLog, .72f);

            // Mossy top
            Prim(PrimitiveType.Cube, seg,
                new Vector3(x, BridgeH + .73f, z), new Vector3(1.4f, .07f, .5f), mossMat, false);
            // Carved stone face on wall
            Prim(PrimitiveType.Sphere, seg,
                new Vector3(x, BridgeH + .5f, z - .3f), new Vector3(.5f, .45f, .3f), goldRuinMat, false);
        }

        private void SpawnHangingObstacle(Transform seg, float x, float z)
        {
            // Dangling rope / statue that the player must avoid
            float ropeTop = 5.5f;
            // Rope segments
            for (int r = 0; r < 5; r++)
                Prim(PrimitiveType.Cylinder, seg,
                    new Vector3(x, ropeTop - r * .8f, z),
                    new Vector3(.08f, .45f, .08f), vineMat, false);

            // Hanging idol at bottom
            var go = Prim(PrimitiveType.Sphere, seg,
                new Vector3(x, ropeTop - 5 * .8f, z), new Vector3(.9f, 1.2f, .9f), goldRuinMat);
            go.GetComponent<Collider>().isTrigger = true;
            go.AddComponent<Obstacle>().Initialize(ObstacleType.Rock, 1.5f);
        }

        private void SpawnCoinLine(Transform seg, int lane, float startZ, int count)
        {
            float x = LaneX[lane];
            for (int i = 0; i < count; i++)
                AddCoin(seg, new Vector3(x, BridgeH + .7f, startZ + i * 1.8f));
        }

        private void SpawnArcCoins(Transform seg, int lane, float cz)
        {
            float x  = LaneX[lane];
            float[] h = { .7f, 1.0f, 1.5f, 1.9f, 2.2f, 1.9f, 1.5f, 1.0f, .7f };
            for (int i = 0; i < h.Length; i++)
                AddCoin(seg, new Vector3(x, BridgeH + h[i], cz - 4f + i * 1.0f));
        }

        private void SpawnGem(Transform seg, int lane, float z)
        {
            float x  = LaneX[lane];
            var   go = Prim(PrimitiveType.Sphere, seg,
                new Vector3(x, BridgeH + .7f, z), Vector3.one * .52f, gemMat);
            go.GetComponent<Collider>().isTrigger = true;
            go.AddComponent<Collectible>().Type    = CollectibleType.Gem;
            // Orbiting ring
            Prim(PrimitiveType.Cylinder, seg,
                new Vector3(x, BridgeH + .7f, z),
                new Vector3(1.05f, .05f, 1.05f), gemMat, false);
        }

        private void SpawnPowerUp(Transform seg, int lane, float z)
        {
            float x  = LaneX[lane];
            var   go = Prim(PrimitiveType.Cube, seg,
                new Vector3(x, BridgeH + .7f, z), Vector3.one * .65f, puMat);
            go.transform.localRotation = Quaternion.Euler(30f, 45f, 0f);
            go.GetComponent<Collider>().isTrigger = true;
            var c = go.AddComponent<Collectible>();
            c.Type = CollectibleType.PowerUp;
            PowerUpType[] types = { PowerUpType.Magnet, PowerUpType.Shield,
                                    PowerUpType.SpeedBoost, PowerUpType.DoubleScore, PowerUpType.Fly };
            c.PowerUpVariant = types[Random.Range(0, types.Length)];
            Prim(PrimitiveType.Cylinder, seg,
                new Vector3(x, BridgeH + .7f, z),
                new Vector3(1.2f, .06f, 1.2f), puMat, false);
        }

        private void AddCoin(Transform seg, Vector3 pos)
        {
            var go = Prim(PrimitiveType.Cylinder, seg, pos,
                new Vector3(.42f, .065f, .42f), coinMat);
            go.GetComponent<Collider>().isTrigger = true;
            go.AddComponent<Collectible>().Type   = CollectibleType.Coin;
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static GameObject Prim(PrimitiveType type, Transform parent,
            Vector3 localPos, Vector3 scale, Material mat, bool keepCollider = true)
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
