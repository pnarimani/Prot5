using System;
using TMPro;
using UnityEngine;

namespace SiegeSurvival
{
    public class TooltipWidget : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;
        
        public static TooltipWidget Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            gameObject.SetActive(false);
        }

        void Update()
        {
            var rt = (RectTransform)transform;
            Vector2 size = rt.sizeDelta;
            Vector2 mouse = Input.mousePosition;
            const float offsetBelow = -40f;
            const float offsetAbove = 40f;

            float x = mouse.x;
            float y = mouse.y + offsetBelow;

            // Flip above cursor if tooltip goes below bottom of screen
            if (y - size.y < 0f)
                y = mouse.y + offsetAbove + size.y;

            // Clamp horizontally so tooltip stays within screen bounds
            x = Mathf.Clamp(x, 0f, Screen.width - size.x);

            // Clamp vertically so tooltip stays within screen bounds
            y = Mathf.Clamp(y, size.y, Screen.height);

            transform.position = new Vector3(x, y, 0f);
        }

        public void Show(string tooltip)
        {
            _text.text = tooltip;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            transform.RebuildAllLayoutsNextFrame();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
