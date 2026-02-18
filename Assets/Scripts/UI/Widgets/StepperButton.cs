using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SiegeSurvival.UI.Widgets
{
    /// <summary>
    /// Reusable ±5 stepper button widget for worker allocation.
    /// </summary>
    public class StepperButton : MonoBehaviour
    {
        public Button minusButton;
        public Button plusButton;
        public TextMeshProUGUI valueText;
        public int stepSize = 5;

        private int _value;
        private Func<int, bool> _canChange;
        private Action<int> _onChange;

        public int Value => _value;

        public void Initialize(int startValue, Func<int, bool> canChange, Action<int> onChange)
        {
            _value = startValue;
            _canChange = canChange;
            _onChange = onChange;

            if (minusButton)
            {
                minusButton.onClick.RemoveAllListeners();
                minusButton.onClick.AddListener(OnMinus);
            }
            if (plusButton)
            {
                plusButton.onClick.RemoveAllListeners();
                plusButton.onClick.AddListener(OnPlus);
            }

            UpdateDisplay();
        }

        public void SetValue(int value)
        {
            _value = value;
            UpdateDisplay();
        }

        private void OnMinus()
        {
            if (_canChange != null && _canChange(-stepSize))
            {
                _onChange?.Invoke(-stepSize);
                UpdateDisplay();
            }
        }

        private void OnPlus()
        {
            if (_canChange != null && _canChange(stepSize))
            {
                _onChange?.Invoke(stepSize);
                UpdateDisplay();
            }
        }

        private void UpdateDisplay()
        {
            if (valueText) valueText.text = _value.ToString();
            if (minusButton) minusButton.interactable = _canChange != null && _canChange(-stepSize);
            if (plusButton) plusButton.interactable = _canChange != null && _canChange(stepSize);
        }
    }
}
