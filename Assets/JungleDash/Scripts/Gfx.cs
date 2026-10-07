// Gfx.cs - Material + primitive helpers (URP first, Built-in fallback).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace JungleDash
{
    public enum MatKind { Opaque, Cutout, Emissive, Glass }

    public static class Mats
    {
        public const string ResDir = "JDMaterials/";
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        public static Shader FindShader()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Sprites/Default");
            Debug.Log("[JungleDash] FindShader returned: " + (s != null ? s.name : "NULL") + " (Pipeline: " + (GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.name : "null") + ")");
            return s;
        }

        /// <summary>Sets shader keywords / properties for a material kind. Used at runtime and by the editor.</summary>
        public static void Configure(Material m, MatKind k)
        {
            bool urp = m.HasProperty("_BaseColor");
            switch (k)
            {
                case MatKind.Cutout:
                    if (urp)
                    {
                        m.SetFloat("_AlphaClip", 1f);
                        m.SetFloat("_Cutoff", 0.5f);
                        m.SetFloat("_Cull", 0f);
                    }
                    else
                    {
                        m.SetFloat("_Mode", 1f);
                        m.SetFloat("_Cutoff", 0.5f);
                    }
                    m.EnableKeyword("_ALPHATEST_ON");
                    m.renderQueue = 2450;
                    break;
                case MatKind.Emissive:
                    m.EnableKeyword("_EMISSION");
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    break;
                case MatKind.Glass:
                    if (urp)
                    {
                        m.SetFloat("_Surface", 1f);
                        m.SetFloat("_Blend", 0f);
                        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    }
                    else
                    {
                        m.SetFloat("_Mode", 3f);
                        m.EnableKeyword("_ALPHABLEND_ON");
                    }
                    m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                    m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                    m.SetFloat("_ZWrite", 0f);
                    m.SetOverrideTag("RenderType", "Transparent");
                    m.renderQueue = 3000;
                    break;
            }
        }

        static Material Template(MatKind k)
        {
            Material t = Resources.Load<Material>(ResDir + "JD_" + k);
            if (t != null) return new Material(t);
            Material m = new Material(FindShader());
            Configure(m, k);
            return m;
        }

        public static Material Get(MatKind k, Color c, Texture tex = null, float tx = 1f, float ty = 1f,
                                   float smooth = 0.15f, float emission = 0f)
        {
            string key = k + "|" + c + "|" + (tex != null ? tex.name : "-") + "|" + tx + "|" + ty + "|" + smooth + "|" + emission;
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;

            m = Template(k);
            m.name = "JD_" + key.GetHashCode();
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (tex != null)
            {
                m.mainTexture = tex;
                m.mainTextureScale = new Vector2(tx, ty);
                if (m.HasProperty("_BaseMap"))
                {
                    m.SetTexture("_BaseMap", tex);
                    m.SetTextureScale("_BaseMap", new Vector2(tx, ty));
                }
            }
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            if (emission > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * emission);
            }
            cache[key] = m;
            return m;
        }

        public static Material Flat(Color c, float smooth = 0.15f)
        {
            return Get(MatKind.Opaque, c, null, 1f, 1f, smooth, 0f);
        }
    }

    public static class Gfx
    {
        static readonly Dictionary<PrimitiveType, Mesh> meshes = new Dictionary<PrimitiveType, Mesh>();

        public static Mesh MeshOf(PrimitiveType t)
        {
            Mesh m;
            if (meshes.TryGetValue(t, out m) && m != null) return m;
            GameObject g = GameObject.CreatePrimitive(t);
            m = g.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(g);
            meshes[t] = m;
            return m;
        }

        /// <summary>Collider-free primitive.</summary>
        public static GameObject Prim(PrimitiveType t, Transform parent, Vector3 pos, Vector3 scale, Material mat,
                                      Vector3 euler = default(Vector3), bool shadows = true, string name = "Part")
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.AddComponent<MeshFilter>().sharedMesh = MeshOf(t);
            MeshRenderer r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            if (!shadows)
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            return go;
        }

        public static float R(System.Random r, float a, float b)
        {
            return a + (float)r.NextDouble() * (b - a);
        }
    }
}
