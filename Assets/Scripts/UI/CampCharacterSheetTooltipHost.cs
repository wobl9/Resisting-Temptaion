using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ShatteredForge.UI
{
    /// <summary>
    /// Screen-space tooltip that follows the pointer while visible.
    /// </summary>
    public sealed class CampCharacterSheetTooltipHost : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private Text body;
        [SerializeField] private float maxWidth = 440f;
        [SerializeField] private float maxHeight = 320f;
        [SerializeField] private Vector2 screenOffset = new(14f, 20f);

        private Canvas _rootCanvas;
        private bool _visible;

        private void Awake()
        {
            TryCacheCanvas();
            if (panel != null)
            {
                panel.anchorMin = panel.anchorMax = new Vector2(0f, 1f);
                panel.pivot = new Vector2(0f, 1f);
            }

            HideImmediate();
        }

        /// <summary>Used by runtime UI bootstrap (fields are assigned after <see cref="Awake"/>).</summary>
        public void Bind(RectTransform panelTransform, Text bodyText)
        {
            panel = panelTransform;
            body = bodyText;
            TryCacheCanvas();
            if (panel != null)
            {
                panel.anchorMin = panel.anchorMax = new Vector2(0f, 1f);
                panel.pivot = new Vector2(0f, 1f);
            }

            HideImmediate();
        }

        public void ConfigureMaxSize(float width, float height)
        {
            maxWidth = Mathf.Max(120f, width);
            maxHeight = Mathf.Max(80f, height);
        }

        private void TryCacheCanvas()
        {
            if (_rootCanvas == null)
            {
                _rootCanvas = GetComponentInParent<Canvas>();
            }
        }

        private void LateUpdate()
        {
            if (!_visible || panel == null)
            {
                return;
            }

            var mouse = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : (Vector2)UnityEngine.Input.mousePosition;
            Reposition(mouse);
        }

        public void Show(string text, Vector2 screenPosition)
        {
            if (panel == null || body == null)
            {
                return;
            }

            body.text = string.IsNullOrEmpty(text) ? string.Empty : text;
            var w = maxWidth;
            body.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
            var preferred = body.preferredHeight + 14f;
            var h = Mathf.Clamp(Mathf.Max(preferred, 26f), 26f, maxHeight);
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);

            _visible = true;
            panel.gameObject.SetActive(true);
            Reposition(screenPosition);
        }

        public void Hide()
        {
            _visible = false;
            HideImmediate();
        }

        private void HideImmediate()
        {
            if (panel != null)
            {
                panel.gameObject.SetActive(false);
            }
        }

        private void Reposition(Vector2 screenPosition)
        {
            if (_rootCanvas == null)
            {
                return;
            }

            var canvasRt = _rootCanvas.transform as RectTransform;
            if (canvasRt == null)
            {
                return;
            }

            var cam = _rootCanvas.renderMode == RenderMode.ScreenSpaceCamera ? _rootCanvas.worldCamera : null;
            var sp = screenPosition + screenOffset;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, sp, cam, out var local))
            {
                return;
            }

            // Convert canvas-local point (center-origin) to top-left anchored coordinates.
            var canvasRect = canvasRt.rect;
            var topLeftLocal = new Vector2(canvasRect.xMin, canvasRect.yMax);
            var anchored = local - topLeftLocal;

            var pr = panel.rect;
            var maxX = Mathf.Max(4f, canvasRect.width - pr.width - 4f);
            var minY = -Mathf.Max(4f, canvasRect.height - pr.height - 4f);
            anchored.x = Mathf.Clamp(anchored.x, 4f, maxX);
            anchored.y = Mathf.Clamp(anchored.y, minY, -4f);
            panel.anchoredPosition = anchored;
        }
    }
}
