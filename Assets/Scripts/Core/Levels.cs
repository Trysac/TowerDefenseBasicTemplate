using System;
using System.Collections.Generic;

namespace SaucerSiege.Core
{
    public enum Theme
    {
        Meadow,
        Snow,
    }

    /// <summary>One wave: its groups (kinds are <see cref="EnemyKind"/> values) and how tough its enemies are.</summary>
    public sealed class WaveDefinition
    {
        public WaveDefinition(IReadOnlyList<WaveGroup> groups, float healthScale)
        {
            Groups = groups ?? throw new ArgumentNullException(nameof(groups));
            if (healthScale <= 0f) throw new ArgumentOutOfRangeException(nameof(healthScale));
            HealthScale = healthScale;
        }

        public IReadOnlyList<WaveGroup> Groups { get; }

        /// <summary>Multiplier on every enemy's base health.</summary>
        public float HealthScale { get; }

        /// <summary>How many enemies of <paramref name="kind"/> the wave sends (for the wave preview).</summary>
        public int Count(EnemyKind kind)
        {
            int total = 0;
            foreach (WaveGroup group in Groups)
            {
                if (group.Kind == (int)kind) total += group.Count;
            }
            return total;
        }

        /// <summary>Health of one <paramref name="kind"/> in this wave.</summary>
        public int HealthOf(EnemyKind kind) => Math.Max(1, (int)Math.Round(Catalog.Enemy(kind).Health * HealthScale));
    }

    public sealed class LevelDefinition
    {
        public LevelDefinition(string name, string tagline, Theme theme, int startingMoney, IReadOnlyList<string> rows, IReadOnlyList<WaveDefinition> waves)
        {
            Name = name;
            Tagline = tagline;
            Theme = theme;
            StartingMoney = startingMoney;
            Rows = rows;
            Waves = waves;
        }

        public string Name { get; }
        public string Tagline { get; }
        public Theme Theme { get; }
        public int StartingMoney { get; }
        public IReadOnlyList<string> Rows { get; }
        public IReadOnlyList<WaveDefinition> Waves { get; }
    }

    /// <summary>The three levels of the game, from gentle to hard.</summary>
    public static class Levels
    {
        public static readonly IReadOnlyList<LevelDefinition> All = new[]
        {
            new LevelDefinition("Green Hollow", "A quiet valley and a long, winding road.", Theme.Meadow, 170, new[]
            {
                "T..R........T..",
                "S######.....C..",
                "......#........",
                ".T....#..#####.",
                "......#..#...#.",
                "..R...####...#.",
                ".............#.",
                ".T.....B######.",
                "...C.....R...T.",
            }, Plan(8, 0.15f)),
            new LevelDefinition("Crystal Pass", "Three switchbacks through the crystal fields.", Theme.Meadow, 200, new[]
            {
                "..C....T....C..",
                "...#####...###.",
                ".T.#...#...#.#.",
                "...#.C.#...#.#.",
                "...#...#.R.#.#.",
                ".R.#...#...#.B.",
                "...#...#...#...",
                "S###...#####..T",
                "..T....C.......",
            }, Plan(10, 0.25f)),
            new LevelDefinition("Frostbite Ridge", "A short road on the ice, and the mothership is coming.", Theme.Snow, 240, new[]
            {
                "T......S....T..",
                "..R....#....B.R",
                "..######..C.#..",
                ".T#.........#..",
                "..#..R..T...#..",
                "..#.........#.T",
                "..###########..",
                ".C....T.....R..",
                "...T......C....",
            }, Plan(12, 0.34f)),
        };

        /// <summary>
        /// The waves of a level: a growing stream of scouts, raiders from wave 2, tanks from wave 4,
        /// a fast scout rush every third wave and the mothership in the last one. Health grows by
        /// <paramref name="growth"/> per wave.
        /// </summary>
        public static WaveDefinition[] Plan(int count, float growth)
        {
            var waves = new WaveDefinition[count];
            for (int n = 1; n <= count; n++)
            {
                var groups = new List<WaveGroup>
                {
                    new WaveGroup((int)EnemyKind.Scout, 4 + 2 * n, 0f, Math.Max(0.4f, 0.8f - 0.03f * n)),
                };
                if (n >= 2)
                {
                    groups.Add(new WaveGroup((int)EnemyKind.Raider, 1 + n, 1.5f, Math.Max(0.7f, 1.2f - 0.04f * n)));
                }
                if (n >= 4)
                {
                    groups.Add(new WaveGroup((int)EnemyKind.Tank, (n - 2) / 2, 2f, 2.4f));
                }
                if (n % 3 == 0)
                {
                    groups.Add(new WaveGroup((int)EnemyKind.Scout, 6 + n, 1.5f, 0.28f));
                }
                if (n == count)
                {
                    groups.Add(new WaveGroup((int)EnemyKind.Mothership, 1, 3f, 1f));
                    groups.Add(new WaveGroup((int)EnemyKind.Tank, 2, 1f, 1.5f));
                }
                waves[n - 1] = new WaveDefinition(groups, 1f + growth * (n - 1));
            }
            return waves;
        }
    }
}
