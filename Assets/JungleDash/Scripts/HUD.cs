// HUD.cs - Premium in-run HUD: distance gauge, animated coin counter, combo tiers, power-up bars, floating popups, and speed indicators.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace JungleDash
{
    public class HUD : MonoBehaviour
    {
        public Action OnPauseRequested;

        readonly List<FloatingPopup> popups = new List<FloatingPopup>();
        string bannerText = "";
        float bannerTimer = 0f;
        float bannerMaxTimer = 0f;

        // Animated counters
        float displayDistance;
        float displayCoins;
        float comboFlashTimer;
        int lastCombo;

        public void AddPopup(string txt, Color c, Vector2 screenPos, float scale = 1f)
        {
            popups.Add(new FloatingPopup
            {
                text = txt,
                color = c,
                time = 1.4f,
                maxTime = 1.4f,
                startPos = screenPos,
                scale = scale
            });
        }

        public void ShowBanner(string text, float duration = 1.6f)
        {
            bannerText = text;
            bannerTimer = duration;
            bannerMaxTimer = duration;
        }

        public void Tick(float dt)
        {
            if (bannerTimer > 0f) bannerTimer -= dt;
            if (comboFlashTimer > 0f) comboFlashTimer -= dt;

            for (int i = popups.Count - 1; i >= 0; i--)
            {
                var p = popups[i];
                p.time -= dt;
                if (p.time <= 0f)
                {
                    popups.RemoveAt(i);
                }
                else
                {
                    popups[i] = p;
                }
            }
        }

        public void DrawHUD(
            float currentDist,
            int runCoins,
            int combo,
            float comboTimer,
            int currentStageId,
            float stageTargetDist,
            float shieldTimer, float maxShield,
            float magnetTimer, float maxMagnet,
            float speedTimer, float maxSpeed,
            float flyTimer, float maxFly,
            float doubleTimer, float maxDouble)
        {
            float W = Screen.width;
            float H = Screen.height;

            // Animate counters smoothly
            displayDistance = Mathf.Lerp(displayDistance, currentDist, 1f - Mathf.Exp(-12f * Time.deltaTime));
            displayCoins = Mathf.Lerp(displayCoins, runCoins, 1f - Mathf.Exp(-10f * Time.deltaTime));

            // Combo flash detection
            if (combo > lastCombo && combo > 1)
            {
                comboFlashTimer = 0.3f;
            }
            lastCombo = combo;

            // ---- TOP STATUS BAR ----
            // Distance card
            Rect distCard = new Rect(16f, 16f, 200f, 46f);
            UIHelper.DrawGradientCard(distCard, new Color(0.12f, 0.14f, 0.20f, 0.9f), new Color(0.06f, 0.08f, 0.12f, 0.9f), new Color(0.4f, 0.5f, 0.6f, 0.4f), 1.5f);
            
            // Distance icon
            UIHelper.Text("◈", new Rect(distCard.x + 8f, distCard.y, 28f, distCard.height), 18, new Color(0.6f, 0.8f, 1f), TextAnchor.MiddleCenter);
            UIHelper.Text(Mathf.FloorToInt(displayDistance) + "m", new Rect(distCard.x + 34f, distCard.y, 160f, distCard.height), 22, Color.white, TextAnchor.MiddleLeft);

            // Coins card with animated scale on pickup
            float coinPulse = displayCoins < runCoins ? 1.05f : 1f;
            Rect coinCard = new Rect(distCard.xMax + 10f, 16f, 145f, 46f);
            UIHelper.DrawGradientCard(coinCard, new Color(0.18f, 0.15f, 0.08f, 0.9f), new Color(0.10f, 0.08f, 0.04f, 0.9f), GameConfig.GoldColor, 1.5f);
            UIHelper.Text("★ " + Mathf.FloorToInt(displayCoins), coinCard, Mathf.RoundToInt(20 * coinPulse), GameConfig.GoldColor, TextAnchor.MiddleCenter);

            // Pause Button
            Rect pauseRect = new Rect(W - 66f, 16f, 50f, 46f);
            if (UIHelper.DrawButton(pauseRect, "▐▐", new Color(0.15f, 0.18f, 0.25f, 0.9f), Color.white, 18))
            {
                OnPauseRequested?.Invoke();
            }

            // Speed indicator
            if (speedTimer > 0f)
            {
                Rect speedLabel = new Rect(W - 130f, 16f, 56f, 46f);
                float speedFlash = Mathf.PingPong(Time.time * 6f, 1f);
                Color speedCol = Color.Lerp(new Color(1f, 0.65f, 0.1f), new Color(1f, 0.9f, 0.4f), speedFlash);
                UIHelper.Text("⚡", speedLabel, 24, speedCol, TextAnchor.MiddleCenter);
            }

            // ---- STAGE PROGRESS BAR ----
            if (currentStageId > 0 && stageTargetDist > 0f)
            {
                float prog = Mathf.Clamp01(currentDist / stageTargetDist);
                float barW = Mathf.Min(420f, W - 120f);
                Rect progBg = new Rect((W - barW) * 0.5f, 72f, barW, 18f);
                
                UIHelper.DrawGradientCard(progBg, new Color(0.1f, 0.12f, 0.16f, 0.85f), new Color(0.06f, 0.07f, 0.10f, 0.85f), new Color(0.3f, 0.4f, 0.3f, 0.4f), 1f);
                UIHelper.DrawPulseBar(new Rect(progBg.x + 2f, progBg.y + 2f, progBg.width - 4f, progBg.height - 4f), prog, GameConfig.AccentGreen, new Color(0.1f, 0.12f, 0.16f, 0.5f));

                LevelData stage = GameConfig.Stages[currentStageId - 1];
                UIHelper.Text(stage.name + "  " + Mathf.RoundToInt(prog * 100f) + "%", new Rect((W - barW) * 0.5f, 92f, barW, 18f), 12, new Color(0.85f, 0.9f, 0.88f), TextAnchor.MiddleCenter);

                // Start/End markers
                UIHelper.Text("0m", new Rect(progBg.x, progBg.yMax + 2f, 40f, 14f), 10, new Color(0.6f, 0.65f, 0.7f), TextAnchor.MiddleLeft, false);
                UIHelper.Text(Mathf.FloorToInt(stageTargetDist) + "m", new Rect(progBg.xMax - 50f, progBg.yMax + 2f, 50f, 14f), 10, new Color(0.6f, 0.65f, 0.7f), TextAnchor.MiddleRight, false);
            }

            // ---- COMBO INDICATOR ----
            if (combo > 1 && comboTimer > 0f)
            {
                float comboAlpha = Mathf.Clamp01(comboTimer * 2f);
                Color comboColor;
                string comboLabel;
                if (combo >= 10)
                {
                    comboColor = GameConfig.AccentPurple;
                    comboLabel = "★ LEGENDARY x" + combo + "! ★";
                }
                else if (combo >= 5)
                {
                    comboColor = GameConfig.GemColor;
                    comboLabel = "✦ MEGA x" + combo + "!";
                }
                else if (combo >= 3)
                {
                    comboColor = GameConfig.GoldColor;
                    comboLabel = "COMBO x" + combo + "!";
                }
                else
                {
                    comboColor = Color.white;
                    comboLabel = "x" + combo;
                }

                comboColor = new Color(comboColor.r, comboColor.g, comboColor.b, comboAlpha);
                float comboScale = 1f;
                if (comboFlashTimer > 0f) comboScale = 1f + comboFlashTimer * 1.5f;

                Rect comboRect = new Rect(16f, 72f, 220f, 32f);
                UIHelper.Text(comboLabel, comboRect, Mathf.RoundToInt(18 * comboScale), comboColor, TextAnchor.MiddleLeft);

                // Combo timer bar
                float comboNorm = Mathf.Clamp01(comboTimer / 2.2f);
                Rect comboBar = new Rect(16f, 104f, 140f, 4f);
                UIHelper.DrawProgressBar(comboBar, comboNorm, comboColor, new Color(0.2f, 0.2f, 0.2f, 0.5f));
            }

            // ---- ACTIVE POWER-UP TIMERS ----
            float powerY = H - 52f;
            if (shieldTimer > 0f)
            {
                DrawPowerUpGauge(14f, powerY, "⛊ SHIELD", new Color(0.3f, 0.65f, 1f), shieldTimer, maxShield);
                powerY -= 38f;
            }
            if (magnetTimer > 0f)
            {
                DrawPowerUpGauge(14f, powerY, "◎ MAGNET", new Color(1f, 0.35f, 0.35f), magnetTimer, maxMagnet);
                powerY -= 38f;
            }
            if (speedTimer > 0f)
            {
                DrawPowerUpGauge(14f, powerY, "⚡ DASH", new Color(1f, 0.65f, 0.1f), speedTimer, maxSpeed);
                powerY -= 38f;
            }
            if (flyTimer > 0f)
            {
                DrawPowerUpGauge(14f, powerY, "✧ FLIGHT", new Color(0.85f, 0.45f, 1f), flyTimer, maxFly);
                powerY -= 38f;
            }
            if (doubleTimer > 0f)
            {
                DrawPowerUpGauge(14f, powerY, "★ 2X", new Color(1f, 0.9f, 0.2f), doubleTimer, maxDouble);
            }

            // ---- CENTER BANNER ----
            if (bannerTimer > 0f)
            {
                float bannerNorm = bannerTimer / bannerMaxTimer;
                float alphaIn = Mathf.Clamp01((1f - bannerNorm) * 8f); // Quick fade in
                float alphaOut = Mathf.Clamp01(bannerTimer * 4f); // Slow fade out
                float alpha = Mathf.Min(alphaIn, alphaOut);
                
                float slideIn = Mathf.Lerp(0f, 1f, Mathf.Clamp01((1f - bannerNorm) * 6f));
                float bannerW = 380f * slideIn;

                Rect bCard = new Rect((W - bannerW) * 0.5f, H * 0.26f, bannerW, 52f);
                UIHelper.DrawGradientCard(bCard,
                    new Color(0.12f, 0.16f, 0.24f, 0.92f * alpha),
                    new Color(0.06f, 0.08f, 0.14f, 0.92f * alpha),
                    new Color(1f, 0.85f, 0.2f, alpha), 2f);

                Color textCol = new Color(1f, 0.95f, 0.4f, alpha);
                Rect bRect = new Rect(0, H * 0.26f, W, 52f);
                UIHelper.Text(bannerText, bRect, 22, textCol, TextAnchor.MiddleCenter);
            }

            // ---- FLOATING POPUPS ----
            foreach (var pop in popups)
            {
                float tNorm = 1f - (pop.time / pop.maxTime);
                float yOffset = tNorm * 70f;
                float popAlpha = Mathf.Clamp01(pop.time * 2.8f);
                float scaleAnim = 1f + (1f - tNorm) * 0.3f * (tNorm < 0.15f ? 1f : 0f); // Pop-in scale
                Color c = new Color(pop.color.r, pop.color.g, pop.color.b, popAlpha);

                Rect r = new Rect(pop.startPos.x - 90f, pop.startPos.y - yOffset, 180f, 30f);
                UIHelper.Text(pop.text, r, Mathf.RoundToInt(18 * pop.scale * scaleAnim), c, TextAnchor.MiddleCenter);
            }
        }

        void DrawPowerUpGauge(float x, float y, string label, Color color, float cur, float max)
        {
            Rect card = new Rect(x, y, 200f, 32f);
            UIHelper.DrawGradientCard(card, new Color(0.10f, 0.12f, 0.18f, 0.9f), new Color(0.06f, 0.07f, 0.10f, 0.9f), new Color(color.r, color.g, color.b, 0.5f), 1f);

            // Label
            UIHelper.Text(label, new Rect(card.x + 8f, card.y, 80f, card.height), 12, Color.white, TextAnchor.MiddleLeft, false);

            // Timer bar
            float norm = cur / Mathf.Max(0.1f, max);
            Rect prog = new Rect(card.x + 84f, card.y + 9f, 74f, 14f);
            
            // Flash when about to expire
            Color barColor = cur < 3f ? Color.Lerp(color, Color.red, Mathf.PingPong(Time.time * 6f, 1f)) : color;
            UIHelper.DrawProgressBar(prog, norm, barColor, new Color(0.15f, 0.15f, 0.2f, 0.8f));

            // Time remaining text
            UIHelper.Text(Mathf.CeilToInt(cur) + "s", new Rect(card.xMax - 35f, card.y, 30f, card.height), 11, new Color(0.8f, 0.85f, 0.9f), TextAnchor.MiddleRight, false);
        }
    }
}
