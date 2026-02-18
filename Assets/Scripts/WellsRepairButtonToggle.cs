using UnityEngine;
using UnityEngine.UI;
using SiegeSurvival.Core;

namespace SiegeSurvival
{
    /// <summary>
    /// Shows/hides the Wells Repair button based on game state.
    /// </summary>
    public class WellsRepairButtonToggle : MonoBehaviour
    {
        private GameManager _gm;
        private Button _btn;

        private void Start()
        {
            _gm = GameManager.Instance;
            _btn = GetComponent<Button>();
            _gm.OnStateChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_gm != null) _gm.OnStateChanged -= Refresh;
        }

        private void Refresh()
        {
            bool show = _gm != null && _gm.State != null && _gm.State.wellsDamaged;
            gameObject.SetActive(show);
            if (_btn && show) _btn.interactable = _gm.CanRepairWells();
        }
    }
}
