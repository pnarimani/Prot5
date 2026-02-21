using UnityEngine;
using SiegeSurvival.Core;
using SiegeSurvival.UI.Panels;
using SiegeSurvival.Data;
using UnityEngine.UI;

namespace SiegeSurvival.UI.Widgets
{
    public class NextDayButton : MonoBehaviour
    {
        const string EventLogPrefabResourcePath = "UI/EventLogPopup";

        [SerializeField] EventLogPanel _eventLogPanel;
        GameManager _gm;

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

        private void OnPhaseChanged()
        {
            if (_gm?.Phase != GamePhase.ShowReport)
                return;

            var prefab = _eventLogPanel != null
                ? _eventLogPanel
                : Resources.Load<EventLogPanel>(EventLogPrefabResourcePath);

            if (prefab != null)
            {
                var reportPanel = Instantiate(prefab, transform.root);
                reportPanel.showLatestEntryOnly = true;

                var popupRoot = reportPanel.popupRoot != null ? reportPanel.popupRoot : reportPanel.gameObject;
                if (popupRoot.TryGetComponent<RectTransform>(out var rt))
                    StretchToParent(rt);
            }

            _gm.ContinueFromReport();
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
