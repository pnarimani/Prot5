using SiegeSurvival.Data;
using SiegeSurvival.Data.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SiegeSurvival.UI.Widgets
{
    public class ZoneWidget : MonoBehaviour
    {
        const int EVAC_THRESHOLD = 40;
        [SerializeField] Color _safe = Color.darkGreen, _danger = Color.darkRed, _lost = Color.black;

        TextMeshProUGUI _population;
        TextMeshProUGUI _safety;
        TextMeshProUGUI _name, _production;
        Image _bg;

        void Awake()
        {
            _population = this.FindChildRecursive<TextMeshProUGUI>("#Population");
            _safety = this.FindChildRecursive<TextMeshProUGUI>("#Safety");
            _name = this.FindChildRecursive<TextMeshProUGUI>("#Name");
            _production = this.FindChildRecursive<TextMeshProUGUI>("#Production");
            _bg = this.FindChildRecursive<Image>("#BG");
        }

        public void Init(ZoneDefinition zone)
        {
            if (_name != null)
                _name.text = zone != null ? zone.zoneName : "Zone";
        }

        public void Refresh(ZoneState zone, bool isPerimeter, int projectedProduction)
        {
            if (zone == null) return;

            Init(zone.definition);

            if (_population != null)
                _population.text = $"Population {zone.currentPopulation}/{zone.effectiveCapacity}";

            if (_production != null)
                _production.text = $"Production +{Mathf.Max(0, projectedProduction)}";

            var lowIntegrity = zone.currentIntegrity < EVAC_THRESHOLD;

            string safetyText;
            var safetyColor = zone.isLost ? _lost : Color.Lerp(_safe, _danger, 1f - zone.currentIntegrity / 100f);

            if (zone.isLost)
                safetyText = "LOST";
            else if (isPerimeter)
                safetyText = $"PERIMETER ({zone.currentIntegrity})";
            else if (lowIntegrity)
                safetyText = $"DANGER ({zone.currentIntegrity})";
            else
                safetyText = $"SAFE ({zone.currentIntegrity})";

            if (_safety != null)
                _safety.text = $"Safety: {safetyText}";

            if (_bg != null)
                _bg.color = safetyColor;
        }
    }
}