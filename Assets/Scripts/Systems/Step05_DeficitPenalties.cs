using SiegeSurvival.Core;

namespace SiegeSurvival.Systems
{
    /// <summary>
    /// Step 5: Apply deficit penalties (Food/Water/Fuel = 0).
    /// </summary>
    public static class Step05_DeficitPenalties
    {
        public static void Execute(GameState state, SimulationContext ctx, CausalityLog log)
        {
            // Food deficit
            if (ctx.foodDeficit)
            {
                ctx.moraleDelta -= 5;
                state.consecutiveFoodDeficitDays++;
                log.AddFlat(CausalityCategory.Morale, "Food Deficit", -5, "Morale -5 (food deficit)");
            }
            else
            {
                state.consecutiveFoodDeficitDays = 0;
            }

            // Water deficit
            if (ctx.waterDeficit)
            {
                ctx.moraleDelta -= 5;
                log.AddFlat(CausalityCategory.Morale, "Water Deficit", -5, "Morale -5 (water deficit)");
            }

            // Fuel deficit
            if (ctx.fuelDeficit)
            {
                ctx.sicknessDelta += 10;
                ctx.moraleDelta -= 10;
                ctx.unrestDelta += 5;
                log.AddFlat(CausalityCategory.Sickness, "Fuel Deficit", 10, "Sickness +10 (no fuel)");
                log.AddFlat(CausalityCategory.Morale, "Fuel Deficit", -10, "Morale -10 (no fuel)");
                log.AddFlat(CausalityCategory.Unrest, "Fuel Deficit", 5, "Unrest +5 (no fuel)");
            }

            // Consecutive Food+Water = 0 tracking
            if (ctx.foodDeficit && ctx.waterDeficit)
            {
                state.consecutiveFoodWaterZeroDays++;
            }
            else
            {
                state.consecutiveFoodWaterZeroDays = 0;
            }
        }
    }
}
