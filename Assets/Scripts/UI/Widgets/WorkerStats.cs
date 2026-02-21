using SiegeSurvival.Core;
using TMPro;
using UnityEngine;

namespace SiegeSurvival
{
    public class WorkerStats : MonoBehaviour
    {
        TextMeshProUGUI _freeText;
        TextMeshProUGUI _sickText;
        TextMeshProUGUI _awayText;
        GameManager _gm;

        void Awake()
        {
            _freeText = this.FindChildRecursive<TextMeshProUGUI>("#Free");
            _sickText = this.FindChildRecursive<TextMeshProUGUI>("#Sick");
            _awayText = this.FindChildRecursive<TextMeshProUGUI>("#Away");
        }

        void Start()
        {
            _gm = GameManager.Instance;
            if (_gm == null)
                return;

            _gm.OnStateChanged += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (_gm != null)
                _gm.OnStateChanged -= Refresh;
        }

        void Refresh()
        {
            var state = _gm?.State;
            if (state == null)
                return;

            int awayWorkers = state.activeMission?.workersCommitted ?? 0;

            if (_freeText != null)
                _freeText.text = $"Free: {state.IdleWorkers}";
            if (_sickText != null)
                _sickText.text = $"Sick: {state.sick}";
            if (_awayText != null)
                _awayText.text = $"Away: {awayWorkers}";
        }
    }
}
