using UnityEngine;
using UnityEngine.UI;

namespace ShatteredForge.UI
{
    /// <summary>
    /// One stash grid cell: index is set by <see cref="CampCharacterSheetView"/> on refresh.
    /// </summary>
    public sealed class CampCharacterSheetStashCellUi : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Text glyph;
        [SerializeField] private Graphic background;
        [SerializeField] private CampCharacterSheetHoverTip hover;

        /// <summary>Runtime bootstrap assigns refs (prefab is built in code).</summary>
        public void AssignRuntimeRefs(Button b, Text g, Graphic bg, CampCharacterSheetHoverTip h)
        {
            button = b;
            glyph = g;
            background = bg;
            hover = h;
        }

        private int _index = -1;
        private bool _used;
        private System.Action<int> _onClicked;
        public int CurrentIndex => _index;
        public bool IsUsed => _used;

        private void EnsureRefs()
        {
            if (background == null)
            {
                background = GetComponent<Graphic>();
                if (background == null)
                {
                    background = gameObject.AddComponent<Image>();
                }
            }

            if (button == null)
            {
                button = GetComponent<Button>();
                if (button == null)
                {
                    button = gameObject.AddComponent<Button>();
                }

                button.targetGraphic = background;
            }

            if (glyph == null)
            {
                glyph = GetComponentInChildren<Text>(true);
                if (glyph == null)
                {
                    var go = new GameObject("G", typeof(RectTransform));
                    go.transform.SetParent(transform, false);
                    var rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    glyph = go.AddComponent<Text>();
                    glyph.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    glyph.fontSize = 20;
                    glyph.fontStyle = FontStyle.Bold;
                    glyph.alignment = TextAnchor.MiddleCenter;
                    glyph.color = new Color(0.95f, 0.89f, 0.78f, 1f);
                }
            }

            if (hover == null)
            {
                hover = GetComponent<CampCharacterSheetHoverTip>();
                if (hover == null)
                {
                    hover = gameObject.AddComponent<CampCharacterSheetHoverTip>();
                }
            }
        }

        public void Wire(System.Action<int> onClicked, CampCharacterSheetTooltipHost tooltipHost)
        {
            EnsureRefs();
            _onClicked = onClicked;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    if (_index >= 0)
                    {
                        _onClicked?.Invoke(_index);
                    }
                });
            }

            if (hover != null && tooltipHost != null)
            {
                hover.Configure(tooltipHost, string.Empty);
            }
        }

        public void Apply(
            int index,
            bool used,
            bool selected,
            string glyphText,
            string tip,
            CampCharacterSheetTooltipHost tooltipHost)
        {
            EnsureRefs();
            _index = index;
            _used = used;
            gameObject.SetActive(true);

            if (button != null)
            {
                button.interactable = used;
            }

            if (glyph != null)
            {
                glyph.gameObject.SetActive(used);
                glyph.text = glyphText ?? string.Empty;
            }

            if (background != null)
            {
                if (used)
                {
                    background.color = selected
                        ? new Color(0.45f, 0.62f, 0.95f, 1f)
                        : new Color(0.19f, 0.2f, 0.24f, 0.94f);
                }
                else
                {
                    background.color = new Color(0.22f, 0.22f, 0.26f, 1f);
                }
            }

            if (hover != null && tooltipHost != null)
            {
                hover.Configure(tooltipHost, used ? tip : string.Empty);
            }
        }

        public bool ContainsScreenPoint(Vector2 screenPosition, Camera eventCamera)
        {
            var rt = transform as RectTransform;
            return rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, screenPosition, eventCamera);
        }
    }
}
