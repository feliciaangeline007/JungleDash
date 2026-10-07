// World.cs - High-fidelity jungle temple track: ancient stone pavement, curbs, ruins, torch braziers, lush canopy & detailed obstacles.
using System.Collections.Generic;
using UnityEngine;

namespace JungleDash
{
    public class World : MonoBehaviour
    {
        public const float LaneW = 2.3f;
        public const float SegLen = 42f;
        const int SegCount = 5;

        public enum K { Log, Boulder, Vine, Coin, Gem, Shield, Magnet, Speed, Fly, Double }

        public class Item
        {
            public K kind;
            public GameObject go;
            public float x, y, z;
            public float hw, hz, y0, y1;   // obstacle half sizes / vertical range
            public float seed;
            public bool IsObstacle { get { return kind == K.Log || kind == K.Boulder || kind == K.Vine; } }
        }

        public System.Action<Item> OnPickup, OnHit;

        readonly List<Item> items = new List<Item>(160);
        readonly Dictionary<K, Stack<Item>> pool = new Dictionary<K, Stack<Item>>();
        readonly Transform[] segs = new Transform[SegCount];
        Transform itemRoot;
        float nextRowZ;
        GameAssets ga;

        Material mStonePath, mStoneCurb, mStoneArch, mGrass, mRock, mBark, mCut, mLeaf, mLeaf2, mMoss, mVine, mFlower, mFlower2;
        Material mGold, mGem, mShield, mMagnet, mMagnetTip, mPalm, mBroad, mTorchFire;
        Material mSpeed, mFly, mDouble;

        // ------------------------------------------------------------------ setup
        public void Init(GameAssets assets)
        {
            ga = assets;

            // AI-generated photorealistic textures with robust fallback
            Texture2D stonePath = (ga != null && ga.stonePath != null) ? ga.stonePath : Resources.Load<Texture2D>("StonePath");
            if (stonePath == null && ga != null) stonePath = ga.mud;
            if (stonePath == null) stonePath = Resources.Load<Texture2D>("MudRocky");

            Texture2D carvedStone = (ga != null && ga.carvedStone != null) ? ga.carvedStone : Resources.Load<Texture2D>("CarvedStone");
            if (carvedStone == null) carvedStone = stonePath;

            Texture2D grass = (ga != null && ga.jungleGrass != null) ? ga.jungleGrass : Resources.Load<Texture2D>("JungleGrass");
            if (grass == null && ga != null) grass = ga.grass;
            if (grass == null) grass = Resources.Load<Texture2D>("GrassHill");

            Texture2D bark = (ga != null && ga.treeBark != null) ? ga.treeBark : Resources.Load<Texture2D>("TreeBark");
            Texture2D relic = (ga != null && ga.goldRelic != null) ? ga.goldRelic : Resources.Load<Texture2D>("GoldRelic");

            Texture2D palm = ga != null && ga.palm != null ? ga.palm : Resources.Load<Texture2D>("PalmBillboard");
            Texture2D broadleaf = ga != null && ga.broadleaf != null ? ga.broadleaf : Resources.Load<Texture2D>("BroadleafBillboard");

            // Refined architectural & nature materials using AI-generated textures
            mStonePath = Mats.Get(MatKind.Opaque, new Color(0.72f, 0.68f, 0.62f), stonePath, 2.5f, SegLen / 4.5f, 0.2f);
            mStoneCurb = Mats.Get(MatKind.Opaque, new Color(0.65f, 0.62f, 0.58f), carvedStone, 1f, SegLen / 5f, 0.25f);
            mStoneArch = Mats.Get(MatKind.Opaque, new Color(0.70f, 0.68f, 0.64f), carvedStone, 1.2f, 1.2f, 0.2f);
            mGrass = Mats.Get(MatKind.Opaque, new Color(0.55f, 0.85f, 0.45f), grass, 8f, SegLen / 3f, 0.08f);
            mRock = Mats.Get(MatKind.Opaque, new Color(0.65f, 0.62f, 0.58f), carvedStone, 1.5f, 1.5f, 0.15f);
            mBark = Mats.Get(MatKind.Opaque, new Color(0.80f, 0.70f, 0.60f), bark, 1f, 1.5f, 0.12f);
            mCut = Mats.Flat(new Color(0.82f, 0.60f, 0.35f), 0.15f);
            mLeaf = Mats.Flat(new Color(0.14f, 0.52f, 0.18f), 0.12f);
            mLeaf2 = Mats.Flat(new Color(0.24f, 0.65f, 0.20f), 0.15f);
            mMoss = Mats.Flat(new Color(0.20f, 0.50f, 0.16f), 0.08f);
            mVine = Mats.Flat(new Color(0.18f, 0.48f, 0.16f), 0.12f);
            mFlower = Mats.Get(MatKind.Emissive, new Color(1f, 0.25f, 0.45f), null, 1, 1, 0.3f, 0.8f);
            mFlower2 = Mats.Get(MatKind.Emissive, new Color(1f, 0.82f, 0.20f), null, 1, 1, 0.3f, 0.8f);
            mTorchFire = Mats.Get(MatKind.Emissive, new Color(1f, 0.55f, 0.08f), null, 1, 1, 0.9f, 2.0f);

            // Power-ups and collectibles with ornate gold relic texture
            mGold = Mats.Get(MatKind.Emissive, new Color(1f, 0.85f, 0.25f), relic, 1f, 1f, 0.85f, 0.6f);
            mGem = Mats.Get(MatKind.Emissive, new Color(0.15f, 0.95f, 1f), null, 1, 1, 0.9f, 1.2f);
            mShield = Mats.Get(MatKind.Emissive, new Color(0.25f, 0.60f, 1f), null, 1, 1, 0.85f, 1.3f);
            mMagnet = Mats.Get(MatKind.Emissive, new Color(1f, 0.22f, 0.22f), null, 1, 1, 0.7f, 1.0f);
            mMagnetTip = Mats.Flat(new Color(0.95f, 0.95f, 0.95f), 0.75f);
            mSpeed = Mats.Get(MatKind.Emissive, new Color(1f, 0.58f, 0.08f), null, 1, 1, 0.9f, 1.5f);
            mFly = Mats.Get(MatKind.Emissive, new Color(0.85f, 0.45f, 1f), null, 1, 1, 0.9f, 1.6f);
            mDouble = Mats.Get(MatKind.Emissive, new Color(1f, 0.92f, 0.2f), null, 1, 1, 0.9f, 1.5f);

            if (palm != null) mPalm = Mats.Get(MatKind.Cutout, Color.white, palm, 1, 1, 0.05f);
            if (broadleaf != null) mBroad = Mats.Get(MatKind.Cutout, Color.white, broadleaf, 1, 1, 0.05f);

            itemRoot = new GameObject("Items").transform;
            itemRoot.SetParent(transform, false);

            for (int i = 0; i < SegCount; i++) segs[i] = BuildSegment(i);
            Reset();
        }

        public void Reset()
        {
            if (items != null)
            {
                for (int i = items.Count - 1; i >= 0; i--)
                {
                    if (items[i] != null) Despawn(items[i], i);
                }
            }
            if (segs != null)
            {
                for (int i = 0; i < SegCount; i++)
                {
                    if (segs[i] != null) segs[i].position = new Vector3(0, 0, (i - 1) * SegLen);
                }
            }
            nextRowZ = 28f;
        }

        // ------------------------------------------------------------------ per frame
        public void Tick(float dt, float playerZ, float speed, bool spawn)
        {
            // recycle scenery
            for (int i = 0; i < SegCount; i++)
            {
                if (segs[i] != null && segs[i].position.z + SegLen < playerZ - 12f)
                {
                    float max = float.MinValue;
                    for (int j = 0; j < SegCount; j++)
                    {
                        if (segs[j] != null) max = Mathf.Max(max, segs[j].position.z);
                    }
                    segs[i].position = new Vector3(0, 0, max + SegLen);
                }
            }

            if (spawn)
                while (nextRowZ < playerZ + 140f) nextRowZ += SpawnPattern(nextRowZ, speed);

            // animate pickups and powerups
            float t = Time.time;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                Item it = items[i];
                if (it == null) { items.RemoveAt(i); continue; }
                if (it.z < playerZ - 14f) { Despawn(it, i); continue; }
                if (it.IsObstacle) continue;
                if (it.go == null) continue;

                Transform tr = it.go.transform;
                float bob = Mathf.Sin(t * 3.5f + it.seed) * 0.1f;
                tr.position = new Vector3(it.x, it.y + bob, it.z);
                tr.rotation = Quaternion.Euler(0f, t * 160f + it.seed * 57f, 0f);
            }
        }

        /// <summary>Tests the player's box against close obstacles and pulls coins.</summary>
        public void Collide(Player p, bool magnet, bool invulnerable, float dt)
        {
            const float PW = 0.38f, PZ = 0.32f;
            float px = p.x, py = p.y, pz = p.Z, ph = p.Height;

            for (int i = items.Count - 1; i >= 0; i--)
            {
                Item it = items[i];
                if (it == null) continue;
                float dz = it.z - pz;
                if (it.IsObstacle)
                {
                    if (invulnerable || dz > 4f || dz < -4f) continue;
                    if (Mathf.Abs(it.x - px) < it.hw + PW && Mathf.Abs(dz) < it.hz + PZ &&
                        py + ph > it.y0 && py < it.y1)
                    {
                        if (OnHit != null) OnHit(it);
                        return;
                    }
                    continue;
                }

                bool pullCoins = (magnet || p.flying);
                if (pullCoins && (it.kind == K.Coin || it.kind == K.Gem) && dz < 11f && dz > -3f)
                {
                    Vector3 target = new Vector3(px, py + ph * 0.5f, pz);
                    Vector3 pos = new Vector3(it.x, it.y, it.z);
                    if ((target - pos).sqrMagnitude < 11f * 11f)
                    {
                        pos = Vector3.MoveTowards(pos, target, 28f * dt);
                        it.x = pos.x; it.y = pos.y; it.z = pos.z;
                        dz = it.z - pz;
                    }
                }

                if (dz > 1.3f || dz < -1.3f) continue;
                if (Mathf.Abs(it.x - px) < 0.95f && Mathf.Abs(dz) < 1.0f &&
                    it.y > py - 0.45f && it.y < py + ph + 0.45f)
                {
                    Item picked = it;
                    if (OnPickup != null) OnPickup(picked);
                    Despawn(picked, i);
                }
            }
        }

        public void Remove(Item it)
        {
            int idx = items.IndexOf(it);
            if (idx >= 0) Despawn(it, idx);
        }

        // ------------------------------------------------------------------ obstacle / pickup spawning
        float SpawnPattern(float z, float speed)
        {
            float gap = Mathf.Lerp(15f, 26f, (speed - 10.5f) / 14f);
            int roll = Random.Range(0, 100);
            int lane = Random.Range(0, 3);
            float len = 0f;
            bool early = z < 80f;

            if (early) roll = roll < 55 ? 0 : 30;

            if (roll < 24)                       // coin line
            {
                len = CoinLine(lane, z, 8);
                return len + gap * 0.6f;
            }
            if (roll < 40)                       // log + coin arc over it
            {
                Place(K.Log, lane, z);
                for (int i = 0; i < 5; i++)
                {
                    float a = i / 4f * Mathf.PI;
                    Coin(lane, 0.9f + Mathf.Sin(a) * 1.5f, z - 2.5f + i * 1.3f);
                }
                return gap + 3f;
            }
            if (roll < 52)                       // double log, one free lane
            {
                for (int l = 0; l < 3; l++) if (l != lane) Place(K.Log, l, z);
                CoinLine(lane, z - 4f, 5);
                return gap + 2f;
            }
            if (roll < 66)                       // two boulders, run through the gap
            {
                for (int l = 0; l < 3; l++) if (l != lane) Place(K.Boulder, l, z);
                CoinLine(lane, z - 3f, 7);
                return gap + 3f;
            }
            if (roll < 76)                       // low vine bar: slide under it
            {
                Place(K.Vine, 1, z);
                for (int i = -2; i <= 2; i++) if (i != 0) Coin(lane, 0.85f, z + i * 1.3f);
                return gap + 2f;
            }
            if (roll < 88)                       // power-up
            {
                float puRoll = Random.value;
                K pu;
                if (puRoll < 0.25f) pu = K.Shield;
                else if (puRoll < 0.48f) pu = K.Magnet;
                else if (puRoll < 0.68f) pu = K.Speed;
                else if (puRoll < 0.84f) pu = K.Fly;
                else pu = K.Double;

                Spawn(pu, lane * LaneW - LaneW, 1.1f, z);
                if (pu == K.Fly)
                {
                    for (int c = 0; c < 12; c++)
                        Spawn(K.Coin, lane * LaneW - LaneW, 3.5f, z + 5f + c * 2f);
                    return gap + 14f;
                }
                CoinLine(lane, z + 3f, 4);
                return gap;
            }
            if (roll < 94)                       // gems
            {
                for (int i = 0; i < 3; i++) Spawn(K.Gem, lane * LaneW - LaneW, 0.95f, z + i * 1.6f);
                Place(K.Log, (lane + 1) % 3, z + 1.6f);
                return gap + 2f;
            }
            // mixed: boulder + log + open lane
            {
                int a = (lane + 1) % 3, b = (lane + 2) % 3;
                Place(K.Boulder, a, z);
                Place(K.Log, b, z);
                CoinLine(lane, z - 3f, 6);
                return gap + 3f;
            }
        }

        float CoinLine(int lane, float z, int n)
        {
            for (int i = 0; i < n; i++) Coin(lane, 0.9f, z + i * 1.5f);
            return n * 1.5f;
        }

        void Coin(int lane, float y, float z) { Spawn(K.Coin, lane * LaneW - LaneW, y, z); }
        void Place(K k, int lane, float z) { Spawn(k, k == K.Vine ? 0f : lane * LaneW - LaneW, 0f, z); }

        void Spawn(K k, float x, float y, float z)
        {
            Stack<Item> st;
            Item it = (pool.TryGetValue(k, out st) && st.Count > 0) ? st.Pop() : Make(k);
            it.x = x; it.y = y; it.z = z;
            it.seed = Random.value * 6.28f;
            it.go.transform.position = new Vector3(x, y, z);
            it.go.transform.rotation = Quaternion.identity;
            it.go.SetActive(true);
            items.Add(it);
        }

        void Despawn(Item it, int index)
        {
            if (index >= 0 && index < items.Count) items.RemoveAt(index);
            if (it != null)
            {
                if (it.go != null) it.go.SetActive(false);
                Stack<Item> st;
                if (!pool.TryGetValue(it.kind, out st)) { st = new Stack<Item>(); pool[it.kind] = st; }
                st.Push(it);
            }
        }

        // ------------------------------------------------------------------ item visuals
        Item Make(K k)
        {
            GameObject root = new GameObject(k.ToString());
            root.transform.SetParent(itemRoot, false);
            Transform r = root.transform;
            Item it = new Item { kind = k, go = root };
            Vector3 none = Vector3.zero;

            switch (k)
            {
                case K.Log:
                    it.hw = 1.05f; it.hz = 0.42f; it.y0 = 0f; it.y1 = 0.95f;
                    // Textured log with bark & moss
                    Gfx.Prim(PrimitiveType.Cylinder, r, new Vector3(0, 0.42f, 0), new Vector3(0.85f, 1.05f, 0.85f), mBark, new Vector3(0, 0, 90));
                    Gfx.Prim(PrimitiveType.Cylinder, r, new Vector3(-1.06f, 0.42f, 0), new Vector3(0.82f, 0.05f, 0.82f), mCut, new Vector3(0, 0, 90));
                    Gfx.Prim(PrimitiveType.Cylinder, r, new Vector3(1.06f, 0.42f, 0), new Vector3(0.82f, 0.05f, 0.82f), mCut, new Vector3(0, 0, 90));
                    Gfx.Prim(PrimitiveType.Sphere, r, new Vector3(0.2f, 0.85f, 0), new Vector3(0.5f, 0.25f, 0.5f), mMoss, none, false);
                    break;

                case K.Boulder:
                    it.hw = 0.92f; it.hz = 0.85f; it.y0 = 0f; it.y1 = 1.8f;
                    // Multi-part rugged mossy monolith
                    Gfx.Prim(PrimitiveType.Sphere, r, new Vector3(0, 0.9f, 0), new Vector3(1.8f, 1.75f, 1.7f), mRock, new Vector3(15, 35, 10));
                    Gfx.Prim(PrimitiveType.Sphere, r, new Vector3(0.25f, 1.4f, 0.1f), new Vector3(1.1f, 0.8f, 1.0f), mMoss, new Vector3(5, 45, 0), false);
                    Gfx.Prim(PrimitiveType.Cube, r, new Vector3(-0.35f, 0.55f, 0.25f), new Vector3(0.7f, 0.7f, 0.7f), mRock, new Vector3(45, 25, 30));
                    break;

                case K.Vine:
                    it.hw = LaneW * 1.45f; it.hz = 0.35f; it.y0 = 0.75f; it.y1 = 2.4f;
                    // Low swinging jungle vine barrier with flowers
                    Gfx.Prim(PrimitiveType.Cylinder, r, new Vector3(0, 1.55f, 0), new Vector3(0.26f, LaneW * 1.5f, 0.26f), mVine, new Vector3(0, 0, 90));
                    for (int f = -2; f <= 2; f++)
                    {
                        Gfx.Prim(PrimitiveType.Sphere, r, new Vector3(f * 1.1f, 1.52f, 0.12f), new Vector3(0.35f, 0.45f, 0.25f), mLeaf, none, false);
                        if (f % 2 != 0)
                            Gfx.Prim(PrimitiveType.Sphere, r, new Vector3(f * 1.1f, 1.35f, 0.18f), new Vector3(0.22f, 0.22f, 0.22f), mFlower, none, false);
                    }
                    break;

                case K.Coin:
                    // Rich 3D golden spinning coin with star relief
                    Gfx.Prim(PrimitiveType.Cylinder, r, none, new Vector3(0.75f, 0.08f, 0.75f), mGold, new Vector3(90, 0, 0), false);
                    Gfx.Prim(PrimitiveType.Cube, r, new Vector3(0, 0, 0.05f), new Vector3(0.32f, 0.32f, 0.05f), mGold, new Vector3(0, 0, 45), false);
                    Gfx.Prim(PrimitiveType.Cube, r, new Vector3(0, 0, -0.05f), new Vector3(0.32f, 0.32f, 0.05f), mGold, new Vector3(0, 0, 45), false);
                    break;

                case K.Gem:
                    // Multi-faceted cut diamond
                    Gfx.Prim(PrimitiveType.Sphere, r, none, new Vector3(0.68f, 0.78f, 0.68f), mGem, new Vector3(45, 45, 0), false);
                    Gfx.Prim(PrimitiveType.Cube, r, none, new Vector3(0.5f, 0.5f, 0.5f), mGem, new Vector3(45, 0, 45), false);
                    break;

                case K.Shield:
                    // Floating orb with orbital ring
                    Gfx.Prim(PrimitiveType.Sphere, r, none, new Vector3(0.65f, 0.65f, 0.65f), mShield, none, false);
                    Gfx.Prim(PrimitiveType.Cylinder, r, none, new Vector3(1.1f, 0.05f, 1.1f), mShield, new Vector3(35, 0, 0), false);
                    break;

                case K.Magnet:
                    // Classic red horseshoe with silver tips
                    Gfx.Prim(PrimitiveType.Cylinder, r, none, new Vector3(0.85f, 0.12f, 0.85f), mMagnet, new Vector3(90, 0, 0), false);
                    Gfx.Prim(PrimitiveType.Cube, r, new Vector3(0, 0.15f, 0), new Vector3(0.55f, 0.6f, 0.3f), mStonePath, none, false); // cutout middle
                    Gfx.Prim(PrimitiveType.Cube, r, new Vector3(-0.35f, 0.4f, 0), new Vector3(0.25f, 0.16f, 0.25f), mMagnetTip, none, false);
                    Gfx.Prim(PrimitiveType.Cube, r, new Vector3(0.35f, 0.4f, 0), new Vector3(0.25f, 0.16f, 0.25f), mMagnetTip, none, false);
                    break;

                case K.Speed:
                    // Sleek rocket turbine
                    Gfx.Prim(PrimitiveType.Cylinder, r, none, new Vector3(0.32f, 0.85f, 0.32f), mSpeed, new Vector3(60, 0, 0), false);
                    Gfx.Prim(PrimitiveType.Sphere, r, new Vector3(0, 0.45f, 0.25f), new Vector3(0.48f, 0.48f, 0.48f), mSpeed, none, false);
                    Gfx.Prim(PrimitiveType.Sphere, r, new Vector3(0, -0.45f, -0.25f), new Vector3(0.35f, 0.35f, 0.35f), mGold, none, false);
                    break;

                case K.Fly:
                    // Ethereal winged crest
                    Gfx.Prim(PrimitiveType.Sphere, r, none, new Vector3(0.65f, 0.65f, 0.65f), mFly, none, false);
                    Gfx.Prim(PrimitiveType.Cylinder, r, new Vector3(-0.55f, 0.08f, 0), new Vector3(0.14f, 0.7f, 0.3f), mFly, new Vector3(25, 0, -45), false);
                    Gfx.Prim(PrimitiveType.Cylinder, r, new Vector3(0.55f, 0.08f, 0), new Vector3(0.14f, 0.7f, 0.3f), mFly, new Vector3(25, 0, 45), false);
                    break;

                case K.Double:
                    // Radiant solar star
                    Gfx.Prim(PrimitiveType.Cube, r, none, new Vector3(0.65f, 0.65f, 0.65f), mDouble, new Vector3(45, 45, 0), false);
                    Gfx.Prim(PrimitiveType.Cube, r, none, new Vector3(0.5f, 0.5f, 0.5f), mGold, new Vector3(0, 45, 45), false);
                    break;
            }
            root.SetActive(false);
            return it;
        }

        // ------------------------------------------------------------------ scenery architecture
        Transform BuildSegment(int idx)
        {
            System.Random rnd = new System.Random(9137 + idx * 331);
            Transform root = new GameObject("Segment" + idx).transform;
            root.SetParent(transform, false);

            float halfLen = SegLen * 0.5f;

            // 1. Main Ancient Stone Path
            Gfx.Prim(PrimitiveType.Cube, root, new Vector3(0, -0.2f, halfLen), new Vector3(7.4f, 0.4f, SegLen), mStonePath, default(Vector3), false, "StonePath");

            // 2. Raised Carved Stone Curbs (Left & Right)
            Gfx.Prim(PrimitiveType.Cube, root, new Vector3(-3.85f, 0.05f, halfLen), new Vector3(0.45f, 0.35f, SegLen), mStoneCurb, default(Vector3), true, "CurbLeft");
            Gfx.Prim(PrimitiveType.Cube, root, new Vector3(3.85f, 0.05f, halfLen), new Vector3(0.45f, 0.35f, SegLen), mStoneCurb, default(Vector3), true, "CurbRight");

            // 3. Side Terraced Grass Embankments
            for (int s = -1; s <= 1; s += 2)
            {
                Gfx.Prim(PrimitiveType.Cube, root, new Vector3(s * (4.1f + 16f), -0.35f, halfLen), new Vector3(32f, 0.5f, SegLen), mGrass, default(Vector3), false, "Grass");
                // Canyon Cliff Ridge
                Gfx.Prim(PrimitiveType.Cube, root, new Vector3(s * 19f, 2.8f, halfLen), new Vector3(8f, 6.5f, SegLen), mRock, new Vector3(0, 0, s * -15f), false, "CliffWall");
            }

            // 4. Ancient Temple Archway (Every other segment)
            if (idx % 2 == 1)
            {
                float archZ = SegLen * 0.5f;
                Transform arch = new GameObject("TempleArch").transform;
                arch.SetParent(root, false);
                arch.localPosition = new Vector3(0, 0, archZ);

                // Left & Right Pillars with carved base and capital
                for (int s = -1; s <= 1; s += 2)
                {
                    Gfx.Prim(PrimitiveType.Cube, arch, new Vector3(s * 3.85f, 2.7f, 0), new Vector3(0.85f, 5.4f, 0.85f), mStoneArch, default(Vector3), true, "Pillar");
                    Gfx.Prim(PrimitiveType.Cube, arch, new Vector3(s * 3.85f, 5.5f, 0), new Vector3(1.15f, 0.45f, 1.15f), mStoneArch, default(Vector3), true, "PillarCap");
                    Gfx.Prim(PrimitiveType.Cube, arch, new Vector3(s * 3.85f, 0.25f, 0), new Vector3(1.15f, 0.5f, 1.15f), mStoneArch, default(Vector3), true, "PillarBase");
                }
                // Spanning Lintel Crossbeam
                Gfx.Prim(PrimitiveType.Cube, arch, new Vector3(0, 5.85f, 0), new Vector3(8.9f, 0.75f, 1.1f), mStoneArch, default(Vector3), true, "Lintel");
                // Temple Central Crest Emblem
                Gfx.Prim(PrimitiveType.Cube, arch, new Vector3(0, 6.5f, 0), new Vector3(1.6f, 1.1f, 0.6f), mStoneArch, new Vector3(0, 0, 45), true, "Crest");
            }

            // 5. Flaming Torch Braziers along the sides
            for (int tz = 8; tz < SegLen; tz += 18)
            {
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector3 torchPos = new Vector3(s * 3.85f, 0, tz);
                    Transform torch = new GameObject("Brazier").transform;
                    torch.SetParent(root, false);
                    torch.localPosition = torchPos;

                    // Stone Pedestal
                    Gfx.Prim(PrimitiveType.Cylinder, torch, new Vector3(0, 0.75f, 0), new Vector3(0.42f, 0.75f, 0.42f), mStoneArch, default(Vector3), false, "Post");
                    // Bronze/Stone Fire Bowl
                    Gfx.Prim(PrimitiveType.Cylinder, torch, new Vector3(0, 1.55f, 0), new Vector3(0.68f, 0.15f, 0.68f), mStoneCurb, default(Vector3), false, "Bowl");
                    // Glowing Flame Sphere
                    Gfx.Prim(PrimitiveType.Sphere, torch, new Vector3(0, 1.8f, 0), new Vector3(0.38f, 0.55f, 0.38f), mTorchFire, default(Vector3), false, "Flame");
                }
            }

            // 6. Lush Tropical Vegetation (Palms, Shrubs, Boulders & Flowers)
            for (int s = -1; s <= 1; s += 2)
            {
                // Palm Trees
                for (int i = 0; i < 4; i++)
                {
                    Vector3 treePos = new Vector3(s * Gfx.R(rnd, 5.5f, 13f), 0, Gfx.R(rnd, 2f, SegLen - 2f));
                    float sc = Gfx.R(rnd, 1.0f, 1.5f);
                    PalmTree(root, treePos, sc, rnd);
                }

                // Billboards (SpeedTree / Fern foliage)
                for (int i = 0; i < 5; i++)
                {
                    bool palm = mPalm != null && (mBroad == null || rnd.Next(2) == 0);
                    Material bm = palm ? mPalm : mBroad;
                    if (bm == null) break;
                    float size = Gfx.R(rnd, 6.5f, 9.5f);
                    float bx = s * Gfx.R(rnd, 6.8f, 18f);
                    float bz = Gfx.R(rnd, 0f, SegLen);
                    Gfx.Prim(PrimitiveType.Quad, root, new Vector3(bx, size * 0.5f - 0.2f, bz), new Vector3(size, size, 1f), bm,
                             new Vector3(0, Gfx.R(rnd, -25f, 25f), 0), true, "Billboard");
                }

                // Lush Tropical Bushes
                for (int i = 0; i < 7; i++)
                {
                    float bx = s * Gfx.R(rnd, 4.4f, 8.5f), bz = Gfx.R(rnd, 0f, SegLen);
                    float bs = Gfx.R(rnd, 0.7f, 1.4f);
                    Gfx.Prim(PrimitiveType.Sphere, root, new Vector3(bx, bs * 0.35f, bz), new Vector3(bs * 1.4f, bs * 0.85f, bs * 1.2f),
                             rnd.Next(2) == 0 ? mLeaf : mLeaf2, default(Vector3), false, "Bush");
                }

                // Ancient Mossy Rocks
                for (int i = 0; i < 4; i++)
                {
                    float rs = Gfx.R(rnd, 0.7f, 1.6f);
                    Gfx.Prim(PrimitiveType.Sphere, root, new Vector3(s * Gfx.R(rnd, 4.5f, 10f), rs * 0.3f, Gfx.R(rnd, 0f, SegLen)),
                             new Vector3(rs * 1.3f, rs * 0.8f, rs * 1.1f), mRock, new Vector3(0, Gfx.R(rnd, 0f, 90f), 0), false, "Rock");
                }

                // Tropical Flowers
                for (int i = 0; i < 16; i++)
                {
                    float fx = s * Gfx.R(rnd, 4.3f, 11f), fz = Gfx.R(rnd, 0f, SegLen);
                    Gfx.Prim(PrimitiveType.Sphere, root, new Vector3(fx, 0.18f, fz), new Vector3(0.24f, 0.24f, 0.24f),
                             rnd.Next(2) == 0 ? mFlower : mFlower2, default(Vector3), false, "Flower");
                }
            }

            return root;
        }

        void PalmTree(Transform seg, Vector3 lp, float sc, System.Random rnd)
        {
            Transform t = new GameObject("PalmTree").transform;
            t.SetParent(seg, false);
            t.localPosition = lp;

            // Curved trunk
            float h = 5.2f * sc;
            Gfx.Prim(PrimitiveType.Cylinder, t, new Vector3(0, h * 0.5f, 0), new Vector3(0.48f * sc, h * 0.5f, 0.48f * sc), mBark);
            Gfx.Prim(PrimitiveType.Sphere, t, new Vector3(0, h, 0), new Vector3(0.85f * sc, 0.65f * sc, 0.85f * sc), mMoss);

            // Spreading palm fronds (layered leaf clusters)
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f + Gfx.R(rnd, -10f, 10f);
                float rad = angle * Mathf.Deg2Rad;
                Vector3 leafPos = new Vector3(Mathf.Sin(rad) * 1.8f * sc, h - 0.25f, Mathf.Cos(rad) * 1.8f * sc);
                Gfx.Prim(PrimitiveType.Sphere, t, leafPos, new Vector3(1.6f * sc, 0.35f * sc, 2.6f * sc),
                         i % 2 == 0 ? mLeaf : mLeaf2, new Vector3(15f, angle, 0), false, "Frond");
            }
        }
    }
}
