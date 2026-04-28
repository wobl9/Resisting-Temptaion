using System.Collections.Generic;
using ShatteredForge.UI;
using UnityEditor;
using UnityEngine;

namespace ShatteredForge.EditorTools
{
    public sealed class CampCharacterSheetLayoutWindow : EditorWindow
    {
        private CampCharacterSheetLayoutAsset _layout;
        private CampCharacterSheetSkinAsset _skin;
        private DefaultAsset _spriteFolder;
        private Vector2 _scroll;
        private int _tab;
        private string _validationText = string.Empty;
        private bool _livePreview;

        [MenuItem("ShatteredForge/UI/Character Sheet Layout Window", priority = 40)]
        public static void Open()
        {
            var w = GetWindow<CampCharacterSheetLayoutWindow>("Character Sheet Layout");
            w.minSize = new Vector2(560f, 520f);
            w.Show();
        }

        private void OnEnable()
        {
            if (_layout == null)
            {
                _layout = CampCharacterSheetPrefabCreator.LoadActiveLayout();
            }

            if (_skin == null)
            {
                _skin = CampCharacterSheetPrefabCreator.LoadActiveSkin();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                var next = (CampCharacterSheetLayoutAsset)EditorGUILayout.ObjectField(
                    "Layout Asset",
                    _layout,
                    typeof(CampCharacterSheetLayoutAsset),
                    false);
                if (next != _layout)
                {
                    _layout = next;
                    CampCharacterSheetPrefabCreator.SetActiveLayout(_layout);
                }

                if (GUILayout.Button("Create", GUILayout.Width(80f)))
                {
                    CreateLayoutAsset();
                }
            }

            var nextSkin = (CampCharacterSheetSkinAsset)EditorGUILayout.ObjectField(
                "Skin Asset",
                _skin,
                typeof(CampCharacterSheetSkinAsset),
                false);
            if (nextSkin != _skin)
            {
                _skin = nextSkin;
                CampCharacterSheetPrefabCreator.SetActiveSkin(_skin);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create Default Diablo Skin", GUILayout.Width(220f)))
                {
                    CreateDefaultDiabloSkinAsset();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _spriteFolder = (DefaultAsset)EditorGUILayout.ObjectField(
                    "Sprite Folder",
                    _spriteFolder,
                    typeof(DefaultAsset),
                    false);
                if (GUILayout.Button("Auto-assign by filename", GUILayout.Width(180f)))
                {
                    AutoAssignSkinByFilename();
                }
            }

            if (_layout == null)
            {
                EditorGUILayout.HelpBox("Выбери или создай CampCharacterSheetLayoutAsset.", MessageType.Info);
                return;
            }

            _layout.EnsureSlotList();
            CampCharacterSheetPrefabCreator.SetActiveLayout(_layout);

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Load From View"))
                {
                    LoadFromSelectedView();
                }

                if (GUILayout.Button("Apply To View"))
                {
                    ApplyToSelectedView();
                }

                if (GUILayout.Button("Apply To Prefab"))
                {
                    CampCharacterSheetPrefabCreator.BakePrefab(_layout, _skin);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset Defaults"))
                {
                    ResetDefaults();
                }

                if (GUILayout.Button("Apply Diablo Preset"))
                {
                    ApplyDiabloPreset();
                }

                if (GUILayout.Button("Auto Layout Slots"))
                {
                    AutoLayoutSlots();
                }

                if (GUILayout.Button("Absolute -> Normalized"))
                {
                    ConvertAllToNormalized();
                }

                if (GUILayout.Button("Normalized -> Absolute"))
                {
                    ConvertAllToAbsolute();
                }

                if (GUILayout.Button("Validate"))
                {
                    _validationText = ValidateLayout(_layout);
                }

                if (GUILayout.Button("Bake Default"))
                {
                    CampCharacterSheetPrefabCreator.BakePrefabDefault();
                }
            }

            if (!string.IsNullOrEmpty(_validationText))
            {
                EditorGUILayout.HelpBox(_validationText, MessageType.None);
            }

            _livePreview = EditorGUILayout.ToggleLeft("Live Preview (auto Apply To View while editing)", _livePreview);
            EditorGUILayout.HelpBox(
                "Чтобы видеть результат сразу: выдели объект со `CampCharacterSheetView` в сцене и включи Live Preview.",
                MessageType.Info);

            _tab = GUILayout.Toolbar(_tab, new[] { "PaperDoll", "Stash", "Chrome" });
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            var so = new SerializedObject(_layout);
            so.Update();
            switch (_tab)
            {
                case 0:
                    DrawProperty(so, "paperDoll");
                    break;
                case 1:
                    DrawProperty(so, "stash");
                    break;
                default:
                    DrawProperty(so, "canvas");
                    DrawProperty(so, "chrome");
                    DrawProperty(so, "tooltip");
                    break;
            }

            so.ApplyModifiedProperties();
            EditorGUILayout.EndScrollView();

            if (_livePreview && GUI.changed)
            {
                TryApplyToSelectedView(showNotification: false);
            }
        }

        private static void DrawProperty(SerializedObject so, string property)
        {
            var p = so.FindProperty(property);
            if (p != null)
            {
                EditorGUILayout.PropertyField(p, true);
                EditorGUILayout.Space(8f);
            }
        }

        private void CreateLayoutAsset()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create Character Sheet Layout",
                "CampCharacterSheetLayout",
                "asset",
                "Choose location for layout asset");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var asset = CreateInstance<CampCharacterSheetLayoutAsset>();
            asset.EnsureSlotList();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            _layout = asset;
            CampCharacterSheetPrefabCreator.SetActiveLayout(_layout);
            EditorGUIUtility.PingObject(asset);
        }

        private void CreateDefaultDiabloSkinAsset()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create Default Diablo Skin",
                "CampCharacterSheetSkin_DiabloDefault",
                "asset",
                "Choose location for skin asset");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var skin = CreateInstance<CampCharacterSheetSkinAsset>();
            skin.panelTint = new Color(0.75f, 0.75f, 0.75f, 0.98f);
            skin.tooltipTint = new Color(0.12f, 0.12f, 0.12f, 0.98f);
            skin.torsoTint = new Color(0.22f, 0.22f, 0.22f, 1f);
            skin.slotTint = new Color(0.96f, 0.96f, 0.96f, 1f);
            skin.stashCellTint = new Color(0.94f, 0.94f, 0.94f, 1f);

            AssetDatabase.CreateAsset(skin, path);
            AssetDatabase.SaveAssets();
            _skin = skin;
            CampCharacterSheetPrefabCreator.SetActiveSkin(_skin);
            EditorGUIUtility.PingObject(skin);
            _validationText = "Default Diablo skin asset created. Assign sprites in this asset.";
        }

        private void AutoAssignSkinByFilename()
        {
            if (_skin == null)
            {
                _validationText = "Select Skin Asset first.";
                return;
            }

            var folderPath = "Assets";
            if (_spriteFolder != null)
            {
                folderPath = AssetDatabase.GetAssetPath(_spriteFolder);
                if (string.IsNullOrEmpty(folderPath))
                {
                    folderPath = "Assets";
                }
            }

            var guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
            if (guids == null || guids.Length == 0)
            {
                _validationText = $"No sprites found in folder: {folderPath}";
                return;
            }

            Undo.RecordObject(_skin, "Auto assign skin sprites");
            var found = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    continue;
                }

                var name = sprite.name.ToLowerInvariant();
                if (_skin.panelSprite == null && ContainsAny(name, "panel", "frame", "window", "inventory_bg", "backplate"))
                {
                    _skin.panelSprite = sprite;
                    found++;
                    continue;
                }

                if (_skin.slotSprite == null && ContainsAny(name, "slot", "equip_slot", "paperdoll_slot"))
                {
                    _skin.slotSprite = sprite;
                    found++;
                    continue;
                }

                if (_skin.stashCellSprite == null && ContainsAny(name, "stash", "grid", "cell", "inventory_cell"))
                {
                    _skin.stashCellSprite = sprite;
                    found++;
                    continue;
                }

                if (_skin.tooltipSprite == null && ContainsAny(name, "tooltip", "hint", "popup"))
                {
                    _skin.tooltipSprite = sprite;
                    found++;
                    continue;
                }

                if (_skin.torsoSprite == null && ContainsAny(name, "torso", "body", "paperdoll_body", "silhouette"))
                {
                    _skin.torsoSprite = sprite;
                    found++;
                }
            }

            EditorUtility.SetDirty(_skin);
            AssetDatabase.SaveAssets();
            _validationText = found > 0
                ? $"Auto-assign complete. Assigned {found} sprite field(s) from {folderPath}."
                : $"Auto-assign found no matching filenames in {folderPath}.";
            if (_livePreview)
            {
                TryApplyToSelectedView(showNotification: false);
            }
        }

        private static bool ContainsAny(string source, params string[] keys)
        {
            for (var i = 0; i < keys.Length; i++)
            {
                if (source.Contains(keys[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private void LoadFromSelectedView()
        {
            var view = ResolveSelectedView();
            if (view == null)
            {
                ShowNotification(new GUIContent("Выбери объект с CampCharacterSheetView."));
                return;
            }

            Undo.RecordObject(_layout, "Load character sheet layout from view");
            view.EditorCaptureCurrentLayout(_layout);
            EditorUtility.SetDirty(_layout);
            AssetDatabase.SaveAssets();
            _validationText = "Layout loaded from selected view.";
        }

        private void ApplyToSelectedView()
        {
            if (TryApplyToSelectedView(showNotification: true))
            {
                _validationText = "Layout applied to selected view.";
            }
        }

        private static CampCharacterSheetView ResolveSelectedView()
        {
            var go = Selection.activeGameObject;
            if (go == null)
            {
                return null;
            }

            var view = go.GetComponent<CampCharacterSheetView>();
            return view != null ? view : go.GetComponentInChildren<CampCharacterSheetView>(true);
        }

        private void ResetDefaults()
        {
            Undo.RecordObject(_layout, "Reset character sheet layout");
            _layout.canvas = new CampCharacterSheetCanvasLayout();
            _layout.chrome = new CampCharacterSheetChromeLayout();
            _layout.paperDoll = new CampCharacterSheetPaperDollLayout();
            _layout.stash = new CampCharacterSheetStashLayout();
            _layout.tooltip = new CampCharacterSheetTooltipLayout();
            _layout.EnsureSlotList();
            EditorUtility.SetDirty(_layout);
            AssetDatabase.SaveAssets();
            _validationText = "Layout reset to defaults.";
        }

        private void ApplyDiabloPreset()
        {
            if (_layout == null)
            {
                return;
            }

            Undo.RecordObject(_layout, "Apply Diablo-like preset");
            _layout.ApplyDiabloLikePreset();
            EditorUtility.SetDirty(_layout);
            AssetDatabase.SaveAssets();
            _validationText = "Diablo-like preset applied.";
            if (_livePreview)
            {
                TryApplyToSelectedView(showNotification: false);
            }
        }

        private void AutoLayoutSlots()
        {
            if (_layout == null)
            {
                return;
            }

            Undo.RecordObject(_layout, "Auto layout body slots");
            _layout.EnsureSlotList();
            var cols = 2;
            var rows = Mathf.CeilToInt(_layout.paperDoll.slots.Length / (float)cols);
            var leftPad = 12f;
            var topPad = 16f;
            var rightPad = 12f;
            var bottomPad = 16f;
            var cellW = Mathf.Max(56f, (_layout.paperDoll.sizeDelta.x - leftPad - rightPad - 12f) / cols);
            var cellH = Mathf.Max(56f, (_layout.paperDoll.sizeDelta.y - topPad - bottomPad - 12f) / rows);
            var slotSize = new Vector2(Mathf.Min(84f, cellW - 8f), Mathf.Min(84f, cellH - 8f));
            for (var i = 0; i < _layout.paperDoll.slots.Length; i++)
            {
                var col = i % cols;
                var row = i / cols;
                var x = leftPad + col * cellW;
                var y = -(topPad + row * cellH);
                _layout.paperDoll.slots[i].anchoredPosition = new Vector2(x, y);
                _layout.paperDoll.slots[i].sizeDelta = slotSize;
                var maxX = Mathf.Max(1f, _layout.paperDoll.sizeDelta.x - slotSize.x);
                var maxY = Mathf.Max(1f, _layout.paperDoll.sizeDelta.y - slotSize.y);
                _layout.paperDoll.slots[i].normalizedPosition = new Vector2(
                    Mathf.Clamp01(x / maxX),
                    Mathf.Clamp01((-y) / maxY));
                _layout.paperDoll.slots[i].useNormalizedPosition = true;
            }

            EditorUtility.SetDirty(_layout);
            _validationText = "Auto layout done for body slots.";
            if (_livePreview)
            {
                TryApplyToSelectedView(showNotification: false);
            }
        }

        private bool TryApplyToSelectedView(bool showNotification)
        {
            var view = ResolveSelectedView();
            if (view == null)
            {
                if (showNotification)
                {
                    ShowNotification(new GUIContent("Выбери объект с CampCharacterSheetView."));
                }

                return false;
            }

            view.EditorSetSkinAsset(_skin);
            view.EditorRebuildFromLayout(_layout, startHidden: false);
            EditorUtility.SetDirty(view.gameObject);
            return true;
        }

        private void ConvertAllToNormalized()
        {
            if (_layout == null)
            {
                return;
            }

            Undo.RecordObject(_layout, "Convert absolute to normalized");
            _layout.EnsureSlotList();
            var dollSize = _layout.paperDoll.sizeDelta;
            for (var i = 0; i < _layout.paperDoll.slots.Length; i++)
            {
                var slot = _layout.paperDoll.slots[i];
                var maxX = Mathf.Max(1f, dollSize.x - slot.sizeDelta.x);
                var maxY = Mathf.Max(1f, dollSize.y - slot.sizeDelta.y);
                slot.normalizedPosition = new Vector2(
                    Mathf.Clamp01(slot.anchoredPosition.x / maxX),
                    Mathf.Clamp01((-slot.anchoredPosition.y) / maxY));
                slot.useNormalizedPosition = true;
            }

            var torsoSize = _layout.paperDoll.torsoSize;
            var torsoMaxX = Mathf.Max(1f, dollSize.x - torsoSize.x);
            var torsoMaxY = Mathf.Max(1f, dollSize.y - torsoSize.y);
            _layout.paperDoll.torsoNormalizedPosition = new Vector2(
                Mathf.Clamp01(_layout.paperDoll.torsoAnchoredPosition.x / torsoMaxX),
                Mathf.Clamp01((-_layout.paperDoll.torsoAnchoredPosition.y) / torsoMaxY));
            _layout.paperDoll.useNormalizedTorsoPosition = true;

            EditorUtility.SetDirty(_layout);
            _validationText = "Converted all slot/torso positions to normalized mode.";
            if (_livePreview)
            {
                TryApplyToSelectedView(showNotification: false);
            }
        }

        private void ConvertAllToAbsolute()
        {
            if (_layout == null)
            {
                return;
            }

            Undo.RecordObject(_layout, "Convert normalized to absolute");
            _layout.EnsureSlotList();
            var dollSize = _layout.paperDoll.sizeDelta;
            for (var i = 0; i < _layout.paperDoll.slots.Length; i++)
            {
                var slot = _layout.paperDoll.slots[i];
                var maxX = Mathf.Max(1f, dollSize.x - slot.sizeDelta.x);
                var maxY = Mathf.Max(1f, dollSize.y - slot.sizeDelta.y);
                slot.anchoredPosition = new Vector2(
                    Mathf.Clamp01(slot.normalizedPosition.x) * maxX,
                    -Mathf.Clamp01(slot.normalizedPosition.y) * maxY);
                slot.useNormalizedPosition = false;
            }

            var torsoSize = _layout.paperDoll.torsoSize;
            var torsoMaxX = Mathf.Max(1f, dollSize.x - torsoSize.x);
            var torsoMaxY = Mathf.Max(1f, dollSize.y - torsoSize.y);
            _layout.paperDoll.torsoAnchoredPosition = new Vector2(
                Mathf.Clamp01(_layout.paperDoll.torsoNormalizedPosition.x) * torsoMaxX,
                -Mathf.Clamp01(_layout.paperDoll.torsoNormalizedPosition.y) * torsoMaxY);
            _layout.paperDoll.useNormalizedTorsoPosition = false;

            EditorUtility.SetDirty(_layout);
            _validationText = "Converted all slot/torso positions to absolute mode.";
            if (_livePreview)
            {
                TryApplyToSelectedView(showNotification: false);
            }
        }

        private static string ValidateLayout(CampCharacterSheetLayoutAsset asset)
        {
            if (asset == null)
            {
                return "Layout is null.";
            }

            asset.EnsureSlotList();
            var issues = new List<string>();

            var dollRect = new Rect(0f, 0f, asset.paperDoll.sizeDelta.x, asset.paperDoll.sizeDelta.y);
            for (var i = 0; i < asset.paperDoll.slots.Length; i++)
            {
                var a = asset.paperDoll.slots[i];
                var ra = new Rect(a.anchoredPosition.x, -a.anchoredPosition.y, a.sizeDelta.x, a.sizeDelta.y);
                if (!dollRect.Overlaps(ra))
                {
                    issues.Add($"slot {a.slot}: вне границ paper doll.");
                }

                for (var j = i + 1; j < asset.paperDoll.slots.Length; j++)
                {
                    var b = asset.paperDoll.slots[j];
                    var rb = new Rect(b.anchoredPosition.x, -b.anchoredPosition.y, b.sizeDelta.x, b.sizeDelta.y);
                    if (ra.Overlaps(rb))
                    {
                        issues.Add($"пересечение: {a.slot} и {b.slot}.");
                    }
                }
            }

            if (asset.stash.columns <= 0)
            {
                issues.Add("stash.columns должен быть > 0.");
            }

            if (asset.stash.cellSize.x <= 1f || asset.stash.cellSize.y <= 1f)
            {
                issues.Add("stash.cellSize слишком маленький.");
            }

            if (asset.stash.poolSize < asset.stash.columns * asset.stash.minRows)
            {
                issues.Add("stash.poolSize меньше columns * minRows.");
            }

            return issues.Count == 0
                ? "Validation OK: пересечений и критичных проблем не найдено."
                : "Validation issues:\n- " + string.Join("\n- ", issues);
        }
    }
}
