using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SiegeSurvival.UI.Widgets
{
    /// <summary>
    /// Reusable meter bar widget for Morale, Unrest, Sickness.
    /// </summary>
    public class MeterBar : MonoBehaviour
    {
        public TextMeshProUGUI labelText;
        public TextMeshProUGUI valueText;
        public Image fillImage;

        [Header("Thresholds")]
        public float yellowThreshold = 0.4f;
        public float redThreshold = 0.6f;
        public bool invertColors; // true for Morale (red when low)

        [Header("Colors")]
        public Color greenColor = new Color(0.2f, 0.8f, 0.2f);
        public Color yellowColor = new Color(0.9f, 0.9f, 0.1f);
        public Color redColor = new Color(0.9f, 0.2f, 0.2f);

        public void SetValue(string label, int value, int max = 100)
        {
            if (labelText) labelText.text = label;
            if (valueText) valueText.text = $"{value}/{max}";

            float normalizedValue = Mathf.Clamp01(value / (float)max);
            if (fillImage)
            {
                fillImage.fillAmount = normalizedValue;
                fillImage.color = GetColor(normalizedValue);
            }
        }

        private Color GetColor(float normalized)
        {
            if (invertColors)
            {
                // Morale: red when low, green when high
                if (normalized < yellowThreshold) return redColor;
                if (normalized < redThreshold) return yellowColor;
                return greenColor;
            }
            else
            {
                // Unrest/Sickness: green when low, red when high
                if (normalized < yellowThreshold) return greenColor;
                if (normalized < redThreshold) return yellowColor;
                return redColor;
            }
        }
    }
}
