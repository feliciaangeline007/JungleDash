using UnityEngine;

namespace JungleDash
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        private GUIStyle titleStyle, subStyle, cardStyle, hudStyle, btnStyle, touchStyle, badgeStyle;
        private Vector2 touchStart;
        private bool swiping;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update() => DetectSwipe();

        private void DetectSwipe()
        {
            var g = JungleDashGame.Instance;
            if (g == null || g.CurrentState != GameState.Playing) return;
            if (Input.touchCount == 0) return;

            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began) { touchStart = t.position; swiping = true; }
            else if (t.phase == TouchPhase.Ended && swiping)
            {
                swiping = false;
                Vector2 d = t.position - touchStart;
                if (d.magnitude < Screen.width * 0.08f) return;
                if (Mathf.Abs(d.x) > Mathf.Abs(d.y))
                {
                    if (d.x > 0) g.Player.MoveRight(); else g.Player.MoveLeft();
                }
                else if (d.y > 0) g.Player.Jump();
            }
        }

        private void InitStyles()
        {
            if (titleStyle != null) return;
            float h = Screen.height;

            Texture2D dark = Tex(new Color(0.05f, 0.12f, 0.06f, 0.88f));
            Texture2D btn  = Tex(new Color(0.16f, 0.44f, 0.20f, 0.95f));
            Texture2D tBtn = Tex(new Color(0.04f, 0.22f, 0.12f, 0.72f));

            titleStyle = new GUIStyle(GUI.skin.label) {
                fontSize = Mathf.RoundToInt(h * 0.065f), fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.88f, 0.3f) }
            };
            subStyle = new GUIStyle(GUI.skin.label) {
                fontSize = Mathf.RoundToInt(h * 0.025f),
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.85f, 0.95f, 0.85f) }
            };
            cardStyle = new GUIStyle(GUI.skin.box) {
                normal = { background = dark, textColor = Color.white }
            };
            hudStyle = new GUIStyle(GUI.skin.label) {
                fontSize = Mathf.RoundToInt(h * 0.028f), fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            btnStyle = new GUIStyle(GUI.skin.button) {
                fontSize = Mathf.RoundToInt(h * 0.035f), fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = btn, textColor = Color.white }
            };
            touchStyle = new GUIStyle(GUI.skin.button) {
                fontSize = Mathf.RoundToInt(h * 0.036f), fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = tBtn, textColor = new Color(1f, 1f, 1f, 0.9f) }
            };
            badgeStyle = new GUIStyle(GUI.skin.box) {
                fontSize = Mathf.RoundToInt(h * 0.022f), fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(1f, 0.95f, 0.5f) }
            };
        }

        private Texture2D Tex(Color c)
        {
            var t = new Texture2D(2, 2);
            t.SetPixels(new[] { c, c, c, c });
            t.Apply();
            return t;
        }

        private void OnGUI()
        {
            InitStyles();
            var g = JungleDashGame.Instance;
            if (g == null) return;
            switch (g.CurrentState)
            {
                case GameState.Menu:     DrawMenu(g);    break;
                case GameState.Playing:  DrawHUD(g); DrawTouchControls(g); break;
                case GameState.Paused:   DrawHUD(g); DrawPause(g); break;
                case GameState.GameOver: DrawGameOver(g); break;
            }
        }

        private void DrawMenu(JungleDashGame g)
        {
            float w = Screen.width, h = Screen.height;
            float pw = Mathf.Min(w * 0.85f, 480f), ph = Mathf.Min(h * 0.72f, 540f);
            Rect r = new Rect((w - pw) * 0.5f, (h - ph) * 0.5f, pw, ph);
            GUI.Box(r, "", cardStyle);
            GUILayout.BeginArea(r);
            GUILayout.Space(22);
            GUILayout.Label("🌴 JUNGLE DASH 🌴", titleStyle);
            GUILayout.Label("ENDLESS RUNNER HUTAN TROPIS", subStyle);
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label($"🏆 Skor Tertinggi: {g.HighScore:#,0}", hudStyle);
            GUILayout.Label($"🪙 Total Koin: {g.TotalCoins:#,0}", hudStyle);
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("▶  MULAI BERLARI", btnStyle, GUILayout.Height(h * 0.09f)))
            { SoundManager.Instance?.PlayClick(); g.StartGame(); }
            GUILayout.Space(10);
            GUILayout.Label("Geser kiri/kanan • Geser atas / Spasi untuk Lompat", subStyle);
            GUILayout.Space(14);
            GUILayout.EndArea();
        }

        private void DrawHUD(JungleDashGame g)
        {
            float w = Screen.width, h = Screen.height;
            float cw = Mathf.Min(w * 0.48f, 260f), ch = h * 0.17f;
            Rect r = new Rect(15, 15, cw, ch);
            GUI.Box(r, "", cardStyle);
            GUILayout.BeginArea(r);
            GUILayout.Space(5);
            GUILayout.Label($"⭐ SKOR: {g.Score:#,0}", hudStyle);
            GUILayout.Label($"🏃 JARAK: {Mathf.FloorToInt(g.Distance)} m", hudStyle);
            GUILayout.Label($"🪙 KOIN: {g.Coins}", hudStyle);
            GUILayout.EndArea();

            // Power-up badges
            float by = 15f;
            DrawBadge(PowerUpType.Magnet,      "🧲 MAGNET",   cw + 28, ref by);
            DrawBadge(PowerUpType.Shield,      "🛡 PERISAI",  cw + 28, ref by);
            DrawBadge(PowerUpType.SpeedBoost,  "⚡ KILAT",    cw + 28, ref by);
            DrawBadge(PowerUpType.DoubleScore, "2x SKOR",     cw + 28, ref by);
            DrawBadge(PowerUpType.Fly,         "🪽 TERBANG",  cw + 28, ref by);

            // Pause button
            float pb = Mathf.Min(h * 0.08f, 65f);
            if (GUI.Button(new Rect(w - pb - 15, 15, pb, pb), "⏸", btnStyle))
            { SoundManager.Instance?.PlayClick(); g.PauseGame(); }
        }

        private void DrawBadge(PowerUpType type, string label, float x, ref float y)
        {
            var pm = PowerUpManager.Instance;
            if (pm == null || !pm.IsActive(type)) return;
            float t = pm.GetRemainingTime(type);
            string txt = type == PowerUpType.Shield ? "AKTIF" : $"{t:0.0}s";
            float bh = Screen.height * 0.045f;
            GUI.Box(new Rect(x, y, 190f, bh), $"{label} ({txt})", badgeStyle);
            y += bh + 5f;
        }

        private void DrawTouchControls(JungleDashGame g)
        {
            float w = Screen.width, h = Screen.height;
            float bh = Mathf.Min(h * 0.12f, 90f);
            float sw = Mathf.Min(w * 0.28f, 150f);
            float jw = Mathf.Min(w * 0.32f, 180f);

            if (GUI.Button(new Rect(20, h - bh - 25, sw, bh), "◀ KIRI", touchStyle))
                g.Player.MoveLeft();
            if (GUI.Button(new Rect((w - jw) * 0.5f, h - bh - 25, jw, bh), "⬆ LOMPAT", touchStyle))
                g.Player.Jump();
            if (GUI.Button(new Rect(w - sw - 20, h - bh - 25, sw, bh), "KANAN ▶", touchStyle))
                g.Player.MoveRight();
        }

        private void DrawPause(JungleDashGame g)
        {
            float w = Screen.width, h = Screen.height;
            float pw = Mathf.Min(w * 0.78f, 420f), ph = Mathf.Min(h * 0.55f, 380f);
            Rect r = new Rect((w - pw) * 0.5f, (h - ph) * 0.5f, pw, ph);
            GUI.Box(r, "", cardStyle);
            GUILayout.BeginArea(r);
            GUILayout.Space(18);
            GUILayout.Label("⏸  GAME DIJEDA", titleStyle);
            GUILayout.Space(20);
            if (GUILayout.Button("▶  LANJUTKAN", btnStyle, GUILayout.Height(h * 0.08f)))
            { SoundManager.Instance?.PlayClick(); g.ResumeGame(); }
            GUILayout.Space(10);
            if (GUILayout.Button("🔄  MAIN LAGI", btnStyle, GUILayout.Height(h * 0.08f)))
            { SoundManager.Instance?.PlayClick(); g.StartGame(); }
            GUILayout.Space(10);
            bool snd = SoundManager.Instance != null && SoundManager.Instance.SoundEnabled;
            if (GUILayout.Button(snd ? "🔊  SUARA: AKTIF" : "🔇  SUARA: MATI", btnStyle, GUILayout.Height(h * 0.07f)))
            {
                if (SoundManager.Instance != null) { SoundManager.Instance.SoundEnabled = !snd; SoundManager.Instance.PlayClick(); }
            }
            GUILayout.EndArea();
        }

        private void DrawGameOver(JungleDashGame g)
        {
            float w = Screen.width, h = Screen.height;
            float pw = Mathf.Min(w * 0.85f, 500f), ph = Mathf.Min(h * 0.78f, 560f);
            Rect r = new Rect((w - pw) * 0.5f, (h - ph) * 0.5f, pw, ph);
            GUI.Box(r, "", cardStyle);
            GUILayout.BeginArea(r);
            GUILayout.Space(20);
            GUILayout.Label("💀  GAME OVER", titleStyle);
            GUILayout.Space(12);
            if (g.IsNewHighScore)
                GUILayout.Label("🎉 SKOR TERTINGGI BARU! 🎉", subStyle);
            GUILayout.Space(8);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label($"⭐ Skor: {g.Score:#,0}", hudStyle);
            GUILayout.Label($"🏃 Jarak: {Mathf.FloorToInt(g.Distance)} meter", hudStyle);
            GUILayout.Label($"🪙 Koin: {g.Coins}", hudStyle);
            GUILayout.Label($"🏆 Tertinggi: {g.HighScore:#,0}", hudStyle);
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("🔄  MAIN LAGI", btnStyle, GUILayout.Height(h * 0.09f)))
            { SoundManager.Instance?.PlayClick(); g.StartGame(); }
            GUILayout.Space(10);
            if (GUILayout.Button("🏠  MENU UTAMA", btnStyle, GUILayout.Height(h * 0.07f)))
            { SoundManager.Instance?.PlayClick(); g.ShowMenu(); }
            GUILayout.Space(14);
            GUILayout.EndArea();
        }
    }
}
