namespace SiegeSurvival.Data.Runtime
{
    [System.Serializable]
    public class NightRaidDebuff
    {
        public int intensityReduction; // 5 or 10
        public int daysRemaining;

        public NightRaidDebuff(int reduction, int days)
        {
            intensityReduction = reduction;
            daysRemaining = days;
        }
    }
}
