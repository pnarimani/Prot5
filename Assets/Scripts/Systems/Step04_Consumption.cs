using SiegeSurvival.Core;
using UnityEngine;

namespace SiegeSurvival.Systems
{
    /// <summary>
    /// Step 4: Apply resource consumption (Food, Water, Fuel).
    /// </summary>
    public static class Step04_Consumption
    {
        public static void Execute(GameState state, SimulationContext ctx, CausalityLog log)
        {
            // === 4a. Food Consumption ===
            int totalFoodBase = 0;
            for (int i = 0; i < state.zones.Length; i++)
            {
                var zone = state.zones[i];
                if (zone.isLost) continue;

                int tiers = zone.OvercrowdingTiers10Pct;
                float zoneFoodMult = 1f + (0.05f * tiers);
                int zoneFood = Mathf.CeilToInt(zone.currentPopulation * 1f * zoneFoodMult);
                totalFoodBase += zoneFood;

                if (tiers > 0)
                {
                    log.AddFlat(CausalityCategory.Consumption, $"Overcrowding Food ({zone.definition.zoneName})",
                        zoneFood - zone.currentPopulation,
                        $"+{zoneFood - zone.currentPopulation} extra food from {zone.OvercrowdingPercent:F0}% overcrowding");
                }
            }

            // Apply profile food consumption multiplier (P1 Disease Wave: ×0.98)
            float foodMult = ctx.foodConsumptionMult * state.profileFoodConsumptionMult;
            int totalFood = Mathf.CeilToInt(totalFoodBase * foodMult) + ctx.flatFoodConsumption;
            ctx.foodConsumed = totalFood;

            state.food -= totalFood;
            if (state.food < 0) state.food = 0;
            ctx.foodDeficit = state.food <= 0;

            log.AddFlat(CausalityCategory.Food, "Food Consumed", -totalFood,
                $"-{totalFood} Food ({totalFoodBase} base × {foodMult:F2} + {ctx.flatFoodConsumption} flat)");

            // === 4b. Water Consumption ===
            float waterMult = ctx.waterConsumptionMult;
            int totalWater = Mathf.CeilToInt(state.TotalPopulation * 1f * waterMult);
            ctx.waterConsumed = totalWater;

            state.water -= totalWater;
            if (state.water < 0) state.water = 0;
            ctx.waterDeficit = state.water <= 0;

            log.AddFlat(CausalityCategory.Water, "Water Consumed", -totalWater,
                $"-{totalWater} Water ({state.TotalPopulation} pop × {waterMult:F2})");

            // === 4c. Fuel Consumption ===
            int zonesOver20 = state.ZonesOver20PctCount;
            float overcrowdingFuelMod = 1f + (0.10f * zonesOver20);
            float pop = state.TotalPopulation;
            int totalFuel = Mathf.CeilToInt(120f * (pop / 120f) * overcrowdingFuelMod);
            ctx.fuelConsumed = totalFuel;

            state.fuel -= totalFuel;
            ctx.fuelDeficit = state.fuel <= 0;
            if (state.fuel < 0) state.fuel = 0;

            log.AddFlat(CausalityCategory.Fuel, "Fuel Consumed", -totalFuel,
                $"-{totalFuel} Fuel (120 × {pop}/120 × {overcrowdingFuelMod:F2})");
            if (zonesOver20 > 0)
            {
                log.AddFlat(CausalityCategory.Consumption, "Fuel Overcrowding Modifier", zonesOver20,
                    $"{zonesOver20} zones ≥20% overcrowded → fuel ×{overcrowdingFuelMod:F2}");
            }
        }
    }
}
