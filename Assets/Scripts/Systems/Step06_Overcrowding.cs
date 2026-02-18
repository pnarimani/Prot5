using SiegeSurvival.Core;
using UnityEngine;

namespace SiegeSurvival.Systems
{
    /// <summary>
    /// Step 6: Apply overcrowding penalties to Unrest and Sickness.
    /// (Food/Fuel overcrowding effects are already in Step 4.)
    /// </summary>
    public static class Step06_Overcrowding
    {
        public static void Execute(GameState state, SimulationContext ctx, CausalityLog log)
        {
            for (int i = 0; i < state.zones.Length; i++)
            {
                var zone = state.zones[i];
                if (zone.isLost || !zone.IsOvercrowded) continue;

                int tiers = zone.OvercrowdingTiers10Pct;
                if (tiers <= 0) continue;

                int unrestAdd = 2 * tiers;
                int sicknessAdd = 2 * tiers;

                ctx.unrestDelta += unrestAdd;
                ctx.sicknessDelta += sicknessAdd;

                log.AddFlat(CausalityCategory.Overcrowding, $"Overcrowding ({zone.definition.zoneName})",
                    unrestAdd + sicknessAdd,
                    $"Overcrowding {zone.OvercrowdingPercent:F0}% in {zone.definition.zoneName}: Unrest +{unrestAdd}, Sickness +{sicknessAdd}");
            }
        }
    }
}
