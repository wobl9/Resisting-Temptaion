using UnityEngine;

namespace ShatteredForge.UI
{
    [CreateAssetMenu(
        fileName = "CampCharacterSheetSkin",
        menuName = "Shattered Forge/UI/Camp Character Sheet Skin")]
    public sealed class CampCharacterSheetSkinAsset : ScriptableObject
    {
        [Header("Main surfaces")]
        public Sprite panelSprite;
        public Color panelTint = Color.white;
        public Sprite tooltipSprite;
        public Color tooltipTint = Color.white;
        public Sprite torsoSprite;
        public Color torsoTint = Color.white;

        [Header("Cells")]
        public Sprite slotSprite;
        public Color slotTint = Color.white;
        public Sprite stashCellSprite;
        public Color stashCellTint = Color.white;
    }
}
