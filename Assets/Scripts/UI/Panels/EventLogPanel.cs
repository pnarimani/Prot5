using System.Text;
using SiegeSurvival.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SiegeSurvival.UI.Panels
{
    /// <summary>
    /// Persistent event log showing all actions/events since game start.
    /// Separated by day headers and shown in a popup panel.
    /// </summary>
    public class EventLogPanel : MonoBehaviour
    {
        [Header("Layout")]
        public TextMeshProUGUI logText;
        public ScrollRect scrollRect;
        public bool showLatestEntryOnly;

        [Header("Popup (optional)")]
        public GameObject popupRoot;
        public Button closeButton;

        private GameManager _gm;

        private void Awake()
        {
            if (popupRoot == null)
                popupRoot = gameObject;
            if (logText == null)
                logText = this.FindChildRecursive<TextMeshProUGUI>("#LogText");
            if (scrollRect == null)
                scrollRect = GetComponentInChildren<ScrollRect>(true);
            if (closeButton == null)
                closeButton = this.FindChildRecursive<Button>("#CloseButton");
            if (closeButton != null)
                closeButton.onClick.AddListener(ClosePopup);
        }

        private void Start()
        {
            _gm = GameManager.Instance;
            if (_gm != null)
                _gm.OnDaySimulated += OnDaySimulated;
            RefreshDisplay();
        }

        private void OnDestroy()
        {
            if (_gm != null)
            {
                _gm.OnDaySimulated -= OnDaySimulated;
            }
            if (closeButton != null)
                closeButton.onClick.RemoveListener(ClosePopup);
        }

        private void OnDaySimulated(SimulationContext ctx)
        {
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            if (logText == null)
                return;

            var reports = _gm?.DayReports;
            if (reports == null || reports.Count == 0)
            {
                logText.text = "No reports yet.";
                return;
            }

            if (showLatestEntryOnly)
            {
                logText.text = reports[reports.Count - 1];
            }
            else
            {
                var sb = new StringBuilder();
                foreach (var report in reports)
                    sb.AppendLine(report);
                logText.text = sb.ToString();
            }

            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        public void ClosePopup()
        {
            Destroy(gameObject);
        }
    }
}
