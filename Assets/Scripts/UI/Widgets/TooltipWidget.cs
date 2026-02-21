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
            transform.position = Input.mousePosition + new Vector3(0, -40, 0);
        }

        public void Show(string tooltip)
        {
            _text.text = tooltip;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
