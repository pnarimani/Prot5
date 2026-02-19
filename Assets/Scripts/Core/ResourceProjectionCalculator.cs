using SiegeSurvival.Data;
using UnityEngine;

namespace SiegeSurvival.Core
{
    /// <summary>
    /// Calculates resource projections and net changes based on game state.
    /// </summary>
    public static class ResourceProjectionCalculator
    {
        #region Production Calculations

        public static int GetFoodProduction(GameState s)
        {
            int units = s.workerAllocation[JobSlot.FoodProduction] / 5;
            float zoneMult = s.OuterFarms.isLost ? s.OuterFarms.definition.foodProductionLostModifier : s.OuterFarms.definition.foodProductionModifier;
            float moraleMult = s.morale < 40 ? 0.8f : 1f;
            float unrestMult = s.unrest > 60 ? 0.7f : 1f;
            float fuelMult = s.fuel <= 0 ? 0.85f : 1f;
            float lawMult = GetLawProductionMult(s);
            return Mathf.FloorToInt(units * 10 * zoneMult * moraleMult * unrestMult * fuelMult * lawMult);
        }

        public static int GetWaterProduction(GameState s)
        {
            int units = s.workerAllocation[JobSlot.WaterDrawing] / 5;
            float wellsMult = s.wellsDamaged ? 0.5f : 1f;
            float lawMult = GetLawProductionMult(s);
            return Mathf.FloorToInt(units * 12 * wellsMult * lawMult);
        }

        public static int GetMaterialsProduction(GameState s)
        {
            int units = s.workerAllocation[JobSlot.MaterialsCrafting] / 5;
            float zoneMult = s.ArtisanQuarter.isLost ? s.ArtisanQuarter.definition.materialsProductionLostModifier : s.ArtisanQuarter.definition.materialsProductionModifier;
            float lawMult = GetLawProductionMult(s);
            return Mathf.FloorToInt(units * 8 * zoneMult * lawMult);
        }

        public static int GetFuelProduction(GameState s)
        {
            int units = s.workerAllocation[JobSlot.FuelScavenging] / 5;
            float zoneMult = s.OuterFarms.isLost ? s.OuterFarms.definition.fuelScavengingLostModifier : 1f;
            float lawMult = GetLawProductionMult(s);
            return Mathf.FloorToInt(units * 15 * zoneMult * lawMult);
        }

        public static int GetRepairsProduction(GameState s)
        {
            int units = s.workerAllocation[JobSlot.Repairs] / 5;
            float lawMult = GetLawAllProdMult(s);
            return Mathf.FloorToInt(units * 8 * lawMult);
        }

        public static int GetRepairsCost(GameState s)
        {
            int units = s.workerAllocation[JobSlot.Repairs] / 5;
            return units * 4;
        }

        public static int GetSanitationEffect(GameState s)
        {
            int units = s.workerAllocation[JobSlot.Sanitation] / 5;
            return units * 5;
        }

        public static int GetClinicEffect(GameState s)
        {
            int units = s.workerAllocation[JobSlot.ClinicStaff] / 5;
            return units * 8;
        }

        public static int GetClinicMedicineCost(GameState s)
        {
            int units = s.workerAllocation[JobSlot.ClinicStaff] / 5;
            float medCostMult = s.enactedLaws.Contains(LawId.L09_MedicalTriage) ? 0.5f : 1f;
            return units * Mathf.CeilToInt(5 * medCostMult);
        }

        #endregion

        #region Consumption Calculations

        public static int GetFoodConsumption(GameState s)
        {
            int totalPop = s.TotalPopulation;
            float consumption = totalPop * s.profileFoodConsumptionMult;
            if (s.water <= 0) consumption *= 1.5f;
            return Mathf.FloorToInt(consumption);
        }

        public static int GetWaterConsumption(GameState s)
        {
            return s.TotalPopulation;
        }

        public static int GetFuelConsumption(GameState s)
        {
            return s.workerAllocation[JobSlot.FoodProduction] > 0 ? 10 : 0;
        }

        #endregion

        #region Net Change Calculations

        public static int GetFoodNetChange(GameState s) => GetFoodProduction(s) - GetFoodConsumption(s);
        public static int GetWaterNetChange(GameState s) => GetWaterProduction(s) - GetWaterConsumption(s);
        public static int GetFuelNetChange(GameState s) => GetFuelProduction(s) - GetFuelConsumption(s);
        public static int GetMedicineNetChange(GameState s) => 0 - GetClinicMedicineCost(s);
        public static int GetMaterialsNetChange(GameState s) => GetMaterialsProduction(s) - GetRepairsCost(s);

        #endregion

        #region Formatted Projections

        public static string GetFoodProjectionFormatted(GameState s)
        {
            return $"+{GetFoodProduction(s)} Food/day";
        }

        public static string GetWaterProjectionFormatted(GameState s)
        {
            return $"+{GetWaterProduction(s)} Water/day";
        }

        public static string GetMaterialsProjectionFormatted(GameState s)
        {
            return $"+{GetMaterialsProduction(s)} Materials/day";
        }

        public static string GetRepairsProjectionFormatted(GameState s)
        {
            int repair = GetRepairsProduction(s);
            int cost = GetRepairsCost(s);
            return $"+{repair} Integrity, Cost: {cost} Mat/day";
        }

        public static string GetSanitationProjectionFormatted(GameState s)
        {
            return $"-{GetSanitationEffect(s)} Sickness/day";
        }

        public static string GetClinicProjectionFormatted(GameState s)
        {
            int effect = GetClinicEffect(s);
            int cost = GetClinicMedicineCost(s);
            return $"-{effect} Sickness/day, Cost: {cost} Med/day";
        }

        public static string GetFuelProjectionFormatted(GameState s)
        {
            int prod = GetFuelProduction(s);
            string risk = s.siegeIntensity >= 4 ? " [20% death risk]" : "";
            return $"+{prod} Fuel/day{risk}";
        }

        public static string FormatNetChange(int value)
        {
            if (value > 0) return $"+{value}";
            if (value < 0) return value.ToString();
            return "—";
        }

        #endregion

        #region Law Multipliers

        public static float GetLawProductionMult(GameState s)
        {
            float mult = 1f;
            if (s.enactedLaws.Contains(LawId.L03_ExtendedShifts)) mult *= 1.25f;
            mult *= GetLawAllProdMult(s);
            return mult;
        }

        public static float GetLawAllProdMult(GameState s)
        {
            float mult = 1f;
            if (s.enactedLaws.Contains(LawId.L10_Curfew)) mult *= 0.8f;
            if (s.todayEmergencyOrder == EmergencyOrderId.O5_QuarantineDistrict) mult *= 0.5f;
            return mult;
        }

        #endregion
    }
}
