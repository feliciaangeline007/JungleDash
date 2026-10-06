// UIManager.cs – Temple Run-inspired HUD and menus
// Dark stone UI panels, gold text, Indonesian localization.
using UnityEngine;

namespace JungleDash
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        // ── Styles ────────────────────────────────────────────────────────────
        private GUIStyle titleStyle, subtitleStyle, bodyStyle, btnStyle;
        private GUIStyle hudStyle, badgeStyle, touchBtnStyle, panelStyle;
        private bool     stylesBuilt;

        // Touch swipe
        private Vector2 touchStart;
        private bool    touchActive;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update() => HandleTouch();

        // ── Touch / swipe ─────────────────────────────────────────────────────
        private void HandleTouch()
        {
            var g = JungleDashGame.Instance;
            if (g == null || g.CurrentState != GameState.Playing) return;
            if (Input.touchCount == 0) return;

            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                touchStart  = t.position;
                touchActive = true;
            }
            else if (t.phase == TouchPhase.Ended && touchActive)
            {
                touchActive = false;
                Vector2 delta   = t.position - touchStart;
                float   minSwipe = Screen.width * 0.07f;
                if (delta.magnitude < minSwipe) return;

                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                {
                    if (delta.x > 0) g.Player.MoveRight();
                    else             g.Player.MoveLeft();
                }
                else
                {
                    if (delta.y > 0) g.Player.Jump();
                    else             g.Player.Slide();
                }
            }
        }

        // ── Style builder ─────────────────────────────────────────────────────
        private void BuildStyles()
        {
            if (stylesBuilt) return;
            stylesBuilt = true;

            float h = Screen.height;

            // Dark stone panel
            var stoneBg  = Tex(new Color(0.06f, 0.05f, 0.04f, 0.92f));
            var stoneDark = Tex(new Color(0.10f, 0.08f, 0.06f, 0.96f));
            // Gold-outline button
            var btnBg    = Tex(new Color(0.55f, 0.38f, 0.05f, 0.95f));
            var btnHover = Tex(new Color(0.72f, 0.52f, 0.08f, 0.98f));
            // Touch control translucent
            var touchBg  = Tex(new Color(0.12f, 0.10f, 0.06f, 0.68f));

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize   = Mathf.RoundToInt(h * 0.072f),
                fontStyle  = FontStyle.Bold,
                alignment  = TextAnchor.MiddleCenter,
                normal     = { textColor = new Color(1.0f, 0.85f, 0.20f) },  // temple gold
                wordWrap   = false
            };
            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = Mathf.RoundToInt(h * 0.026f),
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = new Color(0.80f, 0.72f, 0.55f) },  // aged parchment
                wordWrap  = true
            };
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize   = Mathf.RoundToInt(h * 0.030f),
                fontStyle  = FontStyle.Bold,
                alignment  = TextAnchor.MiddleLeft,
                normal     = { textColor = new Color(0.95f, 0.88f, 0.68f) }
            };
            hudStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize   = Mathf.RoundToInt(h * 0.032f),
                fontStyle  = FontStyle.Bold,
                alignment  = TextAnchor.MiddleLeft,
                normal     = { textColor = Color.white }
            };
            panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = stoneBg, textColor = Color.white }
            };
            btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = Mathf.RoundToInt(h * 0.036f),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { background = btnBg,    textColor = new Color(1f, 0.94f, 0.72f) },
                hover     = { background = btnHover, textColor = Color.white },
                active    = { background = btnHover, textColor = Color.white }
            };
            touchBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = Mathf.RoundToInt(h * 0.038f),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { background = touchBg, textColor = new Color(1f, 0.90f, 0.65f) }
            };
            badgeStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize  = Mathf.RoundToInt(h * 0.022f),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal    = { background = stoneDark, textColor = new Color(1f, 0.82f, 0.22f) }
            };
        }

        private static Texture2D Tex(Color c)
        {
            var t = new Texture2D(2, 2);
            t.SetPixels(new[] { c, c, c, c });
            t.Apply();
            return t;
        }

        // ── OnGUI entry ───────────────────────────────────────────────────────
        private void OnGUI()
        {
            BuildStyles();
            var g = JungleDashGame.Instance;
            if (g == null) return;

            switch (g.CurrentState)
            {
                case GameState.Menu:     DrawMenu(g);     break;
                case GameState.Playing:  DrawHUD(g); DrawTouchControls(g); break;
                case GameState.Paused:   DrawHUD(g); DrawPause(g);         break;
                case GameState.GameOver: DrawGameOver(g); break;
            }
        }

        // ── Main Menu ─────────────────────────────────────────────────────────
        private void DrawMenu(JungleDashGame g)
        {
            float W = Screen.width, H = Screen.height;
            float pw = Mathf.Min(W * .86f, 520f);
            float ph = Mathf.Min(H * .76f, 580f);
            Rect  r  = Centre(W, H, pw, ph);
            GUI.Box(r, "", panelStyle);

            GUILayout.BeginArea(Inset(r, 18));
            GUILayout.Space(16);

            GUILayout.Label("⚔  JUNGLE DASH  ⚔", titleStyle);
            GUILayout.Label("Lari dari kutukan kuil kuno!", subtitleStyle);

            GUILayout.FlexibleSpace();

            // Stats card
            GUI.Box(GUILayoutUtility.GetRect(pw - 36, H * .14f), "", panelStyle);
            GUILayout.Space(-H * .14f);
            GUILayout.BeginVertical();
            GUILayout.Space(4);
            GUILayout.Label($"  🏆  Rekor:  {g.HighScore:#,0}", bodyStyle);
            GUILayout.Label($"  🪙  Koin:    {g.TotalCoins:#,0}", bodyStyle);
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("▶   MULAI BERLARI", btnStyle, GUILayout.Height(H * .10f)))
            {
                SoundManager.Instance?.PlayClick();
                g.StartGame();
            }

            GUILayout.Space(10);
            GUILayout.Label("Geser ◀ ▶ belok  •  Geser ▲ lompat  •  Geser ▼ slide", subtitleStyle);
            GUILayout.Space(14);
            GUILayout.EndArea();
        }

        // ── HUD ───────────────────────────────────────────────────────────────
        private void DrawHUD(JungleDashGame g)
        {
            float W = Screen.width, H = Screen.height;

            // Score/distance card (top-left)
            float cw = Mathf.Min(W * .44f, 240f);
            float ch = H * .175f;
            Rect  cr = new Rect(14, 14, cw, ch);
            GUI.Box(cr, "", panelStyle);
            GUILayout.BeginArea(Inset(cr, 8));
            GUILayout.Space(2);
            GUILayout.Label($"⭐  {g.Score:#,0}", hudStyle);
            GUILayout.Label($"🏃  {Mathf.FloorToInt(g.Distance)} m", hudStyle);
            GUILayout.Label($"🪙  {g.Coins}", hudStyle);
            GUILayout.EndArea();

            // Power-up timers (below score card)
            float by = 14f + ch + 6f;
            DrawBadge(PowerUpType.Magnet,      "🧲 MAGNET",   14, ref by);
            DrawBadge(PowerUpType.Shield,      "🛡 PERISAI",  14, ref by);
            DrawBadge(PowerUpType.SpeedBoost,  "⚡ KILAT",    14, ref by);
            DrawBadge(PowerUpType.DoubleScore, "2✕ SKOR",     14, ref by);
            DrawBadge(PowerUpType.Fly,         "🪽 TERBANG",  14, ref by);

            // Pause button (top-right)
            float pb = Mathf.Min(H * .08f, 64f);
            if (GUI.Button(new Rect(W - pb - 14, 14, pb, pb), "⏸", btnStyle))
            {
                SoundManager.Instance?.PlayClick();
                g.PauseGame();
            }
        }

        private void DrawBadge(PowerUpType type, string label, float x, ref float y)
        {
            var pm = PowerUpManager.Instance;
            if (pm == null || !pm.IsActive(type)) return;
            float t   = pm.GetRemainingTime(type);
            string txt = type == PowerUpType.Shield ? "AKTIF" : $"{t:0.0}s";
            float bh  = Screen.height * .044f;
            GUI.Box(new Rect(x, y, 200f, bh), $"{label} ({txt})", badgeStyle);
            y += bh + 4f;
        }

        // ── Touch controls (bottom) ───────────────────────────────────────────
        private void DrawTouchControls(JungleDashGame g)
        {
            float W = Screen.width, H = Screen.height;
            float bh = Mathf.Min(H * .115f, 88f);
            float sw = Mathf.Min(W * .26f, 140f);
            float jw = Mathf.Min(W * .30f, 165f);
            float sl = Mathf.Min(W * .22f, 120f);
            float by = H - bh - 22f;

            if (GUI.Button(new Rect(18, by, sw, bh), "◀", touchBtnStyle))                g.Player.MoveLeft();
            if (GUI.Button(new Rect(W * .3f, by, jw, bh), "⬆  LOMPAT", touchBtnStyle))  g.Player.Jump();
            if (GUI.Button(new Rect(W * .55f, by, sl, bh), "⬇  SLIDE",  touchBtnStyle))  g.Player.Slide();
            if (GUI.Button(new Rect(W - sw - 18, by, sw, bh), "▶",       touchBtnStyle)) g.Player.MoveRight();
        }

        // ── Pause ─────────────────────────────────────────────────────────────
        private void DrawPause(JungleDashGame g)
        {
            float W = Screen.width, H = Screen.height;
            float pw = Mathf.Min(W * .80f, 440f), ph = Mathf.Min(H * .56f, 400f);
            Rect  r  = Centre(W, H, pw, ph);
            GUI.Box(r, "", panelStyle);
            GUILayout.BeginArea(Inset(r, 18));
            GUILayout.Space(16);
            GUILayout.Label("⏸  DIJEDA", titleStyle);
            GUILayout.Space(18);
            if (GUILayout.Button("▶  LANJUTKAN", btnStyle, GUILayout.Height(H * .085f)))
            { SoundManager.Instance?.PlayClick(); g.ResumeGame(); }
            GUILayout.Space(10);
            if (GUILayout.Button("🔄  MAIN LAGI", btnStyle, GUILayout.Height(H * .085f)))
            { SoundManager.Instance?.PlayClick(); g.StartGame(); }
            GUILayout.Space(10);
            bool snd = SoundManager.Instance != null && SoundManager.Instance.SoundEnabled;
            if (GUILayout.Button(snd ? "🔊  SUARA: ON" : "🔇  SUARA: OFF", btnStyle, GUILayout.Height(H * .072f)))
            {
                if (SoundManager.Instance != null) { SoundManager.Instance.SoundEnabled = !snd; SoundManager.Instance.PlayClick(); }
            }
            GUILayout.EndArea();
        }

        // ── Game Over ─────────────────────────────────────────────────────────
        private void DrawGameOver(JungleDashGame g)
        {
            float W = Screen.width, H = Screen.height;
            float pw = Mathf.Min(W * .88f, 520f), ph = Mathf.Min(H * .80f, 580f);
            Rect  r  = Centre(W, H, pw, ph);
            GUI.Box(r, "", panelStyle);
            GUILayout.BeginArea(Inset(r, 18));
            GUILayout.Space(18);
            GUILayout.Label("💀  GAME OVER  💀", titleStyle);
            GUILayout.Space(8);
            if (g.IsNewHighScore) GUILayout.Label("✨  REKOR BARU!  ✨", subtitleStyle);
            GUILayout.Space(6);

            GUI.Box(GUILayoutUtility.GetRect(pw - 36, H * .24f), "", panelStyle);
            GUILayout.Space(-H * .24f);
            GUILayout.BeginVertical();
            GUILayout.Space(4);
            GUILayout.Label($"  ⭐  Skor:     {g.Score:#,0}",               bodyStyle);
            GUILayout.Label($"  🏃  Jarak:    {Mathf.FloorToInt(g.Distance)} meter", bodyStyle);
            GUILayout.Label($"  🪙  Koin:     {g.Coins}",                   bodyStyle);
            GUILayout.Label($"  🏆  Rekor:    {g.HighScore:#,0}",           bodyStyle);
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("🔄  MAIN LAGI", btnStyle, GUILayout.Height(H * .095f)))
            { SoundManager.Instance?.PlayClick(); g.StartGame(); }
            GUILayout.Space(10);
            if (GUILayout.Button("🏠  MENU UTAMA", btnStyle, GUILayout.Height(H * .072f)))
            { SoundManager.Instance?.PlayClick(); g.ShowMenu(); }
            GUILayout.Space(14);
            GUILayout.EndArea();
        }

        // ── Layout helpers ────────────────────────────────────────────────────
        private static Rect Centre(float W, float H, float w, float h) =>
            new Rect((W - w) * .5f, (H - h) * .5f, w, h);

        private static Rect Inset(Rect r, float pad) =>
            new Rect(r.x + pad, r.y + pad, r.width - pad * 2f, r.height - pad * 2f);
    }
}
