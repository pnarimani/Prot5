using UnityEngine;

namespace SiegeSurvival.Core
{
    /// <summary>
    /// Transient per-day context that accumulates modifiers across simulation steps.
    /// Created fresh each day before Step 1.
    /// </summary>
    [System.Serializable]
    public class SimulationContext
    {
        // --- Production multipliers ---
        public float foodProductionMult = 1f;
        public float waterProductionMult = 1f;
        public float materialsProductionMult = 1f;
        public float fuelProductionMult = 1f;
        public float repairOutputMult = 1f;
        public float allProductionMult = 1f; // applies to Food/Water/Materials/Fuel/Repairs

        // --- Consumption multipliers ---
        public float foodConsumptionMult = 1f;
        public float waterConsumptionMult = 1f;
        public float fuelConsumptionMult = 1f; // reserved

        // --- Flat consumption additions ---
        public int flatFoodConsumption; // L4 adds +15

        // --- Meter deltas (accumulated from laws/orders/etc.) ---
        public int sicknessDelta;
        public int unrestDelta;
        public int moraleDelta;

        // --- Deaths queued ---
        public int deathsSick;         // Deaths targeting Sick category
        public int deathsDefault;      // Deaths using default priority (Sick→Elderly→Healthy→Guards)
        public int deathsHealthyFirst; // Deaths using L6 priority (Healthy→Sick→Elderly→Guards)

        // --- Clinic/Medicine ---
        public float clinicMedicineCostMult = 1f; // L9 halves to 0.5

        // --- Siege ---
        public float siegeDamageMult = 1f; // L11 ×0.8

        // --- Caps (from Martial Law L12) ---
        public int? unrestCap;  // null means no cap
        public int? moraleCap;  // null means no cap

        // --- Production results (stored in Step 3, applied in Step 11) ---
        public int repairAmount;
        public int siegeDamageReduction; // from guards
        public float guardUnrestGrowthModifier = 1f; // 0.5f = 50% unrest growth
        public int sanitationReduction;
        public int clinicReduction;

        // --- Deficit flags (set in Step 4) ---
        public bool foodDeficit;
        public bool waterDeficit;
        public bool fuelDeficit;

        // --- Resource snapshots (start of day, for reporting) ---
        public int foodStart, waterStart, fuelStart, medicineStart, materialsStart;
        public int moraleStart, unrestStart, sicknessStart;
        public int foodProduced, waterProduced, materialsProduced, fuelProduced;
        public int foodConsumed, waterConsumed, fuelConsumed;

        // --- Keep breached flag ---
        public bool keepBreached;
    }
}
