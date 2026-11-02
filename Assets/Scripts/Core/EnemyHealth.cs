using System;

namespace SaucerSiege.Core
{
    /// <summary>An enemy's hit points.</summary>
    public sealed class EnemyHealth
    {
        public EnemyHealth(int max)
        {
            if (max <= 0) throw new ArgumentOutOfRangeException(nameof(max));
            Max = max;
            Current = max;
        }

        public int Max { get; }

        public int Current { get; private set; }

        public bool IsDead => Current <= 0;

        /// <summary>Fraction of health left, from 1 (untouched) to 0 (dead).</summary>
        public float Fraction => Math.Max(0, Current) / (float)Max;

        /// <summary>Applies a hit. Returns true when the enemy is dead and must pay out and be removed.</summary>
        public bool Hit(int damage)
        {
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
            Current -= damage;
            return Current <= 0;
        }
    }
}
