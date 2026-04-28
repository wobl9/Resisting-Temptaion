using UnityEngine;

namespace ShatteredForge.UI
{
    /// <summary>
    /// Paper-doll slot positions/sizes (Diablo II–like, relative to doll block origin).
    /// Order: Head, Amulet, Chest, MainHand, OffHand, Gloves, Ring, Ring2, Boots.
    /// </summary>
    public static class PaperDollDiablo2Layout
    {
        public const int SlotCount = 9;

        public const float ContentWidth = 244f;

        /// <summary>Top-left of each slot in doll-local space (y grows downward).</summary>
        public static readonly Vector2[] SlotAnchors =
        {
            new Vector2(91f, 6f),
            new Vector2(153f, 22f),
            new Vector2(83f, 88f),
            new Vector2(4f, 88f),
            new Vector2(186f, 88f),
            new Vector2(6f, 228f),
            new Vector2(52f, 204f),
            new Vector2(174f, 188f),
            new Vector2(180f, 228f)
        };

        public static readonly Vector2[] SlotSizes =
        {
            new Vector2(58f, 76f),
            new Vector2(30f, 30f),
            new Vector2(74f, 100f),
            new Vector2(50f, 122f),
            new Vector2(50f, 122f),
            new Vector2(54f, 54f),
            new Vector2(36f, 36f),
            new Vector2(36f, 36f),
            new Vector2(54f, 54f)
        };

        /// <summary>Torso silhouette under head / between weapon columns (doll-local).</summary>
        public static Rect TorsoRect(Vector2 origin)
        {
            return new Rect(origin.x + 74f, origin.y + 92f, 96f, 112f);
        }
    }
}
