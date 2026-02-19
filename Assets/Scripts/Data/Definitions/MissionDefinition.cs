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
        [Tooltip("How many days this mission takes to complete. 0 or negative defaults to 5.")]
        public int durationDays = 5;

        public int Duration => durationDays > 0 ? durationDays : 5;
    }
}
