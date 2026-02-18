using UnityEngine;

namespace SiegeSurvival.Data
{
    [CreateAssetMenu(fileName = "NewIncident", menuName = "SiegeSurvival/Early Incident Definition")]
    public class EarlyIncidentDefinition : ScriptableObject
    {
        public EarlyIncidentId incidentId;
        public string displayName;
        [TextArea] public string effectDescription;
    }
}
