using NUnit.Framework;

namespace SaucerSiege.Core.Tests
{
    public class EnemyHealthTests
    {
        [Test]
        public void HitsTakeHealthUntilTheKillingBlow()
        {
            var health = new EnemyHealth(5);
            Assert.That(health.Hit(2), Is.False);
            Assert.That(health.Current, Is.EqualTo(3));
            Assert.That(health.Fraction, Is.EqualTo(0.6f).Within(1e-5f));
            Assert.That(health.Hit(3), Is.True);
            Assert.That(health.IsDead, Is.True);
            Assert.That(health.Fraction, Is.EqualTo(0f));
        }

        [Test]
        public void AHitAfterTheKillingBlowDoesNotKillAgain()
        {
            // Two projectiles landing in the same frame used to pay the bounty twice and
            // count the enemy as destroyed twice, so the wave never ended.
            var health = new EnemyHealth(3);
            Assert.That(health.Hit(3), Is.True);
            Assert.That(health.Hit(2), Is.False);
            Assert.That(health.Current, Is.EqualTo(0));
            Assert.That(health.IsDead, Is.True);
        }
    }
}
