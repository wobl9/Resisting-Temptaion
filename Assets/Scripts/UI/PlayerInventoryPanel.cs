using System;
using ShatteredForge.Core;
using ShatteredForge.Input;
using ShatteredForge.Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShatteredForge.UI
{
    /// <summary>
    /// IMGUI inventory: camp = stash only; gameplay run = stash + equipped + carried loot.
    /// </summary>
    public sealed class PlayerInventoryPanel : MonoBehaviour
    {
        [SerializeField] private Key toggleKey = Key.Tab;

        private AccountState _account;
        private Func<RunState> _getRun;
        private Action _onMutated;
        private Action<bool> _setLookSuppressed;
        private bool _campMode;

        private bool _open;

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

        private void OnGUI()
        {
            if (!_open || _account == null)
            {
                return;
            }

            CharacterPaperDoll.EnsureList(_account);

            var run = _getRun?.Invoke();
            const float w = 520f;
            const float h = 420f;
            var r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.Box(r, GUIContent.none);

            GUILayout.BeginArea(new Rect(r.x + 14f, r.y + 10f, w - 28f, h - 20f));
            GUILayout.Label(_campMode ? "Снабжение (лагерь)" : "Инвентарь", new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold });
            GUILayout.Label($"Золото: {_account.gold}", new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold });
            GUILayout.Space(4f);
            if (_campMode)
            {
                GUILayout.Label("В экспедиции здесь: Tab — экипировка и добыча.", GUI.skin.label);
            }
            else
            {
                GUILayout.Label($"{toggleKey} — открыть/закрыть | Esc — закрыть", GUI.skin.label);
            }

            GUILayout.Space(8f);

            GUILayout.Label("Снабжение", GUI.skin.label);
            DrawStashList(run);
            GUILayout.Space(10f);

            if (!_campMode && run != null)
            {
                GUILayout.Label("Экипировка", GUI.skin.label);
                DrawEquippedList(run);
                GUILayout.Space(10f);
                GUILayout.Label($"С собой (добыча): {run.carryLoot?.Count ?? 0}", GUI.skin.label);
                DrawCarryList(run);
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Закрыть", GUILayout.Height(28f)))
            {
                SetOpen(false);
            }

            GUILayout.EndArea();
        }

        private void DrawStashList(RunState run)
        {
            if (_account.stash == null || _account.stash.Count == 0)
            {
                GUILayout.Label("  (пусто)");
                return;
            }

            for (var i = 0; i < _account.stash.Count; i++)
            {
                var it = _account.stash[i];
                GUILayout.BeginHorizontal();
                GUILayout.Label(FormatItem(it), GUILayout.Width(320f));
                if (!_campMode && run != null)
                {
                    var kind = InventoryEquipmentRules.Classify(it.templateId);
                    if (kind != ItemEquipmentKind.None && GUILayout.Button("Надеть", GUILayout.Width(72f)))
                    {
                        if (InventoryEquipmentRules.TryEquipFromStashIndex(_account, run, i))
                        {
                            _onMutated?.Invoke();
                        }
                    }
                }

                GUILayout.EndHorizontal();
            }
        }

        private void DrawEquippedList(RunState run)
        {
            if (run.equippedLoadout == null || run.equippedLoadout.Count == 0)
            {
                GUILayout.Label("  (пусто)");
                return;
            }

            for (var i = 0; i < run.equippedLoadout.Count; i++)
            {
                var it = run.equippedLoadout[i];
                GUILayout.BeginHorizontal();
                var ins = it.isInsuredForRun ? " [страховка]" : string.Empty;
                GUILayout.Label(FormatItem(it) + ins, GUILayout.Width(360f));
                if (GUILayout.Button("Снять", GUILayout.Width(72f)))
                {
                    if (InventoryEquipmentRules.TryUnequipIndex(_account, run, i))
                    {
                        _onMutated?.Invoke();
                    }
                }

                GUILayout.EndHorizontal();
            }
        }

        private static void DrawCarryList(RunState run)
        {
            if (run.carryLoot == null || run.carryLoot.Count == 0)
            {
                return;
            }

            foreach (var it in run.carryLoot)
            {
                GUILayout.Label("  • " + FormatItem(it));
            }
        }

        private static string FormatItem(ItemInstance it)
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
