using SiegeSurvival.Core;
using SiegeSurvival.Data.Runtime;
using UnityEngine;

namespace SiegeSurvival.Systems
{
    /// <summary>
    /// Step 10: Apply siege damage to active perimeter. Handle intensity schedule and night raid debuff.
    /// </summary>
    public static class Step10_SiegeDamage
    {
        public static void Execute(GameState state, SimulationContext ctx, CausalityLog log)
        {
            // Determine effective intensity (with Night Raid debuff)
            int effectiveIntensity = state.siegeIntensity;
            if (state.nightRaidDebuff != null)
            {
                effectiveIntensity = Mathf.Max(0, effectiveIntensity - state.nightRaidDebuff.intensityReduction);
                state.nightRaidDebuff.daysRemaining--;
                log.AddFlat(CausalityCategory.SiegeDamage, "Night Raid Debuff",
                    -state.nightRaidDebuff.intensityReduction,
                    $"Effective intensity reduced by {state.nightRaidDebuff.intensityReduction} (Night Raid, {state.nightRaidDebuff.daysRemaining + 1} days left)");
                if (state.nightRaidDebuff.daysRemaining <= 0)
                {
                    state.nightRaidDebuff = null;
                }
            }

            // Intensity schedule: +1 every 6 days
            if (state.currentDay > 1 && (state.currentDay - 1) % 6 == 0)
            {
                int oldIntensity = state.siegeIntensity;
                state.siegeIntensity = Mathf.Min(state.siegeIntensity + 1, 6);
                if (state.siegeIntensity > oldIntensity)
                {
                    log.AddFlat(CausalityCategory.SiegeDamage, "Intensity Escalation", 1,
                        $"Siege Intensity: {oldIntensity} → {state.siegeIntensity} (every 6 days)");
                }
            }

            // Calculate damage
            ZoneState perim = state.GetActivePerimeter();
            float baseDamage = 3 + effectiveIntensity;
            float perimFactor = perim.definition.perimeterFactor;
            float rawDamage = baseDamage * perimFactor;
            float afterGuards = Mathf.Max(0, rawDamage - ctx.siegeDamageReduction);
            int finalDamage = Mathf.FloorToInt(afterGuards * ctx.siegeDamageMult);

            perim.currentIntegrity -= finalDamage;

            log.AddFlat(CausalityCategory.SiegeDamage, "Siege Damage", -finalDamage,
                $"Siege: (3+{effectiveIntensity}) × {perimFactor:F1} - {ctx.siegeDamageReduction} guards × {ctx.siegeDamageMult:F2} L11 = {finalDamage} dmg → {perim.definition.zoneName} ({perim.currentIntegrity}/{perim.definition.baseIntegrity})");

            // Check zone loss
            ZoneLossHelper.TryApplyZoneLoss(state, perim, ctx, log, "Siege Damage");
        }
    }
}
