using System.Collections.Generic;

namespace SaucerSiege.Core
{
    /// <summary>Which enemy in range a tower shoots at.</summary>
    public enum TargetPriority
    {
        /// <summary>The enemy furthest along the path (closest to the base).</summary>
        First,
        /// <summary>The enemy nearest to the tower.</summary>
        Close,
        /// <summary>The enemy with the most health left.</summary>
        Strong,
        /// <summary>The enemy least far along the path.</summary>
        Last,
    }

    /// <summary>What a tower knows about an enemy when it picks a target.</summary>
    public readonly struct TargetCandidate
    {
        public TargetCandidate(Vec2 position, float progress, int health)
        {
            Position = position;
            Progress = progress;
            Health = health;
        }

        public Vec2 Position { get; }

        /// <summary>Distance travelled along the path.</summary>
        public float Progress { get; }

        public int Health { get; }
    }

    public static class Targeting
    {
        /// <summary>
        /// Index of the candidate to shoot within <paramref name="range"/> of <paramref name="origin"/>,
        /// or -1 when none is in range. Ties go to the earlier candidate.
        /// </summary>
        public static int Pick(IReadOnlyList<TargetCandidate> candidates, Vec2 origin, float range, TargetPriority priority)
        {
            int best = -1;
            float bestScore = 0f;
            float closest = float.MaxValue;
            float rangeSquared = range * range;
            for (int i = 0; i < candidates.Count; i++)
            {
                TargetCandidate candidate = candidates[i];
                float distanceSquared = (candidate.Position - origin).LengthSquared;
                if (distanceSquared > rangeSquared)
                {
                    continue;
                }
                switch (priority)
                {
                    case TargetPriority.First:
                        if (best < 0 || candidate.Progress > bestScore)
                        {
                            best = i;
                            bestScore = candidate.Progress;
                        }
                        break;
                    case TargetPriority.Last:
                        if (best < 0 || candidate.Progress < bestScore)
                        {
                            best = i;
                            bestScore = candidate.Progress;
                        }
                        break;
                    case TargetPriority.Close:
                        if (distanceSquared < closest)
                        {
                            best = i;
                            closest = distanceSquared;
                        }
                        break;
                    case TargetPriority.Strong:
                        if (best < 0 || candidate.Health > bestScore)
                        {
                            best = i;
                            bestScore = candidate.Health;
                        }
                        break;
                }
            }
            return best;
        }
    }
}
