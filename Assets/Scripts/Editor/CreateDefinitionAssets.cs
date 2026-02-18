#if UNITY_EDITOR
using SiegeSurvival.Data;
using UnityEditor;
using UnityEngine;

namespace SiegeSurvival.Editor
{
    /// <summary>
    /// Editor utility that creates all ScriptableObject asset files for the siege prototype.
    /// Run via menu: SiegeSurvival / Create All Definition Assets.
    /// </summary>
    public static class CreateDefinitionAssets
    {
        private const string BasePath = "Assets/Data";

        [MenuItem("SiegeSurvival/Create All Definition Assets")]
        public static void CreateAll()
        {
            CreateZones();
            CreateLaws();
            CreateOrders();
            CreateMissions();
            CreateEvents();
            CreateProfiles();
            CreateIncidents();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CreateDefinitionAssets] All definition assets created.");
        }

        // ==================== Zones ====================

        private static void CreateZones()
        {
            EnsureFolder($"{BasePath}/Zones");

            CreateZone("OuterFarms", "Outer Farms", 0, 80, 70, 85, 20, 1.0f,
                1.5f, 0.6f, 1f, 1f, 0.5f, 1f, 0,
                15, 10, -10, -15, "Food production -40%, Fuel scavenging -50%", false, true);

            CreateZone("OuterResidential", "Outer Residential", 1, 70, 70, 70, 40, 0.9f,
                1f, 1f, 1f, 1f, 1f, 1f, 0,
                15, 10, -10, -15, "Displaced: +overcrowding", false, false);

            CreateZone("ArtisanQuarter", "Artisan Quarter", 2, 75, 75, 75, 25, 0.8f,
                1f, 1f, 1.4f, 0.5f, 1f, 1f, 0,
                10, 0, -5, -15, "Materials production -50%", false, false);

            CreateZone("InnerDistrict", "Inner District", 3, 90, 90, 90, 50, 0.7f,
                1f, 1f, 1f, 1f, 1f, 0.9f, 0,
                25, 0, -20, -15, "Unrest growth modifier lost", false, false);

            CreateZone("Keep", "Keep", 4, 100, 100, 100, 60, 0.6f,
                1f, 1f, 1f, 1f, 1f, 1f, 10,
                0, 0, 0, 0, "GAME OVER — The Keep has fallen", true, false);
        }

        private static void CreateZone(string fileName, string displayName, int order,
            int baseIntegrity, int intMin, int intMax, int cap, float perimFactor,
            float foodMod, float foodLostMod, float matMod, float matLostMod, float fuelLostMod,
            float unrestGrowthMod, int moraleBonus,
            int lossUnrest, int lossSick, int lossMorale, int evacMorale,
            string lossProductionDesc, bool isKeep, bool hasRandom)
        {
            var zone = ScriptableObject.CreateInstance<ZoneDefinition>();
            zone.zoneName = displayName;
            zone.order = order;
            zone.baseIntegrity = baseIntegrity;
            zone.integrityRangeMin = intMin;
            zone.integrityRangeMax = intMax;
            zone.capacity = cap;
            zone.perimeterFactor = perimFactor;
            zone.foodProductionModifier = foodMod;
            zone.foodProductionLostModifier = foodLostMod;
            zone.materialsProductionModifier = matMod;
            zone.materialsProductionLostModifier = matLostMod;
            zone.fuelScavengingLostModifier = fuelLostMod;
            zone.unrestGrowthModifier = unrestGrowthMod;
            zone.moraleBonus = moraleBonus;
            zone.onLossUnrest = lossUnrest;
            zone.onLossSickness = lossSick;
            zone.onLossMorale = lossMorale;
            zone.onEvacMorale = evacMorale;
            zone.onLossProductionDesc = lossProductionDesc;
            zone.isKeep = isKeep;
            zone.hasRandomIntegrity = hasRandom;

            AssetDatabase.CreateAsset(zone, $"{BasePath}/Zones/{fileName}.asset");
        }

        // ==================== Laws ====================

        private static void CreateLaws()
        {
            EnsureFolder($"{BasePath}/Laws");

            CreateLaw("L01_StrictRations", LawId.L1_StrictRations, "Strict Rations",
                "Reduce food rations to stretch supply.",
                "Always available",
                "On Enact: Morale -10\nOngoing: Food consumption -25%, Unrest +5/day");

            CreateLaw("L02_DilutedWater", LawId.L2_DilutedWater, "Diluted Water",
                "Dilute water supply to reduce consumption.",
                "Water < 100 or water deficit occurred",
                "On Enact: Morale -5\nOngoing: Water consumption -20%, Sickness +5/day");

            CreateLaw("L03_ExtendedShifts", LawId.L3_ExtendedShifts, "Extended Shifts",
                "Force workers into longer hours.",
                "Day ≥ 5",
                "On Enact: Morale -15\nOngoing: All production +25%, Sickness +8/day");

            CreateLaw("L04_MandatoryGuardService", LawId.L4_MandatoryGuardService, "Mandatory Guard Service",
                "Draft workers into guard duty.",
                "Unrest > 40",
                "On Enact: 10 Workers → Guards, Morale -10\nOngoing: Food consumption +15/day");

            CreateLaw("L05_EmergencyShelters", LawId.L5_EmergencyShelters, "Emergency Shelters",
                "Open makeshift shelters in the Inner District.",
                "Any zone lost",
                "On Enact: Unrest +10\nOngoing: Inner District +30 capacity, Sickness +10/day");

            CreateLaw("L06_PublicExecutions", LawId.L6_PublicExecutions, "Public Executions",
                "Execute troublemakers publicly.",
                "Unrest > 60",
                "On Enact: Unrest -25, Morale -20, 5 deaths (healthy first)\nOngoing: (none)");

            CreateLaw("L07_FaithProcessions", LawId.L7_FaithProcessions, "Faith Processions",
                "Organize religious processions to boost morale.",
                "Morale < 40",
                "On Enact: Materials -10, Morale +15, Unrest +5\nOngoing: (none)");

            CreateLaw("L08_FoodConfiscation", LawId.L8_FoodConfiscation, "Food Confiscation",
                "Confiscate food from private stores.",
                "Food < 100",
                "On Enact: Food +100, Unrest +20, Morale -20\nOngoing: (none)");

            CreateLaw("L09_MedicalTriage", LawId.L9_MedicalTriage, "Medical Triage",
                "Only treat those likely to survive.",
                "Medicine < 20",
                "On Enact: (none)\nOngoing: Clinic medicine cost -50%, 5 Sick die/day");

            CreateLaw("L10_Curfew", LawId.L10_Curfew, "Curfew",
                "Impose a nighttime curfew.",
                "Unrest > 50",
                "On Enact: (none)\nOngoing: Unrest -10/day, All production -20%");

            CreateLaw("L11_AbandonOuterRing", LawId.L11_AbandonOuterRing, "Abandon Outer Ring",
                "Deliberately abandon the Outer Farms to shorten the defensive perimeter.",
                "Outer Farms Integrity < 40 (and Farms not already lost)",
                "On Enact: Farms lost, zone penalties + Unrest +15\nOngoing: Siege damage ×0.8");

            CreateLaw("L12_MartialLaw", LawId.L12_MartialLaw, "Martial Law",
                "Declare martial law — last resort for order at cost of hope.",
                "Unrest > 75",
                "On Enact: (none)\nOngoing: Unrest capped at 60, Morale capped at 40");
        }

        private static void CreateLaw(string fileName, LawId lawId, string displayName,
            string description, string requirements, string effects)
        {
            var law = ScriptableObject.CreateInstance<LawDefinition>();
            law.lawId = lawId;
            law.displayName = displayName;
            law.description = description;
            law.requirementsDescription = requirements;
            // Split combined effects string into onEnact and ongoing
            var parts = effects.Split('\n');
            law.onEnactEffectsDescription = parts.Length > 0 ? parts[0] : effects;
            law.ongoingEffectsDescription = parts.Length > 1 ? parts[1] : "";
            AssetDatabase.CreateAsset(law, $"{BasePath}/Laws/{fileName}.asset");
        }

        // ==================== Emergency Orders ====================

        private static void CreateOrders()
        {
            EnsureFolder($"{BasePath}/EmergencyOrders");

            CreateOrder("O1_DivertSuppliesToRepairs", EmergencyOrderId.O1_DivertSuppliesToRepairs,
                "Divert Supplies to Repairs",
                "Redirect food and water to repair efforts.",
                "Food -30, Water -20",
                "Repair output +50% today, fixes wells if damaged");

            CreateOrder("O2_SoupKitchens", EmergencyOrderId.O2_SoupKitchens,
                "Soup Kitchens",
                "Open public kitchens to calm the populace.",
                "Food -40",
                "Unrest -15 today");

            CreateOrder("O3_EmergencyWaterRation", EmergencyOrderId.O3_EmergencyWaterRation,
                "Emergency Water Ration",
                "Slash water rations for one day.",
                "(none)",
                "Water consumption -50% today, Sickness +10");

            CreateOrder("O4_CrackdownPatrols", EmergencyOrderId.O4_CrackdownPatrols,
                "Crackdown Patrols",
                "Send guards to crush dissent violently.",
                "2 deaths, Morale -10",
                "Unrest -20 today");

            CreateOrder("O5_QuarantineDistrict", EmergencyOrderId.O5_QuarantineDistrict,
                "Quarantine District",
                "Lock down a zone for health containment.",
                "(none)",
                "All production -50% today, Sickness -10");

            CreateOrder("O6_InspireThePeople", EmergencyOrderId.O6_InspireThePeople,
                "Inspire the People",
                "Spend materials on a public works display.",
                "Materials -15",
                "Morale +15 today");
        }

        private static void CreateOrder(string fileName, EmergencyOrderId orderId,
            string displayName, string description, string costDesc, string effectDesc)
        {
            var order = ScriptableObject.CreateInstance<EmergencyOrderDefinition>();
            order.orderId = orderId;
            order.displayName = displayName;
            order.description = description;
            order.costDescription = costDesc;
            order.effectDescription = effectDesc;
            AssetDatabase.CreateAsset(order, $"{BasePath}/EmergencyOrders/{fileName}.asset");
        }

        // ==================== Missions ====================

        private static void CreateMissions()
        {
            EnsureFolder($"{BasePath}/Missions");

            CreateMission("M1_Forage", MissionId.M1_ForageBeyondWalls,
                "Forage Beyond Walls",
                "Send 10 workers beyond the walls to scavenge food.",
                "Great: +120 Food (60%) | Moderate: +80 Food (25%) | Ambushed: 5 deaths (15%→30% if siege≥4)");

            CreateMission("M2_NightRaid", MissionId.M2_NightRaid,
                "Night Raid on Siege Camp",
                "Attack the siege camp to weaken the enemy. Costs 40 Fuel. +20% capture chance if fuel < 40.",
                "Great: -10 Siege Intensity (40%) | Moderate: -5 Siege Intensity (40%) | Captured: +1 Siege Intensity, 5 deaths, +15 Unrest (20%→40%)");

            CreateMission("M3_SearchHomes", MissionId.M3_SearchAbandonedHomes,
                "Search Abandoned Homes",
                "Loot evacuated buildings for supplies.",
                "Great: +60 Materials, +40 Medicine (50%) | Moderate: +40 Materials (30%) | Trapped: 3 deaths, -20 Materials (20%)");

            CreateMission("M4_BlackMarket", MissionId.M4_NegotiateBlackMarketeers,
                "Negotiate with Black Marketeers",
                "Make contact with smugglers for trade.",
                "Great: Choose resource +100 (45%) | Moderate: +50 Food, +50 Water (30%) | Betrayed: -50 Materials, +10 Unrest (25%)");
        }

        private static void CreateMission(string fileName, MissionId missionId,
            string displayName, string description, string outcomesDesc)
        {
            var mission = ScriptableObject.CreateInstance<MissionDefinition>();
            mission.missionId = missionId;
            mission.displayName = displayName;
            mission.description = description;
            mission.outcomesDescription = outcomesDesc;
            AssetDatabase.CreateAsset(mission, $"{BasePath}/Missions/{fileName}.asset");
        }

        // ==================== Events ====================

        private static void CreateEvents()
        {
            EnsureFolder($"{BasePath}/Events");

            CreateEvent("E1_HungerRiot", EventId.E1_HungerRiot,
                "Hunger Riot",
                "2 consecutive food deficit days + Unrest > 50",
                "Choice: A) Distribute emergency rations (Food -40, Unrest -10), B) Suppress (5 deaths, Unrest -20, Morale -15)");

            CreateEvent("E2_FeverOutbreak", EventId.E2_FeverOutbreak,
                "Plague Flare-up",
                "Sickness > 60",
                "Sickness +15, 3 Sick die. If no Clinic staff: +10 Sickness, +5 Unrest");

            CreateEvent("E3_DesertionWave", EventId.E3_DesertionWave,
                "Despair Event",
                "Morale < 30",
                "Morale -10, 2 Guards desert → 2 Healthy Workers leave");

            CreateEvent("E4_WallBreachAttempt", EventId.E4_WallBreachAttempt,
                "Sortie Request",
                "Active perimeter Integrity < 30, not negated by ≥15 Guards",
                "Choice: A) Send sortie (5 Guard deaths, repair +25 to perimeter), B) Refuse (Morale -10, Unrest +10)");

            CreateEvent("E5_FireInArtisanQuarter", EventId.E5_FireInArtisanQuarter,
                "Supply Interception",
                "Siege Intensity ≥ 4, 10% chance per day",
                "Lose 30% of largest stockpile resource");

            CreateEvent("E6_CouncilRevolt", EventId.E6_CouncilRevolt,
                "Council Revolt",
                "Unrest > 85",
                "GAME OVER: Council deposes the player");

            CreateEvent("E7_TotalCollapse", EventId.E7_TotalCollapse,
                "Total Collapse",
                "Food = 0 AND Water = 0 for 2 consecutive days",
                "GAME OVER: Mass deaths, complete societal breakdown");
        }

        private static void CreateEvent(string fileName, EventId eventId,
            string displayName, string triggerDesc, string effectDesc)
        {
            var evt = ScriptableObject.CreateInstance<EventDefinition>();
            evt.eventId = eventId;
            evt.displayName = displayName;
            evt.triggerDescription = triggerDesc;
            evt.effectDescription = effectDesc;
            AssetDatabase.CreateAsset(evt, $"{BasePath}/Events/{fileName}.asset");
        }

        // ==================== Pressure Profiles ====================

        private static void CreateProfiles()
        {
            EnsureFolder($"{BasePath}/PressureProfiles");

            CreateProfile("P1_DiseaseWave", PressureProfileId.P1_DiseaseWave,
                "Disease Wave",
                "A plague has weakened your people before the siege even began.",
                "Sickness +10, Medicine -10, Food consumption ×0.98");

            CreateProfile("P2_SupplySpoilage", PressureProfileId.P2_SupplySpoilage,
                "Supply Spoilage",
                "Rats destroyed part of your food stores.",
                "Food -60, Unrest +5, Materials +10");

            CreateProfile("P3_SabotagedWells", PressureProfileId.P3_SabotagedWells,
                "Sabotaged Wells",
                "Enemy agents poisoned the well water.",
                "Wells damaged, Morale +10, Unrest -10");

            CreateProfile("P4_HeavyBombardment", PressureProfileId.P4_HeavyBombardment,
                "Heavy Bombardment",
                "The enemy opened with a devastating barrage.",
                "Siege Intensity starts at 2, Outer Farms Integrity = 65, Food +40");
        }

        private static void CreateProfile(string fileName, PressureProfileId profileId,
            string displayName, string description, string effectsDesc)
        {
            var profile = ScriptableObject.CreateInstance<PressureProfileDefinition>();
            profile.profileId = profileId;
            profile.displayName = displayName;
            profile.description = description;
            profile.modificationsSummary = effectsDesc;
            AssetDatabase.CreateAsset(profile, $"{BasePath}/PressureProfiles/{fileName}.asset");
        }

        // ==================== Early Incidents ====================

        private static void CreateIncidents()
        {
            EnsureFolder($"{BasePath}/EarlyIncidents");

            CreateIncident("MinorFire", EarlyIncidentId.MinorFire,
                "Minor Fire",
                "A small fire breaks out in the district.",
                "Materials -15, Sickness +5, Integrity -10 on active perimeter");

            CreateIncident("FeverCluster", EarlyIncidentId.FeverCluster,
                "Fever Cluster",
                "A cluster of fevers sweeps through the populace.",
                "Sickness +15, Medicine -10, Morale -5");

            CreateIncident("FoodTheft", EarlyIncidentId.FoodTheft,
                "Food Theft",
                "Someone raided the food stores in the night.",
                "Food -40, Unrest +10");

            CreateIncident("GuardDesertion", EarlyIncidentId.GuardDesertion,
                "Guard Desertion",
                "Some guards abandon their posts.",
                "Guards -5 (→ Healthy Workers), Morale -5, Unrest +5");
        }

        private static void CreateIncident(string fileName, EarlyIncidentId incidentId,
            string displayName, string description, string effectsDesc)
        {
            var incident = ScriptableObject.CreateInstance<EarlyIncidentDefinition>();
            incident.incidentId = incidentId;
            incident.displayName = displayName;
            incident.effectDescription = effectsDesc;
            AssetDatabase.CreateAsset(incident, $"{BasePath}/EarlyIncidents/{fileName}.asset");
        }

        // ==================== Utility ====================

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
