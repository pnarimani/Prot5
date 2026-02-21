using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SiegeSurvival
{
    public class DayReportPopup : MonoBehaviour
    {
        TextMeshProUGUI _title;
        TextMeshProUGUI _description;

        /// <summary>Fired when the player closes this popup.</summary>
        public event Action OnClosed;

        void Awake()
        {
            var closeButton = this.FindChildRecursive<Button>("#Close");
            _title = this.FindChildRecursive<TextMeshProUGUI>("#Title");
            _description = this.FindChildRecursive<TextMeshProUGUI>("#Description");
            closeButton.onClick.AddListener(Close);
        }

        /// <summary>Populates the popup with a title and description.</summary>
        public void SetContent(string title, string description)
        {
            if (_title != null) _title.text = title;
            if (_description != null) _description.text = description;
        }

        void Close()
        {
            OnClosed?.Invoke();
            Destroy(gameObject);
        }
    }
}
