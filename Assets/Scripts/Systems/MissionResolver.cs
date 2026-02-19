using System.Linq;
using SiegeSurvival.Core;
using SiegeSurvival.Data;
using SiegeSurvival.Data.Runtime;
using UnityEngine;

namespace SiegeSurvival.Systems
{
    /// <summary>
    /// Resolves the active mission at end of day (sub-step 12b).
    /// </summary>
    public static class MissionResolver
    {
        public static void ResolveMission(GameState state, SimulationContext ctx, CausalityLog log, RandomProvider rng)
        {
            if (state.activeMission == null) return;

            // Check if mission duration has elapsed
            var def = Resources.LoadAll<MissionDefinition>("Data/Missions")
                .FirstOrDefault(x => x.missionId == state.activeMission.missionId);
            int duration = def != null ? def.Duration : 5;

            if (state.currentDay < state.activeMission.startDay + duration - 1)
                return; // Mission still in progress

            // Calculate fuel risk modifier
            float fuelRiskMod = 0f;
            if (state.fuel >= 100) fuelRiskMod = 0f;
            else if (state.fuel >= 50) fuelRiskMod = 0.05f;
            else if (state.fuel >= 1) fuelRiskMod = 0.15f;
            else fuelRiskMod = 0.25f;

            float roll = rng.Range01();

            switch (state.activeMission.missionId)
            {
                case MissionId.M1_ForageBeyondWalls:
                    ResolveM1(state, fuelRiskMod, roll, log);
                    break;
                case MissionId.M2_NightRaid:
                    ResolveM2(state, fuelRiskMod, roll, log);
                    break;
                case MissionId.M3_SearchAbandonedHomes:
                    ResolveM3(state, fuelRiskMod, roll, log);
                    break;
                case MissionId.M4_NegotiateBlackMarketeers:
                    ResolveM4(state, fuelRiskMod, roll, log);
                    break;
            }

            // Return workers
            state.activeMission = null;
        }

        private static void ResolveM1(GameState state, float fuelRiskMod, float roll, CausalityLog log)
        {
            float baseAmbush = state.siegeIntensity >= 4 ? 0.30f : 0.15f;
            float adjustedAmbush = baseAmbush + fuelRiskMod;

            float remainingGood = 1f - adjustedAmbush;
            // Original non-ambush ratio: 60/25 for normal, adjusted for siege
            float origGreat, origOk;
            if (state.siegeIntensity >= 4)
            {
                origGreat = 0.5294f;
                origOk = 0.1706f;
            }
            else
            {
                origGreat = 0.60f;
                origOk = 0.25f;
            }
            float totalOrig = origGreat + origOk;
            float chanceGreat = remainingGood * (origGreat / totalOrig);
            float chanceOk = remainingGood * (origOk / totalOrig);

            if (roll < adjustedAmbush)
            {
                PopulationManager.ApplyDeathsDefault(state, 5, log, "Forage Ambush (M1)");
                log.AddFlat(CausalityCategory.Mission, "Forage (M1): Ambushed", 0,
                    $"Forage mission AMBUSHED! 5 deaths (chance: {adjustedAmbush:P0})");
            }
            else if (roll < adjustedAmbush + chanceOk)
            {
                state.food += 80;
                log.AddFlat(CausalityCategory.Mission, "Forage (M1): Moderate", 80,
                    $"Forage mission returned with +80 Food (chance: {chanceOk:P0})");
            }
            else
            {
                state.food += 120;
                log.AddFlat(CausalityCategory.Mission, "Forage (M1): Great", 120,
                    $"Forage mission returned with +120 Food (chance: {chanceGreat:P0})");
            }
        }

        private static void ResolveM2(GameState state, float fuelRiskMod, float roll, CausalityLog log)
        {
            float baseCaptured = 0.20f;
            if (state.activeMission.fuelWasInsufficient)
                baseCaptured += 0.20f;
            float adjustedCaptured = baseCaptured + fuelRiskMod;

            float remaining = 1f - adjustedCaptured;
            float chanceGreat = remaining * 0.5f; // -10 intensity
            float chanceOk = remaining * 0.5f;    // -5 intensity

            if (roll < adjustedCaptured)
            {
                PopulationManager.ApplyDeathsDefault(state, 8, log, "Night Raid Captured (M2)");
                state.unrest += 15;
                state.siegeIntensity = Mathf.Min(state.siegeIntensity + 1, 6);
                log.AddFlat(CausalityCategory.Mission, "Night Raid (M2): Captured", 0,
                    $"Night Raid CAPTURED! 8 deaths, Unrest +15, Siege Intensity +1 (chance: {adjustedCaptured:P0})");
            }
            else if (roll < adjustedCaptured + chanceOk)
            {
                state.nightRaidDebuff = new NightRaidDebuff(5, 3);
                log.AddFlat(CausalityCategory.Mission, "Night Raid (M2): Moderate", 0,
                    $"Night Raid partial success: Siege Intensity -5 for 3 days (chance: {chanceOk:P0})");
            }
            else
            {
                state.nightRaidDebuff = new NightRaidDebuff(10, 3);
                log.AddFlat(CausalityCategory.Mission, "Night Raid (M2): Great", 0,
                    $"Night Raid great success: Siege Intensity -10 for 3 days (chance: {chanceGreat:P0})");
            }
        }

        private static void ResolveM3(GameState state, float fuelRiskMod, float roll, CausalityLog log)
        {
            float basePlague = 0.20f;
            float adjustedPlague = basePlague + fuelRiskMod;
            float remaining = 1f - adjustedPlague;
            float chanceMatl = remaining * (0.50f / 0.80f);
            float chanceMed = remaining * (0.30f / 0.80f);

            if (roll < adjustedPlague)
            {
                state.sickness += 15;
                log.AddFlat(CausalityCategory.Mission, "Search Homes (M3): Plague", 15,
                    $"Search mission: Plague exposure! Sickness +15 (chance: {adjustedPlague:P0})");
            }
            else if (roll < adjustedPlague + chanceMed)
            {
                state.medicine += 40;
                log.AddFlat(CausalityCategory.Mission, "Search Homes (M3): Medicine", 40,
                    $"Search mission found +40 Medicine (chance: {chanceMed:P0})");
            }
            else
            {
                state.materials += 60;
                log.AddFlat(CausalityCategory.Mission, "Search Homes (M3): Materials", 60,
                    $"Search mission found +60 Materials (chance: {chanceMatl:P0})");
            }
        }

        private static void ResolveM4(GameState state, float fuelRiskMod, float roll, CausalityLog log)
        {
            float baseScandal = 0.20f;
            float adjustedScandal = baseScandal + fuelRiskMod;
            float remaining = 1f - adjustedScandal;
            float chanceWater = remaining * (0.50f / 0.80f);
            float chanceFood = remaining * (0.30f / 0.80f);

            if (roll < adjustedScandal)
            {
                state.unrest += 20;
                log.AddFlat(CausalityCategory.Mission, "Black Market (M4): Scandal", 20,
                    $"Black Market scandal! Unrest +20 (chance: {adjustedScandal:P0})");
            }
            else if (roll < adjustedScandal + chanceFood)
            {
                state.food += 80;
                log.AddFlat(CausalityCategory.Mission, "Black Market (M4): Food", 80,
                    $"Black Market: +80 Food (chance: {chanceFood:P0})");
            }
            else
            {
                state.water += 100;
                log.AddFlat(CausalityCategory.Mission, "Black Market (M4): Water", 100,
                    $"Black Market: +100 Water (chance: {chanceWater:P0})");
            }
        }

        // === Odds Calculation for UI Display ===

        public static void GetMissionOdds(MissionId id, GameState state, out float[] probs, out string[] labels)
        {
            float fuelRiskMod = 0f;
            if (state.fuel >= 100) fuelRiskMod = 0f;
            else if (state.fuel >= 50) fuelRiskMod = 0.05f;
            else if (state.fuel >= 1) fuelRiskMod = 0.15f;
            else fuelRiskMod = 0.25f;

            switch (id)
            {
                case MissionId.M1_ForageBeyondWalls:
                {
                    float baseAmbush = state.siegeIntensity >= 4 ? 0.30f : 0.15f;
                    float adjAmbush = baseAmbush + fuelRiskMod;
                    float rem = 1f - adjAmbush;
                    float origG = state.siegeIntensity >= 4 ? 0.5294f : 0.60f;
                    float origO = state.siegeIntensity >= 4 ? 0.1706f : 0.25f;
                    float total = origG + origO;
                    probs = new[] { rem * (origG / total), rem * (origO / total), adjAmbush };
                    labels = new[] { "+120 Food", "+80 Food", "Ambushed: 5 deaths" };
                    return;
                }
                case MissionId.M2_NightRaid:
                {
                    float baseCap = 0.20f;
                    // Check if fuel < 40 currently (preview)
                    if (state.fuel < 40) baseCap += 0.20f;
                    float adjCap = baseCap + fuelRiskMod;
                    float rem = 1f - adjCap;
                    probs = new[] { rem * 0.5f, rem * 0.5f, adjCap };
                    labels = new[] { "Siege -10 (3 days)", "Siege -5 (3 days)", "Captured: 8 deaths +15 Unrest" };
                    return;
                }
                case MissionId.M3_SearchAbandonedHomes:
                {
                    float adjPlague = 0.20f + fuelRiskMod;
                    float rem = 1f - adjPlague;
                    probs = new[] { rem * (0.50f / 0.80f), rem * (0.30f / 0.80f), adjPlague };
                    labels = new[] { "+60 Materials", "+40 Medicine", "Plague: Sickness +15" };
                    return;
                }
                case MissionId.M4_NegotiateBlackMarketeers:
                {
                    float adjScandal = 0.20f + fuelRiskMod;
                    float rem = 1f - adjScandal;
                    probs = new[] { rem * (0.50f / 0.80f), rem * (0.30f / 0.80f), adjScandal };
                    labels = new[] { "+100 Water", "+80 Food", "Scandal: Unrest +20" };
                    return;
                }
                default:
                    probs = new float[0];
                    labels = new string[0];
                    return;
            }
        }
    }
}
