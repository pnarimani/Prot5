using SiegeSurvival.Core;
using UnityEngine;

namespace SiegeSurvival.Systems
{
    /// <summary>
    /// Step 9: Apply unrest progression. Base +1 conditions, idle workers, Inner District bonus.
    /// </summary>
    public static class Step09_Unrest
    {
        public static void Execute(GameState state, SimulationContext ctx, CausalityLog log)
        {
            int unrestDelta = ctx.unrestDelta;

            // Base +1 conditions (each adds +1, they stack)
            if (ctx.foodDeficit)
            {
                unrestDelta += 1;
                log.AddFlat(CausalityCategory.Unrest, "Food Deficit", 1, "Unrest +1 (food deficit)");
            }
            if (ctx.waterDeficit)
            {
                unrestDelta += 1;
                log.AddFlat(CausalityCategory.Unrest, "Water Deficit", 1, "Unrest +1 (water deficit)");
            }
            if (ctx.fuelDeficit)
            {
                unrestDelta += 1;
                log.AddFlat(CausalityCategory.Unrest, "Fuel Deficit", 1, "Unrest +1 (fuel deficit)");
            }
            if (state.AnyZoneOvercrowded)
            {
                unrestDelta += 1;
                log.AddFlat(CausalityCategory.Unrest, "Overcrowding", 1, "Unrest +1 (overcrowding present)");
            }
            if (state.morale < 50)
            {
                unrestDelta += 1;
                log.AddFlat(CausalityCategory.Unrest, "Low Morale (<50)", 1, "Unrest +1 (morale < 50)");
            }
            if (state.daysSinceLastLawEnacted > 3)
            {
                unrestDelta += 1;
                log.AddFlat(CausalityCategory.Unrest, "No Law in 3+ Days", 1, "Unrest +1 (no law enacted recently)");
            }

            // Idle worker penalty
            float idlePercent = state.IdlePercent;
            if (idlePercent > 20f)
            {
                unrestDelta += 5;
                log.AddFlat(CausalityCategory.Unrest, "Idle Workers (>20%)", 5,
                    $"Unrest +5 (idle workers {idlePercent:F0}%)");
            }
            else if (idlePercent > 10f)
            {
                unrestDelta += 2;
                log.AddFlat(CausalityCategory.Unrest, "Idle Workers (>10%)", 2,
                    $"Unrest +2 (idle workers {idlePercent:F0}%)");
            }

            // Inner District intact: -10% positive unrest growth
            if (!state.InnerDistrict.isLost && unrestDelta > 0)
            {
                int before = unrestDelta;
                unrestDelta = Mathf.FloorToInt(unrestDelta * state.InnerDistrict.definition.unrestGrowthModifier);
                int reduction = before - unrestDelta;
                if (reduction > 0)
                {
                    log.AddFlat(CausalityCategory.Unrest, "Inner District Intact (-10%)", -reduction,
                        $"Unrest reduced by {reduction} (Inner District -10% growth)");
                }
            }

            int oldUnrest = state.unrest;
            state.unrest = Mathf.Clamp(state.unrest + unrestDelta, 0, 100);

            // L12 Martial Law cap
            if (ctx.unrestCap.HasValue && state.unrest > ctx.unrestCap.Value)
            {
                state.unrest = ctx.unrestCap.Value;
            }

            log.AddFlat(CausalityCategory.Unrest, "Net Unrest Change", state.unrest - oldUnrest,
                $"Unrest: {oldUnrest} → {state.unrest}");
        }
    }
}
