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

        /// <summary>
        /// Applies a hit. Returns true only for the killing blow, so the bounty is paid and the
        /// enemy removed once; hits on a dead enemy change nothing.
        /// </summary>
        public bool Hit(int damage)
        {
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
            if (IsDead)
            {
                return false;
            }
            Current = Math.Max(0, Current - damage);
            return Current == 0;
        }
    }
}
