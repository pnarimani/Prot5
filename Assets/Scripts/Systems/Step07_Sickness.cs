using SiegeSurvival.Core;
using UnityEngine;

namespace SiegeSurvival.Systems
{
    /// <summary>
    /// Step 7: Apply sickness progression. Base +2/day, modified by laws, overcrowding, fuel deficit, sanitation, clinic.
    /// Also apply L9 Medical Triage deaths.
    /// </summary>
    public static class Step07_Sickness
    {
        public static void Execute(GameState state, SimulationContext ctx, CausalityLog log)
        {
            int baseSickness = 2;
            int totalChange = baseSickness + ctx.sicknessDelta - ctx.sanitationReduction - ctx.clinicReduction;

            log.AddFlat(CausalityCategory.Sickness, "Base Sickness", baseSickness, "Sickness +2/day (base)");

            int oldSickness = state.sickness;
            state.sickness = Mathf.Clamp(state.sickness + totalChange, 0, 100);

            log.AddFlat(CausalityCategory.Sickness, "Net Sickness Change", state.sickness - oldSickness,
                $"Sickness: {oldSickness} → {state.sickness} (net {totalChange}: base +{baseSickness}, modifiers {ctx.sicknessDelta:+#;-#;0}, sanitation -{ctx.sanitationReduction}, clinic -{ctx.clinicReduction})");

            // L9 Medical Triage deaths (kill from Sick population specifically)
            if (ctx.deathsSick > 0)
            {
                PopulationManager.ApplyDeathsSickOnly(state, ctx.deathsSick, log, "Medical Triage (L9)");
            }
        }
    }
}
