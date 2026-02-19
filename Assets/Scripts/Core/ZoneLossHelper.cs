using SiegeSurvival.Data.Runtime;
using UnityEngine;

namespace SiegeSurvival.Core
{
    /// <summary>
    /// Consolidates zone loss logic used by Step10, Step12, and GameManager.
    /// Call <see cref="TryApplyZoneLoss"/> after reducing a zone's integrity to check
    /// for loss and apply standard effects.
    /// </summary>
    public static class ZoneLossHelper
    {
        /// <summary>
        /// Checks whether a zone has reached 0 integrity and, if so, applies standard
        /// on-loss effects (stat changes, population migration, optional production log).
        /// Returns true if the zone was lost (or keep was breached).
        /// </summary>
        /// <param name="state">Current game state.</param>
        /// <param name="zone">The zone whose integrity may have hit 0.</param>
        /// <param name="ctx">Simulation context (used for keep breach flag). May be null outside simulation.</param>
        /// <param name="log">Causality log for recording what happened.</param>
        /// <param name="causeLabel">Short label for log entries, e.g. "Siege Damage" or "Wall Breach (E4)".</param>
        /// <returns>True if the zone was lost or keep was breached.</returns>
        public static bool TryApplyZoneLoss(GameState state, ZoneState zone, SimulationContext ctx,
            CausalityLog log, string causeLabel)
        {
            if (zone.currentIntegrity > 0) return false;

            zone.currentIntegrity = 0;

            if (zone.definition.isKeep)
            {
                if (ctx != null) ctx.keepBreached = true;
                log.AddFlat(CausalityCategory.Integrity, "KEEP BREACHED", 0,
                    $"Keep integrity reached 0 — BREACH GAME OVER ({causeLabel})");
                return true;
            }

            zone.isLost = true;

            // Apply standard on-loss stat effects
            state.unrest += zone.definition.onLossUnrest;
            state.sickness += zone.definition.onLossSickness;
            state.morale += zone.definition.onLossMorale;

            log.AddFlat(CausalityCategory.Integrity,
                $"Zone Lost: {zone.definition.zoneName}", 0,
                $"{zone.definition.zoneName} LOST! Unrest +{zone.definition.onLossUnrest}, " +
                $"Sickness +{zone.definition.onLossSickness}, Morale {zone.definition.onLossMorale} ({causeLabel})");

            if (!string.IsNullOrEmpty(zone.definition.onLossProductionDesc))
            {
                log.AddFlat(CausalityCategory.Production, "Zone Loss Production",
                    0, zone.definition.onLossProductionDesc);
            }

            // Move population inward
            PopulationManager.ForcePopulationInward(state, zone, log);

            return true;
        }
    }
}
