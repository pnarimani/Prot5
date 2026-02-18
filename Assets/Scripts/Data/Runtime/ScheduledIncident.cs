namespace SiegeSurvival.Data.Runtime
{
    [System.Serializable]
    public class ScheduledIncident
    {
        public EarlyIncidentId incidentId;
        public int scheduledDay;
        public bool resolved;

        public ScheduledIncident(EarlyIncidentId id, int day)
        {
            incidentId = id;
            scheduledDay = day;
            resolved = false;
        }
    }
}
