using UnityEngine;
using SiegeSurvival.Core;
using SiegeSurvival.UI.Panels;
using SiegeSurvival.Data;
using UnityEngine.UI;

namespace SiegeSurvival.UI.Widgets
{
    public class NextDayButton : MonoBehaviour
    {
        [SerializeField] EventLogPanel _eventLogPanel;
        GameManager _gm;

        void Start()
        {
            _gm = GameManager.Instance;
            if (_gm != null)
            {
                _gm.OnDaySimulated += OnDaySimulated;
                _gm.OnPhaseChanged += OnPhaseChanged;
            }

            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        void OnDestroy()
        {
            if (_gm != null)
            {
                _gm.OnDaySimulated -= OnDaySimulated;
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

        private void OnDaySimulated(SimulationContext ctx)
        {
            // Day simulation just completed, wait for phase to change to ShowReport
        }

        private void OnPhaseChanged()
        {
            if (_gm?.Phase == GamePhase.ShowReport && _eventLogPanel != null)
            {
                Instantiate(_eventLogPanel);
            }
        }
    }
}
