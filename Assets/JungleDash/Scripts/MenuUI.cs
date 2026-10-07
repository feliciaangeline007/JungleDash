// MenuUI.cs - Premium animated menu suite: Main Menu, Stage Expeditions, Temple Shop, Missions, Daily Calendar, Pause, Game Over, Victory.
using System;
using UnityEngine;

namespace JungleDash
{
    public class MenuUI : MonoBehaviour
    {
        public Action OnPlayEndless;
        public Action<int> OnPlayStage;
        public Action OnOpenShop;
        public Action OnOpenLevels;
        public Action OnOpenMissions;
        public Action OnOpenDailyRewards;
        public Action OnBackToMenu;
        public Action OnResume;
        public Action OnRestart;
        public Action OnRevive;
        public Action OnNextStage;
        public Action OnToggleSound;
        public Action<int> OnSkinSelected;

        int shopTab = 0;
        Vector2 stageScrollPos;
        Vector2 shopScrollPos;
        Vector2 missionsScrollPos;

        // Animation state
        float menuEntryAnim;
        float titlePulse;
        float deadScreenTimer;
        float victoryStarAnim;

        void Update()
        {
            menuEntryAnim = Mathf.Lerp(menuEntryAnim, 1f, 1f - Mathf.Exp(-4f * Time.unscaledDeltaTime));
            titlePulse += Time.unscaledDeltaTime;
            deadScreenTimer += Time.unscaledDeltaTime;
        }

        public void ResetMenuAnim()
        {
            menuEntryAnim = 0f;
            deadScreenTimer = 0f;
            victoryStarAnim = 0f;
        }

        public void DrawMenu(ProgressionManager prog, bool soundOn)
        {
            float W = Screen.width;
            float H = Screen.height;

            // Full screen vignette
            GUI.DrawTexture(new Rect(0, 0, W, H), UIHelper.GetGradientTexture(
                new Color(0.02f, 0.04f, 0.06f, 0.55f),
                new Color(0.06f, 0.08f, 0.12f, 0.75f)));

            // Top bar
            Rect coinCard = new Rect(24f, 22f, 185f, 46f);
            UIHelper.DrawGradientCard(coinCard, new Color(0.14f, 0.12f, 0.08f, 0.92f), new Color(0.08f, 0.06f, 0.03f, 0.92f), GameConfig.GoldColor, 1.5f);
            UIHelper.Text("★ " + prog.TotalCoins, coinCard, 19, GameConfig.GoldColor, TextAnchor.MiddleCenter);

            Rect bestCard = new Rect(coinCard.xMax + 10f, 22f, 170f, 46f);
            UIHelper.DrawGradientCard(bestCard, new Color(0.12f, 0.14f, 0.18f, 0.92f), new Color(0.06f, 0.07f, 0.10f, 0.92f), new Color(0.6f, 0.7f, 0.8f, 0.3f), 1f);
            UIHelper.Text("BEST " + prog.BestDistance + "m", bestCard, 15, new Color(0.85f, 0.9f, 0.95f), TextAnchor.MiddleCenter);

            Rect soundBtn = new Rect(W - 78f, 22f, 54f, 46f);
            string soundIcon = soundOn ? "♫" : "♪";
            if (UIHelper.DrawButton(soundBtn, soundIcon, new Color(0.12f, 0.15f, 0.20f, 0.9f), soundOn ? Color.white : new Color(0.5f, 0.5f, 0.5f), 22))
            {
                OnToggleSound?.Invoke();
            }

            // Title
            float center = W * 0.5f;
            float titleY = H * 0.14f;
            float titleGlow = 0.5f + Mathf.Sin(titlePulse * 1.5f) * 0.5f;
            Color titleColor = Color.Lerp(GameConfig.GoldColor, new Color(1f, 0.95f, 0.6f), titleGlow);

            // Title glow backdrop
            Rect titleGlowRect = new Rect(center - 260f, titleY - 8f, 520f, 72f);
            UIHelper.DrawGlow(titleGlowRect, new Color(GameConfig.GoldColor.r, GameConfig.GoldColor.g, GameConfig.GoldColor.b, 0.08f), 12f);

            UIHelper.Text("JUNGLE DASH", new Rect(center - 260f, titleY, 520f, 56f), 50, titleColor, TextAnchor.MiddleCenter);
            
            // Decorative separator
            UIHelper.DrawSeparator(new Rect(center - 140f, titleY + 56f, 280f, 2f), new Color(1f, 0.85f, 0.3f, 0.5f));
            
            UIHelper.Text("THE ANCIENT EXPEDITION", new Rect(center - 260f, titleY + 64f, 520f, 22f), 14, new Color(0.78f, 0.88f, 0.82f, 0.9f), TextAnchor.MiddleCenter);

            // Menu buttons with staggered animation
            float btnW = Mathf.Min(350f, W - 60f);
            float btnH = 52f;
            float startY = H * 0.37f;
            float gap = 13f;

            float anim = menuEntryAnim;
            float stagger1 = Mathf.Clamp01(anim * 4f);
            float stagger2 = Mathf.Clamp01(anim * 4f - 0.3f);
            float stagger3 = Mathf.Clamp01(anim * 4f - 0.6f);
            float stagger4 = Mathf.Clamp01(anim * 4f - 0.9f);
            float stagger5 = Mathf.Clamp01(anim * 4f - 1.2f);

            float slideOffset1 = (1f - stagger1) * 60f;
            float slideOffset2 = (1f - stagger2) * 60f;
            float slideOffset3 = (1f - stagger3) * 60f;
            float slideOffset4 = (1f - stagger4) * 60f;
            float slideOffset5 = (1f - stagger5) * 60f;

            if (UIHelper.DrawAccentButton(new Rect(center - btnW * 0.5f, startY + slideOffset1, btnW, btnH), "▶  ENDLESS DASH", new Color(0.18f, 0.72f, 0.35f), Color.white, 21))
            {
                OnPlayEndless?.Invoke();
            }

            if (UIHelper.DrawButton(new Rect(center - btnW * 0.5f, startY + (btnH + gap) + slideOffset2, btnW, btnH), "🗺  STAGE EXPEDITIONS", new Color(0.18f, 0.48f, 0.82f), Color.white, 18))
            {
                OnOpenLevels?.Invoke();
            }

            if (UIHelper.DrawButton(new Rect(center - btnW * 0.5f, startY + (btnH + gap) * 2 + slideOffset3, btnW, btnH), "⚡  TEMPLE SHOP", new Color(0.82f, 0.58f, 0.12f), Color.white, 18))
            {
                OnOpenShop?.Invoke();
            }

            if (UIHelper.DrawButton(new Rect(center - btnW * 0.5f, startY + (btnH + gap) * 3 + slideOffset4, btnW, btnH), "📜  EXPLORER MISSIONS", new Color(0.58f, 0.32f, 0.82f), Color.white, 18))
            {
                OnOpenMissions?.Invoke();
            }

            bool canClaimDaily = prog.CanClaimDailyReward();
            Color dailyColor = canClaimDaily ? GameConfig.GoldColor : new Color(0.35f, 0.38f, 0.42f);
            string dailyLabel = canClaimDaily ? "🎁  DAILY REWARD!" : "🎁  DAILY REWARD";
            if (canClaimDaily)
            {
                if (UIHelper.DrawAccentButton(new Rect(center - btnW * 0.5f, startY + (btnH + gap) * 4 + slideOffset5, btnW, btnH), dailyLabel, dailyColor, Color.black, 16))
                {
                    OnOpenDailyRewards?.Invoke();
                }
            }
            else
            {
                if (UIHelper.DrawButton(new Rect(center - btnW * 0.5f, startY + (btnH + gap) * 4 + slideOffset5, btnW, btnH), dailyLabel, dailyColor, Color.white, 14))
                {
                    OnOpenDailyRewards?.Invoke();
                }
            }

            // Version/credit
            UIHelper.Text("v2.0", new Rect(W - 60f, H - 30f, 50f, 20f), 11, new Color(0.5f, 0.55f, 0.6f, 0.5f), TextAnchor.MiddleRight, false);
        }

        public void DrawStageSelect(ProgressionManager prog)
        {
            float W = Screen.width;
            float H = Screen.height;

            DrawScreenHeader("STAGE EXPEDITIONS", "Conquer the ancient biomes", prog.TotalCoins);

            float cardW = Mathf.Min(500f, W - 40f);
            float cardH = 98f;
            float startY = 112f;
            float gap = 12f;

            stageScrollPos = GUI.BeginScrollView(new Rect((W - cardW) * 0.5f, startY, cardW + 20f, H - 200f), stageScrollPos, new Rect(0, 0, cardW, GameConfig.Stages.Length * (cardH + gap)));

            for (int i = 0; i < GameConfig.Stages.Length; i++)
            {
                LevelData stage = GameConfig.Stages[i];
                bool unlocked = stage.id <= prog.UnlockedStage;
                int stars = prog.GetStageStars(stage.id);

                Rect card = new Rect(0, i * (cardH + gap), cardW, cardH);
                
                Color cardTop = unlocked ? new Color(0.14f, 0.17f, 0.24f, 0.95f) : new Color(0.10f, 0.10f, 0.13f, 0.85f);
                Color cardBot = unlocked ? new Color(0.08f, 0.10f, 0.16f, 0.95f) : new Color(0.06f, 0.06f, 0.08f, 0.85f);
                Color border = unlocked ? new Color(stage.pathColor.r, stage.pathColor.g, stage.pathColor.b, 0.6f) : new Color(0.3f, 0.3f, 0.3f, 0.4f);

                UIHelper.DrawGradientCard(card, cardTop, cardBot, border, 1.5f);

                // Stage number badge
                Rect badge = new Rect(card.x + 12f, card.y + 12f, 38f, 38f);
                UIHelper.DrawGradientCard(badge, stage.hazeColor, stage.ambientColor, stage.sunColor, 2f);
                UIHelper.Text(stage.id.ToString(), badge, 20, Color.white, TextAnchor.MiddleCenter);

                // Stage info
                UIHelper.Text(stage.name, new Rect(card.x + 60f, card.y + 12f, 260f, 24f), 19, unlocked ? Color.white : Color.gray, TextAnchor.MiddleLeft);
                UIHelper.Text(stage.subtitle + " • " + Mathf.FloorToInt(stage.targetDistance) + "m", new Rect(card.x + 60f, card.y + 38f, 260f, 20f), 12, new Color(0.65f, 0.75f, 0.8f), TextAnchor.MiddleLeft);

                // Stars
                string starStr = "";
                for (int s = 1; s <= 3; s++)
                {
                    starStr += (s <= stars ? "★ " : "☆ ");
                }
                UIHelper.Text(starStr, new Rect(card.x + 60f, card.y + 62f, 120f, 24f), 20, stars > 0 ? GameConfig.GoldColor : new Color(0.4f, 0.4f, 0.45f), TextAnchor.MiddleLeft);

                // Reward info
                UIHelper.Text("★" + stage.coinReward, new Rect(card.x + 180f, card.y + 64f, 80f, 20f), 12, new Color(0.7f, 0.7f, 0.5f), TextAnchor.MiddleLeft, false);

                // Action button
                Rect btnRect = new Rect(card.xMax - 118f, card.y + 26f, 102f, 44f);
                if (unlocked)
                {
                    if (UIHelper.DrawButton(btnRect, "PLAY ▶", new Color(0.18f, 0.72f, 0.35f), Color.white, 16))
                    {
                        OnPlayStage?.Invoke(stage.id);
                    }
                }
                else
                {
                    UIHelper.DrawButton(btnRect, "🔒", new Color(0.2f, 0.2f, 0.24f), Color.gray, 18, false);
                }
            }

            GUI.EndScrollView();
            DrawBackButton();
        }

        public void DrawShop(ProgressionManager prog)
        {
            float W = Screen.width;
            float H = Screen.height;

            DrawScreenHeader("TEMPLE BAZAAR", "Enhance abilities & unlock expedition attire", prog.TotalCoins);

            // Premium tab bar
            float tabW = 175f;
            Rect tab1 = new Rect(W * 0.5f - tabW - 2f, 88f, tabW, 42f);
            Rect tab2 = new Rect(W * 0.5f + 2f, 88f, tabW, 42f);

            Color activeTab = new Color(0.85f, 0.65f, 0.12f);
            Color inactiveTab = new Color(0.12f, 0.14f, 0.18f, 0.9f);

            if (UIHelper.DrawButton(tab1, "⚔ UPGRADES", shopTab == 0 ? activeTab : inactiveTab, shopTab == 0 ? Color.black : Color.white, 15))
            {
                shopTab = 0;
            }
            if (UIHelper.DrawButton(tab2, "👤 OUTFITS", shopTab == 1 ? activeTab : inactiveTab, shopTab == 1 ? Color.black : Color.white, 15))
            {
                shopTab = 1;
            }

            float contentW = Mathf.Min(530f, W - 40f);
            float startY = 145f;

            if (shopTab == 0)
            {
                UpgradeType[] upgs = new UpgradeType[] { UpgradeType.Shield, UpgradeType.Magnet, UpgradeType.SpeedBoost, UpgradeType.Flight, UpgradeType.DoubleScore };
                string[] names = new string[] { "Divine Shield", "Magnetic Relic", "Dash Surge", "Canopy Wings", "Double Bounty" };
                string[] descs = new string[] { "Survive obstacle collisions", "Attract gold coins from afar", "Super speed, destroy obstacles", "Fly above all dangers", "Earn 2x coins per pickup" };
                string[] icons = new string[] { "⛊", "◎", "⚡", "✧", "★" };
                Color[] colors = new Color[] {
                    new Color(0.3f, 0.65f, 1f),
                    new Color(1f, 0.35f, 0.35f),
                    new Color(1f, 0.65f, 0.1f),
                    new Color(0.85f, 0.45f, 1f),
                    new Color(1f, 0.9f, 0.2f)
                };

                float cardH = 92f;
                float gap = 10f;
                shopScrollPos = GUI.BeginScrollView(new Rect((W - contentW) * 0.5f, startY, contentW + 20f, H - 230f), shopScrollPos, new Rect(0, 0, contentW, upgs.Length * (cardH + gap)));

                for (int i = 0; i < upgs.Length; i++)
                {
                    UpgradeType ut = upgs[i];
                    int lvl = prog.GetUpgradeLevel(ut);
                    int cost = prog.GetUpgradeCost(ut);
                    bool maxed = lvl >= GameConfig.MaxUpgradeLevel;

                    Rect card = new Rect(0, i * (cardH + gap), contentW, cardH);
                    UIHelper.DrawGradientCard(card, new Color(0.14f, 0.17f, 0.24f, 0.95f), new Color(0.08f, 0.10f, 0.14f, 0.95f), new Color(colors[i].r, colors[i].g, colors[i].b, 0.35f), 1.5f);

                    // Icon
                    UIHelper.Text(icons[i], new Rect(card.x + 12f, card.y + 10f, 30f, 30f), 22, colors[i], TextAnchor.MiddleCenter);

                    UIHelper.Text(names[i], new Rect(card.x + 48f, card.y + 10f, 260f, 24f), 17, Color.white, TextAnchor.MiddleLeft);
                    UIHelper.Text(descs[i], new Rect(card.x + 48f, card.y + 34f, 280f, 20f), 12, new Color(0.7f, 0.78f, 0.85f), TextAnchor.MiddleLeft);

                    // Level pips with colors
                    float pipX = card.x + 48f;
                    for (int p = 1; p <= GameConfig.MaxUpgradeLevel; p++)
                    {
                        Rect pip = new Rect(pipX + (p - 1) * 22f, card.y + 60f, 16f, 16f);
                        Color pipCol = p <= lvl ? colors[i] : new Color(0.3f, 0.3f, 0.35f);
                        UIHelper.DrawGradientCard(pip, Color.Lerp(pipCol, Color.white, 0.2f), pipCol, new Color(1f, 1f, 1f, 0.2f), 1f);
                    }

                    // Duration info
                    float dur = prog.GetPowerUpDuration(ut);
                    UIHelper.Text(dur.ToString("F1") + "s", new Rect(card.x + 48f + GameConfig.MaxUpgradeLevel * 22f + 8f, card.y + 60f, 50f, 16f), 11, new Color(0.6f, 0.7f, 0.75f), TextAnchor.MiddleLeft, false);

                    Rect btnRect = new Rect(card.xMax - 128f, card.y + 24f, 112f, 44f);
                    if (maxed)
                    {
                        UIHelper.DrawButton(btnRect, "MAX ✓", new Color(0.15f, 0.45f, 0.25f), Color.white, 14, false);
                    }
                    else
                    {
                        bool canAfford = prog.TotalCoins >= cost;
                        if (UIHelper.DrawButton(btnRect, "★ " + cost, canAfford ? GameConfig.GoldColor : new Color(0.25f, 0.25f, 0.3f), canAfford ? Color.black : Color.gray, 15, canAfford))
                        {
                            prog.BuyUpgrade(ut);
                        }
                    }
                }

                GUI.EndScrollView();
            }
            else
            {
                float cardH = 96f;
                float gap = 12f;
                shopScrollPos = GUI.BeginScrollView(new Rect((W - contentW) * 0.5f, startY, contentW + 20f, H - 230f), shopScrollPos, new Rect(0, 0, contentW, GameConfig.Skins.Length * (cardH + gap)));

                for (int i = 0; i < GameConfig.Skins.Length; i++)
                {
                    SkinData skin = GameConfig.Skins[i];
                    bool unlocked = prog.IsSkinUnlocked(skin.id);
                    bool selected = prog.SelectedSkin == skin.id;

                    Rect card = new Rect(0, i * (cardH + gap), contentW, cardH);
                    Color borderCol = selected ? GameConfig.GoldColor : new Color(skin.primaryColor.r, skin.primaryColor.g, skin.primaryColor.b, 0.4f);
                    UIHelper.DrawGradientCard(card, new Color(0.14f, 0.17f, 0.22f, 0.95f), new Color(0.08f, 0.10f, 0.14f, 0.95f), borderCol, selected ? 2.5f : 1.5f);

                    // Color swatch with gradient
                    Rect swatch = new Rect(card.x + 14f, card.y + 16f, 52f, 60f);
                    UIHelper.DrawGradientCard(swatch, Color.Lerp(skin.primaryColor, Color.white, 0.15f), skin.primaryColor, skin.accentColor, 2.5f);
                    // Accent stripe
                    Rect stripe = new Rect(swatch.x, swatch.yMax - 12f, swatch.width, 12f);
                    GUI.DrawTexture(stripe, UIHelper.GetColorTexture(skin.accentColor));

                    UIHelper.Text(skin.name, new Rect(card.x + 78f, card.y + 16f, 240f, 24f), 18, Color.white, TextAnchor.MiddleLeft);
                    UIHelper.Text(skin.title, new Rect(card.x + 78f, card.y + 42f, 240f, 20f), 13, new Color(0.65f, 0.75f, 0.82f), TextAnchor.MiddleLeft);

                    if (selected)
                    {
                        UIHelper.Text("EQUIPPED", new Rect(card.x + 78f, card.y + 64f, 120f, 18f), 12, GameConfig.AccentGreen, TextAnchor.MiddleLeft, false);
                    }

                    Rect btnRect = new Rect(card.xMax - 128f, card.y + 26f, 112f, 44f);
                    if (selected)
                    {
                        UIHelper.DrawButton(btnRect, "✓ ACTIVE", new Color(0.15f, 0.55f, 0.3f), Color.white, 13, false);
                    }
                    else if (unlocked)
                    {
                        if (UIHelper.DrawButton(btnRect, "EQUIP", new Color(0.18f, 0.48f, 0.82f), Color.white, 15))
                        {
                            if (prog.SelectSkin(skin.id))
                                OnSkinSelected?.Invoke(skin.id);
                        }
                    }
                    else
                    {
                        bool canAfford = prog.TotalCoins >= skin.price;
                        if (UIHelper.DrawButton(btnRect, "★ " + skin.price, canAfford ? GameConfig.GoldColor : new Color(0.25f, 0.25f, 0.3f), canAfford ? Color.black : Color.gray, 15, canAfford))
                        {
                            if (prog.BuySkin(skin.id))
                            {
                                prog.SelectSkin(skin.id);
                                OnSkinSelected?.Invoke(skin.id);
                            }
                        }
                    }
                }

                GUI.EndScrollView();
            }

            DrawBackButton();
        }

        public void DrawMissions(ProgressionManager prog)
        {
            float W = Screen.width;
            float H = Screen.height;

            DrawScreenHeader("EXPLORER MISSIONS", "Complete objectives for generous coin rewards", prog.TotalCoins);

            float cardW = Mathf.Min(500f, W - 40f);
            float cardH = 92f;
            float startY = 112f;
            float gap = 12f;

            missionsScrollPos = GUI.BeginScrollView(new Rect((W - cardW) * 0.5f, startY, cardW + 20f, H - 200f), missionsScrollPos, new Rect(0, 0, cardW, prog.Missions.Count * (cardH + gap)));

            for (int i = 0; i < prog.Missions.Count; i++)
            {
                Mission m = prog.Missions[i];
                Rect card = new Rect(0, i * (cardH + gap), cardW, cardH);
                
                Color border = m.completed ? GameConfig.GoldColor : new Color(0.3f, 0.35f, 0.45f, 0.5f);
                UIHelper.DrawGradientCard(card, new Color(0.14f, 0.17f, 0.24f, 0.95f), new Color(0.08f, 0.10f, 0.14f, 0.95f), border, m.completed ? 2f : 1.5f);

                // Mission icon
                string icon = m.type == 0 ? "★" : (m.type == 1 ? "↓" : "◈");
                Color iconCol = m.completed ? GameConfig.GoldColor : new Color(0.5f, 0.6f, 0.7f);
                UIHelper.Text(icon, new Rect(card.x + 12f, card.y + 12f, 28f, 28f), 22, iconCol, TextAnchor.MiddleCenter);

                UIHelper.Text(m.title, new Rect(card.x + 46f, card.y + 12f, 280f, 24f), 16, Color.white, TextAnchor.MiddleLeft);

                // Progress bar
                float pNorm = Mathf.Clamp01((float)m.current / m.target);
                Rect barRect = new Rect(card.x + 46f, card.y + 44f, 230f, 14f);
                Color barColor = m.completed ? GameConfig.GoldColor : GameConfig.AccentGreen;
                UIHelper.DrawProgressBar(barRect, pNorm, barColor, new Color(0.15f, 0.15f, 0.2f));
                UIHelper.Text(m.current + " / " + m.target, new Rect(barRect.xMax + 10f, card.y + 40f, 65f, 20f), 12, Color.white, TextAnchor.MiddleLeft);

                // Reward display
                UIHelper.Text("Reward: ★" + m.reward, new Rect(card.x + 46f, card.y + 64f, 140f, 18f), 12, new Color(0.7f, 0.7f, 0.5f), TextAnchor.MiddleLeft, false);

                Rect btnRect = new Rect(card.xMax - 118f, card.y + 24f, 102f, 44f);
                if (m.completed && !m.claimed)
                {
                    if (UIHelper.DrawAccentButton(btnRect, "CLAIM!", GameConfig.GoldColor, Color.black, 14))
                    {
                        prog.ClaimMission(i);
                    }
                }
                else if (m.claimed)
                {
                    UIHelper.DrawButton(btnRect, "✓ DONE", new Color(0.15f, 0.4f, 0.25f), Color.white, 13, false);
                }
                else
                {
                    UIHelper.DrawButton(btnRect, Mathf.RoundToInt(pNorm * 100f) + "%", new Color(0.2f, 0.22f, 0.28f), Color.gray, 14, false);
                }
            }

            GUI.EndScrollView();
            DrawBackButton();
        }

        public void DrawDailyRewards(ProgressionManager prog)
        {
            float W = Screen.width;
            float H = Screen.height;

            DrawScreenHeader("DAILY EXPEDITION LOG", "Return every day for powerful gifts", prog.TotalCoins);

            int streak = prog.DailyStreak;
            int currentDay = ((streak - 1) % 7) + 1;
            bool canClaim = prog.CanClaimDailyReward();

            // Streak display
            UIHelper.Text("STREAK: " + streak + " days", new Rect(W * 0.5f - 100f, 80f, 200f, 22f), 14, new Color(0.85f, 0.9f, 0.6f), TextAnchor.MiddleCenter);

            float gridW = Mathf.Min(570f, W - 40f);
            float cardW = (gridW - 30f) / 4f;
            float cardH = 100f;
            float startX = (W - gridW) * 0.5f;
            float startY = 115f;

            string[] dayLabels = new string[] { "Day 1", "Day 2", "Day 3", "Day 4", "Day 5", "Day 6", "Day 7" };
            string[] rewLabels = new string[] { "100 Gold", "200 Gold", "Shield", "350 Gold", "Magnet", "600 Gold", "1000+Flight" };
            string[] rewIcons = new string[] { "★", "★★", "⛊", "★★★", "◎", "★★★★", "✧" };

            for (int i = 0; i < 7; i++)
            {
                int dayNum = i + 1;
                int row = i / 4;
                int col = i % 4;

                Rect card = new Rect(startX + col * (cardW + 10f), startY + row * (cardH + 14f), cardW, cardH);
                bool isToday = (canClaim && dayNum == currentDay) || (!canClaim && dayNum == currentDay);
                bool isPast = dayNum < currentDay || (!canClaim && dayNum == currentDay);

                Color topBg = isToday ? new Color(0.22f, 0.28f, 0.40f, 0.95f) : new Color(0.12f, 0.15f, 0.20f, 0.92f);
                Color botBg = isToday ? new Color(0.12f, 0.16f, 0.28f, 0.95f) : new Color(0.06f, 0.08f, 0.12f, 0.92f);
                Color border = isToday ? GameConfig.GoldColor : (isPast ? GameConfig.AccentGreen : new Color(0.3f, 0.35f, 0.45f, 0.5f));

                if (isToday && canClaim)
                {
                    UIHelper.DrawGlow(card, new Color(GameConfig.GoldColor.r, GameConfig.GoldColor.g, GameConfig.GoldColor.b, 0.12f), 4f);
                }

                UIHelper.DrawGradientCard(card, topBg, botBg, border, isToday ? 2.5f : 1.5f);
                UIHelper.Text(dayLabels[i], new Rect(card.x, card.y + 8f, card.width, 18f), 14, Color.white, TextAnchor.MiddleCenter);
                UIHelper.Text(rewIcons[i], new Rect(card.x, card.y + 28f, card.width, 24f), 18, GameConfig.GoldColor, TextAnchor.MiddleCenter);
                UIHelper.Text(rewLabels[i], new Rect(card.x + 4f, card.y + 54f, card.width - 8f, 18f), 10, new Color(0.8f, 0.85f, 0.7f), TextAnchor.MiddleCenter, false);

                if (isPast && (!isToday || !canClaim))
                {
                    UIHelper.Text("✓", new Rect(card.x, card.y + 74f, card.width, 18f), 16, GameConfig.AccentGreen, TextAnchor.MiddleCenter);
                }
                else if (isToday && canClaim)
                {
                    UIHelper.PulseText("READY!", new Rect(card.x, card.y + 74f, card.width, 18f), 12, GameConfig.GoldColor, 5f, 0.12f);
                }
            }

            if (canClaim)
            {
                float btnW = 280f;
                Rect claimBtn = new Rect((W - btnW) * 0.5f, startY + (cardH + 14f) * 2 + 12f, btnW, 54f);
                if (UIHelper.DrawAccentButton(claimBtn, "🎁  CLAIM REWARD", GameConfig.GoldColor, Color.black, 19))
                {
                    prog.ClaimDailyReward();
                }
            }

            DrawBackButton();
        }

        public void DrawPause()
        {
            float W = Screen.width;
            float H = Screen.height;

            GUI.DrawTexture(new Rect(0, 0, W, H), UIHelper.GetGradientTexture(
                new Color(0.02f, 0.04f, 0.06f, 0.82f),
                new Color(0.06f, 0.08f, 0.12f, 0.88f)));

            float center = W * 0.5f;
            float centerY = H * 0.32f;

            UIHelper.Text("PAUSED", new Rect(center - 200f, centerY - 60f, 400f, 50f), 40, Color.white, TextAnchor.MiddleCenter);
            UIHelper.DrawSeparator(new Rect(center - 100f, centerY - 10f, 200f, 2f), new Color(1f, 1f, 1f, 0.3f));

            float btnW = 250f;
            float btnH = 48f;
            float gap = 14f;

            if (UIHelper.DrawAccentButton(new Rect(center - btnW * 0.5f, centerY + 24f, btnW, btnH), "▶ RESUME", new Color(0.18f, 0.72f, 0.35f), Color.white, 19))
            {
                OnResume?.Invoke();
            }

            if (UIHelper.DrawButton(new Rect(center - btnW * 0.5f, centerY + 24f + (btnH + gap), btnW, btnH), "↺ RETRY", new Color(0.18f, 0.48f, 0.82f), Color.white, 18))
            {
                OnRestart?.Invoke();
            }

            if (UIHelper.DrawButton(new Rect(center - btnW * 0.5f, centerY + 24f + (btnH + gap) * 2, btnW, btnH), "◀ MAIN MENU", new Color(0.28f, 0.30f, 0.36f), Color.white, 16))
            {
                OnBackToMenu?.Invoke();
            }
        }

        public void DrawGameOver(float runDist, int runCoins, int bestDist, bool isNewBest, bool canRevive)
        {
            float W = Screen.width;
            float H = Screen.height;

            GUI.DrawTexture(new Rect(0, 0, W, H), UIHelper.GetGradientTexture(
                new Color(0.08f, 0.03f, 0.03f, 0.85f),
                new Color(0.04f, 0.02f, 0.02f, 0.92f)));

            float center = W * 0.5f;
            float topY = H * 0.18f;

            // Title with red glow
            UIHelper.DrawGlow(new Rect(center - 200f, topY - 4f, 400f, 48f), new Color(0.9f, 0.2f, 0.2f, 0.1f), 8f);
            UIHelper.Text("EXPEDITION ENDED", new Rect(center - 250f, topY, 500f, 44f), 38, GameConfig.AccentRed, TextAnchor.MiddleCenter);

            // Summary Card
            Rect card = new Rect(center - 190f, topY + 64f, 380f, 140f);
            UIHelper.DrawGradientCard(card, new Color(0.14f, 0.16f, 0.22f, 0.95f), new Color(0.08f, 0.09f, 0.14f, 0.95f), new Color(0.5f, 0.5f, 0.6f, 0.4f), 1.5f);

            UIHelper.Text("DISTANCE", new Rect(card.x, card.y + 14f, card.width, 18f), 12, new Color(0.6f, 0.65f, 0.7f), TextAnchor.MiddleCenter, false);
            UIHelper.Text(Mathf.FloorToInt(runDist) + "m", new Rect(card.x, card.y + 32f, card.width, 30f), 26, Color.white, TextAnchor.MiddleCenter);

            UIHelper.DrawSeparator(new Rect(card.x + 20f, card.y + 68f, card.width - 40f, 1f), new Color(1f, 1f, 1f, 0.15f));

            UIHelper.Text("COINS RECOVERED", new Rect(card.x, card.y + 76f, card.width, 18f), 12, new Color(0.6f, 0.65f, 0.7f), TextAnchor.MiddleCenter, false);
            UIHelper.Text("★ " + runCoins, new Rect(card.x, card.y + 94f, card.width, 26f), 22, GameConfig.GoldColor, TextAnchor.MiddleCenter);

            if (isNewBest)
            {
                UIHelper.PulseText("★ NEW RECORD! ★", new Rect(card.x, card.y + 118f, card.width, 20f), 15, GameConfig.GoldColor, 4f, 0.1f);
            }
            else
            {
                UIHelper.Text("Best: " + bestDist + "m", new Rect(card.x, card.y + 118f, card.width, 18f), 13, new Color(0.6f, 0.65f, 0.7f), TextAnchor.MiddleCenter, false);
            }

            // Buttons
            float btnW = 270f;
            float btnH = 48f;
            float startBtnY = card.yMax + 22f;

            if (canRevive)
            {
                if (UIHelper.DrawAccentButton(new Rect(center - btnW * 0.5f, startBtnY, btnW, btnH), "⛊ REVIVE WITH SHIELD", new Color(0.2f, 0.6f, 0.92f), Color.white, 16))
                {
                    OnRevive?.Invoke();
                }
                startBtnY += btnH + 12f;
            }

            if (UIHelper.DrawButton(new Rect(center - btnW * 0.5f, startBtnY, btnW, btnH), "↺ RUN AGAIN", new Color(0.18f, 0.72f, 0.35f), Color.white, 18))
            {
                OnRestart?.Invoke();
            }

            if (UIHelper.DrawButton(new Rect(center - btnW * 0.5f, startBtnY + btnH + 12f, btnW, btnH), "◀ MAIN MENU", new Color(0.28f, 0.30f, 0.36f), Color.white, 16))
            {
                OnBackToMenu?.Invoke();
            }
        }

        public void DrawVictory(int stageId, int coinsCollected, int stars)
        {
            float W = Screen.width;
            float H = Screen.height;

            victoryStarAnim += Time.unscaledDeltaTime;

            GUI.DrawTexture(new Rect(0, 0, W, H), UIHelper.GetGradientTexture(
                new Color(0.06f, 0.10f, 0.06f, 0.88f),
                new Color(0.02f, 0.06f, 0.04f, 0.94f)));

            float center = W * 0.5f;
            float topY = H * 0.16f;

            LevelData stage = GameConfig.Stages[Mathf.Clamp(stageId - 1, 0, GameConfig.Stages.Length - 1)];

            // Victory glow
            UIHelper.DrawGlow(new Rect(center - 200f, topY - 4f, 400f, 48f), new Color(GameConfig.GoldColor.r, GameConfig.GoldColor.g, GameConfig.GoldColor.b, 0.12f), 12f);
            UIHelper.Text("STAGE CLEARED!", new Rect(center - 250f, topY, 500f, 44f), 40, GameConfig.GoldColor, TextAnchor.MiddleCenter);
            UIHelper.Text(stage.name, new Rect(center - 250f, topY + 48f, 500f, 26f), 20, Color.white, TextAnchor.MiddleCenter);

            // Animated 3-Star Rating
            string starStr = "";
            for (int s = 1; s <= 3; s++)
            {
                float starDelay = s * 0.4f;
                bool earned = s <= stars;
                float starScale = earned ? Mathf.Clamp01((victoryStarAnim - starDelay) * 4f) : 1f;
                starStr += (earned ? "★ " : "☆ ");
            }
            UIHelper.Text(starStr, new Rect(center - 200f, topY + 84f, 400f, 50f), 44, GameConfig.GoldColor, TextAnchor.MiddleCenter);

            // Reward Card
            Rect card = new Rect(center - 190f, topY + 148f, 380f, 90f);
            UIHelper.DrawGradientCard(card, new Color(0.14f, 0.17f, 0.22f, 0.95f), new Color(0.08f, 0.10f, 0.14f, 0.95f), new Color(0.5f, 0.5f, 0.4f, 0.4f), 1.5f);
            UIHelper.Text("STAGE REWARD", new Rect(card.x, card.y + 12f, card.width, 18f), 12, new Color(0.6f, 0.65f, 0.7f), TextAnchor.MiddleCenter, false);
            UIHelper.Text("★ " + stage.coinReward, new Rect(card.x, card.y + 30f, card.width, 24f), 20, GameConfig.GoldColor, TextAnchor.MiddleCenter);
            UIHelper.DrawSeparator(new Rect(card.x + 20f, card.y + 58f, card.width - 40f, 1f), new Color(1f, 1f, 1f, 0.15f));
            UIHelper.Text("Coins Collected: ★ " + coinsCollected, new Rect(card.x, card.y + 64f, card.width, 20f), 14, Color.white, TextAnchor.MiddleCenter);

            float btnW = 270f;
            float btnH = 48f;
            float startBtnY = card.yMax + 22f;

            if (stageId < GameConfig.Stages.Length)
            {
                if (UIHelper.DrawAccentButton(new Rect(center - btnW * 0.5f, startBtnY, btnW, btnH), "NEXT STAGE ▶", new Color(0.18f, 0.72f, 0.35f), Color.white, 19))
                {
                    OnNextStage?.Invoke();
                }
                startBtnY += btnH + 12f;
            }

            if (UIHelper.DrawButton(new Rect(center - btnW * 0.5f, startBtnY, btnW, btnH), "🗺 EXPEDITIONS MAP", new Color(0.18f, 0.48f, 0.82f), Color.white, 16))
            {
                OnOpenLevels?.Invoke();
            }

            if (UIHelper.DrawButton(new Rect(center - btnW * 0.5f, startBtnY + btnH + 12f, btnW, btnH), "◀ MAIN MENU", new Color(0.28f, 0.30f, 0.36f), Color.white, 16))
            {
                OnBackToMenu?.Invoke();
            }
        }

        void DrawScreenHeader(string title, string subtitle, int bankCoins)
        {
            float W = Screen.width;

            // Coins pill
            Rect coinCard = new Rect(24f, 22f, 175f, 42f);
            UIHelper.DrawGradientCard(coinCard, new Color(0.14f, 0.12f, 0.08f, 0.92f), new Color(0.08f, 0.06f, 0.03f, 0.92f), GameConfig.GoldColor, 1.5f);
            UIHelper.Text("★ " + bankCoins, coinCard, 17, GameConfig.GoldColor, TextAnchor.MiddleCenter);

            // Header
            UIHelper.Text(title, new Rect(W * 0.5f - 250f, 18f, 500f, 32f), 25, Color.white, TextAnchor.MiddleCenter);
            UIHelper.DrawSeparator(new Rect(W * 0.5f - 120f, 50f, 240f, 1.5f), new Color(1f, 1f, 1f, 0.25f));
            UIHelper.Text(subtitle, new Rect(W * 0.5f - 250f, 54f, 500f, 20f), 12, new Color(0.65f, 0.75f, 0.82f), TextAnchor.MiddleCenter);
        }

        void DrawBackButton()
        {
            float W = Screen.width;
            float H = Screen.height;
            Rect backBtn = new Rect((W - 190f) * 0.5f, H - 66f, 190f, 46f);
            if (UIHelper.DrawButton(backBtn, "◀  BACK", new Color(0.22f, 0.25f, 0.32f), Color.white, 16))
            {
                OnBackToMenu?.Invoke();
            }
        }
    }
}
