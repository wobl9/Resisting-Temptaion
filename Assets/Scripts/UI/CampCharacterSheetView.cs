using System;
using System.Text;
using ShatteredForge.Core;
using ShatteredForge.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ShatteredForge.UI
{
    [Serializable]
    public sealed class CampCharacterSheetDollSlotBinding
    {
        public EquipmentBodySlot bodySlot;
        public Button button;
        public Text glyphText;
        public Text labelText;
        public CampCharacterSheetHoverTip hoverTip;
    }

    /// <summary>
    /// uGUI layout for the camp character sheet. Leave references empty for a runtime-built default
    /// (same layout as the old IMGUI). Assign your own Canvas / RectTransforms to tune layout in the editor.
    /// </summary>
    [DefaultExecutionOrder(10)]
    public sealed class CampCharacterSheetView : MonoBehaviour
    {
        private const int PageInventory = 0;
        private const int PageStats = 1;
        private static readonly Color SlotDefaultColor = new(0.18f, 0.16f, 0.2f, 0.96f);
        private static readonly Color SlotHighlightColor = new(0.66f, 0.72f, 0.28f, 1f);
        private static readonly Color SlotOutlineColor = new(0.92f, 0.98f, 0.54f, 1f);
        private static readonly Vector2 SlotOutlineDistance = new(2f, -2f);

        [Header("Root (optional — auto-built when null)")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private GameObject panelRoot;

        [Header("Pages")]
        [SerializeField] private GameObject inventoryPage;
        [SerializeField] private GameObject statsPage;

        [Header("Chrome")]
        [SerializeField] private Text goldText;
        [SerializeField] private Text hintInventoryText;
        [SerializeField] private Text statsBodyText;
        [SerializeField] private Text debugOverlayText;
        [SerializeField] private bool debugOverlayEnabled = true;

        [Header("Paper doll (9 slots, same order as metadata)")]
        [SerializeField] private CampCharacterSheetDollSlotBinding[] dollSlots;

        [Header("Stash")]
        [SerializeField] private Transform stashGrid;
        [SerializeField] private CampCharacterSheetStashCellUi stashCellPrefab;
        [SerializeField] [Min(20)] private int stashCellPoolSize = 144;

        [Header("Layout source (optional)")]
        [SerializeField] private CampCharacterSheetLayoutAsset layoutAsset;
        [SerializeField] private CampCharacterSheetSkinAsset skinAsset;

        [Header("Tooltip")]
        [SerializeField] private CampCharacterSheetTooltipHost tooltipHost;
        [SerializeField] private RectTransform dragGhostRoot;
        [SerializeField] private Text dragGhostText;

        private CampCharacterSheetStashCellUi[] _stashPool = Array.Empty<CampCharacterSheetStashCellUi>();

        private bool _built;
        private string _equipTargetTemplateId;

        public CampCharacterSheetLayoutAsset LayoutAsset
        {
            get => layoutAsset;
            set => layoutAsset = value;
        }

        private CampCharacterSheetLayoutAsset EffectiveLayoutAsset
        {
            get
            {
                if (layoutAsset == null)
                {
                    return null;
                }

                layoutAsset.EnsureSlotList();
                return layoutAsset;
            }
        }

        private static Vector2 ResolvePaperDollAnchoredPosition(
            CampCharacterSheetPaperDollLayout layout,
            Vector2 sizeDelta,
            Vector2 fallbackAnchored)
        {
            if (layout == null)
            {
                return fallbackAnchored;
            }

            if (!layout.useNormalizedTorsoPosition)
            {
                return fallbackAnchored;
            }

            var maxX = Mathf.Max(1f, layout.sizeDelta.x - sizeDelta.x);
            var maxY = Mathf.Max(1f, layout.sizeDelta.y - sizeDelta.y);
            return new Vector2(
                Mathf.Clamp01(layout.torsoNormalizedPosition.x) * maxX,
                -Mathf.Clamp01(layout.torsoNormalizedPosition.y) * maxY);
        }

        private static Vector2 ResolveSlotAnchoredPosition(
            CampCharacterSheetPaperDollLayout layout,
            CampCharacterSheetBodySlotLayout slot,
            Vector2 fallbackAnchored,
            Vector2 sizeDelta)
        {
            if (layout == null || slot == null || !slot.useNormalizedPosition)
            {
                return fallbackAnchored;
            }

            var maxX = Mathf.Max(1f, layout.sizeDelta.x - sizeDelta.x);
            var maxY = Mathf.Max(1f, layout.sizeDelta.y - sizeDelta.y);
            return new Vector2(
                Mathf.Clamp01(slot.normalizedPosition.x) * maxX,
                -Mathf.Clamp01(slot.normalizedPosition.y) * maxY);
        }

        private static Vector2 ComputeNormalizedFromAnchored(Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 parentSize)
        {
            var maxX = Mathf.Max(1f, parentSize.x - sizeDelta.x);
            var maxY = Mathf.Max(1f, parentSize.y - sizeDelta.y);
            return new Vector2(
                Mathf.Clamp01(anchoredPosition.x / maxX),
                Mathf.Clamp01((-anchoredPosition.y) / maxY));
        }

        private void ApplySkin(Image image, Sprite sprite, Color tint)
        {
            if (image == null || sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = tint;
        }

        public void EnsureBuilt()
        {
            if (_built)
            {
                return;
            }

            TryAutoWireExistingHierarchy();

            if (!IsUserHierarchyComplete())
            {
                if (rootCanvas == null && panelRoot == null)
                {
                    BuildDefaultUi(startHidden: true);
                }
                else
                {
                    TryAutoWireExistingHierarchy();
                }
            }

            if (!IsUserHierarchyComplete())
            {
                if (rootCanvas != null || panelRoot != null)
                {
                    Debug.LogWarning(
                        $"{nameof(CampCharacterSheetView)} on {gameObject.name}: UI references incomplete. " +
                        "Leave root Canvas / Panel empty for auto-generated UI, or assign every field (9 doll slots, stash prefab, tooltip, …).");
                }
            }

            var la = EffectiveLayoutAsset;
            if (la != null)
            {
                stashCellPoolSize = Mathf.Max(20, la.stash.poolSize);
            }

            EnsureStashPool();
            if (tooltipHost == null)
            {
                Debug.LogWarning($"{nameof(CampCharacterSheetView)}: tooltip host missing.");
            }

            EnsureDragGhostExists();
            _built = true;
        }

        private void TryAutoWireExistingHierarchy()
        {
            if (rootCanvas == null)
            {
                rootCanvas = GetComponentInChildren<Canvas>(true);
            }

            if (panelRoot == null && rootCanvas != null)
            {
                var p = rootCanvas.transform.Find("Panel");
                panelRoot = p != null ? p.gameObject : null;
            }

            var inner = panelRoot != null ? panelRoot.transform.Find("Inner") : null;
            if (inventoryPage == null && inner != null)
            {
                var t = inner.Find("InventoryPage");
                inventoryPage = t != null ? t.gameObject : null;
            }

            if (statsPage == null && inner != null)
            {
                var t = inner.Find("StatsPage");
                statsPage = t != null ? t.gameObject : null;
            }

            if (goldText == null && inner != null)
            {
                goldText = inner.Find("Gold")?.GetComponent<Text>();
            }

            if (hintInventoryText == null && inventoryPage != null)
            {
                hintInventoryText = inventoryPage.transform.Find("Hint")?.GetComponent<Text>();
            }

            if (statsBodyText == null && statsPage != null)
            {
                statsBodyText = statsPage.transform.Find("StatsBody")?.GetComponent<Text>();
            }

            if (debugOverlayText == null && inventoryPage != null)
            {
                debugOverlayText = inventoryPage.transform.Find("DebugOverlay")?.GetComponent<Text>();
            }

            if (stashGrid == null && inventoryPage != null)
            {
                stashGrid = inventoryPage.transform.Find("StashGrid");
            }

            if (tooltipHost == null && rootCanvas != null)
            {
                tooltipHost = rootCanvas.GetComponentInChildren<CampCharacterSheetTooltipHost>(true);
            }

            if (dragGhostRoot == null && rootCanvas != null)
            {
                var dg = rootCanvas.transform.Find("DragGhost") as RectTransform;
                if (dg != null)
                {
                    dragGhostRoot = dg;
                    dragGhostText = dg.Find("Text")?.GetComponent<Text>();
                }
            }

            if (stashCellPrefab == null)
            {
                var t = transform.Find("_StashCellTemplate");
                if (t != null)
                {
                    stashCellPrefab = t.GetComponent<CampCharacterSheetStashCellUi>();
                }
            }

            if ((dollSlots == null || dollSlots.Length != CampCharacterSheetMetadata.DollSlotUiOrder.Length) &&
                inventoryPage != null)
            {
                var paperDoll = inventoryPage.transform.Find("PaperDoll");
                if (paperDoll != null)
                {
                    var rebuilt = new CampCharacterSheetDollSlotBinding[CampCharacterSheetMetadata.DollSlotUiOrder.Length];
                    for (var i = 0; i < CampCharacterSheetMetadata.DollSlotUiOrder.Length; i++)
                    {
                        var (slot, _) = CampCharacterSheetMetadata.DollSlotUiOrder[i];
                        var slotTr = paperDoll.Find($"Slot_{slot}");
                        if (slotTr == null)
                        {
                            continue;
                        }

                        rebuilt[i] = new CampCharacterSheetDollSlotBinding
                        {
                            bodySlot = slot,
                            button = slotTr.GetComponent<Button>(),
                            glyphText = slotTr.Find("Glyph")?.GetComponent<Text>(),
                            labelText = paperDoll.Find($"Lbl_{slot}")?.GetComponent<Text>(),
                            hoverTip = slotTr.GetComponent<CampCharacterSheetHoverTip>()
                        };
                    }

                    dollSlots = rebuilt;
                }
            }
        }

        private void Awake()
        {
            EnsureBuilt();
        }

        private void OnEnable()
        {
            EnsureUiEventSystemExists();
        }

        public void SetPresentationOpen(bool open)
        {
            if (rootCanvas != null)
            {
                rootCanvas.gameObject.SetActive(open);
                if (!open)
                {
                    SetDragPreview(false, string.Empty, Vector2.zero);
                }
                return;
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(open);
            }
        }

        public void SetDragPreview(bool active, string glyph, Vector2 screenPosition)
        {
            EnsureDragGhostExists();
            if (dragGhostRoot == null || dragGhostText == null)
            {
                return;
            }

            if (!active)
            {
                dragGhostRoot.gameObject.SetActive(false);
                return;
            }

            dragGhostText.text = string.IsNullOrEmpty(glyph) ? "?" : glyph;
            dragGhostRoot.gameObject.SetActive(true);
            PlaceNearCursor(dragGhostRoot, screenPosition, new Vector2(18f, -18f));
        }

        public void UpdateDragPreviewPosition(Vector2 screenPosition)
        {
            EnsureDragGhostExists();
            if (dragGhostRoot == null || !dragGhostRoot.gameObject.activeSelf)
            {
                return;
            }

            PlaceNearCursor(dragGhostRoot, screenPosition, new Vector2(18f, -18f));
        }

        public bool IsPointerInsideInventory(Vector2 screenPosition)
        {
            if (inventoryPage == null || !inventoryPage.activeInHierarchy)
            {
                return false;
            }

            var invRt = inventoryPage.transform as RectTransform;
            var cam = rootCanvas != null && rootCanvas.renderMode == RenderMode.ScreenSpaceCamera ? rootCanvas.worldCamera : null;
            return invRt != null && RectTransformUtility.RectangleContainsScreenPoint(invRt, screenPosition, cam);
        }

        public void SetHandlers(
            Action<EquipmentBodySlot> onDollSlot,
            Action<int> onStashCell)
        {
            // Kept for backward compatibility; click handling is now done by panel hit-testing.
            WireDollSlots();
            WireStashPool();
        }

        public void Display(AccountState account, int pageIndex, int selectedStashIndex)
        {
            if (account == null || goldText == null)
            {
                return;
            }

            goldText.text = $"Золото: {account.gold}";

            if (inventoryPage != null)
            {
                inventoryPage.SetActive(pageIndex == PageInventory);
            }

            if (statsPage != null)
            {
                statsPage.SetActive(pageIndex == PageStats);
            }

            if (pageIndex == PageInventory)
            {
                RefreshInventoryPage(account, selectedStashIndex);
            }
            else if (statsBodyText != null)
            {
                statsBodyText.text = BuildStatsPageText(account);
            }

            if (debugOverlayText != null)
            {
                debugOverlayText.gameObject.SetActive(debugOverlayEnabled && pageIndex == PageInventory);
            }
        }

        public void SetDebugOverlay(string text)
        {
            if (debugOverlayText == null)
            {
                return;
            }

            debugOverlayText.text = text ?? string.Empty;
            debugOverlayText.gameObject.SetActive(debugOverlayEnabled);
        }

        public void SetEquipHighlightTemplate(string templateId)
        {
            _equipTargetTemplateId = templateId;
        }

        private bool IsUserHierarchyComplete()
        {
            return panelRoot != null
                   && rootCanvas != null
                   && inventoryPage != null
                   && statsPage != null
                   && goldText != null
                   && statsBodyText != null
                   && stashGrid != null
                   && stashCellPrefab != null
                   && tooltipHost != null
                   && dollSlots != null
                   && dollSlots.Length == CampCharacterSheetMetadata.DollSlotUiOrder.Length
                   && DollBindingsValid(dollSlots);
        }

        private static bool DollBindingsValid(CampCharacterSheetDollSlotBinding[] slots)
        {
            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null || slots[i].button == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void WireDollSlots()
        {
            // No-op: click handling moved to panel manual hit-tests.
        }

        private void WireStashPool()
        {
            if (_stashPool == null || _stashPool.Length == 0)
            {
                return;
            }

            for (var i = 0; i < _stashPool.Length; i++)
            {
                var cell = _stashPool[i];
                if (cell == null)
                {
                    continue;
                }

                cell.Wire(null, tooltipHost);
            }
        }

        public bool TryGetStashIndexAt(Vector2 screenPosition, out int index)
        {
            index = -1;
            var cam = rootCanvas != null && rootCanvas.renderMode == RenderMode.ScreenSpaceCamera ? rootCanvas.worldCamera : null;
            if (_stashPool == null)
            {
                return false;
            }

            for (var i = 0; i < _stashPool.Length; i++)
            {
                var cell = _stashPool[i];
                if (cell == null || !cell.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!cell.ContainsScreenPoint(screenPosition, cam))
                {
                    continue;
                }

                index = cell.CurrentIndex;
                return true;
            }

            return false;
        }

        public bool TryGetDollSlotAt(Vector2 screenPosition, out EquipmentBodySlot slot)
        {
            slot = EquipmentBodySlot.None;
            var cam = rootCanvas != null && rootCanvas.renderMode == RenderMode.ScreenSpaceCamera ? rootCanvas.worldCamera : null;
            if (dollSlots == null)
            {
                return false;
            }

            for (var i = 0; i < dollSlots.Length; i++)
            {
                var b = dollSlots[i];
                var rt = b?.button != null ? b.button.transform as RectTransform : null;
                if (rt == null || !b.button.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!RectTransformUtility.RectangleContainsScreenPoint(rt, screenPosition, cam))
                {
                    continue;
                }

                slot = b.bodySlot;
                return true;
            }

            return false;
        }

        private void RefreshInventoryPage(AccountState account, int selectedStashIndex)
        {
            if (hintInventoryText != null)
            {
                hintInventoryText.text =
                    "Снабжение: клик по предмету — взять в руку, клик по другому слоту — обмен, клик по слоту тела — надеть.";
            }

            for (var i = 0; i < CampCharacterSheetMetadata.DollSlotUiOrder.Length; i++)
            {
                var meta = CampCharacterSheetMetadata.DollSlotUiOrder[i];
                var bind = FindBinding(meta.slot);
                if (bind?.button == null)
                {
                    continue;
                }

                var equipped = CharacterPaperDoll.GetEquipped(account, meta.slot);
                if (bind.glyphText != null)
                {
                    bind.glyphText.text = equipped != null ? ItemInventoryUi.GetGlyph(equipped) : "—";
                }

                if (bind.labelText != null)
                {
                    bind.labelText.text = string.Empty;
                }

                var tip = equipped != null
                    ? ItemInventoryUi.BuildTooltip(equipped)
                    : $"Пусто — {meta.label}. Выберите вещь в снабжении и нажмите эту ячейку.";
                if (bind.hoverTip != null && tooltipHost != null)
                {
                    bind.hoverTip.Configure(tooltipHost, tip);
                }

                var img = bind.button.targetGraphic as Graphic;
                var canWearHere = !string.IsNullOrEmpty(_equipTargetTemplateId) &&
                                  CampItemSlotRules.CanWearInBodySlot(_equipTargetTemplateId, meta.slot);
                if (img != null)
                {
                    img.color = canWearHere
                        ? SlotHighlightColor
                        : SlotDefaultColor;
                }

                ConfigureSlotOutline(bind.button, canWearHere);
            }

            RefreshStashGrid(account, selectedStashIndex);
        }

        private CampCharacterSheetDollSlotBinding FindBinding(EquipmentBodySlot slot)
        {
            if (dollSlots == null)
            {
                return null;
            }

            for (var i = 0; i < dollSlots.Length; i++)
            {
                if (dollSlots[i] != null && dollSlots[i].bodySlot == slot)
                {
                    return dollSlots[i];
                }
            }

            return null;
        }

        private void ConfigureSlotOutline(Button button, bool active)
        {
            if (button == null)
            {
                return;
            }

            var outline = button.GetComponent<Outline>();
            if (outline == null && active)
            {
                outline = button.gameObject.AddComponent<Outline>();
                outline.useGraphicAlpha = false;
            }

            if (outline == null)
            {
                return;
            }

            outline.effectColor = SlotOutlineColor;
            outline.effectDistance = SlotOutlineDistance;
            outline.enabled = active;
        }

        private void EnsureStashPool()
        {
            if (stashGrid == null || stashCellPrefab == null)
            {
                return;
            }

            if (_stashPool.Length == stashCellPoolSize)
            {
                return;
            }

            _stashPool = new CampCharacterSheetStashCellUi[stashCellPoolSize];
            for (var i = 0; i < stashCellPoolSize; i++)
            {
                var go = Instantiate(stashCellPrefab.gameObject, stashGrid);
                go.name = $"Stash_{i}";
                var cell = go.GetComponent<CampCharacterSheetStashCellUi>();
                _stashPool[i] = cell;
            }
        }

        private void RefreshStashGrid(AccountState account, int selectedStashIndex)
        {
            if (_stashPool.Length == 0 || account == null)
            {
                return;
            }

            var stash = account.stash;
            var count = stash != null ? stash.Count : 0;
            var la = EffectiveLayoutAsset;
            var cols = la != null ? Mathf.Max(1, la.stash.columns) : 12;
            var minRows = la != null ? Mathf.Max(1, la.stash.minRows) : 8;
            var rows = Mathf.Max(minRows, (count + cols - 1) / cols);
            var totalCells = rows * cols;
            var use = Mathf.Min(totalCells, _stashPool.Length);

            for (var i = 0; i < use; i++)
            {
                var cell = _stashPool[i];
                if (i < count && HasRealItem(stash[i]))
                {
                    var it = stash[i];
                    cell.Apply(
                        i,
                        true,
                        i == selectedStashIndex,
                        ItemInventoryUi.GetGlyph(it),
                        ItemInventoryUi.BuildTooltip(it),
                        tooltipHost);
                }
                else
                {
                    cell.Apply(i, false, false, string.Empty, string.Empty, tooltipHost);
                }
            }

            for (var j = use; j < _stashPool.Length; j++)
            {
                _stashPool[j].gameObject.SetActive(false);
            }
        }

        private static string BuildStatsPageText(AccountState account)
        {
            var primary = account.primaryStats ?? CharacterPrimaryStats.CreateDefault();
            var computed = account.computedStats ?? new ComputedCharacterStats();
            var resists = computed.elementalResists ?? new ElementalResistanceProfile();

            var sb = new StringBuilder(512);
            sb.AppendLine("<b>Базовые характеристики</b>");
            sb.AppendLine($"Сила: {primary.strength}");
            sb.AppendLine($"Ловкость: {primary.agility}");
            sb.AppendLine($"Выносливость: {primary.vitality}");
            sb.AppendLine($"Интеллект: {primary.intellect}");
            sb.AppendLine();
            sb.AppendLine("<b>Бой</b>");
            sb.AppendLine($"Урон: {computed.damage}");
            sb.AppendLine($"Броня: {computed.armor}");
            sb.AppendLine($"Скорость атаки: {computed.attackSpeed:0.00}");
            sb.AppendLine($"Шанс критического удара: {computed.critChance * 100f:0.#}%");
            sb.AppendLine($"Мана: {computed.mana}");
            sb.AppendLine($"Сила магии: {computed.magicPower}");
            sb.AppendLine();
            sb.AppendLine("<b>Сопротивления стихиям</b>");
            sb.AppendLine($"Огонь: {resists.fire}");
            sb.AppendLine($"Холод: {resists.cold}");
            sb.AppendLine($"Молния: {resists.lightning}");
            sb.AppendLine();
            sb.AppendLine("<b>Ресурсы (мета)</b>");
            sb.AppendLine($"Золото: {account.gold}");
            sb.AppendLine($"Пыль кузницы: {account.forgeDust}");
            sb.AppendLine($"Угольные ядра: {account.emberCore}");
            sb.AppendLine($"Жетоны сигилов: {account.sigilToken}");
            sb.AppendLine($"Печати страховки: {account.insuranceSeal}");
            return sb.ToString().TrimEnd();
        }

        private static bool HasRealItem(ItemInstance item)
        {
            return item != null && !string.IsNullOrWhiteSpace(item.templateId);
        }

        private static void EnsureUiEventSystemExists()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

#if UNITY_EDITOR
        public void EditorSetLayoutAsset(CampCharacterSheetLayoutAsset asset)
        {
            layoutAsset = asset;
        }

        public void EditorSetSkinAsset(CampCharacterSheetSkinAsset asset)
        {
            skinAsset = asset;
        }

        public void EditorRebuildFromLayout(CampCharacterSheetLayoutAsset asset, bool startHidden)
        {
            layoutAsset = asset;
            DestroyGeneratedChildrenImmediate();
            _built = false;
            BuildDefaultUi(startHidden);
        }

        public void EditorCaptureCurrentLayout(CampCharacterSheetLayoutAsset asset)
        {
            if (asset == null || panelRoot == null)
            {
                return;
            }

            asset.EnsureSlotList();
            var canvas = rootCanvas != null ? rootCanvas.GetComponent<CanvasScaler>() : null;
            if (canvas != null)
            {
                asset.canvas.referenceResolution = canvas.referenceResolution;
                asset.canvas.widthHeightMatch = canvas.matchWidthOrHeight;
            }

            var panelRt = panelRoot.transform as RectTransform;
            if (panelRt != null)
            {
                asset.canvas.panelSize = panelRt.sizeDelta;
                var inner = panelRoot.transform.Find("Inner") as RectTransform;
                if (inner != null)
                {
                    asset.canvas.innerOffsetMin = inner.offsetMin;
                    asset.canvas.innerOffsetMax = inner.offsetMax;

                    var title = inner.Find("Title") as RectTransform;
                    var gold = inner.Find("Gold") as RectTransform;
                    if (title != null)
                    {
                        asset.chrome.titlePos = title.anchoredPosition;
                        asset.chrome.titleSize = title.sizeDelta;
                    }

                    if (gold != null)
                    {
                        asset.chrome.goldPos = gold.anchoredPosition;
                        asset.chrome.goldSize = gold.sizeDelta;
                    }

                }
            }

            var doll = inventoryPage != null ? inventoryPage.transform.Find("PaperDoll") as RectTransform : null;
            if (doll != null)
            {
                asset.paperDoll.anchoredPosition = doll.anchoredPosition;
                asset.paperDoll.sizeDelta = doll.sizeDelta;
                var torso = doll.Find("Torso") as RectTransform;
                if (torso != null)
                {
                    asset.paperDoll.torsoAnchoredPosition = torso.anchoredPosition;
                    asset.paperDoll.torsoSize = torso.sizeDelta;
                    asset.paperDoll.torsoNormalizedPosition = ComputeNormalizedFromAnchored(
                        torso.anchoredPosition,
                        torso.sizeDelta,
                        asset.paperDoll.sizeDelta);
                }

                for (var i = 0; i < asset.paperDoll.slots.Length; i++)
                {
                    var slotCfg = asset.paperDoll.slots[i];
                    var slotRt = doll.Find($"Slot_{slotCfg.slot}") as RectTransform;
                    if (slotRt == null)
                    {
                        continue;
                    }

                    slotCfg.anchoredPosition = slotRt.anchoredPosition;
                    slotCfg.sizeDelta = slotRt.sizeDelta;
                    slotCfg.normalizedPosition = ComputeNormalizedFromAnchored(
                        slotRt.anchoredPosition,
                        slotRt.sizeDelta,
                        asset.paperDoll.sizeDelta);
                }
            }

            var stash = inventoryPage != null ? inventoryPage.transform.Find("StashGrid") as RectTransform : null;
            if (stash != null)
            {
                asset.stash.anchoredPosition = stash.anchoredPosition;
                asset.stash.sizeDelta = stash.sizeDelta;
                var grid = stash.GetComponent<GridLayoutGroup>();
                if (grid != null)
                {
                    asset.stash.cellSize = grid.cellSize;
                    asset.stash.spacing = grid.spacing;
                    asset.stash.columns = Mathf.Max(1, grid.constraintCount);
                }
            }

            var caption = inventoryPage != null ? inventoryPage.transform.Find("StashCaption") as RectTransform : null;
            if (caption != null)
            {
                asset.stash.captionAnchoredPosition = caption.anchoredPosition;
                asset.stash.captionSize = caption.sizeDelta;
            }

            if (tooltipHost != null)
            {
                var tooltipRt = tooltipHost.transform as RectTransform;
                if (tooltipRt != null)
                {
                    asset.tooltip.sizeDelta = tooltipRt.sizeDelta;
                }
            }
        }

        private void DestroyGeneratedChildrenImmediate()
        {
            if (rootCanvas != null)
            {
                DestroyImmediate(rootCanvas.gameObject);
            }

            if (panelRoot != null && panelRoot.transform.parent == transform)
            {
                DestroyImmediate(panelRoot);
            }

            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child != null && child.gameObject.name == "_StashCellTemplate")
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            panelRoot = null;
            rootCanvas = null;
            inventoryPage = null;
            statsPage = null;
            goldText = null;
            hintInventoryText = null;
            statsBodyText = null;
            debugOverlayText = null;
            stashGrid = null;
            stashCellPrefab = null;
            tooltipHost = null;
            dragGhostRoot = null;
            dragGhostText = null;
            dollSlots = null;
            _stashPool = Array.Empty<CampCharacterSheetStashCellUi>();
        }

        /// <summary>
        /// Editor: builds the default hierarchy (visible canvas) so you can save this object as a prefab
        /// and tune RectTransforms. Menu: ShatteredForge → UI → Bake Camp Character Sheet UI Prefab.
        /// </summary>
        public void EditorBakeDefaultUiForPrefab()
        {
            if (rootCanvas != null)
            {
                Debug.LogWarning(
                    $"{nameof(CampCharacterSheetView)} on {gameObject.name}: уже есть UI — удалите дочерние объекты и сбросьте ссылки, чтобы пересобрать.");
                return;
            }

            BuildDefaultUi(startHidden: false);
        }
#endif

        private void BuildDefaultUi(bool startHidden = true)
        {
            var canvasGo = new GameObject("_CampSheetCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            rootCanvas = canvasGo.AddComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.sortingOrder = 800;
            rootCanvas.pixelPerfect = true;
            canvasGo.AddComponent<GraphicRaycaster>();
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            var la = EffectiveLayoutAsset;
            var canvasLayout = la != null ? la.canvas : new CampCharacterSheetCanvasLayout();
            var chromeLayout = la != null ? la.chrome : new CampCharacterSheetChromeLayout();
            var dollLayout = la != null ? la.paperDoll : new CampCharacterSheetPaperDollLayout();
            var stashLayout = la != null ? la.stash : new CampCharacterSheetStashLayout();
            var tooltipLayout = la != null ? la.tooltip : new CampCharacterSheetTooltipLayout();

            scaler.referenceResolution = canvasLayout.referenceResolution;
            scaler.matchWidthOrHeight = canvasLayout.widthHeightMatch;

            panelRoot = CreatePanel(canvasGo.transform, canvasLayout.panelSize);
            if (skinAsset != null)
            {
                ApplySkin(panelRoot.GetComponent<Image>(), skinAsset.panelSprite, skinAsset.panelTint);
            }

            var inner = CreateStretchRect(panelRoot.transform, "Inner", canvasLayout.innerOffsetMin, canvasLayout.innerOffsetMax);

            goldText = CreateText(inner.transform, "Gold", chromeLayout.goldPos, chromeLayout.goldSize, chromeLayout.goldFont, FontStyle.Bold, TextAnchor.UpperLeft);
            var title = CreateText(inner.transform, "Title", chromeLayout.titlePos, chromeLayout.titleSize, chromeLayout.titleFont, FontStyle.Bold, TextAnchor.UpperLeft);
            title.color = new Color(0.92f, 0.85f, 0.73f, 1f);
            title.text = "Персонаж";
            goldText.color = new Color(0.95f, 0.76f, 0.31f, 1f);

            inventoryPage = new GameObject("InventoryPage", typeof(RectTransform));
            var invRt = inventoryPage.GetComponent<RectTransform>();
            invRt.SetParent(inner.transform, false);
            StretchFull(invRt, 0f, 0f, 0f, 90f);

            hintInventoryText = CreateText(
                inventoryPage.transform,
                "Hint",
                new Vector2(10f, -8f),
                new Vector2(-12f, 36f),
                chromeLayout.hintFont,
                FontStyle.Normal,
                TextAnchor.UpperLeft);
            var hintRt = hintInventoryText.rectTransform;
            hintRt.anchorMin = new Vector2(0f, 1f);
            hintRt.anchorMax = new Vector2(1f, 1f);
            hintRt.pivot = new Vector2(0f, 1f);
            hintRt.offsetMin = chromeLayout.hintOffsetMin;
            hintRt.offsetMax = chromeLayout.hintOffsetMax;

            debugOverlayText = CreateText(
                inventoryPage.transform,
                "DebugOverlay",
                new Vector2(10f, -106f),
                new Vector2(620f, 120f),
                12,
                FontStyle.Normal,
                TextAnchor.UpperLeft);
            debugOverlayText.color = new Color(0.95f, 0.42f, 0.42f, 1f);
            debugOverlayText.text = "debug: init";
            debugOverlayText.gameObject.SetActive(debugOverlayEnabled);

            var dollBlock = new GameObject("PaperDoll", typeof(RectTransform));
            var dollRt = dollBlock.GetComponent<RectTransform>();
            dollBlock.transform.SetParent(inventoryPage.transform, false);
            dollRt.anchorMin = dollRt.anchorMax = new Vector2(0f, 1f);
            dollRt.pivot = new Vector2(0f, 1f);
            dollRt.anchoredPosition = dollLayout.anchoredPosition;
            dollRt.sizeDelta = dollLayout.sizeDelta;

            var torsoGo = new GameObject("Torso", typeof(RectTransform), typeof(Image));
            var torsoRt = torsoGo.GetComponent<RectTransform>();
            torsoRt.SetParent(dollBlock.transform, false);
            torsoRt.anchorMin = torsoRt.anchorMax = new Vector2(0f, 1f);
            torsoRt.pivot = new Vector2(0f, 1f);
            torsoRt.anchoredPosition = ResolvePaperDollAnchoredPosition(dollLayout, dollLayout.torsoSize, dollLayout.torsoAnchoredPosition);
            torsoRt.sizeDelta = dollLayout.torsoSize;
            torsoGo.GetComponent<Image>().color = new Color(0.16f, 0.16f, 0.2f, 1f);
            if (skinAsset != null)
            {
                ApplySkin(torsoGo.GetComponent<Image>(), skinAsset.torsoSprite, skinAsset.torsoTint);
            }

            dollSlots = new CampCharacterSheetDollSlotBinding[CampCharacterSheetMetadata.DollSlotUiOrder.Length];
            for (var i = 0; i < CampCharacterSheetMetadata.DollSlotUiOrder.Length; i++)
            {
                var (slot, label) = CampCharacterSheetMetadata.DollSlotUiOrder[i];
                var fallbackCol = i % 2;
                var fallbackRow = i / 2;
                var fallbackAnchor = new Vector2(12f + fallbackCol * 222f, -(12f + fallbackRow * 92f));
                var fallbackSize = new Vector2(76f, 76f);
                var slotLayout = dollLayout.slots != null && i < dollLayout.slots.Length ? dollLayout.slots[i] : null;
                var sz = slotLayout != null ? slotLayout.sizeDelta : fallbackSize;
                var anchor = slotLayout != null
                    ? ResolveSlotAnchoredPosition(dollLayout, slotLayout, slotLayout.anchoredPosition, sz)
                    : fallbackAnchor;
                var bind = new CampCharacterSheetDollSlotBinding { bodySlot = slot };
                bind.labelText = CreateText(
                    dollBlock.transform,
                    $"Lbl_{slot}",
                    new Vector2(anchor.x, anchor.y + 15f),
                    new Vector2(Mathf.Max(88f, sz.x), 18f),
                    14,
                    FontStyle.Normal,
                    TextAnchor.LowerLeft);
                bind.labelText.color = new Color(0.9f, 0.84f, 0.73f, 1f);
                bind.labelText.text = string.Empty;
                bind.labelText.gameObject.SetActive(false);
                var labelRt = bind.labelText.rectTransform;
                labelRt.anchorMin = labelRt.anchorMax = new Vector2(0f, 1f);
                labelRt.pivot = new Vector2(0f, 0f);

                var (btnGo, btn, glyph) = CreateSlotButton(dollBlock.transform, anchor, sz);
                btnGo.name = $"Slot_{slot}";
                if (slotLayout != null && slotLayout.iconSprite != null)
                {
                    var img = btnGo.GetComponent<Image>();
                    img.sprite = slotLayout.iconSprite;
                    img.type = Image.Type.Sliced;
                }
                else if (skinAsset != null)
                {
                    ApplySkin(btnGo.GetComponent<Image>(), skinAsset.slotSprite, skinAsset.slotTint);
                }

                bind.button = btn;
                bind.glyphText = glyph;
                bind.hoverTip = btnGo.AddComponent<CampCharacterSheetHoverTip>();
                dollSlots[i] = bind;
            }

            var stashGo = new GameObject("StashGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            stashGo.transform.SetParent(inventoryPage.transform, false);
            var stashRt = stashGo.GetComponent<RectTransform>();
            stashRt.anchorMin = stashRt.anchorMax = new Vector2(0f, 1f);
            stashRt.pivot = new Vector2(0f, 1f);
            stashRt.anchoredPosition = stashLayout.anchoredPosition;
            stashRt.sizeDelta = stashLayout.sizeDelta;
            var grid = stashGo.GetComponent<GridLayoutGroup>();
            grid.cellSize = stashLayout.cellSize;
            grid.spacing = stashLayout.spacing;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Mathf.Max(1, stashLayout.columns);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            stashGrid = stashGo.transform;

            var stashCaption = CreateText(
                inventoryPage.transform,
                "StashCaption",
                stashLayout.captionAnchoredPosition,
                stashLayout.captionSize,
                15,
                FontStyle.Bold,
                TextAnchor.UpperLeft);
            stashCaption.color = new Color(0.9f, 0.84f, 0.73f, 1f);
            stashCaption.text = "Снабжение (ячейки)";

            statsPage = new GameObject("StatsPage", typeof(RectTransform));
            var stRt = statsPage.GetComponent<RectTransform>();
            statsPage.transform.SetParent(inner.transform, false);
            StretchFull(stRt, 0f, 0f, 0f, 90f);
            statsBodyText = CreateText(statsPage.transform, "StatsBody", new Vector2(24f, -90f), new Vector2(-48f, -120f), 18, FontStyle.Normal, TextAnchor.UpperLeft);
            var stBodyRt = statsBodyText.rectTransform;
            stBodyRt.anchorMin = Vector2.zero;
            stBodyRt.anchorMax = Vector2.one;
            stBodyRt.offsetMin = new Vector2(24f, 40f);
            stBodyRt.offsetMax = new Vector2(-24f, -90f);
            statsBodyText.supportRichText = true;
            statsBodyText.lineSpacing = 1.2f;
            statsBodyText.color = new Color(0.85f, 0.8f, 0.7f, 1f);

            var tipGo = new GameObject("Tooltip", typeof(RectTransform));
            tipGo.transform.SetParent(canvasGo.transform, false);
            var tipRt = tipGo.GetComponent<RectTransform>();
            tipRt.anchorMin = tipRt.anchorMax = new Vector2(0f, 1f);
            tipRt.pivot = new Vector2(0f, 1f);
            tipRt.sizeDelta = tooltipLayout.sizeDelta;
            var tipBg = tipGo.AddComponent<Image>();
            tipBg.color = new Color(0.11f, 0.11f, 0.13f, 0.97f);
            if (skinAsset != null)
            {
                ApplySkin(tipBg, skinAsset.tooltipSprite, skinAsset.tooltipTint);
            }
            var tipTextGo = new GameObject("Body", typeof(RectTransform));
            tipTextGo.transform.SetParent(tipGo.transform, false);
            var tipTextRt = tipTextGo.GetComponent<RectTransform>();
            tipTextRt.anchorMin = Vector2.zero;
            tipTextRt.anchorMax = Vector2.one;
            tipTextRt.offsetMin = new Vector2(8f, 8f);
            tipTextRt.offsetMax = new Vector2(-8f, -8f);
            var tipTx = tipTextGo.AddComponent<Text>();
            tipTx.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tipTx.fontSize = tooltipLayout.fontSize;
            tipTx.color = new Color(0.95f, 0.95f, 0.92f);
            tipTx.alignment = TextAnchor.UpperLeft;
            tipTx.horizontalOverflow = HorizontalWrapMode.Wrap;
            tipTx.verticalOverflow = VerticalWrapMode.Overflow;

            tooltipHost = tipGo.AddComponent<CampCharacterSheetTooltipHost>();
            tooltipHost.Bind(tipRt, tipTx);
            tooltipHost.ConfigureMaxSize(tooltipLayout.maxWidth, tooltipLayout.maxHeight);

            var dragGo = new GameObject("DragGhost", typeof(RectTransform), typeof(Image));
            dragGo.transform.SetParent(canvasGo.transform, false);
            var dragRt = dragGo.GetComponent<RectTransform>();
            dragRt.anchorMin = dragRt.anchorMax = new Vector2(0f, 1f);
            dragRt.pivot = new Vector2(0f, 1f);
            dragRt.sizeDelta = stashLayout.cellSize;
            var dragBg = dragGo.GetComponent<Image>();
            dragBg.color = new Color(0.14f, 0.14f, 0.14f, 0.86f);
            var dragTextGo = new GameObject("Text", typeof(RectTransform));
            dragTextGo.transform.SetParent(dragGo.transform, false);
            var dragTextRt = dragTextGo.GetComponent<RectTransform>();
            dragTextRt.anchorMin = Vector2.zero;
            dragTextRt.anchorMax = Vector2.one;
            dragTextRt.offsetMin = Vector2.zero;
            dragTextRt.offsetMax = Vector2.zero;
            var dragText = dragTextGo.AddComponent<Text>();
            dragText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            dragText.fontSize = 21;
            dragText.fontStyle = FontStyle.Bold;
            dragText.alignment = TextAnchor.MiddleCenter;
            dragText.color = new Color(0.95f, 0.89f, 0.78f, 1f);
            dragGhostRoot = dragRt;
            dragGhostText = dragText;
            dragGhostRoot.gameObject.SetActive(false);

            var stashTemplate = CreateStashCellTemplate(transform, stashLayout.cellSize);
            if (skinAsset != null)
            {
                var stashImage = stashTemplate.GetComponent<Image>();
                ApplySkin(stashImage, skinAsset.stashCellSprite, skinAsset.stashCellTint);
            }
            stashCellPrefab = stashTemplate;
            stashCellPoolSize = Mathf.Max(20, stashLayout.poolSize);

            statsPage.SetActive(false);
            if (startHidden)
            {
                rootCanvas.gameObject.SetActive(false);
            }
        }

        private static GameObject CreatePanel(Transform canvasT, Vector2 panelSize)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(canvasT, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = panelSize;
            go.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.96f);
            return go;
        }

        private static GameObject CreateStretchRect(Transform parent, string name, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return go;
        }

        private static void StretchFull(RectTransform rt, float left, float right, float top, float bottom)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        private static Text CreateText(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
            var tx = go.AddComponent<Text>();
            tx.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tx.fontSize = fontSize;
            tx.fontStyle = fontStyle;
            tx.color = Color.white;
            tx.alignment = align;
            tx.horizontalOverflow = HorizontalWrapMode.Wrap;
            tx.verticalOverflow = VerticalWrapMode.Overflow;
            return tx;
        }

        private static (GameObject go, Button btn, Text glyph) CreateSlotButton(Transform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("SlotButton", typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.15f, 0.96f);
            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.32f, 0.31f, 0.29f, 0.96f);
            colors.pressedColor = new Color(0.4f, 0.37f, 0.32f, 0.96f);
            btn.colors = colors;
            btn.targetGraphic = img;
            var glyph = CreateText(go.transform, "Glyph", Vector2.zero, size, 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            glyph.color = new Color(0.95f, 0.89f, 0.78f, 1f);
            glyph.resizeTextForBestFit = false;
            glyph.horizontalOverflow = HorizontalWrapMode.Overflow;
            glyph.verticalOverflow = VerticalWrapMode.Overflow;
            var grt = glyph.rectTransform;
            grt.anchorMin = Vector2.zero;
            grt.anchorMax = Vector2.one;
            grt.offsetMin = Vector2.zero;
            grt.offsetMax = Vector2.zero;
            return (go, btn, glyph);
        }

        private static CampCharacterSheetStashCellUi CreateStashCellTemplate(Transform holder, Vector2 cellSize)
        {
            var root = new GameObject("_StashCellTemplate", typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = cellSize;
            var bg = root.GetComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.12f, 0.96f);
            var btn = root.GetComponent<Button>();
            btn.targetGraphic = bg;
            var glyph = CreateText(root.transform, "G", Vector2.zero, rt.sizeDelta, 21, FontStyle.Bold, TextAnchor.MiddleCenter);
            glyph.color = new Color(0.95f, 0.89f, 0.78f, 1f);
            glyph.resizeTextForBestFit = false;
            glyph.horizontalOverflow = HorizontalWrapMode.Overflow;
            glyph.verticalOverflow = VerticalWrapMode.Overflow;
            var gr = glyph.rectTransform;
            gr.anchorMin = Vector2.zero;
            gr.anchorMax = Vector2.one;
            gr.offsetMin = gr.offsetMax = Vector2.zero;
            var hoverTip = root.AddComponent<CampCharacterSheetHoverTip>();
            var cell = root.AddComponent<CampCharacterSheetStashCellUi>();
            cell.AssignRuntimeRefs(btn, glyph, bg, hoverTip);
            root.SetActive(false);
            root.transform.SetParent(holder, false);
            return cell;
        }

        private void EnsureDragGhostExists()
        {
            if (dragGhostRoot != null && dragGhostText != null)
            {
                return;
            }

            if (rootCanvas == null)
            {
                return;
            }

            var existing = rootCanvas.transform.Find("DragGhost") as RectTransform;
            if (existing != null)
            {
                dragGhostRoot = existing;
                dragGhostText = existing.Find("Text")?.GetComponent<Text>();
                if (dragGhostRoot != null)
                {
                    dragGhostRoot.gameObject.SetActive(false);
                }

                return;
            }

            var dragGo = new GameObject("DragGhost", typeof(RectTransform), typeof(Image));
            dragGo.transform.SetParent(rootCanvas.transform, false);
            var dragRt = dragGo.GetComponent<RectTransform>();
            dragRt.anchorMin = dragRt.anchorMax = new Vector2(0f, 1f);
            dragRt.pivot = new Vector2(0f, 1f);
            var sz = EffectiveLayoutAsset != null ? EffectiveLayoutAsset.stash.cellSize : new Vector2(72f, 72f);
            dragRt.sizeDelta = sz;
            var dragBg = dragGo.GetComponent<Image>();
            dragBg.color = new Color(0.14f, 0.14f, 0.14f, 0.86f);
            var dragTextGo = new GameObject("Text", typeof(RectTransform));
            dragTextGo.transform.SetParent(dragGo.transform, false);
            var dragTextRt = dragTextGo.GetComponent<RectTransform>();
            dragTextRt.anchorMin = Vector2.zero;
            dragTextRt.anchorMax = Vector2.one;
            dragTextRt.offsetMin = Vector2.zero;
            dragTextRt.offsetMax = Vector2.zero;
            var dragText = dragTextGo.AddComponent<Text>();
            dragText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            dragText.fontSize = 21;
            dragText.fontStyle = FontStyle.Bold;
            dragText.alignment = TextAnchor.MiddleCenter;
            dragText.color = new Color(0.95f, 0.89f, 0.78f, 1f);
            dragGhostRoot = dragRt;
            dragGhostText = dragText;
            dragGhostRoot.gameObject.SetActive(false);
        }

        private void PlaceNearCursor(RectTransform rt, Vector2 screenPosition, Vector2 offset)
        {
            if (rootCanvas == null || rt == null)
            {
                return;
            }

            var canvasRt = rootCanvas.transform as RectTransform;
            if (canvasRt == null)
            {
                return;
            }

            var cam = rootCanvas.renderMode == RenderMode.ScreenSpaceCamera ? rootCanvas.worldCamera : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screenPosition + offset, cam, out var local))
            {
                return;
            }

            var canvasRect = canvasRt.rect;
            var topLeftLocal = new Vector2(canvasRect.xMin, canvasRect.yMax);
            var anchored = local - topLeftLocal;
            var pr = rt.rect;
            var maxX = Mathf.Max(2f, canvasRect.width - pr.width - 2f);
            var minY = -Mathf.Max(2f, canvasRect.height - pr.height - 2f);
            anchored.x = Mathf.Clamp(anchored.x, 2f, maxX);
            anchored.y = Mathf.Clamp(anchored.y, minY, -2f);
            rt.anchoredPosition = anchored;
        }
    }
}
