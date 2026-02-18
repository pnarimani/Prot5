using UnityEngine;

namespace SiegeSurvival.Data.Runtime
{
    [System.Serializable]
    public class ZoneState
    {
        public ZoneDefinition definition;
        public int currentIntegrity;
        public int currentPopulation;
        public bool isLost;
        public int effectiveCapacity;

        public ZoneState(ZoneDefinition def, int integrity)
        {
            definition = def;
            currentIntegrity = integrity;
            currentPopulation = 0;
            isLost = false;
            effectiveCapacity = def.capacity;
        }

        public float OvercrowdingPercent
        {
            get
            {
                if (effectiveCapacity <= 0 || currentPopulation <= effectiveCapacity) return 0f;
                return (currentPopulation - effectiveCapacity) / (float)effectiveCapacity * 100f;
            }
        }

        public int OvercrowdingTiers10Pct => Mathf.FloorToInt(OvercrowdingPercent / 10f);

        public bool IsOvercrowded => currentPopulation > effectiveCapacity;

        public bool IsOvercrowded20Pct => currentPopulation > effectiveCapacity * 1.2f;
    }
}
