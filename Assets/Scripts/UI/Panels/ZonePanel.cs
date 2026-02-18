using SiegeSurvival.Core;
using SiegeSurvival.Data.Runtime;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SiegeSurvival.UI.Panels
{
    /// <summary>
    /// Displays all 5 zones with integrity, capacity, population, overcrowding, status.
    /// </summary>
    public class ZonePanel : MonoBehaviour
    {
        [Header("Zone Rows (order 0-4)")]
        public ZoneRowUI[] zoneRows;

        [Header("Wells Repair")]
        public GameObject wellsRepairButton;
        public TextMeshProUGUI wellsRepairText;

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

            var perim = s.GetActivePerimeter();

            for (int i = 0; i < s.zones.Length && i < zoneRows.Length; i++)
            {
                var zone = s.zones[i];
                var row = zoneRows[i];
                if (row == null) continue;

                row.Refresh(zone, zone == perim);
            }

            // Wells repair
            if (wellsRepairButton)
            {
                wellsRepairButton.SetActive(s.wellsDamaged);
                var btn = wellsRepairButton.GetComponent<Button>();
                if (btn) btn.interactable = _gm.CanRepairWells();
            }
            if (wellsRepairText)
                wellsRepairText.text = $"Repair Wells (Cost: 10 Materials)";
        }

        public void OnRepairWellsClicked()
        {
            _gm?.RepairWells();
        }
    }

    [System.Serializable]
    public class ZoneRowUI
    {
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI statusText;
        public Slider integritySlider;
        public Image integrityFill;
        public TextMeshProUGUI integrityText;
        public TextMeshProUGUI populationText;
        public TextMeshProUGUI overcrowdingText;
        public TextMeshProUGUI bonusText;
        public Image background;

        public void Refresh(ZoneState zone, bool isPerimeter)
        {
            if (nameText) nameText.text = zone.definition.zoneName;

            // Status
            if (statusText)
            {
                if (zone.isLost)
                {
                    statusText.text = "LOST";
                    statusText.color = Color.red;
                }
                else if (isPerimeter)
                {
                    statusText.text = "ACTIVE PERIMETER";
                    statusText.color = new Color(0.2f, 0.6f, 1f);
                }
                else
                {
                    statusText.text = "Intact";
                    statusText.color = Color.green;
                }
            }

            // Integrity
            if (integritySlider)
            {
                integritySlider.maxValue = zone.definition.baseIntegrity;
                integritySlider.value = zone.isLost ? 0 : zone.currentIntegrity;
            }
            if (integrityText)
                integrityText.text = zone.isLost ? "0" : $"{zone.currentIntegrity}/{zone.definition.baseIntegrity}";
            if (integrityFill)
            {
                float pct = zone.definition.baseIntegrity > 0 ? zone.currentIntegrity / (float)zone.definition.baseIntegrity : 0;
                integrityFill.color = pct > 0.6f ? Color.green : pct > 0.3f ? Color.yellow : Color.red;
            }

            // Population
            if (populationText)
                populationText.text = $"{zone.currentPopulation}/{zone.effectiveCapacity}";

            // Overcrowding
            if (overcrowdingText)
            {
                if (zone.IsOvercrowded && !zone.isLost)
                {
                    overcrowdingText.text = $"Overcrowded: {zone.OvercrowdingPercent:F0}%";
                    overcrowdingText.color = Color.red;
                    overcrowdingText.gameObject.SetActive(true);
                }
                else
                {
                    overcrowdingText.gameObject.SetActive(false);
                }
            }

            // Bonuses
            if (bonusText)
            {
                string bonus = "";
                if (!zone.isLost)
                {
                    if (zone.definition.foodProductionModifier > 1f)
                        bonus += $"+{(zone.definition.foodProductionModifier - 1f) * 100:F0}% Food ";
                    if (zone.definition.materialsProductionModifier > 1f)
                        bonus += $"+{(zone.definition.materialsProductionModifier - 1f) * 100:F0}% Materials ";
                    if (zone.definition.moraleBonus > 0)
                        bonus += $"+{zone.definition.moraleBonus} Morale ";
                    if (zone.definition.unrestGrowthModifier < 1f)
                        bonus += $"-{(1f - zone.definition.unrestGrowthModifier) * 100:F0}% Unrest growth ";
                }
                bonusText.text = bonus;
            }

            // Background color
            if (background)
            {
                if (zone.isLost) background.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                else if (isPerimeter) background.color = new Color(0.15f, 0.25f, 0.5f, 0.8f);
                else background.color = new Color(0.15f, 0.35f, 0.15f, 0.5f);
            }
        }
    }
}
