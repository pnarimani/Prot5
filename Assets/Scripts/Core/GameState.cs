using System.Collections.Generic;
using System.Linq;
using SiegeSurvival.Data;
using SiegeSurvival.Data.Runtime;
using UnityEngine;

namespace SiegeSurvival.Core
{
    /// <summary>
    /// Holds ALL mutable state for a single run. Pure C# (no MonoBehaviour).
    /// </summary>
    [System.Serializable]
    public class GameState
    {
        // --- Day ---
        public int currentDay = 1;

        // --- Resources ---
        public int food = 320;
        public int water = 360;
        public int fuel = 240;
        public int medicine = 40;
        public int materials = 120;

        // --- Meters ---
        public int morale = 55;
        public int unrest = 25;
        public int sickness = 20;
        public int siegeIntensity = 1;

        // --- Population ---
        public int healthyWorkers = 85;
        public int guards = 10;
        public int sick = 15;
        public int elderly = 10;

        // --- Zones ---
        public ZoneState[] zones; // length 5, index 0=Farms .. 4=Keep

        // --- Laws ---
        public List<LawId> enactedLaws = new();
        public int daysSinceLastLaw = 3; // can enact on Day 1

        // --- Emergency Orders ---
        public EmergencyOrderId? todayEmergencyOrder;
        public int quarantineZoneIndex = -1; // for O5

        // --- Missions ---
        public ActiveMission activeMission;
        public NightRaidDebuff nightRaidDebuff;

        // --- Scheduled Action (one per day, executed on EndDay) ---
        public LawId? scheduledLaw;
        public EmergencyOrderId? scheduledOrder;
        public int scheduledQuarantineZone = -1;
        public MissionId? scheduledMission;

        // --- Wells ---
        public bool wellsDamaged;

        // --- Pressure Profile ---
        public PressureProfileId activeProfile;
        public float profileFoodConsumptionMult = 1f; // P1 Disease Wave applies ×0.98

        // --- Scheduled early incidents ---
        public List<ScheduledIncident> scheduledIncidents = new();

        // --- Tracking ---
        public int consecutiveFoodWaterZeroDays;
        public int consecutiveFoodDeficitDays;
        public int daysSinceLastLawEnacted; // for "no law in 3 days" unrest

        // --- Worker allocation ---
        public Dictionary<JobSlot, int> workerAllocation = new()
        {
            { JobSlot.FoodProduction, 0 },
            { JobSlot.WaterDrawing, 0 },
            { JobSlot.MaterialsCrafting, 0 },
            { JobSlot.Repairs, 0 },
            { JobSlot.Sanitation, 0 },
            { JobSlot.ClinicStaff, 0 },
            { JobSlot.FuelScavenging, 0 },
        };

        // --- End state ---
        public bool isGameOver;
        public string gameOverReason;
        public bool isVictory;

        // ============= Computed Properties =============

        public int TotalPopulation => healthyWorkers + guards + sick + elderly;

        public int AvailableWorkers => healthyWorkers - (activeMission != null ? activeMission.workersCommitted : 0);

        public int AssignedWorkers
        {
            get
            {
                int sum = 0;
                foreach (var kvp in workerAllocation)
                {
                    if (kvp.Key != JobSlot.GuardDuty)
                        sum += kvp.Value;
                }
                return sum;
            }
        }

        public int IdleWorkers => System.Math.Max(0, AvailableWorkers - AssignedWorkers);

        public float IdlePercent => TotalPopulation > 0 ? IdleWorkers / (float)TotalPopulation * 100f : 0f;

        // ============= Zone Helpers =============

        /// <summary>Returns the outermost non-lost zone (active perimeter).</summary>
        public ZoneState GetActivePerimeter()
        {
            for (int i = 0; i < zones.Length; i++)
            {
                if (!zones[i].isLost) return zones[i];
            }
            return zones[4]; // Keep (should never reach here unless Keep is lost)
        }

        /// <summary>Returns zone by order index (0-4).</summary>
        public ZoneState GetZone(int order) => zones[order];

        public ZoneState OuterFarms => zones[0];
        public ZoneState OuterResidential => zones[1];
        public ZoneState ArtisanQuarter => zones[2];
        public ZoneState InnerDistrict => zones[3];
        public ZoneState Keep => zones[4];

        public bool AnyZoneLost => zones.Any(z => z.isLost);

        public bool AnyZoneOvercrowded
        {
            get
            {
                foreach (var z in zones)
                {
                    if (!z.isLost && z.IsOvercrowded) return true;
                }
                return false;
            }
        }

        /// <summary>Number of non-lost zones with ≥20% overcrowding.</summary>
        public int ZonesOver20PctCount
        {
            get
            {
                int count = 0;
                foreach (var z in zones)
                {
                    if (!z.isLost && z.IsOvercrowded20Pct) count++;
                }
                return count;
            }
        }

        // ============= Meter Helpers =============

        /// <summary>Clamps morale, unrest, and sickness to [0, 100].</summary>
        public void ClampAllMeters()
        {
            morale = Mathf.Clamp(morale, 0, 100);
            unrest = Mathf.Clamp(unrest, 0, 100);
            sickness = Mathf.Clamp(sickness, 0, 100);
        }
    }
}
