using System.Collections.Generic;
using SiegeSurvival.Data;
using UnityEngine;

namespace SiegeSurvival.Core
{
    /// <summary>
    /// Loads and caches all ScriptableObject definitions once.
    /// Replaces scattered Resources.LoadAll calls throughout the codebase.
    /// </summary>
    public static class DefinitionRegistry
    {
        private static Dictionary<LawId, LawDefinition> _laws;
        private static Dictionary<MissionId, MissionDefinition> _missions;
        private static Dictionary<EmergencyOrderId, EmergencyOrderDefinition> _orders;

        private static bool _initialized;

        public static void EnsureLoaded()
        {
            if (_initialized) return;

            _laws = new Dictionary<LawId, LawDefinition>();
            foreach (var def in Resources.LoadAll<LawDefinition>("Data/Laws"))
                _laws[def.lawId] = def;

            _missions = new Dictionary<MissionId, MissionDefinition>();
            foreach (var def in Resources.LoadAll<MissionDefinition>("Data/Missions"))
                _missions[def.missionId] = def;

            _orders = new Dictionary<EmergencyOrderId, EmergencyOrderDefinition>();
            foreach (var def in Resources.LoadAll<EmergencyOrderDefinition>("Data/EmergencyOrders"))
                _orders[def.orderId] = def;

            _initialized = true;
        }

        public static LawDefinition GetLaw(LawId id)
        {
            EnsureLoaded();
            return _laws.TryGetValue(id, out var def) ? def : null;
        }

        public static MissionDefinition GetMission(MissionId id)
        {
            EnsureLoaded();
            return _missions.TryGetValue(id, out var def) ? def : null;
        }

        public static EmergencyOrderDefinition GetOrder(EmergencyOrderId id)
        {
            EnsureLoaded();
            return _orders.TryGetValue(id, out var def) ? def : null;
        }

        public static IEnumerable<LawDefinition> AllLaws
        {
            get { EnsureLoaded(); return _laws.Values; }
        }

        public static IEnumerable<MissionDefinition> AllMissions
        {
            get { EnsureLoaded(); return _missions.Values; }
        }

        public static IEnumerable<EmergencyOrderDefinition> AllOrders
        {
            get { EnsureLoaded(); return _orders.Values; }
        }
    }
}
