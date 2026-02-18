namespace SiegeSurvival.Data.Runtime
{
    [System.Serializable]
    public class ActiveMission
    {
        public MissionId missionId;
        public int startDay;
        public int workersCommitted;
        public bool fuelWasInsufficient; // For M2: true if fuel < 40 at start

        public ActiveMission(MissionId id, int day, int workers, bool fuelInsufficient = false)
        {
            missionId = id;
            startDay = day;
            workersCommitted = workers;
            fuelWasInsufficient = fuelInsufficient;
        }
    }
}
