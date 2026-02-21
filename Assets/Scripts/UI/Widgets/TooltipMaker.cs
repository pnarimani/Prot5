using UnityEngine;
using UnityEngine.EventSystems;

namespace SiegeSurvival
{
    public class TooltipMaker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField, TextArea] private string _tooltip;


        public void OnPointerEnter(PointerEventData eventData)
        {
            TooltipWidget.Instance.Show(_tooltip);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            TooltipWidget.Instance.Hide();
        }
    }
}