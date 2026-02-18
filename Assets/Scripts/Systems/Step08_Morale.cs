using SiegeSurvival.Core;
using UnityEngine;

namespace SiegeSurvival.Systems
{
    /// <summary>
    /// Step 8: Apply morale progression. Conditional drift, Keep bonus, recovery.
    /// </summary>
    public static class Step08_Morale
    {
        public static void Execute(GameState state, SimulationContext ctx, CausalityLog log)
        {
            int moraleDelta = ctx.moraleDelta;

            // Sickness > 60
            if (state.sickness > 60)
            {
                moraleDelta -= 3;
                log.AddFlat(CausalityCategory.Morale, "High Sickness (>60)", -3, "Morale -3 (sickness > 60)");
            }

            // Overcrowding present (any zone)
            if (state.AnyZoneOvercrowded)
            {
                moraleDelta -= 2;
                log.AddFlat(CausalityCategory.Morale, "Overcrowding", -2, "Morale -2 (overcrowding present)");
            }

            // Keep intact bonus
            if (!state.Keep.isLost)
            {
                moraleDelta += state.Keep.definition.moraleBonus; // +10
                log.AddFlat(CausalityCategory.Morale, "Keep Intact", state.Keep.definition.moraleBonus,
                    $"Morale +{state.Keep.definition.moraleBonus} (Keep intact)");
            }

            // Recovery check
            bool noDeficits = state.food > 0 && state.water > 0 && state.fuel > 0;
            bool noOvercrowding = !state.AnyZoneOvercrowded;
            if (noDeficits && noOvercrowding && state.sickness < 30 && state.unrest < 40)
            {
                moraleDelta += 2;
                log.AddFlat(CausalityCategory.Morale, "Recovery", 2,
                    "Morale +2 (no deficits, no overcrowding, sickness <30, unrest <40)");
            }

            int oldMorale = state.morale;
            state.morale = Mathf.Clamp(state.morale + moraleDelta, 0, 100);

            // L12 Martial Law cap
            if (ctx.moraleCap.HasValue && state.morale > ctx.moraleCap.Value)
            {
                state.morale = ctx.moraleCap.Value;
            }

            log.AddFlat(CausalityCategory.Morale, "Net Morale Change", state.morale - oldMorale,
                $"Morale: {oldMorale} → {state.morale}");
        }
    }
}
