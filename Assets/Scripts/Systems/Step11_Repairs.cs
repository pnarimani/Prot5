using SiegeSurvival.Core;
using SiegeSurvival.Data.Runtime;
using UnityEngine;

namespace SiegeSurvival.Systems
{
    /// <summary>
    /// Step 11: Apply repairs to active perimeter zone.
    /// </summary>
    public static class Step11_Repairs
    {
        public static void Execute(GameState state, SimulationContext ctx, CausalityLog log)
        {
            if (ctx.repairAmount <= 0) return;

            ZoneState perim = state.GetActivePerimeter();
            int oldIntegrity = perim.currentIntegrity;
            perim.currentIntegrity = Mathf.Min(
                perim.currentIntegrity + ctx.repairAmount,
                perim.definition.baseIntegrity
            );
            int actualRepair = perim.currentIntegrity - oldIntegrity;

            log.AddFlat(CausalityCategory.Integrity, "Repairs Applied", actualRepair,
                $"Repaired {actualRepair} integrity on {perim.definition.zoneName}: {oldIntegrity} → {perim.currentIntegrity}/{perim.definition.baseIntegrity}");
        }
    }
}
