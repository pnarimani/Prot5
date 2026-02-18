using SiegeSurvival.Core;
using SiegeSurvival.Data;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SiegeSurvival.UI.Panels
{
    /// <summary>
    /// Worker allocation panel with stepper buttons for 7 allocatable job slots + guard info.
    /// </summary>
    public class WorkerAllocationPanel : MonoBehaviour
    {
        [Header("Header")]
        public TextMeshProUGUI headerText;
        public TextMeshProUGUI idleWarningText;

        [Header("Job Rows")]
        public WorkerJobRowUI foodRow;
        public WorkerJobRowUI waterRow;
        public WorkerJobRowUI materialsRow;
        public WorkerJobRowUI repairsRow;
        public WorkerJobRowUI sanitationRow;
        public WorkerJobRowUI clinicRow;
        public WorkerJobRowUI fuelRow;

        [Header("Guard Info")]
        public TextMeshProUGUI guardInfoText;

        private GameManager _gm;

        private void Start()
        {
            _gm = GameManager.Instance;
            _gm.OnStateChanged += Refresh;

            SetupRow(foodRow, JobSlot.FoodProduction);
            SetupRow(waterRow, JobSlot.WaterDrawing);
            SetupRow(materialsRow, JobSlot.MaterialsCrafting);
            SetupRow(repairsRow, JobSlot.Repairs);
            SetupRow(sanitationRow, JobSlot.Sanitation);
            SetupRow(clinicRow, JobSlot.ClinicStaff);
            SetupRow(fuelRow, JobSlot.FuelScavenging);

            Refresh();
        }

        private void OnDestroy()
        {
            if (_gm != null) _gm.OnStateChanged -= Refresh;
        }

        private void SetupRow(WorkerJobRowUI row, JobSlot slot)
        {
            if (row == null) return;
            if (row.plusButton) row.plusButton.onClick.AddListener(() => _gm.AllocateWorkers(slot, 5));
            if (row.minusButton) row.minusButton.onClick.AddListener(() => _gm.AllocateWorkers(slot, -5));
        }

        private void Refresh()
        {
            var s = _gm.State;
            if (s == null) return;

            string missionLock = s.activeMission != null ? " (10 on mission)" : "";
            if (headerText) headerText.text = $"Workers Available: {s.IdleWorkers}/{s.AvailableWorkers}{missionLock}";

            // Idle warning
            if (idleWarningText)
            {
                float idle = s.IdlePercent;
                if (idle > 20f)
                {
                    idleWarningText.text = $"WARNING: {idle:F0}% idle → Unrest +5/day";
                    idleWarningText.color = Color.red;
                    idleWarningText.gameObject.SetActive(true);
                }
                else if (idle > 10f)
                {
                    idleWarningText.text = $"Caution: {idle:F0}% idle → Unrest +2/day";
                    idleWarningText.color = Color.yellow;
                    idleWarningText.gameObject.SetActive(true);
                }
                else
                {
                    idleWarningText.gameObject.SetActive(false);
                }
            }

            // Job rows
            RefreshJobRow(foodRow, JobSlot.FoodProduction, s, "Food Production", GetFoodProjection(s));
            RefreshJobRow(waterRow, JobSlot.WaterDrawing, s, "Water Drawing", GetWaterProjection(s));
            RefreshJobRow(materialsRow, JobSlot.MaterialsCrafting, s, "Materials Crafting", GetMaterialsProjection(s));
            RefreshJobRow(repairsRow, JobSlot.Repairs, s, "Repairs", GetRepairsProjection(s));
            RefreshJobRow(sanitationRow, JobSlot.Sanitation, s, "Sanitation", GetSanitationProjection(s));
            RefreshJobRow(clinicRow, JobSlot.ClinicStaff, s, "Clinic Staff", GetClinicProjection(s));
            RefreshJobRow(fuelRow, JobSlot.FuelScavenging, s, "Fuel Scavenging", GetFuelProjection(s));

            // Guard info
            if (guardInfoText)
            {
                int gUnits = s.guards / 5;
                guardInfoText.text = $"Guards on Duty: {s.guards} → Siege Dmg -{gUnits}, Unrest -{gUnits * 3}/day";
            }
        }

        private void RefreshJobRow(WorkerJobRowUI row, JobSlot slot, GameState s, string label, string projection)
        {
            if (row == null) return;
            int assigned = s.workerAllocation[slot];
            if (row.labelText) row.labelText.text = label;
            if (row.assignedText) row.assignedText.text = $"Assigned: {assigned}";
            if (row.projectionText) row.projectionText.text = projection;
            if (row.plusButton) row.plusButton.interactable = _gm.CanAllocateWorkers(slot, 5);
            if (row.minusButton) row.minusButton.interactable = _gm.CanAllocateWorkers(slot, -5);

            // Warnings
            if (row.warningText)
            {
                string warn = "";
                if (slot == JobSlot.Repairs && s.materials <= 0 && assigned > 0)
                    warn = "No materials!";
                else if (slot == JobSlot.ClinicStaff && s.medicine <= 0 && assigned > 0)
                    warn = "No medicine!";
                row.warningText.text = warn;
                row.warningText.gameObject.SetActive(!string.IsNullOrEmpty(warn));
            }
        }

        // === Projection Calculations ===

        private string GetFoodProjection(GameState s)
        {
            int units = s.workerAllocation[JobSlot.FoodProduction] / 5;
            float zoneMult = s.OuterFarms.isLost ? s.OuterFarms.definition.foodProductionLostModifier : s.OuterFarms.definition.foodProductionModifier;
            float moraleMult = s.morale < 40 ? 0.8f : 1f;
            float unrestMult = s.unrest > 60 ? 0.7f : 1f;
            float fuelMult = s.fuel <= 0 ? 0.85f : 1f;
            float lawMult = GetLawProductionMult(s);
            int prod = Mathf.FloorToInt(units * 10 * zoneMult * moraleMult * unrestMult * fuelMult * lawMult);
            return $"+{prod} Food/day";
        }

        private string GetWaterProjection(GameState s)
        {
            int units = s.workerAllocation[JobSlot.WaterDrawing] / 5;
            float wellsMult = s.wellsDamaged ? 0.5f : 1f;
            float lawMult = GetLawProductionMult(s);
            int prod = Mathf.FloorToInt(units * 12 * wellsMult * lawMult);
            return $"+{prod} Water/day";
        }

        private string GetMaterialsProjection(GameState s)
        {
            int units = s.workerAllocation[JobSlot.MaterialsCrafting] / 5;
            float zoneMult = s.ArtisanQuarter.isLost ? s.ArtisanQuarter.definition.materialsProductionLostModifier : s.ArtisanQuarter.definition.materialsProductionModifier;
            float lawMult = GetLawProductionMult(s);
            int prod = Mathf.FloorToInt(units * 8 * zoneMult * lawMult);
            return $"+{prod} Materials/day";
        }

        private string GetRepairsProjection(GameState s)
        {
            int units = s.workerAllocation[JobSlot.Repairs] / 5;
            float lawMult = GetLawAllProdMult(s);
            int repair = Mathf.FloorToInt(units * 8 * lawMult);
            int cost = units * 4;
            return $"+{repair} Integrity, Cost: {cost} Mat/day";
        }

        private string GetSanitationProjection(GameState s)
        {
            int units = s.workerAllocation[JobSlot.Sanitation] / 5;
            return $"-{units * 5} Sickness/day";
        }

        private string GetClinicProjection(GameState s)
        {
            int units = s.workerAllocation[JobSlot.ClinicStaff] / 5;
            float medCostMult = s.enactedLaws.Contains(LawId.L9_MedicalTriage) ? 0.5f : 1f;
            int medPerUnit = Mathf.CeilToInt(5 * medCostMult);
            return $"-{units * 8} Sickness/day, Cost: {units * medPerUnit} Med/day";
        }

        private string GetFuelProjection(GameState s)
        {
            int units = s.workerAllocation[JobSlot.FuelScavenging] / 5;
            float zoneMult = s.OuterFarms.isLost ? s.OuterFarms.definition.fuelScavengingLostModifier : 1f;
            float lawMult = GetLawProductionMult(s);
            int prod = Mathf.FloorToInt(units * 15 * zoneMult * lawMult);
            string risk = s.siegeIntensity >= 4 ? " [20% death risk]" : "";
            return $"+{prod} Fuel/day{risk}";
        }

        private float GetLawProductionMult(GameState s)
        {
            float mult = 1f;
            if (s.enactedLaws.Contains(LawId.L3_ExtendedShifts)) mult *= 1.25f;
            mult *= GetLawAllProdMult(s);
            return mult;
        }

        private float GetLawAllProdMult(GameState s)
        {
            float mult = 1f;
            if (s.enactedLaws.Contains(LawId.L10_Curfew)) mult *= 0.8f;
            if (s.todayEmergencyOrder == EmergencyOrderId.O5_QuarantineDistrict) mult *= 0.5f;
            return mult;
        }
    }

    [System.Serializable]
    public class WorkerJobRowUI
    {
        public TextMeshProUGUI labelText;
        public TextMeshProUGUI assignedText;
        public TextMeshProUGUI projectionText;
        public TextMeshProUGUI warningText;
        public Button plusButton;
        public Button minusButton;
    }
}
