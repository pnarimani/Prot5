namespace SiegeSurvival.Core
{
    /// <summary>
    /// A single structured report card shown to the player at end of day.
    /// </summary>
    public class DayReportEntry
    {
        public string Title;
        public string Description;

        public DayReportEntry(string title, string description)
        {
            Title = title;
            Description = description;
        }
    }
}
