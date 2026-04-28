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
    /// Camp sheet: inventory page (B) and stats page (C).
    /// Layout is driven by <see cref="CampCharacterSheetView"/> (uGUI). Leave view unassigned for an auto-built default UI.
    /// </summary>
    [DefaultExecutionOrder(20)]
    public sealed class CampCharacterSheetPanel : MonoBehaviour
    {
        private const int PageInventory = 0;
        private const int PageStats = 1;

        /// <summary>Resources path used when <see cref="view"/> and <see cref="viewPrefab"/> are not set.</summary>
        public const string DefaultViewResourcesPath = "UI/CampCharacterSheetUi";

        [Tooltip("Если задано — используется вместо Resources. Иначе загрузка из Resources по пути DefaultViewResourcesPath, иначе пустой объект с автогенерацией в рантайме.")]
        [SerializeField] private CampCharacterSheetView viewPrefab;

        [SerializeField] private CampCharacterSheetView view;

        private AccountState _account;
        private Action _onMutated;
        private Action _refreshLookCapture;

        private bool _open;
        private int _selectedStashIndex = -1;
        private int _pageIndex;
        private bool _dragActive;
        private int _dragStashIndex = -1;
        private readonly Queue<string> _debugLines = new();

        public bool IsOpen => _open;

        private void Awake()
        {
            if (view == null)
            {
                view = GetComponentInChildren<CampCharacterSheetView>(true);
            }

            if (view == null && viewPrefab != null)
            {
                view = Instantiate(viewPrefab, transform);
                view.name = "CharacterSheetUi";
            }

            if (view == null)
            {
                var fromResources = Resources.Load<CampCharacterSheetView>(DefaultViewResourcesPath);
                if (fromResources != null)
                {
                    view = Instantiate(fromResources, transform);
                    view.name = "CharacterSheetUi";
                }
            }

            if (view == null)
            {
                var holder = new GameObject("CharacterSheetUi");
                holder.transform.SetParent(transform, false);
                view = holder.AddComponent<CampCharacterSheetView>();
            }

            view.EnsureBuilt();
        }

        private void Start()
        {
            if (view != null)
            {
                view.SetPresentationOpen(_open);
            }
        }

        public void Bind(AccountState account, Action onMutated, Action refreshLookCapture)
        {
            _account = account;
            _onMutated = onMutated;
            _refreshLookCapture = refreshLookCapture;
            view?.SetHandlers(OnDollSlotClicked, OnStashCellClicked);
            Trace("bind");
            if (_open)
            {
                RefreshView();
            }
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
                EndDrag(false);
                _selectedStashIndex = -1;
                _pageIndex = PageInventory;
                Trace("close");
            }
            else
            {
                Trace(_pageIndex == PageInventory ? "open:inventory(B)" : "open:stats(C)");
            }

            if (view != null)
            {
                view.SetPresentationOpen(open);
                if (open)
                {
                    RefreshView();
                }
            }

            _refreshLookCapture?.Invoke();
        }

        public void Toggle()
        {
            SetOpen(!_open);
        }

        public void ToggleStats()
        {
            if (_open)
            {
                SetOpen(false);
            }
            else
            {
                OpenStats();
            }
        }

        public void ToggleInventory()
        {
            if (_open)
            {
                SetOpen(false);
            }
            else
            {
                OpenInventory();
            }
        }

        public void OpenStats()
        {
            _pageIndex = PageStats;
            _selectedStashIndex = -1;
            SetOpen(true);
            RefreshView();
        }

        public void OpenInventory()
        {
            _pageIndex = PageInventory;
            SetOpen(true);
            RefreshView();
        }

        private void Update()
        {
            if (_open && DemoInput.KeyDown(Key.Escape))
            {
                Trace("esc:close");
                SetOpen(false);
                return;
            }

            if (!_open || _pageIndex != PageInventory)
            {
                return;
            }

            var mouse = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : (Vector2)UnityEngine.Input.mousePosition;
            if (_dragActive)
            {
                view?.UpdateDragPreviewPosition(mouse);
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (view != null && view.TryGetStashIndexAt(mouse, out var stashIdx))
                {
                    OnStashCellClicked(stashIdx);
                    return;
                }

                if (view != null && view.TryGetDollSlotAt(mouse, out var slot))
                {
                    OnDollSlotClicked(slot);
                    return;
                }

                if (_dragActive && (view == null || !view.IsPointerInsideInventory(mouse)))
                {
                    Trace("outside-click:cancel+close");
                    EndDrag(true);
                    SetOpen(false);
                }
            }
        }

        private void OnDollSlotClicked(EquipmentBodySlot slot)
        {
            if (!_open || _account == null)
            {
                return;
            }

            if (_dragActive)
            {
                if (_dragStashIndex >= 0 && CharacterPaperDoll.TryEquipFromStashToSlot(_account, _dragStashIndex, slot))
                {
                    Trace($"drop->equip slot:{slot}");
                    EndDrag(true);
                    _onMutated?.Invoke();
                    RefreshView();
                }
                else
                {
                    Trace($"drop->equip failed slot:{slot}");
                }

                return;
            }

            var equipped = CharacterPaperDoll.GetEquipped(_account, slot);
            if (equipped != null)
            {
                if (CharacterPaperDoll.TryUnequipSlotToStash(_account, slot))
                {
                    Trace($"unequip slot:{slot}");
                    _selectedStashIndex = -1;
                    _onMutated?.Invoke();
                    RefreshView();
                }
            }
            else if (_selectedStashIndex >= 0 &&
                     CharacterPaperDoll.TryEquipFromStashToSlot(_account, _selectedStashIndex, slot))
            {
                Trace($"equip selected idx:{_selectedStashIndex} -> {slot}");
                _selectedStashIndex = -1;
                _onMutated?.Invoke();
                RefreshView();
            }
        }

        private void OnStashCellClicked(int index)
        {
            if (!_open || _account == null)
            {
                return;
            }

            var stash = _account.stash;
            if (stash == null || index < 0)
            {
                return;
            }

            if (!_dragActive)
            {
                if (index >= stash.Count || !HasRealItem(stash[index]))
                {
                    return;
                }

                BeginDrag(index);
                RefreshView();
                return;
            }

            if (index == _dragStashIndex)
            {
                Trace($"cancel drag idx:{index}");
                EndDrag(true);
                RefreshView();
                return;
            }

            if (!MoveOrSwapStash(_account.stash, _dragStashIndex, index))
            {
                return;
            }

            Trace($"move/swap stash {_dragStashIndex}->{index}");
            EndDrag(true);
            _onMutated?.Invoke();
            RefreshView();
        }

        private void BeginDrag(int index)
        {
            if (_account?.stash == null || index < 0 || index >= _account.stash.Count)
            {
                return;
            }

            var it = _account.stash[index];
            if (it == null)
            {
                return;
            }

            _dragActive = true;
            _dragStashIndex = index;
            _selectedStashIndex = index;
            view?.SetEquipHighlightTemplate(it.templateId);
            Trace($"begin drag idx:{index} item:{it.templateId}");
            var mouse = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : (Vector2)UnityEngine.Input.mousePosition;
            view?.SetDragPreview(true, ItemInventoryUi.GetGlyph(it), mouse);
        }

        private void EndDrag(bool clearSelection)
        {
            _dragActive = false;
            _dragStashIndex = -1;
            if (clearSelection)
            {
                _selectedStashIndex = -1;
            }

            view?.SetEquipHighlightTemplate(null);
            Trace(clearSelection ? "end drag + clear" : "end drag");
            view?.SetDragPreview(false, string.Empty, Vector2.zero);
        }

        private static bool MoveOrSwapStash(System.Collections.Generic.List<ItemInstance> stash, int from, int to)
        {
            if (stash == null || from < 0 || to < 0 || from >= stash.Count || from == to)
            {
                return false;
            }

            var fromItem = stash[from];
            if (!HasRealItem(fromItem))
            {
                return false;
            }

            while (stash.Count <= to)
            {
                stash.Add(CreateEmptyStashSlot());
            }

            var toItem = HasRealItem(stash[to]) ? stash[to] : CreateEmptyStashSlot();
            stash[to] = fromItem;
            stash[from] = toItem;

            TrimTrailingEmptyStashSlots(stash);
            return true;
        }

        private static void TrimTrailingEmptyStashSlots(System.Collections.Generic.List<ItemInstance> stash)
        {
            if (stash == null)
            {
                return;
            }

            for (var i = stash.Count - 1; i >= 0; i--)
            {
                if (HasRealItem(stash[i]))
                {
                    break;
                }

                stash.RemoveAt(i);
            }
        }

        private static bool HasRealItem(ItemInstance item)
        {
            return item != null && !string.IsNullOrWhiteSpace(item.templateId);
        }

        private static ItemInstance CreateEmptyStashSlot()
        {
            return new ItemInstance
            {
                id = string.Empty,
                templateId = string.Empty,
                rarity = string.Empty,
                enhanceLevel = 0
            };
        }

        private void RefreshView()
        {
            if (!_open || _account == null || view == null)
            {
                return;
            }

            CharacterPaperDoll.EnsureList(_account);
            CharacterStatsService.RecalculateForCamp(_account);
            view.Display(_account, _pageIndex, _selectedStashIndex);
            view.SetDebugOverlay(BuildDebugText());
        }

        private void Trace(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            var stamp = DateTime.Now.ToString("HH:mm:ss");
            _debugLines.Enqueue($"{stamp} {message}");
            while (_debugLines.Count > 8)
            {
                _debugLines.Dequeue();
            }
        }

        private string BuildDebugText()
        {
            var state = $"drag:{_dragActive} dragIdx:{_dragStashIndex} selected:{_selectedStashIndex} page:{_pageIndex}";
            if (_debugLines.Count == 0)
            {
                return "debug: ready\n" + state;
            }

            return state + "\n" + string.Join("\n", _debugLines.ToArray());
        }
    }
}
