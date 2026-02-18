using UnityEngine;

namespace SiegeSurvival.Data
{
    [CreateAssetMenu(fileName = "NewEvent", menuName = "SiegeSurvival/Event Definition")]
    public class EventDefinition : ScriptableObject
    {
        public EventId eventId;
        public string displayName;
        [TextArea] public string triggerDescription;
        [TextArea] public string effectDescription;
    }
}
