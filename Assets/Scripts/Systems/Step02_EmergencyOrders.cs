using SiegeSurvival.Core;
using SiegeSurvival.Data;

namespace SiegeSurvival.Systems
{
    /// <summary>
    /// Step 2: Apply 1-day emergency order effects to the SimulationContext.
    /// </summary>
    public static class Step02_EmergencyOrders
    {
        public static void Execute(GameState state, SimulationContext ctx, CausalityLog log)
        {
            if (state.todayEmergencyOrder == null) return;

            switch (state.todayEmergencyOrder.Value)
            {
                case EmergencyOrderId.O1_DivertSuppliesToRepairs:
                    ctx.repairOutputMult *= 1.5f;
                    log.AddMult(CausalityCategory.Production, "Divert Supplies (O1)", 1.5f,
                        "Repair output ×1.5 today");
                    // Also fix wells
                    if (state.wellsDamaged)
                    {
                        state.wellsDamaged = false;
                        log.AddFlat(CausalityCategory.General, "Divert Supplies (O1)", 0,
                            "Wells repaired as part of O1");
                    }
                    break;

                case EmergencyOrderId.O2_SoupKitchens:
                    ctx.unrestDelta -= 15;
                    log.AddFlat(CausalityCategory.Unrest, "Soup Kitchens (O2)", -15,
                        "Unrest -15 today");
                    break;

                case EmergencyOrderId.O3_EmergencyWaterRation:
                    ctx.waterConsumptionMult *= 0.5f;
                    ctx.sicknessDelta += 10;
                    log.AddMult(CausalityCategory.Consumption, "Emergency Water Ration (O3)", 0.5f,
                        "Water consumption ×0.5 today");
                    log.AddFlat(CausalityCategory.Sickness, "Emergency Water Ration (O3)", 10,
                        "Sickness +10 today");
                    break;

                case EmergencyOrderId.O4_CrackdownPatrols:
                    ctx.unrestDelta -= 20;
                    ctx.deathsDefault += 2;
                    ctx.moraleDelta -= 10;
                    log.AddFlat(CausalityCategory.Unrest, "Crackdown Patrols (O4)", -20,
                        "Unrest -20 today");
                    log.AddFlat(CausalityCategory.Death, "Crackdown Patrols (O4)", -2,
                        "2 deaths from crackdown");
                    log.AddFlat(CausalityCategory.Morale, "Crackdown Patrols (O4)", -10,
                        "Morale -10 from crackdown");
                    break;

                case EmergencyOrderId.O5_QuarantineDistrict:
                    // -50% all production globally, -10 sickness
                    ctx.allProductionMult *= 0.5f;
                    ctx.sicknessDelta -= 10;
                    log.AddMult(CausalityCategory.Production, "Quarantine District (O5)", 0.5f,
                        "All production ×0.5 today (quarantine)");
                    log.AddFlat(CausalityCategory.Sickness, "Quarantine District (O5)", -10,
                        "Sickness -10 today");
                    break;

                case EmergencyOrderId.O6_InspireThePeople:
                    ctx.moraleDelta += 15;
                    log.AddFlat(CausalityCategory.Morale, "Inspire the People (O6)", 15,
                        "Morale +15 today");
                    break;
            }
        }
    }
}
