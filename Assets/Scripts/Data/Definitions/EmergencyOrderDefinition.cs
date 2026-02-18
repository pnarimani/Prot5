using UnityEngine;

namespace SiegeSurvival.Data
{
    [CreateAssetMenu(fileName = "NewOrder", menuName = "SiegeSurvival/Emergency Order Definition")]
    public class EmergencyOrderDefinition : ScriptableObject
    {
        public EmergencyOrderId orderId;
        public string displayName;
        [TextArea] public string description;
        [TextArea] public string costDescription;
        [TextArea] public string effectDescription;
    }
}
