using NUnit.Framework;

namespace SaucerSiege.Core.Tests
{
    public class EconomyTests
    {
        [Test]
        public void SpendingTakesTheCostWhenAffordable()
        {
            var economy = new Economy(100, 10);
            Assert.That(economy.TrySpend(60), Is.True);
            Assert.That(economy.Money, Is.EqualTo(40));
        }

        [Test]
        public void SpendingMoreThanThePlayerHasChangesNothing()
        {
            var economy = new Economy(50, 10);
            Assert.That(economy.CanAfford(51), Is.False);
            Assert.That(economy.TrySpend(51), Is.False);
            Assert.That(economy.Money, Is.EqualTo(50));
        }

        [Test]
        public void EarningAddsMoney()
        {
            var economy = new Economy(0, 10);
            economy.Earn(25);
            Assert.That(economy.Money, Is.EqualTo(25));
        }

        [Test]
        public void LeaksCostLivesAndTheLastOneDefeatsTheBase()
        {
            var economy = new Economy(0, 3);
            Assert.That(economy.Leak(2), Is.False);
            Assert.That(economy.Lives, Is.EqualTo(1));
            Assert.That(economy.IsDefeated, Is.False);
            Assert.That(economy.Leak(1), Is.True);
            Assert.That(economy.IsDefeated, Is.True);
            Assert.That(economy.StartingLives, Is.EqualTo(3));
        }

        [Test]
        public void TheBaseFallsOnlyOnce()
        {
            // Every leak after the base fell used to report the defeat again and reopen the
            // game over screen.
            var economy = new Economy(0, 1);
            Assert.That(economy.Leak(1), Is.True);
            Assert.That(economy.Leak(1), Is.False);
            Assert.That(economy.Lives, Is.EqualTo(0));
            Assert.That(economy.IsDefeated, Is.True);
        }
    }
}
