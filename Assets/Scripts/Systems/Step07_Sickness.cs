using SiegeSurvival.Core;
using UnityEngine;

namespace SiegeSurvival.Systems
{
    /// <summary>
    /// Step 7: Apply sickness progression. Base +2/day, modified by laws, overcrowding, fuel deficit.
    /// Treatment (sanitation + clinic) scales with current sickness - higher sickness = stronger treatment.
    /// Result: can slow growth but never fully erase it.
    /// Also apply L9 Medical Triage deaths.
    /// </summary>
    public static class Step07_Sickness
    {
        const float TreatmentScaling = 0.10f;

        public static void Execute(GameState state, SimulationContext ctx, CausalityLog log)
        {
            int baseSickness = 2;

            int totalCapacity = ctx.sanitationReduction + ctx.clinicReduction;
            float treatmentEffect = totalCapacity * TreatmentScaling * state.sickness;

            int totalChange = baseSickness + ctx.sicknessDelta - Mathf.RoundToInt(treatmentEffect);

            log.AddFlat(CausalityCategory.Sickness, "Base Sickness", baseSickness, "Sickness +2/day (base)");

            int oldSickness = state.sickness;
            state.sickness = Mathf.Clamp(state.sickness + totalChange, 0, 100);

            log.AddFlat(CausalityCategory.Sickness, "Net Sickness Change", state.sickness - oldSickness,
                $"Sickness: {oldSickness} → {state.sickness} (net {totalChange}: base +{baseSickness}, pressure {ctx.sicknessDelta:+#;-#;0}, treatment -{Mathf.RoundToInt(treatmentEffect):+0;-0;0})");

            // L9 Medical Triage deaths (kill from Sick population specifically)
            if (ctx.deathsSick > 0)
            {
                PopulationManager.ApplyDeathsSickOnly(state, ctx.deathsSick, log, "Medical Triage (L9)");
            }
        }
    }
}
