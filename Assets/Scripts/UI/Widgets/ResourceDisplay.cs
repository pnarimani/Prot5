using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SiegeSurvival.UI.Widgets
{
    /// <summary>
    /// Reusable resource display widget showing value + projected delta.
    /// </summary>
    public class ResourceDisplay : MonoBehaviour
    {
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI valueText;
        public TextMeshProUGUI deltaText;
        public Image iconImage;

        public void SetValue(string resourceName, int value, int delta)
        {
            if (nameText) nameText.text = resourceName;
            if (valueText) valueText.text = value.ToString();
            if (deltaText)
            {
                if (delta > 0)
                {
                    deltaText.text = $"(+{delta})";
                    deltaText.color = Color.green;
                }
                else if (delta < 0)
                {
                    deltaText.text = $"({delta})";
                    deltaText.color = Color.red;
                }
                else
                {
                    deltaText.text = "(0)";
                    deltaText.color = Color.white;
                }
            }
        }
    }
}
