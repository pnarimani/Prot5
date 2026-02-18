using System.Collections.Generic;
using SiegeSurvival.Core;
using SiegeSurvival.Data;
using UnityEngine;

namespace SiegeSurvival.UI.Panels
{
    public class MyWorkerAllocationPanel : MonoBehaviour
    {
        [SerializeField] WorkerAllocationWidget _widgetPrefab;
        [SerializeField] Transform _parent;

        GameManager _gm;

        readonly Dictionary<JobSlot, WorkerAllocationWidget> _widgets = new();

        void Start()
        {
            _gm = GameManager.Instance;
            _gm.OnStateChanged += Refresh;

            SetupRow(JobSlot.FoodProduction);
            SetupRow(JobSlot.WaterDrawing);
            SetupRow(JobSlot.MaterialsCrafting);
            SetupRow(JobSlot.Repairs);
            SetupRow(JobSlot.Sanitation);
            SetupRow(JobSlot.ClinicStaff);
            SetupRow(JobSlot.FuelScavenging);

            Refresh();
        }

        void OnDestroy()
        {
            if (_gm != null) _gm.OnStateChanged -= Refresh;
        }

        void SetupRow(JobSlot slot)
        {
            var row = Instantiate(_widgetPrefab, _parent, false);
            row.Init(slot);
            _widgets[slot] = row;
        }

        void Refresh()
        {
            var s = _gm.State;
            if (s == null) return;

            _widgets[JobSlot.FoodProduction].RefreshJobRow(s, "Food Production", GetFoodProjection(s));
            _widgets[JobSlot.WaterDrawing].RefreshJobRow(s, "Water Drawing", GetWaterProjection(s));
            _widgets[JobSlot.MaterialsCrafting].RefreshJobRow(s, "Materials Crafting", GetMaterialsProjection(s));
            _widgets[JobSlot.Repairs].RefreshJobRow(s, "Repairs", GetRepairsProjection(s));
            _widgets[JobSlot.Sanitation].RefreshJobRow(s, "Sanitation", GetSanitationProjection(s));
            _widgets[JobSlot.ClinicStaff].RefreshJobRow(s, "Clinic Staff", GetClinicProjection(s));
            _widgets[JobSlot.FuelScavenging].RefreshJobRow(s, "Fuel Scavenging", GetFuelProjection(s));
            
            transform.RebuildAllLayouts();
            transform.RebuildAllLayoutsNextFrame();

            // // Guard info
            // if (guardInfoText)
            // {
            //     var gUnits = s.guards / 5;
            //     guardInfoText.text = $"Guards on Duty: {s.guards} → Siege Dmg -{gUnits}, Unrest -{gUnits * 3}/day";
            // }
        }
        
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
}