using System.Collections.Generic;

namespace SiegeSurvival.Core
{
    /// <summary>
    /// Seeded random provider for deterministic runs.
    /// </summary>
    public class RandomProvider
    {
        private readonly System.Random _rng;
        public int Seed { get; }

        public RandomProvider(int seed)
        {
            Seed = seed;
            _rng = new System.Random(seed);
        }

        public RandomProvider() : this(System.Environment.TickCount) { }

        /// <summary>Returns int in [min, maxExclusive).</summary>
        public int Range(int min, int maxExclusive) => _rng.Next(min, maxExclusive);

        /// <summary>Returns float in [0, 1).</summary>
        public float Range01() => (float)_rng.NextDouble();

        /// <summary>Returns true with the given probability [0‥1].</summary>
        public bool Chance(float probability) => Range01() < probability;

        /// <summary>Shuffle a list in place (Fisher–Yates).</summary>
        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
