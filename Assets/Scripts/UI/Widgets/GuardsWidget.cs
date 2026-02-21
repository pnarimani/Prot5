using TMPro;
using UnityEngine;
using SiegeSurvival.Core;

namespace SiegeSurvival
{
    public class GuardsWidget : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _warning;
        [SerializeField] private TextMeshProUGUI _projection;

        GameManager _gm;

        void Start()
        {
            _gm = GameManager.Instance;
            if (_gm == null) return;

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
            var s = _gm?.State;
            if (s == null) return;

            int guardUnits = s.guards / 5;
            int siegeReduction = guardUnits * 1;
            int unrestReduction = guardUnits * 3;

            if (_projection != null)
                _projection.text = $"{s.guards} guards — Siege Dmg -{siegeReduction}, Unrest -{unrestReduction}/day";

            string warn = string.Empty;
            if (s.guards < 5)
                warn = "No active guard units!";
            else if (s.guards < 10)
                warn = "Guard strength low";

            if (_warning != null)
            {
                _warning.text = warn;
                _warning.gameObject.SetActive(!string.IsNullOrEmpty(warn));
            }
        }
    }
}