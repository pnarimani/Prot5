using System.Collections.Generic;
using System.IO;
using SiegeSurvival.Core;
using SiegeSurvival.Data;
using UnityEngine;

namespace SiegeSurvival.Telemetry
{
    [System.Serializable]
    public class DaySnapshot
    {
        public int day;
        public int food, water, fuel, medicine, materials;
        public int morale, unrest, sickness, siegeIntensity;
        public int population, healthy, guards, sick, elderly;
        public List<string> events = new();
    }

    [System.Serializable]
    public class RunTelemetry
    {
        public string pressureProfile;
        public int randomSeed;
        public List<string> earlyIncidents = new();

        public int dayOfFirstDeficit = -1;
        public string whichResourceFirst;
        public int dayOfFirstZoneLost = -1;
        public string whichZoneFirst;
        public string firstLawEnacted;
        public int firstLawDay = -1;

        public string causeOfLoss;
        public int dayOfLoss;
        public int finalPopulation;
        public float averageUnrest;

        public List<DaySnapshot> dayByDaySnapshots = new();
    }

    public class TelemetryLogger
    {
        public RunTelemetry Data { get; private set; } = new();
        private float _unrestSum;
        private int _dayCount;

        // Convenience properties for UI panels
        public int FirstDeficitDay => Data.dayOfFirstDeficit;
        public string FirstDeficitResource => Data.whichResourceFirst ?? "None";
        public int FirstZoneLostDay => Data.dayOfFirstZoneLost;
        public string FirstZoneLostName => Data.whichZoneFirst ?? "None";
        public float AverageUnrest => Data.averageUnrest;

        public void RecordSetup(GameState state, int seed)
        {
            Data = new RunTelemetry();
            Data.pressureProfile = state.activeProfile.ToString();
            Data.randomSeed = seed;

            foreach (var inc in state.scheduledIncidents)
            {
                Data.earlyIncidents.Add($"{inc.incidentId} on Day {inc.scheduledDay}");
            }

            _unrestSum = 0;
            _dayCount = 0;
        }

        public void RecordDayEnd(GameState state, CausalityLog log)
        {
            var snap = new DaySnapshot
            {
                day = state.currentDay - 1, // Day just completed (currentDay was already incremented)
                food = state.food,
                water = state.water,
                fuel = state.fuel,
                medicine = state.medicine,
                materials = state.materials,
                morale = state.morale,
                unrest = state.unrest,
                sickness = state.sickness,
                siegeIntensity = state.siegeIntensity,
                population = state.TotalPopulation,
                healthy = state.healthyWorkers,
                guards = state.guards,
                sick = state.sick,
                elderly = state.elderly,
            };

            // Collect event log entries
            foreach (var entry in log.Entries)
            {
                if (entry.category == CausalityCategory.Event || entry.category == CausalityCategory.Mission)
                {
                    snap.events.Add(entry.description);
                }
            }

            Data.dayByDaySnapshots.Add(snap);

            // Track averages
            _unrestSum += state.unrest;
            _dayCount++;
            Data.averageUnrest = _unrestSum / _dayCount;

            // First deficit
            if (Data.dayOfFirstDeficit < 0)
            {
                if (state.food == 0) { Data.dayOfFirstDeficit = snap.day; Data.whichResourceFirst = "Food"; }
                else if (state.water == 0) { Data.dayOfFirstDeficit = snap.day; Data.whichResourceFirst = "Water"; }
                else if (state.fuel == 0) { Data.dayOfFirstDeficit = snap.day; Data.whichResourceFirst = "Fuel"; }
            }

            // First zone lost
            if (Data.dayOfFirstZoneLost < 0)
            {
                foreach (var z in state.zones)
                {
                    if (z.isLost)
                    {
                        Data.dayOfFirstZoneLost = snap.day;
                        Data.whichZoneFirst = z.definition.zoneName;
                        break;
                    }
                }
            }

            // Game end
            if (state.isGameOver)
            {
                Data.causeOfLoss = state.gameOverReason;
                Data.dayOfLoss = snap.day;
                Data.finalPopulation = state.TotalPopulation;
            }
            else if (state.isVictory)
            {
                Data.causeOfLoss = "Survived";
                Data.dayOfLoss = 40;
                Data.finalPopulation = state.TotalPopulation;
            }
        }

        public void RecordLawEnacted(LawId lawId, int day)
        {
            if (string.IsNullOrEmpty(Data.firstLawEnacted))
            {
                Data.firstLawEnacted = lawId.ToString();
                Data.firstLawDay = day;
            }
        }

        public void RecordZoneLost(string zoneName, int day)
        {
            if (Data.dayOfFirstZoneLost < 0)
            {
                Data.dayOfFirstZoneLost = day;
                Data.whichZoneFirst = zoneName;
            }
        }

        public void SaveToFile()
        {
            string json = JsonUtility.ToJson(Data, true);
            string path = Path.Combine(Application.persistentDataPath, "telemetry.json");
            File.WriteAllText(path, json);
            Debug.Log($"[Telemetry] Saved to {path}");
        }

        public string GetSummary()
        {
            return $"Profile: {Data.pressureProfile}\n" +
                   $"Seed: {Data.randomSeed}\n" +
                   $"First Deficit: Day {Data.dayOfFirstDeficit} ({Data.whichResourceFirst})\n" +
                   $"First Zone Lost: Day {Data.dayOfFirstZoneLost} ({Data.whichZoneFirst})\n" +
                   $"First Law: {Data.firstLawEnacted} (Day {Data.firstLawDay})\n" +
                   $"Outcome: {Data.causeOfLoss} on Day {Data.dayOfLoss}\n" +
                   $"Final Pop: {Data.finalPopulation}\n" +
                   $"Avg Unrest: {Data.averageUnrest:F1}";
        }
    }
}
