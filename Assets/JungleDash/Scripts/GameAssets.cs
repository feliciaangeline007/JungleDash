// GameAssets.cs
// Holds runtime-generated texture references.
// All textures are filled procedurally by ProceduralAssets.FillAssets() at startup.
// No external files or Standard Assets required.
using UnityEngine;

namespace JungleDash
{
    public class GameAssets : ScriptableObject
    {
        // Procedurally generated textures (filled at runtime)
        public Texture2D stonePath;
        public Texture2D carvedStone;
        public Texture2D treeBark;
        public Texture2D jungleGrass;
        public Texture2D goldRelic;
        public Texture2D mud;

        // Aliases used by World.cs
        public Texture2D grass  { get { return jungleGrass; } set { jungleGrass = value; } }
        public Texture2D cliff  { get { return carvedStone; } set { carvedStone = value; } }

        // These are kept null — World.cs handles null gracefully (skips billboards)
        public Texture2D palm      = null;
        public Texture2D broadleaf = null;

        // No Ethan FBX, no animator — player built from primitives in Player.cs
        public GameObject ethan                    = null;
        public RuntimeAnimatorController animator  = null;

        public static GameAssets Load()
        {
            return Resources.Load<GameAssets>("GameAssets");
        }
    }
}
