// MatFactory.cs - centralized material/shader factory for URP 17 (Unity 6)
// FIX: replaced ??= (C# 8) with explicit null checks (C# 7.3 compatible)
using UnityEngine;

namespace JungleDash
{
    public static class MatFactory
    {
        private static Shader _lit;
        private static Shader _unlit;

        public static Shader Lit
        {
            get
            {
                if (_lit == null)
                {
                    _lit = Shader.Find("Universal Render Pipeline/Lit");
                    if (_lit == null) _lit = Shader.Find("Universal Render Pipeline/Simple Lit");
                    if (_lit == null) _lit = Shader.Find("Standard");
                }
                return _lit;
            }
        }

        public static Shader Unlit
        {
            get
            {
                if (_unlit == null)
                {
                    _unlit = Shader.Find("Universal Render Pipeline/Unlit");
                    if (_unlit == null) _unlit = Shader.Find("Unlit/Color");
                    if (_unlit == null) _unlit = Shader.Find("Standard");
                }
                return _unlit;
            }
        }

        // Opaque PBR material
        public static Material Opaque(Color c, float metal = 0f, float smooth = 0.3f)
        {
            var m = new Material(Lit);
            Apply(m, c, metal, smooth);
            return m;
        }

        // Emissive (collectibles/fx) - Unlit for consistent brightness regardless of lighting
        public static Material Glow(Color c, float brightness = 1.8f)
        {
            var sh = Unlit;
            var m  = new Material(sh);
            Color bright = c * brightness;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", bright);
            if (m.HasProperty("_Color"))     m.SetColor("_Color",     bright);
            if (m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * brightness * 1.5f);
            }
            return m;
        }

        // Texture + tint
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

        private static void Apply(Material m, Color c, float metal, float smooth)
        {
            if (m.HasProperty("_BaseColor"))  m.SetColor("_BaseColor",  c);
            if (m.HasProperty("_Color"))      m.SetColor("_Color",      c);
            if (m.HasProperty("_Metallic"))   m.SetFloat("_Metallic",   metal);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
        }
    }
}
