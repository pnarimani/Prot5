using SiegeSurvival.Core;
using SiegeSurvival.Data.Runtime;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SiegeSurvival.UI.Panels
{
    /// <summary>
    /// Evacuation panel — only visible when eligible.
    /// </summary>
    public class EvacuationPanel : MonoBehaviour
    {
        public TextMeshProUGUI headerText;
        public TextMeshProUGUI costText;
        public TextMeshProUGUI penaltiesText;
        public Button evacuateButton;

        private GameManager _gm;

        private void Start()
        {
            _gm = GameManager.Instance;
            _gm.OnStateChanged += Refresh;
            if (evacuateButton) evacuateButton.onClick.AddListener(() => _gm.Evacuate());
            Refresh();
        }

        private void OnDestroy()
        {
            if (_gm != null) _gm.OnStateChanged -= Refresh;
        }

        private void Refresh()
        {
            var s = _gm.State;
            if (s == null) return;

            bool canEvac = _gm.CanEvacuate();
            if (!canEvac)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            var perim = s.GetActivePerimeter();

            if (headerText) headerText.text = $"Evacuate {perim.definition.zoneName}";
            if (costText) costText.text = "Cost: 20 Materials, +10 Sickness, +10 Unrest, -15 Morale";
            if (penaltiesText)
            {
                string pen = $"Zone penalties: Unrest +{perim.definition.onLossUnrest}, Sickness +{perim.definition.onLossSickness}";
                if (!string.IsNullOrEmpty(perim.definition.onLossProductionDesc))
                    pen += $"\n{perim.definition.onLossProductionDesc}";
                penaltiesText.text = pen;
            }
            if (evacuateButton) evacuateButton.interactable = s.materials >= 20;
        }
    }
}
