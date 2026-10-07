// UIHelper.cs - Premium procedural styling: gradient cards, glow effects, responsive GUI scaling, zero-GC caching.
using System.Collections.Generic;
using UnityEngine;

namespace JungleDash
{
    public static class UIHelper
    {
        static readonly Dictionary<Color, Texture2D> colorTexCache = new Dictionary<Color, Texture2D>();
        static readonly Dictionary<string, Texture2D> gradientCache = new Dictionary<string, Texture2D>();
        static GUIStyle labelStyle;
        static GUIStyle labelStyleNoShadow;
        static Matrix4x4 originalMatrix;

        // Fast string caches to avoid garbage collection per frame
        static readonly string[] distanceCache = new string[5001];
        static readonly string[] coinCache = new string[2001];

        static UIHelper()
        {
            for (int i = 0; i < distanceCache.Length; i++)
                distanceCache[i] = i + " m";

            for (int i = 0; i < coinCache.Length; i++)
                coinCache[i] = "★ " + i;
        }

        public static string FormatDistance(int meters)
        {
            if (meters >= 0 && meters < distanceCache.Length) return distanceCache[meters];
            return meters + " m";
        }

        public static string FormatCoins(int coins)
        {
            if (coins >= 0 && coins < coinCache.Length) return coinCache[coins];
            return "★ " + coins;
        }

        public static void BeginScaledGUI(out float virtualW, out float virtualH)
        {
            originalMatrix = GUI.matrix;
            virtualH = 1080f;
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            virtualW = virtualH * aspect;

            float scale = Screen.height / virtualH;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
        }

        public static void EndScaledGUI()
        {
            GUI.matrix = originalMatrix;
        }

        public static Texture2D GetColorTexture(Color col)
        {
            if (colorTexCache.TryGetValue(col, out Texture2D tex) && tex != null)
                return tex;

            tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.DontSave;
            Color[] pix = new Color[] { col, col, col, col };
            tex.SetPixels(pix);
            tex.Apply();
            colorTexCache[col] = tex;
            return tex;
        }

        // Vertical gradient texture for premium card backgrounds
        public static Texture2D GetGradientTexture(Color top, Color bottom, int height = 32)
        {
            string key = top.ToString() + bottom.ToString() + height;
            if (gradientCache.TryGetValue(key, out Texture2D tex) && tex != null) return tex;

            tex = new Texture2D(1, height, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.DontSave;
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < height; y++)
            {
                float t = y / (float)(height - 1);
                tex.SetPixel(0, y, Color.Lerp(bottom, top, t));
            }
            tex.Apply();
            gradientCache[key] = tex;
            return tex;
        }

        public static void DrawCard(Rect r, Color bgColor, Color borderColor, float borderWidth = 1.5f)
        {
            // Soft shadow (larger, more diffuse)
            Color shadowCol = new Color(0f, 0f, 0f, 0.35f);
            GUI.DrawTexture(new Rect(r.x + 2f, r.y + 3f, r.width + 2f, r.height + 2f), GetColorTexture(shadowCol));
            GUI.DrawTexture(new Rect(r.x + 4f, r.y + 5f, r.width, r.height), GetColorTexture(new Color(0f, 0f, 0f, 0.2f)));

            // Outer border
            GUI.DrawTexture(r, GetColorTexture(borderColor));

            // Inner background with subtle gradient
            Rect inner = new Rect(r.x + borderWidth, r.y + borderWidth, r.width - borderWidth * 2f, r.height - borderWidth * 2f);
            Color topColor = Color.Lerp(bgColor, Color.white, 0.06f);
            GUI.DrawTexture(inner, GetGradientTexture(topColor, bgColor));

            // Top highlight line (glass effect)
            Rect highlight = new Rect(inner.x + 2f, inner.y, inner.width - 4f, 1f);
            GUI.DrawTexture(highlight, GetColorTexture(new Color(1f, 1f, 1f, 0.12f)));
        }

        // Premium gradient card with two-tone background
        public static void DrawGradientCard(Rect r, Color topColor, Color bottomColor, Color borderColor, float borderWidth = 1.5f)
        {
            // Shadow
            GUI.DrawTexture(new Rect(r.x + 3f, r.y + 4f, r.width, r.height), GetColorTexture(new Color(0f, 0f, 0f, 0.4f)));

            // Border
            GUI.DrawTexture(r, GetColorTexture(borderColor));

            // Gradient fill
            Rect inner = new Rect(r.x + borderWidth, r.y + borderWidth, r.width - borderWidth * 2f, r.height - borderWidth * 2f);
            GUI.DrawTexture(inner, GetGradientTexture(topColor, bottomColor));

            // Top highlight
            GUI.DrawTexture(new Rect(inner.x + 2f, inner.y, inner.width - 4f, 1f), GetColorTexture(new Color(1f, 1f, 1f, 0.15f)));
        }

        public static void DrawProgressBar(Rect r, float progress, Color fillColor, Color bgColor)
        {
            progress = Mathf.Clamp01(progress);

            // Background
            GUI.DrawTexture(r, GetColorTexture(bgColor));

            // Fill with gradient
            if (progress > 0.005f)
            {
                Rect fill = new Rect(r.x + 1f, r.y + 1f, (r.width - 2f) * progress, r.height - 2f);
                Color bright = Color.Lerp(fillColor, Color.white, 0.25f);
                GUI.DrawTexture(fill, GetGradientTexture(bright, fillColor));

                // Bright edge sheen
                Rect edge = new Rect(fill.xMax - 2f, fill.y, 2f, fill.height);
                GUI.DrawTexture(edge, GetColorTexture(Color.white));

                // Inner glow line
                Rect glow = new Rect(fill.x, fill.y, fill.width, 1f);
                GUI.DrawTexture(glow, GetColorTexture(new Color(1f, 1f, 1f, 0.3f)));
            }

            // Thin border outline
            DrawOutline(r, new Color(1f, 1f, 1f, 0.2f), 1f);
        }

        // Animated progress bar with pulse
        public static void DrawPulseBar(Rect r, float progress, Color fillColor, Color bgColor, float pulseSpeed = 3f)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * 0.08f;
            Color pulsedColor = Color.Lerp(fillColor, Color.white, (pulse - 1f) * 2f);
            DrawProgressBar(r, progress, pulsedColor, bgColor);
        }

        public static void DrawOutline(Rect r, Color c, float width = 1f)
        {
            Texture2D t = GetColorTexture(c);
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, width), t);
            GUI.DrawTexture(new Rect(r.x, r.yMax - width, r.width, width), t);
            GUI.DrawTexture(new Rect(r.x, r.y, width, r.height), t);
            GUI.DrawTexture(new Rect(r.xMax - width, r.y, width, r.height), t);
        }

        // Glow outline effect
        public static void DrawGlow(Rect r, Color c, float size = 4f)
        {
            Color glowOuter = new Color(c.r, c.g, c.b, c.a * 0.15f);
            Color glowInner = new Color(c.r, c.g, c.b, c.a * 0.3f);
            Rect outer = new Rect(r.x - size, r.y - size, r.width + size * 2f, r.height + size * 2f);
            Rect inner = new Rect(r.x - size * 0.5f, r.y - size * 0.5f, r.width + size, r.height + size);
            GUI.DrawTexture(outer, GetColorTexture(glowOuter));
            GUI.DrawTexture(inner, GetColorTexture(glowInner));
        }

        public static bool DrawButton(Rect r, string text, Color bgColor, Color textColor, int fontSize = 18, bool active = true)
        {
            Vector2 mousePos = Event.current.mousePosition;
            bool hovered = r.Contains(mousePos);

            Color currentBg = active ? (hovered ? Color.Lerp(bgColor, Color.white, 0.2f) : bgColor) : new Color(0.2f, 0.2f, 0.25f, 0.6f);
            Color currentBorder = hovered && active ? Color.Lerp(bgColor, Color.white, 0.6f) : new Color(1f, 1f, 1f, 0.3f);

            // Hover glow
            if (hovered && active)
            {
                DrawGlow(r, bgColor, 3f);
            }

            // Gradient button background
            Color topBg = Color.Lerp(currentBg, Color.white, 0.12f);
            DrawGradientCard(r, topBg, currentBg, currentBorder, 1.5f);

            Text(text, r, fontSize, active ? textColor : new Color(0.6f, 0.6f, 0.6f), TextAnchor.MiddleCenter);

            if (active && GUI.Button(r, GUIContent.none, GUIStyle.none))
            {
                return true;
            }
            return false;
        }

        // Accent button with colored glow
        public static bool DrawAccentButton(Rect r, string text, Color bgColor, Color textColor, int fontSize = 18)
        {
            float pulse = 0.5f + Mathf.Sin(Time.time * 3f) * 0.5f;
            Color glowCol = new Color(bgColor.r, bgColor.g, bgColor.b, 0.15f + pulse * 0.1f);
            DrawGlow(r, glowCol, 5f);
            return DrawButton(r, text, bgColor, textColor, fontSize, true);
        }

        public static float DrawSlider(Rect r, string label, float value, float min, float max, Color accent)
        {
            Rect labelRect = new Rect(r.x, r.y, 140f, r.height);
            Text(label, labelRect, 16, Color.white, TextAnchor.MiddleLeft);

            Rect trackRect = new Rect(r.x + 150f, r.y + (r.height - 12f) * 0.5f, r.width - 230f, 12f);
            float norm = Mathf.Clamp01((value - min) / Mathf.Max(0.001f, max - min));
            DrawProgressBar(trackRect, norm, accent, new Color(0.15f, 0.18f, 0.22f));

            Rect valRect = new Rect(trackRect.xMax + 12f, r.y, 60f, r.height);
            Text(Mathf.RoundToInt(norm * 100f) + "%", valRect, 16, Color.white, TextAnchor.MiddleRight);

            Event e = Event.current;
            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && trackRect.Contains(e.mousePosition))
            {
                float newNorm = Mathf.Clamp01((e.mousePosition.x - trackRect.x) / trackRect.width);
                value = Mathf.Lerp(min, max, newNorm);
                e.Use();
            }

            return value;
        }

        // Separator line
        public static void DrawSeparator(Rect r, Color c)
        {
            Color left = new Color(c.r, c.g, c.b, 0f);
            Color center = c;
            float halfW = r.width * 0.5f;

            // Left fade
            GUI.DrawTexture(new Rect(r.x, r.y, halfW, r.height), GetGradientTexture(center, left, 2));
            // Right fade (reuse reversed)
            GUI.DrawTexture(new Rect(r.x + halfW, r.y, halfW, r.height), GetGradientTexture(left, center, 2));
        }

        public static void Text(string s, Rect r, int size, Color c, TextAnchor anchor = TextAnchor.MiddleCenter, bool shadow = true)
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.fontStyle = FontStyle.Bold;
                labelStyle.wordWrap = false;
                labelStyle.clipping = TextClipping.Overflow;
            }

            labelStyle.fontSize = size;
            labelStyle.alignment = anchor;

            if (shadow)
            {
                labelStyle.normal.textColor = new Color(0f, 0f, 0f, 0.65f);
                GUI.Label(new Rect(r.x + 1.5f, r.y + 2f, r.width, r.height), s, labelStyle);
            }

            labelStyle.normal.textColor = c;
            GUI.Label(r, s, labelStyle);
        }

        // Animated text with size pulse
        public static void PulseText(string s, Rect r, int baseSize, Color c, float speed = 4f, float amount = 0.08f)
        {
            float pulse = 1f + Mathf.Sin(Time.time * speed) * amount;
            Text(s, r, Mathf.RoundToInt(baseSize * pulse), c, TextAnchor.MiddleCenter);
        }
    }
}
