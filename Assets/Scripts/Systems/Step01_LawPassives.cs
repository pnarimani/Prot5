using SiegeSurvival.Core;
using SiegeSurvival.Data;

namespace SiegeSurvival.Systems
{
    /// <summary>
    /// Step 1: Apply ongoing effects from all enacted laws to the SimulationContext.
    /// </summary>
    public static class Step01_LawPassives
    {
        public static void Execute(GameState state, SimulationContext ctx, CausalityLog log)
        {
            foreach (var lawId in state.enactedLaws)
            {
                switch (lawId)
                {
                    case LawId.L1_StrictRations:
                        ctx.foodConsumptionMult *= 0.75f;
                        ctx.unrestDelta += 5;
                        log.AddMult(CausalityCategory.Consumption, "Strict Rations (L1)", 0.75f,
                            "Food consumption ×0.75");
                        log.AddFlat(CausalityCategory.Unrest, "Strict Rations (L1)", 5,
                            "Unrest +5/day from rationing");
                        break;

                    case LawId.L2_DilutedWater:
                        ctx.waterConsumptionMult *= 0.8f;
                        ctx.sicknessDelta += 5;
                        log.AddMult(CausalityCategory.Consumption, "Diluted Water (L2)", 0.8f,
                            "Water consumption ×0.8");
                        log.AddFlat(CausalityCategory.Sickness, "Diluted Water (L2)", 5,
                            "Sickness +5/day from diluted water");
                        break;

                    case LawId.L3_ExtendedShifts:
                        ctx.foodProductionMult *= 1.25f;
                        ctx.waterProductionMult *= 1.25f;
                        ctx.materialsProductionMult *= 1.25f;
                        ctx.fuelProductionMult *= 1.25f;
                        ctx.sicknessDelta += 8;
                        log.AddMult(CausalityCategory.Production, "Extended Shifts (L3)", 1.25f,
                            "All production ×1.25");
                        log.AddFlat(CausalityCategory.Sickness, "Extended Shifts (L3)", 8,
                            "Sickness +8/day from overwork");
                        break;

                    case LawId.L4_MandatoryGuardService:
                        ctx.flatFoodConsumption += 15;
                        log.AddFlat(CausalityCategory.Consumption, "Mandatory Guard Service (L4)", 15,
                            "Food +15/day extra consumption");
                        break;

                    case LawId.L5_EmergencyShelters:
                        // Increase Inner District effective capacity
                        state.InnerDistrict.effectiveCapacity = state.InnerDistrict.definition.capacity + 30;
                        ctx.sicknessDelta += 10;
                        log.AddFlat(CausalityCategory.General, "Emergency Shelters (L5)", 30,
                            "Inner District capacity +30");
                        log.AddFlat(CausalityCategory.Sickness, "Emergency Shelters (L5)", 10,
                            "Sickness +10/day from overcrowded shelters");
                        break;

                    case LawId.L6_PublicExecutions:
                        // No ongoing effect
                        break;

                    case LawId.L7_FaithProcessions:
                        // No ongoing effect
                        break;

                    case LawId.L8_FoodConfiscation:
                        // No ongoing effect
                        break;

                    case LawId.L9_MedicalTriage:
                        ctx.clinicMedicineCostMult *= 0.5f;
                        // Kill 5 Sick/day (or all Sick if <5)
                        int sickToKill = System.Math.Min(5, state.sick);
                        ctx.deathsSick += sickToKill;
                        log.AddMult(CausalityCategory.Production, "Medical Triage (L9)", 0.5f,
                            "Clinic medicine cost ×0.5");
                        log.AddFlat(CausalityCategory.Death, "Medical Triage (L9)", -sickToKill,
                            $"{sickToKill} sick die from triage daily");
                        break;

                    case LawId.L10_Curfew:
                        ctx.unrestDelta -= 10;
                        ctx.allProductionMult *= 0.8f;
                        log.AddFlat(CausalityCategory.Unrest, "Curfew (L10)", -10,
                            "Unrest -10/day from curfew");
                        log.AddMult(CausalityCategory.Production, "Curfew (L10)", 0.8f,
                            "All production ×0.8 from curfew");
                        break;

                    case LawId.L11_AbandonOuterRing:
                        ctx.siegeDamageMult *= 0.8f;
                        log.AddMult(CausalityCategory.SiegeDamage, "Abandon Outer Ring (L11)", 0.8f,
                            "Siege damage ×0.8");
                        break;

                    case LawId.L12_MartialLaw:
                        ctx.unrestCap = 60;
                        ctx.moraleCap = 40;
                        log.AddFlat(CausalityCategory.Unrest, "Martial Law (L12)", 0,
                            "Unrest capped at 60");
                        log.AddFlat(CausalityCategory.Morale, "Martial Law (L12)", 0,
                            "Morale capped at 40");
                        break;
                }
            }
        }
    }
}
