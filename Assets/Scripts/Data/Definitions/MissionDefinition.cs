using UnityEngine;

namespace SiegeSurvival.Data
{
    [CreateAssetMenu(fileName = "NewMission", menuName = "SiegeSurvival/Mission Definition")]
    public class MissionDefinition : ScriptableObject
    {
        public MissionId missionId;
        public string displayName;
        [TextArea] public string description;
        [TextArea] public string outcomesDescription;
    }
}
