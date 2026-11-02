using System;
using System.Collections.Generic;

namespace SaucerSiege.Core
{
    /// <summary>
    /// One group of a wave: after <see cref="Delay"/> seconds, <see cref="Count"/> enemies of the
    /// same kind appear <see cref="Interval"/> seconds apart. The next group's delay starts one
    /// interval after this group's last enemy.
    /// </summary>
    public readonly struct WaveGroup
    {
        public WaveGroup(int kind, int count, float delay, float interval)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (delay < 0f) throw new ArgumentOutOfRangeException(nameof(delay));
            if (interval < 0f) throw new ArgumentOutOfRangeException(nameof(interval));
            Kind = kind;
            Count = count;
            Delay = delay;
            Interval = interval;
        }

        /// <summary>What to spawn; the caller decides what the number means.</summary>
        public int Kind { get; }

        public int Count { get; }

        public float Delay { get; }

        public float Interval { get; }
    }

    /// <summary>An enemy the wave wants spawned now.</summary>
    public readonly struct WaveSpawn
    {
        public WaveSpawn(int group, int kind)
        {
            Group = group;
            Kind = kind;
        }

        public int Group { get; }

        public int Kind { get; }
    }

    /// <summary>
    /// A wave being played: spawns its groups over time and tracks how many of its enemies
    /// are still on the field.
    /// </summary>
    public sealed class WaveRun
    {
        readonly IReadOnlyList<WaveGroup> groups;
        int group;
        int spawnedInGroup;
        float clock;
        float nextSpawnAt;

        public WaveRun(IReadOnlyList<WaveGroup> groups)
        {
            this.groups = groups ?? throw new ArgumentNullException(nameof(groups));
            foreach (WaveGroup g in groups)
            {
                Planned += g.Count;
            }
            EnterGroup(0, 0f);
        }

        /// <summary>Enemies the wave spawns in total.</summary>
        public int Planned { get; }

        public int Spawned { get; private set; }

        /// <summary>Spawned enemies not yet killed or leaked.</summary>
        public int Alive { get; private set; }

        public bool SpawningDone => Spawned >= Planned;

        /// <summary>True once the wave is over: everything has spawned and nothing of it is left.</summary>
        public bool IsCleared => SpawningDone && Alive == 0;

        /// <summary>Moves the wave's clock and adds every enemy now due to <paramref name="spawns"/>.</summary>
        public void Advance(float deltaTime, List<WaveSpawn> spawns)
        {
            clock += deltaTime;
            while (group < groups.Count && clock >= nextSpawnAt)
            {
                WaveGroup current = groups[group];
                spawns.Add(new WaveSpawn(group, current.Kind));
                Spawned++;
                Alive++;
                spawnedInGroup++;
                float due = nextSpawnAt + current.Interval;
                if (spawnedInGroup >= current.Count)
                {
                    EnterGroup(group + 1, due);
                }
                else
                {
                    nextSpawnAt = due;
                }
            }
        }

        /// <summary>One of this wave's enemies was killed or reached the base.</summary>
        public void Remove()
        {
            if (Alive > 0)
            {
                Alive--;
            }
        }

        void EnterGroup(int index, float previousEnd)
        {
            group = index;
            spawnedInGroup = 0;
            while (group < groups.Count && groups[group].Count == 0)
            {
                previousEnd += groups[group].Delay;
                group++;
            }
            if (group < groups.Count)
            {
                nextSpawnAt = previousEnd + groups[group].Delay;
            }
        }
    }
}
