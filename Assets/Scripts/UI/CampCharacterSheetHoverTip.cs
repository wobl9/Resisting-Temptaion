using UnityEngine;
using UnityEngine.EventSystems;

namespace ShatteredForge.UI
{
    /// <summary>
    /// Attach to the same GameObject as a raycast target (e.g. Button Image) to show a hover tooltip.
    /// </summary>
    public sealed class CampCharacterSheetHoverTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private CampCharacterSheetTooltipHost _host;
        private string _tip = string.Empty;

        public void Configure(CampCharacterSheetTooltipHost host, string tip)
        {
            _host = host;
            _tip = tip ?? string.Empty;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_host == null || string.IsNullOrEmpty(_tip))
            {
                return;
            }

            _host.Show(_tip, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _host?.Hide();
        }
    }
}
