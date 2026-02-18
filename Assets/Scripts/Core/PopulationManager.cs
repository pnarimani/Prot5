using System;
using System.Collections.Generic;
using SiegeSurvival.Data;
using SiegeSurvival.Data.Runtime;
using UnityEngine;

namespace SiegeSurvival.Core
{
    /// <summary>
    /// Handles population death, forced inward movement, and zone initialization.
    /// </summary>
    public static class PopulationManager
    {
        // ===== Death Priority =====

        /// <summary>Default death priority: Sick → Elderly → Healthy → Guards.</summary>
        public static void ApplyDeathsDefault(GameState state, int count, CausalityLog log, string source)
        {
            if (count <= 0) return;
            int remaining = count;
            int totalKilled = 0;

            // Sick first
            int fromSick = Math.Min(remaining, state.sick);
            state.sick -= fromSick;
            remaining -= fromSick;
            totalKilled += fromSick;

            // Elderly
            if (remaining > 0)
            {
                int fromElderly = Math.Min(remaining, state.elderly);
                state.elderly -= fromElderly;
                remaining -= fromElderly;
                totalKilled += fromElderly;
            }

            // Healthy Workers
            if (remaining > 0)
            {
                int fromHealthy = Math.Min(remaining, state.healthyWorkers);
                state.healthyWorkers -= fromHealthy;
                remaining -= fromHealthy;
                totalKilled += fromHealthy;
            }

            // Guards
            if (remaining > 0)
            {
                int fromGuards = Math.Min(remaining, state.guards);
                state.guards -= fromGuards;
                remaining -= fromGuards;
                totalKilled += fromGuards;
            }

            if (totalKilled > 0)
            {
                log.AddFlat(CausalityCategory.Death, source, -totalKilled, $"{totalKilled} deaths ({source})");
                ValidateWorkerAllocations(state);
            }
        }

        /// <summary>L6 death priority: Healthy → Sick → Elderly → Guards.</summary>
        public static void ApplyDeathsHealthyFirst(GameState state, int count, CausalityLog log, string source)
        {
            if (count <= 0) return;
            int remaining = count;
            int totalKilled = 0;

            // Healthy first
            int fromHealthy = Math.Min(remaining, state.healthyWorkers);
            state.healthyWorkers -= fromHealthy;
            remaining -= fromHealthy;
            totalKilled += fromHealthy;

            // Sick
            if (remaining > 0)
            {
                int fromSick = Math.Min(remaining, state.sick);
                state.sick -= fromSick;
                remaining -= fromSick;
                totalKilled += fromSick;
            }

            // Elderly
            if (remaining > 0)
            {
                int fromElderly = Math.Min(remaining, state.elderly);
                state.elderly -= fromElderly;
                remaining -= fromElderly;
                totalKilled += fromElderly;
            }

            // Guards
            if (remaining > 0)
            {
                int fromGuards = Math.Min(remaining, state.guards);
                state.guards -= fromGuards;
                remaining -= fromGuards;
                totalKilled += fromGuards;
            }

            if (totalKilled > 0)
            {
                log.AddFlat(CausalityCategory.Death, source, -totalKilled, $"{totalKilled} deaths ({source}, healthy first)");
                ValidateWorkerAllocations(state);
            }
        }

        /// <summary>Kill specifically from Sick population.</summary>
        public static void ApplyDeathsSickOnly(GameState state, int count, CausalityLog log, string source)
        {
            if (count <= 0) return;
            int killed = Math.Min(count, state.sick);
            state.sick -= killed;
            if (killed > 0)
            {
                log.AddFlat(CausalityCategory.Death, source, -killed, $"{killed} sick deaths ({source})");
            }
        }

        // ===== Worker Allocation Validation =====

        /// <summary>
        /// If total assigned workers exceeds available, unassign from lowest priority slots.
        /// Priority (lowest first): FuelScavenging, ClinicStaff, Sanitation, Repairs, MaterialsCrafting, WaterDrawing, FoodProduction
        /// </summary>
        public static void ValidateWorkerAllocations(GameState state)
        {
            int available = state.AvailableWorkers;
            int assigned = state.AssignedWorkers;
            if (assigned <= available) return;

            int excess = assigned - available;
            var priority = new[]
            {
                JobSlot.FuelScavenging,
                JobSlot.ClinicStaff,
                JobSlot.Sanitation,
                JobSlot.Repairs,
                JobSlot.MaterialsCrafting,
                JobSlot.WaterDrawing,
                JobSlot.FoodProduction
            };

            foreach (var slot in priority)
            {
                if (excess <= 0) break;
                int current = state.workerAllocation[slot];
                int remove = Math.Min(current, excess);
                // Remove in increments of 5, rounding up to remove enough
                remove = ((remove + 4) / 5) * 5;
                remove = Math.Min(remove, current);
                state.workerAllocation[slot] -= remove;
                excess -= remove;
            }
        }

        // ===== Desertion (remove healthy workers specifically) =====

        public static void ApplyDesertion(GameState state, int count, CausalityLog log)
        {
            int deserted = Math.Min(count, state.healthyWorkers);
            state.healthyWorkers -= deserted;
            if (deserted > 0)
            {
                log.AddFlat(CausalityCategory.Death, "Desertion Wave (E3)", -deserted,
                    $"{deserted} healthy workers deserted");
                ValidateWorkerAllocations(state);
            }
        }

        // ===== Force Population Inward =====

        /// <summary>
        /// Move all population from a lost/evacuated zone to the next inner non-lost zone.
        /// </summary>
        public static void ForcePopulationInward(GameState state, ZoneState lostZone, CausalityLog log)
        {
            int displaced = lostZone.currentPopulation;
            lostZone.currentPopulation = 0;

            if (displaced <= 0) return;

            // Find next inner non-lost zone
            for (int i = lostZone.definition.order + 1; i < state.zones.Length; i++)
            {
                if (!state.zones[i].isLost)
                {
                    state.zones[i].currentPopulation += displaced;
                    log.AddFlat(CausalityCategory.Population, "Forced Inward", displaced,
                        $"{displaced} displaced from {lostZone.definition.zoneName} → {state.zones[i].definition.zoneName}");
                    return;
                }
            }

            // Should not happen (Keep is last resort)
            Debug.LogError("ForcePopulationInward: No zone available to receive displaced population!");
        }

        // ===== Zone Population Initialization =====

        /// <summary>Fill zones outer-to-inner with total population.</summary>
        public static void InitializeZonePopulations(GameState state)
        {
            int remaining = state.TotalPopulation;
            for (int i = 0; i < state.zones.Length; i++)
            {
                int placed = Math.Min(state.zones[i].effectiveCapacity, remaining);
                state.zones[i].currentPopulation = placed;
                remaining -= placed;
            }
        }

        // ===== Recompute zone populations from total =====
        // After deaths, reduce total pop from outermost zones
        public static void RecomputeZonePopulationsAfterDeaths(GameState state)
        {
            // Total pop may have decreased. We don't move people between zones for deaths.
            // Instead, proportionally reduce from outermost overcrowded zones.
            int totalInZones = 0;
            foreach (var z in state.zones)
            {
                if (!z.isLost) totalInZones += z.currentPopulation;
            }

            int actualPop = state.TotalPopulation;
            int excess = totalInZones - actualPop;
            if (excess <= 0) return;

            // Remove excess from outermost non-lost zones first
            for (int i = 0; i < state.zones.Length && excess > 0; i++)
            {
                if (state.zones[i].isLost) continue;
                int canRemove = Math.Min(state.zones[i].currentPopulation, excess);
                state.zones[i].currentPopulation -= canRemove;
                excess -= canRemove;
            }
        }
    }
}
