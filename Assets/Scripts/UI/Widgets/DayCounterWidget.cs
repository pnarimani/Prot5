using TMPro;
using UnityEngine;
using SiegeSurvival.Core;

namespace SiegeSurvival.UI.Widgets
{
    public class DayCounterWidget : MonoBehaviour
    {
        TextMeshProUGUI _text;
        GameManager _gm;

        void Awake()
        {
            _text = GetComponentInChildren<TextMeshProUGUI>();
        }

        void Start()
        {
            _gm = GameManager.Instance;
            if (_gm != null)
            {
                _gm.OnStateChanged += Refresh;
                Refresh();
            }
        }

        void OnDestroy()
        {
            if (_gm != null)
                _gm.OnStateChanged -= Refresh;
        }

        void Refresh()
        {
            if (_text != null && _gm?.State != null)
                _text.text = $"Day {_gm.State.currentDay}";
        }
    }
}