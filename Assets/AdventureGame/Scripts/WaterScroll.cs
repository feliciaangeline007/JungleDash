using UnityEngine;

namespace AdventureGame
{
    public class WaterScroll : MonoBehaviour
    {
        public Vector2 scrollSpeed1 = new Vector2(0.04f, 0.02f);
        public Vector2 scrollSpeed2 = new Vector2(-0.02f, 0.03f);

        private Renderer m_Renderer;
        private Material m_Material;
        private Vector2 m_Offset1;
        private Vector2 m_Offset2;

        private void Start()
        {
            m_Renderer = GetComponent<Renderer>();
            if (m_Renderer != null)
            {
                m_Material = m_Renderer.material;
            }
        }

        private void Update()
        {
            if (m_Material == null) return;

            m_Offset1 += scrollSpeed1 * Time.deltaTime;
            m_Offset2 += scrollSpeed2 * Time.deltaTime;

            if (m_Material.HasProperty("_BaseMap"))
            {
                m_Material.SetTextureOffset("_BaseMap", m_Offset1);
            }
            if (m_Material.HasProperty("_BumpMap"))
            {
                m_Material.SetTextureOffset("_BumpMap", m_Offset2);
            }
        }
    }
}
