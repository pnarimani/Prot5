using UnityEngine;

namespace SiegeSurvival.Data
{
    [CreateAssetMenu(fileName = "NewProfile", menuName = "SiegeSurvival/Pressure Profile Definition")]
    public class PressureProfileDefinition : ScriptableObject
    {
        public PressureProfileId profileId;
        public string displayName;
        [TextArea] public string description;
        [TextArea] public string modificationsSummary;
    }
}
