using System.Collections.Generic;

namespace SiegeSurvival.Core
{
    public enum CausalityCategory
    {
        Food, Water, Fuel, Medicine, Materials,
        Morale, Unrest, Sickness,
        Integrity, SiegeDamage,
        Population, Death,
        Event, Mission, Law, EmergencyOrder,
        Production, Consumption, Overcrowding,
        General
    }

    [System.Serializable]
    public struct CausalityEntry
    {
        public CausalityCategory category;
        public string source;
        public int value;          // flat change (+5, -10, etc.)
        public float multiplier;   // production/consumption modifier (0.75, 1.25, etc.; 0 means N/A)
        public string description;

        public static CausalityEntry Flat(CausalityCategory cat, string source, int value, string desc)
        {
            return new CausalityEntry
            {
                category = cat,
                source = source,
                value = value,
                multiplier = 0f,
                description = desc
            };
        }

        public static CausalityEntry Mult(CausalityCategory cat, string source, float mult, string desc)
        {
            return new CausalityEntry
            {
                category = cat,
                source = source,
                value = 0,
                multiplier = mult,
                description = desc
            };
        }
    }

    public class CausalityLog
    {
        private readonly List<CausalityEntry> _entries = new();

        public IReadOnlyList<CausalityEntry> Entries => _entries;

        public void Add(CausalityEntry entry) => _entries.Add(entry);

        public void AddFlat(CausalityCategory cat, string source, int value, string desc)
            => _entries.Add(CausalityEntry.Flat(cat, source, value, desc));

        public void AddMult(CausalityCategory cat, string source, float mult, string desc)
            => _entries.Add(CausalityEntry.Mult(cat, source, mult, desc));

        public void Clear() => _entries.Clear();

        public List<CausalityEntry> GetByCategory(CausalityCategory cat)
        {
            var result = new List<CausalityEntry>();
            foreach (var e in _entries)
            {
                if (e.category == cat) result.Add(e);
            }
            return result;
        }
    }
}
