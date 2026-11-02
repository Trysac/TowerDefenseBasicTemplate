using System;

namespace SaucerSiege.Core
{
    /// <summary>
    /// The player's money and the base's lives. Buying refuses what the player cannot afford,
    /// and the base reports its fall from the leak that destroyed it.
    /// </summary>
    public sealed class Economy
    {
        public Economy(int money, int lives)
        {
            if (money < 0) throw new ArgumentOutOfRangeException(nameof(money));
            if (lives <= 0) throw new ArgumentOutOfRangeException(nameof(lives));
            Money = money;
            Lives = lives;
            StartingLives = lives;
        }

        public int Money { get; private set; }

        public int Lives { get; private set; }

        public int StartingLives { get; }

        public bool IsDefeated => Lives <= 0;

        public bool CanAfford(int cost) => cost >= 0 && cost <= Money;

        /// <summary>Takes <paramref name="cost"/> if the player has it; otherwise changes nothing.</summary>
        public bool TrySpend(int cost)
        {
            if (!CanAfford(cost))
            {
                return false;
            }
            Money -= cost;
            return true;
        }

        public void Earn(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Money += amount;
        }

        /// <summary>
        /// An enemy reached the base and costs it <paramref name="damage"/> lives.
        /// Returns true when the base has fallen.
        /// </summary>
        public bool Leak(int damage)
        {
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
            Lives -= damage;
            return Lives <= 0;
        }
    }
}
