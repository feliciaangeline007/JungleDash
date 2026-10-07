// ProceduralAssets.cs
// All game textures generated 100% procedurally — no external files.
// Visual theme: Ancient Bioluminescent Jungle Temple
//   - Stone paths with glowing cyan rune cracks
//   - Carved temple stone with geometric tribal patterns
//   - Deep-grain tropical bark with bioluminescent blue moss
//   - Lush vivid jungle floor with neon flower speckles
//   - Aztec sun-disc gold relic coin
//   - Rich ochre mud with water-pooling cracks
//
using UnityEngine;

namespace JungleDash
{
    public static class ProceduralAssets
    {
        // ── Public API ────────────────────────────────────────────────────────
        public static void FillAssets(GameAssets ga)
        {
            if (ga == null) return;
            ga.stonePath   = MakeStonePath_(128, 128);
            ga.carvedStone = MakeCarvedStone_(128, 128);
            ga.treeBark    = MakeTreeBark_(64, 128);
            ga.jungleGrass = MakeJungleGrass_(128, 128);
            ga.goldRelic   = MakeGoldRelic_(64, 64);
            ga.mud         = MakeMud_(64, 64);
            ga.grass       = ga.jungleGrass;
            ga.cliff       = ga.carvedStone;
        }

        public static Texture2D MakeStonePath_(int w, int h)   => StonePath(w, h);
        public static Texture2D MakeCarvedStone_(int w, int h) => CarvedStone(w, h);
        public static Texture2D MakeTreeBark_(int w, int h)    => TreeBark(w, h);
        public static Texture2D MakeJungleGrass_(int w, int h) => JungleGrass(w, h);
        public static Texture2D MakeGoldRelic_(int w, int h)   => GoldRelic(w, h);
        public static Texture2D MakeMud_(int w, int h)         => Mud(w, h);

        // ── Ancient Bioluminescent Stone Path ────────────────────────────────
        // Dark basalt-like stone with glowing cyan rune cracks running through it
        static Texture2D StonePath(int w, int h)
        {
            var t = New(w, h);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float nx = x / (float)w;
                float ny = y / (float)h;

                // Base: dark warm basalt
                float rock = FBM(nx * 7f, ny * 7f, 5);
                float base_ = 0.22f + rock * 0.10f;
                var c = new Color(base_ * 0.92f, base_ * 0.88f, base_ * 0.85f);

                // Large tile grid with bevelled edges
                float tx = Mod(nx * 2.5f, 1f);
                float ty = Mod(ny * 1.8f + Mathf.Floor(nx * 2.5f) * 0.5f, 1f);
                float border = Mathf.Max(
                    SmoothEdge(tx, 0.04f, 0.96f),
                    SmoothEdge(ty, 0.04f, 0.96f));
                c = Color.Lerp(c, new Color(0.10f, 0.10f, 0.11f), border * 0.7f);

                // Glowing cyan rune cracks — the key visual element
                float crackField = FBM(nx * 18f + 3.3f, ny * 18f + 7.1f, 3);
                float crackMask  = FBM(nx * 6f  + 1.1f, ny * 6f  + 4.5f, 2);
                bool inCrack = crackField > 0.68f && crackMask > 0.45f;
                if (inCrack)
                {
                    float glow = (crackField - 0.68f) / 0.32f;
                    glow = Mathf.Pow(glow, 0.5f);
                    // Cyan bioluminescent glow
                    var runeCol = new Color(0.05f + glow * 0.1f, 0.7f * glow, 0.9f * glow);
                    c = Color.Lerp(c, runeCol, glow * 0.85f);
                }

                // Subtle worn highlight
                float worn = Noise(nx * 22f, ny * 22f);
                if (worn > 0.78f) c += new Color(0.04f, 0.04f, 0.04f) * ((worn - 0.78f) * 4f);

                px[y * w + x] = c;
            }
            t.SetPixels(px); t.Apply(); return t;
        }

        // ── Carved Temple Stone ───────────────────────────────────────────────
        // Warm sandstone with deep geometric tribal carvings and glowing inlays
        static Texture2D CarvedStone(int w, int h)
        {
            var t = New(w, h);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float nx = x / (float)w;
                float ny = y / (float)h;

                // Base: warm sandstone
                float noise = FBM(nx * 5f, ny * 5f, 4);
                float base_ = 0.62f + noise * 0.16f;
                var c = new Color(base_, base_ * 0.88f, base_ * 0.70f);

                // Geometric tribal diamond grid
                float gx = Mod(nx * 4f + ny * 4f, 1f);
                float gy = Mod(nx * 4f - ny * 4f, 1f);
                float diamond = Mathf.Max(Mathf.Abs(gx - 0.5f), Mathf.Abs(gy - 0.5f));
                float carve = 1f - Mathf.SmoothStep(0.35f, 0.5f, diamond);
                c = Color.Lerp(c, new Color(0.32f, 0.26f, 0.18f), carve * 0.55f);

                // Horizontal stacked-block lines (temple masonry)
                float blockLine = 1f - SmoothEdge(Mod(ny * 5f, 1f), 0.05f, 0.95f);
                c = Color.Lerp(c, new Color(0.28f, 0.22f, 0.15f), blockLine * 0.4f);

                // Amber-orange glowing rune inlays in the diamond centres
                float inlay = Mathf.Max(0, 0.12f - diamond) / 0.12f;
                if (inlay > 0.3f)
                {
                    var glowAmber = new Color(1f, 0.65f + inlay * 0.2f, 0.05f * inlay);
                    c = Color.Lerp(c, glowAmber, inlay * 0.7f);
                }

                // Age staining
                float age = FBM(nx * 3f + 8f, ny * 3f + 2f, 3);
                c = Color.Lerp(c, new Color(0.35f, 0.45f, 0.22f), Mathf.Max(0, age - 0.65f) * 0.35f);

                px[y * w + x] = c;
            }
            t.SetPixels(px); t.Apply(); return t;
        }

        // ── Bioluminescent Tree Bark ──────────────────────────────────────────
        // Deep mahogany bark with glowing blue-green bioluminescent moss in crevices
        static Texture2D TreeBark(int w, int h)
        {
            var t = New(w, h);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float nx = x / (float)w;
                float ny = y / (float)h;

                // Deep mahogany base with vertical grain
                float grain = FBM(nx * 14f, ny * 1.5f, 5);
                float streak = Mathf.Sin((nx + grain * 0.12f) * 20f * Mathf.PI) * 0.5f + 0.5f;
                float base_ = Mathf.Lerp(0.20f, 0.42f, streak);
                var c = new Color(base_ * 0.85f, base_ * 0.55f, base_ * 0.32f);

                // Deep fissures
                float fiss = Noise(nx * 10f, ny * 20f);
                if (fiss > 0.62f)
                {
                    float d = (fiss - 0.62f) / 0.38f;
                    c = Color.Lerp(c, new Color(0.08f, 0.05f, 0.03f), d * 0.75f);

                    // Bioluminescent blue-green glow inside deep fissures
                    if (d > 0.5f)
                    {
                        float bio = (d - 0.5f) / 0.5f;
                        var bioCol = new Color(0.0f, bio * 0.85f, bio * 0.65f);
                        c = Color.Lerp(c, bioCol, bio * 0.6f);
                    }
                }

                // Horizontal bark ridges
                float ridge = Mathf.Abs(Mathf.Sin(ny * 12f * Mathf.PI)) * 0.5f + 0.5f;
                float ridgeAmt = FBM(nx * 6f, ny * 4f, 2) * 0.08f;
                c += new Color(ridgeAmt * ridge, ridgeAmt * ridge * 0.6f, 0);

                // Large knot cluster
                float kx = nx - 0.5f; float ky = ny - 0.38f;
                float knotD = Mathf.Sqrt(kx*kx*5f + ky*ky);
                float knot = Mathf.Exp(-knotD * 5f) * 0.4f;
                float kRing = Mathf.Abs(Mathf.Sin(knotD * 22f)) * Mathf.Exp(-knotD * 3.5f);
                c = Color.Lerp(c, new Color(0.12f, 0.07f, 0.04f), knot);
                c = Color.Lerp(c, new Color(0.05f, 0.04f, 0.02f), kRing * 0.6f);

                px[y * w + x] = c;
            }
            t.SetPixels(px); t.Apply(); return t;
        }

        // ── Vivid Bioluminescent Jungle Floor ────────────────────────────────
        // Rich layered tropical ground: deep greens + neon teal flower speckles
        static Texture2D JungleGrass(int w, int h)
        {
            var t = New(w, h);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float nx = x / (float)w;
                float ny = y / (float)h;

                // Layered green base
                float n1 = FBM(nx * 9f, ny * 9f, 5);
                float n2 = FBM(nx * 4f + 3.7f, ny * 4f + 1.2f, 3);
                float green = 0.38f + n1 * 0.25f;
                var c = new Color(0.05f + n2 * 0.06f, green, 0.06f + n1 * 0.05f);

                // Dark humus patches
                if (n1 < 0.28f)
                    c = Color.Lerp(c, new Color(0.10f, 0.16f, 0.06f), (0.28f - n1) * 2.5f);

                // Fallen leaf debris: warm amber-brown scatter
                float leaf = Noise(nx * 28f + 5.1f, ny * 28f + 2.8f);
                if (leaf > 0.80f)
                    c = Color.Lerp(c, new Color(0.55f, 0.35f, 0.08f), (leaf - 0.80f) * 4f);

                // Neon teal bioluminescent flower speckles
                float flower = Noise(nx * 35f + 9f, ny * 35f + 4f);
                if (flower > 0.88f)
                {
                    float bright = (flower - 0.88f) / 0.12f;
                    c = Color.Lerp(c, new Color(0.0f, 0.9f * bright, 0.7f * bright), bright * 0.9f);
                }

                // Tiny pebbles
                float pebble = Noise(nx * 20f + 1.5f, ny * 20f + 7f);
                if (pebble > 0.84f)
                    c = Color.Lerp(c, new Color(0.42f, 0.40f, 0.38f), (pebble - 0.84f) * 5f);

                // Subtle moisture variation (darker wet zones)
                float wet = FBM(nx * 3f + 6f, ny * 3f + 11f, 2);
                if (wet < 0.38f)
                    c = Color.Lerp(c, new Color(0.02f, 0.20f, 0.08f), (0.38f - wet) * 0.8f);

                px[y * w + x] = c;
            }
            t.SetPixels(px); t.Apply(); return t;
        }

        // ── Aztec Sun-Disc Gold Relic ─────────────────────────────────────────
        // Golden coin with Aztec calendar-style concentric rings + sun god face
        static Texture2D GoldRelic(int w, int h)
        {
            var t = New(w, h);
            var px = new Color[w * h];
            Color goldBase  = new Color(0.78f, 0.52f, 0.04f);
            Color goldMid   = new Color(0.98f, 0.80f, 0.16f);
            Color goldBright = new Color(1.00f, 0.97f, 0.72f);
            Color goldShadow = new Color(0.40f, 0.26f, 0.02f);

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float nx = x / (float)w * 2f - 1f;
                float ny = y / (float)h * 2f - 1f;
                float d  = Mathf.Sqrt(nx*nx + ny*ny);
                float angle = Mathf.Atan2(ny, nx);

                // Outer rim bevel
                float rimBevel = Mathf.SmoothStep(0.78f, 0.98f, d);

                // Aztec calendar rings (3 distinct bands)
                float ring1 = BandRing(d, 0.18f, 0.24f);
                float ring2 = BandRing(d, 0.44f, 0.52f);
                float ring3 = BandRing(d, 0.66f, 0.76f);

                // 20-point sun ray glyph (Aztec 20-day cycle)
                float rays20 = Mathf.Abs(Mathf.Sin(angle * 10f)) * Mathf.SmoothStep(0.75f, 0.0f, d);
                // 8-point cardinal rays (bolder)
                float rays8  = Mathf.Abs(Mathf.Sin(angle * 4f))  * Mathf.SmoothStep(0.65f, 0.0f, d) * 0.6f;

                // Spiral notch pattern in outer ring
                float spiral = Mathf.Abs(Mathf.Sin(angle * 16f + d * 12f)) * BandRing(d, 0.64f, 0.78f);

                // Coin surface noise
                float sNoise = FBM(nx * 6f + 2f, ny * 6f + 5f, 3) * 0.07f;

                float light = Mathf.Clamp01(
                    rays20 * 0.5f + rays8 * 0.35f + spiral * 0.4f
                    - ring1 * 0.5f - ring2 * 0.45f - ring3 * 0.4f
                    - rimBevel * 0.8f + sNoise + 0.35f);

                Color c = Color.Lerp(goldShadow, goldBase, light);
                c = Color.Lerp(c, goldMid,    Mathf.Max(0, light - 0.45f) * 2f);
                c = Color.Lerp(c, goldBright, Mathf.Max(0, light - 0.72f) * 3f);

                // Center sun-face eye pair
                float eyeL = Mathf.Max(0, 0.055f - Mathf.Sqrt((nx+0.12f)*(nx+0.12f) + (ny-0.06f)*(ny-0.06f))) / 0.055f;
                float eyeR = Mathf.Max(0, 0.055f - Mathf.Sqrt((nx-0.12f)*(nx-0.12f) + (ny-0.06f)*(ny-0.06f))) / 0.055f;
                float eyes = Mathf.Max(eyeL, eyeR);
                c = Color.Lerp(c, goldShadow, eyes * 0.8f);

                // Mouth arc
                float mouthX = nx, mouthY = ny + 0.20f;
                float mouthD = Mathf.Abs(Mathf.Sqrt(mouthX*mouthX + mouthY*mouthY) - 0.18f);
                float mouth = Mathf.SmoothStep(0.04f, 0f, mouthD) * Mathf.SmoothStep(0.16f, -0.16f, Mathf.Abs(mouthX));
                c = Color.Lerp(c, goldShadow, mouth * 0.7f);

                // Outside disc = black (alpha would need RGBA but RGB black is fine here)
                if (d > 0.97f) c = Color.black;

                px[y * w + x] = c;
            }
            t.SetPixels(px); t.Apply(); return t;
        }

        // ── Cracked Ochre Mud ────────────────────────────────────────────────
        // Warm ochre-terracotta dried mud with dramatic pooling water cracks
        static Texture2D Mud(int w, int h)
        {
            var t = New(w, h);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float nx = x / (float)w;
                float ny = y / (float)h;

                // Terracotta-ochre base
                float n = FBM(nx * 8f, ny * 8f, 4);
                float base_ = 0.52f + n * 0.14f;
                var c = new Color(base_ * 0.92f, base_ * 0.72f, base_ * 0.42f);

                // Voronoi-style crack cells (approximated with noise layers)
                float v1 = FBM(nx * 5f + 0.5f, ny * 5f + 0.5f, 3);
                float v2 = FBM(nx * 5f + 3.5f, ny * 5f + 2.1f, 3);
                float cell = Mathf.Abs(v1 - v2);
                if (cell < 0.06f)
                {
                    float depth = (0.06f - cell) / 0.06f;
                    // Deep crack = dark with hint of standing water (teal tint)
                    Color crackCol = Color.Lerp(
                        new Color(0.18f, 0.12f, 0.06f),
                        new Color(0.08f, 0.18f, 0.20f),
                        depth * 0.5f);
                    c = Color.Lerp(c, crackCol, depth * 0.88f);
                }

                // Surface shine variation (drier patches are lighter)
                float dry = FBM(nx * 4f + 9f, ny * 4f + 3f, 2);
                if (dry > 0.68f)
                    c = Color.Lerp(c, new Color(0.78f, 0.65f, 0.45f), (dry - 0.68f) * 0.9f);

                // Pebble inclusions
                float peb = Noise(nx * 22f + 4f, ny * 22f + 8f);
                if (peb > 0.83f)
                    c = Color.Lerp(c, new Color(0.50f, 0.46f, 0.40f), (peb - 0.83f) * 5f);

                px[y * w + x] = c;
            }
            t.SetPixels(px); t.Apply(); return t;
        }

        // ═════════════════════════════════════════════════════════════════════
        //  HELPERS
        // ═════════════════════════════════════════════════════════════════════
        static Texture2D New(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGB24, true);
            t.filterMode = FilterMode.Bilinear;
            t.wrapMode   = TextureWrapMode.Repeat;
            return t;
        }

        // 0..1 smooth edge: returns 0 inside (lo..hi), 1 outside
        static float SmoothEdge(float v, float lo, float hi)
        {
            float a = Mathf.SmoothStep(lo, lo + (hi-lo)*0.5f, v);
            float b = Mathf.SmoothStep(hi, hi - (hi-lo)*0.5f, v);
            return 1f - (a * b);
        }

        // Sharp band ring: 1 inside (inner..outer) band, 0 outside
        static float BandRing(float d, float inner, float outer)
        {
            return Mathf.SmoothStep(inner - 0.01f, inner + 0.01f, d)
                 * (1f - Mathf.SmoothStep(outer - 0.01f, outer + 0.01f, d));
        }

        static float Mod(float v, float m) => v - Mathf.Floor(v / m) * m;

        // ── Noise ─────────────────────────────────────────────────────────────
        static float Noise(float x, float y)
        {
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            float fx = x - ix, fy = y - iy;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            return Mathf.Lerp(
                Mathf.Lerp(H(ix,   iy),   H(ix+1, iy),   fx),
                Mathf.Lerp(H(ix,   iy+1), H(ix+1, iy+1), fx), fy);
        }

        static float FBM(float x, float y, int oct)
        {
            float v = 0, a = 0.5f, f = 1f, mx = 0;
            for (int i = 0; i < oct; i++)
            {
                v += Noise(x*f, y*f) * a;
                mx += a; a *= 0.5f; f *= 2f;
            }
            return v / mx;
        }

        static float H(int x, int y)
        {
            int n = x + y * 57;
            n = (n << 13) ^ n;
            return (1f - ((n * (n*n*15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824f) * 0.5f + 0.5f;
        }
    }
}
