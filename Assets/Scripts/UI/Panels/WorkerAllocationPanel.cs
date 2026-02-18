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
            RefreshJobRow(foodRow, JobSlot.FoodProduction, s, "Food Production", ResourceProjectionCalculator.GetFoodProjectionFormatted(s));
            RefreshJobRow(waterRow, JobSlot.WaterDrawing, s, "Water Drawing", ResourceProjectionCalculator.GetWaterProjectionFormatted(s));
            RefreshJobRow(materialsRow, JobSlot.MaterialsCrafting, s, "Materials Crafting", ResourceProjectionCalculator.GetMaterialsProjectionFormatted(s));
            RefreshJobRow(repairsRow, JobSlot.Repairs, s, "Repairs", ResourceProjectionCalculator.GetRepairsProjectionFormatted(s));
            RefreshJobRow(sanitationRow, JobSlot.Sanitation, s, "Sanitation", ResourceProjectionCalculator.GetSanitationProjectionFormatted(s));
            RefreshJobRow(clinicRow, JobSlot.ClinicStaff, s, "Clinic Staff", ResourceProjectionCalculator.GetClinicProjectionFormatted(s));
            RefreshJobRow(fuelRow, JobSlot.FuelScavenging, s, "Fuel Scavenging", ResourceProjectionCalculator.GetFuelProjectionFormatted(s));

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
