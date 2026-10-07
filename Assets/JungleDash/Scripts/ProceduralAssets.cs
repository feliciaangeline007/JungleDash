// ProceduralAssets.cs
// Generates all game textures procedurally at runtime.
// No external image files required – everything is AI-style algorithmic generation.
// Called once at startup by Game.cs before World.Init() and Player.Build().
using UnityEngine;

namespace JungleDash
{
    public static class ProceduralAssets
    {
        // ── Public API ────────────────────────────────────────────────────────
        // Call this once; it fills a GameAssets ScriptableObject with generated textures.
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

        // Public wrappers so Editor scripts can also call individual generators
        public static Texture2D MakeStonePath_(int w, int h)   => MakeStonePath(w, h);
        public static Texture2D MakeCarvedStone_(int w, int h) => MakeCarvedStone(w, h);
        public static Texture2D MakeTreeBark_(int w, int h)    => MakeTreeBark(w, h);
        public static Texture2D MakeJungleGrass_(int w, int h) => MakeJungleGrass(w, h);
        public static Texture2D MakeGoldRelic_(int w, int h)   => MakeGoldRelic(w, h);
        public static Texture2D MakeMud_(int w, int h)         => MakeMud(w, h);

        // ── Stone Path ────────────────────────────────────────────────────────
        // Warm grey flagstone tiles with subtle cracks
        static Texture2D MakeStonePath(int w, int h)
        {
            Texture2D t = New(w, h);
            Color[] px = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float nx = x / (float)w;
                float ny = y / (float)h;

                // Tile grid
                float tx = Mathf.Repeat(nx * 3f, 1f);
                float ty = Mathf.Repeat(ny * 2f + (Mathf.Floor(nx * 3f) % 2f == 0f ? 0f : 0.5f), 1f);

                // Base stone colour
                float n = FBM(nx * 8f, ny * 8f, 4);
                float base_ = 0.58f + n * 0.12f;
                Color c = new Color(base_, base_ * 0.97f, base_ * 0.93f);

                // Grout lines
                float gx = Mathf.Abs(tx - 0.5f);
                float gy = Mathf.Abs(ty - 0.5f);
                float grout = Mathf.SmoothStep(0.46f, 0.5f, Mathf.Max(gx, gy));
                c = Color.Lerp(c, new Color(0.38f, 0.36f, 0.34f), grout * 0.55f);

                // Crack lines
                float crack = Noise(nx * 24f + 3.1f, ny * 24f + 7.3f);
                if (crack > 0.72f) c = Color.Lerp(c, new Color(0.3f, 0.28f, 0.26f), (crack - 0.72f) * 3f);

                // Moss patches
                float moss = FBM(nx * 5f + 11f, ny * 5f + 2.2f, 3);
                if (moss > 0.6f) c = Color.Lerp(c, new Color(0.28f, 0.48f, 0.22f), (moss - 0.6f) * 0.4f);

                px[y * w + x] = c;
            }
            t.SetPixels(px); t.Apply(); return t;
        }

        // ── Carved Stone ──────────────────────────────────────────────────────
        // Temple stone with geometric relief lines
        static Texture2D MakeCarvedStone(int w, int h)
        {
            Texture2D t = New(w, h);
            Color[] px = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float nx = x / (float)w;
                float ny = y / (float)h;

                float n = FBM(nx * 6f, ny * 6f, 5);
                float base_ = 0.52f + n * 0.15f;
                Color c = new Color(base_, base_ * 0.96f, base_ * 0.91f);

                // Horizontal carved lines (temple reliefs)
                float ly = Mathf.Repeat(ny * 8f, 1f);
                float line = Mathf.SmoothStep(0.0f, 0.07f, ly) * (1f - Mathf.SmoothStep(0.07f, 0.15f, ly));
                c = Color.Lerp(c, new Color(0.32f, 0.30f, 0.28f), line * 0.6f);

                // Vertical carved lines
                float lx = Mathf.Repeat(nx * 6f, 1f);
                float vline = Mathf.SmoothStep(0.0f, 0.05f, lx) * (1f - Mathf.SmoothStep(0.05f, 0.10f, lx));
                c = Color.Lerp(c, new Color(0.35f, 0.32f, 0.28f), vline * 0.35f);

                // Weathering
                float stain = FBM(nx * 3f + 5f, ny * 3f + 8f, 3);
                c = Color.Lerp(c, new Color(0.28f, 0.42f, 0.24f), Mathf.Max(0, stain - 0.6f) * 0.5f);

                px[y * w + x] = c;
            }
            t.SetPixels(px); t.Apply(); return t;
        }

        // ── Tree Bark ─────────────────────────────────────────────────────────
        // Vertical striated bark with knots
        static Texture2D MakeTreeBark(int w, int h)
        {
            Texture2D t = New(w, h);
            Color[] px = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float nx = x / (float)w;
                float ny = y / (float)h;

                // Vertical grain
                float grain = FBM(nx * 16f, ny * 2f, 4);
                float streak = Mathf.Sin((nx + grain * 0.1f) * 18f * Mathf.PI) * 0.5f + 0.5f;
                float base_ = Mathf.Lerp(0.28f, 0.52f, streak) + FBM(nx * 8f, ny * 8f, 3) * 0.08f;
                Color c = new Color(base_ * 0.9f, base_ * 0.72f, base_ * 0.5f);

                // Horizontal fissures
                float fiss = Noise(nx * 12f, ny * 24f);
                if (fiss > 0.65f)
                    c = Color.Lerp(c, new Color(0.18f, 0.14f, 0.09f), (fiss - 0.65f) * 2f);

                // Knots
                for (int k = 0; k < 2; k++)
                {
                    float kx = (k == 0) ? 0.3f : 0.7f;
                    float ky = (k == 0) ? 0.35f : 0.68f;
                    float d = Mathf.Sqrt((nx-kx)*(nx-kx)*4f + (ny-ky)*(ny-ky));
                    float ring = Mathf.Abs(Mathf.Sin(d * 18f)) * Mathf.Exp(-d * 4f);
                    c = Color.Lerp(c, new Color(0.20f, 0.15f, 0.10f), ring * 0.5f);
                }

                px[y * w + x] = c;
            }
            t.SetPixels(px); t.Apply(); return t;
        }

        // ── Jungle Grass ──────────────────────────────────────────────────────
        // Rich tropical ground cover with variation
        static Texture2D MakeJungleGrass(int w, int h)
        {
            Texture2D t = New(w, h);
            Color[] px = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float nx = x / (float)w;
                float ny = y / (float)h;

                float n  = FBM(nx * 10f, ny * 10f, 5);
                float n2 = FBM(nx * 5f  + 3.7f, ny * 5f + 1.2f, 3);

                // Base colour: bright tropical green
                float g = 0.55f + n * 0.20f;
                float r = 0.15f + n2 * 0.08f;
                float b = 0.10f + n * 0.05f;
                Color c = new Color(r, g, b);

                // Darker patches
                if (n < 0.3f) c = Color.Lerp(c, new Color(0.08f, 0.30f, 0.06f), (0.3f - n) * 1.5f);

                // Dry yellowish patches
                float dry = FBM(nx * 4f + 9f, ny * 4f + 2f, 2);
                if (dry > 0.72f) c = Color.Lerp(c, new Color(0.55f, 0.52f, 0.15f), (dry - 0.72f) * 1.2f);

                // Small stones
                float stone = Noise(nx * 20f + 1.5f, ny * 20f + 6.3f);
                if (stone > 0.82f) c = Color.Lerp(c, new Color(0.52f, 0.50f, 0.48f), (stone - 0.82f) * 3f);

                px[y * w + x] = c;
            }
            t.SetPixels(px); t.Apply(); return t;
        }

        // ── Gold Relic ────────────────────────────────────────────────────────
        // Ancient golden coin face with embossed motif
        static Texture2D MakeGoldRelic(int w, int h)
        {
            Texture2D t = New(w, h);
            Color[] px = new Color[w * h];
            Color goldDark  = new Color(0.72f, 0.50f, 0.05f);
            Color goldMid   = new Color(0.95f, 0.78f, 0.15f);
            Color goldLight = new Color(1.00f, 0.95f, 0.65f);

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float nx = (x / (float)w) * 2f - 1f;  // -1..1
                float ny = (y / (float)h) * 2f - 1f;
                float d  = Mathf.Sqrt(nx*nx + ny*ny);

                // Rim
                float rim = Mathf.SmoothStep(0.82f, 0.95f, d);
                // Concentric rings engraving
                float rings = Mathf.Abs(Mathf.Sin(d * 14f)) * Mathf.SmoothStep(0.85f, 0.0f, d);
                // Radial lines (sun motif)
                float angle = Mathf.Atan2(ny, nx);
                float radial = Mathf.Abs(Mathf.Sin(angle * 8f)) * (1f - d) * Mathf.SmoothStep(0.85f, 0.0f, d);
                // Surface noise
                float noise = FBM(nx * 8f + 2f, ny * 8f + 5f, 3) * 0.08f;

                float light = Mathf.Clamp01(radial * 0.45f + rings * 0.35f - rim * 0.6f + noise + 0.25f);
                Color c = Color.Lerp(goldDark, goldMid, light);
                c = Color.Lerp(c, goldLight, Mathf.Max(0, light - 0.6f) * 2.5f);

                // Outside circle = transparent/black
                if (d > 0.96f) c = Color.black;

                px[y * w + x] = c;
            }
            t.SetPixels(px); t.Apply(); return t;
        }

        // ── Mud ──────────────────────────────────────────────────────────────
        static Texture2D MakeMud(int w, int h)
        {
            Texture2D t = New(w, h);
            Color[] px = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float nx = x / (float)w;
                float ny = y / (float)h;
                float n = FBM(nx * 9f, ny * 9f, 4);
                float base_ = 0.45f + n * 0.15f;
                Color c = new Color(base_ * 0.85f, base_ * 0.72f, base_ * 0.54f);
                // Crack lines
                float crack = Noise(nx * 18f + 4f, ny * 18f + 2f);
                if (crack > 0.7f) c = Color.Lerp(c, new Color(0.22f, 0.18f, 0.12f), (crack - 0.7f) * 2.5f);
                px[y * w + x] = c;
            }
            t.SetPixels(px); t.Apply(); return t;
        }

        // ── Texture factory ───────────────────────────────────────────────────
        static Texture2D New(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGB24, true);
            t.filterMode = FilterMode.Bilinear;
            t.wrapMode   = TextureWrapMode.Repeat;
            return t;
        }

        // ── Noise functions ───────────────────────────────────────────────────
        // Smooth value noise in [0..1]
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

        // Fractal Brownian Motion
        static float FBM(float x, float y, int octaves)
        {
            float v = 0f, amp = 0.5f, freq = 1f, max = 0f;
            for (int i = 0; i < octaves; i++)
            {
                v   += Noise(x * freq, y * freq) * amp;
                max += amp;
                amp  *= 0.5f;
                freq *= 2f;
            }
            return v / max;
        }

        // Hash noise helper (deterministic)
        static float H(int x, int y)
        {
            int n = x + y * 57;
            n = (n << 13) ^ n;
            return (1f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824f) * 0.5f + 0.5f;
        }
    }
}
