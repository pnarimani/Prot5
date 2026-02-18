using SiegeSurvival.Core;
using SiegeSurvival.Data;
using SiegeSurvival.Systems;
using UnityEngine;

namespace SiegeSurvival.Core
{
    /// <summary>
    /// Orchestrates all 13 simulation steps in exact order.
    /// Returns the SimulationContext for reporting.
    /// </summary>
    public static class DaySimulator
    {
        /// <summary>
        /// Run a full day simulation. Emergency order costs are deducted BEFORE calling this.
        /// </summary>
        public static SimulationContext SimulateDay(GameState state, CausalityLog log, RandomProvider rng)
        {
            var ctx = new SimulationContext();
            log.Clear();

            // Snapshot start-of-day values
            ctx.foodStart = state.food;
            ctx.waterStart = state.water;
            ctx.fuelStart = state.fuel;
            ctx.medicineStart = state.medicine;
            ctx.materialsStart = state.materials;
            ctx.moraleStart = state.morale;
            ctx.unrestStart = state.unrest;
            ctx.sicknessStart = state.sickness;

            log.AddFlat(CausalityCategory.General, "Day Start", state.currentDay,
                $"=== Day {state.currentDay} Simulation Start ===");

            // Step 1: Law Passives
            Step01_LawPassives.Execute(state, ctx, log);

            // Step 2: Emergency Order Effects
            Step02_EmergencyOrders.Execute(state, ctx, log);

            // Step 3: Production
            Step03_Production.Execute(state, ctx, log, rng);

            // Step 4: Consumption
            Step04_Consumption.Execute(state, ctx, log);

            // Step 5: Deficit Penalties
            Step05_DeficitPenalties.Execute(state, ctx, log);

            // Step 6: Overcrowding Penalties
            Step06_Overcrowding.Execute(state, ctx, log);

            // Step 7: Sickness Progression
            Step07_Sickness.Execute(state, ctx, log);

            // Step 8: Morale Progression
            Step08_Morale.Execute(state, ctx, log);

            // Step 9: Unrest Progression
            Step09_Unrest.Execute(state, ctx, log);

            // Step 10: Siege Damage
            Step10_SiegeDamage.Execute(state, ctx, log);

            // Step 11: Repairs
            Step11_Repairs.Execute(state, ctx, log);

            // Step 12: Events + Early Incidents + Mission Resolution
            Step12_Events.Execute(state, ctx, log, rng);

            // Apply queued deaths (default priority + L6 healthy-first)
            if (ctx.deathsDefault > 0)
                PopulationManager.ApplyDeathsDefault(state, ctx.deathsDefault, log, "Queued Deaths (default)");
            if (ctx.deathsHealthyFirst > 0)
                PopulationManager.ApplyDeathsHealthyFirst(state, ctx.deathsHealthyFirst, log, "Queued Deaths (L6)");

            // Recompute zone populations after deaths
            PopulationManager.RecomputeZonePopulationsAfterDeaths(state);

            // Step 13: Loss Conditions
            Step13_LossConditions.Execute(state, ctx, log);

            // Post-simulation bookkeeping
            if (!state.isGameOver)
            {
                state.currentDay++;
                state.daysSinceLastLaw++;
                state.daysSinceLastLawEnacted++;
                state.todayEmergencyOrder = null;
                state.quarantineZoneIndex = -1;

                // Victory check
                if (state.currentDay > 40)
                {
                    state.isVictory = true;
                    log.AddFlat(CausalityCategory.General, "VICTORY", 40,
                        "You survived 40 days! The siege has ended.");
                }
            }

            log.AddFlat(CausalityCategory.General, "Day End", state.currentDay - 1,
                $"=== Day {state.currentDay - 1} Simulation End ===");

            return ctx;
        }

        /// <summary>
        /// Deduct emergency order and mission costs BEFORE simulation.
        /// Call this before SimulateDay().
        /// </summary>
        public static void DeductPreSimulationCosts(GameState state, CausalityLog log)
        {
            // Emergency Order resource costs
            if (state.todayEmergencyOrder.HasValue)
            {
                switch (state.todayEmergencyOrder.Value)
                {
                    case EmergencyOrderId.O1_DivertSuppliesToRepairs:
                        state.food = Mathf.Max(0, state.food - 30);
                        state.water = Mathf.Max(0, state.water - 20);
                        log.AddFlat(CausalityCategory.EmergencyOrder, "O1 Cost", 0,
                            "Divert Supplies cost: Food -30, Water -20");
                        break;
                    case EmergencyOrderId.O2_SoupKitchens:
                        state.food = Mathf.Max(0, state.food - 40);
                        log.AddFlat(CausalityCategory.EmergencyOrder, "O2 Cost", 0,
                            "Soup Kitchens cost: Food -40");
                        break;
                    case EmergencyOrderId.O3_EmergencyWaterRation:
                        // No resource cost; effect applied in step
                        break;
                    case EmergencyOrderId.O4_CrackdownPatrols:
                        // Deaths + morale applied in step
                        break;
                    case EmergencyOrderId.O5_QuarantineDistrict:
                        // No resource cost
                        break;
                    case EmergencyOrderId.O6_InspireThePeople:
                        state.materials = Mathf.Max(0, state.materials - 15);
                        log.AddFlat(CausalityCategory.EmergencyOrder, "O6 Cost", 0,
                            "Inspire the People cost: Materials -15");
                        break;
                }
            }

            // Night Raid fuel cost
            if (state.activeMission != null && state.activeMission.missionId == MissionId.M2_NightRaid)
            {
                int fuelBefore = state.fuel;
                state.fuel = Mathf.Max(0, state.fuel - 40);
                log.AddFlat(CausalityCategory.Mission, "Night Raid Fuel Cost", -(fuelBefore - state.fuel),
                    $"Night Raid fuel cost: -{fuelBefore - state.fuel} Fuel");
            }
        }
    }
}
