using SiegeSurvival.Core;
using SiegeSurvival.Data;
using UnityEngine;

namespace SiegeSurvival.Systems
{
    /// <summary>
    /// Step 3: Calculate all production based on worker allocations and modifiers.
    /// </summary>
    public static class Step03_Production
    {
        public static void Execute(GameState state, SimulationContext ctx, CausalityLog log, RandomProvider rng)
        {
            // Pre-check: Fuel ≤ 0 penalty on food production
            if (state.fuel <= 0)
            {
                ctx.foodProductionMult *= 0.85f;
                log.AddMult(CausalityCategory.Production, "Fuel Deficit", 0.85f,
                    "Food production ×0.85 (no fuel)");
            }

            // === 3a. Food Production ===
            int foodUnits = state.workerAllocation[JobSlot.FoodProduction] / 5;
            if (foodUnits > 0)
            {
                float zoneMult = state.OuterFarms.isLost
                    ? state.OuterFarms.definition.foodProductionLostModifier
                    : state.OuterFarms.definition.foodProductionModifier;
                float moraleMult = state.morale < 40 ? 0.8f : 1f;
                float unrestMult = state.unrest > 60 ? 0.7f : 1f;

                float totalMult = ctx.foodProductionMult * ctx.allProductionMult * zoneMult * moraleMult * unrestMult;
                int produced = Mathf.FloorToInt(foodUnits * 10 * totalMult);
                state.food += produced;
                ctx.foodProduced = produced;

                log.AddFlat(CausalityCategory.Food, "Food Production", produced,
                    $"+{produced} Food ({foodUnits} units × 10 × {totalMult:F2})");
                if (zoneMult != 1f)
                    log.AddMult(CausalityCategory.Production, state.OuterFarms.isLost ? "Farms Lost" : "Farms Intact",
                        zoneMult, $"Food zone mult ×{zoneMult:F2}");
                if (moraleMult != 1f)
                    log.AddMult(CausalityCategory.Production, "Low Morale (<40)", moraleMult, "Food ×0.8 (low morale)");
                if (unrestMult != 1f)
                    log.AddMult(CausalityCategory.Production, "High Unrest (>60)", unrestMult, "Food ×0.7 (high unrest)");
            }

            // === 3b. Water Drawing ===
            int waterUnits = state.workerAllocation[JobSlot.WaterDrawing] / 5;
            if (waterUnits > 0)
            {
                float wellsMult = state.wellsDamaged ? 0.5f : 1f;
                float totalMult = ctx.waterProductionMult * ctx.allProductionMult * wellsMult;
                int produced = Mathf.FloorToInt(waterUnits * 12 * totalMult);
                state.water += produced;
                ctx.waterProduced = produced;

                log.AddFlat(CausalityCategory.Water, "Water Drawing", produced,
                    $"+{produced} Water ({waterUnits} units × 12 × {totalMult:F2})");
                if (wellsMult != 1f)
                    log.AddMult(CausalityCategory.Production, "Wells Damaged", wellsMult, "Water ×0.5 (damaged wells)");
            }

            // === 3c. Materials Crafting ===
            int matUnits = state.workerAllocation[JobSlot.MaterialsCrafting] / 5;
            if (matUnits > 0)
            {
                float zoneMult = state.ArtisanQuarter.isLost
                    ? state.ArtisanQuarter.definition.materialsProductionLostModifier
                    : state.ArtisanQuarter.definition.materialsProductionModifier;
                float totalMult = ctx.materialsProductionMult * ctx.allProductionMult * zoneMult;
                int produced = Mathf.FloorToInt(matUnits * 8 * totalMult);
                state.materials += produced;
                ctx.materialsProduced = produced;

                log.AddFlat(CausalityCategory.Materials, "Materials Crafting", produced,
                    $"+{produced} Materials ({matUnits} units × 8 × {totalMult:F2})");
            }

            // === 3d. Repairs ===
            int repairUnits = state.workerAllocation[JobSlot.Repairs] / 5;
            if (repairUnits > 0)
            {
                float totalMult = ctx.repairOutputMult * ctx.allProductionMult;
                int materialCost = repairUnits * 4;
                int repairAmount;

                if (state.materials < materialCost && state.materials > 0)
                {
                    // Scale proportionally
                    float ratio = state.materials / (float)materialCost;
                    repairAmount = Mathf.FloorToInt(repairUnits * 8 * totalMult * ratio);
                    materialCost = state.materials;
                }
                else if (state.materials <= 0)
                {
                    repairAmount = 0;
                    materialCost = 0;
                }
                else
                {
                    repairAmount = Mathf.FloorToInt(repairUnits * 8 * totalMult);
                }

                state.materials -= materialCost;
                ctx.repairAmount = repairAmount;

                log.AddFlat(CausalityCategory.Integrity, "Repairs", repairAmount,
                    $"Repair +{repairAmount} integrity (cost {materialCost} materials)");
            }

            // === 3e. Sanitation ===
            int sanUnits = state.workerAllocation[JobSlot.Sanitation] / 5;
            ctx.sanitationReduction = sanUnits;
            if (sanUnits > 0)
            {
                log.AddFlat(CausalityCategory.Sickness, "Sanitation", 0,
                    $"Sanitation capacity: {sanUnits} units");
            }

            // === 3f. Guard Duty (automatic) ===
            int guardUnits = state.guards / 5;
            ctx.siegeDamageReduction = guardUnits * 1;
            ctx.guardUnrestGrowthModifier = guardUnits > 0 ? 0.5f : 1f;

            if (guardUnits > 0)
            {
                log.AddFlat(CausalityCategory.SiegeDamage, "Guards", -ctx.siegeDamageReduction,
                    $"Guards reduce siege damage by {ctx.siegeDamageReduction}");
                log.AddFlat(CausalityCategory.Unrest, "Guards", 0,
                    $"Guards reduce unrest growth by 50%");
            }

            // === 3g. Clinic Staff ===
            int clinicUnits = state.workerAllocation[JobSlot.ClinicStaff] / 5;
            if (clinicUnits > 0)
            {
                int adjustedMedCost = Mathf.CeilToInt(5f * ctx.clinicMedicineCostMult); // Normal: 5, with L9: 3
                int totalMedCost = clinicUnits * adjustedMedCost;
                int effectiveUnits;

                if (state.medicine < totalMedCost && state.medicine > 0)
                {
                    effectiveUnits = state.medicine / adjustedMedCost;
                    state.medicine -= effectiveUnits * adjustedMedCost;
                }
                else if (state.medicine <= 0)
                {
                    effectiveUnits = 0;
                }
                else
                {
                    effectiveUnits = clinicUnits;
                    state.medicine -= totalMedCost;
                }

                ctx.clinicReduction = effectiveUnits;

                log.AddFlat(CausalityCategory.Sickness, "Clinic", 0,
                    $"Clinic capacity: {effectiveUnits} units (used {effectiveUnits * adjustedMedCost} medicine)");
            }

            // === 3h. Fuel Scavenging ===
            int fuelUnits = state.workerAllocation[JobSlot.FuelScavenging] / 5;
            if (fuelUnits > 0)
            {
                float zoneMult = state.OuterFarms.isLost
                    ? state.OuterFarms.definition.fuelScavengingLostModifier
                    : 1f;
                float totalMult = ctx.fuelProductionMult * ctx.allProductionMult * zoneMult;
                int produced = Mathf.FloorToInt(fuelUnits * 15 * totalMult);
                state.fuel += produced;
                ctx.fuelProduced = produced;

                log.AddFlat(CausalityCategory.Fuel, "Fuel Scavenging", produced,
                    $"+{produced} Fuel ({fuelUnits} units × 15 × {totalMult:F2})");

                // Death risk if siege intensity >= 4
                if (state.siegeIntensity >= 4 && rng.Chance(0.20f))
                {
                    ctx.deathsDefault += 2;
                    log.AddFlat(CausalityCategory.Death, "Fuel Scavenging Ambush", -2,
                        "2 deaths from fuel scavenging ambush (Siege ≥4)");
                }
            }
        }
    }
}
