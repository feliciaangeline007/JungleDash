// MatFactory.cs – central shader/material factory for URP 17 (Unity 6000.6.3f1)
// Resolves the correct shader at runtime so we NEVER get a pink material.
using UnityEngine;

namespace JungleDash
{
    public static class MatFactory
    {
        private static Shader _lit;
        private static Shader _unlit;

        public static Shader Lit =>
            _lit ??= Shader.Find("Universal Render Pipeline/Lit")
                  ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                  ?? Shader.Find("Standard");

        public static Shader Unlit =>
            _unlit ??= Shader.Find("Universal Render Pipeline/Unlit")
                    ?? Shader.Find("Unlit/Color")
                    ?? Shader.Find("Standard");

        // Opaque PBR material
        public static Material Opaque(Color c, float metal = 0f, float smooth = 0.3f)
        {
            var m = new Material(Lit);
            Set(m, c, metal, smooth);
            return m;
        }

        // Emissive (collectibles/effects) – uses Unlit for consistent brightness
        public static Material Glow(Color c, float brightness = 1.8f)
        {
            var m = new Material(Unlit);
            Color bright = c * brightness;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", bright);
            if (m.HasProperty("_Color"))     m.SetColor("_Color",     bright);
            // Also enable emission for Lit fallback
            if (m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * brightness * 1.5f);
            }
            return m;
        }

        // Texture + tint material
        public static Material Textured(Texture2D tex, Color tint, Vector2 tiling)
        {
            var m = Opaque(tint);
            if (tex == null) return m;
            if (m.HasProperty("_BaseMap"))
            {
                m.SetTexture("_BaseMap", tex);
                m.SetTextureScale("_BaseMap", tiling);
            }
            else
            {
                m.mainTexture      = tex;
                m.mainTextureScale = tiling;
            }
            return m;
        }

        private static void Set(Material m, Color c, float metal, float smooth)
        {
            if (m.HasProperty("_BaseColor"))  m.SetColor("_BaseColor",  c);
            if (m.HasProperty("_Color"))      m.SetColor("_Color",      c);
            if (m.HasProperty("_Metallic"))   m.SetFloat("_Metallic",   metal);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
        }
    }
}
