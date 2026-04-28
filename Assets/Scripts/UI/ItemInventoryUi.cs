using System.Collections.Generic;
using System.Text;
using ShatteredForge.Core;
using ShatteredForge.Items;
using UnityEngine;

namespace ShatteredForge.UI
{
    /// <summary>
    /// Shared Diablo-style inventory rendering: 2-letter glyphs, tooltips, run loadout → body slots (visual).
    /// </summary>
    public static class ItemInventoryUi
    {
        public const float DefaultCellSize = 36f;
        public const float CellGap = 2f;
        public const int DefaultStashColumns = 10;

        public static string GetDisplayName(ItemInstance it)
        {
            if (it == null)
            {
                return "?";
            }

            var cat = ItemCatalogRuntime.Current;
            if (cat != null && cat.TryGet(it.templateId, out var entry) && !string.IsNullOrEmpty(entry.displayNameRu))
            {
                return entry.displayNameRu;
            }

            return it.templateId;
        }

        /// <summary>Two-letter stub icon (first significant letters of display name or template id).</summary>
        public static string GetGlyph(ItemInstance it)
        {
            if (it == null)
            {
                return "?";
            }

            var src = GetDisplayName(it);
            var chars = new List<char>(2);
            foreach (var c in src)
            {
                if (char.IsWhiteSpace(c))
                {
                    continue;
                }

                chars.Add(char.ToUpperInvariant(c));
                if (chars.Count >= 2)
                {
                    break;
                }
            }

            if (chars.Count == 0)
            {
                return "?";
            }

            return chars.Count == 1 ? chars[0].ToString() : $"{chars[0]}{chars[1]}";
        }

        public static string BuildTooltip(ItemInstance it, bool includeRunInsurance = false)
        {
            if (it == null)
            {
                return "Пусто";
            }

            var sb = new StringBuilder(256);
            sb.AppendLine(GetDisplayName(it));
            sb.AppendLine($"Шаблон: {it.templateId}");
            sb.AppendLine($"Редкость: {it.rarity}  Усиление: +{it.enhanceLevel}");
            var cat = ItemCatalogRuntime.Current;
            if (cat != null)
            {
                var p = cat.GetBuyGoldPrice(it.templateId);
                if (p > 0)
                {
                    sb.AppendLine($"Цена: {p} зол.");
                }
            }

            if (it.affixes != null && it.affixes.Count > 0)
            {
                sb.AppendLine("Аффиксы:");
                foreach (var a in it.affixes)
                {
                    if (!string.IsNullOrEmpty(a))
                    {
                        sb.AppendLine("  • " + a);
                    }
                }
            }

            if (it.sockets != null && it.sockets.Count > 0)
            {
                sb.AppendLine("Сокеты: " + string.Join(", ", it.sockets));
            }

            if (includeRunInsurance && it.isInsuredForRun)
            {
                sb.AppendLine("Страховка на этот выход.");
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Flat <see cref="RunState.equippedLoadout"/> mapped to body slots for UI (first weapon main, second off; first armor chest).
        /// </summary>
        public static void BuildRunEquippedSlotMap(
            RunState run,
            Dictionary<EquipmentBodySlot, (int listIndex, ItemInstance item)> intoMap)
        {
            intoMap.Clear();
            if (run?.equippedLoadout == null)
            {
                return;
            }

            var weapons = new List<(int i, ItemInstance it)>();
            var armorIdx = -1;
            ItemInstance armorItem = null;

            for (var i = 0; i < run.equippedLoadout.Count; i++)
            {
                var it = run.equippedLoadout[i];
                if (it == null || string.IsNullOrEmpty(it.templateId))
                {
                    continue;
                }

                var k = InventoryEquipmentRules.Classify(it.templateId);
                if (k == ItemEquipmentKind.Weapon)
                {
                    weapons.Add((i, it));
                }
                else if (k == ItemEquipmentKind.Armor && armorIdx < 0)
                {
                    armorIdx = i;
                    armorItem = it;
                }
            }

            if (weapons.Count > 0)
            {
                intoMap[EquipmentBodySlot.MainHand] = (weapons[0].i, weapons[0].it);
            }

            if (weapons.Count > 1)
            {
                intoMap[EquipmentBodySlot.OffHand] = (weapons[1].i, weapons[1].it);
            }

            if (armorIdx >= 0 && armorItem != null)
            {
                intoMap[EquipmentBodySlot.Chest] = (armorIdx, armorItem);
            }
        }

        public static GUIStyle CreateIconCellStyle()
        {
            return new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };
        }

        public static GUIStyle CreateEmptyCellStyle()
        {
            return new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter
            };
        }

        private static GUIStyle _paperDollSlotStyle;
        private static Texture2D _texSlotNormal;
        private static Texture2D _texSlotHover;
        private static Texture2D _texSlotActive;

        private static Texture2D SolidColorTexture(Color c)
        {
            var t = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp
            };
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        /// <summary>
        /// Flat slot buttons for the paper doll (avoids default IMGUI bevel looking like a bright square on the torso).
        /// </summary>
        public static GUIStyle CreatePaperDollSlotButtonStyle()
        {
            if (_paperDollSlotStyle != null)
            {
                return _paperDollSlotStyle;
            }

            _texSlotNormal = SolidColorTexture(new Color(0.19f, 0.2f, 0.24f, 0.94f));
            _texSlotHover = SolidColorTexture(new Color(0.28f, 0.3f, 0.36f, 0.96f));
            _texSlotActive = SolidColorTexture(new Color(0.34f, 0.36f, 0.42f, 0.96f));

            _paperDollSlotStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                border = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(1, 1, 1, 1),
                padding = new RectOffset(0, 0, 0, 0)
            };
            _paperDollSlotStyle.normal.background = _texSlotNormal;
            _paperDollSlotStyle.hover.background = _texSlotHover;
            _paperDollSlotStyle.active.background = _texSlotActive;
            _paperDollSlotStyle.focused.background = _texSlotNormal;
            _paperDollSlotStyle.onNormal.background = _texSlotNormal;
            _paperDollSlotStyle.onHover.background = _texSlotHover;
            _paperDollSlotStyle.onActive.background = _texSlotActive;
            var tc = new Color(0.94f, 0.94f, 0.9f);
            _paperDollSlotStyle.normal.textColor = tc;
            _paperDollSlotStyle.hover.textColor = tc;
            _paperDollSlotStyle.active.textColor = tc;
            _paperDollSlotStyle.focused.textColor = tc;
            return _paperDollSlotStyle;
        }

        private static GUIStyle _tooltipBoxStyle;

        private static GUIStyle TooltipBoxStyle()
        {
            if (_tooltipBoxStyle != null)
            {
                return _tooltipBoxStyle;
            }

            _tooltipBoxStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 12,
                wordWrap = true,
                padding = new RectOffset(10, 10, 8, 8)
            };
            _tooltipBoxStyle.normal.textColor = new Color(0.95f, 0.95f, 0.92f);
            return _tooltipBoxStyle;
        }

        /// <summary>
        /// Floating tooltip in root IMGUI coordinates (call after <c>GUI.EndGroup</c> for the panel).
        /// </summary>
        public static void DrawFloatingTooltip(Vector2 anchorParentGui, string text)
        {
            if (string.IsNullOrEmpty(text) || Event.current.type != EventType.Repaint)
            {
                return;
            }

            const float maxW = 320f;
            var style = TooltipBoxStyle();
            var h = style.CalcHeight(new GUIContent(text), maxW);
            if (h < 26f)
            {
                h = 26f;
            }

            h = Mathf.Min(260f, h + 6f);
            var w = maxW;
            var pos = anchorParentGui + new Vector2(14f, 20f);
            if (pos.x + w > Screen.width - 4f)
            {
                pos.x = Screen.width - w - 4f;
            }

            if (pos.y + h > Screen.height - 4f)
            {
                pos.y = anchorParentGui.y - h - 10f;
                if (pos.y < 4f)
                {
                    pos.y = 4f;
                }
            }

            var prevDepth = GUI.depth;
            var prevBg = GUI.backgroundColor;
            GUI.depth = -4000;
            GUI.backgroundColor = new Color(0.11f, 0.11f, 0.13f, 0.97f);
            GUI.Box(new Rect(pos.x, pos.y, w, h), text, style);
            GUI.backgroundColor = prevBg;
            GUI.depth = prevDepth;
        }
    }
}
