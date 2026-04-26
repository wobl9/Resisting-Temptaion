using UnityEngine;

namespace ShatteredForge.SceneFlow
{
    /// <summary>
    /// Runtime URP/Lit (or fallback) materials for camp prototype geometry — small shared palette.
    /// </summary>
    internal static class CampHubProtoMaterials
    {
        private static Material Lit(Color baseColor, float smoothness = 0.45f, float metallic = 0f)
        {
            var shader =
                Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                ?? Shader.Find("Sprites/Default");

            var m = new Material(shader)
            {
                name = $"CampProto_{ColorUtility.ToHtmlStringRGBA(baseColor)}"
            };

            if (m.HasProperty("_BaseColor"))
            {
                m.SetColor("_BaseColor", baseColor);
            }
            else
            {
                m.color = baseColor;
            }

            if (m.HasProperty("_Smoothness"))
            {
                m.SetFloat("_Smoothness", smoothness);
            }

            if (m.HasProperty("_Metallic"))
            {
                m.SetFloat("_Metallic", metallic);
            }

            return m;
        }

        private static Material _stoneWarm;
        private static Material _stoneDark;
        private static Material _wood;
        private static Material _clothTeal;
        private static Material _metalDark;
        private static Material _metalRust;
        private static Material _ember;
        private static Material _glassGreen;
        private static Material _glassPurple;
        private static Material _voidDark;
        private static Material _trimGold;
        private static Material _skin;
        private static Material _clothHero;
        private static Material _accentFront;

        public static Material StoneWarm => _stoneWarm ??= Lit(new Color(0.62f, 0.52f, 0.42f), 0.35f, 0f);
        public static Material StoneDark => _stoneDark ??= Lit(new Color(0.28f, 0.26f, 0.3f), 0.42f, 0f);
        public static Material Wood => _wood ??= Lit(new Color(0.45f, 0.28f, 0.14f), 0.38f, 0f);
        public static Material ClothTeal => _clothTeal ??= Lit(new Color(0.2f, 0.55f, 0.48f), 0.55f, 0f);
        public static Material MetalDark => _metalDark ??= Lit(new Color(0.22f, 0.22f, 0.24f), 0.55f, 0.85f);
        public static Material MetalRust => _metalRust ??= Lit(new Color(0.38f, 0.22f, 0.14f), 0.4f, 0.6f);
        public static Material Ember => _ember ??= Lit(new Color(1f, 0.35f, 0.08f), 0.65f, 0f);
        public static Material GlassGreen => _glassGreen ??= Lit(new Color(0.25f, 0.75f, 0.45f), 0.75f, 0.1f);
        public static Material GlassPurple => _glassPurple ??= Lit(new Color(0.55f, 0.3f, 0.85f), 0.72f, 0.05f);
        public static Material VoidDark => _voidDark ??= Lit(new Color(0.02f, 0.02f, 0.04f), 0.9f, 0f);
        public static Material TrimGold => _trimGold ??= Lit(new Color(0.85f, 0.7f, 0.25f), 0.55f, 0.7f);
        public static Material Skin => _skin ??= Lit(new Color(0.92f, 0.72f, 0.58f), 0.48f, 0f);
        public static Material ClothHero => _clothHero ??= Lit(new Color(0.28f, 0.38f, 0.62f), 0.52f, 0f);
        public static Material AccentFront => _accentFront ??= Lit(new Color(0.95f, 0.45f, 0.2f), 0.55f, 0f);
    }
}
