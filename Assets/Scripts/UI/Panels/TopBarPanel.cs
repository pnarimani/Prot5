using SiegeSurvival.Core;
using SiegeSurvival.Data;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SiegeSurvival.UI.Panels
{
    /// <summary>
    /// Top bar: Day counter, resources with deltas, meters, siege intensity, population, profile.
    /// </summary>
    public class TopBarPanel : MonoBehaviour
    {
        [Header("Day")]
        public TextMeshProUGUI dayText;

        [Header("Resources")]
        public TextMeshProUGUI foodText;
        public TextMeshProUGUI waterText;
        public TextMeshProUGUI fuelText;
        public TextMeshProUGUI medicineText;
        public TextMeshProUGUI materialsText;

        [Header("Meters")]
        public Slider moraleSlider;
        public TextMeshProUGUI moraleText;
        public Image moraleFill;
        public Slider unrestSlider;
        public TextMeshProUGUI unrestText;
        public Image unrestFill;
        public Slider sicknessSlider;
        public TextMeshProUGUI sicknessText;
        public Image sicknessFill;

        [Header("Other")]
        public TextMeshProUGUI siegeText;
        public TextMeshProUGUI populationText;
        public TextMeshProUGUI profileText;
        public TextMeshProUGUI wellsText;

        private GameManager _gm;

        private void Start()
        {
            _gm = GameManager.Instance;
            _gm.OnStateChanged += Refresh;
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

            if (dayText) dayText.text = $"Day {s.currentDay} / 40";

            if (foodText) foodText.text = $"Food: {s.food}";
            if (waterText) waterText.text = $"Water: {s.water}";
            if (fuelText) fuelText.text = $"Fuel: {s.fuel}";
            if (medicineText) medicineText.text = $"Medicine: {s.medicine}";
            if (materialsText) materialsText.text = $"Materials: {s.materials}";

            // Show deltas if we have a last sim context
            if (_gm.LastSimContext != null)
            {
                var ctx = _gm.LastSimContext;
                if (foodText) foodText.text = $"Food: {s.food} ({FormatDelta(ctx.foodProduced - ctx.foodConsumed)})";
                if (waterText) waterText.text = $"Water: {s.water} ({FormatDelta(ctx.waterProduced - ctx.waterConsumed)})";
                if (fuelText) fuelText.text = $"Fuel: {s.fuel} ({FormatDelta(ctx.fuelProduced - ctx.fuelConsumed)})";
            }

            // Meters
            SetMeter(moraleSlider, moraleText, moraleFill, s.morale, "Morale",
                s.morale > 50 ? Color.green : s.morale > 30 ? Color.yellow : Color.red);
            SetMeter(unrestSlider, unrestText, unrestFill, s.unrest, "Unrest",
                s.unrest < 40 ? Color.green : s.unrest < 60 ? Color.yellow : Color.red);
            SetMeter(sicknessSlider, sicknessText, sicknessFill, s.sickness, "Sickness",
                s.sickness < 30 ? Color.green : s.sickness < 60 ? Color.yellow : Color.red);

            if (siegeText) siegeText.text = $"Siege: {s.siegeIntensity}/6";
            if (populationText) populationText.text = $"Pop: {s.TotalPopulation} (H:{s.healthyWorkers} G:{s.guards} S:{s.sick} E:{s.elderly})";
            if (profileText) profileText.text = $"Profile: {FormatProfile(s.activeProfile)}";
            if (wellsText)
            {
                wellsText.text = s.wellsDamaged ? "WELLS DAMAGED" : "";
                wellsText.color = Color.red;
            }
        }

        private void SetMeter(Slider slider, TextMeshProUGUI text, Image fill, int value, string label, Color color)
        {
            if (slider) { slider.maxValue = 100; slider.value = value; }
            if (text) text.text = $"{label}: {value}";
            if (fill) fill.color = color;
        }

        private string FormatDelta(int delta) => delta >= 0 ? $"+{delta}" : delta.ToString();

        private string FormatProfile(PressureProfileId id)
        {
            return id switch
            {
                PressureProfileId.P1_DiseaseWave => "Disease Wave",
                PressureProfileId.P2_SupplySpoilage => "Supply Spoilage",
                PressureProfileId.P3_SabotagedWells => "Sabotaged Wells",
                PressureProfileId.P4_HeavyBombardment => "Heavy Bombardment",
                _ => id.ToString()
            };
        }
    }
}
