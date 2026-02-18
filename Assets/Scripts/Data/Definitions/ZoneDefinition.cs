using UnityEngine;

namespace SiegeSurvival.Data
{
    [CreateAssetMenu(fileName = "NewZone", menuName = "SiegeSurvival/Zone Definition")]
    public class ZoneDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string zoneName;
        public int order; // 0=Outer Farms, 1=Outer Residential, 2=Artisan, 3=Inner, 4=Keep

        [Header("Base Stats")]
        public int baseIntegrity;
        public int integrityRangeMin; // For random (Farms: 70)
        public int integrityRangeMax; // For random (Farms: 85)
        public int capacity;

        [Header("Siege")]
        public float perimeterFactor; // 1.0 / 0.9 / 0.8 / 0.7 / 0.6

        [Header("Production Modifiers (while intact / when lost)")]
        public float foodProductionModifier = 1f;
        public float foodProductionLostModifier = 1f;
        public float materialsProductionModifier = 1f;
        public float materialsProductionLostModifier = 1f;
        public float fuelScavengingLostModifier = 1f;

        [Header("Meter Modifiers (while intact)")]
        public float unrestGrowthModifier = 1f; // 0.9 for Inner District
        public int moraleBonus; // +10 for Keep

        [Header("On-Loss Effects")]
        public int onLossUnrest;
        public int onLossSickness;
        public int onLossMorale; // negative
        public int onEvacMorale; // negative, typically -15
        [TextArea] public string onLossProductionDesc;

        [Header("Flags")]
        public bool isKeep;
        public bool hasRandomIntegrity; // true for Outer Farms
    }
}
