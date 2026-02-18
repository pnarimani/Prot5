using FastSpring;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SiegeSurvival.UI.Widgets
{
    public class ActionWidget : MonoBehaviour
    {
        Button _button;
        TextMeshProUGUI _buttonText, _title, _desc, _cons;
        RectTransform _actionPanel;
        TransformSpring _panelHolder;

        bool _isOpen;

        void Awake()
        {
            _button = this.FindChildRecursive<Button>("#ActivationButton");
            _buttonText = this.FindChildRecursive<TextMeshProUGUI>("#Text");
            _title = this.FindChildRecursive<TextMeshProUGUI>("#Title");
            _desc = this.FindChildRecursive<TextMeshProUGUI>("#Desc");
            _cons = this.FindChildRecursive<TextMeshProUGUI>("#Cons");
            _actionPanel = this.FindChildRecursive<RectTransform>("#ChosenActionPanel");
            _panelHolder = this.FindChildRecursive<TransformSpring>("#ChosenActionPanelHolder");

            _panelHolder.SizeDelta.MoveInstantly(Vector2.zero);

            _button.onClick.AddListener(TogglePanel);
        }

        void TogglePanel()
        {
            if (_isOpen)
                Hide();
            else
                Show();
        }

        public void Show()
        {
            _panelHolder.SizeDelta.Target = _actionPanel.rect.size;
            _isOpen = true;
        }

        public void Hide()
        {
            _panelHolder.SizeDelta.Target = Vector2.zero;
            _isOpen = false;
        }
    }
}