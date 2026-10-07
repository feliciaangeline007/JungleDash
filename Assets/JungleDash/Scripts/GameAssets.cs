// GameAssets.cs - Holds references to Standard Assets content. Filled in by the editor setup tool
// (Jungle Dash > Setup Project) so the references survive into builds via Resources.
using UnityEngine;

namespace JungleDash
{
    public class GameAssets : ScriptableObject
    {
        public GameObject ethan;
        public RuntimeAnimatorController animator;
        public Texture2D mud, grass, cliff, palm, broadleaf;
        public Texture2D stonePath, carvedStone, treeBark, jungleGrass, goldRelic;

        public static GameAssets Load() { return Resources.Load<GameAssets>("GameAssets"); }
    }
}
