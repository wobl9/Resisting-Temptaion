using System;
using ShatteredForge.Core;
using UnityEngine;

namespace ShatteredForge.UI
{
    [Serializable]
    public sealed class CampCharacterSheetBodySlotLayout
    {
        public EquipmentBodySlot slot = EquipmentBodySlot.None;
        public Vector2 anchoredPosition;
        [Tooltip("Relative slot position in paper-doll space (0..1): x left->right, y top->bottom.")]
        public Vector2 normalizedPosition = new(0.5f, 0.5f);
        [Tooltip("When enabled, slot anchoredPosition is derived from normalizedPosition.")]
        public bool useNormalizedPosition = true;
        public Vector2 sizeDelta = new(76f, 76f);
        public Sprite iconSprite;
    }

    [Serializable]
    public sealed class CampCharacterSheetCanvasLayout
    {
        public Vector2 referenceResolution = new(1920f, 1080f);
        [Range(0f, 1f)] public float widthHeightMatch = 0.45f;
        public Vector2 panelSize = new(980f, 760f);
        public Vector2 innerOffsetMin = new(10f, 48f);
        public Vector2 innerOffsetMax = new(-10f, -8f);
    }

    [Serializable]
    public sealed class CampCharacterSheetPaperDollLayout
    {
        public Vector2 anchoredPosition = new(12f, -96f);
        public Vector2 sizeDelta = new(940f, 360f);
        public Vector2 torsoAnchoredPosition = new(428f, -92f);
        [Tooltip("Relative torso position in paper-doll space (0..1): x left->right, y top->bottom.")]
        public Vector2 torsoNormalizedPosition = new(0.5f, 0.36f);
        [Tooltip("When enabled, torso anchoredPosition is derived from torsoNormalizedPosition.")]
        public bool useNormalizedTorsoPosition = true;
        public Vector2 torsoSize = new(84f, 112f);
        public CampCharacterSheetBodySlotLayout[] slots = Array.Empty<CampCharacterSheetBodySlotLayout>();
    }

    [Serializable]
    public sealed class CampCharacterSheetStashLayout
    {
        public Vector2 anchoredPosition = new(20f, -470f);
        public Vector2 sizeDelta = new(920f, 236f);
        public Vector2 cellSize = new(88f, 56f);
        public Vector2 spacing = new(4f, 4f);
        [Min(1)] public int columns = 10;
        [Min(1)] public int minRows = 4;
        [Min(20)] public int poolSize = 80;
        public Vector2 captionAnchoredPosition = new(20f, -444f);
        public Vector2 captionSize = new(780f, 22f);
    }

    [Serializable]
    public sealed class CampCharacterSheetChromeLayout
    {
        public Vector2 titlePos = new(8f, -4f);
        public Vector2 titleSize = new(220f, 28f);
        public int titleFont = 18;
        public Vector2 goldPos = new(170f, -4f);
        public Vector2 goldSize = new(240f, 28f);
        public int goldFont = 16;
        public Vector2 hintOffsetMin = new(8f, -72f);
        public Vector2 hintOffsetMax = new(-8f, -48f);
        public int hintFont = 10;
    }

    [Serializable]
    public sealed class CampCharacterSheetTooltipLayout
    {
        public Vector2 sizeDelta = new(420f, 220f);
        public int fontSize = 14;
        public float maxWidth = 420f;
        public float maxHeight = 280f;
    }

    [CreateAssetMenu(
        fileName = "CampCharacterSheetLayout",
        menuName = "Shattered Forge/UI/Camp Character Sheet Layout")]
    public sealed class CampCharacterSheetLayoutAsset : ScriptableObject
    {
        public CampCharacterSheetCanvasLayout canvas = new();
        public CampCharacterSheetChromeLayout chrome = new();
        public CampCharacterSheetPaperDollLayout paperDoll = new();
        public CampCharacterSheetStashLayout stash = new();
        public CampCharacterSheetTooltipLayout tooltip = new();

        private void OnValidate()
        {
            EnsureSlotList();
            stash.columns = Mathf.Max(1, stash.columns);
            stash.minRows = Mathf.Max(1, stash.minRows);
            stash.poolSize = Mathf.Max(20, stash.poolSize);
        }

        public void EnsureSlotList()
        {
            var needed = CampCharacterSheetMetadata.DollSlotUiOrder.Length;
            if (paperDoll.slots == null || paperDoll.slots.Length != needed)
            {
                var next = new CampCharacterSheetBodySlotLayout[needed];
                for (var i = 0; i < needed; i++)
                {
                    var old = paperDoll.slots != null && i < paperDoll.slots.Length ? paperDoll.slots[i] : null;
                    next[i] = old ?? new CampCharacterSheetBodySlotLayout();
                    next[i].slot = CampCharacterSheetMetadata.DollSlotUiOrder[i].slot;
                    if (next[i].anchoredPosition.sqrMagnitude < 0.001f)
                    {
                        ApplyDiabloLikeSlotDefaults(next[i], i);
                    }

                    next[i].normalizedPosition = ComputeNormalized(
                        next[i].anchoredPosition,
                        next[i].sizeDelta,
                        paperDoll.sizeDelta);
                    next[i].useNormalizedPosition = true;

                    if (next[i].sizeDelta.sqrMagnitude < 1f)
                    {
                        next[i].sizeDelta = new Vector2(76f, 76f);
                    }
                }

                paperDoll.slots = next;
            }

            paperDoll.torsoNormalizedPosition = ComputeNormalized(
                paperDoll.torsoAnchoredPosition,
                paperDoll.torsoSize,
                paperDoll.sizeDelta);
        }

        public void ApplyDiabloLikePreset()
        {
            canvas = new CampCharacterSheetCanvasLayout();
            chrome = new CampCharacterSheetChromeLayout();
            paperDoll = new CampCharacterSheetPaperDollLayout();
            stash = new CampCharacterSheetStashLayout();
            tooltip = new CampCharacterSheetTooltipLayout();
            EnsureSlotList();
        }

        private static void ApplyDiabloLikeSlotDefaults(CampCharacterSheetBodySlotLayout slot, int index)
        {
            // Layout order: Head, Amulet, Chest, MainHand, OffHand, Gloves, Ring, Ring2, Boots
            switch (index)
            {
                case 0: // head
                    slot.anchoredPosition = new Vector2(430f, -14f);
                    slot.sizeDelta = new Vector2(78f, 66f);
                    break;
                case 1: // amulet
                    slot.anchoredPosition = new Vector2(536f, -28f);
                    slot.sizeDelta = new Vector2(52f, 52f);
                    break;
                case 2: // chest
                    slot.anchoredPosition = new Vector2(414f, -96f);
                    slot.sizeDelta = new Vector2(108f, 132f);
                    break;
                case 3: // mainhand
                    slot.anchoredPosition = new Vector2(122f, -80f);
                    slot.sizeDelta = new Vector2(92f, 172f);
                    break;
                case 4: // offhand
                    slot.anchoredPosition = new Vector2(712f, -80f);
                    slot.sizeDelta = new Vector2(92f, 172f);
                    break;
                case 5: // gloves
                    slot.anchoredPosition = new Vector2(210f, -270f);
                    slot.sizeDelta = new Vector2(76f, 76f);
                    break;
                case 6: // ring left
                    slot.anchoredPosition = new Vector2(328f, -268f);
                    slot.sizeDelta = new Vector2(54f, 54f);
                    break;
                case 7: // ring right
                    slot.anchoredPosition = new Vector2(560f, -268f);
                    slot.sizeDelta = new Vector2(54f, 54f);
                    break;
                case 8: // boots
                    slot.anchoredPosition = new Vector2(678f, -270f);
                    slot.sizeDelta = new Vector2(76f, 76f);
                    break;
            }

            slot.useNormalizedPosition = true;
        }

        private static Vector2 ComputeNormalized(Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 parentSize)
        {
            var maxX = Mathf.Max(1f, parentSize.x - sizeDelta.x);
            var maxY = Mathf.Max(1f, parentSize.y - sizeDelta.y);
            var nx = Mathf.Clamp01(anchoredPosition.x / maxX);
            var ny = Mathf.Clamp01((-anchoredPosition.y) / maxY);
            return new Vector2(nx, ny);
        }
    }
}
