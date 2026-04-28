using System;
using System.Collections.Generic;
using ShatteredForge.Core;
using ShatteredForge.Input;
using ShatteredForge.Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShatteredForge.UI
{
    /// <summary>
    /// Diablo-style IMGUI inventory: grid stash, paper-doll with glyph cells and hover tooltips; camp = stash only.
    /// </summary>
    public sealed class PlayerInventoryPanel : MonoBehaviour
    {
        private static readonly (EquipmentBodySlot slot, string label)[] RunSlotUi =
        {
            (EquipmentBodySlot.Head, "Шлем"),
            (EquipmentBodySlot.Amulet, "Амулет"),
            (EquipmentBodySlot.Chest, "Броня"),
            (EquipmentBodySlot.MainHand, "Слева"),
            (EquipmentBodySlot.OffHand, "Справа"),
            (EquipmentBodySlot.Gloves, "Перчатки"),
            (EquipmentBodySlot.Ring, "Кольцо слева"),
            (EquipmentBodySlot.Ring2, "Кольцо справа"),
            (EquipmentBodySlot.Boots, "Ботинки")
        };

        [SerializeField] private Key toggleKey = Key.Tab;

        private AccountState _account;
        private Func<RunState> _getRun;
        private Action _onMutated;
        private Action<bool> _setLookSuppressed;
        private bool _campMode;

        private bool _open;
        private int _selectedStashIndex = -1;

        private readonly Dictionary<EquipmentBodySlot, (int listIndex, ItemInstance item)> _runSlotMap =
            new Dictionary<EquipmentBodySlot, (int, ItemInstance)>();

        private GUIStyle _iconStyle;
        private GUIStyle _paperDollSlotStyle;
        private GUIStyle _emptyStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _hintStyle;

        private string _hoverTip;
        private Vector2 _hoverTipAnchor;

        public bool IsOpen => _open;

        public void BindCamp(AccountState account, Action onMutated, Action<bool> setLookSuppressedFromUi)
        {
            _account = account;
            _getRun = () => null;
            _onMutated = onMutated;
            _setLookSuppressed = setLookSuppressedFromUi;
            _campMode = true;
        }

        public void BindGameplay(AccountState account, Func<RunState> getRun, Action onMutated)
        {
            _account = account;
            _getRun = getRun ?? (() => null);
            _onMutated = onMutated;
            _setLookSuppressed = null;
            _campMode = false;
        }

        public void SetOpen(bool open)
        {
            if (_open == open)
            {
                return;
            }

            _open = open;
            _setLookSuppressed?.Invoke(_open);
            if (!open)
            {
                _selectedStashIndex = -1;
            }
        }

        private void Update()
        {
            if (DemoInput.KeyDown(toggleKey))
            {
                SetOpen(!_open);
            }

            if (_open && DemoInput.KeyDown(Key.Escape))
            {
                SetOpen(false);
            }
        }

        private void EnsureStyles()
        {
            if (_iconStyle == null)
            {
                _iconStyle = ItemInventoryUi.CreateIconCellStyle();
            }

            if (_paperDollSlotStyle == null)
            {
                _paperDollSlotStyle = ItemInventoryUi.CreatePaperDollSlotButtonStyle();
            }

            if (_emptyStyle == null)
            {
                _emptyStyle = ItemInventoryUi.CreateEmptyCellStyle();
            }

            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold
                };
            }

            if (_hintStyle == null)
            {
                _hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true };
            }
        }

        private void OnGUI()
        {
            if (!_open || _account == null)
            {
                return;
            }

            EnsureStyles();
            CharacterPaperDoll.EnsureList(_account);
            var run = _getRun?.Invoke();

            if (_campMode)
            {
                DrawCampInventoryPanel();
            }
            else
            {
                DrawGameplayInventoryPanel(run);
            }
        }

        private void RegisterHover(Rect localRect, string tip, Vector2 groupTopLeftParentGui)
        {
            if (string.IsNullOrEmpty(tip) || Event.current.type != EventType.Repaint)
            {
                return;
            }

            if (!localRect.Contains(Event.current.mousePosition))
            {
                return;
            }

            _hoverTip = tip;
            _hoverTipAnchor = groupTopLeftParentGui + Event.current.mousePosition;
        }

        private void DrawCampInventoryPanel()
        {
            _hoverTip = null;

            const float pw = 520f;
            const float ph = 420f;
            var panel = new Rect((Screen.width - pw) * 0.5f, (Screen.height - ph) * 0.5f, pw, ph);
            GUI.Box(panel, "");
            var innerW = pw - 24f;
            var innerH = ph - 20f;
            var innerRect = new Rect(panel.x + 12f, panel.y + 10f, innerW, innerH);
            var groupOrigin = new Vector2(innerRect.x, innerRect.y);
            GUI.BeginGroup(innerRect);

            GUI.Label(new Rect(4f, 4f, 480f, 28f), "Снабжение (лагерь)", _headerStyle);
            GUI.Label(new Rect(4f, 32f, 480f, 22f), $"Золото: {_account.gold}", _headerStyle);
            GUI.Label(
                new Rect(4f, 58f, 480f, 40f),
                "В экспедиции: Tab — схема тела, экипировка и добыча. Ячейки: наведите мышь — описание.",
                _hintStyle);
            DrawStashGridBlock(new Vector2(8f, 108f), 480f, 300f, groupOrigin);

            if (GUI.Button(new Rect(innerW * 0.5f - 70f, innerH - 42f, 140f, 30f), "Закрыть"))
            {
                SetOpen(false);
            }

            GUI.EndGroup();

            if (!string.IsNullOrEmpty(_hoverTip))
            {
                ItemInventoryUi.DrawFloatingTooltip(_hoverTipAnchor, _hoverTip);
            }
        }

        private void DrawGameplayInventoryPanel(RunState run)
        {
            _hoverTip = null;

            const float pw = 920f;
            const float ph = 560f;
            var panel = new Rect((Screen.width - pw) * 0.5f, (Screen.height - ph) * 0.5f, pw, ph);
            GUI.Box(panel, "");
            var innerW = pw - 20f;
            var innerH = ph - 16f;
            var innerRect = new Rect(panel.x + 10f, panel.y + 8f, innerW, innerH);
            var groupOrigin = new Vector2(innerRect.x, innerRect.y);
            GUI.BeginGroup(innerRect);

            GUI.Label(new Rect(6f, 4f, 500f, 28f), "Инвентарь", _headerStyle);
            GUI.Label(new Rect(520f, 4f, 360f, 24f), $"Золото: {_account.gold}", _headerStyle);
            GUI.Label(
                new Rect(6f, 34f, innerW - 12f, 36f),
                $"{toggleKey} — открыть/закрыть | Esc — закрыть | Снабжение: выберите ячейку, затем слот на схеме — надеть.",
                _hintStyle);

            _runSlotMap.Clear();
            if (run != null)
            {
                ItemInventoryUi.BuildRunEquippedSlotMap(run, _runSlotMap);
            }

            if (run != null)
            {
                DrawRunPaperDollBlock(new Vector2(8f, 88f), run, groupOrigin);
            }

            DrawStashGridBlock(new Vector2(288f, 88f), 600f, 360f, groupOrigin);

            if (run != null)
            {
                GUI.Label(new Rect(288f, 456f, 600f, 22f), $"Добыча (с собой): {run.carryLoot?.Count ?? 0}", _hintStyle);
                DrawCarryGrid(new Vector2(288f, 478f), run, groupOrigin);
            }

            if (GUI.Button(new Rect(innerW * 0.5f - 80f, innerH - 44f, 160f, 32f), "Закрыть"))
            {
                SetOpen(false);
            }

            GUI.EndGroup();

            if (!string.IsNullOrEmpty(_hoverTip))
            {
                ItemInventoryUi.DrawFloatingTooltip(_hoverTipAnchor, _hoverTip);
            }
        }

        private void DrawRunPaperDollBlock(Vector2 origin, RunState run, Vector2 groupOrigin)
        {
            var torso = PaperDollDiablo2Layout.TorsoRect(origin);
            var bodyColor = new Color(0.16f, 0.16f, 0.2f, 1f);
            GUI.DrawTexture(torso, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, bodyColor, 0f, 0f);

            GUI.Label(new Rect(origin.x + 40f, origin.y + 2f, 200f, 22f), "Схема тела", _hintStyle);

            for (var i = 0; i < RunSlotUi.Length && i < PaperDollDiablo2Layout.SlotCount; i++)
            {
                var (slot, label) = RunSlotUi[i];
                var p = origin + PaperDollDiablo2Layout.SlotAnchors[i];
                var sz = PaperDollDiablo2Layout.SlotSizes[i];
                var r = new Rect(p.x, p.y, sz.x, sz.y);
                GUI.Label(new Rect(r.x, r.y - 15f, Mathf.Max(72f, r.width), 14f), label, _hintStyle);

                if (_runSlotMap.TryGetValue(slot, out var entry))
                {
                    var it = entry.item;
                    var glyph = ItemInventoryUi.GetGlyph(it);
                    var tip = ItemInventoryUi.BuildTooltip(it, includeRunInsurance: true);
                    RegisterHover(r, tip, groupOrigin);
                    if (GUI.Button(r, glyph, _paperDollSlotStyle))
                    {
                        if (InventoryEquipmentRules.TryUnequipIndex(_account, run, entry.listIndex))
                        {
                            _selectedStashIndex = -1;
                            _onMutated?.Invoke();
                        }
                    }
                }
                else
                {
                    var blocked = slot != EquipmentBodySlot.MainHand &&
                                  slot != EquipmentBodySlot.OffHand &&
                                  slot != EquipmentBodySlot.Chest;
                    var emptyTip = blocked
                        ? $"{label}: пусто (в этой версии экипировка слота только в лагере)."
                        : $"{label}: пусто. Выберите вещь в снабжении и нажмите ячейку.";
                    RegisterHover(r, emptyTip, groupOrigin);
                    if (GUI.Button(r, "—", _paperDollSlotStyle))
                    {
                        if (!blocked &&
                            _selectedStashIndex >= 0 &&
                            InventoryEquipmentRules.TryEquipFromStashIndex(_account, run, _selectedStashIndex))
                        {
                            _selectedStashIndex = -1;
                            _onMutated?.Invoke();
                        }
                    }
                }
            }
        }

        private void DrawStashGridBlock(Vector2 origin, float maxWidth, float maxHeight, Vector2 groupOrigin)
        {
            var stash = _account.stash;
            const int cols = ItemInventoryUi.DefaultStashColumns;
            const float cell = ItemInventoryUi.DefaultCellSize;
            const float gap = ItemInventoryUi.CellGap;
            var cellStep = cell + gap;

            GUI.Label(new Rect(origin.x, origin.y - 22f, maxWidth, 20f), "Снабжение", _hintStyle);

            var count = stash != null ? stash.Count : 0;
            var rows = Mathf.Max(4, (count + cols - 1) / cols);
            var totalCells = rows * cols;
            var gridH = rows * cellStep;
            if (gridH > maxHeight)
            {
                rows = Mathf.Max(1, Mathf.FloorToInt(maxHeight / cellStep));
                totalCells = rows * cols;
            }

            for (var i = 0; i < totalCells; i++)
            {
                var col = i % cols;
                var row = i / cols;
                var r = new Rect(origin.x + col * cellStep, origin.y + row * cellStep, cell, cell);
                if (r.yMax > origin.y + maxHeight)
                {
                    break;
                }

                if (i < count)
                {
                    var it = stash[i];
                    var sel = i == _selectedStashIndex;
                    var prev = GUI.backgroundColor;
                    if (sel)
                    {
                        GUI.backgroundColor = new Color(0.45f, 0.62f, 0.95f, 1f);
                    }

                    var glyph = ItemInventoryUi.GetGlyph(it);
                    var tip = ItemInventoryUi.BuildTooltip(it);
                    RegisterHover(r, tip, groupOrigin);
                    if (GUI.Button(r, glyph, _iconStyle))
                    {
                        _selectedStashIndex = sel ? -1 : i;
                    }

                    GUI.backgroundColor = prev;
                }
                else
                {
                    var c = GUI.color;
                    GUI.color = new Color(0.22f, 0.22f, 0.26f, 1f);
                    GUI.Box(r, "", _emptyStyle);
                    GUI.color = c;
                }
            }
        }

        private void DrawCarryGrid(Vector2 origin, RunState run, Vector2 groupOrigin)
        {
            if (run.carryLoot == null || run.carryLoot.Count == 0)
            {
                return;
            }

            const float cell = ItemInventoryUi.DefaultCellSize;
            const float gap = ItemInventoryUi.CellGap;
            var step = cell + gap;
            const int cols = 10;

            for (var i = 0; i < run.carryLoot.Count; i++)
            {
                var it = run.carryLoot[i];
                var col = i % cols;
                var row = i / cols;
                var r = new Rect(origin.x + col * step, origin.y + row * step, cell, cell);
                var glyph = ItemInventoryUi.GetGlyph(it);
                var tip = ItemInventoryUi.BuildTooltip(it, includeRunInsurance: true);
                RegisterHover(r, tip, groupOrigin);
                var bg = GUI.color;
                GUI.color = new Color(0.24f, 0.24f, 0.3f, 1f);
                GUI.Box(r, "", _emptyStyle);
                GUI.color = bg;
                GUI.Label(r, glyph, _iconStyle);
            }
        }
    }
}
