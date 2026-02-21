using UnityEngine;
using UnityEngine.EventSystems;

namespace SiegeSurvival
{
    public class TooltipMaker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField, TextArea(7, 15)] private string _tooltip;

        public void SetTooltip(string tooltip)
        {
            _tooltip = tooltip;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (TooltipWidget.Instance == null || string.IsNullOrWhiteSpace(_tooltip))
                return;

            TooltipWidget.Instance.Show(_tooltip);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (TooltipWidget.Instance == null)
                return;

            TooltipWidget.Instance.Hide();
        }
    }
}
