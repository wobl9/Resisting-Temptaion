using System;
using ShatteredForge.Core;
using ShatteredForge.Input;
using ShatteredForge.Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShatteredForge.UI
{
    /// <summary>
    /// Camp: character stats (stub) + paper-doll slots + stash. Click stash row to select, click slot to equip / unequip.
    /// </summary>
    public sealed class CampCharacterSheetPanel : MonoBehaviour
    {
        private static readonly (EquipmentBodySlot slot, string label)[] SlotUi =
        {
            (EquipmentBodySlot.Head, "Шлем"),
            (EquipmentBodySlot.Chest, "Броня (грудь)"),
            (EquipmentBodySlot.MainHand, "Правая рука"),
            (EquipmentBodySlot.OffHand, "Левая рука"),
            (EquipmentBodySlot.Gloves, "Перчатки"),
            (EquipmentBodySlot.Boots, "Ботинки"),
            (EquipmentBodySlot.Ring, "Кольцо"),
            (EquipmentBodySlot.Amulet, "Амулет")
        };

        private AccountState _account;
        private Action _onMutated;
        private Action _refreshLookCapture;

        private bool _open;
        private int _selectedStashIndex = -1;

        public bool IsOpen => _open;

        public void Bind(AccountState account, Action onMutated, Action refreshLookCapture)
        {
            _account = account;
            _onMutated = onMutated;
            _refreshLookCapture = refreshLookCapture;
        }

        public void SetOpen(bool open)
        {
            if (_open == open)
            {
                return;
            }

            _open = open;
            if (!open)
            {
                _selectedStashIndex = -1;
            }

            _refreshLookCapture?.Invoke();
        }

        public void Toggle()
        {
            SetOpen(!_open);
        }

        private void Update()
        {
            if (_open && DemoInput.KeyDown(Key.Escape))
            {
                SetOpen(false);
            }
        }

        private void OnGUI()
        {
            if (!_open || _account == null)
            {
                return;
            }

            CharacterPaperDoll.EnsureList(_account);
            CharacterStatsService.RecalculateForCamp(_account);

            const float w = 720f;
            const float h = 480f;
            var r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.Box(r, GUIContent.none);

            GUILayout.BeginArea(new Rect(r.x + 12f, r.y + 10f, w - 24f, h - 20f));
            GUILayout.Label("Персонаж", new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold });
            GUILayout.Label($"Золото: {_account.gold}", new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold });
            GUILayout.Space(4f);
            var primary = _account.primaryStats ?? CharacterPrimaryStats.CreateDefault();
            var computed = _account.computedStats ?? new ComputedCharacterStats();
            var resists = computed.elementalResists ?? new ElementalResistanceProfile();
            GUILayout.Label(
                $"База: СИЛ {primary.strength}  ЛОВ {primary.agility}  ВЫН {primary.vitality}  ИНТ {primary.intellect}",
                GUI.skin.label);
            GUILayout.Label(
                $"Бой: Урон {computed.damage}  Броня {computed.armor}  Резисты [Огонь {resists.fire}, Холод {resists.cold}, Молния {resists.lightning}]",
                GUI.skin.label);
            GUILayout.Space(10f);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(220f));
            GUILayout.Label("Слоты", GUI.skin.label);
            foreach (var (slot, label) in SlotUi)
            {
                DrawSlotRow(slot, label);
            }

            GUILayout.EndVertical();

            GUILayout.Space(16f);
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUILayout.Label("Снабжение (клик — выбрать, слот — надеть / снять)", GUI.skin.label);
            DrawStash();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Закрыть", GUILayout.Height(28f)))
            {
                SetOpen(false);
            }

            GUILayout.EndArea();
        }

        private void DrawSlotRow(EquipmentBodySlot slot, string label)
        {
            var equipped = CharacterPaperDoll.GetEquipped(_account, slot);
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(120f));
            var text = equipped != null ? FormatItemShort(equipped) : "— пусто —";
            if (GUILayout.Button(text, GUILayout.MinWidth(160f)))
            {
                if (equipped != null)
                {
                    if (CharacterPaperDoll.TryUnequipSlotToStash(_account, slot))
                    {
                        _selectedStashIndex = -1;
                        _onMutated?.Invoke();
                    }
                }
                else if (_selectedStashIndex >= 0 &&
                         CharacterPaperDoll.TryEquipFromStashToSlot(_account, _selectedStashIndex, slot))
                {
                    _selectedStashIndex = -1;
                    _onMutated?.Invoke();
                }
            }

            GUILayout.EndHorizontal();
        }

        private void DrawStash()
        {
            if (_account.stash == null || _account.stash.Count == 0)
            {
                GUILayout.Label("(снабжение пусто)");
                return;
            }

            for (var i = 0; i < _account.stash.Count; i++)
            {
                var it = _account.stash[i];
                var sel = i == _selectedStashIndex;
                GUI.backgroundColor = sel ? new Color(0.55f, 0.7f, 1f) : Color.white;
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(sel ? "► " : "   ", GUILayout.Width(28f)))
                {
                    _selectedStashIndex = sel ? -1 : i;
                }

                GUILayout.Label(FormatItemShort(it), GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();
                GUI.backgroundColor = Color.white;
            }
        }

        private static string FormatItemShort(ItemInstance it)
        {
            if (it == null)
            {
                return "(null)";
            }

            var cat = ItemCatalogRuntime.Current;
            var price = cat != null ? cat.GetBuyGoldPrice(it.templateId) : 0;
            if (cat != null && cat.TryGet(it.templateId, out var entry) && !string.IsNullOrEmpty(entry.displayNameRu))
            {
                var line = $"{entry.displayNameRu}  +{it.enhanceLevel}  {it.rarity}";
                return price > 0 ? $"{line}  ·  {price} зол." : line;
            }

            var fallback = $"{it.templateId}  +{it.enhanceLevel}  {it.rarity}";
            return price > 0 ? $"{fallback}  ·  {price} зол." : fallback;
        }
    }
}
