using SiegeSurvival.Core;
using SiegeSurvival.Data;
using SiegeSurvival.Data.Runtime;
using UnityEngine;

namespace SiegeSurvival.Systems
{
    /// <summary>
    /// Step 12: Resolve triggered events, early incidents, and missions.
    /// </summary>
    public static class Step12_Events
    {
        public static void Execute(GameState state, SimulationContext ctx, CausalityLog log, RandomProvider rng)
        {
            // === Main Events ===

            // E1. Hunger Riot
            if (state.consecutiveFoodDeficitDays >= 2 && state.unrest > 50)
            {
                state.food = Mathf.Max(0, state.food - 80);
                PopulationManager.ApplyDeathsDefault(state, 5, log, "Hunger Riot (E1)");
                state.unrest += 15;
                log.AddFlat(CausalityCategory.Event, "Hunger Riot (E1)", 0,
                    "Hunger Riot! Food -80, 5 deaths, Unrest +15 (2+ days food deficit + Unrest > 50)");
            }

            // E2. Fever Outbreak
            if (state.sickness > 60)
            {
                PopulationManager.ApplyDeathsDefault(state, 10, log, "Fever Outbreak (E2)");
                state.unrest += 10;
                log.AddFlat(CausalityCategory.Event, "Fever Outbreak (E2)", 0,
                    "Fever Outbreak! 10 deaths, Unrest +10 (Sickness > 60)");
            }

            // E3. Desertion Wave
            if (state.morale < 30)
            {
                PopulationManager.ApplyDesertion(state, 10, log);
                log.AddFlat(CausalityCategory.Event, "Desertion Wave (E3)", 0,
                    "Desertion Wave! Up to 10 healthy workers leave (Morale < 30)");
            }

            // E4. Wall Breach Attempt
            ZoneState perim = state.GetActivePerimeter();
            if (perim.currentIntegrity < 30 && perim.currentIntegrity > 0)
            {
                if (state.guards >= 15)
                {
                    log.AddFlat(CausalityCategory.Event, "Wall Breach Attempt (E4) — NEGATED", 0,
                        "Wall Breach Attempt negated by guards (≥15 on duty)");
                }
                else
                {
                    perim.currentIntegrity -= 15;
                    log.AddFlat(CausalityCategory.Event, "Wall Breach Attempt (E4)", -15,
                        $"Wall Breach Attempt! {perim.definition.zoneName} Integrity -15 → {perim.currentIntegrity}");

                    // Check if this causes zone loss
                    ZoneLossHelper.TryApplyZoneLoss(state, perim, ctx, log, "Wall Breach (E4)");
                }
            }

            // E5. Fire in Artisan Quarter
            if (state.siegeIntensity >= 4 && rng.Chance(0.10f))
            {
                state.materials = Mathf.Max(0, state.materials - 50);
                if (!state.ArtisanQuarter.isLost)
                {
                    state.ArtisanQuarter.currentIntegrity -= 10;
                    log.AddFlat(CausalityCategory.Event, "Fire in Artisan Quarter (E5)", 0,
                        $"Fire in Artisan Quarter! Materials -50, Artisan Integrity -10 → {state.ArtisanQuarter.currentIntegrity}");
                }
                else
                {
                    log.AddFlat(CausalityCategory.Event, "Fire in Artisan Quarter (E5)", 0,
                        "Fire in Artisan Quarter! Materials -50 (quarter already lost)");
                }
            }

            // E6 & E7 are checked in Step 13 (Loss Conditions)

            // === Early Incidents ===
            foreach (var incident in state.scheduledIncidents)
            {
                if (incident.resolved) continue;
                if (incident.scheduledDay != state.currentDay) continue;

                incident.resolved = true;
                switch (incident.incidentId)
                {
                    case EarlyIncidentId.MinorFire:
                        state.materials = Mathf.Max(0, state.materials - 20);
                        log.AddFlat(CausalityCategory.Event, "Early Incident: Minor Fire", -20,
                            "Minor Fire! Materials -20");
                        break;
                    case EarlyIncidentId.FeverCluster:
                        state.sickness += 8;
                        log.AddFlat(CausalityCategory.Event, "Early Incident: Fever Cluster", 8,
                            "Fever Cluster! Sickness +8");
                        break;
                    case EarlyIncidentId.FoodTheft:
                        state.food = Mathf.Max(0, state.food - 40);
                        state.unrest += 5;
                        log.AddFlat(CausalityCategory.Event, "Early Incident: Food Theft", 0,
                            "Food Theft! Food -40, Unrest +5");
                        break;
                    case EarlyIncidentId.GuardDesertion:
                        state.guards = Mathf.Max(0, state.guards - 5);
                        state.unrest += 5;
                        log.AddFlat(CausalityCategory.Event, "Early Incident: Guard Desertion", 0,
                            "Guard Desertion! Guards -5, Unrest +5");
                        break;
                }
            }

            // === Mission Resolution (sub-step 12b) ===
            MissionResolver.ResolveMission(state, ctx, log, rng);
        }

        /// <summary>Check if any early incident is scheduled for tomorrow (for warning display).</summary>
        public static string GetTomorrowIncidentWarning(GameState state)
        {
            int tomorrow = state.currentDay + 1;
            foreach (var incident in state.scheduledIncidents)
            {
                if (incident.resolved) continue;
                if (incident.scheduledDay == tomorrow)
                {
                    return $"WARNING: Reports suggest a '{incident.incidentId}' incident may occur tomorrow.";
                }
            }
            return null;
        }
    }
}
