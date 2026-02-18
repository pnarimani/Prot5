using UnityEngine;

namespace SiegeSurvival.Data
{
    [CreateAssetMenu(fileName = "NewLaw", menuName = "SiegeSurvival/Law Definition")]
    public class LawDefinition : ScriptableObject
    {
        public LawId lawId;
        public string displayName;
        [TextArea] public string description;
        [TextArea] public string requirementsDescription;
        [TextArea] public string onEnactEffectsDescription;
        [TextArea] public string ongoingEffectsDescription;
    }
}
