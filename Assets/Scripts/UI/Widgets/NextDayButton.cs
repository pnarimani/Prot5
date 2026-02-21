using System.Collections.Generic;
using UnityEngine;
using SiegeSurvival;
using SiegeSurvival.Core;
using SiegeSurvival.Data;
using UnityEngine.UI;

namespace SiegeSurvival.UI.Widgets
{
    public class NextDayButton : MonoBehaviour
    {
        const string DayReportPopupPrefabPath = "Prefabs/DayReportPopup";

        [SerializeField] DayReportPopup _dayReportPopupPrefab;
        GameManager _gm;

        // Queue of entries to show one by one
        readonly Queue<DayReportEntry> _pendingReports = new();

        void Start()
        {
            _gm = GameManager.Instance;
            if (_gm != null)
            {
                _gm.OnPhaseChanged += OnPhaseChanged;
            }

            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        void OnDestroy()
        {
            if (_gm != null)
            {
                _gm.OnPhaseChanged -= OnPhaseChanged;
            }
        }

        void OnClick()
        {
            if (_gm?.Phase == GamePhase.PlayerTurn)
            {
                _gm.EndDay();
            }
        }

        void OnPhaseChanged()
        {
            if (_gm?.Phase != GamePhase.ShowReport)
                return;

            // Load the pending report entries into the queue
            _pendingReports.Clear();
            foreach (var entry in _gm.LastDayReportEntries)
                _pendingReports.Enqueue(entry);

            // Show the first popup — each popup calls ShowNextReport when closed
            ShowNextReport();
        }

        void ShowNextReport()
        {
            if (_pendingReports.Count == 0)
            {
                // All reports shown — continue the game
                _gm.ContinueFromReport();
                return;
            }

            var entry = _pendingReports.Dequeue();

            var prefab = _dayReportPopupPrefab != null
                ? _dayReportPopupPrefab
                : Resources.Load<DayReportPopup>(DayReportPopupPrefabPath);

            if (prefab == null)
            {
                // Fallback: if prefab not found, skip all and continue
                _pendingReports.Clear();
                _gm.ContinueFromReport();
                return;
            }

            var popup = Object.Instantiate(prefab, transform.root);

            // Stretch to fill the screen
            if (popup.TryGetComponent<RectTransform>(out var rt))
                StretchToParent(rt);

            popup.SetContent(entry.Title, entry.Description);
            popup.OnClosed += ShowNextReport;
        }

        static void StretchToParent(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
